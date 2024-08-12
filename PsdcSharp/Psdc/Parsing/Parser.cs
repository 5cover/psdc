using System.Reflection.Emit;
using Scover.Psdc.Messages;
using Scover.Psdc.Tokenization;

using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Tokenization.TokenType;

using Type = Scover.Psdc.Parsing.Node.Type;

namespace Scover.Psdc.Parsing;

public sealed partial class Parser
{
    readonly IReadOnlyDictionary<TokenType, Parser<Type.Complete>> _completeTypeParsers;
    readonly IReadOnlyDictionary<TokenType, Parser<Declaration>> _declarationParsers;
    readonly IReadOnlyDictionary<TokenType, Parser<Statement>> _statementParsers;

    readonly Messenger _msger;

    Parser(Messenger messenger)
    {
        _msger = messenger;

        _declarationParsers = new Dictionary<TokenType, Parser<Declaration>>() {
            [Keyword.Begin] = ParseMainProgram,
            [Keyword.Constant] = ParseConstant,
            [Keyword.Function] = ParseFunctionDeclarationOrDefinition,
            [Keyword.Procedure] = ParseProcedureDeclarationOrDefinition,
            [Keyword.TypeAlias] = ParseAliasDeclaration,
        };

        _statementParsers = new Dictionary<TokenType, Parser<Statement>>() {
            [Valued.Identifier] = ParserFirst(ParseAssignment, ParseLocalVariable, ParseProcedureCall),
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
            [Keyword.Integer] = ParserReturn(1, t => new Type.Complete.Integer(t)),
            [Keyword.Real] = ParserReturn(1, t => new Type.Complete.Real(t)),
            [Keyword.Character] = ParserReturn(1, t => new Type.Complete.Character(t)),
            [Keyword.Boolean] = ParserReturn(1, t => new Type.Complete.Boolean(t)),
            [Keyword.File] = ParserReturn(1, t => new Type.Complete.File(t)),
            [Keyword.String] = ParseTypeLengthedString,
            [Keyword.Array] = ParseTypeArray,
            [Valued.Identifier] = ParserReturn((t, val) => new Type.Complete.AliasReference(t, new(t, val))),
            [Keyword.Structure] = ParseTypeStructure,
        };

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

    // Parsing starts here with the "Algorithm" production rule
    public static ParseResult<Algorithm> Parse(Messenger messenger, IEnumerable<Token> tokens)
    {
        Parser p = new(messenger);

        var algorithm = p.ParseAlgorithm(tokens);

        if (!algorithm.HasValue) {
            messenger.Report(Message.ErrorSyntax(algorithm.SourceTokens, algorithm.Error));
        }

        return algorithm;
    }

    ParseResult<Algorithm> ParseAlgorithm(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "algorithm")
        .ParseToken(Keyword.Program)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Keyword.Is)
        .ParseZeroOrMoreUntilToken(out var declarations, t
         => ParseByTokenType(t, "declaration", _declarationParsers),
        [Eof])
        .MapResult(t => new Algorithm(t, name, declarations));

    ParseResult<Type> ParseType(IEnumerable<Token> tokens)
     => ParserFirst<Type>(ParseTypeComplete, ParseTypeAliasReference, ParseTypeString)(tokens);

    #region Declarations

