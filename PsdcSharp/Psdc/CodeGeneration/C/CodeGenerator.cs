using System.Globalization;
using System.Text;

using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

sealed partial class CodeGenerator(Messenger messenger, SemanticAst ast)
    : CodeGenerator<KeywordTable, OperatorTable>(messenger, ast, KeywordTable.Instance, OperatorTable.Instance)
{
    readonly IncludeSet _includes = new();
    Group _currentGroup = Group.None;

    public override string Generate()
    {
        StringBuilder o = new();

        foreach (Declaration declaration in _ast.Root.Declarations) {
            AppendDeclaration(o, _ast.Scopes[_ast.Root], declaration);
        }

        return _includes.AppendIncludeSection(AppendFileHeader(new()))
            .Append(o).ToString();
    }

    #region Declarations

    StringBuilder AppendAliasDeclaration(StringBuilder o, ReadOnlyScope scope, Declaration.TypeAlias alias)
     => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).Map(alias => {
         SetGroup(o, Group.Types);
         var type = CreateTypeInfo(scope, alias.TargetType);
         return Indent(o).AppendLine($"typedef {type.GenerateDeclaration(ValidateIdentifier(scope, alias.Name).Yield())};");
     }).ValueOr(o);

    StringBuilder AppendConstant(StringBuilder o, ReadOnlyScope scope, Declaration.Constant constant)
    {
        SetGroup(o, Group.Macros);
        Indent(o).Append($"#define {ValidateIdentifier(scope, constant.Name)} ");
        return AppendExpression(o, scope, constant.Value, _opTable.ShouldBracketUnary(scope, constant.Value, _opTable.None)).AppendLine();
    }

    StringBuilder AppendDeclaration(StringBuilder o, ReadOnlyScope scope, Declaration decl) => decl switch {
        Declaration.TypeAlias alias => AppendAliasDeclaration(o, scope, alias),
        Declaration.Constant constant => AppendConstant(o, scope, constant),
        Declaration.MainProgram mainProgram => AppendMainProgram(o, mainProgram),
        Declaration.Function func => AppendFunctionDeclaration(o, scope, func),
        Declaration.Procedure proc => AppendProcedureDeclaration(o, scope, proc),
        Declaration.FunctionDefinition funcDef => AppendFunctionDefinition(o, scope, funcDef),
        Declaration.ProcedureDefinition procDef => AppendProcedureDefinition(o, scope, procDef),
        _ => throw decl.ToUnmatchedException(),
    };

    StringBuilder AppendFunctionDeclaration(StringBuilder o, ReadOnlyScope scope, Declaration.Function func)
     => scope.GetSymbol<Symbol.Function>(func.Signature.Name)
    .Map(sig => AppendFunctionSignature(SetGroup(o, Group.Prototypes), scope, sig).AppendLine(";")).ValueOr(o);

    StringBuilder AppendFunctionDefinition(StringBuilder o, ReadOnlyScope scope, Declaration.FunctionDefinition funcDef)
    => scope.GetSymbol<Symbol.Function>(funcDef.Signature.Name).Map(sig => {
        AppendFunctionSignature(o.AppendLine(), scope, sig);
        return AppendBlock(o.Append(' '), funcDef).AppendLine();
    }).ValueOr(o);

    StringBuilder AppendFunctionSignature(StringBuilder o, ReadOnlyScope scope, Symbol.Function func)
     => AppendSignature(o, scope, CreateTypeInfo(scope, func.ReturnType).Generate(), func.Name, func.Parameters);

    StringBuilder AppendMainProgram(StringBuilder o, Declaration.MainProgram mainProgram)
    {
        SetGroup(o, Group.Main);
        Indent(o).Append("int main() ");
        return AppendBlock(o, mainProgram,
            sb => Indent(sb.AppendLine()).AppendLine("return 0;"))
        .AppendLine();
    }

    StringBuilder AppendProcedureDeclaration(StringBuilder o, ReadOnlyScope scope, Declaration.Procedure proc)
     => scope.GetSymbol<Symbol.Procedure>(proc.Signature.Name)
     .Map(sig => AppendProcedureSignature(SetGroup(o, Group.Prototypes), scope, sig).AppendLine(";")).ValueOr(o);

    StringBuilder AppendProcedureDefinition(StringBuilder o, ReadOnlyScope scope, Declaration.ProcedureDefinition procDef)
        => scope.GetSymbol<Symbol.Procedure>(procDef.Signature.Name).Map(sig => {
            AppendProcedureSignature(o.AppendLine(), scope, sig);
            return AppendBlock(o.Append(' '), procDef).AppendLine();
        }).ValueOr(o);

    StringBuilder AppendProcedureSignature(StringBuilder o, ReadOnlyScope scope, Symbol.Procedure proc)
     => AppendSignature(o, scope, "void", proc.Name, proc.Parameters);

    StringBuilder AppendSignature(StringBuilder o, ReadOnlyScope scope, string cReturnType, Identifier name, IEnumerable<Symbol.Parameter> parameters)
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

    string GenerateParameter(ReadOnlyScope scope, Symbol.Parameter param)
    {
        StringBuilder o = new();
        var type = CreateTypeInfo(scope, param.Type);
        if (param.Mode.RequiresPointer()) {
            type = type.ToPointer(1);
        }

        AppendVariableDeclaration(o, scope, type, param.Name.Yield());

        return o.ToString();
    }

    #endregion Declarations

    #region Statements

    StringBuilder AppendAlternative(StringBuilder o, ReadOnlyScope scope, Statement.Alternative alternative)
    {
        AppendAlternativeClause(Indent(o), scope, "if", alternative.If.Condition, alternative.If);

        if (alternative.ElseIfs.Count == 0 && !alternative.Else.HasValue) {
            return o.AppendLine();
        }

        foreach (var elseIf in alternative.ElseIfs) {
            AppendAlternativeClause(o, scope, " else if", elseIf.Condition, elseIf);
        }

        alternative.Else.MatchSome(elseBlock
         => AppendBlock(o.Append(" else "), elseBlock));

        o.AppendLine();

        return o;

        StringBuilder AppendAlternativeClause(StringBuilder o, ReadOnlyScope scope, string keyword, Expression condition, BlockNode node)
        {
            AppendBracketedExpression(o.Append($"{keyword} "), scope, condition);
            return AppendBlock(o.Append(' '), node);
        }
    }

    StringBuilder AppendAssignment(StringBuilder o, ReadOnlyScope scope, Statement.Assignment assignment)
    {
        Indent(o);

        if (_ast.InferredTypes[assignment.Target].IsConvertibleTo(StringType.Instance)
         && _ast.InferredTypes[assignment.Value].IsConvertibleTo(StringType.Instance)) {
            _includes.Ensure(IncludeSet.String);
            AppendExpression(o.Append("strcpy("), scope, assignment.Target);
            AppendExpression(o.Append(", "), scope, assignment.Value).Append(')');
        } else {
            AppendExpression(o, scope, assignment.Target);
            o.Append(" = ");
            AppendExpression(o, scope, assignment.Value);
        }

        return o.AppendLine(";");
    }

    StringBuilder AppendBlock(StringBuilder o, BlockNode node, Action<StringBuilder>? suffix = null)
    {
        o.AppendLine("{");
        _indent.Increase();
        AppendBlockStatements(o, node);
        suffix?.Invoke(o);
        _indent.Decrease();
        Indent(o).Append('}');

        return o;
    }

    StringBuilder AppendBlockStatements(StringBuilder o, BlockNode node)
    {
        foreach (Statement statement in node.Block) {
            AppendStatement(o, _ast.Scopes[node], statement);
        }

        return o;
    }

    StringBuilder AppendBuiltin(StringBuilder o, ReadOnlyScope scope, Statement.Builtin builtin) => builtin switch {
        Statement.Builtin.EcrireEcran ee => AppendEcrireEcran(o, scope, ee),
        Statement.Builtin.LireClavier lc => AppendLireClavier(o, scope, lc),
        _ => throw builtin.ToUnmatchedException(),
    };

    StringBuilder AppendDoWhileLoop(StringBuilder o, ReadOnlyScope scope, Statement.DoWhileLoop doWhileLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, doWhileLoop).Append(" while ");
        AppendBracketedExpression(o, scope, doWhileLoop.Condition);
        return o.AppendLine(";");
    }

    StringBuilder AppendEcrireEcran(StringBuilder o, ReadOnlyScope scope, Statement.Builtin.EcrireEcran ecrireEcran)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(scope, ecrireEcran.Arguments);

        Indent(o).Append($@"printf(""{format}\n""");

        foreach (Expression arg in arguments) {
            AppendExpression(o.Append(", "), scope, arg);
        }

        return o.AppendLine(");");
    }

    StringBuilder AppendForLoop(StringBuilder o, ReadOnlyScope scope, Statement.ForLoop forLoop)
    {
        Indent(o);
        AppendExpression(o.Append("for ("), scope, forLoop.Variant).Append(" = ");
        AppendExpression(o, scope, forLoop.Start);

        AppendExpression(o.Append("; "), scope, forLoop.Variant).Append(" <= ");
        AppendExpression(o, scope, forLoop.End);

        AppendExpression(o.Append("; "), scope, forLoop.Variant);
        forLoop.Step.When(
            // replace += by ++ when the step is a literal 1
            step => step is not Expression.Literal.Integer { Value: 1 })
            .Match(step => AppendExpression(o.Append(" += "), scope, step),
                   none: () => o.Append("++"));

        return AppendBlock(o.Append(") "), forLoop).AppendLine();
    }

    StringBuilder AppendLireClavier(StringBuilder o, ReadOnlyScope scope, Statement.Builtin.LireClavier lireClavier)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(scope, lireClavier.ArgumentVariable.Yield());

        Indent(o).Append($@"scanf(""{format}""");

        foreach (Expression arg in arguments) {
            AppendExpression(o.Append(", &"), scope, arg);
        }

        return o.AppendLine(");");
    }

    StringBuilder AppendRepeatLoop(StringBuilder o, ReadOnlyScope scope, Statement.RepeatLoop repeatLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, repeatLoop).Append(" while ");
        AppendBracketedExpression(o, scope, _ast.Invert(repeatLoop.Condition));
        return o.AppendLine(";");
    }

    StringBuilder AppendReturn(StringBuilder o, ReadOnlyScope scope, Statement.Return ret)
    {
        Indent(o).Append($"return ");
        AppendExpression(o, scope, ret.Value);
        return o.AppendLine(";");
    }

    StringBuilder AppendStatement(StringBuilder o, ReadOnlyScope scope, Statement stmt) => stmt switch {
        // don't generate Nop statements
        // their only usage statement in C is in braceless control structures, but we don't have them, so they serve no purpose.
        // In addition we make get them as a result of parsing errors.
        Statement.Nop => o,
        Statement.Alternative alt => AppendAlternative(o, scope, alt),
        Statement.Builtin builtin => AppendBuiltin(o, scope, builtin),
        Statement.Assignment assignment => AppendAssignment(o, scope, assignment),
        Statement.DoWhileLoop doWhile => AppendDoWhileLoop(o, scope, doWhile),
        Statement.ForLoop @for => AppendForLoop(o, scope, @for),
        Statement.ProcedureCall call => AppendCall(Indent(o), scope, call).AppendLine(";"),
        Statement.RepeatLoop repeat => AppendRepeatLoop(o, scope, repeat),
        Statement.Return ret => AppendReturn(o, scope, ret),
        Statement.Switch @switch => AppendSwitch(o, scope, @switch),
        Statement.LocalVariable varDecl => AppendVariableDeclaration(o, scope, varDecl),
        Statement.WhileLoop whileLoop => AppendWhileLoop(o, scope, whileLoop),
        _ => throw stmt.ToUnmatchedException(),
    };

    StringBuilder AppendSwitch(StringBuilder o, ReadOnlyScope scope, Statement.Switch @switch)
    {
        Indent(o).Append("switch ");
        AppendBracketedExpression(o, scope, @switch.Expression).AppendLine(" {");

        foreach (var @case in @switch.Cases) {
            Indent(o).Append("case ");
            AppendExpression(o, scope, @case.When).AppendLine(":");
            _indent.Increase();
            AppendBlockStatements(o, @case);
            Indent(o).AppendLine("break;");
            _indent.Decrease();
        }

        @switch.Default.MatchSome(@default => {
            Indent(o).AppendLine("default:");
            _indent.Increase();
            AppendBlockStatements(o, @default);
            _indent.Decrease();
        });

        return Indent(o).AppendLine("}");
    }

    StringBuilder AppendVariableDeclaration(StringBuilder o, ReadOnlyScope scope, Statement.LocalVariable varDecl)
     => scope.GetSymbol<Symbol.Variable>(varDecl.Binding.Names.First()).Map(variable => {
         Indent(o);
         AppendVariableDeclaration(o, scope, CreateTypeInfo(scope, variable.Type), varDecl.Binding.Names);
         varDecl.Initializer.MatchSome(init => AppendInitializer(o.Append(" = "), scope, init));
         return o.AppendLine(";");
     }).ValueOr(o);

    StringBuilder AppendInitializer(StringBuilder o, ReadOnlyScope scope, Initializer initializer) => initializer switch {
        Expression expr => AppendExpression(o, scope, expr),
        Initializer.Braced array => AppendBracedInitializer(o, scope, array),
        _ => throw initializer.ToUnmatchedException(),
    };

    StringBuilder AppendBracedInitializer(StringBuilder o, ReadOnlyScope scope, Initializer.Braced initializer)
    {
        o.AppendLine("{");
        _indent.Increase();
        foreach (var value in initializer.Values) {
            Indent(o);
            value.Designator.MatchSome(d => (d switch {
                Designator.Array array => AppendIndex(o, scope, array.Index),
                Designator.Structure structure => o.Append($".{structure.Component}"),
                _ => throw d.ToUnmatchedException(),
            }).Append(" = "));
            AppendInitializer(o, scope, value.Initializer);
            o.AppendLine(",");
        }
        _indent.Decrease();
        return Indent(o).Append('}');
    }

    StringBuilder AppendVariableDeclaration(StringBuilder o, ReadOnlyScope scope, TypeInfo type, IEnumerable<Identifier> names)
    {
        foreach (string header in type.RequiredHeaders) {
            _includes.Ensure(header);
        }
        return o.Append(type.GenerateDeclaration(names.Select(i => ValidateIdentifier(scope, i))));
    }

    StringBuilder AppendWhileLoop(StringBuilder o, ReadOnlyScope scope, Statement.WhileLoop whileLoop)
    {
        Indent(o).Append("while ");
        AppendBracketedExpression(o, scope, whileLoop.Condition);
        return AppendBlock(o.Append(' '), whileLoop).AppendLine();
    }

    #endregion Statements

    #region Expressions

    StringBuilder AppendArraySubscript(StringBuilder o, ReadOnlyScope scope, Expression.Lvalue.ArraySubscript arrSub)
    {
        AppendExpression(o, scope, arrSub.Array, _opTable.ShouldBracket(scope, arrSub));
        AppendIndex(o, scope, arrSub.Index);
        return o;
    }

    StringBuilder AppendIndex(StringBuilder o, ReadOnlyScope scope, IReadOnlyList<Expression> index)
    {
        foreach (Expression i in index) {
            var (expression, messages) = _ast.Alter(i, BinaryOperator.Subtract, 1);
            _msger.ReportAll(messages);
            AppendExpression(o.Append('['), scope, expression).Append(']');
        }
        return o;
    }

    StringBuilder AppendBracketedExpression(StringBuilder o, ReadOnlyScope scope, Expression expr)
     => AppendExpression(o, scope, expr, expr is not BracketedExpressionNode);

    StringBuilder AppendExpression(StringBuilder o, ReadOnlyScope scope, Expression expr, bool bracket = false) => AppendBracketed(o, bracket, o => {
        _ = expr switch {
            BracketedExpressionNode b => AppendExpression(o, scope, b.ContainedExpression, !bracket),
            Expression.FunctionCall call => AppendCall(o, scope, call),
            Expression.Literal.Character litChar => o.Append($"'{litChar.Value.ToString(CultureInfo.InvariantCulture)}'"),
            Expression.Literal.False l => AppendLiteralBoolean(l.Value),
            Expression.Literal.String litStr => o.Append($"\"{litStr.Value.ToString(CultureInfo.InvariantCulture)}\""),
            Expression.Literal.True l => AppendLiteralBoolean(l.Value),
            Expression.Literal literal => o.Append(literal.Value.ToString(CultureInfo.InvariantCulture)),
            Expression.Lvalue.ArraySubscript arrSub => AppendArraySubscript(o, scope, arrSub),
            Expression.Lvalue.ComponentAccess compAccess
             => AppendExpression(o, scope, compAccess.Structure, _opTable.ShouldBracket(scope, compAccess)).Append('.').Append(compAccess.ComponentName),
            Expression.Lvalue.VariableReference variable => AppendVariableReference(o, scope, variable),
            Expression.BinaryOperation opBin => AppendOperationBinary(o, scope, opBin),
            Expression.UnaryOperation opUn => AppendOperationUnary(o, scope, opUn),
            _ => throw expr.ToUnmatchedException(),
        };

        StringBuilder AppendLiteralBoolean(bool b)
        {
            _includes.Ensure(IncludeSet.StdBool);
            return o.Append(b ? "true" : "false");
        }
    });

    StringBuilder AppendVariableReference(StringBuilder o, ReadOnlyScope scope, Expression.Lvalue.VariableReference variable)
     => scope.IsPointer(variable)
        ? AppendUnaryPrefixOperation(o, scope, _opTable.Dereference, variable,
                                    (o, _, v) => o.Append(ValidateIdentifier(scope, v.Name)))
        : o.Append(ValidateIdentifier(scope, variable.Name));

    StringBuilder AppendOperationBinary(StringBuilder o, ReadOnlyScope scope, Expression.BinaryOperation opBin)
    {
        if (_ast.InferredTypes[opBin.Left].IsConvertibleTo(StringType.Instance)
         && _ast.InferredTypes[opBin.Right].IsConvertibleTo(StringType.Instance)
         && IsStringComparisonOperator(opBin.Operator)) {
            _includes.Ensure(IncludeSet.String);
            AppendExpression(o.Append("strcmp("), scope, opBin.Left);
            AppendExpression(o.Append(", "), scope, opBin.Right);
            return o.Append($") {_opTable.Get(opBin.Operator).Code} 0");
        }

        var (bracketLeft, bracketRight) = _opTable.ShouldBracket(scope, opBin);
        AppendExpression(o, scope, opBin.Left, bracketLeft);
        o.Append($" {_opTable.Get(opBin.Operator).Code} ");
        return AppendExpression(o, scope, opBin.Right, bracketRight);
    }

    private static bool IsStringComparisonOperator(BinaryOperator b) => b
        is BinaryOperator.Equal
        or BinaryOperator.GreaterThan
        or BinaryOperator.GreaterThanOrEqual
        or BinaryOperator.LessThan
        or BinaryOperator.LessThanOrEqual
        or BinaryOperator.NotEqual;

    StringBuilder AppendOperationUnary(StringBuilder o, ReadOnlyScope scope, Expression.UnaryOperation opUn)
    {
        o.Append(_opTable.Get(opUn.Operator).Code);
        return AppendExpression(o, scope, opUn.Operand, _opTable.ShouldBracket(scope, opUn));
    }

    #endregion Expressions

    #region Helpers

    StringBuilder AppendCall(StringBuilder o, ReadOnlyScope scope, CallNode call)
    {
        const string ParameterSeparator = ", ";

        o.Append($"{ValidateIdentifier(scope, call.Name)}(");
        foreach (var param in call.Parameters) {
            if (param.Mode.RequiresPointer() && !scope.IsPointer(param.Value)) {
                AppendUnaryPrefixOperation(o, scope, _opTable.AddressOf, param.Value,
                                           (o, s, e) => AppendExpression(o, s, e));
            } else {
                AppendExpression(o, scope, param.Value);
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
         * @brief {_ast.Root.Name}
         * @author {Environment.UserName}
         * @date {DateOnly.FromDateTime(DateTime.Now)}
         */

        """);

    TypeInfo CreateTypeInfo(ReadOnlyScope scope, EvaluatedType evaluatedType)
     => TypeInfo.Create(_ast, scope, evaluatedType, _msger, expr => AppendExpression(new(), scope, expr).ToString(), _kwTable);

    StringBuilder Indent(StringBuilder o) => _indent.Indent(o);

    StringBuilder SetGroup(StringBuilder o, Group newGroup)
    {
        if (_currentGroup != newGroup) {
            _currentGroup = newGroup;
            o.AppendLine();
        }
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
