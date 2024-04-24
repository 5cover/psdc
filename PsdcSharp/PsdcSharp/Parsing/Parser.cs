using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

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
            [TokenType.KeywordBegin] = ParseMainProgram,
            [TokenType.KeywordConstant] = ParseConstant,
            [TokenType.KeywordFunction] = ParseFunctionDeclarationOrDefinition,
            [TokenType.KeywordProcedure] = ParseProcedureDeclarationOrDefinition,
            [TokenType.KeywordTypeAlias] = ParseAliasDeclaration,
        };

        _statementParsers = new Dictionary<TokenType, ParseMethod<Node.Statement>>() {
            [TokenType.Identifier] = ParserVariableDeclarationOrAssignmentOrProcedureCall,
            [TokenType.KeywordDo] = ParseDoWhileLoop,
            [TokenType.KeywordEcrireEcran] = ParseEcrireEcran,
            [TokenType.KeywordFor] = ParseForLoop,
            [TokenType.KeywordIf] = ParseAlternative,
            [TokenType.KeywordLireClavier] = ParseLireClavier,
            [TokenType.KeywordRepeat] = ParseRepeatLoop,
            [TokenType.KeywordReturn] = ParseReturn,
            [TokenType.KeywordSwitch] = ParseSwitch,
            [TokenType.KeywordWhile] = ParseWhileLoop,
        };

        _completeTypeParsers = new Dictionary<TokenType, ParseMethod<Node.Type>> {
            [TokenType.KeywordInteger] = MakePrimitiveTypeParser(PrimitiveType.Integer),
            [TokenType.KeywordReal] = MakePrimitiveTypeParser(PrimitiveType.Real),
            [TokenType.KeywordCharacter] = MakePrimitiveTypeParser(PrimitiveType.Character),
            [TokenType.KeywordBoolean] = MakePrimitiveTypeParser(PrimitiveType.Boolean),
            [TokenType.KeywordString] = ParseLengthedString,
            [TokenType.KeywordArray] = ParseArrayType,
            [TokenType.Identifier] = ParseAliasReference,
        };
        _literalParsers = new Dictionary<TokenType, ParseMethod<Node.Expression.Literal>> {
            [TokenType.KeywordFalse] = tokens
             => ParseToken(tokens, TokenType.KeywordFalse, t => new Node.Expression.Literal.False(t)),
            [TokenType.KeywordTrue] = tokens
             => ParseToken(tokens, TokenType.KeywordTrue, t => new Node.Expression.Literal.True(t)),
            [TokenType.LiteralCharacter] = tokens
             => ParseTokenValue(tokens, TokenType.LiteralCharacter, (t, value) => new Node.Expression.Literal.Character(t, value)),
            [TokenType.LiteralInteger] = tokens
             => ParseTokenValue(tokens, TokenType.LiteralInteger, (t, value) => new Node.Expression.Literal.Integer(t, value)),
            [TokenType.LiteralReal] = tokens
             => ParseTokenValue(tokens, TokenType.LiteralReal, (t, value) => new Node.Expression.Literal.Real(t, value)),
            [TokenType.LiteralString] = tokens
             => ParseTokenValue(tokens, TokenType.LiteralString, (t, value) => new Node.Expression.Literal.String(t, value)),
        };
    }

    // Parsing starts here with the "Algorithm" production rule
    public ParseResult<Node.Algorithm> Parse()
    {
        var algorithm = ParseOperation.Start(this, _tokens)
        .ParseToken(TokenType.KeywordProgram)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.KeywordIs)
        .ParseZeroOrMoreUntilToken(out var declarations, tokens => ParseByTokenType(tokens, _declarationParsers), TokenType.Eof)
    .MapResult(t => new Node.Algorithm(t, name, declarations));

        if (!algorithm.HasValue) {
            AddMessage(Message.ErrorSyntax<Node.Algorithm>(algorithm.SourceTokens, algorithm.Error));
        }

        return algorithm;
    }

    #region Declarations

    private ParseResult<Node.Declaration.Alias> ParseAliasDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordTypeAlias)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.OperatorTypeAssignment)
        .Parse(out var type, tokens => ParseType(tokens)
                           .Else(() => ParseStructureDefinition(tokens)))
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(t => new Node.Declaration.Alias(t, name, type));

    private ParseResult<Node.Declaration.Constant> ParseConstant(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordConstant)
        .Parse(out var type, ParseType)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.OperatorAssignment)
        .Parse(out var value, ParseExpression)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(t => new Node.Declaration.Constant(t, type, name, value));

    private ParseResult<Node.Declaration.MainProgram> ParseMainProgram(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEnd)
        .ParseToken(TokenType.KeywordEnd)
    .MapResult(t => new Node.Declaration.MainProgram(t, block));

    private ParseResult<Node.Declaration> ParseProcedureDeclarationOrDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseProcedureSignature)
        .Branch<Node.Declaration>(new() {
            [TokenType.PunctuationSemicolon] = o => o
                .MapResult(t => new Node.Declaration.Procedure(t, signature)),
            [TokenType.KeywordIs] = o => o
                .ParseToken(TokenType.KeywordBegin)
                .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEnd)
                .ParseToken(TokenType.KeywordEnd)
            .MapResult(t => new Node.Declaration.ProcedureDefinition(t, signature, block)),
        });

    private ParseResult<Node.Declaration> ParseFunctionDeclarationOrDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseFunctionSignature)
        .Branch<Node.Declaration>(new() {
            [TokenType.PunctuationSemicolon] = o => o
                .MapResult(t => new Node.Declaration.Function(t, signature)),
            [TokenType.KeywordIs] = o => o
                .ParseToken(TokenType.KeywordBegin)
                .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEnd)
                .ParseToken(TokenType.KeywordEnd)
            .MapResult(t => new Node.Declaration.FunctionDefinition(t, signature, block)),
        });

    private ParseResult<Node.FunctionSignature> ParseFunctionSignature(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordFunction)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseFormalParameter, TokenType.PunctuationComma)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.KeywordDelivers)
        .Parse(out var returnType, ParseType)
    .MapResult(t => new Node.FunctionSignature(t, name, parameters, returnType));

    private ParseResult<Node.ProcedureSignature> ParseProcedureSignature(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordProcedure)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseFormalParameter, TokenType.PunctuationComma)
        .ParseToken(TokenType.CloseBracket)
    .MapResult(t => new Node.ProcedureSignature(t, name, parameters));

    #endregion Declarations

    #region Statements

    private ParseResult<Node.Statement> ParseStatement(IEnumerable<Token> tokens) => ParseByTokenType(tokens, _statementParsers);

    private ParseResult<Node.Statement.Alternative> ParseAlternative(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var ifClause, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(TokenType.KeywordIf)
            .ParseToken(TokenType.OpenBracket)
            .Parse(out var condition, ParseExpression)
            .ParseToken(TokenType.CloseBracket)
            .ParseToken(TokenType.KeywordThen)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                TokenType.KeywordEndIf, TokenType.KeywordElse, TokenType.KeywordElseIf)
        .MapResult(t => new Node.Statement.Alternative.IfClause(t, condition, block)))

        .ParseZeroOrMoreUntilToken(out var elseIfClauses, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(TokenType.KeywordElseIf)
            .ParseToken(TokenType.OpenBracket)
            .Parse(out var condition, ParseExpression)
            .ParseToken(TokenType.CloseBracket)
            .ParseToken(TokenType.KeywordThen)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                TokenType.KeywordEndIf, TokenType.KeywordElse)
        .MapResult(t => new Node.Statement.Alternative.ElseIfClause(t, condition, block)),
            TokenType.KeywordEndIf, TokenType.KeywordElse)

        .ParseOptional(out var elseClause, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(TokenType.KeywordElse)
            .ParseZeroOrMoreUntilToken(out var elseBlock, ParseStatement, TokenType.KeywordEndIf)
        .MapResult(t => new Node.Statement.Alternative.ElseClause(t, elseBlock)))

        .ParseToken(TokenType.KeywordEndIf)
    .MapResult(t => new Node.Statement.Alternative(t, ifClause, elseIfClauses, elseClause));

    private ParseResult<Node.Statement> ParseEcrireEcran(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordEcrireEcran)
        .ParseToken(TokenType.OpenBracket)
        .ParseOneOrMoreSeparated(out var arguments, ParseExpression, TokenType.PunctuationComma)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(t => new Node.Statement.EcrireEcran(t, arguments));

    private ParseResult<Node.Statement> ParseLireClavier(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordLireClavier)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var argument, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(t => new Node.Statement.LireClavier(t, argument));

    private ParseResult<Node.Statement.VariableDeclaration> ParseVariableDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseOneOrMoreSeparated(out var names, tokens => ParseTokenValue(tokens, TokenType.Identifier), TokenType.PunctuationComma)
        .ParseToken(TokenType.PunctuationColon)
        .Parse(out var type, ParseCompleteType)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(t => new Node.Statement.VariableDeclaration(t, names, type));

    private ParseResult<Node.Statement> ParserVariableDeclarationOrAssignmentOrProcedureCall(IEnumerable<Token> tokens)
    {
        ParseResult<Node.Statement.VariableDeclaration> FinishVariableDeclaration(ParseOperation o, IEnumerable<string> names) => o
            .Parse(out var type, ParseCompleteType)
            .MapResult(t => new Node.Statement.VariableDeclaration(t, names.ToList(), type));

        return ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var ident, TokenType.Identifier)
        .Branch(out Node.Statement result, new() {
            [TokenType.OperatorAssignment] = o => o
                .Parse(out var value, ParseExpression)
            .MapResult(t => new Node.Statement.Assignment(t, ident, value)),
            [TokenType.PunctuationComma] = o => FinishVariableDeclaration(o
                .ParseZeroOrMoreSeparated(out var names,
                    tokens => ParseTokenValue(tokens, TokenType.Identifier),
                    TokenType.PunctuationComma)
                .ParseToken(TokenType.PunctuationColon), names.Prepend(ident)),
            [TokenType.PunctuationColon] = o => FinishVariableDeclaration(o, ident.Yield()),
            [TokenType.OpenBracket] = o => o
                .ParseZeroOrMoreSeparated(out var parameters, ParseEffectiveParameter, TokenType.PunctuationComma)
                .ParseToken(TokenType.CloseBracket)
            .MapResult(t => new Node.Statement.ProcedureCall(t, ident, parameters)),
        })
        .ParseToken(TokenType.PunctuationSemicolon)
        .MapResult(_ => result);
    }

    private ParseResult<Node.Statement.WhileLoop> ParseWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordWhile)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEndDo)
        .ParseToken(TokenType.KeywordEndDo)
        .MapResult(t => new Node.Statement.WhileLoop(t, condition, block));

    private ParseResult<Node.Statement.DoWhileLoop> ParseDoWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordWhile)
        .ParseToken(TokenType.KeywordWhile)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
        .MapResult(t => new Node.Statement.DoWhileLoop(t, condition, block));

    private ParseResult<Node.Statement.RepeatLoop> ParseRepeatLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordRepeat)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordUntil)
        .ParseToken(TokenType.KeywordUntil)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
    .MapResult(t => new Node.Statement.RepeatLoop(t, condition, block));

    private ParseResult<Node.Statement.ForLoop> ParseForLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordFor)
        .ParseTokenValue(out var variantName, TokenType.Identifier)
        .ParseToken(TokenType.KeywordFrom)
        .Parse(out var start, ParseExpression)
        .ParseToken(TokenType.KeywordTo)
        .Parse(out var end, ParseExpression)
        .ParseOptional(out var step, tokens => ParseOperation.Start(this, tokens)
                .ParseToken(TokenType.KeywordStep)
                .Parse(out var step, ParseExpression)
                .MapResult(_ => step))
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEndDo)
        .ParseToken(TokenType.KeywordEndDo)
    .MapResult(t => new Node.Statement.ForLoop(t, variantName, start, end, step, block));

    private ParseResult<Node.Statement.Return> ParseReturn(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordReturn)
        .Parse(out var returnValue, ParseExpression)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(t => new Node.Statement.Return(t, returnValue));

    private ParseResult<Node.Statement.Switch> ParseSwitch(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordSwitch)
        .Parse(out var expression, ParseExpression)
        .ParseToken(TokenType.KeywordIs)

        .ParseZeroOrMoreUntilToken(out var cases, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(TokenType.KeywordWhen)
            .Parse(out var when, ParseExpression)
            .ParseToken(TokenType.PunctuationCase)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
                TokenType.KeywordWhen, TokenType.KeywordWhenOther)
        .MapResult(t => new Node.Statement.Switch.Case(t, when, block)),
            TokenType.KeywordWhenOther, TokenType.KeywordEndSwitch)

        .ParseOptional(out var @default, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(TokenType.KeywordWhenOther)
            .ParseToken(TokenType.PunctuationCase)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEndSwitch)
        .MapResult(t => new Node.Statement.Switch.CaseDefault(t, block)))
        
        .ParseToken(TokenType.KeywordEndSwitch)
    .MapResult(t => new Node.Statement.Switch(t, expression, cases, @default));

    #endregion Statements

    #region Types

    private ParseResult<Node.Type> ParseCompleteType(IEnumerable<Token> tokens) => ParseByTokenType(tokens, _completeTypeParsers);

    private ParseResult<Node.Type> ParseType(IEnumerable<Token> tokens)
     => ParseCompleteType(tokens).Else(()
         => ParseOperation.Start(this, tokens)
            .ParseToken(TokenType.KeywordString)
        .MapResult(t => new Node.Type.String(t)));

    private ParseResult<Node.Type.AliasReference> ParseAliasReference(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var name, TokenType.Identifier)
    .MapResult(t => new Node.Type.AliasReference(t, name));

    private ParseResult<Node.Type.Array> ParseArrayType(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordArray)
        .Parse(out var dimensions, ParseIndexes)
        .ParseToken(TokenType.KeywordFrom)
        .Parse(out var type, ParseCompleteType)
    .MapResult(t => new Node.Type.Array(t, type, dimensions));

    private ParseResult<Node.Type.StringLengthed> ParseLengthedString(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordString)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var length, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
    .MapResult(t => new Node.Type.StringLengthed(t, length));

    private ParseResult<Node.Type.StructureDefinition> ParseStructureDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordStructure)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(out var components, ParseVariableDeclaration, TokenType.KeywordEnd)
        .ParseToken(TokenType.KeywordEnd)
    .MapResult(t => new Node.Type.StructureDefinition(t, components));

    private static ParseMethod<Node.Type.Primitive> MakePrimitiveTypeParser(PrimitiveType type)
     => tokens => ParseResult.Ok(new Node.Type.Primitive(new(tokens, 1), type));

    #endregion Types

    #region Other

    private ParseResult<Node.FormalParameter> ParseFormalParameter(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var mode, tokens => GetByTokenType(tokens, formalParameterModes))
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.PunctuationColon)
        .Parse(out var type, ParseType)
    .MapResult(t => new Node.FormalParameter(t, mode, name, type));

    private ParseResult<Node.EffectiveParameter> ParseEffectiveParameter(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var mode, tokens => GetByTokenType(tokens, effectiveParameterModes))
        .Parse(out var value, ParseExpression)
    .MapResult(t => new Node.EffectiveParameter(t, mode, value));

    #endregion Other

    #region Terminals

    private static readonly IReadOnlyDictionary<TokenType, ParameterMode> formalParameterModes = new Dictionary<TokenType, ParameterMode> {
        [TokenType.KeywordEntF] = ParameterMode.In,
        [TokenType.KeywordSortF] = ParameterMode.Out,
        [TokenType.KeywordEntSortF] = ParameterMode.InOut,
    };
    private static readonly IReadOnlyDictionary<TokenType, ParameterMode> effectiveParameterModes = new Dictionary<TokenType, ParameterMode> {
        [TokenType.KeywordEntE] = ParameterMode.In,
        [TokenType.KeywordSortE] = ParameterMode.Out,
        [TokenType.KeywordEntSortE] = ParameterMode.InOut,
    };

    #endregion Terminals
}
