using Scover.Psdc.Messages;
using Scover.Psdc.Tokenization;

using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Tokenization.TokenType;

using Type = Scover.Psdc.Parsing.Node.Type;

namespace Scover.Psdc.Parsing;

public sealed partial class Parser
{
    readonly Messenger _msger;
    readonly Parser<Type> _type;
    readonly Parser<Declaration> _declaration;
    readonly Parser<Statement> _statement;
    readonly Parser<CompilerDirective> _compilerDirective;

    Parser(Messenger messenger)
    {
        _msger = messenger;

        Dictionary<string, Parser<CompilerDirective>> compilerDirectiveParsers = [];
        AddContextKeyword(compilerDirectiveParsers, ContextKeyword.Assert, Assert);
        AddContextKeyword(compilerDirectiveParsers, ContextKeyword.Eval, ParserFirst<CompilerDirective>(EvaluateExpr, EvaluateType));
        _compilerDirective = t => ParseByIdentifierValue(t, "compiler directive", compilerDirectiveParsers, 1);

        Dictionary<TokenType, Parser<Declaration>> declarationParsers = new() {
            [Keyword.Begin] = MainProgram,
            [Keyword.Constant] = Constant,
            [Keyword.Function] = FunctionDeclarationOrDefinition,
            [Keyword.Procedure] = ProcedureDeclarationOrDefinition,
            [Keyword.Type] = AliasDeclaration,
        };
        _declaration = t => ParseByTokenType(t, "declaration", declarationParsers, fallback: _compilerDirective);

        Dictionary<TokenType, Parser<Statement>> statementParsers = new() {
            [Valued.Identifier] = ParserFirst(Assignment, LocalVariable, ProcedureCall),
            [Keyword.Do] = DoWhileLoop,
            [Keyword.For] = ForLoop,
            [Keyword.If] = Alternative,
            [Keyword.Repeat] = RepeatLoop,
            [Keyword.Return] = Return,
            [Keyword.Switch] = Switch,
            [Keyword.While] = WhileLoop,
            [Punctuation.Semicolon] = Nop,
        };
        _statement = t => ParseByTokenType(t, "statement", statementParsers, fallback: ParserFirst(Builtin, _compilerDirective));

        Dictionary<TokenType, Parser<Type>> typeParsers = new() {
            [Keyword.Integer] = ParserReturn(1, t => new Type.Integer(t)),
            [Keyword.Real] = ParserReturn(1, t => new Type.Real(t)),
            [Keyword.Character] = ParserReturn(1, t => new Type.Character(t)),
            [Keyword.Boolean] = ParserReturn(1, t => new Type.Boolean(t)),
            [Keyword.File] = ParserReturn(1, t => new Type.File(t)),
            [Keyword.String] = TypeString,
            [Keyword.Array] = TypeArray,
            [Valued.Identifier] = ParserReturn1((t, val) => new Type.AliasReference(t, new(t, val))),
            [Keyword.Structure] = TypeStructure,
        };
        _type = t => ParseByTokenType(t, "type", typeParsers);

        Dictionary<TokenType, Parser<Expression.Literal>> literalParsers = new() {
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
        _literal = t => ParseByTokenType(t, "literal", literalParsers);
    }

    // Parsing starts here with the "Algorithm" production rule
    public static ParseResult<Algorithm> Parse(Messenger messenger, IEnumerable<Token> tokens)
    {
        Parser p = new(messenger);

        var algorithm = p.Algorithm(tokens);

        if (!algorithm.HasValue) {
            messenger.Report(Message.ErrorSyntax(algorithm.SourceTokens, algorithm.Error));
        }

        return algorithm;
    }

    ParseResult<Algorithm> Algorithm(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "algorithm")
        .ParseZeroOrMoreUntilToken(out var leadingDirectives, _compilerDirective, Set.Of<TokenType>(Keyword.Program))
        .ParseToken(Keyword.Program)
        .Parse(out var name, Identifier)
        .ParseToken(Keyword.Is)
        .ParseZeroOrMoreUntilToken(out var declarations, _declaration,
        Set.Of(Eof))
        .MapResult(t => new Algorithm(t, ReportErrors(leadingDirectives), name, ReportErrors(declarations)));

    #region Declarations

    ParseResult<Declaration.TypeAlias> AliasDeclaration(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "type alias")
        .ParseToken(Keyword.Type)
        .Parse(out var name, Identifier)
        .ParseToken(Operator.Equal)
        .Parse(out var type, _type)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Declaration.TypeAlias(t, name, type));

