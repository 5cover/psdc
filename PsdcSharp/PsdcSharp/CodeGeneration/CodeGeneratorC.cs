using System.Diagnostics;
using System.Text;
using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.CodeGeneration;

internal sealed partial class CodeGeneratorC : CodeGenerator
{
    private readonly IncludeSet _includes = new();
    private readonly Indentation _indent = new();
    private readonly Node.Algorithm _root;
    private readonly Scope _scope = new();

    public CodeGeneratorC(Node.Algorithm root) => _root = root;

    public override string Generate()
    {
        StringBuilder output = new();
        GenerateAlgorithm(output, _root);

        StringBuilder head = new();
        _includes.Generate(head);

        return head.Append(output).ToString();
    }

    private StringBuilder GenerateAlgorithm(StringBuilder output, Node.Algorithm algorithm)
    {
        _scope.Push(); // push the first scope: the global scope

        foreach (Node.Declaration declaration in algorithm.Declarations) {
            GenerateDeclaration(output, declaration);
        }

        _scope.Pop();

        return output;
    }

    private StringBuilder GenerateBlock(StringBuilder output, IEnumerable<Node.Statement> block)
    {
        foreach (Node.Statement statement in block) {
            GenerateStatement(output, statement);
        }

        return output;
    }

    #region Declarations

    private StringBuilder GenerateDeclaration(StringBuilder output, Node.Declaration decl) => decl switch {
        Node.Declaration.MainProgram mainProgram => GenerateMainProgram(output, mainProgram),
        Node.Declaration.FunctionDeclaration functionDeclaration => GenerateFunctionDeclaration(output, functionDeclaration),
        Node.Declaration.ProcedureDeclaration procedureDeclaration => GenerateProcedureDeclaration(output, procedureDeclaration),
        Node.Declaration.FunctionDefinition functionDefinition => GenerateFunctionDefinition(output, functionDefinition),
        Node.Declaration.ProcedureDefinition procedureDefinition => GenerateProcedureDefinition(output, procedureDefinition),
        _ => throw decl.ToUnmatchedException(),
    };

    private StringBuilder GenerateMainProgram(StringBuilder output, Node.Declaration.MainProgram mainProgram)
    {
        _indent.Indent(output).AppendLine($"// {mainProgram.ProgramName}");
        output.AppendLine("int main() ");

        return GenerateScopedIndentedBlock(output, mainProgram.Block
            .Append(new Node.Statement.Return(new Node.Expression.Literal.Integer("0"))))
        .AppendLine();
    }

    private StringBuilder GenerateFunctionDefinition(StringBuilder output, Node.Declaration.FunctionDefinition functionDefinition)
    {
        GenerateFunctionSignature(output, functionDefinition.Signature);
        GenerateSubroutineBody(output, functionDefinition.Signature.Parameters, functionDefinition.Block);

        return output;
    }
    private StringBuilder GenerateProcedureDefinition(StringBuilder output, Node.Declaration.ProcedureDefinition procedureDefinition)
    {
        GenerateProcedureSignature(output, procedureDefinition.Signature);
        GenerateSubroutineBody(output, procedureDefinition.Signature.Parameters, procedureDefinition.Block);
        return output;
    }

    private StringBuilder GenerateSubroutineBody(StringBuilder output, IEnumerable<Node.Declaration.FormalParameter> parameters, IEnumerable<Node.Statement> block)
    {
        output.AppendLine(" {");
        _scope.Push();
        _indent.Increase();

        foreach (var parameter in parameters) {
            CreateType(parameter.Type).MatchSome(type => {
                bool added = _scope.TryAdd(parameter.Name, new Symbol.Variable(
                    parameter.Mode != ParameterMode.In ? type.ToPointer(1) : type));
                Debug.Assert(added, "Failed to add parameter variable to scope");
            });
        }
        GenerateBlock(output, block);

        _indent.Decrease();
        _scope.Pop();
        output.AppendLine("}");

        return output;
    }

    private StringBuilder GenerateFunctionDeclaration(StringBuilder output, Node.Declaration.FunctionDeclaration functionDeclaration)
    {
        GenerateFunctionSignature(output, functionDeclaration.Signature);
        output.AppendLine(";");
        return output;
    }

