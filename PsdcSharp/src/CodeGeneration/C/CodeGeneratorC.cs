using System.Text;
using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

internal sealed partial class CodeGeneratorC(Messenger messenger, SemanticAst semanticAst)
    : CodeGenerator<OperatorInfoC>(messenger, semanticAst)
{
    private readonly IncludeSet _includes = new();

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

    private StringBuilder AppendDeclaration(StringBuilder o, ReadOnlyScope scope, Declaration decl) => decl switch {
        Declaration.TypeAlias alias => AppendAliasDeclaration(o, scope, alias),
        Declaration.Constant constant => AppendConstant(o, constant),
        Declaration.MainProgram mainProgram => AppendMainProgram(o, mainProgram),
        Declaration.Function func => AppendFunctionDeclaration(o, scope, func),
        Declaration.Procedure proc => AppendProcedureDeclaration(o, scope, proc),
        Declaration.FunctionDefinition funcDef => AppendFunctionDefinition(o, scope, funcDef),
        Declaration.ProcedureDefinition procDef => AppendProcedureDefinition(o, scope, procDef),
        _ => throw decl.ToUnmatchedException(),
    };

    private StringBuilder AppendAliasDeclaration(StringBuilder o, ReadOnlyScope scope, Declaration.TypeAlias alias)
     => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).Map(alias => {
         var type = CreateTypeInfo(alias.TargetType);
         return Indent(o.AppendLine()).AppendLine($"typedef {type.GenerateDeclaration(alias.Name.Yield())};");
     }).ValueOr(o);

    private StringBuilder AppendConstant(StringBuilder o, Declaration.Constant constant)
    {
        Indent(o).Append($"#define {constant.Name} ");
        return AppendExpression(o, constant.Value, GetPrecedence(constant.Value) > 1).AppendLine();
    }

    private StringBuilder AppendMainProgram(StringBuilder o, Declaration.MainProgram mainProgram)
    {
        Indent(o.AppendLine()).Append("int main() ");
        return AppendBlock(o, mainProgram,
            sb => Indent(sb.AppendLine()).AppendLine("return 0;"))
        .AppendLine();
    }

    private StringBuilder AppendFunctionDefinition(StringBuilder o, ReadOnlyScope scope, Declaration.FunctionDefinition funcDef)
    => scope.GetSymbol<Symbol.Function>(funcDef.Signature.Name).Map(sig => {
        AppendFunctionSignature(o.AppendLine(), sig);
        return AppendBlock(o.Append(' '), funcDef).AppendLine();
    }).ValueOr(o);

    private StringBuilder AppendProcedureDefinition(StringBuilder o, ReadOnlyScope scope, Declaration.ProcedureDefinition procDef)
    => scope.GetSymbol<Symbol.Procedure>(procDef.Signature.Name).Map(sig => {
        AppendProcedureSignature(o.AppendLine(), sig);
        return AppendBlock(o.Append(' '), procDef).AppendLine();
    }).ValueOr(o);

    private StringBuilder AppendFunctionDeclaration(StringBuilder o, ReadOnlyScope scope, Declaration.Function func)
     => scope.GetSymbol<Symbol.Function>(func.Signature.Name)
    .Map(sig => AppendFunctionSignature(o, sig).AppendLine(";")).ValueOr(o);

    private StringBuilder AppendProcedureDeclaration(StringBuilder o, ReadOnlyScope scope, Declaration.Procedure proc)
     => scope.GetSymbol<Symbol.Procedure>(proc.Signature.Name)
     .Map(sig => AppendProcedureSignature(o, sig).AppendLine(";")).ValueOr(o);

    private StringBuilder AppendFunctionSignature(StringBuilder o, Symbol.Function func)
     => AppendSignature(o, CreateTypeInfo(func.ReturnType).Generate(), func.Name, func.Parameters);

    private StringBuilder AppendProcedureSignature(StringBuilder o, Symbol.Procedure proc)
     => AppendSignature(o, "void", proc.Name, proc.Parameters);

    private StringBuilder AppendSignature(StringBuilder o, string cReturnType, Identifier name, IEnumerable<Symbol.Parameter> parameters)
    {
        o.Append($"{cReturnType} {name}(");
        if (parameters.Any()) {
            o.AppendJoin(", ", parameters.Select(GenerateParameter));
        } else {
            o.Append("void");
        }
        o.Append(')');

        return o;
    }

    private string GenerateParameter(Symbol.Parameter param)
    {
        StringBuilder o = new();
        var type = CreateTypeInfo(param.Type);
        if (RequiresPointer(param.Mode)) {
            type = type.ToPointer(1);
        }

        AppendVariableDeclaration(o, type, param.Name.Yield());

        return o.ToString();
    }

    #endregion Declarations

    #region Statements

    private StringBuilder AppendStatement(StringBuilder o, ReadOnlyScope scope, Statement stmt) => stmt switch {
        // don't generate Nop statements
        // their only usage statement in C is in braceless control structures, but we don't have them, so they serve no purpose.
        // In addition we make get them as a result of parsing errors.
        Statement.Nop => o,
        Statement.Alternative alt => AppendAlternative(o, alt),
        Statement.Builtin builtin => AppendBuiltin(o, builtin),
        Statement.Assignment assignment => AppendAssignment(o, assignment),
        Statement.DoWhileLoop doWhile => AppendDoWhileLoop(o, doWhile),
        Statement.ForLoop @for => AppendForLoop(o, @for),
        Statement.ProcedureCall call => AppendCall(Indent(o), call).AppendLine(";"),
        Statement.RepeatLoop repeat => AppendRepeatLoop(o, repeat),
        Statement.Return ret => AppendReturn(o, ret),
        Statement.Switch @switch => AppendSwitch(o, @switch),
        Statement.LocalVariable varDecl => AppendVariableDeclaration(o, varDecl, scope),
        Statement.WhileLoop whileLoop => AppendWhileLoop(o, whileLoop),
        _ => throw stmt.ToUnmatchedException(),
    };

    private StringBuilder AppendBuiltin(StringBuilder o, Statement.Builtin builtin) => builtin switch {
        Statement.Builtin.EcrireEcran ee => AppendEcrireEcran(o, ee),
        Statement.Builtin.LireClavier lc => AppendLireClavier(o, lc),
        _ => throw builtin.ToUnmatchedException(),
    };

    private StringBuilder AppendEcrireEcran(StringBuilder o, Statement.Builtin.EcrireEcran ecrireEcran)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(ecrireEcran.Arguments);

        Indent(o).Append($@"printf(""{format}\n""");

        foreach (Expression arg in arguments) {
            AppendExpression(o.Append(", "), arg);
        }

        return o.AppendLine(");");
    }

    private StringBuilder AppendLireClavier(StringBuilder o, Statement.Builtin.LireClavier lireClavier)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(lireClavier.ArgumentVariable.Yield());

        Indent(o).Append($@"scanf(""{format}""");

        foreach (Expression arg in arguments) {
            AppendExpression(o.Append(", &"), arg);
        }

        return o.AppendLine(");");
    }

    private StringBuilder AppendAlternative(StringBuilder o, Statement.Alternative alternative)
    {
        AppendAlternativeClause(Indent(o), "if", alternative.If.Condition, alternative.If);

        if (alternative.ElseIfs.Count == 0 && !alternative.Else.HasValue) {
            return o.AppendLine();
        }

        foreach (var elseIf in alternative.ElseIfs) {
            AppendAlternativeClause(o, " else if", elseIf.Condition, elseIf);
        }

        alternative.Else.MatchSome(elseBlock
         => AppendBlock(o.Append(" else "), elseBlock));

        o.AppendLine();

        return o;

        StringBuilder AppendAlternativeClause(StringBuilder o, string keyword, Expression condition, BlockNode node)
        {
            AppendBracketedExpression(o.Append($"{keyword} "), condition);
            return AppendBlock(o.Append(' '), node);
        }
    }

    private StringBuilder AppendBlock(StringBuilder o, BlockNode node, Action<StringBuilder>? suffix = null)
    {
        o.AppendLine("{");
        _indent.Increase();
        AppendBlockStatements(o, node);
        suffix?.Invoke(o);
        _indent.Decrease();
        Indent(o).Append('}');

        return o;
    }

    private StringBuilder AppendBlockStatements(StringBuilder o, BlockNode node)
    {
        foreach (Statement statement in node.Block) {
            AppendStatement(o, _ast.Scopes[node], statement);
        }

        return o;
    }

    private StringBuilder AppendAssignment(StringBuilder o, Statement.Assignment assignment)
    {
        Indent(o);
        AppendExpression(o, assignment.Target);
        o.Append(" = ");
        AppendExpression(o, assignment.Value);
        return o.AppendLine(";");
    }

    private StringBuilder AppendSwitch(StringBuilder o, Statement.Switch @switch)
    {
        Indent(o).Append("switch ");
        AppendBracketedExpression(o, @switch.Expression).AppendLine(" {");

        foreach (var @case in @switch.Cases) {
            Indent(o).Append("case ");
            AppendExpression(o, @case.When).AppendLine(":");
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

    private StringBuilder AppendVariableDeclaration(StringBuilder o, Statement.LocalVariable varDecl, ReadOnlyScope scope)
     => scope.GetSymbol<Symbol.Variable>(varDecl.Names.First()).Map(variable => {
         Indent(o);
         return AppendVariableDeclaration(o, CreateTypeInfo(variable.Type), varDecl.Names).AppendLine(";");
     }).ValueOr(o);

    private StringBuilder AppendVariableDeclaration(StringBuilder o, TypeInfoC type, IEnumerable<Identifier> names)
    {
        foreach (string header in type.RequiredHeaders) {
            _includes.Ensure(header);
        }
        o.Append(type.GenerateDeclaration(names));
        return o;
    }

    private StringBuilder AppendWhileLoop(StringBuilder o, Statement.WhileLoop whileLoop)
    {
        Indent(o).Append("while ");
        AppendBracketedExpression(o, whileLoop.Condition);
        return AppendBlock(o.Append(' '), whileLoop).AppendLine();
    }

    private StringBuilder AppendDoWhileLoop(StringBuilder o, Statement.DoWhileLoop doWhileLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, doWhileLoop).Append(" while ");
        AppendBracketedExpression(o, doWhileLoop.Condition);
        return o.AppendLine(";");
    }

    private StringBuilder AppendRepeatLoop(StringBuilder o, Statement.RepeatLoop repeatLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, repeatLoop).Append(" while (!");
        AppendBracketedExpression(o, repeatLoop.Condition);
        return o.AppendLine(");");
    }

    private StringBuilder AppendForLoop(StringBuilder o, Statement.ForLoop forLoop)
    {
        Indent(o);
        AppendExpression(o.Append("for ("), forLoop.Variant).Append(" = ");
        AppendExpression(o, forLoop.Start);

        AppendExpression(o.Append("; "), forLoop.Variant).Append(" <= ");
        AppendExpression(o, forLoop.End);

        AppendExpression(o.Append("; "), forLoop.Variant);
        forLoop.Step.Match(
            step => AppendExpression(o.Append(" += "), step),
            none: () => o.Append("++"));

        return AppendBlock(o.Append(") "), forLoop).AppendLine();
    }

    private StringBuilder AppendReturn(StringBuilder o, Statement.Return ret)
    {
        Indent(o).Append($"return ");
        AppendExpression(o, ret.Value);
        return o.AppendLine(";");
    }

    #endregion Statements

    #region Expressions

    private StringBuilder AppendBracketedExpression(StringBuilder o, Expression expr)
     => AppendExpression(o, expr, expr is not NodeBracketedExpression);

    private StringBuilder AppendExpression(StringBuilder o, Expression expr, bool bracketed = false)
    {
        if (bracketed) {
            o.Append('(');
        }
        _ = expr switch {
            Expression.Bracketed b => AppendExpression(o, b.ContainedExpression, !bracketed),
            Expression.FunctionCall call => AppendCall(o, call),
            Expression.Literal.Character litChar => o.Append($"'{FormatValue(litChar.Value)}'"),
            Expression.Literal.False l => AppendLiteralBoolean(l),
            Expression.Literal.String litStr => o.Append($"\"{FormatValue(litStr.Value)}\""),
            Expression.Literal.True l => AppendLiteralBoolean(l),
            Expression.Literal literal => o.Append(FormatValue(literal.Value)),
            Expression.Lvalue.ArraySubscript arrSub => AppendArraySubscript(o, arrSub),
            Expression.Lvalue.Bracketed b => AppendExpression(o, b.ContainedLvalue, !bracketed),
            Expression.Lvalue.ComponentAccess compAccess
             => AppendExpression(o, compAccess.Structure, ShouldBracket(compAccess)).Append('.').Append(compAccess.ComponentName),
            Expression.Lvalue.VariableReference variable => o.Append(variable.Name),
            Expression.BinaryOperation opBin => AppendOperationBinary(o, opBin),
            Expression.UnaryOperation opUn => AppendOperationUnary(o, opUn),
            _ => throw expr.ToUnmatchedException(),
        };
        if (bracketed) {
            o.Append(')');
        }
        return o;

        StringBuilder AppendLiteralBoolean(Expression.Literal l)
        {
            _includes.Ensure(IncludeSet.StdBool);
            return o.Append(FormatValue(l.Value));
        }
    }

    private StringBuilder AppendArraySubscript(StringBuilder o, Expression.Lvalue.ArraySubscript arrSub)
    {
        AppendExpression(o, arrSub.Array, ShouldBracket(arrSub));

        foreach (Expression index in arrSub.Indexes) {
            AppendExpression(o.Append('['), index.Alter(BinaryOperator.Minus, 1)).Append(']');
        }

        return o;
    }

    private StringBuilder AppendOperationBinary(StringBuilder o, Expression.BinaryOperation opBin)
    {
        var (bracketLeft, bracketRight) = ShouldBracket(opBin);
        AppendExpression(o, opBin.Left, bracketLeft);
        o.Append($" {OperatorInfoC.Get(opBin.Operator).Code} ");
        return AppendExpression(o, opBin.Right, bracketRight);
    }

    private StringBuilder AppendOperationUnary(StringBuilder o, Expression.UnaryOperation opUn)
    {
        o.Append(OperatorInfoC.Get(opUn.Operator).Code);
        return AppendExpression(o, opUn.Operand, ShouldBracket(opUn));
    }

    #endregion Expressions

    #region Helpers

    private StringBuilder AppendFileHeader(StringBuilder o) => o.AppendLine($"""
        /** @file
         * @brief {_ast.Root.Name}
         * @author {Environment.UserName}
         * @date {DateOnly.FromDateTime(DateTime.Now)}
         */
        """);

    private StringBuilder AppendCall(StringBuilder o, NodeCall call)
    {
        const string ParameterSeparator = ", ";

        o.Append($"{call.Name}(");
        foreach (var param in call.Parameters) {
            if (RequiresPointer(param.Mode)) {
                o.Append('&');
            }
            AppendExpression(o, param.Value).Append(ParameterSeparator);
        }
        if (call.Parameters.Count > 0) {
            o.Length -= ParameterSeparator.Length; // Remove unnecessary last separator
        }
        return o.Append(')');
    }

    private TypeInfoC CreateTypeInfo(EvaluatedType evaluatedType)
     => TypeInfoC.Create(evaluatedType, expr => AppendExpression(new(), expr).ToString());

    private StringBuilder Indent(StringBuilder o) => _indent.Indent(o);

    private static bool RequiresPointer(ParameterMode mode) => mode != ParameterMode.In;

    #endregion Helpers
}
