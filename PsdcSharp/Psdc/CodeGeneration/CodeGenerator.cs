using System.Diagnostics.CodeAnalysis;
using System.Text;
using Scover.Psdc.Pseudocode;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration;

public static class CodeGenerator
{
    public static bool TryGet(string language, [NotNullWhen(true)] out Func<Messenger, Algorithm, string>? func)
    {
        switch (language.ToLower(Format.Code)) {
        case Language.CliOption.C:
            func = (m, a) => new C.CodeGenerator(m).Generate(a);
            return true;
        default:
            func = null;
            return false;
        }
    }
}

delegate StringBuilder Appender<in T>(StringBuilder o, T node);
delegate StringBuilder Appender(StringBuilder o);
delegate string Generator<in T>(T node);

abstract class CodeGenerator<TKwTable, TOpTable>(Messenger msger, TKwTable keywordTable, TOpTable operatorTable)
where TKwTable : KeywordTable
where TOpTable : OperatorTable
{
    protected static Generator<T> ToGenerator<T>(Appender<T> appender)
     => node => appender(new(), node).ToString();

    protected readonly Indentation Indentation = new(4);
    protected readonly Messenger Msger = msger;
    protected readonly TKwTable KwTable = keywordTable;
    protected readonly TOpTable OpTable = operatorTable;

    protected string ValidateIdentifier(Scope scope, Ident ident)
     => KwTable.Validate(scope, ident, Msger);
    protected string ValidateIdentifier(Scope scope, Range location, string ident)
     => KwTable.Validate(scope, location, ident, Msger);

    public abstract string Generate(Algorithm algorithm);

    protected abstract TypeGenerator TypeGeneratorFor(Scope scope);

    protected StringBuilder AppendUnaryOperation<TExpr>(StringBuilder o, OperatorInfo op, TExpr operand, Appender<TExpr> appender) where TExpr : Expr
     => op.Append(o, TypeGeneratorFor(operand.Meta.Scope), [
        o => AppendBetweenParens(o, OpTable.ShouldBracketOperand(op, operand), o => appender(o, operand)),
    ]);

    protected static StringBuilder AppendBetweenParens(StringBuilder o, bool bracket, Action<StringBuilder> appender)
    {
        if (bracket) {
            o.Append('(');
        }
        appender(o);
        if (bracket) {
            o.Append(')');
        }
        return o;
    }

    protected StringBuilder AppendStatements(StringBuilder o, SemanticBlock statements)
    {
        foreach (Statement statement in statements) {
            AppendStatement(o, statement);
        }

        return o;
    }

    protected StringBuilder AppendStatement(StringBuilder o, Statement stmt) => stmt switch {
        Nop => o, // Don't generate Nop, has no purpose, + often got as a result of parsing errors.
        Statement.Alternative alt => AppendAlternative(o, alt),
        Statement.Assignment assignment => AppendAssignment(o, assignment),
        Statement.Builtin.Assigner b => AppendBuiltinAssigner(o, b),
        Statement.Builtin.Ecrire b => AppendBuiltinEcrire(o, b),
        Statement.Builtin.EcrireEcran b => AppendBuiltinEcrireEcran(o, b),
        Statement.Builtin.Fermer b => AppendBuiltinFermer(o, b),
        Statement.Builtin.Lire b => AppendBuiltinLire(o, b),
        Statement.Builtin.LireClavier b => AppendBuiltinLireClavier(o, b),
        Statement.Builtin.OuvrirAjout b => AppendBuiltinOuvrirAjout(o, b),
        Statement.Builtin.OuvrirEcriture b => AppendBuiltinOuvrirEcriture(o, b),
        Statement.Builtin.OuvrirLecture b => AppendBuiltinOuvrirLecture(o, b),
        Statement.DoWhileLoop doWhile => AppendDoWhileLoop(o, doWhile),
        Statement.ExpressionStatement e => AppendExpressionStatement(o, e),
        Statement.ForLoop @for => AppendForLoop(o, @for),
        Statement.LocalVariable local => AppendLocalVariable(o, local),
        Statement.RepeatLoop repeat => AppendRepeatLoop(o, repeat),
        Statement.Return ret => AppendReturn(o, ret),
        Statement.Switch @switch => AppendSwitch(o, @switch),
        Statement.WhileLoop whileLoop => AppendWhileLoop(o, whileLoop),
        _ => throw stmt.ToUnmatchedException(),
    };

    protected StringBuilder AppendDeclaration(StringBuilder o, Declaration decl) => decl switch {
        Nop => o,
        Declaration.Callable callable => AppendCallableDeclaration(o, callable),
        Declaration.CallableDefinition def => AppendCallableDefinition(o, def),
        Declaration.Constant constant => AppendConstant(o, constant),
        Declaration.MainProgram mainProgram => AppendMainProgram(o, mainProgram),
        Declaration.TypeAlias alias => AppendAliasDeclaration(o, alias),
        _ => throw decl.ToUnmatchedException(),
    };

    protected abstract StringBuilder AppendExpressionStatement(StringBuilder o, Statement.ExpressionStatement exprStmt);
    protected abstract StringBuilder AppendAliasDeclaration(StringBuilder o, Declaration.TypeAlias alias);
    protected abstract StringBuilder AppendConstant(StringBuilder o, Declaration.Constant constant);
    protected abstract StringBuilder AppendMainProgram(StringBuilder o, Declaration.MainProgram mainProgram);
    protected abstract StringBuilder AppendCallableDeclaration(StringBuilder o, Declaration.Callable callable);
    protected abstract StringBuilder AppendCallableDefinition(StringBuilder o, Declaration.CallableDefinition def);
    protected abstract StringBuilder AppendAlternative(StringBuilder o, Statement.Alternative alternative);
    protected abstract StringBuilder AppendAssignment(StringBuilder o, Statement.Assignment call);
    protected abstract StringBuilder AppendBuiltinAssigner(StringBuilder o, Statement.Builtin.Assigner assigner);
    protected abstract StringBuilder AppendBuiltinEcrire(StringBuilder o, Statement.Builtin.Ecrire ecrire);
    protected abstract StringBuilder AppendBuiltinEcrireEcran(StringBuilder o, Statement.Builtin.EcrireEcran ecrireEcran);
    protected abstract StringBuilder AppendBuiltinFermer(StringBuilder o, Statement.Builtin.Fermer fermer);
    protected abstract StringBuilder AppendBuiltinLire(StringBuilder o, Statement.Builtin.Lire lire);
    protected abstract StringBuilder AppendBuiltinLireClavier(StringBuilder o, Statement.Builtin.LireClavier lireClavier);
    protected abstract StringBuilder AppendBuiltinOuvrirAjout(StringBuilder o, Statement.Builtin.OuvrirAjout ouvrirAjout);
    protected abstract StringBuilder AppendBuiltinOuvrirEcriture(StringBuilder o, Statement.Builtin.OuvrirEcriture ouvrirEcriture);
    protected abstract StringBuilder AppendBuiltinOuvrirLecture(StringBuilder o, Statement.Builtin.OuvrirLecture ouvrirLecture);
    protected abstract StringBuilder AppendDoWhileLoop(StringBuilder o, Statement.DoWhileLoop doWhileLoop);
    protected abstract StringBuilder AppendForLoop(StringBuilder o, Statement.ForLoop forLoop);
    protected abstract StringBuilder AppendLocalVariable(StringBuilder o, Statement.LocalVariable local);
    protected abstract StringBuilder AppendRepeatLoop(StringBuilder o, Statement.RepeatLoop repeatLoop);
    protected abstract StringBuilder AppendReturn(StringBuilder o, Statement.Return call);
    protected abstract StringBuilder AppendSwitch(StringBuilder o, Statement.Switch @switch);
    protected abstract StringBuilder AppendWhileLoop(StringBuilder o, Statement.WhileLoop whileLoop);
}
