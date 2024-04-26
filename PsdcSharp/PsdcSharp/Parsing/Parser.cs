using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;
using static Scover.Psdc.Tokenization.TokenType;

namespace Scover.Psdc.Parsing;

internal sealed partial class Parser : MessageProvider
{
    private readonly IEnumerable<Token> _tokens;

    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Declaration>> _declarationParsers;
    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Statement>> _statementParsers;
    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Type.Complete>> _completeTypeParsers;

    public Parser(IEnumerable<Token> tokens)
    {
        _tokens = tokens;

        _declarationParsers = new Dictionary<TokenType, ParseMethod<Node.Declaration>>() {
            [Keyword.Begin] = ParseMainProgram,
            [Keyword.Constant] = ParseConstant,
            [Keyword.Function] = ParseFunctionDeclarationOrDefinition,
            [Keyword.Procedure] = ParseProcedureDeclarationOrDefinition,
            [Keyword.TypeAlias] = ParseAliasDeclaration,
        };

        _statementParsers = new Dictionary<TokenType, ParseMethod<Node.Statement>>() {
            [Valued.Identifier] = t => ParseAnyOf(t, ParserVariableDeclarationOrProcedureCall, ParseAssignment),
            [Keyword.Do] = ParseDoWhileLoop,
            [Keyword.EcrireEcran] = ParseBuiltinEcrireEcran,
            [Keyword.For] = ParseForLoop,
            [Keyword.If] = ParseAlternative,
            [Keyword.LireClavier] = ParseBuiltinLireClavier,
            [Keyword.Repeat] = ParseRepeatLoop,
            [Keyword.Return] = ParseReturn,
            [Keyword.Switch] = ParseSwitch,
            [Keyword.While] = ParseWhileLoop,
            [Punctuation.Semicolon] = ParseNop,
        };

        _completeTypeParsers = new Dictionary<TokenType, ParseMethod<Node.Type.Complete>> {
            [Keyword.Integer] = MakePrimitiveTypeParser(PrimitiveType.Integer),
            [Keyword.Real] = MakePrimitiveTypeParser(PrimitiveType.Real),
            [Keyword.Character] = MakePrimitiveTypeParser(PrimitiveType.Character),
            [Keyword.Boolean] = MakePrimitiveTypeParser(PrimitiveType.Boolean),
            [Keyword.String] = ParseTypeLengthedString,
            [Keyword.Array] = ParseTypeArray,
            [Valued.Identifier] = MakeAlwaysOkParser((t, val) => new Node.Type.Complete.AliasReference(t, new(t, val))),
            [Keyword.Structure] = ParseTypeStructure,
        };

        ParseMethod<Node.Type.Complete.Primitive> MakePrimitiveTypeParser(PrimitiveType type)
         => MakeAlwaysOkParser(1, t => new Node.Type.Complete.Primitive(t, type));

        _literalParsers = new Dictionary<TokenType, ParseMethod<Node.Expression.Literal>> {
            [Keyword.False] = tokens
             => ParseToken(tokens, Keyword.False, t => new Node.Expression.Literal.False(t)),
            [Keyword.True] = tokens
             => ParseToken(tokens, Keyword.True, t => new Node.Expression.Literal.True(t)),
            [Valued.LiteralCharacter] = tokens
             => ParseTokenValue(tokens, Valued.LiteralCharacter, (t, value) => new Node.Expression.Literal.Character(t, value)),
            [Valued.LiteralInteger] = tokens
             => ParseTokenValue(tokens, Valued.LiteralInteger, (t, value) => new Node.Expression.Literal.Integer(t, value)),
            [Valued.LiteralReal] = tokens
             => ParseTokenValue(tokens, Valued.LiteralReal, (t, value) => new Node.Expression.Literal.Real(t, value)),
            [Valued.LiteralString] = tokens
             => ParseTokenValue(tokens, Valued.LiteralString, (t, value) => new Node.Expression.Literal.String(t, value)),
        };
    }

