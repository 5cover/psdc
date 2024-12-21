using Scover.Psdc.Messages;
using Scover.Psdc.Pseudocode;
using Scover.Psdc.Lexing;

using static Scover.Psdc.Parsing.Node;
using static Scover.Psdc.Lexing.TokenType;

using Type = Scover.Psdc.Parsing.Node.Type;

namespace Scover.Psdc.Parsing;

public sealed partial class Parser
{
    readonly Messenger _msger;
    readonly Parser<Type> _type;
    readonly Parser<Declaration> _declaration;
    readonly Parser<Stmt> _statement;
    readonly Parser<CompilerDirective> _compilerDirective;
    readonly Parser<IEnumerable<Designator>> _designator;

    Parser(Messenger messenger)
    {
        _msger = messenger;

        Dictionary<string, Parser<CompilerDirective>> compilerDirectiveParsers = [];
        AddContextKeyword(compilerDirectiveParsers, ContextKeyword.Assert, HashAssert);
        AddContextKeyword(compilerDirectiveParsers, ContextKeyword.Eval, ParserFirst<CompilerDirective>(HashEvalExpr, HashEvalType));
        _compilerDirective = t => ParseByIdentifierValue(t, "compiler directive", compilerDirectiveParsers, 1);

        Dictionary<TokenType, Parser<Declaration>> declarationParsers = new() {
            [Keyword.Begin] = MainProgram,
            [Keyword.Constant] = Constant,
            [Keyword.Function] = FunctionDeclarationOrDefinition,
            [Keyword.Procedure] = ProcedureDeclarationOrDefinition,
            [Keyword.Type] = TypeAlias,
            [Punctuation.Semicolon] = Nop,
        };
        _declaration = t => ParseByTokenType(t, "declaration", declarationParsers, fallback: _compilerDirective);

        Dictionary<TokenType, Parser<Stmt>> statementParsers = new() {
            [Valued.Identifier] = ParserFirst(Assignment, LocalVariable, ExpressionStatement),
            [Keyword.Do] = DoWhileLoop,
            [Keyword.For] = ForLoop,
            [Keyword.If] = Alternative,
            [Keyword.Repeat] = RepeatLoop,
            [Keyword.Return] = Return,
            [Keyword.Switch] = Switch,
            [Keyword.While] = WhileLoop,
            [Punctuation.Semicolon] = Nop,
        };
        _statement = t => ParseByTokenType(t, "statement", statementParsers, fallback: ParserFirst(Builtin, ExpressionStatement, _compilerDirective));

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

        Dictionary<TokenType, Parser<Expr.Literal>> literalParsers = new() {
            [Keyword.False] = t => ParseToken(t, Keyword.False, t => new Expr.Literal.False(t)),
            [Keyword.True] = t => ParseToken(t, Keyword.True, t => new Expr.Literal.True(t)),
            [Valued.LiteralCharacter] = t => ParseTokenValue(t, Valued.LiteralCharacter, (t, val) => {
                var unescaped = Strings.Unescape(val, Format.Code).ToString();
                if (unescaped.Length != 1) {
                    _msger.Report(Message.ErrorCharacterLiteralContainsMoreThanOneCharacter(t, unescaped[0]));
                }
                return new Expr.Literal.Character(t, unescaped[0]);
            }),
            [Valued.LiteralInteger] = t => ParseTokenValue(t, Valued.LiteralInteger, (t, val) => new Expr.Literal.Integer(t, val)),
            [Valued.LiteralReal] = t => ParseTokenValue(t, Valued.LiteralReal, (t, val) => new Expr.Literal.Real(t, val)),
            [Valued.LiteralString] = t => ParseTokenValue(t, Valued.LiteralString, (t, val) => new Expr.Literal.String(t,
                Strings.Unescape(val, Format.Code).ToString())),
        };
        _literal = t => ParseByTokenType(t, "literal", literalParsers);

        Dictionary<TokenType, Parser<IEnumerable<Designator>>> designatorParsers = new() {
            [Punctuation.LBracket] = ArrayDesignator,
            [Punctuation.Dot] = StructureDesignator,
        };
        _designator = t => ParseByTokenType(t, "designator", designatorParsers);
    }

