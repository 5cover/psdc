using System.Text;

using Scover.Psdc.Pseudocode;
using Scover.Psdc.Messages;
using Scover.Psdc.StaticAnalysis;

using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration.C;

sealed partial class CodeGenerator(Messenger messenger)
    : CodeGenerator<KeywordTable, OperatorTable>(messenger, KeywordTable.Instance, OperatorTable.Instance)
{
    readonly IncludeSet _includes = new();
    Group _currentGroup = Group.None;

    public override string Generate(Algorithm algorithm)
    {
        StringBuilder o = new();

        foreach (var decl in algorithm.Declarations) {
            AppendDeclaration(o, decl);
        }

        return _includes.AppendIncludeSection(AppendFileHeader(new(), algorithm))
           .Append(o).ToString();
    }

    #region Declarations

    protected override StringBuilder AppendAliasDeclaration(StringBuilder o, Declaration.TypeAlias alias)
    {
        SetGroup(o, Group.Types);
        return Indent(o).AppendLine(Format.Code,
            $"typedef {CreateTypeInfo(alias.Meta.Scope, alias.Type).GenerateDeclaration(ValidateIdentifier(alias.Meta.Scope, alias.Name))};");
    }

    protected override StringBuilder AppendConstant(StringBuilder o, Declaration.Constant constant)
    {
        var normalName = ValidateIdentifier(constant.Meta.Scope, constant.Name);
        return AppendCDefine(o, normalName, constant.Value switch {
            Initializer.Braced braced => AppendBracedInitializer(new(), braced),
            Expr expr => AppendExpression(new(), expr, OpTable.ShouldBracketOperand(OperatorInfo.None, expr)),
            _ => throw constant.Value.ToUnmatchedException(),
        });
    }

    StringBuilder AppendCDefine(StringBuilder o, string name, StringBuilder body)
    {
        SetGroup(o, Group.Macros);
        return Indent(o).AppendLine(Format.Code,
            $"#define {name} {body.Replace(Environment.NewLine, '\\' + Environment.NewLine)}");
    }

    protected override StringBuilder AppendCallableDeclaration(StringBuilder o, Declaration.Callable callable)
        => AppendCallableSignature(SetGroup(o, Group.Prototypes), callable.Signature).AppendLine(";");

    protected override StringBuilder AppendCallableDefinition(StringBuilder o, Declaration.CallableDefinition def)
    {
        AppendCallableSignature(o.AppendLine(), def.Signature);
        return AppendBlock(o.Append(' '), def.Block).AppendLine();
    }

    protected override StringBuilder AppendMainProgram(StringBuilder o, Declaration.MainProgram mainProgram)
    {
        SetGroup(o, Group.Main);
        Indent(o).Append("int main() ");
        _includes.Ensure(IncludeSet.StdLib); // for EXIT_SUCCESS
        return AppendBlock(o, mainProgram.Block, o => Indent(o.AppendLine()).AppendLine("return EXIT_SUCCESS;"))
           .AppendLine();
    }

    StringBuilder AppendCallableSignature(StringBuilder o, CallableSignature sig)
    {
        o.Append(Format.Code, $"{CreateTypeInfo(sig.Meta.Scope, sig.ReturnType)} {ValidateIdentifier(sig.Meta.Scope, sig.Name)}(");
        if (sig.Parameters.Count != 0) {
            o.AppendJoin(", ", sig.Parameters.Select(GenerateParameter));
        } else {
            o.Append("void");
        }
        o.Append(')');

        return o;
    }

    string GenerateParameter(ParameterFormal param)
    {
        StringBuilder o = new();
        var type = CreateTypeInfo(param.Meta.Scope, param.Type);
        if (C.RequiresPointer(param.Mode, param.Type)) {
            type = type.ToPointer(1);
        }

        o.Append(type.GenerateDeclaration(ValidateIdentifier(param.Meta.Scope, param.Name)));

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

        StringBuilder AppendAlternativeClause(StringBuilder o, string keyword, Expr condition, SemanticBlock block)
        {
            AppendParenExpr(o.Append(Format.Code, $"{keyword} "), condition);
            return AppendBlock(o.Append(' '), block);
        }
    }

    protected override StringBuilder AppendAssignment(StringBuilder o, Statement.Assignment assignment)
    {
        Indent(o);

        var valueType = assignment.Value.Value.Type;
        var targetType = assignment.Target.Value.Type;

        switch (valueType) {
        case LengthedStringType
            when targetType.IsConvertibleTo(StringType.Instance):
            _includes.Ensure(IncludeSet.String);
            AppendExpression(o.Append("strcpy("), assignment.Target);
            AppendExpression(o.Append(", "), assignment.Value).Append(')');
            break;
        case ArrayType when targetType is ArrayType:
            _includes.Ensure(IncludeSet.String);
            AppendExpression(o.Append("memcpy("), assignment.Target);
            AppendExpression(o.Append(", "), assignment.Value);
            AppendUnaryOperation(o.Append(", "), OpTable.SizeOf, assignment.Target, AppendExpression).Append(')');
            break;
        default:
            AppendExpression(o, assignment.Target);
            o.Append(" = ");
            AppendExpression(o, assignment.Value);
            break;
        }

        return o.AppendLine(";");
    }

    StringBuilder AppendBlock(StringBuilder o, SemanticBlock block, Action<StringBuilder>? suffix = null)
    {
        o.AppendLine("{");
        Indentation.Increase();
        AppendStatements(o, block);
        suffix?.Invoke(o);
        Indentation.Decrease();
        Indent(o).Append('}');

        return o;
    }

    protected override StringBuilder AppendDoWhileLoop(StringBuilder o, Statement.DoWhileLoop doWhileLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, doWhileLoop.Block).Append(" while ");
        AppendParenExpr(o, doWhileLoop.Condition);
        return o.AppendLine(";");
    }

    protected override StringBuilder AppendBuiltinEcrireEcran(StringBuilder o, Statement.Builtin.EcrireEcran ecrireEcran)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(ecrireEcran.Arguments);

        Indent(o).Append(Format.Code, $@"printf(""{format}\n""");

        foreach (Expr arg in arguments) {
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
                step => step is not Expr.Literal { UnderlyingValue: 1 })
           .Match(step => AppendExpression(o.Append(" += "), step),
                none: () => o.Append("++"));

        return AppendBlock(o.Append(") "), forLoop.Block).AppendLine();
    }

    protected override StringBuilder AppendBuiltinLireClavier(StringBuilder o, Statement.Builtin.LireClavier lireClavier)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(lireClavier.ArgumentVariable.Yield());

        Indent(o).Append(Format.Code, $@"scanf(""{format}""");

        foreach (Expr arg in arguments) {
            AppendExpression(o.Append(", &"), arg);
        }

        return o.AppendLine(");");
    }

    protected override StringBuilder AppendRepeatLoop(StringBuilder o, Statement.RepeatLoop repeatLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, repeatLoop.Block).Append(" while ");
        AppendParenExpr(o, repeatLoop.Condition.Invert());
        return o.AppendLine(";");
    }

    protected override StringBuilder AppendReturn(StringBuilder o, Statement.Return ret)
    {
        Indent(o).Append("return");
        ret.Value.Tap(rv => AppendExpression(o.Append(' '), rv));
        return o.AppendLine(";");
    }

    protected override StringBuilder AppendSwitch(StringBuilder o, Statement.Switch @switch)
    {
        Indent(o).Append("switch ");
        AppendParenExpr(o, @switch.Expression).AppendLine(" {");

        foreach (var @case in @switch.Cases) {
            switch (@case) {
            case Statement.Switch.Case.OfValue c: {
                Indent(o).Append("case ");
                AppendExpression(o, c.Value).AppendLine(":");
                break;
            }
            case Statement.Switch.Case.Default d: {
                Indent(o).AppendLine("default:");
                break;
            }
            default: throw @case.ToUnmatchedException();
            }

            if (@case.Block.Count != 0) {
                Indentation.Increase();
                AppendStatements(o, @case.Block);
                Indent(o).AppendLine("break;");
                Indentation.Decrease();
            }
        }

        return Indent(o).AppendLine("}");
    }

    protected override StringBuilder AppendLocalVariable(StringBuilder o, Statement.LocalVariable local)
    {
        Indent(o).Append(CreateTypeInfo(local.Meta.Scope, local.Decl.Type)
           .GenerateDeclaration(local.Decl.Names.Select(n => ValidateIdentifier(local.Meta.Scope, n))));
        local.Value.Tap(i => AppendInitializer(o.Append(" = "), i, false));
        return o.AppendLine(";");
    }

    StringBuilder AppendInitializer(
        StringBuilder o,
        Initializer initializer,
        bool convertInitToCompoundLiteral = true
    )
        => initializer switch {
            Expr left => AppendExpression(o, left,
                convertInitToCompoundLiteral: convertInitToCompoundLiteral),
            Initializer.Braced array => AppendBracedInitializer(o, array),
            _ => throw initializer.ToUnmatchedException(),
        };

    StringBuilder AppendBracedInitializer(StringBuilder o, Initializer.Braced initializer)
    {
        o.AppendLine("{");
        Indentation.Increase();
        foreach (var value in initializer.Items) {
            Indent(o);
            foreach (var d in value.Designators) {
                (d switch {
                    Designator.Array array => AppendIndex(o, array.Index.Expression),
                    Designator.Structure structure => o.Append(Format.Code, $".{structure.Component}"),
                    _ => throw d.ToUnmatchedException(),
                }).Append(" = ");
            }
            AppendInitializer(o, value.Value);
            o.AppendLine(",");
        }
        Indentation.Decrease();
        return Indent(o).Append('}');
    }

    protected override StringBuilder AppendWhileLoop(StringBuilder o, Statement.WhileLoop whileLoop)
    {
        Indent(o).Append("while ");
        AppendParenExpr(o, whileLoop.Condition);
        return AppendBlock(o.Append(' '), whileLoop.Block).AppendLine();
    }

    #endregion Statements

    #region Expressions

    StringBuilder AppendArraySubscript(StringBuilder o, Expr.Lvalue.ArraySubscript arrSub)
    {
        AppendExpression(o, arrSub.Array, OpTable.ShouldBracket(arrSub));
        AppendIndex(o, arrSub.Index);
        return o;
    }

    StringBuilder AppendIndex(StringBuilder o, Expr index)
        => AppendExpressionAlter(o.Append('['), index, OpTable.Subtract, 1, (l, r) => l - r).Append(']');

    StringBuilder AppendParenExpr(StringBuilder o, Expr expr)
        => AppendExpression(o, expr, expr is not ParenExpr);

    protected override StringBuilder AppendExpressionStatement(StringBuilder o, Statement.ExpressionStatement exprStmt)
        => AppendExpression(Indent(o), exprStmt.Expression).AppendLine(";");

    StringBuilder AppendExpression(StringBuilder o, Expr expr) => AppendExpression(o, expr, false);

    StringBuilder AppendExpression(
        StringBuilder o,
        Expr expr,
        // ReSharper disable once MethodOverloadWithOptionalParameter
        // Justification : method group calls
        bool bracket = false,
        bool convertInitToCompoundLiteral = true
    )
        => AppendBetweenParens(o, bracket, o => {
            _ = expr switch {
                ParenExpr b => AppendExpression(o, b.ContainedExpression, !bracket),
                Expr.Call call => AppendCall(o, call),
                Expr.Literal(_, bool v, BooleanValue) => AppendLiteralBoolean(v),
                // We're using the Pseudocode syntax for escaping string literals since it is the same as C's (except for \? which we can ignore, no one uses trigraphs)
                Expr.Literal { Value: CharacterValue v } => o.Append(v.ToString(Value.FmtNoType, Format.Code)),
                Expr.Literal { Value: StringValue v } => o.Append(v.ToString(Value.FmtNoType, Format.Code)),
                Expr.Literal l => o.Append(l.UnderlyingValue.ToStringFmt(Format.Code)),
                Expr.Lvalue.ArraySubscript arrSub => AppendArraySubscript(o, arrSub),
                Expr.Lvalue.ComponentAccess compAccess
                    => AppendExpression(o, compAccess.Structure, OpTable.ShouldBracket(compAccess)).Append('.').Append(compAccess.ComponentName),
                Expr.Lvalue.VariableReference variable => AppendVariableReference(o, variable, convertInitToCompoundLiteral),
                Expr.BinaryOperation opBin => AppendOperationBinary(o, opBin),
                Expr.UnaryOperation opUn => AppendOperationUnary(o, opUn),
                Expr.BuiltinFdf fdf => AppendBuiltinFdf(o, fdf),
                _ => throw expr.ToUnmatchedException(),
            };

            StringBuilder AppendLiteralBoolean(bool b)
            {
                _includes.Ensure(IncludeSet.StdBool);
                return o.Append(b ? "true" : "false");
            }
        });

    StringBuilder AppendVariableReference(StringBuilder o, Expr.Lvalue.VariableReference variable, bool convertInitToCompoundLiteral)
    {
        if (convertInitToCompoundLiteral
         && variable.Meta.Scope.GetSymbol<Symbol.Constant>(variable.Name) is { HasValue: true } s
         && s.Value.Type is ArrayType or StructureType) {
            o.Append(Format.Code, $"({CreateTypeInfo(variable.Meta.Scope, s.Value.Type)})");
        }
        return C.IsPointerParameter(variable)
            ? AppendUnaryOperation(o, OpTable.Dereference, variable,
                (o, v) => o.Append(ValidateIdentifier(v.Meta.Scope, v.Name)))
            : o.Append(ValidateIdentifier(variable.Meta.Scope, variable.Name));
    }

    StringBuilder AppendOperationBinary(StringBuilder o, Expr.BinaryOperation opBin)
    {
        if (opBin.Left.Value.Type.IsConvertibleTo(StringType.Instance)
         && opBin.Right.Value.Type.IsConvertibleTo(StringType.Instance)
         && IsStringComparisonOperator(opBin.Operator)) {
            _includes.Ensure(IncludeSet.String);
            return OpTable.Get(opBin).Append(o, TypeGeneratorFor(opBin.Meta.Scope), [
                o => AppendExpression(AppendExpression(o.Append("strcmp("), opBin.Left).Append(", "), opBin.Right).Append(')'),
                o => o.Append('0'),
            ]);
        }

        var (bracketLeft, bracketRight) = OpTable.ShouldBracketBinary(opBin);
        return OpTable.Get(opBin).Append(o, TypeGeneratorFor(opBin.Meta.Scope), [
            o => AppendExpression(o, opBin.Left, bracketLeft),
            o => AppendExpression(o, opBin.Right, bracketRight),
        ]);
    }

    static bool IsStringComparisonOperator(BinaryOperator b) => b
        is BinaryOperator.Equal
     or BinaryOperator.GreaterThan
     or BinaryOperator.GreaterThanOrEqual
     or BinaryOperator.LessThan
     or BinaryOperator.LessThanOrEqual
     or BinaryOperator.NotEqual;

    StringBuilder AppendOperationUnary(StringBuilder o, Expr.UnaryOperation opUn)
        => OpTable.Get(opUn).Append(o, TypeGeneratorFor(opUn.Meta.Scope), [
            o => AppendExpression(o, opUn.Operand, OpTable.ShouldBracketUnary(opUn)),
        ]);

    #endregion Expressions

    #region Helpers

    StringBuilder AppendCall(StringBuilder o, Expr.Call call)
    {
        const string ParameterSeparator = ", ";

        o.Append(Format.Code, $"{ValidateIdentifier(call.Meta.Scope, call.Callee)}(");
        foreach (var param in call.Parameters) {
            if (C.RequiresPointer(param.Mode, param.Value.Value.Type) && !C.IsPointerParameter(param.Value)) {
                AppendUnaryOperation(o, OpTable.AddressOf, param.Value, AppendExpression);
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

    static StringBuilder AppendFileHeader(StringBuilder o, Algorithm algorithm) => o.AppendLine(Format.Code, $"""
        /** @file
         * @brief {algorithm.Title}
         * @author {Environment.UserName}
         * @date {DateOnly.FromDateTime(DateTime.Now).ToString(Format.Date)}
         */

        """);

    TypeInfo CreateTypeInfo(Scope scope, EvaluatedType type)
    {
        var info = TypeInfo.Create(type, new(scope, Msger, KwTable,
            ToGenerator<Expr>(AppendExpression),
            ToGenerator<Expr>((o, e) => AppendExpressionAlter(o, e, OpTable.Add, 1, (l, r) => l + r))));
        foreach (string header in info.RequiredHeaders) {
            _includes.Ensure(header);
        }
        return info;
    }

    StringBuilder AppendExpressionAlter<T>(
        StringBuilder o,
        Expr left,
        OperatorInfo @operator,
        T right,
        Func<T, T, T>? collapseLiterals = null
    ) where T : notnull
    // Collapse literals
        => collapseLiterals is not null && GetExpressionObviousValue(left) is { HasValue: true } v
            ? o.Append(collapseLiterals((T)v.Value, right).ToStringFmt(Format.Code))
            : @operator.Append(o, TypeGeneratorFor(left.Meta.Scope), [
                o => AppendExpression(o, left, OpTable.ShouldBracketOperand(@operator, left)),
                o => o.Append(right.ToStringFmt(Format.Code)),
            ]);

    static ValueOption<object> GetExpressionObviousValue(Expr expr)
        => expr is Expr.Literal
            or Expr.UnaryOperation { Operator: UnaryOperator.Plus or UnaryOperator.Minus }
        && expr.Value.Status.ComptimeValue is { HasValue: true } v
            ? v
            : default;

    protected override TypeGenerator TypeGeneratorFor(Scope scope)
        => type => CreateTypeInfo(scope, type);

    StringBuilder Indent(StringBuilder o) => Indentation.Indent(o);

    StringBuilder SetGroup(StringBuilder o, Group newGroup)
    {
        if (_currentGroup != newGroup) {
            _currentGroup = newGroup;
            o.AppendLine();
        }
        return o;
    }

    protected override StringBuilder AppendBuiltinLire(StringBuilder o, Statement.Builtin.Lire lire)
        => FeatureComingSoon(o, lire, "files");
    protected override StringBuilder AppendBuiltinEcrire(StringBuilder o, Statement.Builtin.Ecrire ecrire)
        => FeatureComingSoon(o, ecrire, "files");
    protected override StringBuilder AppendBuiltinFermer(StringBuilder o, Statement.Builtin.Fermer fermer)
        => FeatureComingSoon(o, fermer, "files");
    protected override StringBuilder AppendBuiltinOuvrirLecture(StringBuilder o, Statement.Builtin.OuvrirLecture ouvrirLecture)
        => FeatureComingSoon(o, ouvrirLecture, "files");
    protected override StringBuilder AppendBuiltinOuvrirEcriture(StringBuilder o, Statement.Builtin.OuvrirEcriture ouvrirEcriture)
        => FeatureComingSoon(o, ouvrirEcriture, "files");
    protected override StringBuilder AppendBuiltinOuvrirAjout(StringBuilder o, Statement.Builtin.OuvrirAjout ouvrirAjout)
        => FeatureComingSoon(o, ouvrirAjout, "files");
    protected override StringBuilder AppendBuiltinAssigner(StringBuilder o, Statement.Builtin.Assigner assigner)
        => FeatureComingSoon(o, assigner, "files");
    StringBuilder AppendBuiltinFdf(StringBuilder o, Expr.BuiltinFdf fdf)
        => FeatureComingSoon(o, fdf, "files");

    StringBuilder FeatureComingSoon(StringBuilder o, SemanticNode node, string feature)
    {
        Msger.Report(Message.ErrorFeatureComingSoon(node.Meta.Location, feature));
        return o;
    }

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
