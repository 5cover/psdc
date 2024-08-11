using System.Text;
using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration;

public static class CodeGenerator
{
    public static string GenerateC(Messenger messenger, Algorithm astRoot)
     => new C.CodeGenerator(messenger, astRoot).Generate();
}

delegate StringBuilder Appender<T>(StringBuilder o, T node);
delegate string Generator<T>(T node);

abstract partial class CodeGenerator<TKwTable, TOpTable>(Messenger msger, Algorithm astRoot, TKwTable keywordTable, TOpTable operatorTable)
where TKwTable : KeywordTable
where TOpTable : OperatorTable
{
    protected static Generator<T> ToGenerator<T>(Appender<T> appender)
     => node => appender(new(), node).ToString();

    protected readonly Algorithm _astRoot = astRoot;
    protected readonly Indentation _indent = new();
    protected readonly Messenger _msger = msger;
    protected readonly TKwTable _kwTable = keywordTable;
    protected readonly TOpTable _opTable = operatorTable;

    protected string ValidateIdentifier(Scope scope, Identifier ident) => _kwTable.Validate(scope, ident, _msger);

    public abstract string Generate();

    protected abstract TypeGenerator GenerateType(Scope scope);

    protected StringBuilder AppendUnaryPrefixOperation<TExpr>(StringBuilder o, OperatorInfo op, TExpr operand, Appender<TExpr> appender) where TExpr : Expression
    {
        o.Append(op.Code.Get(GenerateType(operand.Meta.Scope)));
        return AppendBracketed(o, _opTable.ShouldBracketOperand(op, operand), o => appender(o, operand));
    }

    protected static StringBuilder AppendBracketed(StringBuilder o, bool bracket, Action<StringBuilder> appender)
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
        Statement.ForLoop @for => AppendForLoop(o, @for),
        Statement.LocalVariable local => AppendLocalVariable(o, local),
        // Don't generate Nop statements.
        // They serve no purpose.
        // In addition we may get them as a result of parsing errors.
        Statement.Nop => o,
        Statement.ProcedureCall call => AppendProcedureCall(o, call).AppendLine(";"),
        Statement.RepeatLoop repeat => AppendRepeatLoop(o, repeat),
        Statement.Return ret => AppendReturn(o, ret),
        Statement.Switch @switch => AppendSwitch(o, @switch),
        Statement.WhileLoop whileLoop => AppendWhileLoop(o, whileLoop),
        _ => throw stmt.ToUnmatchedException(),
    };

    protected StringBuilder AppendDeclaration(StringBuilder o, Declaration decl) => decl switch {
        Declaration.TypeAlias alias => AppendAliasDeclaration(o, alias),
        Declaration.Constant constant => AppendConstant(o, constant),
        Declaration.MainProgram mainProgram => AppendMainProgram(o, mainProgram),
        Declaration.Function func => AppendFunctionDeclaration(o, func),
        Declaration.Procedure proc => AppendProcedureDeclaration(o, proc),
        Declaration.FunctionDefinition funcDef => AppendFunctionDefinition(o, funcDef),
        Declaration.ProcedureDefinition procDef => AppendProcedureDefinition(o, procDef),
        _ => throw decl.ToUnmatchedException(),
    };

    protected abstract StringBuilder AppendAliasDeclaration(StringBuilder o, Declaration.TypeAlias alias);
    protected abstract StringBuilder AppendConstant(StringBuilder o, Declaration.Constant constant);
    protected abstract StringBuilder AppendFunctionDeclaration(StringBuilder o, Declaration.Function func);
    protected abstract StringBuilder AppendFunctionDefinition(StringBuilder o, Declaration.FunctionDefinition funcDef);
    protected abstract StringBuilder AppendMainProgram(StringBuilder o, Declaration.MainProgram mainProgram);
    protected abstract StringBuilder AppendProcedureDeclaration(StringBuilder o, Declaration.Procedure proc);
    protected abstract StringBuilder AppendProcedureDefinition(StringBuilder o, Declaration.ProcedureDefinition procDef);

    protected abstract StringBuilder AppendAlternative(StringBuilder o, Statement.Alternative alternative);
    protected abstract StringBuilder AppendAssignment(StringBuilder o, Statement.Assignment call);
    protected abstract StringBuilder AppendBuiltinAssigner(StringBuilder o, Statement.Builtin.Assigner assigner);
    protected abstract StringBuilder AppendBuiltinEcrire(StringBuilder o, Statement.Builtin.Ecrire lire);
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
    protected abstract StringBuilder AppendProcedureCall(StringBuilder o, Statement.ProcedureCall call);
    protected abstract StringBuilder AppendRepeatLoop(StringBuilder o, Statement.RepeatLoop repeatLoop);
    protected abstract StringBuilder AppendReturn(StringBuilder o, Statement.Return call);
    protected abstract StringBuilder AppendSwitch(StringBuilder o, Statement.Switch @switch);
    protected abstract StringBuilder AppendWhileLoop(StringBuilder o, Statement.WhileLoop whileLoop);
}