    // Parsing starts here with the "Algorithm" production rule
    public static ValueOption<Algorithm> Parse(Messenger messenger, IEnumerable<Token> tokens)
    {
        Parser p = new(messenger);
        var algorithm = p.Algorithm(tokens);

        // Don't report an error the errenous token is EOF (which means the tokens stream was empty)
        if (!algorithm.HasValue && algorithm.Error.ErroneousToken.Map(t => t.Type != Eof).ValueOr(true)) {
            messenger.Report(Message.ErrorSyntax(algorithm.SourceTokens.Location, algorithm.Error));
        }

        return algorithm.DropError();
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

    ParseResult<Declaration.TypeAlias> TypeAlias(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "type alias")
        .ParseToken(Keyword.Type)
        .Parse(out var name, Identifier)
        .ParseToken(Punctuation.Equal)
        .Parse(out var type, _type)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Declaration.TypeAlias(t, name, type));

    ParseResult<Declaration.Constant> Constant(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "constant")
        .ParseToken(Keyword.Constant)
        .Parse(out var type, _type)
        .Parse(out var name, Identifier)
        .ParseToken(Punctuation.ColonEqual)
        .Parse(out var value, Initializer)
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
            [Keyword.Is] = o => (o
                .ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.End))
                .ParseToken(Keyword.End),
                t => new Declaration.FunctionDefinition(t, signature, ReportErrors(block))),
            [Keyword.Begin] = o => (o
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
            [Keyword.Is] = o => (o
                .ParseToken(Keyword.Begin)
                .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.End))
                .ParseToken(Keyword.End),
                t => new Declaration.ProcedureDefinition(t, signature, ReportErrors(block))),
            [Keyword.Begin] = o => (o
                .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.End))
                .ParseToken(Keyword.End),
                t => new Declaration.ProcedureDefinition(t, signature, ReportErrors(block))),
        })
        .Fork(out var result, branch)
        .MapResult(result);

    #endregion Declarations

    #region Statements

    ParseResult<Stmt> ExpressionStatement(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "expression statement")
        .Parse(out var expr, Expression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Stmt.ExprStmt(t, expr));

    ParseResult<Stmt> Alternative(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "alternative")
        .ParseToken(Keyword.If)
        .Parse(out var ifCondition, Expression)
        .ParseToken(Keyword.Then)
        .ParseZeroOrMoreUntilToken(out var ifBlock, _statement,
        Set.Of<TokenType>(Keyword.EndIf, Keyword.Else, Keyword.ElseIf))
        .Get(out var ifClause, t => new Stmt.Alternative.IfClause(t, ifCondition, ReportErrors(ifBlock)))

        .ParseZeroOrMoreUntilToken(out var elseIfClauses, tokens
         => ParseOperation.Start(tokens, "alternative sinonsi")
            .ParseToken(Keyword.ElseIf)
            .Parse(out var condition, Expression)
            .ParseToken(Keyword.Then)
            .ParseZeroOrMoreUntilToken(out var block, _statement,
            Set.Of<TokenType>(Keyword.EndIf, Keyword.Else, Keyword.ElseIf))
            .MapResult(t => new Stmt.Alternative.ElseIfClause(t, condition, ReportErrors(block))),
        Set.Of<TokenType>(Keyword.EndIf, Keyword.Else))

        .ParseOptional(out var elseClause, tokens
         => ParseOperation.Start(tokens, "alternative sinon")
            .ParseToken(Keyword.Else)
            .ParseZeroOrMoreUntilToken(out var elseBlock, _statement,
            Set.Of<TokenType>(Keyword.EndIf))
            .MapResult(t => new Stmt.Alternative.ElseClause(t, ReportErrors(elseBlock))))

        .ParseToken(Keyword.EndIf)
        .MapResult(t => new Stmt.Alternative(t, ifClause, ReportErrors(elseIfClauses), elseClause));

    ParseResult<Stmt> Assignment(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "assignment")
        .Parse(out var target, ParseLvalue)
        .ParseToken(Punctuation.ColonEqual)
        .Parse(out var value, Expression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Stmt.Assignment(t, target, value));

    ParseResult<Stmt> Builtin(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "builtin procedure call")
        .Switch<Stmt.Builtin>(out var branch, new() {
            [Keyword.EcrireEcran] = o
             => (o.ParseZeroOrMoreSeparated(out var arguments, Expression,
                    Punctuation.Comma, Punctuation.RParen, readEndToken: false),
                t => new Stmt.Builtin.EcrireEcran(t, ReportErrors(arguments))),
            [Keyword.LireClavier] = o
             => (o.Parse(out var argVariable, ParseLvalue),
                t => new Stmt.Builtin.LireClavier(t, argVariable)),
            [Keyword.Lire] = o
             => (o.Parse(out var argNomLog, Expression)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argVariable, ParseLvalue),
                 t => new Stmt.Builtin.Lire(t, argNomLog, argVariable)),
            [Keyword.Ecrire] = o
             => (o.Parse(out var argNomLog, Expression)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argExpr, Expression),
                t => new Stmt.Builtin.Ecrire(t, argNomLog, argExpr)),
            [Keyword.OuvrirAjout] = o
             => (o.Parse(out var argNomLog, Expression),
                t => new Stmt.Builtin.OuvrirAjout(t, argNomLog)),
            [Keyword.OuvrirEcriture] = o
             => (o.Parse(out var argNomLog, Expression),
                t => new Stmt.Builtin.OuvrirEcriture(t, argNomLog)),
            [Keyword.OuvrirLecture] = o
             => (o.Parse(out var argNomLog, Expression),
                t => new Stmt.Builtin.OuvrirLecture(t, argNomLog)),
            [Keyword.Fermer] = o
             => (o.Parse(out var argNomLog, Expression),
                t => new Stmt.Builtin.Fermer(t, argNomLog)),
            [Keyword.Assigner] = o
             => (o.Parse(out var argNomLog, ParseLvalue)
                 .ParseToken(Punctuation.Comma)
                 .Parse(out var argNomExt, Expression),
                t => new Stmt.Builtin.Assigner(t, argNomLog, argNomExt)),
        })
        .ParseToken(Punctuation.LParen)
        .Fork(out var result, branch)
        .ParseToken(Punctuation.RParen)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(result);

    ParseResult<Stmt> DoWhileLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "do ... while loop")
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.While))
        .ParseToken(Keyword.While)
        .ParseContextKeyword(ContextKeyword.That)
        .Parse(out var condition, Expression)
        .MapResult(t => new Stmt.DoWhileLoop(t, condition, ReportErrors(block)));

    ParseResult<Stmt> ForLoop(IEnumerable<Token> tokens)
    {
        var endTokens = Set.Of<TokenType>(Keyword.EndDo, Keyword.EndFor);
        return ParseOperation.Start(tokens, "for loop")
            .ParseToken(Keyword.For)
            .Parse(out var head, ParserFirst(
                t => ParseOperation.Start(t, "for loop")
                    .Parse(out var variant, ParseLvalue)
                    .ParseContextKeyword(ContextKeyword.From)
                    .Parse(out var start, Expression)
                    .ParseContextKeyword(ContextKeyword.To)
                    .Parse(out var end, Expression)
                    .ParseOptional(out var step, t => ParseOperation.Start(t, "for loop step")
                        .ParseContextKeyword(ContextKeyword.Step)
                        .Parse(out var step, Expression)
                        .MapResult(_ => step))
                    .MapResult(_ => (variant, start, end, step)),
                t => ParseOperation.Start(t, "for loop")
                    .ParseToken(Punctuation.LParen)
                    .Parse(out var variant, ParseLvalue)
                    .ParseContextKeyword(ContextKeyword.From)
                    .Parse(out var start, Expression)
                    .ParseContextKeyword(ContextKeyword.To)
                    .Parse(out var end, Expression)
                    .ParseOptional(out var step, t => ParseOperation.Start(t, "for loop step")
                        .ParseContextKeyword(ContextKeyword.Step)
                        .Parse(out var step, Expression)
                        .MapResult(_ => step))
                    .ParseToken(Punctuation.RParen)
                    .MapResult(_ => (variant, start, end, step))))
            .ParseToken(Keyword.Do)
            .ParseZeroOrMoreUntilToken(out var block, _statement, endTokens)
            .ParseToken(endTokens)
            .MapResult(t => new Stmt.ForLoop(t, head.variant, head.start, head.end, head.step, ReportErrors(block)));
    }

    ParseResult<Nop> Nop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "procedure")
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Nop(t));

    ParseResult<Stmt> RepeatLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "repeat loop")
        .ParseToken(Keyword.Repeat)
        .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.Until))
        .ParseToken(Keyword.Until)
        .Parse(out var condition, Expression)
        .MapResult(t => new Stmt.RepeatLoop(t, condition, ReportErrors(block)));

    ParseResult<Stmt> Return(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "return statement")
        .ParseToken(Keyword.Return)
        .ParseOptional(out var returnValue, Expression)
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Stmt.Return(t, returnValue));

    ParseResult<Stmt> LocalVariable(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "local variable declaration")
        .Parse(out var declaration, t => VariableDeclaration(t, "local variable declaration"))
        .ParseOptional(out var init, t => ParseOperation.Start(t, "local variable initializer")
            .ParseToken(Punctuation.ColonEqual)
            .Parse(out var init, Initializer)
            .MapResult(_ => init))
        .ParseToken(Punctuation.Semicolon)
        .MapResult(t => new Stmt.LocalVariable(t, declaration, init));

    ParseResult<Stmt> Switch(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "switch statement")
        .ParseToken(Keyword.Switch)
        .Parse(out var expression, Expression)
        .ParseToken(Keyword.Is)

        .ParseOneOrMoreUntilToken(out var cases, t => ParseOperation.Start(t, "switch case")
            .ParseToken(Keyword.When)
            .Parse(out var when, Expression)
            .ParseToken(Punctuation.Arrow)
            .ParseZeroOrMoreUntilToken(out var block, _statement,
            Set.Of<TokenType>(Keyword.When, Keyword.EndSwitch))
            .MapResult<Stmt.Switch.Case>(t => when is Expr.Lvalue.VariableReference { Name.Name: "autre" }
                ? new Stmt.Switch.Case.Default(t, ReportErrors(block))
                : new Stmt.Switch.Case.OfValue(t, when, ReportErrors(block))),
        Set.Of<TokenType>(Keyword.EndSwitch))

        .ParseToken(Keyword.EndSwitch)
        .MapResult(t => new Stmt.Switch(t, expression, ReportErrors(cases)));

    ParseResult<Stmt> WhileLoop(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "while loop")
        .ParseToken(Keyword.While)
        .ParseContextKeyword(ContextKeyword.That)
        .Parse(out var condition, Expression)
        .ParseToken(Keyword.Do)
        .ParseZeroOrMoreUntilToken(out var block, _statement, Set.Of<TokenType>(Keyword.EndDo))
        .ParseToken(Keyword.EndDo)
        .MapResult(t => new Stmt.WhileLoop(t, condition, ReportErrors(block)));

    #endregion Statements
    #region Types

    ParseResult<Type> TypeArray(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "array type")
        .ParseToken(Keyword.Array)
        .ParseToken(Punctuation.LBracket)
        .ParseOneOrMoreSeparated(out var dimensions, Expression, Punctuation.Comma, Punctuation.RBracket)
        .ParseContextKeyword(ContextKeyword.From)
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
        .MapResult(_ => init);

    ParseResult<Initializer> BracedInitializer(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "braced initializer")
        .ParseToken(Punctuation.LBrace)
        .ParseZeroOrMoreSeparated(out var values, ParserFirst<Initializer.Braced.Item>((tokens)
             => ParseOperation.Start(tokens, "braced initializer item")
                .ParseOptional(out var designators, t => ParseOperation.Start(t, "designators")
                    .ParseZeroOrMoreUntilToken(out var des, _designator, Set.Of<TokenType>(Punctuation.ColonEqual))
                    .ParseToken(Punctuation.ColonEqual)
                    .MapResult(_ => des))
                .Parse(out var init, Initializer)
                .MapResult(t => new Initializer.Braced.ValuedItem(t, designators.Match(des => ReportErrors(des).SelectMany(d => d).ToArray(), () => []), init)),
            _compilerDirective),
        Punctuation.Comma, Punctuation.RBrace)
        .MapResult(t => new Initializer.Braced(t, ReportErrors(values)));

    ParseResult<IEnumerable<Designator.Array>> ArrayDesignator(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "array designator")
        .ParseToken(Punctuation.LBracket)
        .ParseOneOrMoreSeparated(out var indexes, Expression, Punctuation.Comma, Punctuation.RBracket)
        .MapResult(_ => ReportErrors(indexes).Select(i => new Designator.Array(i.Location, i)));

    ParseResult<IEnumerable<Designator.Structure>> StructureDesignator(IEnumerable<Token> tokens)
     => ParseOperation.Start(tokens, "structure designator")
        .ParseToken(Punctuation.Dot)
        .Parse(out var component, Identifier)
        .MapResult(t => new Designator.Structure(t, component).Yield());

    ParseResult<VariableDeclaration> VariableDeclaration(IEnumerable<Token> tokens, string production)
     => ParseOperation.Start(tokens, production)
        .ParseOneOrMoreSeparated(out var names, Identifier, Punctuation.Comma, Punctuation.Colon)
        .Parse(out var type, _type)
        .MapResult(t => new VariableDeclaration(t, ReportErrors(names), type));

    #endregion Other

    #region Compiler directives

    ParseResult<CompilerDirective.Assert> HashAssert(IEnumerable<Token> tokens) => ParseOperation.Start(tokens, "#assert")
        .ParseToken(Punctuation.NumberSign)
        .ParseContextKeyword(ContextKeyword.Assert)
        .Parse(out var expr, Expression)
        .ParseOptional(out var msg, Expression)
        .MapResult(t => new CompilerDirective.Assert(t, expr, msg));

    ParseResult<CompilerDirective.EvalExpr> HashEvalExpr(IEnumerable<Token> tokens) => ParseOperation.Start(tokens, "#eval expr")
        .ParseToken(Punctuation.NumberSign)
        .ParseContextKeyword(ContextKeyword.Eval)
        .ParseContextKeyword(ContextKeyword.Expr)
        .Parse(out var expr, Expression)
        .MapResult(t => new CompilerDirective.EvalExpr(t, expr));

    ParseResult<CompilerDirective.EvalType> HashEvalType(IEnumerable<Token> tokens) => ParseOperation.Start(tokens, "#eval type")
        .ParseToken(Punctuation.NumberSign)
        .ParseContextKeyword(ContextKeyword.Eval)
        .ParseToken(Keyword.Type)
        .Parse(out var type, _type)
        .MapResult(t => new CompilerDirective.EvalType(t, type));

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
     => source.Select(pr => pr.DropError(err => _msger.Report(Message.ErrorSyntax(pr.SourceTokens.Location, err)))).WhereSome().ToArray();
}
