using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal sealed partial class Parser : CompilationStep
{
    private readonly IEnumerable<Token> _tokens;
    public Parser(IEnumerable<Token> tokens) => _tokens = tokens;

#region General

    // Parsing starts here with the "Algorithm" production rule
    public ParseResult<Node.Algorithm> Parse() => ParseOperation.Start(_tokens)
        .ParseToken(TokenType.KeywordProgram)
        .ParseToken(TokenType.Identifier, out var name)
        .ParseToken(TokenType.KeywordIs)
        .ParseZeroOrMoreUntilEnd(ParseDeclaration, out IReadOnlyCollection<ParseResult<Node.Declaration>> declarations)
    .BuildResult(() => new Node.Algorithm(name, declarations));

#endregion General

#region Declarations

    private static ParseResult<Node.Declaration> ParseDeclaration(IEnumerable<Token> tokens)
     => ParseAliasDeclaration(tokens)
     .Else<Node.Declaration>(() => ParseMainProgram(tokens));

    private static ParseResult<Node.Declaration.Alias> ParseAliasDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordTypeAlias)
        .ParseToken(TokenType.Identifier, out var name)
        .ParseToken(TokenType.OperatorTypeAssignment)
        .Parse(tokens => ParseType(tokens)
            .Else(() => ParseStructureDefinition(tokens)), out var type)
        .ParseToken(TokenType.DelimiterTerminator)
    .BuildResult(() => new Node.Declaration.Alias(name, type));

    private static ParseResult<Node.Declaration.MainProgram> ParseMainProgram(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordBegin)
        .ParseZeroOrMoreUntilToken(ParseStatement, TokenType.KeywordEnd, out var block)
        .ParseToken(TokenType.KeywordEnd)
    .BuildResult(() => new Node.Declaration.MainProgram(block));

#endregion Declarations

#region Statements

    private static readonly IReadOnlyDictionary<TokenType, ParseMethod<Node.Statement>> statementParsers = new Dictionary<TokenType, ParseMethod<Node.Statement>> {
        [TokenType.KeywordEcrireEcran] = ParsePrintStatement,
        [TokenType.KeywordLireClavier] = ParseReadStatement,
        [TokenType.Identifier] = (tokens) => ParseVariableDeclaration(tokens)
            .Else<Node.Statement>(() => ParseAssignment(tokens)),
    };

    private static ParseResult<Node.Statement> ParseStatement(IEnumerable<Token> tokens) => ParseEither(tokens, statementParsers);

    /*private static ParseResult<Node.Statement.Alternative> ParseAlternative(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordIf)
        .Parse(ParseExpression, out var condition)
        .ParseToken(TokenType.KeywordThen)
        .ParseZeroOrMoreUntilToken(ParseStatement, TokenType., out var thenBlock)
        .Parse(tokens => ParseOperation.Start(tokens)
            .ParseToken(TokenType.KeywordElse)
            .ParseZeroOrMoreUntilToken(ParseStatement, TokenType., out var elseBlock)
            .BuildResult(() => elseBlock), out var elseBlock)
        .ParseToken(TokenType.KeywordEndIf)
        .BuildResult(() => new Node.Statement.Alternative(condition, thenBlock, elseBlock.DiscardError()));*/

    private static ParseResult<Node.Statement.VariableDeclaration> ParseVariableDeclaration(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseOneOrMoreDelimited(tokens => ParseTokenValue(tokens, TokenType.Identifier), TokenType.DelimiterSeparator, out var names)
        .ParseToken(TokenType.DelimiterColon)
        .Parse(ParseCompleteType, out var type)
        .ParseToken(TokenType.DelimiterTerminator)
    .BuildResult(() => new Node.Statement.VariableDeclaration(names, type));

    private static ParseResult<Node.Statement.Assignment> ParseAssignment(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.Identifier, out var target)
        .ParseToken(TokenType.OperatorAssignment)
        .Parse(ParseExpression, out var value)
        .ParseToken(TokenType.DelimiterTerminator)
    .BuildResult(() => new Node.Statement.Assignment(target, value));

    private static ParseResult<Node.Statement.Print> ParsePrintStatement(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordEcrireEcran)
        .ParseToken(TokenType.OpenBracket)
        .ParseOneOrMoreDelimited(ParseExpression, TokenType.DelimiterSeparator, out var arguments)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.DelimiterTerminator)
    .BuildResult(() => new Node.Statement.Print(arguments));

    private static ParseResult<Node.Statement.Read> ParseReadStatement(IEnumerable<Token> tokens) => ParseOperation.Start(tokens)
        .ParseToken(TokenType.KeywordLireClavier)
        .ParseToken(TokenType.OpenBracket)
        .Parse(ParseExpression, out var argument)
        .ParseToken(TokenType.CloseBracket)
        .ParseToken(TokenType.DelimiterTerminator)
    .BuildResult(() => new Node.Statement.Read(argument));

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
        .ParseToken(TokenType.KeywordOf)
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
        .ParseZeroOrMoreUntilToken(ParseVariableDeclaration, TokenType.KeywordEnd, out var components)
        .ParseToken(TokenType.KeywordEnd)
    .BuildResult(() => new Node.Type.StructureDefinition(components));

    private static ParseMethod<Node.Type.Primitive> MakePrimitiveTypeParser(PrimitiveType type)
     => tokens => ParseResult.Ok(Take(1, tokens), new Node.Type.Primitive(type));

#endregion Types

#region Terminals

#endregion Terminals
}
