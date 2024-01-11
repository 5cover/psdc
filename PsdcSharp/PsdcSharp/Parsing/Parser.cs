using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal sealed partial class Parser : CompilationStep
{
    private readonly IEnumerable<Token> _tokens;
    public Parser(IEnumerable<Token> tokens) => _tokens = tokens;

    // Parsing starts here with the "Algorithm" production rule
    public ParseResult<Node.Algorithm> Parse() => ParseOperation.Start(_tokens)
        .ParseZeroOrMoreUntilToken(tokens => ParseEither(tokens, declarationParsers),
            out IReadOnlyCollection<ParseResult<Node.Declaration>> declarations)
    .BuildResult(() => new Node.Algorithm(declarations));

    #region Declarations

    private static readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Declaration>> declarationParsers = new Dictionary<TokenType, ParseMethod<Node.Declaration>> {
        [TokenType.KeywordTypeAlias] = ParseAliasDeclaration,
        [TokenType.KeywordProgram] = ParseMainProgram,
    };

    private static ParseResult<Node.Declaration.Alias> ParseAliasDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordTypeAlias)
        .ParseToken(TokenType.Identifier, out var name)
        .ParseToken(TokenType.OperatorTypeAssignment)
        .Parse(tokens => ParseType(tokens)
            .Else(() => ParseStructureDefinition(tokens)), out var type)
        .ParseToken(TokenType.PunctuationTerminator)
    .BuildResult(() => new Node.Declaration.Alias(name, type));

    private static ParseResult<Node.Declaration.MainProgram> ParseMainProgram(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordProgram)
        .ParseToken(TokenType.Identifier, out var name)
        .ParseToken(TokenType.KeywordIs)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(ParseStatement, out var block, TokenType.KeywordEnd)
        .ParseToken(TokenType.KeywordEnd)
    .BuildResult(() => new Node.Declaration.MainProgram(name, block));

    #endregion Declarations

    #region Statements

    private static readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Statement>> statementParsers = new Dictionary<TokenType, ParseMethod<Node.Statement>> {
        [TokenType.KeywordEcrireEcran] = ParsePrintStatement,
        [TokenType.KeywordLireClavier] = ParseReadStatement,
        [TokenType.Identifier] = (tokens) => ParseVariableDeclaration(tokens)
            .Else<Node.Statement>(() => ParseAssignment(tokens)),
        [TokenType.KeywordIf] = ParseAlternative,
        [TokenType.KeywordWhile] = ParseWhileLoop,
        [TokenType.KeywordDo] = ParseDoWhileLoop,
        [TokenType.KeywordRepeat] = ParseRepeatLoop,
        [TokenType.KeywordFor] = ParseForLoop,
    };

    private static ParseResult<Node.Statement> ParseStatement(IEnumerable<Token> tokens) => ParseEither(tokens, statementParsers);

    private static ParseResult<Node.Statement.Alternative> ParseAlternative(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordIf)
        .Parse(ParseExpression, out var ifClauseCondition)
        .ParseToken(TokenType.KeywordThen)
        .ParseZeroOrMoreUntilToken(ParseStatement, out var ifClauseBlock,
            TokenType.KeywordElseIf, TokenType.KeywordElse, TokenType.KeywordEndIf)
        .ParseZeroOrMoreUntilToken(tokens => ParseAlternativeClause(tokens, TokenType.KeywordElseIf), out var elseIfClauses,
            TokenType.KeywordEndIf, TokenType.KeywordElse)
        .Parse(tokens => ParseOperation.Start(tokens)
            .ParseToken(TokenType.KeywordElse)
            .ParseZeroOrMoreUntilToken(ParseStatement, out var elseBlock, TokenType.KeywordEndIf)
            .BuildResult(() => elseBlock), out var elseClause)
        .ParseToken(TokenType.KeywordEndIf)
        .BuildResult(() => new Node.Statement.Alternative(new(ifClauseCondition, ifClauseBlock),
            elseIfClauses, elseClause.DiscardError()));

    private static ParseResult<Node.Statement.Alternative.Clause> ParseAlternativeClause(IEnumerable<Token> tokens, TokenType type) => ParseOperation.Start(tokens)
        .ParseToken(type)
        .Parse(ParseExpression, out var condition)
        .ParseToken(TokenType.KeywordThen)
        .ParseZeroOrMoreUntilToken(ParseStatement, out var block,
            TokenType.KeywordElseIf, TokenType.KeywordElse, TokenType.KeywordEndIf)
    .BuildResult(() => new Node.Statement.Alternative.Clause(condition, block));

    private static ParseResult<Node.Statement.Assignment> ParseAssignment(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.Identifier, out var target)
        .ParseToken(TokenType.OperatorAssignment)
        .Parse(ParseExpression, out var value)
        .ParseToken(TokenType.PunctuationTerminator)
    .BuildResult(() => new Node.Statement.Assignment(target, value));

    private static ParseResult<Node.Statement.Print> ParsePrintStatement(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordEcrireEcran)
        .ParseToken(TokenType.OpenBracket)
        .ParseOneOrMoreDelimited(ParseExpression, TokenType.PunctuationSeparator, out var arguments)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.PunctuationTerminator)
    .BuildResult(() => new Node.Statement.Print(arguments));

    private static ParseResult<Node.Statement.Read> ParseReadStatement(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordLireClavier)
        .ParseToken(TokenType.OpenBracket)
        .Parse(ParseExpression, out var argument)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.PunctuationTerminator)
    .BuildResult(() => new Node.Statement.Read(argument));

    private static ParseResult<Node.Statement.VariableDeclaration> ParseVariableDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseOneOrMoreDelimited(tokens => ParseTokenValue(tokens, TokenType.Identifier), TokenType.PunctuationSeparator, out var names)
        .ParseToken(TokenType.PunctuationColon)
        .Parse(ParseCompleteType, out var type)
        .ParseToken(TokenType.PunctuationTerminator)
    .BuildResult(() => new Node.Statement.VariableDeclaration(names, type));

    private static ParseResult<Node.Statement.WhileLoop> ParseWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordWhile)
        .Parse(ParseExpression, out var condition)
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(ParseStatement, out var block, TokenType.KeywordEndDo)
        .ParseToken(TokenType.KeywordEndDo)
        .BuildResult(() => new Node.Statement.WhileLoop(condition, block));

    private static ParseResult<Node.Statement.DoWhileLoop> ParseDoWhileLoop(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(ParseStatement, out var block, TokenType.KeywordWhile)
        .ParseToken(TokenType.KeywordWhile)
        .ParseToken(TokenType.OpenBracket)
        .Parse(ParseExpression, out var condition)
        .ParseToken(TokenType.CloseBracket)
        .BuildResult(() => new Node.Statement.DoWhileLoop(block, condition));

    private static ParseResult<Node.Statement.RepeatLoop> ParseRepeatLoop(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordRepeat)
        .ParseZeroOrMoreUntilToken(ParseStatement, out var block, TokenType.KeywordUntil)
        .ParseToken(TokenType.KeywordUntil)
        .ParseToken(TokenType.OpenBracket)
        .Parse(ParseExpression, out var condition)
        .ParseToken(TokenType.CloseBracket)
        .BuildResult(() => new Node.Statement.RepeatLoop(block, condition));

    private static ParseResult<Node.Statement.ForLoop> ParseForLoop(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordFor)
        .ParseToken(TokenType.Identifier, out var variantName)
        .ParseToken(TokenType.KeywordFrom)
        .Parse(ParseExpression, out var start)
        .ParseToken(TokenType.KeywordTo)
        .Parse(ParseExpression, out var end)
        .Parse(tokens => ParseOperation.Start(tokens)
                .ParseToken(TokenType.KeywordStep)
                .Parse(ParseExpression, out var step)
                .FlattenResult(() => step), out var step)
        .ParseToken(TokenType.KeywordDo)
        .ParseZeroOrMoreUntilToken(ParseStatement, out var block, TokenType.KeywordEndDo)
        .ParseToken(TokenType.KeywordEndDo)
        .BuildResult(() => new Node.Statement.ForLoop(variantName, start, end, step.DiscardError(), block));

    #endregion Statements

    #region Types

    private static readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Type>> completeTypeParsers = new Dictionary<TokenType, ParseMethod<Node.Type>> {
        [TokenType.KeywordInteger] = MakePrimitiveTypeParser(PrimitiveType.Integer),
        [TokenType.KeywordReal] = MakePrimitiveTypeParser(PrimitiveType.Real),
        [TokenType.KeywordCharacter] = MakePrimitiveTypeParser(PrimitiveType.Character),
        [TokenType.KeywordBoolean] = MakePrimitiveTypeParser(PrimitiveType.Boolean),
        [TokenType.KeywordString] = ParseLengthedString,
        [TokenType.KeywordArray] = ParseArrayType,
        [TokenType.Identifier] = ParseAliasReference,
    };

    private static ParseResult<Node.Type> ParseCompleteType(IEnumerable<Token> tokens) => ParseEither(tokens, completeTypeParsers);

    private static ParseResult<Node.Type> ParseType(IEnumerable<Token> tokens)
     => ParseCompleteType(tokens).Else(()
         => ParseOperation.Start(tokens)
            .ParseToken(TokenType.KeywordString)
        .BuildResult(() => new Node.Type.String()));

    private static ParseResult<Node.Type.AliasReference> ParseAliasReference(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.Identifier, out var name)
    .BuildResult(() => new Node.Type.AliasReference(name));

    private static ParseResult<Node.Type.Array> ParseArrayType(IEnumerable<Token> tokens)
     => ParseIndexes(ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordArray), out var dimensions)
        .ParseToken(TokenType.KeywordFrom)
        .Parse(ParseCompleteType, out var type)
    .BuildResult(() => new Node.Type.Array(type, dimensions));

    private static ParseResult<Node.Type.LengthedString> ParseLengthedString(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordString)
        .ParseToken(TokenType.OpenBracket)
        .Parse(ParseExpression, out var length)
        .ParseToken(TokenType.CloseBracket)
    .BuildResult(() => new Node.Type.LengthedString(length));

    private static ParseResult<Node.Type.StructureDefinition> ParseStructureDefinition(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordStructure)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(ParseVariableDeclaration, out var components, TokenType.KeywordEnd)
        .ParseToken(TokenType.KeywordEnd)
    .BuildResult(() => new Node.Type.StructureDefinition(components));

    private static ParseMethod<Node.Type.Primitive> MakePrimitiveTypeParser(PrimitiveType type)
     => tokens => ParseResult.Ok(Take(1, tokens), new Node.Type.Primitive(type));

    #endregion Types

    #region Terminals

    #endregion Terminals
}