    ParseResult<Declaration.Constant> Constant(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "constant")
        .ParseToken(Keyword.Constant)
        .Parse(out var type, _type)
        .Parse(out var name, Identifier)
        .ParseToken(Operator.ColonEqual)
        .Parse(out var value, Expression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Declaration.Constant(t, type, name, value));

    ParseResult<Declaration> FunctionDeclarationOrDefinition(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "function")
        .ParseToken(Keyword.Function)
        .Parse(out var name, Identifier)
        .ParseToken(Punctuation.LParen)
        .ParseZeroOrMoreSeparated(out var parameters, ParameterFormal, Punctuation.Comma, Punctuation.RParen)
        .ParseToken(Keyword.Delivers)
        .Parse(out var returnType, _type)
        .Get(out var signature, t => new FunctionSignature(t, name, ReportErrors(parameters), returnType))
        .Switch<Declaration>(out var branch, new() {
            [Punctuation.Semicolon] = o => (o, t => new Declaration.Function(t, signature)),
            [Keyword.Is] = o
             => (o.ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.End))
                .ParseToken(Keyword.End),
                t => new Declaration.FunctionDefinition(t, signature, ReportErrors(block))),
        })
        .Fork(out var result, branch)
        .MapResult(result);

    ParseResult<Declaration.MainProgram> MainProgram(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "main program")
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.End))
        .ParseToken(Keyword.End)
        .MapResult(t => new Declaration.MainProgram(t, ReportErrors(block)));

    ParseResult<Declaration> ProcedureDeclarationOrDefinition(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "procedure")
        .ParseToken(Keyword.Procedure)
        .Parse(out var name, Identifier)
        .ParseToken(Punctuation.LParen)
        .ParseZeroOrMoreSeparated(out var parameters, ParameterFormal, Punctuation.Comma, Punctuation.RParen)
        .Get(out var signature, t => new ProcedureSignature(t, name, ReportErrors(parameters)))
        .Switch<Declaration>(out var branch, new() {
            [Punctuation.Semicolon] = o => (o, t => new Declaration.Procedure(t, signature)),
            [Keyword.Is] = o
             => (o.ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.End))
                .ParseToken(Keyword.End),
                t => new Declaration.ProcedureDefinition(t, signature, ReportErrors(block)))
        })
        .Fork(out var result, branch)
        .MapResult(result);

    #endregion Declarations

    #region Statements

    ParseResult<Statement> Alternative(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "alternative")
        .ParseToken(Keyword.If)
        .Parse(out var ifCondition, Expression)
        .ParseToken(Keyword.Then)
        .ParseZeroOrMoreUntilToken(out var ifBlock, _statement,
        Set.Of<TokenType>(Keyword.EndIf, Keyword.Else, Keyword.ElseIf))
        .Get(out var ifClause, t => new Statement.Alternative.IfClause(t, ifCondition, ReportErrors(ifBlock)))

        .ParseZeroOrMoreUntilToken(out var elseIfClauses, tokens
         => ParseOperation.Start(tokens, "alternative sinonsi")
            .ParseToken(Keyword.ElseIf)
            .Parse(out var condition, Expression)
            .ParseToken(Keyword.Then)
            .ParseZeroOrMoreUntilToken(out var block, _statement,
            Set.Of<TokenType>(Keyword.EndIf, Keyword.Else, Keyword.ElseIf))
            .MapResult(t => new Statement.Alternative.ElseIfClause(t, condition, ReportErrors(block))),
        Set.Of<TokenType>(Keyword.EndIf, Keyword.Else))

        .ParseOptional(out var elseClause, tokens
         => ParseOperation.Start(tokens, "alternative sinon")
            .ParseToken(Keyword.Else)
            .ParseZeroOrMoreUntilToken(out var elseBlock, _statement,
            Set.Of<TokenType>(Keyword.EndIf))
            .MapResult(t => new Statement.Alternative.ElseClause(t, ReportErrors(elseBlock))))

        .ParseToken(Keyword.EndIf)
        .MapResult(t => new Statement.Alternative(t, ifClause, ReportErrors(elseIfClauses), elseClause));

    ParseResult<Statement> Assignment(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "assignment")
        .Parse(out var target, ParseLvalue)
        .ParseToken(Operator.ColonEqual)
        .Parse(out var value, Expression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.Assignment(t, target, value));

    ParseResult<Statement> Builtin(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "builtin procedure call")
        .Switch<Statement.Builtin>(out var branch, new() {
            [Keyword.EcrireEcran] = o
             => (o.ParseZeroOrMoreSeparated(out var arguments, Expression,
                    Punctuation.Comma, Punctuation.RParen, readEndToken: false),
                t => new Statement.Builtin.EcrireEcran(t, ReportErrors(arguments))),
            [Keyword.LireClavier] = o
             => (o.Parse(out var argVariable, ParseLvalue),
                t => new Statement.Builtin.LireClavier(t, argVariable)),
            [Keyword.Lire] = o
             => (o.Parse(out var argNomLog, Expression)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argVariable, ParseLvalue),
                 t => new Statement.Builtin.Lire(t, argNomLog, argVariable)),
            [Keyword.Ecrire] = o
             => (o.Parse(out var argNomLog, Expression)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argExpr, Expression),
                t => new Statement.Builtin.Ecrire(t, argNomLog, argExpr)),
            [Keyword.OuvrirAjout] = o
             => (o.Parse(out var argNomLog, Expression),
                t => new Statement.Builtin.OuvrirAjout(t, argNomLog)),
            [Keyword.OuvrirEcriture] = o
             => (o.Parse(out var argNomLog, Expression),
                t => new Statement.Builtin.OuvrirEcriture(t, argNomLog)),
            [Keyword.OuvrirLecture] = o
             => (o.Parse(out var argNomLog, Expression),
                t => new Statement.Builtin.OuvrirLecture(t, argNomLog)),
            [Keyword.Fermer] = o
             => (o.Parse(out var argNomLog, Expression),
                t => new Statement.Builtin.Fermer(t, argNomLog)),
            [Keyword.Assigner] = o
             => (o.Parse(out var argNomLog, ParseLvalue)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argNomExt, Expression),
                t => new Statement.Builtin.Assigner(t, argNomLog, argNomExt)),
        })
        .ParseToken(Punctuation.LParen)
        .Fork(out var result, branch)
        .ParseToken(Punctuation.RParen)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(result);

    ParseResult<Statement> DoWhileLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "do..while loop")
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.While))
        .ParseToken(Keyword.While)
        .Parse(out var condition, Expression)
        .MapResult(t => new Statement.DoWhileLoop(t, condition, ReportErrors(block)));

    ParseResult<Statement> ForLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "for loop")
        .ParseToken(Keyword.For)
        .Parse(out var head, ParserFirst(
            t => ParseOperation.Start(t, "for loop")
                .Parse(out var variant, ParseLvalue)
                .ParseToken(Keyword.From)
                .Parse(out var start, Expression)
                .ParseToken(Keyword.To)
                .Parse(out var end, Expression)
                .ParseOptional(out var step, t => ParseOperation.Start(t, "for loop step")
                    .ParseToken(Keyword.Step)
                    .Parse(out var step, Expression)
                    .MapResult(_ => step))
                .MapResult(_ => (variant, start, end, step)),
            t => ParseOperation.Start(t, "for loop")
                .ParseToken(Punctuation.LParen)
                .Parse(out var variant, ParseLvalue)
                .ParseToken(Keyword.From)
                .Parse(out var start, Expression)
                .ParseToken(Keyword.To)
                .Parse(out var end, Expression)
                .ParseOptional(out var step, t => ParseOperation.Start(t, "for loop step")
                    .ParseToken(Keyword.Step)
                    .Parse(out var step, Expression)
                    .MapResult(_ => step))
                .ParseToken(Punctuation.RParen)
                .MapResult(_ => (variant, start, end, step))))
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.EndDo))
        .ParseToken(Keyword.EndDo)
        .MapResult(t => new Statement.ForLoop(t, head.variant, head.start, head.end, head.step, ReportErrors(block)));

    ParseResult<Statement> Nop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "procedure")
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.Nop(t));

    ParseResult<Statement> RepeatLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "repeat loop")
        .ParseToken(Keyword.Repeat)
        .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.Until))
        .ParseToken(Keyword.Until)
        .Parse(out var condition, Expression)
        .MapResult(t => new Statement.RepeatLoop(t, condition, ReportErrors(block)));

    ParseResult<Statement> Return(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "return statement")
        .ParseToken(Keyword.Return)
        .Parse(out var returnValue, Expression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.Return(t, returnValue));

    ParseResult<Statement> LocalVariable(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "local variable declaration")
        .Parse(out var declaration, t => VariableDeclaration(t, "local variable declaration"))
        .ParseOptional(out var init, t => ParseOperation.Start(t, "local variable initializer")
            .ParseToken(Operator.ColonEqual)
            .Parse(out var init, Initializer)
            .MapResult(_ => init))
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.LocalVariable(t, declaration, init));

    ParseResult<Statement> ProcedureCall(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "procedure call")
        .Parse(out var name, Identifier)
        .ParseToken(Punctuation.LParen)
        .ParseZeroOrMoreSeparated(out var arguments, ParameterActual, Punctuation.Comma, Punctuation.RParen)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Statement.ProcedureCall(t, name, ReportErrors(arguments)));

    ParseResult<Statement> Switch(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "switch statement")
        .ParseToken(Keyword.Switch)
        .Parse(out var expression, Expression)
        .ParseToken(Keyword.Is)

        .ParseZeroOrMoreUntilToken(out var cases, t => ParseOperation.Start(t, "switch case")
            .ParseToken(Keyword.When)
            .Parse(out var when, Expression)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, _statement,
            Set.Of<TokenType>(Keyword.When, Keyword.WhenOther))
            .MapResult(t => new Statement.Switch.Case(t, when, ReportErrors(block))),
        Set.Of<TokenType>(Keyword.WhenOther, Keyword.EndSwitch))

        .ParseOptional(out var @default, t => ParseOperation.Start(t, "switch default case")
            .ParseToken(Keyword.WhenOther)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, _statement,
            Set.Of<TokenType>(Keyword.EndSwitch))
            .MapResult(t => new Statement.Switch.DefaultCase(t, ReportErrors(block))))

        .ParseToken(Keyword.EndSwitch)
        .MapResult(t => new Statement.Switch(t, expression, ReportErrors(cases), @default));

    ParseResult<Statement> WhileLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "while loop")
        .ParseToken(Keyword.While)
        .Parse(out var condition, Expression)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.EndDo))
        .ParseToken(Keyword.EndDo)
        .MapResult(t => new Statement.WhileLoop(t, condition, ReportErrors(block)));

    #endregion Statements
    #region Types

    ParseResult<Type> TypeArray(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "array type")
        .ParseToken(Keyword.Array)
        .ParseToken(Punctuation.LBracket)
        .ParseOneOrMoreSeparated(out var dimensions, Expression, Punctuation.Comma, Punctuation.RBracket)
        .ParseToken(Keyword.From)
        .Parse(out var type, _type)
        .MapResult(t => new Type.Array(t, type, ReportErrors(dimensions)));

    ParseResult<Type> TypeString(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "string type")
        .ParseToken(Keyword.String)
        .Switch<Type>(out var branch, new() {
            [Punctuation.LParen] = o => (o
                .Parse(out var length, Expression)
                .ParseToken(Punctuation.RParen),
                t => new Type.LengthedString(t, length))
        }, o => (o, t => new Type.String(t)))
        .Fork(out var result, branch)
        .MapResult(result);

    ParseResult<Type> TypeStructure(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "structure type")
        .ParseToken(Keyword.Structure)
        .ParseToken(Keyword.Begin)
        .ParseZeroOrMoreUntilToken(out var varDecls, ParserFirst<Component>(t => {
            const string Prod = "structure component";
            return ParseOperation.Start(t, Prod)
                .Parse(out var varDecl, t => VariableDeclaration(t, Prod))
                .ParseToken(Punctuation.Semicolon)
                .MapResult(_ => varDecl);
        },
        _compilerDirective), Set.Of<TokenType>(Keyword.End))
        .ParseToken(Keyword.End)
        .MapResult(t => new Type.Structure(t, ReportErrors(varDecls)));

    #endregion Types

    #region Other

    ParseResult<ParameterActual> ParameterActual(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "actual parameter")
        .Parse(out var mode, t => GetByTokenType(t, "actual parameter mode", parameterActualModes))
        .Parse(out var value, Expression)
        .MapResult(t => new ParameterActual(t, mode, value));

    ParseResult<ParameterFormal> ParameterFormal(IEnumerable<Token> tokens)
         => ParseOperation.Start(tokens, "formal parameter")
        .Parse(out var mode, t => GetByTokenType(t, "formal parameter mode", parameterFormalModes))
        .Parse(out var name, Identifier)
        .ParseToken(Punctuation.Colon)
        .Parse(out var type, _type)
        .MapResult(t => new ParameterFormal(t, mode, name, type));

    ParseResult<Initializer> Initializer(IEnumerable<Token> tokens) => ParseOperation.Start(tokens, "initializer")
        .Parse(out var init, ParserFirst(BracedInitializer, Expression))
        .MapResult(t => init);

    ParseResult<Initializer> BracedInitializer(IEnumerable<Token> tokens)
         => ParseOperation.Start(tokens, "braced initializer")
            .ParseToken(Punctuation.LBrace)
            .ParseZeroOrMoreSeparated(out var values, ParserFirst<Initializer.Braced.Item>((tokens)
                 => ParseOperation.Start(tokens, "braced initializer item")
                    .ParseOptional(out var designator, ParserFirst<Designator>(ArrayDesignator, StructureDesignator))
                    .Parse(out var init, Initializer)
                    .MapResult(t => new Initializer.Braced.ValuedItem(t, designator, init)),
                _compilerDirective),
            Punctuation.Comma, Punctuation.RBrace, allowTrailingSeparator: true)
            .MapResult(t => new Initializer.Braced(t, ReportErrors(values)));

    ParseResult<Designator.Array> ArrayDesignator(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "array designator")
        .ParseToken(Punctuation.LBracket)
        .ParseOneOrMoreSeparated(out var indexes, Expression, Punctuation.Comma, Punctuation.RBracket)
        .Get(out var d, t => new Designator.Array(t, ReportErrors(indexes)))
        .ParseToken(Operator.ColonEqual) // hide this token from the designator's SourceToken, as it's not "technically" part of it.
        .MapResult(_ => d);

    ParseResult<Designator.Structure> StructureDesignator(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "structure designator")
        .ParseToken(Operator.Dot)
        .Parse(out var component, Identifier)
        .Get(out var d, t => new Designator.Structure(t, component))
        .ParseToken(Operator.ColonEqual)
        .MapResult(_ => d);

    ParseResult<VariableDeclaration> VariableDeclaration(IEnumerable<Token> tokens, string production)
     => ParseOperation.Start(tokens, production)
        .ParseOneOrMoreSeparated(out var names, Identifier, Punctuation.Comma, Punctuation.Colon)
        .Parse(out var type, _type)
        .MapResult(t => new VariableDeclaration(t, ReportErrors(names), type));

    #endregion Other

    #region Compiler directives

    ParseResult<CompilerDirective.Assert> Assert(IEnumerable<Token> tokens) => ParseOperation.Start(tokens, "#assert")
        .ParseToken(Punctuation.NumberSign)
        .ParseContextKeyword(ContextKeyword.Assert)
        .Parse(out var expr, Expression)
        .ParseOptional(out var msg, Expression)
        .MapResult(t => new CompilerDirective.Assert(t, expr, msg));

    ParseResult<CompilerDirective.EvaluateExpr> EvaluateExpr(IEnumerable<Token> tokens) => ParseOperation.Start(tokens, "#eval expr")
        .ParseToken(Punctuation.NumberSign)
        .ParseContextKeyword(ContextKeyword.Eval)
        .ParseContextKeyword(ContextKeyword.Expr)
        .Parse(out var expr, Expression)
        .MapResult(t => new CompilerDirective.EvaluateExpr(t, expr));

    ParseResult<CompilerDirective.EvaluateType> EvaluateType(IEnumerable<Token> tokens) => ParseOperation.Start(tokens, "#eval type")
        .ParseToken(Punctuation.NumberSign)
        .ParseContextKeyword(ContextKeyword.Eval)
        .ParseToken(Keyword.Type)
        .Parse(out var type, _type)
        .MapResult(t => new CompilerDirective.EvaluateType(t, type));

    #endregion Compiler directives

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

    ParseResult<Identifier> Identifier(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "identifier")
        .ParseTokenValue(out var name, Valued.Identifier)
        .MapResult(t => new Identifier(t, name));

    #endregion Terminals

    T[] ReportErrors<T>(IEnumerable<ParseResult<T>> source)
     => source.Select(pr => pr.DropError(err => _msger.Report(Message.ErrorSyntax(pr.SourceTokens, err)))).WhereSome().ToArray();
}