    private StringBuilder GenerateProcedureDeclaration(StringBuilder output, Node.Declaration.ProcedureDeclaration procedure)
    {
        GenerateProcedureSignature(output, procedure.Signature);
        output.AppendLine(";");
        return output;
    }

    private StringBuilder GenerateFunctionSignature(StringBuilder output, Node.Declaration.FunctionSignature functionSignature)
    {
        var type = CreateType(functionSignature.ReturnType).Map(type => type.ToString());
        Debug.Assert(type.HasValue, "return type not created : todo semantic analysis");
        GenerateSignature(output, type.Value, functionSignature.Name, functionSignature.Parameters);

        return output;
    }

    private StringBuilder GenerateProcedureSignature(StringBuilder output, Node.Declaration.ProcedureSignature procedureSignature)
    {
        GenerateSignature(output, "void", procedureSignature.Name, procedureSignature.Parameters);
        return output;
    }

    private StringBuilder GenerateSignature(StringBuilder output, string cReturnType, string name, IEnumerable<Node.Declaration.FormalParameter> parameters)
    {
        output.Append("void ");
        output.Append(name);
        output.Append('(');
        if (parameters.Any()) {
            output.AppendJoin(", ", parameters.Select(GenerateFormalParameterToString));
        } else {
            output.Append("void");
        }
        output.Append(')');

        return output;
    }

    private string GenerateFormalParameterToString(Node.Declaration.FormalParameter formalParameter)
    {
        StringBuilder output = new();
        var type = CreateType(formalParameter.Type).Value.NotNull();
        if (formalParameter.Mode is not ParameterMode.In) {
            type = type.ToPointer(1);
        }

        GenerateVariableDeclaration(output, type, formalParameter.Name.Yield());

        return output.ToString();
    }

    #endregion Declarations

    #region Statements

    private StringBuilder GeneratePrintStatement(StringBuilder output, Node.Statement.Print print)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(print.Arguments);

        _indent.Indent(output).Append($@"printf(""{format}\n""");

        foreach (Node.Expression arg in arguments) {
            GenerateExpression(output.Append(", "), arg);
        }

