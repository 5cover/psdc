using System.Text;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal sealed partial class CodeGeneratorC(Messenger messenger, SemanticAst semanticAst) : CodeGenerator(messenger, semanticAst)
{
    private readonly IncludeSet _includes = new();

    public override string Generate()
    {
        StringBuilder o = new();

        foreach (Node.Declaration declaration in _ast.Root.Declarations) {
            AppendDeclaration(o, _ast.Scopes[_ast.Root], declaration);
        }

        return _includes.AppendIncludeSection(AppendFileHeader(new()))
            .Append(o).ToString();
    }

    #region Declarations

    private StringBuilder AppendDeclaration(StringBuilder o, ReadOnlyScope scope, Node.Declaration decl) => decl switch {
        Node.Declaration.TypeAlias alias => AppendAliasDeclaration(o, scope, alias),
        Node.Declaration.Constant constant => AppendConstant(o, constant),
        Node.Declaration.MainProgram mainProgram => AppendMainProgram(o, mainProgram),
        Node.Declaration.Function func => AppendFunctionDeclaration(o, scope, func),
        Node.Declaration.Procedure proc => AppendProcedureDeclaration(o, scope, proc),
        Node.Declaration.FunctionDefinition funcDef => AppendFunctionDefinition(o, scope, funcDef),
        Node.Declaration.ProcedureDefinition procDef => AppendProcedureDefinition(o, scope, procDef),
        _ => throw decl.ToUnmatchedException(),
    };

    private StringBuilder AppendAliasDeclaration(StringBuilder o, ReadOnlyScope scope, Node.Declaration.TypeAlias alias)
     => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).Map(alias => {
        var type = CreateTypeInfo(alias.TargetType);
        return Indent(o).AppendLine($"typedef {type.GenerateDeclaration(alias.Name.Yield())};");
    }).ValueOr(o);

    private StringBuilder AppendConstant(StringBuilder o, Node.Declaration.Constant constant)
    {
        Indent(o).Append($"#define {constant.Name} ");
        return AppendExpression(o, constant.Value, PrecedenceRequiresBrackets(constant.Value)).AppendLine();
    }

    private StringBuilder AppendMainProgram(StringBuilder o, Node.Declaration.MainProgram mainProgram)
    {
        o.AppendLine().Append("int main() ");

        return AppendBlock(o, mainProgram,
            sb => Indent(sb).AppendLine("return 0;"))
        .AppendLine();
    }

    private StringBuilder AppendFunctionDefinition(StringBuilder o, ReadOnlyScope scope, Node.Declaration.FunctionDefinition funcDef)
    => scope.GetSymbol<Symbol.Function>(funcDef.Signature.Name).Map(sig => {
        AppendFunctionSignature(o.AppendLine(), sig);
        return AppendBlock(o.Append(' '), funcDef).AppendLine();
    }).ValueOr(o);

    private StringBuilder AppendProcedureDefinition(StringBuilder o, ReadOnlyScope scope, Node.Declaration.ProcedureDefinition procDef)
    => scope.GetSymbol<Symbol.Procedure>(procDef.Signature.Name).Map(sig => {
        AppendProcedureSignature(o.AppendLine(), sig);
        return AppendBlock(o.Append(' '), procDef).AppendLine();
    }).ValueOr(o);

    private StringBuilder AppendFunctionDeclaration(StringBuilder o, ReadOnlyScope scope, Node.Declaration.Function func)
     => scope.GetSymbol<Symbol.Function>(func.Signature.Name)
    .Map(sig => AppendFunctionSignature(o, sig).AppendLine(";")).ValueOr(o);

    private StringBuilder AppendProcedureDeclaration(StringBuilder o, ReadOnlyScope scope, Node.Declaration.Procedure proc)
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

    private StringBuilder AppendStatement(StringBuilder o, ReadOnlyScope scope, Node.Statement stmt) => stmt switch {
        // don't generate Nop statements
        // their only usage statement in C is in braceless control structures, but we don't have them, so they serve no purpose.
        // In addition we make get them as a result of parsing errors.
        Node.Statement.Nop => o,
        Node.Statement.Alternative alternative => AppendAlternative(o, alternative),
        Node.Statement.Assignment assignment => AppendAssignment(o, assignment),
        Node.Statement.DoWhileLoop doWhileLoop => AppendDoWhileLoop(o, doWhileLoop),
        Node.Statement.BuiltinEcrireEcran ecrireEcran => AppendEcrireEcran(o, scope, ecrireEcran),
        Node.Statement.ForLoop forLoop => AppendForLoop(o, forLoop),
        Node.Statement.BuiltinLireClavier lireClavier => AppendLireClavier(o, scope, lireClavier),
        Node.Statement.ProcedureCall call => AppendCall(Indent(o), call).AppendLine(";"),
        Node.Statement.RepeatLoop repeatLoop => AppendRepeatLoop(o, repeatLoop),
        Node.Statement.Return ret => AppendReturn(o, ret),
        Node.Statement.Switch @switch => AppendSwitch(o, @switch),
        Node.Statement.LocalVariable varDecl => AppendVariableDeclaration(o, scope, varDecl),
        Node.Statement.WhileLoop whileLoop => AppendWhileLoop(o, whileLoop),
        _ => throw stmt.ToUnmatchedException(),
    };

    private StringBuilder AppendEcrireEcran(StringBuilder o, ReadOnlyScope scope, Node.Statement.BuiltinEcrireEcran ecrireEcran)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(scope, ecrireEcran.Arguments);

        Indent(o).Append($@"printf(""{format}\n""");

        foreach (Node.Expression arg in arguments) {
            AppendExpression(o.Append(", "), arg);
        }

        return o.AppendLine(");");
    }

    private StringBuilder AppendLireClavier(StringBuilder o, ReadOnlyScope scope, Node.Statement.BuiltinLireClavier lireClavier)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(scope, lireClavier.ArgumentVariable.Yield());

        Indent(o).Append($@"scanf(""{format}""");

        foreach (Node.Expression arg in arguments) {
            AppendExpression(o.Append(", &"), arg);
        }

        return o.AppendLine(");");
    }

    private StringBuilder AppendAlternative(StringBuilder o, Node.Statement.Alternative alternative)
    {
        AppendAlternativeClause(Indent(o), "if", alternative.If.Condition, alternative.If);

        if (alternative.ElseIfs.Count == 0 && !alternative.Else.HasValue) {
            return o.AppendLine();
        }

        foreach (var elseIf in alternative.ElseIfs) {
            AppendAlternativeClause(o, " else if", elseIf.Condition, elseIf);
        }

        alternative.Else.MatchSome(elseBlock
         => AppendBlock(o.Append(" else "), elseBlock).AppendLine());

        return o;

        StringBuilder AppendAlternativeClause(StringBuilder o, string keyword, Node.Expression condition, BlockNode node)
        {
            AppendExpression(o.Append($"{keyword} ("), condition).Append(") ");
            return AppendBlock(o, node);
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
        foreach (Node.Statement statement in node.Block) {
            AppendStatement(o, _ast.Scopes[node], statement);
        }

        return o;
    }

    private StringBuilder AppendAssignment(StringBuilder o, Node.Statement.Assignment assignment)
    {
        Indent(o);
        AppendExpression(o, assignment.Target);
        o.Append(" = ");
        AppendExpression(o, assignment.Value);
        return o.AppendLine(";");
    }

    private StringBuilder AppendSwitch(StringBuilder o, Node.Statement.Switch @switch)
    {
        Indent(o).Append("switch ");
        AppendExpression(o, @switch.Expression, true).AppendLine(" {");

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

    private StringBuilder AppendVariableDeclaration(StringBuilder o, ReadOnlyScope scope, Node.Statement.LocalVariable varDecl)
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

    private StringBuilder AppendWhileLoop(StringBuilder o, Node.Statement.WhileLoop whileLoop)
    {
        Indent(o).Append("while ");
        AppendExpression(o, whileLoop.Condition, true);
        return AppendBlock(o.Append(' '), whileLoop).AppendLine();
    }

    private StringBuilder AppendDoWhileLoop(StringBuilder o, Node.Statement.DoWhileLoop doWhileLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, doWhileLoop).Append(" while ");
        AppendExpression(o, doWhileLoop.Condition, true);
        return o.AppendLine(";");
    }

    private StringBuilder AppendRepeatLoop(StringBuilder o, Node.Statement.RepeatLoop repeatLoop)
    {
        Indent(o).Append("do ");
        AppendBlock(o, repeatLoop).Append(" while (!");
        AppendExpression(o, repeatLoop.Condition, true);
        return o.AppendLine(");");
    }

    private StringBuilder AppendForLoop(StringBuilder o, Node.Statement.ForLoop forLoop)
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

    private StringBuilder AppendReturn(StringBuilder o, Node.Statement.Return ret)
    {
        Indent(o).Append($"return ");
        AppendExpression(o, ret.Value);
        return o.AppendLine(";");
    }

    #endregion Statements

    #region Expressions

    private StringBuilder AppendExpression(StringBuilder o, Node.Expression expr, bool bracketed = false)
    {
        if (bracketed) {
            o.Append('(');
        }
        _ = expr switch {
            Node.Expression.FunctionCall call => AppendCall(o, call),
            Node.Expression.True => AppendLiteralBoolean(o, true),
            Node.Expression.False => AppendLiteralBoolean(o, false),
            Node.Expression.Literal.Character litChar => o.Append($"'{litChar.Value}'"),
            Node.Expression.Literal.String litStr => o.Append($"\"{litStr.Value}\""),
            Node.Expression.Literal literal => o.Append(literal.Value),
            Node.Expression.OperationBinary opBin => AppendOperationBinary(o, opBin),
            Node.Expression.OperationUnary opUn => AppendOperationUnary(o, opUn),
            Node.Expression.Bracketed b => AppendExpression(o, b.Expression, true),
            Node.Expression.Lvalue.Bracketed b => AppendExpression(o, b.Lvalue, true),
            Node.Expression.Lvalue.ArraySubscript arraySub => AppendArraySubscript(o, arraySub),
            Node.Expression.Lvalue.VariableReference variable => o.Append(variable.Name),
            Node.Expression.Lvalue.ComponentAccess compAccess
                => AppendExpression(o, compAccess.Structure, PrecedenceRequiresBrackets(compAccess.Structure))
                    .Append('.').Append(compAccess.ComponentName),
            _ => throw expr.ToUnmatchedException(),
        };
        if (bracketed) {
            o.Append(')');
        }
        return o;
    }

    private StringBuilder AppendArraySubscript(StringBuilder o, Node.Expression.Lvalue.ArraySubscript arraySub)
    {
        AppendExpression(o, arraySub.Array);

        foreach (Node.Expression index in arraySub.Indexes) {
            AppendExpression(o.Append('['), index).Append(']');
        }

        return o;
    }

    private StringBuilder AppendLiteralBoolean(StringBuilder o, bool isTrue)
    {
        _includes.Ensure(IncludeSet.StdBool);
        return o.Append(isTrue ? "true" : "false");
    }

    private StringBuilder AppendOperationBinary(StringBuilder o, Node.Expression.OperationBinary opBin)
    {
        AppendExpression(o, opBin.Operand1);
        o.Append($" {GetOperator(opBin.Operator)} ");
        AppendExpression(o, opBin.Operand2);
        return o;
    }

    private StringBuilder AppendOperationUnary(StringBuilder o, Node.Expression.OperationUnary opUn)
    {
        o.Append(GetOperator(opUn.Operator));
        AppendExpression(o, opUn.Operand);
        return o;
    }

    #endregion Expressions

    #region Helpers

    private StringBuilder AppendFileHeader(StringBuilder o) => o
        .AppendLine($"/** @file")
        .AppendLine($" * @brief {_ast.Root.Name}")
        .AppendLine($" * @author {Environment.UserName}")
        .AppendLine($" * @date {DateOnly.FromDateTime(DateTime.Now)}")
        .AppendLine($" */");

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

    private static bool PrecedenceRequiresBrackets(Node.Expression expr) => expr is not
        (Node.Expression.Bracketed
        or Node.Expression.BuiltinFdf
        or Node.Expression.FunctionCall
        or Node.Expression.Literal
        or Node.Expression.Lvalue);

    #endregion Helpers

    #region Terminals

    private static string GetOperator(BinaryOperator op) => op switch {
        BinaryOperator.And => "&&",
        BinaryOperator.Divide => "/",
        BinaryOperator.Equal => "==",
        BinaryOperator.GreaterThan => ">",
        BinaryOperator.GreaterThanOrEqual => ">=",
        BinaryOperator.LessThan => "<",
        BinaryOperator.LessThanOrEqual => "<=",
        BinaryOperator.Minus => "-",
        BinaryOperator.Modulus => "%",
        BinaryOperator.Multiply => "*",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.Or => "||",
        BinaryOperator.Plus => "+",
        BinaryOperator.Xor => "^",
        _ => throw op.ToUnmatchedException(),
    };

    private static string GetOperator(UnaryOperator op) => op switch {
        UnaryOperator.Minus => "-",
        UnaryOperator.Not => "!",
        UnaryOperator.Plus => "+",
        _ => throw op.ToUnmatchedException(),
    };

    #endregion Terminals
}
