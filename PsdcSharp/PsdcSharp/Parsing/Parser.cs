
using Scover.Psdc.Tokenization;
using static Scover.Psdc.Tokenization.TokenType;
using static Scover.Psdc.Parsing.Node;
using Type = Scover.Psdc.Parsing.Node.Type;

namespace Scover.Psdc.Parsing;

internal sealed partial class Parser
{
    private readonly Messenger _messenger;
    private readonly IEnumerable<Token> _tokens;

    private readonly IReadOnlyDictionary<TokenType, Parser<Declaration>> _declarationParsers;
    private readonly IReadOnlyDictionary<TokenType, Parser<Statement>> _statementParsers;
    private readonly IReadOnlyDictionary<TokenType, Parser<Type.Complete>> _completeTypeParsers;

    public Parser(Messenger messenger, IEnumerable<Token> tokens)
    {
        _messenger = messenger;
        _tokens = tokens;

        _declarationParsers = new Dictionary<TokenType, Parser<Declaration>>() {
            [Keyword.Begin] = ParseMainProgram,
            [Keyword.Constant] = ParseConstant,
            [Keyword.Function] = ParseFunctionDeclarationOrDefinition,
            [Keyword.Procedure] = ParseProcedureDeclarationOrDefinition,
            [Keyword.TypeAlias] = ParseAliasDeclaration,
        };

        _statementParsers = new Dictionary<TokenType, Parser<Statement>>() {
            [Valued.Identifier] = ParseAnyOf(ParserVariableDeclarationOrProcedureCall, ParseAssignment),
            [Keyword.Do] = ParseDoWhileLoop,
            [Keyword.For] = ParseForLoop,
            [Keyword.If] = ParseAlternative,
            [Keyword.Repeat] = ParseRepeatLoop,
            [Keyword.Return] = ParseReturn,
            [Keyword.Switch] = ParseSwitch,
            [Keyword.While] = ParseWhileLoop,
            [Punctuation.Semicolon] = ParseNop,
        };

        _completeTypeParsers = new Dictionary<TokenType, Parser<Type.Complete>> {
            [Keyword.Integer] = MakeNumericParser(NumericType.Integer),
            [Keyword.Real] = MakeNumericParser(NumericType.Real),
            [Keyword.Character] = MakeAlwaysOkParser(1, t => new Type.Complete.Character(t)),
            [Keyword.Boolean] = MakeAlwaysOkParser(1, t => new Type.Complete.Boolean(t)),
            [Keyword.File] = MakeAlwaysOkParser(1, t => new Type.Complete.File(t)),
            [Keyword.String] = ParseTypeStringLengthed,
            [Keyword.Array] = ParseTypeArray,
            [Valued.Identifier] = MakeAlwaysOkParser((t, val) => new Type.Complete.AliasReference(t, new(t, val))),
            [Keyword.Structure] = ParseTypeStructure,
        };

        static Parser<Type.Complete.Numeric> MakeNumericParser(NumericType type)
         => MakeAlwaysOkParser(1, t => new Type.Complete.Numeric(t, type));

        _literalParsers = new Dictionary<TokenType, Parser<Expression>> {
            [Keyword.False] = tokens
             => ParseToken(tokens, Keyword.False, t => new Expression.Literal.False(t)),
            [Keyword.True] = tokens
             => ParseToken(tokens, Keyword.True, t => new Expression.Literal.True(t)),
            [Valued.LiteralCharacter] = tokens
             => ParseTokenValue(tokens, Valued.LiteralCharacter, (t, val) => new Expression.Literal.Character(t, val)),
            [Valued.LiteralInteger] = tokens
             => ParseTokenValue(tokens, Valued.LiteralInteger, (t, val) => new Expression.Literal.Integer(t, val)),
            [Valued.LiteralReal] = tokens
             => ParseTokenValue(tokens, Valued.LiteralReal, (t, val) => new Expression.Literal.Real(t, val)),
            [Valued.LiteralString] = tokens
             => ParseTokenValue(tokens, Valued.LiteralString, (t, val) => new Expression.Literal.String(t, val)),
        };
    }

    private ParseResult<Type> ParseType(IEnumerable<Token> tokens)
     => ParseAnyOf<Type>(ParseTypeComplete, ParseTypeAliasReference, ParseTypeString)(tokens);

    // Parsing starts here with the "Algorithm" production rule
    public ParseResult<Algorithm> Parse()
    {
        var algorithm = ParseOperation.Start(_messenger, _tokens)
        .ParseToken(Keyword.Program)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Keyword.Is)
        .ParseZeroOrMoreUntilToken(out var declarations, tokens => ParseByTokenType(tokens, _declarationParsers), Special.Eof)
        .MapResult(t => new Algorithm(t, name, declarations));