    // Parsing starts here with the "Algorithm" production rule
    public ParseResult<Node.Algorithm> Parse()
    {
        var algorithm = ParseOperation.Start(this, _tokens)
        .ParseToken(Keyword.Program)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Keyword.Is)
        .ParseZeroOrMoreUntilToken(out var declarations, tokens => ParseByTokenType(tokens, _declarationParsers), Special.Eof)
    .MapResult(t => new Node.Algorithm(t, name, declarations));

        if (!algorithm.HasValue) {
            AddMessage(Message.ErrorSyntax(algorithm.SourceTokens, algorithm.Error));
        }

        return algorithm;
    }

    #region Declarations

    private ParseResult<Node.Declaration.TypeAlias> ParseAliasDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.TypeAlias)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Operator.TypeAssignment)
        .Parse(out var type, ParseType)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Node.Declaration.TypeAlias(t, name, type));

    private ParseResult<Node.Declaration.Constant> ParseConstant(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Constant)
        .Parse(out var type, ParseType)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Operator.Assignment)
        .Parse(out var value, ParseExpression)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Node.Declaration.Constant(t, type, name, value));

    private ParseResult<Node.Declaration.MainProgram> ParseMainProgram(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.End)
        .ParseToken(Keyword.End)
    .MapResult(t => new Node.Declaration.MainProgram(t, block));

    private ParseResult<Node.Declaration> ParseProcedureDeclarationOrDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseProcedureSignature)
        .Branch<Node.Declaration>(new() {
            [Punctuation.Semicolon] = o => o
                .MapResult(t => new Node.Declaration.Procedure(t, signature)),
            [Keyword.Is] = o => o
                .ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.End)
                .ParseToken(Keyword.End)
            .MapResult(t => new Node.Declaration.ProcedureDefinition(t, signature, block)),
        });

    private ParseResult<Node.Declaration> ParseFunctionDeclarationOrDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseFunctionSignature)
        .Branch<Node.Declaration>(new() {
            [Punctuation.Semicolon] = o => o
                .MapResult(t => new Node.Declaration.Function(t, signature)),
            [Keyword.Is] = o => o
                .ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.End)
                .ParseToken(Keyword.End)
            .MapResult(t => new Node.Declaration.FunctionDefinition(t, signature, block)),
        });

    private ParseResult<Node.FunctionSignature> ParseFunctionSignature(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Function)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseFormalParameter, Punctuation.Comma)
        .ParseToken(Punctuation.CloseBracket)
        .ParseToken(Keyword.Delivers)
        .Parse(out var returnType, ParseType)
    .MapResult(t => new Node.FunctionSignature(t, name, parameters, returnType));

    private ParseResult<Node.ProcedureSignature> ParseProcedureSignature(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Procedure)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseFormalParameter, Punctuation.Comma)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Node.ProcedureSignature(t, name, parameters));

    #endregion Declarations

    #region Statements

    private ParseResult<Node.Statement> ParseStatement(IEnumerable<Token> tokens) => ParseByTokenType(tokens, _statementParsers);

    private ParseResult<Node.Statement.Nop> ParseNop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Node.Statement.Nop(t));

    private ParseResult<Node.Statement.Alternative> ParseAlternative(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var ifClause, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.If)
            .ParseToken(Punctuation.OpenBracket)
            .Parse(out var condition, ParseExpression)
            .ParseToken(Punctuation.CloseBracket)
            .ParseToken(Keyword.Then)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                Keyword.EndIf, Keyword.Else, Keyword.ElseIf)
        .MapResult(t => new Node.Statement.Alternative.IfClause(t, condition, block)))

        .ParseZeroOrMoreUntilToken(out var elseIfClauses, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.ElseIf)
            .ParseToken(Punctuation.OpenBracket)
            .Parse(out var condition, ParseExpression)
            .ParseToken(Punctuation.CloseBracket)
            .ParseToken(Keyword.Then)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                Keyword.EndIf, Keyword.Else)
        .MapResult(t => new Node.Statement.Alternative.ElseIfClause(t, condition, block)),
            Keyword.EndIf, Keyword.Else)

        .ParseOptional(out var elseClause, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.Else)
            .ParseZeroOrMoreUntilToken(out var elseBlock, ParseStatement, Keyword.EndIf)
        .MapResult(t => new Node.Statement.Alternative.ElseClause(t, elseBlock)))

        .ParseToken(Keyword.EndIf)
    .MapResult(t => new Node.Statement.Alternative(t, ifClause, elseIfClauses, elseClause));

    private ParseResult<Node.Statement> ParseBuiltinEcrireEcran(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.EcrireEcran)
        .ParseToken(Punctuation.OpenBracket)
        .ParseZeroOrMoreSeparated(out var arguments, ParseExpression, Punctuation.Comma)
        .ParseToken(Punctuation.CloseBracket)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Node.Statement.BuiltinEcrireEcran(t, arguments));

    private ParseResult<Node.Statement> ParseBuiltinLireClavier(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.LireClavier)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var argVariable, ParseLValue)
        .ParseToken(Punctuation.CloseBracket)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Node.Statement.BuiltinLireClavier(t, argVariable));

    private ParseResult<Node.Statement.LocalVariable> ParseVariableDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseOneOrMoreSeparated(out var names, ParseIdentifier, Punctuation.Comma)
        .ParseToken(Punctuation.Colon)
        .Parse(out var type, ParseTypeComplete)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Node.Statement.LocalVariable(t, names, type));

    private ParseResult<Node.Statement.Assignment> ParseAssignment(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var target, ParseLValue)
        .ParseToken(Operator.Assignment)
        .Parse(out var value, ParseExpression)
    .MapResult(t => new Node.Statement.Assignment(t, target, value));

    private ParseResult<Node.Statement> ParserVariableDeclarationOrProcedureCall(IEnumerable<Token> tokens)
    {
        ParseResult<Node.Statement.LocalVariable> FinishVariableDeclaration(ParseOperation o, IEnumerable<Node.Identifier> names) => o
            .Parse(out var type, ParseTypeComplete)
            .MapResult(t => new Node.Statement.LocalVariable(t, names.ToList(), type));

        return ParseOperation.Start(this, tokens)
        .Parse(out var ident, ParseIdentifier)
        .Branch(out Node.Statement result, new() {
            [Punctuation.Comma] = o => FinishVariableDeclaration(o
                .ParseZeroOrMoreSeparated(out var names, ParseIdentifier, Punctuation.Comma)
                .ParseToken(Punctuation.Colon), names.Prepend(ident)),
            [Punctuation.Colon] = o => FinishVariableDeclaration(o, ident.Yield()),
            [Punctuation.OpenBracket] = o => o
                .ParseZeroOrMoreSeparated(out var parameters, ParseEffectiveParameter, Punctuation.Comma)
                .ParseToken(Punctuation.CloseBracket)
            .MapResult(t => new Node.Statement.ProcedureCall(t, ident, parameters)),
        })
        .ParseToken(Punctuation.Semicolon)
        .MapResult(_ => result);
    }

    private ParseResult<Node.Statement.WhileLoop> ParseWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.While)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.EndDo)
        .ParseToken(Keyword.EndDo)
        .MapResult(t => new Node.Statement.WhileLoop(t, condition, block));

    private ParseResult<Node.Statement.DoWhileLoop> ParseDoWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.While)
        .ParseToken(Keyword.While)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
        .MapResult(t => new Node.Statement.DoWhileLoop(t, condition, block));

    private ParseResult<Node.Statement.RepeatLoop> ParseRepeatLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Repeat)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.Until)
        .ParseToken(Keyword.Until)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Node.Statement.RepeatLoop(t, condition, block));

    private ParseResult<Node.Statement.ForLoop> ParseForLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.For)
        .Parse(out var variant, ParseLValue)
        .ParseToken(Keyword.From)
        .Parse(out var start, ParseExpression)
        .ParseToken(Keyword.To)
        .Parse(out var end, ParseExpression)
        .ParseOptional(out var step, tokens => ParseOperation.Start(this, tokens)
                .ParseToken(Keyword.Step)
                .Parse(out var step, ParseExpression)
                .MapResult(_ => step))
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.EndDo)
        .ParseToken(Keyword.EndDo)
    .MapResult(t => new Node.Statement.ForLoop(t, variant, start, end, step, block));

    private ParseResult<Node.Statement.Return> ParseReturn(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Return)
        .Parse(out var returnValue, ParseExpression)
        .ParseToken(Punctuation.Semicolon)
    .MapResult(t => new Node.Statement.Return(t, returnValue));

    private ParseResult<Node.Statement.Switch> ParseSwitch(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Switch)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Keyword.Is)

        .ParseZeroOrMoreUntilToken(out var cases, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.When)
            .Parse(out var when, ParseExpression)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                Keyword.When, Keyword.WhenOther)
        .MapResult(t => new Node.Statement.Switch.Case(t, when, block)),
            Keyword.WhenOther, Keyword.EndSwitch)

        .ParseOptional(out var @default, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.WhenOther)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.EndSwitch)
        .MapResult(t => new Node.Statement.Switch.CaseDefault(t, block)))

        .ParseToken(Keyword.EndSwitch)
    .MapResult(t => new Node.Statement.Switch(t, expression, cases, @default));

    #endregion Statements

    #region Types

    private ParseResult<Node.Type> ParseType(IEnumerable<Token> tokens)
     => ParseAnyOf<Node.Type>(tokens, ParseTypeComplete, ParseTypeAliasReference, ParseTypeString);

    private ParseResult<Node.Type.AliasReference> ParseTypeAliasReference(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var name, ParseIdentifier)
    .MapResult(t => new Node.Type.AliasReference(t, name));

    private ParseResult<Node.Type.String> ParseTypeString(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.String)
    .MapResult(t => new Node.Type.String(t));

    private ParseResult<Node.Type.Complete> ParseTypeComplete(IEnumerable<Token> tokens) => ParseByTokenType(tokens, _completeTypeParsers);

    private ParseResult<Node.Type.Complete.Array> ParseTypeArray(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Array)
        .Parse(out var dimensions, ParseIndexes)
        .ParseToken(Keyword.From)
        .Parse(out var type, ParseTypeComplete)
    .MapResult(t => new Node.Type.Complete.Array(t, type, dimensions));

    private ParseResult<Node.Type.Complete.LengthedString> ParseTypeLengthedString(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.String)
        .ParseToken(Punctuation.OpenBracket)
        .Parse(out var length, ParseExpression)
        .ParseToken(Punctuation.CloseBracket)
    .MapResult(t => new Node.Type.Complete.LengthedString(t, length));

    private ParseResult<Node.Type.Complete.Structure> ParseTypeStructure(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Structure)
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var components, ParseVariableDeclaration, Keyword.End)
        .ParseToken(Keyword.End)
    .MapResult(t => new Node.Type.Complete.Structure(t, components));

    #endregion Types

    #region Other

    private ParseResult<Node.FormalParameter> ParseFormalParameter(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var mode, tokens => GetByTokenType(tokens, formalParameterModes))
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.Colon)
        .Parse(out var type, ParseType)
    .MapResult(t => new Node.FormalParameter(t, mode, name, type));

    private ParseResult<Node.EffectiveParameter> ParseEffectiveParameter(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var mode, tokens => GetByTokenType(tokens, effectiveParameterModes))
        .Parse(out var value, ParseExpression)
    .MapResult(t => new Node.EffectiveParameter(t, mode, value));

    private ParseResult<Node.Identifier> ParseIdentifier(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var name, Valued.Identifier)
    .MapResult(t => new Node.Identifier(t, name));

    #endregion Other

    #region Terminals

    private static readonly IReadOnlyDictionary<TokenType, ParameterMode> formalParameterModes = new Dictionary<TokenType, ParameterMode> {
        [Keyword.EntF] = ParameterMode.In,
        [Keyword.SortF] = ParameterMode.Out,
        [Keyword.EntSortF] = ParameterMode.InOut,
    };
    private static readonly IReadOnlyDictionary<TokenType, ParameterMode> effectiveParameterModes = new Dictionary<TokenType, ParameterMode> {
        [Keyword.EntE] = ParameterMode.In,
        [Keyword.SortE] = ParameterMode.Out,
        [Keyword.EntSortE] = ParameterMode.InOut,
    };

    #endregion Terminals
}
