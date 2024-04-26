using System.Text;
using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.SemanticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal sealed partial class CodeGeneratorC(SemanticAst semanticAst) : CodeGenerator(semanticAst)
{
    private readonly IncludeSet _includes = new();

    public override string Generate()
    {
        StringBuilder output = new();

        foreach (Node.Declaration declaration in _ast.Root.Declarations) {
            AppendDeclaration(output, _ast.Scopes[_ast.Root], declaration);
        }

        return _includes.AppendIncludeSection(AppendFileHeader(new()))
            .Append(output).ToString();
    }

    #region Declarations

    private StringBuilder AppendDeclaration(StringBuilder output, ReadOnlyScope parentScope, Node.Declaration decl) => decl switch {
        Node.Declaration.Alias alias => AppendAliasDeclaration(output, parentScope, alias),
        Node.Declaration.Constant constant => AppendConstant(output, constant),
        Node.Declaration.MainProgram mainProgram => AppendMainProgram(output, mainProgram),
        Node.Declaration.Function func => AppendFunctionDeclaration(output, parentScope, func),
        Node.Declaration.Procedure proc => AppendProcedureDeclaration(output, parentScope, proc),
        Node.Declaration.FunctionDefinition funcDef => AppendFunctionDefinition(output, parentScope, funcDef),
        Node.Declaration.ProcedureDefinition procDef => AppendProcedureDefinition(output, parentScope, procDef),
        _ => throw decl.ToUnmatchedException(),
    };

    private StringBuilder AppendAliasDeclaration(StringBuilder output, ReadOnlyScope scope, Node.Declaration.Alias alias)
    {
        var type = CreateTypeInfo(scope.GetSymbol<Symbol.TypeAlias>(alias.Name).Unwrap().TargetType);
        return Indent(output).AppendLine($"typedef {type.GenerateDeclaration(alias.Name.Yield())};");
    }

    private StringBuilder AppendConstant(StringBuilder output, Node.Declaration.Constant constant)
    {
        Indent(output).Append($"#define {constant.Name} ");
        return AppendExpression(output.Append('('), constant.Value).AppendLine(")");
    }

    private StringBuilder AppendMainProgram(StringBuilder output, Node.Declaration.MainProgram mainProgram)
    {
        output.AppendLine().Append("int main() ");

        return AppendBlock(output, mainProgram,
            sb => Indent(sb).AppendLine("return 0;"))
        .AppendLine();
    }

    private StringBuilder AppendFunctionDefinition(StringBuilder output, ReadOnlyScope parentScope, Node.Declaration.FunctionDefinition funcDef)
    {
        AppendFunctionSignature(output.AppendLine(), parentScope.GetSymbol<Symbol.Function>(funcDef.Signature.Name).Unwrap());
        return AppendBlock(output.Append(' '), funcDef).AppendLine();
    }
    private StringBuilder AppendProcedureDefinition(StringBuilder output, ReadOnlyScope parentScope, Node.Declaration.ProcedureDefinition procDef)
    {
        AppendProcedureSignature(output.AppendLine(), parentScope.GetSymbol<Symbol.Procedure>(procDef.Signature.Name).Unwrap());
        return AppendBlock(output.Append(' '), procDef).AppendLine();
    }

    private StringBuilder AppendFunctionDeclaration(StringBuilder output, ReadOnlyScope parentScope, Node.Declaration.Function func)
     => AppendFunctionSignature(output, parentScope.GetSymbol<Symbol.Function>(func.Signature.Name).Unwrap()).AppendLine(";");
    private StringBuilder AppendProcedureDeclaration(StringBuilder output, ReadOnlyScope parentScope, Node.Declaration.Procedure proc)
     => AppendProcedureSignature(output, parentScope.GetSymbol<Symbol.Procedure>(proc.Signature.Name).Unwrap()).AppendLine(";");

    private StringBuilder AppendFunctionSignature(StringBuilder output, Symbol.Function func)
     => AppendSignature(output, CreateTypeInfo(func.ReturnType).Generate(), func.Name, func.Parameters);
    private StringBuilder AppendProcedureSignature(StringBuilder output, Symbol.Procedure proc)
     => AppendSignature(output, "void", proc.Name, proc.Parameters);

    private StringBuilder AppendSignature(StringBuilder output, string cReturnType, Node.Identifier name, IEnumerable<Symbol.Parameter> parameters)
    {
        output.Append($"{cReturnType} {name}(");
        if (parameters.Any()) {
            output.AppendJoin(", ", parameters.Select(GenerateParameter));
        } else {
            output.Append("void");
        }
        output.Append(')');

        return output;
    }

    private string GenerateParameter(Symbol.Parameter param)
    {
        StringBuilder output = new();
        var type = CreateTypeInfo(param.Type);
        if (RequiresPointer(param.Mode)) {
            type = type.ToPointer(1);
        }

        AppendVariableDeclaration(output, type, param.Name.Yield());

        return output.ToString();
    }

    #endregion Declarations

    #region Statements

    private StringBuilder AppendStatement(StringBuilder output, ReadOnlyScope parentScope, Node.Statement stmt) => stmt switch {
        Node.Statement.Nop => Indent(output).AppendLine(";"),
        Node.Statement.Alternative alternative => AppendAlternative(output, alternative),
        Node.Statement.Assignment assignment => AppendAssignment(output, assignment),
        Node.Statement.DoWhileLoop doWhileLoop => AppendDoWhileLoop(output, doWhileLoop),
        Node.Statement.EcrireEcran ecrireEcran => AppendEcrireEcran(output, parentScope, ecrireEcran),
        Node.Statement.ForLoop forLoop => AppendForLoop(output, forLoop),
        Node.Statement.LireClavier lireClavier => AppendLireClavier(output, parentScope, lireClavier),
        Node.Statement.ProcedureCall call => AppendCall(Indent(output), call).AppendLine(";"),
        Node.Statement.RepeatLoop repeatLoop => AppendRepeatLoop(output, repeatLoop),
        Node.Statement.Return ret => AppendReturn(output, ret),
        Node.Statement.Switch @switch => AppendSwitch(output, @switch),
        Node.Statement.VariableDeclaration varDecl => AppendVariableDeclaration(output, parentScope, varDecl),
        Node.Statement.WhileLoop whileLoop => AppendWhileLoop(output, whileLoop),
        _ => throw stmt.ToUnmatchedException(),
    };

    private StringBuilder AppendEcrireEcran(StringBuilder output, ReadOnlyScope parentScope, Node.Statement.EcrireEcran ecrireEcran)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(parentScope, ecrireEcran.Arguments);

        Indent(output).Append($@"printf(""{format}\n""");

        foreach (Node.Expression arg in arguments) {
            AppendExpression(output.Append(", "), arg);
        }

        return output.AppendLine(");");
    }

    private StringBuilder AppendLireClavier(StringBuilder output, ReadOnlyScope parentScope, Node.Statement.LireClavier lireClavier)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(parentScope, lireClavier.Argument.Yield());

        Indent(output).Append($@"scanf(""{format}""");

        foreach (Node.Expression arg in arguments) {
            AppendExpression(output.Append(", &"), arg);
        }

        return output.AppendLine(");");
    }

    private StringBuilder AppendAlternative(StringBuilder output, Node.Statement.Alternative alternative)
    {
        AppendAlternativeClause(Indent(output), "if", alternative.If.Condition, alternative.If);

        if (alternative.ElseIfs.Count == 0 && !alternative.Else.HasValue) {
            return output.AppendLine();
        }

        foreach (var elseIf in alternative.ElseIfs) {
            AppendAlternativeClause(output, " else if", elseIf.Condition, elseIf);
        }

        alternative.Else.MatchSome(elseBlock
         => AppendBlock(output.Append(" else "), elseBlock).AppendLine());

        return output;

        StringBuilder AppendAlternativeClause(StringBuilder output, string keyword, Node.Expression condition, BlockNode node)
        {
            AppendExpression(output.Append($"{keyword} ("), condition).Append(") ");
            return AppendBlock(output, node);
        }
    }

    private StringBuilder AppendBlock(StringBuilder output, BlockNode node, Action<StringBuilder>? suffix = null)
    {
        output.AppendLine("{");
        _indent.Increase();
        AppendBlockStatements(output, node);
        suffix?.Invoke(output);
        _indent.Decrease();
        Indent(output).Append('}');

        return output;
    }

    private StringBuilder AppendBlockStatements(StringBuilder output, BlockNode node)
    {
        foreach (Node.Statement statement in node.Block) {
            AppendStatement(output, _ast.Scopes[node], statement);
        }

        return output;
    }

    private StringBuilder AppendAssignment(StringBuilder output, Node.Statement.Assignment assignment)
    {
        Indent(output);
        output.Append(assignment.Target);
        output.Append(" = ");
        AppendExpression(output, assignment.Value);
        output.AppendLine(";");

        return output;
    }

    private StringBuilder AppendSwitch(StringBuilder output, Node.Statement.Switch @switch)
    {
        Indent(output).Append("switch ");
        AppendBracketedExpression(output, @switch.Expression).AppendLine(" {");

        foreach (var @case in @switch.Cases) {
            Indent(output).Append("case ");
            AppendExpression(output, @case.When).AppendLine(":");
            _indent.Increase();
            AppendBlockStatements(output, @case);
            Indent(output).AppendLine("break;");
            _indent.Decrease();
        }

        @switch.Default.MatchSome(@default => {
            Indent(output).AppendLine("default:");
            _indent.Increase();
            AppendBlockStatements(output, @default);
            _indent.Decrease();
        });

        return Indent(output).AppendLine("}");
    }

    private StringBuilder AppendVariableDeclaration(StringBuilder output, ReadOnlyScope parentScope, Node.Statement.VariableDeclaration varDecl)
    {
        Indent(output);

        var variable = parentScope.GetSymbol<Symbol.Variable>(varDecl.Names.First()).Unwrap();

        return AppendVariableDeclaration(output, CreateTypeInfo(variable.Type), varDecl.Names).AppendLine(";");
    }

    private StringBuilder AppendVariableDeclaration(StringBuilder output, TypeInfoC type, IEnumerable<Node.Identifier> names)
    {
        foreach (string header in type.RequiredHeaders) {
            _includes.Ensure(header);
        }
        output.Append(type.GenerateDeclaration(names));

        return output;
    }

    private StringBuilder AppendWhileLoop(StringBuilder output, Node.Statement.WhileLoop whileLoop)
    {
        Indent(output).Append("while ");
        AppendBracketedExpression(output, whileLoop.Condition);

        return AppendBlock(output.Append(' '), whileLoop).AppendLine();
    }

    private StringBuilder AppendDoWhileLoop(StringBuilder output, Node.Statement.DoWhileLoop doWhileLoop)
    {
        Indent(output).Append("do ");

        AppendBlock(output, doWhileLoop).Append(" while ");

        AppendBracketedExpression(output, doWhileLoop.Condition);
        output.AppendLine(";");

        return output;
    }

    private StringBuilder AppendRepeatLoop(StringBuilder output, Node.Statement.RepeatLoop repeatLoop)
    {
        Indent(output).Append("do ");

        AppendBlock(output, repeatLoop).Append(" while (!");

        AppendBracketedExpression(output, repeatLoop.Condition);
        output.AppendLine(");");

        return output;
    }

    private StringBuilder AppendForLoop(StringBuilder output, Node.Statement.ForLoop forLoop)
    {
        Indent(output).Append($"for ({forLoop.VariantName} = ");
        AppendExpression(output, forLoop.Start);

        output.Append($"; {forLoop.VariantName} <= ");
        AppendExpression(output, forLoop.End);

        output.Append($"; {forLoop.VariantName}");
        forLoop.Step.Match(
            step => AppendExpression(output.Append(" += "), step),
            none: () => output.Append("++"));

        return AppendBlock(output.Append(") "), forLoop).AppendLine();
    }

    private StringBuilder AppendReturn(StringBuilder output, Node.Statement.Return ret)
    {
        Indent(output).Append($"return ");
        AppendExpression(output, ret.Value);
        output.AppendLine(";");
        return output;
    }

    #endregion Statements

    #region Types

    #endregion Types

    #region Expressions

    private StringBuilder AppendExpression(StringBuilder output, Node.Expression expr) => expr switch {
        Node.Expression.FunctionCall call => AppendCall(output, call),
        Node.Expression.Literal.True => AppendLiteralBoolean(output, true),
        Node.Expression.Literal.False => AppendLiteralBoolean(output, false),
        Node.Expression.Literal.Character litChar => output.Append($"'{litChar.Value}'"),
        Node.Expression.Literal.String litStr => output.Append($"\"{litStr.Value}\""),
        Node.Expression.Literal literal => output.Append(literal.Value),
        Node.Expression.OperationBinary opBin => AppendOperationBinary(output, opBin),
        Node.Expression.OperationUnary opUn => AppendOperationUnary(output, opUn),
        Node.Expression.ArraySubscript arraySub => AppendArraySubscript(output, arraySub),
        Node.Expression.Bracketed bracketed => AppendExpression(output.Append('('), bracketed.Expression).Append(')'),
        Node.Expression.VariableReference variable => output.Append(variable.Name),
        _ => throw expr.ToUnmatchedException(),
    };

    private StringBuilder AppendArraySubscript(StringBuilder output, Node.Expression.ArraySubscript arraySub)
    {
        AppendExpression(output, arraySub.Array);

        foreach (Node.Expression index in arraySub.Indexes) {
            AppendExpression(output.Append('['), index).Append(']');
        }

        return output;
    }

    private StringBuilder AppendLiteralBoolean(StringBuilder output, bool isTrue)
    {
        _includes.Ensure(IncludeSet.StdBool);
        return output.Append(isTrue ? "true" : "false");
    }

    private StringBuilder AppendOperationBinary(StringBuilder output, Node.Expression.OperationBinary opBin)
    {
        AppendExpression(output, opBin.Operand1);
        output.Append($" {GetOperator(opBin.Operator)} ");
        AppendExpression(output, opBin.Operand2);
        return output;
    }

    private StringBuilder AppendOperationUnary(StringBuilder output, Node.Expression.OperationUnary opUn)
    {
        output.Append(GetOperator(opUn.Operator));
        AppendExpression(output, opUn.Operand);
        return output;
    }

    #endregion Expressions

    #region Helpers

    private StringBuilder AppendFileHeader(StringBuilder output) => output
        .AppendLine($"/** @file")
        .AppendLine($" * @brief {_ast.Root.Name}")
        .AppendLine($" * @author {Environment.UserName}")
        .AppendLine($" * @date {DateOnly.FromDateTime(DateTime.Now)}")
        .AppendLine($" */")
        .AppendLine();

    private StringBuilder AppendCall(StringBuilder output, CallNode call)
    {
        output.Append($"{call.Name}(");
        const string ParameterSeparator = ", ";
        foreach (var param in call.Parameters) {
            if (RequiresPointer(param.Mode)) {
                output.Append('&');
            }
            AppendExpression(output, param.Value).Append(ParameterSeparator);
        }
        output.Length -= ParameterSeparator.Length; // Remove unneeded last separator
        return output.Append(')');
    }

    private StringBuilder AppendBracketedExpression(StringBuilder output, Node.Expression expr)
     => expr switch {
         Node.Expression.Bracketed => AppendExpression(output, expr),
         _ => AppendExpression(output.Append('('), expr).Append(')')
     };

    private TypeInfoC CreateTypeInfo(EvaluatedType evaluatedType)
     => TypeInfoC.Create(evaluatedType, expr => AppendExpression(new(), expr).ToString());

    private StringBuilder Indent(StringBuilder output) => _indent.Indent(output);

    private static bool RequiresPointer(ParameterMode mode) => mode is not ParameterMode.In;

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