        return output.AppendLine(");");
    }

    private StringBuilder GenerateReadStatement(StringBuilder output, Node.Statement.Read read)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(read.Argument.Yield());

        _indent.Indent(output).Append($@"scanf(""{format}""");

        foreach (Node.Expression arg in arguments) {
            GenerateExpression(output.Append(", &"), arg);
        }

        return output.AppendLine(");");
    }

    private StringBuilder GenerateStatement(StringBuilder output, Node.Statement statement) => statement switch {
        Node.Statement.Print print => GeneratePrintStatement(output, print),
        Node.Statement.Read read => GenerateReadStatement(output, read),
        Node.Statement.VariableDeclaration variableDeclaration => GenerateLocalVariableDeclaration(output, variableDeclaration),
        Node.Statement.Assignment assignment => GenerateAssignment(output, assignment),
        Node.Statement.Alternative alternative => GenerateAlternative(output, alternative),
        Node.Statement.WhileLoop whileLoop => GenerateWhileLoop(output, whileLoop),
        Node.Statement.DoWhileLoop doWhileLoop => GenerateDoWhileLoop(output, doWhileLoop),
        Node.Statement.RepeatLoop repeatLoop => GenerateRepeatLoop(output, repeatLoop),
        Node.Statement.ForLoop forLoop => GenerateForLoop(output, forLoop),
        Node.Statement.Return @return => GenerateReturn(output, @return),
        _ => throw statement.ToUnmatchedException(),
    };

    private StringBuilder GenerateAlternative(StringBuilder output, Node.Statement.Alternative alternative)
    {
        GenerateAlternativeClause(_indent.Indent(output), alternative.If, "if");

        foreach (var elseIf in alternative.ElseIfs) {
            GenerateAlternativeClause(output, elseIf, " else if");
        }

        alternative.Else.MatchSome(elseBlock
         => GenerateScopedIndentedBlock(output.Append(" else "), elseBlock).AppendLine());

        return output;
    }

    private StringBuilder GenerateAlternativeClause(StringBuilder output, Node.Statement.Alternative.Clause clause, string keyword)
    {
        GenerateExpression(output.Append($"{keyword} ("), clause.Condition).Append(") ");

        return GenerateScopedIndentedBlock(output, clause.Block);
    }

    private StringBuilder GenerateScopedIndentedBlock(StringBuilder output, IEnumerable<Node.Statement> block)
    {
        output.AppendLine("{");
        _indent.Increase();
        _scope.Push();
        GenerateBlock(output, block);
        _scope.Pop();
        _indent.Decrease();
        _indent.Indent(output).Append('}');

        return output;
    }

    private StringBuilder GenerateAssignment(StringBuilder output, Node.Statement.Assignment assignment)
    {
        _indent.Indent(output);
        output.Append(assignment.Target);
        output.Append(" = ");
        GenerateExpression(output, assignment.Value);
        output.AppendLine(";");

        return output;
    }

    private StringBuilder GenerateLocalVariableDeclaration(StringBuilder output, Node.Statement.VariableDeclaration variableDeclaration)
    {
        _indent.Indent(output);

        CreateType(variableDeclaration.Type).MatchSome(type
         => GenerateVariableDeclaration(output, type, variableDeclaration.Names).AppendLine(";"));

        return output;
    }

    private StringBuilder GenerateVariableDeclaration(StringBuilder output, TypeInfo type, IEnumerable<string> names)
    {
        foreach (var name in names) {
            _scope.TryAdd(name, new Symbol.Variable(type));
        }
        foreach (string header in type.RequiredHeaders) {
            _includes.Ensure(header);
        }
        output.Append(type.CreateDeclaration(names));

        return output;
    }

    private StringBuilder GenerateWhileLoop(StringBuilder output, Node.Statement.WhileLoop whileLoop)
    {
        _indent.Indent(output).Append("while ");
        GenerateExpressionEnclosedInBrackets(output, whileLoop.Condition);

        return GenerateScopedIndentedBlock(output.Append(' '), whileLoop.Block).AppendLine();
    }

    private StringBuilder GenerateDoWhileLoop(StringBuilder output, Node.Statement.DoWhileLoop doWhileLoop)
    {
        _indent.Indent(output).Append("do ");

        GenerateScopedIndentedBlock(output, doWhileLoop.Block).Append(" while ");

        GenerateExpressionEnclosedInBrackets(output, doWhileLoop.Condition);
        output.AppendLine(";");

        return output;
    }

    private StringBuilder GenerateRepeatLoop(StringBuilder output, Node.Statement.RepeatLoop repeatLoop)
    {
        _indent.Indent(output).Append("do ");

        GenerateScopedIndentedBlock(output, repeatLoop.Block).Append(" while (!");

        GenerateExpressionEnclosedInBrackets(output, repeatLoop.Condition);
        output.AppendLine(");");

        return output;
    }

    private StringBuilder GenerateForLoop(StringBuilder output, Node.Statement.ForLoop forLoop)
    {
        _indent.Indent(output).Append($"for ({forLoop.VariantName} = ");
        GenerateExpression(output, forLoop.Start);

        output.Append($"; {forLoop.VariantName} <= ");
        GenerateExpression(output, forLoop.End);

        output.Append($"; {forLoop.VariantName}");
        forLoop.Step.Match(
            step => GenerateExpression(output.Append(" += "), step),
            none: () => output.Append("++"));

        return GenerateScopedIndentedBlock(output.Append(") "), forLoop.Block).AppendLine();
    }

    private StringBuilder GenerateReturn(StringBuilder output, Node.Statement.Return @return)
    {
        _indent.Indent(output).Append($"return ");
        GenerateExpression(output, @return.Value);
        output.AppendLine(";");
        return output;
    }

    #endregion Statements

    #region Types

    #endregion Types

    #region Expressions

    private StringBuilder GenerateExpression(StringBuilder output, Node.Expression expression) => expression switch {
        Node.Expression.Literal.True => GenerateLiteralBoolean(output, true),
        Node.Expression.Literal.False => GenerateLiteralBoolean(output, false),
        Node.Expression.Literal.Character litChar => output.Append($"'{litChar.Value}'"),
        Node.Expression.Literal.String litStr => output.Append($"\"{litStr.Value}\""),
        Node.Expression.Literal literal => output.Append(literal.Value),
        Node.Expression.OperationBinary binaryOperation => GenerateOperationBinary(output, binaryOperation),
        Node.Expression.OperationUnary unaryOperation => GenerateOperationUnary(output, unaryOperation),
        Node.Expression.ArraySubscript arraySubscript => GenerateArraySubscript(output, arraySubscript),
        Node.Expression.Bracketed bracketed => GenerateExpression(output.Append('('), bracketed.Expression).Append(')'),
        Node.Expression.Variable variable => output.Append(variable.Name),
        _ => throw expression.ToUnmatchedException(),
    };

    private string GenerateExpressionToString(Node.Expression expr) => GenerateExpression(new(), expr).ToString();

    private StringBuilder GenerateArraySubscript(StringBuilder output, Node.Expression.ArraySubscript arraySubscript)
    {
        GenerateExpression(output, arraySubscript.Array);

        foreach (Node.Expression index in arraySubscript.Indices) {
            GenerateExpression(output.Append('['), index).Append(']');
        }

        return output;
    }

    private StringBuilder GenerateLiteralBoolean(StringBuilder output, bool isTrue)
    {
        _includes.Ensure(IncludeSet.StdBool);
        return output.Append(isTrue ? "true" : "false");
    }

    private StringBuilder GenerateOperationBinary(StringBuilder output, Node.Expression.OperationBinary operation)
    {
        GenerateExpression(output, operation.Operand1);
        output.Append($" {GetCOperator(operation.Operator)} ");
        GenerateExpression(output, operation.Operand2);
        return output;
    }

    private StringBuilder GenerateOperationUnary(StringBuilder output, Node.Expression.OperationUnary operation)
    {
        output.Append(GetCOperator(operation.Operator));
        GenerateExpression(output, operation.Operand);
        return output;
    }

    #endregion Expressions

    #region Helpers

    private Option<TypeInfo> CreateType(Node.Type type) => type switch {
        Node.Type.String => TypeInfo.CreateString().Some(),
        Node.Type.Primitive p => TypeInfo.Primitive.Create(p.Type).Some(),
        Node.Type.AliasReference alias => _scope.GetSymbol<Symbol.TypeAlias>(alias.Name)
                .Map(aliasType => TypeInfo.CreateAlias(alias.Name, aliasType.TargetType)),
        Node.Type.Array array => CreateType(array.Type).Map(elementType
         => TypeInfo.CreateArray(elementType, array.Dimensions.Select(GenerateExpressionToString))),
        Node.Type.LengthedString str => EvaluateValue(str.Length)
            .FlatMap(Parse.ToInt32)
            .Map(Convert.ToString)
            .Map(TypeInfo.CreateLengthedString)
            .ValueOr(() => TypeInfo.CreateLengthedString(GenerateExpressionToString(str.Length))).Some(),
        _ => throw type.ToUnmatchedException(),
    };

    private Option<string> EvaluateValue(Node.Expression expr) => expr switch {
        Node.Expression.Literal l => l.Value.Some(),
        Node.Expression.Variable v => _scope.GetSymbol<Symbol.Constant>(v.Name).Map(constant => constant.Value),
        _ => Option.None<string>(),
    };

    private StringBuilder GenerateExpressionEnclosedInBrackets(StringBuilder output, Node.Expression expression)
    #endregion Helpers

     => expression switch {
         Node.Expression.Bracketed => GenerateExpression(output, expression),
         _ => GenerateExpression(output.Append('('), expression).Append(')')
     };

    #region Terminals

    private static string GetCOperator(BinaryOperator op) => op switch {
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
        _ => throw op.ToUnmatchedException(),
    };

    private static string GetCOperator(UnaryOperator op) => op switch {
        UnaryOperator.Minus => "-",
        UnaryOperator.Not => "!",
        UnaryOperator.Plus => "+",
        _ => throw op.ToUnmatchedException(),
    };

    #endregion Terminals

}
