using System.Globalization;
using System.Text;

using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration.C;

sealed partial class CodeGenerator(Messenger messenger, Algorithm astRoot)
    : CodeGenerator<KeywordTable, OperatorTable>(messenger, astRoot, KeywordTable.Instance, OperatorTable.Instance)
{
    readonly IncludeSet _includes = new();
    Group _currentGroup = Group.None;

    public override string Generate()
    {
        StringBuilder o = new();

        foreach (Declaration decl in _astRoot.Declarations) {
            AppendDeclaration(o, decl);
        }

        return _includes.AppendIncludeSection(AppendFileHeader(new()))
            .Append(o).ToString();
    }

    #region Declarations

    protected override StringBuilder AppendAliasDeclaration(StringBuilder o, Declaration.TypeAlias alias)
    {
        SetGroup(o, Group.Types);
        return Indent(o).AppendLine($"typedef {CreateTypeInfo(alias.Meta.Scope, alias.Type).GenerateDeclaration(ValidateIdentifier(alias.Meta.Scope, alias.Name))};");
    }

    protected override StringBuilder AppendConstant(StringBuilder o, Declaration.Constant constant)
    {
        SetGroup(o, Group.Macros);
        Indent(o).Append($"#define {ValidateIdentifier(constant.Meta.Scope, constant.Name)} ");
        return AppendExpression(o, constant.Value, _opTable.ShouldBracketOperand(_opTable.None, constant.Value)).AppendLine();
    }

    protected override StringBuilder AppendFunctionDeclaration(StringBuilder o, Declaration.Function func)
     => AppendFunctionSignature(SetGroup(o, Group.Prototypes), func.Signature).AppendLine(";");

    protected override StringBuilder AppendFunctionDefinition(StringBuilder o, Declaration.FunctionDefinition func)
    {
        AppendFunctionSignature(o.AppendLine(), func.Signature);
        return AppendBlock(o.Append(' '), func.Block).AppendLine();
    }

    StringBuilder AppendFunctionSignature(StringBuilder o, FunctionSignature sig)
     => AppendSignature(o, CreateTypeInfo(sig.Meta.Scope, sig.ReturnType).ToString(), sig.Name, sig.Meta.Scope, sig.Parameters);

    protected override StringBuilder AppendMainProgram(StringBuilder o, Declaration.MainProgram mainProgram)
    {
        SetGroup(o, Group.Main);
        Indent(o).Append("int main() ");
        return AppendBlock(o, mainProgram.Block, sb
             => Indent(sb.AppendLine()).AppendLine("return 0;"))
            .AppendLine();
    }

    protected override StringBuilder AppendProcedureDeclaration(StringBuilder o, Declaration.Procedure proc)
     => AppendProcedureSignature(SetGroup(o, Group.Prototypes), proc.Signature).AppendLine(";");

    protected override StringBuilder AppendProcedureDefinition(StringBuilder o, Declaration.ProcedureDefinition procDef)
    {
        AppendProcedureSignature(o.AppendLine(), procDef.Signature);
        return AppendBlock(o.Append(' '), procDef.Block).AppendLine();
    }

    StringBuilder AppendProcedureSignature(StringBuilder o, ProcedureSignature sig)
     => AppendSignature(o, "void", sig.Name, sig.Meta.Scope, sig.Parameters);

    StringBuilder AppendSignature(StringBuilder o, string cReturnType, Identifier name, Scope scope, IEnumerable<ParameterFormal> parameters)
    {
        o.Append($"{cReturnType} {ValidateIdentifier(scope, name)}(");
        if (parameters.Any()) {
            o.AppendJoin(", ", parameters.Select(p => GenerateParameter(scope, p)));
        } else {
            o.Append("void");
        }
        o.Append(')');

        return o;
    }

    string GenerateParameter(Scope scope, ParameterFormal param)
    {
        StringBuilder o = new();
        var type = CreateTypeInfo(scope, param.Type);
        if (C.RequiresPointer(param.Mode)) {
            type = type.ToPointer(1);
        }

        o.Append(type.GenerateDeclaration(param.Name.Name));

        return o.ToString();
    }

    #endregion Declarations

    #region Statements

    protected override StringBuilder AppendAlternative(StringBuilder o, Statement.Alternative alternative)
    {
        AppendAlternativeClause(Indent(o), "if", alternative.If.Condition, alternative.If.Block);

        if (alternative.ElseIfs.Count == 0 && !alternative.Else.HasValue) {
            return o.AppendLine();
        }

        foreach (var elseIf in alternative.ElseIfs) {
            AppendAlternativeClause(o, " else if", elseIf.Condition, elseIf.Block);
        }

        alternative.Else.Tap(@else => AppendBlock(o.Append(" else "), @else.Block));

        o.AppendLine();

        return o;

        StringBuilder AppendAlternativeClause(StringBuilder o, string keyword, Expression condition, SemanticBlock block)
        {
            AppendBracketedExpression(o.Append($"{keyword} "), condition);
            return AppendBlock(o.Append(' '), block);
        }
    }

    protected override StringBuilder AppendAssignment(StringBuilder o, Statement.Assignment assignment)
    {
        Indent(o);

        if (assignment.Target.Value.Type.IsConvertibleTo(StringType.Instance)
         && assignment.Target.Value.Type.IsConvertibleTo(StringType.Instance)) {
            _includes.Ensure(IncludeSet.String);
            AppendExpression(o.Append("strcpy("), assignment.Target);
            AppendExpression(o.Append(", "), assignment.Value).Append(')');
        } else {
            AppendExpression(o, assignment.Target);
            o.Append(" = ");
            AppendExpression(o, assignment.Value);
        }

        return o.AppendLine(";");
    }

    StringBuilder AppendBlock(StringBuilder o, SemanticBlock block, Action<StringBuilder>? suffix = null)
    {
        o.AppendLine("{");
        _indent.Increase();
        AppendStatements(o, block);
        suffix?.Invoke(o);
        _indent.Decrease();
        Indent(o).Append('}');

        return o;
    }

    protected override StringBuilder AppendDoWhileLoop(StringBuilder o, Statement.DoWhileLoop doWhileLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, doWhileLoop.Block).Append(" while ");
        AppendBracketedExpression(o, doWhileLoop.Condition);
        return o.AppendLine(";");
    }

    protected override StringBuilder AppendBuiltinEcrireEcran(StringBuilder o, Statement.Builtin.EcrireEcran ecrireEcran)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(ecrireEcran.Arguments);

        Indent(o).Append($@"printf(""{format}\n""");

        foreach (Expression arg in arguments) {
            AppendExpression(o.Append(", "), arg);
        }

        return o.AppendLine(");");
    }

    protected override StringBuilder AppendForLoop(StringBuilder o, Statement.ForLoop forLoop)
    {
        Indent(o);
        AppendExpression(o.Append("for ("), forLoop.Variant).Append(" = ");
        AppendExpression(o, forLoop.Start);

        AppendExpression(o.Append("; "), forLoop.Variant).Append(" <= ");
        AppendExpression(o, forLoop.End);

        AppendExpression(o.Append("; "), forLoop.Variant);
        forLoop.Step.Must(
            // replace += by ++ when the step is a literal 1
            step => step is not Expression.Literal { UnderlyingValue: 1 })
            .Match(step => AppendExpression(o.Append(" += "), step),
                   none: () => o.Append("++"));

        return AppendBlock(o.Append(") "), forLoop.Block).AppendLine();
    }

    protected override StringBuilder AppendBuiltinLireClavier(StringBuilder o, Statement.Builtin.LireClavier lireClavier)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(lireClavier.ArgumentVariable.Yield());

        Indent(o).Append($@"scanf(""{format}""");

        foreach (Expression arg in arguments) {
            AppendExpression(o.Append(", &"), arg);
        }

        return o.AppendLine(");");
    }

    protected override StringBuilder AppendRepeatLoop(StringBuilder o, Statement.RepeatLoop repeatLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, repeatLoop.Block).Append(" while ");
        AppendBracketedExpression(o, repeatLoop.Condition.Invert());
        return o.AppendLine(";");
    }

    protected override StringBuilder AppendReturn(StringBuilder o, Statement.Return ret)
    {
        Indent(o).Append($"return ");
        AppendExpression(o, ret.Value);
        return o.AppendLine(";");
    }

    protected override StringBuilder AppendSwitch(StringBuilder o, Statement.Switch @switch)
    {
        Indent(o).Append("switch ");
        AppendBracketedExpression(o, @switch.Expression).AppendLine(" {");

        foreach (var @case in @switch.Cases) {
            Indent(o).Append("case ");
            AppendExpression(o, @case.Value).AppendLine(":");
            _indent.Increase();
            AppendStatements(o, @case.Block);
            Indent(o).AppendLine("break;");
            _indent.Decrease();
        }

        @switch.Default.Tap(@default => {
            Indent(o).AppendLine("default:");
            _indent.Increase();
            AppendStatements(o, @default.Block);
            _indent.Decrease();
        });

        return Indent(o).AppendLine("}");
    }

    protected override StringBuilder AppendLocalVariable(StringBuilder o, Statement.LocalVariable local)
    {
        Indent(o).Append(CreateTypeInfo(local.Meta.Scope, local.Declaration.Type)
            .GenerateDeclaration(local.Declaration.Names.Select(n => n.Name)));
        local.Initializer.Tap(i => AppendInitializer(o.Append(" = "), i));
        return o.AppendLine(";");
    }

    StringBuilder AppendInitializer(StringBuilder o, Initializer initializer) => initializer switch {
        Expression left => AppendExpression(o, left),
        Initializer.Braced array => AppendBracedInitializer(o, array),
        _ => throw initializer.ToUnmatchedException(),
    };

    StringBuilder AppendBracedInitializer(StringBuilder o, Initializer.Braced initializer)
    {
        o.AppendLine("{");
        _indent.Increase();
        foreach (var value in initializer.Items) {
            Indent(o);
            value.Designator.Tap(d => (d switch {
                Designator.Array array => AppendIndex(o, array.Index),
                Designator.Structure structure => o.Append($".{structure.Component}"),
                _ => throw d.ToUnmatchedException(),
            }).Append(" = "));
            AppendInitializer(o, value.Initializer);
            o.AppendLine(",");
        }
        _indent.Decrease();
        return Indent(o).Append('}');
    }

    protected override StringBuilder AppendWhileLoop(StringBuilder o, Statement.WhileLoop whileLoop)
    {
        Indent(o).Append("while ");
        AppendBracketedExpression(o, whileLoop.Condition);
        return AppendBlock(o.Append(' '), whileLoop.Block).AppendLine();
    }

    protected override StringBuilder AppendProcedureCall(StringBuilder o, Statement.ProcedureCall call)
     => AppendCall(Indent(o), call);

    #endregion Statements

    #region Expressions

    StringBuilder AppendArraySubscript(StringBuilder o, Expression.Lvalue.ArraySubscript arrSub)
    {
        AppendExpression(o, arrSub.Array, _opTable.ShouldBracket(arrSub));
        AppendIndex(o, arrSub.Index);
        return o;
    }

    StringBuilder AppendIndex(StringBuilder o, IReadOnlyList<Expression> index)
    {
        foreach (Expression i in index) {
            AppendExpressionAlter(o.Append('['), i, _opTable.Subtract, 1, (l, r) => l - r).Append(']');
        }
        return o;
    }

    StringBuilder AppendBracketedExpression(StringBuilder o, Expression left)
     => AppendExpression(o, left, left is not BracketedExpressionNode);

    StringBuilder AppendExpression(StringBuilder o, Expression left) => AppendExpression(o, left, false);
    
    StringBuilder AppendExpression(StringBuilder o, Expression left, bool bracket) => AppendBracketed(o, bracket, o => {
        _ = left switch {
            SemanticBracketedExpressionNode b => AppendExpression(o, b.ContainedExpression, !bracket),
            Expression.FunctionCall call => AppendCall(o, call),
            Expression.Literal { Value: CharacterValue v } => o.Append($"'{v.Status.Comptime.Unwrap().ToString(CultureInfo.InvariantCulture)}'"),
            Expression.Literal { Value: BooleanValue v } => AppendLiteralBoolean(v.Status.Comptime.Unwrap()),
            Expression.Literal { Value: StringValue v } => o.Append($"\"{v.Status.Comptime.Unwrap().ToString(CultureInfo.InvariantCulture)}\""),
            Expression.Literal l => o.Append(l.UnderlyingValue.ToString(CultureInfo.InvariantCulture)),
            Expression.Lvalue.ArraySubscript arrSub => AppendArraySubscript(o, arrSub),
            Expression.Lvalue.ComponentAccess compAccess
             => AppendExpression(o, compAccess.Structure, _opTable.ShouldBracket(compAccess)).Append('.').Append(compAccess.ComponentName),
            Expression.Lvalue.VariableReference variable => AppendVariableReference(o, variable),
            Expression.BinaryOperation opBin => AppendOperationBinary(o, opBin),
            Expression.UnaryOperation opUn => AppendOperationUnary(o, opUn),
            _ => throw left.ToUnmatchedException(),
        };

        StringBuilder AppendLiteralBoolean(bool b)
        {
            _includes.Ensure(IncludeSet.StdBool);
            return o.Append(b ? "true" : "false");
        }
    });

    StringBuilder AppendVariableReference(StringBuilder o, Expression.Lvalue.VariableReference variable)
     => C.IsPointer(variable)
        ? AppendUnaryPrefixOperation(o, _opTable.Dereference, variable,
                                    (o, v) => o.Append(ValidateIdentifier(v.Meta.Scope, v.Name)))
        : o.Append(ValidateIdentifier(variable.Meta.Scope, variable.Name));

    StringBuilder AppendOperationBinary(StringBuilder o, Expression.BinaryOperation opBin)
    {
        if (opBin.Left.Value.Type.IsConvertibleTo(StringType.Instance)
         && opBin.Right.Value.Type.IsConvertibleTo(StringType.Instance)
         && IsStringComparisonOperator(opBin.Operator)) {
            _includes.Ensure(IncludeSet.String);
            AppendExpression(o.Append("strcmp("), opBin.Left);
            AppendExpression(o.Append(", "), opBin.Right);
            return o.Append($") {_opTable.Get(opBin.Operator).Code.Get(GenerateType(opBin.Meta.Scope))} 0");
        }

        var (bracketLeft, bracketRight) = _opTable.ShouldBracketBinary(opBin);
        AppendExpression(o, opBin.Left, bracketLeft);
        o.Append($" {_opTable.Get(opBin.Operator).Code.Get(GenerateType(opBin.Meta.Scope))} ");
        return AppendExpression(o, opBin.Right, bracketRight);
    }

    private static bool IsStringComparisonOperator(BinaryOperator b) => b
        is BinaryOperator.Equal
        or BinaryOperator.GreaterThan
        or BinaryOperator.GreaterThanOrEqual
        or BinaryOperator.LessThan
        or BinaryOperator.LessThanOrEqual
        or BinaryOperator.NotEqual;

    StringBuilder AppendOperationUnary(StringBuilder o, Expression.UnaryOperation opUn)
    {
        o.Append(_opTable.Get(opUn.Operator).Code.Get(GenerateType(opUn.Meta.Scope)));
        return AppendExpression(o, opUn.Operand, _opTable.ShouldBracketUnary(opUn));
    }

    #endregion Expressions

    #region Helpers

    StringBuilder AppendCall(StringBuilder o, SemanticCallNode call)
    {
        const string ParameterSeparator = ", ";

        o.Append($"{ValidateIdentifier(call.Meta.Scope, call.Name)}(");
        foreach (var param in call.Parameters) {
            if (C.RequiresPointer(param.Mode) && !C.IsPointer(param.Value)) {
                AppendUnaryPrefixOperation(o, _opTable.AddressOf, param.Value,
                                           (o, e) => AppendExpression(o, e));
            } else {
                AppendExpression(o, param.Value);
            }
            o.Append(ParameterSeparator);
        }
        if (call.Parameters.Count > 0) {
            o.Length -= ParameterSeparator.Length; // Remove unnecessary last separator
        }
        return o.Append(')');
    }

    StringBuilder AppendFileHeader(StringBuilder o) => o.AppendLine($"""
        /** @file
         * @brief {_astRoot.Name}
         * @author {Environment.UserName}
         * @date {DateOnly.FromDateTime(DateTime.Now)}
         */

        """);

    TypeInfo CreateTypeInfo(Scope scope, EvaluatedType type)
    {
        var info = TypeInfo.Create(type, new(scope, _msger, _kwTable,
            ToGenerator<Expression>(AppendExpression),
            ToGenerator<Expression>((o, e) => AppendExpressionAlter(o, e, _opTable.Add, 1, (l, r) => l + r))));
        foreach (string header in info.RequiredHeaders) {
            _includes.Ensure(header);
        }
        return info;
    }

    StringBuilder AppendExpressionAlter<T>(StringBuilder o,
        Expression left,
        OperatorInfo @operator,
        T right,
        Func<T, T, T> collapseLiterals) where T : IConvertible
        // Collapse literals
         => left is Expression.Literal { UnderlyingValue: T leftVal }
                ? o.Append(collapseLiterals(leftVal, right).ToString(CultureInfo.InvariantCulture))
                : AppendExpression(o, left, _opTable.ShouldBracketOperand(@operator, left))
                    .Append($" {@operator.Code.Get(GenerateType(left.Meta.Scope))} {right.ToString(CultureInfo.InvariantCulture)}");

    protected override TypeGenerator GenerateType(Scope scope)
     => type => CreateTypeInfo(scope, type);

    StringBuilder Indent(StringBuilder o) => _indent.Indent(o);

    StringBuilder SetGroup(StringBuilder o, Group newGroup)
    {
        if (_currentGroup != newGroup) {
            _currentGroup = newGroup;
            o.AppendLine();
        }
        return o;
    }

    protected override StringBuilder AppendBuiltinLire(StringBuilder o, Statement.Builtin.Lire lire)
     => throw new NotImplementedException();
    protected override StringBuilder AppendBuiltinEcrire(StringBuilder o, Statement.Builtin.Ecrire lire)
     => throw new NotImplementedException();
    protected override StringBuilder AppendBuiltinFermer(StringBuilder o, Statement.Builtin.Fermer fermer)
     => throw new NotImplementedException();
    protected override StringBuilder AppendBuiltinOuvrirLecture(StringBuilder o, Statement.Builtin.OuvrirLecture ouvrirLecture)
     => throw new NotImplementedException();
    protected override StringBuilder AppendBuiltinOuvrirEcriture(StringBuilder o, Statement.Builtin.OuvrirEcriture ouvrirEcriture)
     => throw new NotImplementedException();
    protected override StringBuilder AppendBuiltinOuvrirAjout(StringBuilder o, Statement.Builtin.OuvrirAjout ouvrirAjout)
     => throw new NotImplementedException();
    protected override StringBuilder AppendBuiltinAssigner(StringBuilder o, Statement.Builtin.Assigner assigner)
     => throw new NotImplementedException();

    #endregion Helpers

    enum Group
    {
        None,
        Macros,
        Types,
        Prototypes,
        Main,
    }
}