    ParseResult<Declaration.TypeAlias> ParseAliasDeclaration(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "type alias")
        .ParseToken(Keyword.TypeAlias)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Operator.Equal)
        .Parse(out var type, ParseType)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Declaration.TypeAlias(t, name, type));

    ParseResult<Declaration.Constant> ParseConstant(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "constant")
        .ParseToken(Keyword.Constant)
        .Parse(out var type, ParseType)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Operator.ColonEqual)
        .Parse(out var value, ParseExpression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Declaration.Constant(t, type, name, value));

    ParseResult<Declaration> ParseFunctionDeclarationOrDefinition(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "function")
        .ParseToken(Keyword.Function)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.LParen)
        .ParseZeroOrMoreSeparated(out var parameters, ParseParameterFormal, Punctuation.Comma, Punctuation.RParen)
        .ParseToken(Keyword.Delivers)
        .Parse(out var returnType, ParseType)
        .Get(out var signature, t => new FunctionSignature(t, name, parameters, returnType))
        .ChooseBranch<Declaration>(out var branch, new() {
            [Punctuation.Semicolon] = o => (o, t => new Declaration.Function(t, signature)),
            [Keyword.Is] = o
             => (o.ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, ParseStatement, [Keyword.End])
                .ParseToken(Keyword.End),
                t => new Declaration.FunctionDefinition(t, signature, block)),
        })
        .Fork(out var result, branch)
        .MapResult(result);

    ParseResult<Declaration.MainProgram> ParseMainProgram(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "main program")
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, [Keyword.End])
        .ParseToken(Keyword.End)
        .MapResult(t => new Declaration.MainProgram(t, block));

    ParseResult<Declaration> ParseProcedureDeclarationOrDefinition(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "procedure")
        .ParseToken(Keyword.Procedure)
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.LParen)
        .ParseZeroOrMoreSeparated(out var parameters, ParseParameterFormal, Punctuation.Comma, Punctuation.RParen)
        .Get(out var signature, t => new ProcedureSignature(t, name, parameters))
        .ChooseBranch<Declaration>(out var branch, new() {
            [Punctuation.Semicolon] = o => (o, t => new Declaration.Procedure(t, signature)),
            [Keyword.Is] = o
             => (o.ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, ParseStatement, [Keyword.End])
                .ParseToken(Keyword.End),
                t => new Declaration.ProcedureDefinition(t, signature, block))
        })
        .Fork(out var result, branch)
        .MapResult(result);

    #endregion Declarations

    #region Statements

    ParseResult<Statement> ParseAlternative(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "alternative")
        .ParseToken(Keyword.If)
        .Parse(out var ifCondition, ParseExpression)
        .ParseToken(Keyword.Then)
        .ParseZeroOrMoreUntilToken(out var ifBlock, ParseStatement,
        [Keyword.EndIf, Keyword.Else, Keyword.ElseIf])
        .Get(out var ifClause, t => new Statement.Alternative.IfClause(t, ifCondition, ifBlock))

        .ParseZeroOrMoreUntilToken(out var elseIfClauses, tokens
         => ParseOperation.Start(_msger, tokens, "alternative sinonsi")
            .ParseToken(Keyword.ElseIf)
            .Parse(out var condition, ParseExpression)
            .ParseToken(Keyword.Then)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
            [Keyword.EndIf, Keyword.Else, Keyword.ElseIf])
            .MapResult(t => new Statement.Alternative.ElseIfClause(t, condition, block)),
        [Keyword.EndIf, Keyword.Else])

        .ParseOptional(out var elseClause, tokens
         => ParseOperation.Start(_msger, tokens, "alternative sinon")
            .ParseToken(Keyword.Else)
            .ParseZeroOrMoreUntilToken(out var elseBlock, ParseStatement,
            [Keyword.EndIf])
            .MapResult(t => new Statement.Alternative.ElseClause(t, elseBlock)))

        .ParseToken(Keyword.EndIf)
        .MapResult(t => new Statement.Alternative(t, ifClause, elseIfClauses, elseClause));

    ParseResult<Statement> ParseAssignment(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "assignment")
        .Parse(out var target, ParseLvalue)
        .ParseToken(Operator.ColonEqual)
        .Parse(out var value, ParseExpression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.Assignment(t, target, value));

    ParseResult<Statement> ParseBuiltin(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "builtin procedure call")
        .ChooseBranch<Statement.Builtin>(out var branch, new() {
            [Keyword.EcrireEcran] = o
             => (o.ParseZeroOrMoreSeparated(out var arguments, ParseExpression,
                    Punctuation.Comma, Punctuation.RParen, readEndToken: false),
                t => new Statement.Builtin.EcrireEcran(t, arguments)),
            [Keyword.LireClavier] = o
             => (o.Parse(out var argVariable, ParseLvalue),
                t => new Statement.Builtin.LireClavier(t, argVariable)),
            [Keyword.Lire] = o
             => (o.Parse(out var argNomLog, ParseExpression)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argVariable, ParseLvalue),
                 t => new Statement.Builtin.Lire(t, argNomLog, argVariable)),
            [Keyword.Ecrire] = o
             => (o.Parse(out var argNomLog, ParseExpression)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argExpr, ParseExpression),
                t => new Statement.Builtin.Ecrire(t, argNomLog, argExpr)),
            [Keyword.OuvrirAjout] = o
             => (o.Parse(out var argNomLog, ParseExpression),
                t => new Statement.Builtin.OuvrirAjout(t, argNomLog)),
            [Keyword.OuvrirEcriture] = o
             => (o.Parse(out var argNomLog, ParseExpression),
                t => new Statement.Builtin.OuvrirEcriture(t, argNomLog)),
            [Keyword.OuvrirLecture] = o
             => (o.Parse(out var argNomLog, ParseExpression),
                t => new Statement.Builtin.OuvrirLecture(t, argNomLog)),
            [Keyword.Fermer] = o
             => (o.Parse(out var argNomLog, ParseExpression),
                t => new Statement.Builtin.Fermer(t, argNomLog)),
            [Keyword.Assigner] = o
             => (o.Parse(out var argNomLog, ParseLvalue)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argNomExt, ParseExpression),
                t => new Statement.Builtin.Assigner(t, argNomLog, argNomExt)),
        })
        .ParseToken(Punctuation.LParen)
        .Fork(out var result, branch)
        .ParseToken(Punctuation.RParen)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(result);

    ParseResult<Statement> ParseDoWhileLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "do..while loop")
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, [Keyword.While])
        .ParseToken(Keyword.While)
        .Parse(out var condition, ParseExpression)
        .MapResult(t => new Statement.DoWhileLoop(t, condition, block));

    ParseResult<Statement> ParseForLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "for loop")
        .ParseToken(Keyword.For)
        .Parse(out var head, ParserFirst(
            t => ParseOperation.Start(_msger, t, "for loop")
                .Parse(out var variant, ParseLvalue)
                .ParseToken(Keyword.From)
                .Parse(out var start, ParseExpression)
                .ParseToken(Keyword.To)
                .Parse(out var end, ParseExpression)
                .ParseOptional(out var step, o => ParseOperation.Start(_msger, t, "for loop step")
                    .ParseToken(Keyword.Step)
                    .Parse(out var step, ParseExpression)
                    .MapResult(_ => step))
                .MapResult(_ => (variant, start, end, step)),
            t => ParseOperation.Start(_msger, t, "for loop")
                .ParseToken(Punctuation.LParen)
                .Parse(out var variant, ParseLvalue)
                .ParseToken(Keyword.From)
                .Parse(out var start, ParseExpression)
                .ParseToken(Keyword.To)
                .Parse(out var end, ParseExpression)
                .ParseOptional(out var step, t => ParseOperation.Start(_msger, t, "for loop step")
                    .ParseToken(Keyword.Step)
                    .Parse(out var step, ParseExpression)
                    .MapResult(_ => step))
                .ParseToken(Punctuation.RParen)
                .MapResult(_ => (variant, start, end, step))))
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, [Keyword.EndDo])
        .ParseToken(Keyword.EndDo)
        .MapResult(t => new Statement.ForLoop(t, head.variant, head.start, head.end, head.step, block));

    ParseResult<Statement> ParseNop(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "procedure")
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.Nop(t));

    ParseResult<Statement> ParseRepeatLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "repeat loop")
        .ParseToken(Keyword.Repeat)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, [Keyword.Until])
        .ParseToken(Keyword.Until)
        .Parse(out var condition, ParseExpression)
        .MapResult(t => new Statement.RepeatLoop(t, condition, block));

    ParseResult<Statement> ParseReturn(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "return statement")
        .ParseToken(Keyword.Return)
        .Parse(out var returnValue, ParseExpression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.Return(t, returnValue));

    ParseResult<Statement> ParseLocalVariable(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "local variable declaration")
        .Parse(out var declaration, t => ParseVariableDeclaration(t, "local variable declaration"))
        .ParseOptional(out var init, t => ParseOperation.Start(_msger, t, "local variable initializer")
            .ParseToken(Operator.ColonEqual)
            .Parse(out var init, ParseInitializer)
            .MapResult(_ => init))
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.LocalVariable(t, declaration, init));

    ParseResult<Statement> ParseProcedureCall(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "procedure call")
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.LParen)
        .ParseZeroOrMoreSeparated(out var arguments, ParseParameterActual, Punctuation.Comma, Punctuation.RParen)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.ProcedureCall(t, name, arguments));

    ParseResult<Statement> ParseStatement(IEnumerable<Token> tokens)
     => ParseByTokenType(tokens, "statement", _statementParsers, ParseBuiltin);

    ParseResult<Statement> ParseSwitch(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "switch statement")
        .ParseToken(Keyword.Switch)
        .Parse(out var expression, ParseExpression)
        .ParseToken(Keyword.Is)

        .ParseZeroOrMoreUntilToken(out var cases, t => ParseOperation.Start(_msger, t, "switch case")
            .ParseToken(Keyword.When)
            .Parse(out var when, ParseExpression)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
            [Keyword.When, Keyword.WhenOther])
            .MapResult(t => new Statement.Switch.Case(t, when, block)),
        [Keyword.WhenOther, Keyword.EndSwitch])

        .ParseOptional(out var @default, t => ParseOperation.Start(_msger, t, "switch default case")
            .ParseToken(Keyword.WhenOther)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, ParseStatement,
            [Keyword.EndSwitch])
            .MapResult(t => new Statement.Switch.DefaultCase(t, block)))

        .ParseToken(Keyword.EndSwitch)
        .MapResult(t => new Statement.Switch(t, expression, cases, @default));

    ParseResult<Statement> ParseWhileLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "while loop")
        .ParseToken(Keyword.While)
        .Parse(out var condition, ParseExpression)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, ParseStatement, [Keyword.EndDo])
        .ParseToken(Keyword.EndDo)
        .MapResult(t => new Statement.WhileLoop(t, condition, block));

    #endregion Statements

    #region Types

    ParseResult<Type.AliasReference> ParseTypeAliasReference(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "type alias reference")
        .Parse(out var name, ParseIdentifier)
        .MapResult(t => new Type.AliasReference(t, name));

    ParseResult<Type.Complete.Array> ParseTypeArray(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "array type")
        .ParseToken(Keyword.Array)
        .ParseToken(Punctuation.LBracket)
        .ParseOneOrMoreSeparated(out var dimensions, ParseExpression, Punctuation.Comma, Punctuation.RBracket)
        .ParseToken(Keyword.From)
        .Parse(out var type, ParseTypeComplete)
        .MapResult(t => new Type.Complete.Array(t, type, dimensions));

    ParseResult<Type.Complete> ParseTypeComplete(IEnumerable<Token> tokens)
     => ParseByTokenType(tokens, "complete type", _completeTypeParsers);

    ParseResult<Type.Complete.LengthedString> ParseTypeLengthedString(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "string type")
        .ParseToken(Keyword.String)
        .ParseToken(Punctuation.LParen)
        .Parse(out var length, ParseExpression)
        .ParseToken(Punctuation.RParen)
        .MapResult(t => new Type.Complete.LengthedString(t, length));

    ParseResult<Type.String> ParseTypeString(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "string type")
        .ParseToken(Keyword.String)
        .MapResult(t => new Type.String(t));

    ParseResult<Type.Complete.Structure> ParseTypeStructure(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "structure type")
        .ParseToken(Keyword.Structure)
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var varDecls, op => op
            .Parse(out var varDecl, t => ParseVariableDeclaration(t, "structure component"))
            .ParseToken(Punctuation.Semicolon)
            .GetResult(_ => varDecl), [Keyword.End])
        .ParseToken(Keyword.End)
        .MapResult(t => new Type.Complete.Structure(t, varDecls));

    #endregion Types

    #region Other

    ParseResult<ParameterActual> ParseParameterActual(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "actual parameter")
        .Parse(out var mode, t => GetByTokenType(t, "actual parameter mode", parameterActualModes))
        .Parse(out var value, ParseExpression)
        .MapResult(t => new ParameterActual(t, mode, value));

    ParseResult<ParameterFormal> ParseParameterFormal(IEnumerable<Token> tokens)
         => ParseOperation.Start(_msger, tokens, "formal parameter")
        .Parse(out var mode, t => GetByTokenType(t, "formal parameter mode", parameterFormalModes))
        .Parse(out var name, ParseIdentifier)
        .ParseToken(Punctuation.Colon)
        .Parse(out var type, ParseType)
        .MapResult(t => new ParameterFormal(t, mode, name, type));

    ParseResult<Initializer> ParseInitializer(IEnumerable<Token> tokens) => ParseOperation.Start(_msger, tokens, "initializer")
        .Parse(out var initializer, ParserFirst(ParseBracedInitializer, ParseExpression))
        .MapResult(t => initializer);

    ParseResult<Initializer> ParseBracedInitializer(IEnumerable<Token> tokens)
         => ParseOperation.Start(_msger, tokens, "braced initializer")
            .ParseToken(Punctuation.LBrace)
            .ParseZeroOrMoreSeparated(out var values, (tokens)
             => ParseOperation.Start(_msger, tokens, "braced initializer item")
                .ParseOptional(out var designator, ParserFirst<Designator>(ParseArrayDesignator, ParseStructureDesignator))
                .Parse(out var init, ParseInitializer)
                .MapResult(t => new Initializer.Braced.Item(t, designator, init)),
            Punctuation.Comma, Punctuation.RBrace, allowTrailingSeparator: true)
            .MapResult(t => new Initializer.Braced(t, values));

    ParseResult<Designator.Array> ParseArrayDesignator(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "array designator")
        .ParseToken(Punctuation.LBracket)
        .ParseOneOrMoreSeparated(out var indexes, ParseExpression, Punctuation.Comma, Punctuation.RBracket)
        .Get(out var d, t => new Designator.Array(t, indexes))
        .ParseToken(Operator.ColonEqual) // hide this token from the designator's SourceToken, as it's not "technically" part of it.
        .MapResult(_ => d);

    ParseResult<Designator.Structure> ParseStructureDesignator(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "structure designator")
        .ParseToken(Operator.Dot)
        .Parse(out var component, ParseIdentifier)
        .Get(out var d, t => new Designator.Structure(t, component))
        .ParseToken(Operator.ColonEqual)
        .MapResult(_ => d);

    ParseResult<VariableDeclaration> ParseVariableDeclaration(IEnumerable<Token> tokens, string production)
     => ParseOperation.Start(_msger, tokens, production)
        .ParseOneOrMoreSeparated(out var names, ParseIdentifier, Punctuation.Comma, Punctuation.Colon)
        .Parse(out var type, ParseTypeComplete)
        .MapResult(t => new VariableDeclaration(t, names, type));

    #endregion Other

    #region Terminals

    static readonly IReadOnlyDictionary<TokenType, ParameterMode> parameterActualModes = new Dictionary<TokenType, ParameterMode> {
        [Keyword.EntE] = ParameterMode.In,
        [Keyword.SortE] = ParameterMode.Out,
        [Keyword.EntSortE] = ParameterMode.InOut,
    };

    static readonly IReadOnlyDictionary<TokenType, ParameterMode> parameterFormalModes = new Dictionary<TokenType, ParameterMode> {
        [Keyword.EntF] = ParameterMode.In,
        [Keyword.SortF] = ParameterMode.Out,
        [Keyword.EntSortF] = ParameterMode.InOut,
    };

    ParseResult<Identifier> ParseIdentifier(IEnumerable<Token> tokens)
     => ParseOperation.Start(_msger, tokens, "identifier")
        .ParseTokenValue(out var name, Valued.Identifier)
        .MapResult(t => new Identifier(t, name));

    #endregion Terminals
}
