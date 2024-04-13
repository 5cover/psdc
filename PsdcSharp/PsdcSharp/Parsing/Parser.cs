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
        _declarationParsers = new Dictionary<TokenType, ParseMethod<Node.Declaration>> {
            [TokenType.KeywordTypeAlias] = ParseAliasDeclaration,
            [TokenType.KeywordProgram] = ParseMainProgram,
            [TokenType.KeywordFunction] = tokens => ParseFunctionDefinition(tokens)
                      .Else<Node.Declaration>(() => ParseFunctionDeclaration(tokens)),
            [TokenType.KeywordProcedure] = tokens => ParseProcedureDefinition(tokens)
                       .Else<Node.Declaration>(() => ParseProcedureDeclaration(tokens)),
        };
        _statementParsers = new Dictionary<TokenType, ParseMethod<Node.Statement>> {
            [TokenType.KeywordEcrireEcran] = ParsePrintStatement,
            [TokenType.KeywordLireClavier] = ParseReadStatement,
            [TokenType.Identifier] = (tokens) => ParseVariableDeclaration(tokens)
                     .Else<Node.Statement>(() => ParseAssignment(tokens)),
            [TokenType.KeywordIf] = ParseAlternative,
            [TokenType.KeywordWhile] = ParseWhileLoop,
            [TokenType.KeywordDo] = ParseDoWhileLoop,
            [TokenType.KeywordRepeat] = ParseRepeatLoop,
            [TokenType.KeywordFor] = ParseForLoop,
            [TokenType.KeywordReturn] = ParseReturn,
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
            [TokenType.KeywordFalse] = tokens => ParseToken(tokens, TokenType.KeywordFalse, () => new Node.Expression.Literal.False()),
            [TokenType.KeywordTrue] = tokens => ParseToken(tokens, TokenType.KeywordTrue, () => new Node.Expression.Literal.True()),
            [TokenType.LiteralCharacter] = tokens => ParseTokenValue(tokens, TokenType.LiteralCharacter, value => new Node.Expression.Literal.Character(value)),
            [TokenType.LiteralInteger] = tokens => ParseTokenValue(tokens, TokenType.LiteralInteger, value => new Node.Expression.Literal.Integer(value)),
            [TokenType.LiteralReal] = tokens => ParseTokenValue(tokens, TokenType.LiteralReal, value => new Node.Expression.Literal.Real(value)),
            [TokenType.LiteralString] = tokens => ParseTokenValue(tokens, TokenType.LiteralString, value => new Node.Expression.Literal.String(value)),
        };
    }

    // Parsing starts here with the "Algorithm" production rule
    public ParseResult<Node.Algorithm> Parse() => ParseOperation.Start(this, _tokens)
        .ParseZeroOrMoreUntilToken(out var declarations, tokens => ParseEither(tokens, _declarationParsers), TokenType.Eof)
    .MapResult(() => new Node.Algorithm(declarations));

    #region Declarations

    private ParseResult<Node.Declaration.Alias> ParseAliasDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordTypeAlias)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.OperatorTypeAssignment)
        .Parse(out var type, tokens => ParseType(tokens)
                           .Else(() => ParseStructureDefinition(tokens)))
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(() => new Node.Declaration.Alias(name, type));

    private ParseResult<Node.Declaration.MainProgram> ParseMainProgram(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordProgram)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.KeywordIs)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEnd)
        .ParseToken(TokenType.KeywordEnd)
    .MapResult(() => new Node.Declaration.MainProgram(name, block));

    private ParseResult<Node.Declaration.FunctionDeclaration> ParseFunctionDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseFunctionSignature)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(() => new Node.Declaration.FunctionDeclaration(signature));

    private ParseResult<Node.Declaration.ProcedureDeclaration> ParseProcedureDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseProcedureSignature)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(() => new Node.Declaration.ProcedureDeclaration(signature));

    private ParseResult<Node.Declaration.FunctionDefinition> ParseFunctionDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseFunctionSignature)
        .ParseToken(TokenType.KeywordIs)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEnd)
        .ParseToken(TokenType.KeywordEnd)
    .MapResult(() => new Node.Declaration.FunctionDefinition(signature, block));

    private ParseResult<Node.Declaration.ProcedureDefinition> ParseProcedureDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var signature, ParseProcedureSignature)
        .ParseToken(TokenType.KeywordIs)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEnd)
        .ParseToken(TokenType.KeywordEnd)
    .MapResult(() => new Node.Declaration.ProcedureDefinition(signature, block));

    private ParseResult<Node.Declaration.FunctionSignature> ParseFunctionSignature(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordFunction)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseFormalParameter, TokenType.PunctuationComma)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.KeywordDelivers)
        .Parse(out var returnType, ParseType)
    .MapResult(() => new Node.Declaration.FunctionSignature(name, parameters, returnType));

    private ParseResult<Node.Declaration.ProcedureSignature> ParseProcedureSignature(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordProcedure)
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.OpenBracket)
        .ParseZeroOrMoreSeparated(out var parameters, ParseFormalParameter, TokenType.PunctuationComma)
        .ParseToken(TokenType.CloseBracket)
    .MapResult(() => new Node.Declaration.ProcedureSignature(name, parameters));

    private ParseResult<Node.Declaration.FormalParameter> ParseFormalParameter(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .Parse(out var mode, tokens => ParseEither(tokens, formalParameterModes))
        .ParseTokenValue(out var name, TokenType.Identifier)
        .ParseToken(TokenType.PunctuationColon)
        .Parse(out var type, ParseType)
    .MapResult(() => new Node.Declaration.FormalParameter(mode, name, type));

    #endregion Declarations

    #region Statements

    private ParseResult<Node.Statement> ParseStatement(IEnumerable<Token> tokens) => ParseEither(tokens, _statementParsers);

    private ParseResult<Node.Statement.Alternative> ParseAlternative(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordIf)
        .Parse(out var ifClauseCondition, ParseExpression)
        .ParseToken(TokenType.KeywordThen)
        .ParseZeroOrMoreUntilToken(out var ifClauseBlock, ParseStatement,
            TokenType.KeywordElseIf, TokenType.KeywordElse, TokenType.KeywordEndIf)
        .ParseZeroOrMoreUntilToken(out var elseIfClauses,
            tokens => ParseAlternativeClause(tokens, TokenType.KeywordElseIf),
            TokenType.KeywordEndIf, TokenType.KeywordElse)
        .ParseOptional(out var elseClause, tokens => ParseOperation.Start(this, tokens)
            .ParseToken(TokenType.KeywordElse)
            .ParseZeroOrMoreUntilToken(out var elseBlock, ParseStatement, TokenType.KeywordEndIf)
            .MapResult(() => elseBlock))
        .ParseToken(TokenType.KeywordEndIf)
    .MapResult(() => new Node.Statement.Alternative(
                        new(ifClauseCondition, ifClauseBlock),
                        elseIfClauses, elseClause));

    private ParseResult<Node.Statement.Alternative.Clause> ParseAlternativeClause(IEnumerable<Token> tokens, TokenType type) => ParseOperation.Start(this, tokens)
        .ParseToken(type)
        .Parse(out var condition, ParseExpression)
        .ParseToken(TokenType.KeywordThen)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
            TokenType.KeywordElseIf, TokenType.KeywordElse, TokenType.KeywordEndIf)
    .MapResult(() => new Node.Statement.Alternative.Clause(condition, block));

    private ParseResult<Node.Statement.Assignment> ParseAssignment(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var target, TokenType.Identifier)
        .ParseToken(TokenType.OperatorAssignment)
        .Parse(out var value, ParseExpression)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(() => new Node.Statement.Assignment(target, value));

    private ParseResult<Node.Statement.Print> ParsePrintStatement(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordEcrireEcran)
        .ParseToken(TokenType.OpenBracket)
        .ParseOneOrMoreSeparated(out var arguments, ParseExpression, TokenType.PunctuationComma)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(() => new Node.Statement.Print(arguments));

    private ParseResult<Node.Statement.Read> ParseReadStatement(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordLireClavier)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var argument, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(() => new Node.Statement.Read(argument));

    private ParseResult<Node.Statement.VariableDeclaration> ParseVariableDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseOneOrMoreSeparated(out var names, tokens => ParseTokenValue(tokens, TokenType.Identifier), TokenType.PunctuationComma)
        .ParseToken(TokenType.PunctuationColon)
        .Parse(out var type, ParseCompleteType)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(() => new Node.Statement.VariableDeclaration(names, type));

    private ParseResult<Node.Statement.WhileLoop> ParseWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordWhile)
        .Parse(out var condition, ParseExpression)
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEndDo)
        .ParseToken(TokenType.KeywordEndDo)
        .MapResult(() => new Node.Statement.WhileLoop(condition, block));

    private ParseResult<Node.Statement.DoWhileLoop> ParseDoWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordWhile)
        .ParseToken(TokenType.KeywordWhile)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
        .MapResult(() => new Node.Statement.DoWhileLoop(block, condition));

    private ParseResult<Node.Statement.RepeatLoop> ParseRepeatLoop(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordRepeat)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordUntil)
        .ParseToken(TokenType.KeywordUntil)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var condition, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
    .MapResult(() => new Node.Statement.RepeatLoop(block, condition));

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
                .MapResult(() => step))
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, TokenType.KeywordEndDo)
        .ParseToken(TokenType.KeywordEndDo)
    .MapResult(() => new Node.Statement.ForLoop(variantName, start, end, step, block));

    private ParseResult<Node.Statement.Return> ParseReturn(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordReturn)
        .Parse(out var returnValue, ParseExpression)
        .ParseToken(TokenType.PunctuationSemicolon)
    .MapResult(() => new Node.Statement.Return(returnValue));

    #endregion Statements

    #region Types

    private ParseResult<Node.Type> ParseCompleteType(IEnumerable<Token> tokens) => ParseEither(tokens, _completeTypeParsers);

    private ParseResult<Node.Type> ParseType(IEnumerable<Token> tokens)
     => ParseCompleteType(tokens).Else(()
         => ParseOperation.Start(this, tokens)
            .ParseToken(TokenType.KeywordString)
        .MapResult(() => new Node.Type.String()));

    private ParseResult<Node.Type.AliasReference> ParseAliasReference(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var name, TokenType.Identifier)
    .MapResult(() => new Node.Type.AliasReference(name));

    private ParseResult<Node.Type.Array> ParseArrayType(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordArray)
        .Parse(out var dimensions, ParseIndexes)
        .ParseToken(TokenType.KeywordFrom)
        .Parse(out var type, ParseCompleteType)
    .MapResult(() => new Node.Type.Array(type, dimensions));

    private ParseResult<Node.Type.LengthedString> ParseLengthedString(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordString)
        .ParseToken(TokenType.OpenBracket)
        .Parse(out var length, ParseExpression)
        .ParseToken(TokenType.CloseBracket)
    .MapResult(() => new Node.Type.LengthedString(length));

    private ParseResult<Node.Type.StructureDefinition> ParseStructureDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(this, tokens)
        .ParseToken(TokenType.KeywordStructure)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(out var components, ParseVariableDeclaration, TokenType.KeywordEnd)
        .ParseToken(TokenType.KeywordEnd)
    .MapResult(() => new Node.Type.StructureDefinition(components));

    private static ParseMethod<Node.Type.Primitive> MakePrimitiveTypeParser(PrimitiveType type)
     => tokens => ParseResult.Ok(Take(1, tokens), new Node.Type.Primitive(type));

    #endregion Types

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
