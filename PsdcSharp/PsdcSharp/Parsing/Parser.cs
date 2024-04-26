using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;
using static Scover.Psdc.Tokenization.TokenType;

namespace Scover.Psdc.Parsing;

internal sealed partial class Parser : MessageProvider
{
    private readonly IEnumerable<Token> _tokens;

    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Declaration>> _declarationParsers;
    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Statement>> _statementParsers;
    private readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Type>> _completeTypeParsers;

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
            [Named.Identifier] = ParserVariableDeclarationOrAssignmentOrProcedureCall,
            [Keyword.Do] = ParseDoWhileLoop,
            [Keyword.EcrireEcran] = ParseEcrireEcran,
            [Keyword.For] = ParseForLoop,
            [Keyword.If] = ParseAlternative,
            [Keyword.LireClavier] = ParseLireClavier,
            [Keyword.Repeat] = ParseRepeatLoop,
            [Keyword.Return] = ParseReturn,
            [Keyword.Switch] = ParseSwitch,
            [Keyword.While] = ParseWhileLoop,
            [Symbol.Semicolon] = ParseNop,
        };

        _completeTypeParsers = new Dictionary<TokenType, ParseMethod<Node.Type>> {
            [Keyword.Integer] = MakePrimitiveTypeParser(PrimitiveType.Integer),
            [Keyword.Real] = MakePrimitiveTypeParser(PrimitiveType.Real),
            [Keyword.Character] = MakePrimitiveTypeParser(PrimitiveType.Character),
            [Keyword.Boolean] = MakePrimitiveTypeParser(PrimitiveType.Boolean),
            [Keyword.String] = ParseLengthedString,
            [Keyword.Array] = ParseArrayType,
            [Named.Identifier] = ParseAliasReference,
        };
        _literalParsers = new Dictionary<TokenType, ParseMethod<Node.Expression.Literal>> {
            [Keyword.False] = tokens
             => ParseToken(tokens, Keyword.False, t => new Node.Expression.Literal.False(t)),
            [Keyword.True] = tokens
             => ParseToken(tokens, Keyword.True, t => new Node.Expression.Literal.True(t)),
            [Named.LiteralCharacter] = tokens
             => ParseTokenValue(tokens, Named.LiteralCharacter, (t, value) => new Node.Expression.Literal.Character(t, value)),
            [Named.LiteralInteger] = tokens
             => ParseTokenValue(tokens, Named.LiteralInteger, (t, value) => new Node.Expression.Literal.Integer(t, value)),
            [Named.LiteralReal] = tokens
             => ParseTokenValue(tokens, Named.LiteralReal, (t, value) => new Node.Expression.Literal.Real(t, value)),
            [Named.LiteralString] = tokens
             => ParseTokenValue(tokens, Named.LiteralString, (t, value) => new Node.Expression.Literal.String(t, value)),
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

    private ParseResult<Node.Declaration.Alias> ParseAliasDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.TypeAlias)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Symbol.OperatorTypeAssignment)
        .Parse(out var type, tokens => ParseType(tokens)
                           .Else(() => ParseStructureDefinition(tokens)))
        .ParseToken(Symbol.Semicolon)
    .MapResult(t => new Node.Declaration.Alias(t, name, type));

    private ParseResult<Node.Declaration.Constant> ParseConstant(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Constant)
        .Parse(out var type, ParseType)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Symbol.OperatorAssignment)
        .Parse(out var value, ParseExpression)
        .ParseToken(Symbol.Semicolon)
    .MapResult(t => new Node.Declaration.Constant(t, type, name, value));

    private ParseResult<Node.Declaration.MainProgram> ParseMainProgram(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.End)
        .ParseToken(Keyword.End)
    .MapResult(t => new Node.Declaration.MainProgram(t, block));

    private ParseResult<Node.Declaration> ParseProcedureDeclarationOrDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseProcedureSignature)
        .Branch<Node.Declaration>(new() {
            [Symbol.Semicolon] = o => o
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
            [Symbol.Semicolon] = o => o
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
        .ParseToken(Symbol.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseFormalParameter, Symbol.Comma)
        .ParseToken(Symbol.CloseBracket)
        .ParseToken(Keyword.Delivers)
        .Parse(out var returnType, ParseType)
    .MapResult(t => new Node.FunctionSignature(t, name, parameters, returnType));

    private ParseResult<Node.ProcedureSignature> ParseProcedureSignature(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Procedure)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Symbol.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseFormalParameter, Symbol.Comma)
        .ParseToken(Symbol.CloseBracket)
    .MapResult(t => new Node.ProcedureSignature(t, name, parameters));

    #endregion Declarations

    #region Statements

    private ParseResult<Node.Statement> ParseStatement(IEnumerable<Token> tokens) => ParseByTokenType(tokens, _statementParsers);

    private ParseResult<Node.Statement.Nop> ParseNop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Symbol.Semicolon)
    .MapResult(t => new Node.Statement.Nop(t));

    private ParseResult<Node.Statement.Alternative> ParseAlternative(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var ifClause, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.If)
            .ParseToken(Symbol.OpenBracket)
            .Parse(out var condition, ParseExpression)
            .ParseToken(Symbol.CloseBracket)
            .ParseToken(Keyword.Then)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                Keyword.EndIf, Keyword.Else, Keyword.ElseIf)
        .MapResult(t => new Node.Statement.Alternative.IfClause(t, condition, block)))

        .ParseZeroOrMoreUntilToken(out var elseIfClauses, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.ElseIf)
            .ParseToken(Symbol.OpenBracket)
            .Parse(out var condition, ParseExpression)
            .ParseToken(Symbol.CloseBracket)
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

    private ParseResult<Node.Statement> ParseEcrireEcran(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.EcrireEcran)
        .ParseToken(Symbol.OpenBracket)
        .ParseZeroOrMoreSeparated(out var arguments, ParseExpression, Symbol.Comma)
        .ParseToken(Symbol.CloseBracket)
        .ParseToken(Symbol.Semicolon)
    .MapResult(t => new Node.Statement.EcrireEcran(t, arguments));

    private ParseResult<Node.Statement> ParseLireClavier(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.LireClavier)
        .ParseToken(Symbol.OpenBracket)
        .Parse(out var argument, ParseExpression)
        .ParseToken(Symbol.CloseBracket)
        .ParseToken(Symbol.Semicolon)
    .MapResult(t => new Node.Statement.LireClavier(t, argument));

    private ParseResult<Node.Statement.VariableDeclaration> ParseVariableDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseOneOrMoreSeparated(out var names, ParseIdentifier, Symbol.Comma)
        .ParseToken(Symbol.Colon)
        .Parse(out var type, ParseCompleteType)
        .ParseToken(Symbol.Semicolon)
    .MapResult(t => new Node.Statement.VariableDeclaration(t, names, type));

    private ParseResult<Node.Statement> ParserVariableDeclarationOrAssignmentOrProcedureCall(IEnumerable<Token> tokens)
    {
        ParseResult<Node.Statement.VariableDeclaration> FinishVariableDeclaration(ParseOperation o, IEnumerable<Node.Identifier> names) => o
            .Parse(out var type, ParseCompleteType)
            .MapResult(t => new Node.Statement.VariableDeclaration(t, names.ToList(), type));

        return ParseOperation.Start(this, tokens)
        .Parse(out var ident, ParseIdentifier)
        .Branch(out Node.Statement result, new() {
            [Symbol.OperatorAssignment] = o => o
                .Parse(out var value, ParseExpression)
            .MapResult(t => new Node.Statement.Assignment(t, ident, value)),
            [Symbol.Comma] = o => FinishVariableDeclaration(o
                .ParseZeroOrMoreSeparated(out var names, ParseIdentifier, Symbol.Comma)
                .ParseToken(Symbol.Colon), names.Prepend(ident)),
            [Symbol.Colon] = o => FinishVariableDeclaration(o, ident.Yield()),
            [Symbol.OpenBracket] = o => o
                .ParseZeroOrMoreSeparated(out var parameters, ParseEffectiveParameter, Symbol.Comma)
                .ParseToken(Symbol.CloseBracket)
            .MapResult(t => new Node.Statement.ProcedureCall(t, ident, parameters)),
        })
        .ParseToken(Symbol.Semicolon)
        .MapResult(_ => result);
    }

    private ParseResult<Node.Statement.WhileLoop> ParseWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.While)
        .ParseToken(Symbol.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Symbol.CloseBracket)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.EndDo)
        .ParseToken(Keyword.EndDo)
        .MapResult(t => new Node.Statement.WhileLoop(t, condition, block));

    private ParseResult<Node.Statement.DoWhileLoop> ParseDoWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.While)
        .ParseToken(Keyword.While)
        .ParseToken(Symbol.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Symbol.CloseBracket)
        .MapResult(t => new Node.Statement.DoWhileLoop(t, condition, block));

    private ParseResult<Node.Statement.RepeatLoop> ParseRepeatLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Repeat)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.Until)
        .ParseToken(Keyword.Until)
        .ParseToken(Symbol.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Symbol.CloseBracket)
    .MapResult(t => new Node.Statement.RepeatLoop(t, condition, block));

    private ParseResult<Node.Statement.ForLoop> ParseForLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.For)
        .Parse(out var variantName, ParseIdentifier)
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
    .MapResult(t => new Node.Statement.ForLoop(t, variantName, start, end, step, block));

    private ParseResult<Node.Statement.Return> ParseReturn(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Return)
        .Parse(out var returnValue, ParseExpression)
        .ParseToken(Symbol.Semicolon)
    .MapResult(t => new Node.Statement.Return(t, returnValue));

    private ParseResult<Node.Statement.Switch> ParseSwitch(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Switch)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Keyword.Is)

        .ParseZeroOrMoreUntilToken(out var cases, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.When)
            .Parse(out var when, ParseExpression)
            .ParseToken(Symbol.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                Keyword.When, Keyword.WhenOther)
        .MapResult(t => new Node.Statement.Switch.Case(t, when, block)),
            Keyword.WhenOther, Keyword.EndSwitch)

        .ParseOptional(out var @default, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.WhenOther)
            .ParseToken(Symbol.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement, Keyword.EndSwitch)
        .MapResult(t => new Node.Statement.Switch.CaseDefault(t, block)))
        
        .ParseToken(Keyword.EndSwitch)
    .MapResult(t => new Node.Statement.Switch(t, expression, cases, @default));

    #endregion Statements

    #region Types

    private ParseResult<Node.Type> ParseCompleteType(IEnumerable<Token> tokens) => ParseByTokenType(tokens, _completeTypeParsers);

    private ParseResult<Node.Type> ParseType(IEnumerable<Token> tokens)
     => ParseCompleteType(tokens).Else(()
         => ParseOperation.Start(this, tokens)
            .ParseToken(Keyword.String)
        .MapResult(t => new Node.Type.String(t)));

    private ParseResult<Node.Type.AliasReference> ParseAliasReference(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var name, ParseIdentifier)
    .MapResult(t => new Node.Type.AliasReference(t, name));

    private ParseResult<Node.Type.Array> ParseArrayType(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Array)
        .Parse(out var dimensions, ParseIndexes)
        .ParseToken(Keyword.From)
        .Parse(out var type, ParseCompleteType)
    .MapResult(t => new Node.Type.Array(t, type, dimensions));

    private ParseResult<Node.Type.StringLengthed> ParseLengthedString(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.String)
        .ParseToken(Symbol.OpenBracket)
        .Parse(out var length, ParseExpression)
        .ParseToken(Symbol.CloseBracket)
    .MapResult(t => new Node.Type.StringLengthed(t, length));

    private ParseResult<Node.Type.StructureDefinition> ParseStructureDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(Keyword.Structure)
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var components, ParseVariableDeclaration, Keyword.End)
        .ParseToken(Keyword.End)
    .MapResult(t => new Node.Type.StructureDefinition(t, components));

    private static ParseMethod<Node.Type.Primitive> MakePrimitiveTypeParser(PrimitiveType type)
     => tokens => ParseResult.Ok(new Node.Type.Primitive(new(tokens, 1), type));

    #endregion Types

    #region Other

    private ParseResult<Node.FormalParameter> ParseFormalParameter(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var mode, tokens => GetByTokenType(tokens, formalParameterModes))
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Symbol.Colon)
        .Parse(out var type, ParseType)
    .MapResult(t => new Node.FormalParameter(t, mode, name, type));

    private ParseResult<Node.EffectiveParameter> ParseEffectiveParameter(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var mode, tokens => GetByTokenType(tokens, effectiveParameterModes))
        .Parse(out var value, ParseExpression)
    .MapResult(t => new Node.EffectiveParameter(t, mode, value));

    private ParseResult<Node.Identifier> ParseIdentifier(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var name, Named.Identifier)
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