        if (!algorithm.HasValue) {
            _messenger.Report(Message.ErrorSyntax(algorithm.SourceTokens, algorithm.Error));
        }

        return algorithm;
    }

    #region Declarations

    private ParseResult<Declaration.TypeAlias> ParseAliasDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.TypeAlias)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Operator.TypeAssignment)
        .Parse(out var type, ParseType)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Declaration.TypeAlias(t, name, type));

    private ParseResult<Declaration.Constant> ParseConstant(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Constant)
        .Parse(out var type, ParseType)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Operator.Assignment)
        .Parse(out var value, ParseExpression)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Declaration.Constant(t, type, name, value));

    private ParseResult<Declaration.MainProgram> ParseMainProgram(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.End)
        .ParseToken(Keyword.End)
    .MapResult(t => new Declaration.MainProgram(t, block));

    private ParseResult<Declaration> ParseProcedureDeclarationOrDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var signature, ParseProcedureSignature)
        .ChooseBranch<Declaration>(out var branch, new() {
            [Punctuation.Semicolon] = _ => t => new Declaration.Procedure(t, signature),
            [Keyword.Is] = o => {
                o.ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.End)
                .ParseToken(Keyword.End);
                return t => new Declaration.ProcedureDefinition(t, signature, block);
            },
        })
        .Fork(out var result, branch)
    .MapResult(result);

    private ParseResult<Declaration> ParseFunctionDeclarationOrDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var signature, ParseFunctionSignature)
        .ChooseBranch<Declaration>(out var branch, new() {
            [Punctuation.Semicolon] = _ => t => new Declaration.Function(t, signature),
            [Keyword.Is] = o => {
                o.ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.End)
                .ParseToken(Keyword.End);
                return t => new Declaration.FunctionDefinition(t, signature, block);
            },
        })
        .Fork(out var result, branch)
    .MapResult(result);

    private ParseResult<FunctionSignature> ParseFunctionSignature(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Function)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseParameterFormal, Punctuation.Comma)
        .ParseToken(Punctuation.CloseBracket)
        .ParseToken(Keyword.Delivers)
        .Parse(out var returnType, ParseType)
    .MapResult(t => new FunctionSignature(t, name, parameters, returnType));

    private ParseResult<ProcedureSignature> ParseProcedureSignature(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Procedure)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseParameterFormal, Punctuation.Comma)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new ProcedureSignature(t, name, parameters));

    #endregion Declarations

    #region Statements

    private ParseResult<Statement> ParseStatement(IEnumerable<Token> tokens)
     => ParseByTokenType(tokens, _statementParsers, ParseBuiltin);

    private ParseResult<Statement.Nop> ParseNop(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Statement.Nop(t));

    private ParseResult<Statement.Alternative> ParseAlternative(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var ifClause, tokens => ParseOperation.Start(_messenger, tokens)
            .ParseToken(Keyword.If)
            .ParseToken(Punctuation.OpenBracket)
            .Parse(out var condition, ParseExpression)
            .ParseToken(Punctuation.CloseBracket)
            .ParseToken(Keyword.Then)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                Keyword.EndIf, Keyword.Else, Keyword.ElseIf)
        .MapResult(t => new Statement.Alternative.IfClause(t, condition, block)))

        .ParseZeroOrMoreUntilToken(out var elseIfClauses, tokens => ParseOperation.Start(_messenger, tokens)
            .ParseToken(Keyword.ElseIf)
            .ParseToken(Punctuation.OpenBracket)
            .Parse(out var condition, ParseExpression)
            .ParseToken(Punctuation.CloseBracket)
            .ParseToken(Keyword.Then)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                Keyword.EndIf, Keyword.Else)
        .MapResult(t => new Statement.Alternative.ElseIfClause(t, condition, block)),
            Keyword.EndIf, Keyword.Else)

        .ParseOptional(out var elseClause, tokens => ParseOperation.Start(_messenger, tokens)
            .ParseToken(Keyword.Else)
            .ParseZeroOrMoreUntilToken(out var elseBlock, ParseStatement, Keyword.EndIf)
        .MapResult(t => new Statement.Alternative.ElseClause(t, elseBlock)))

        .ParseToken(Keyword.EndIf)
    .MapResult(t => new Statement.Alternative(t, ifClause, elseIfClauses, elseClause));

    private ParseResult<Statement.LocalVariable> ParseVariableDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseOneOrMoreSeparated(out var names, ParseIdentifier, Punctuation.Comma)
        .ParseToken(Punctuation.Colon)
        .Parse(out var type, ParseTypeComplete)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Statement.LocalVariable(t, names, type));

    private ParseResult<Statement.Assignment> ParseAssignment(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var target, ParseLvalue)
        .ParseToken(Operator.Assignment)
        .Parse(out var value, ParseExpression)
    .MapResult(t => new Statement.Assignment(t, target, value));

    private ParseResult<Statement> ParserVariableDeclarationOrProcedureCall(IEnumerable<Token> tokens)
    {
        ResultCreator<Statement.LocalVariable> FinishVariableDeclaration(ParseOperation o, IEnumerable<Identifier> names)
        {
            o.Parse(out var type, ParseTypeComplete);
            return t => new Statement.LocalVariable(t, names.ToList(), type);
        }

        return ParseOperation.Start(_messenger, tokens)
        .Parse(out var ident, ParseIdentifier)
        .ChooseBranch<Statement>(out var branch, new() {
            [Punctuation.Comma] = o => FinishVariableDeclaration(o
                .ParseZeroOrMoreSeparated(out var names, ParseIdentifier, Punctuation.Comma)
                .ParseToken(Punctuation.Colon), names.Prepend(ident)),
            [Punctuation.Colon] = o => FinishVariableDeclaration(o, ident.Yield()),
            [Punctuation.OpenBracket] = o => {
                o.ParseZeroOrMoreSeparated(out var parameters, ParseParameterActual, Punctuation.Comma)
                .ParseToken(Punctuation.CloseBracket);
                return t => new Statement.ProcedureCall(t, ident, parameters);
            },
        })
        .Fork(out var result, branch)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(result);
    }

    private ParseResult<Statement.WhileLoop> ParseWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.While)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.EndDo)
        .ParseToken(Keyword.EndDo)
    .MapResult(t => new Statement.WhileLoop(t, condition, block));

    private ParseResult<Statement.DoWhileLoop> ParseDoWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.While)
        .ParseToken(Keyword.While)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Statement.DoWhileLoop(t, condition, block));

    private ParseResult<Statement.RepeatLoop> ParseRepeatLoop(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Repeat)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.Until)
        .ParseToken(Keyword.Until)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Statement.RepeatLoop(t, condition, block));

    private ParseResult<Statement.ForLoop> ParseForLoop(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.For)
        .Parse(out var variant, ParseLvalue)
        .ParseToken(Keyword.From)
        .Parse(out var start, ParseExpression)
        .ParseToken(Keyword.To)
        .Parse(out var end, ParseExpression)
        .ParseOptional(out var step, tokens => ParseOperation.Start(_messenger, tokens)
            .ParseToken(Keyword.Step)
            .Parse(out var step, ParseExpression)
        .MapResult(_ => step))
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.EndDo)
        .ParseToken(Keyword.EndDo)
    .MapResult(t => new Statement.ForLoop(t, variant, start, end, step, block));

    private ParseResult<Statement.Return> ParseReturn(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Return)
        .Parse(out var returnValue, ParseExpression)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Statement.Return(t, returnValue));

    private ParseResult<Statement.Switch> ParseSwitch(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Switch)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Keyword.Is)

        .ParseZeroOrMoreUntilToken(out var cases, tokens => ParseOperation.Start(_messenger, tokens)
            .ParseToken(Keyword.When)
            .Parse(out var when, ParseExpression)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                Keyword.When, Keyword.WhenOther)
        .MapResult(t => new Statement.Switch.Case(t, when, block)),
            Keyword.WhenOther, Keyword.EndSwitch)

        .ParseOptional(out var @default, tokens => ParseOperation.Start(_messenger, tokens)
            .ParseToken(Keyword.WhenOther)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.EndSwitch)
        .MapResult(t => new Statement.Switch.DefaultCase(t, block)))

        .ParseToken(Keyword.EndSwitch)
    .MapResult(t => new Statement.Switch(t, expression, cases, @default));

    private ParseResult<Statement.Builtin> ParseBuiltin(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ChooseBranch<Statement.Builtin>(out var branch, new() {
            [Keyword.EcrireEcran] = o => {
                o.ParseZeroOrMoreSeparated(out var arguments, ParseExpression, Punctuation.Comma);
                return t => new Statement.Builtin.EcrireEcran(t, arguments);
            },
            [Keyword.LireClavier] = o => {
                o.Parse(out var argVariable, ParseLvalue);
                return t => new Statement.Builtin.LireClavier(t, argVariable);
            },
            [Keyword.Lire] = o => {
                o.Parse(out var argNomLog, ParseExpression)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argVariable, ParseLvalue);
                return t => new Statement.Builtin.Lire(t, argNomLog, argVariable);
            },
            [Keyword.Ecrire] = o => {
                o.Parse(out var argNomLog, ParseExpression)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argExpr, ParseExpression);
                return t => new Statement.Builtin.Ecrire(t, argNomLog, argExpr);
            },
            [Keyword.OuvrirAjout] = o => {
                o.Parse(out var argNomLog, ParseExpression);
                return t => new Statement.Builtin.OuvrirAjout(t, argNomLog);
            },
            [Keyword.OuvrirEcriture] = o => {
                o.Parse(out var argNomLog, ParseExpression);
                return t => new Statement.Builtin.OuvrirEcriture(t, argNomLog);
            },
            [Keyword.OuvrirLecture] = o => {
                o.Parse(out var argNomLog, ParseExpression);
                return t => new Statement.Builtin.OuvrirLecture(t, argNomLog);
            },
            [Keyword.Fermer] = o => {
                o.Parse(out var argNomLog, ParseExpression);
                return t => new Statement.Builtin.Fermer(t, argNomLog);
            },
            [Keyword.Assigner] = o => {
                o.Parse(out var argNomLog, ParseLvalue)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argNomExt, ParseExpression);
                return t => new Statement.Builtin.Assigner(t, argNomLog, argNomExt);
            }
        })
        .ParseToken(Punctuation.OpenBracket)
        .Fork(out var result, branch)
        .ParseToken(Punctuation.CloseBracket)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(result);

    #endregion Statements

    #region Types

    private ParseResult<Type.AliasReference> ParseTypeAliasReference(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var name, ParseIdentifier)
    .MapResult(t => new Type.AliasReference(t, name));

    private ParseResult<Type.String> ParseTypeString(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.String)
    .MapResult(t => new Type.String(t));

    private ParseResult<Type.Complete> ParseTypeComplete(IEnumerable<Token> tokens) => ParseByTokenType(tokens, _completeTypeParsers);

    private ParseResult<Type.Complete.Array> ParseTypeArray(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Array)
        .Parse(out var dimensions, ParseIndexes)
        .ParseToken(Keyword.From)
        .Parse(out var type, ParseTypeComplete)
    .MapResult(t => new Type.Complete.Array(t, type, dimensions));

    private ParseResult<Type.Complete.StringLengthed> ParseTypeStringLengthed(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.String)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var length, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Type.Complete.StringLengthed(t, length));

    private ParseResult<Type.Complete.Structure> ParseTypeStructure(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(Keyword.Structure)
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var components, ParseVariableDeclaration, Keyword.End)
        .ParseToken(Keyword.End)
    .MapResult(t => new Type.Complete.Structure(t, components));

    #endregion Types

    #region Other

    private ParseResult<ParameterFormal> ParseParameterFormal(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var mode, tokens => GetByTokenType(tokens, parameterFormalModes))
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.Colon)
        .Parse(out var type, ParseType)
    .MapResult(t => new ParameterFormal(t, mode, name, type));

    private ParseResult<ParameterActual> ParseParameterActual(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .Parse(out var mode, tokens => GetByTokenType(tokens, parameterActualModes))
        .Parse(out var value, ParseExpression)
    .MapResult(t => new ParameterActual(t, mode, value));

    #endregion Other

    #region Terminals

    private ParseResult<Identifier> ParseIdentifier(IEnumerable<Token> tokens) => ParseOperation.Start(_messenger, tokens)
        .ParseTokenValue(out var name, Valued.Identifier)
    .MapResult(t => new Identifier(t, name));

    private static readonly IReadOnlyDictionary<TokenType, ParameterMode> parameterFormalModes = new Dictionary<TokenType, ParameterMode> {
        [Keyword.EntF] = ParameterMode.In,
        [Keyword.SortF] = ParameterMode.Out,
        [Keyword.EntSortF] = ParameterMode.InOut,
    };
    private static readonly IReadOnlyDictionary<TokenType, ParameterMode> parameterActualModes = new Dictionary<TokenType, ParameterMode> {
        [Keyword.EntE] = ParameterMode.In,
        [Keyword.SortE] = ParameterMode.Out,
        [Keyword.EntSortE] = ParameterMode.InOut,
    };

    #endregion Terminals
}
