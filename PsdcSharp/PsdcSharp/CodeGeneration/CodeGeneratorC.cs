using System.Text;

using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.CodeGeneration;

internal sealed partial class CodeGeneratorC : CodeGenerator
{
    private readonly IncludeSet _includes = new();
    private readonly Indentation _indentation = new();
    private readonly ParseResult<Node.Algorithm> _root;
    private readonly Scope _scope = new();

    public CodeGeneratorC(ParseResult<Node.Algorithm> root) => _root = root;

    public override string Generate()
    {
        StringBuilder output = new();
        GetValueOrAddSyntaxError(_root).MatchSome(root => GenerateAlgorithm(output, root).AppendLine());

        StringBuilder head = new();
        _includes.Generate(head);

        return head.Append(output).ToString();
    }

    private static string GetOperationOperator(TokenType @operator) => @operator switch {
        TokenType.OperatorAnd => "&&",
        TokenType.OperatorDivide => "/",
        TokenType.OperatorEqual => "==",
        TokenType.OperatorGreaterThan => ">",
        TokenType.OperatorGreaterThanOrEqual => ">=",
        TokenType.OperatorLessThan => "<",
        TokenType.OperatorLessThanOrEqual => "<=",
        TokenType.OperatorMinus => "-",
        TokenType.OperatorModulus => "%",
        TokenType.OperatorMultiply => "*",
        TokenType.OperatorNot => "!",
        TokenType.OperatorNotEqual => "!=",
        TokenType.OperatorOr => "||",
        TokenType.OperatorPlus => "+",
        _ => throw @operator.ToUnmatchedException(),
    };

    private Option<TypeInfo> CreateType(Node.Type type) => type switch {
        Node.Type.String => TypeInfo.String.Create().Some(),
        Node.Type.Primitive p => TypeInfo.Primitive.Create(p.Type).Some(),
        Node.Type.AliasReference alias => GetValueOrAddSyntaxError(alias.Name)
            .FlatMap(aliasName => _scope.GetSymbol<Symbol.TypeAlias>(aliasName)
                .Map(aliasType => TypeInfo.Alias.Create(aliasName, aliasType.TargetType))),
        Node.Type.Array array => GetValueOrAddSyntaxError(array.Type).FlatMap(CreateType).Map(elementType
         => TypeInfo.Array.Create(elementType, GetValuesOrAddSyntaxErrors(array.Dimensions).Select(GenerateExpressionToString))),
        Node.Type.LengthedString str => GetValueOrAddSyntaxError(str.Length).Map(length
         => EvaluateValue(length)
            .FlatMap(Parse.ToInt32)
            .Map(TypeInfo.LengthedString.Create)
            .ValueOr(() => TypeInfo.LengthedString.Create(GenerateExpressionToString(length)))),
        _ => throw type.ToUnmatchedException(),
    };

    private Option<string> EvaluateValue(Node.Expression expr) => expr switch {
        Node.Expression.Literal l => l.Value.Some(),
        Node.Expression.Variable v => _scope.GetSymbol<Symbol.Constant>(v.Name).Map(constant => constant.Value),
        _ => Option.None<string>(),
    };

    private StringBuilder GenerateAlgorithm(StringBuilder output, Node.Algorithm algorithm)
    {
        _scope.Push(); // global scope

        GetValueOrAddSyntaxError(algorithm.Name).MatchSome(name => _indentation.Indent(output).AppendLine($"// {name}"));

        foreach (Node.Declaration declaration in GetValuesOrAddSyntaxErrors(algorithm.Declarations)) {
            GenerateDeclaration(output, declaration);
        }

        _scope.Pop();

        return output;
    }

    private StringBuilder GenerateArraySubscript(StringBuilder output, Node.Expression.ArraySubscript arraySubscript)
    {
        GetValueOrAddSyntaxError(arraySubscript.Array).MatchSome(array => GenerateExpression(output, array));

        foreach (Node.Expression index in GetValuesOrAddSyntaxErrors(arraySubscript.Indices)) {
            GenerateExpression(output.Append('['), index).Append(']');
        }

        return output;
    }

    private StringBuilder GenerateDeclaration(StringBuilder output, Node.Declaration decl) => decl switch {
        Node.Declaration.MainProgram mainProgram => GenerateMainProgram(output, mainProgram),
        _ => throw decl.ToUnmatchedException(),
    };

    private StringBuilder GenerateExpression(StringBuilder output, Node.Expression expression) => expression switch {
        Node.Expression.Literal.True => GenerateLiteralBoolean(output, true),
        Node.Expression.Literal.False => GenerateLiteralBoolean(output, false),
        Node.Expression.Literal.Character litChar => output.Append($"'{litChar.Value}'"),
        Node.Expression.Literal.String litStr => output.Append($"\"{litStr.Value}\""),
        Node.Expression.Literal literal => output.Append(literal.Value),
        Node.Expression.OperationBinary binaryOperation => GenerateOperationBinary(output, binaryOperation),
        Node.Expression.OperationUnary unaryOperation => GenerateOperationUnary(output, unaryOperation),
        Node.Expression.ArraySubscript arraySubscript => GenerateArraySubscript(output, arraySubscript),
        Node.Expression.Bracketed bracketed => GetValueOrAddSyntaxError(bracketed.Expression)
            .Map(expr => GenerateExpression(output.Append('('), expr).Append(')')).ValueOr(output),
        Node.Expression.Variable variable => output.Append(variable.Name),
        _ => throw expression.ToUnmatchedException(),
    };

    private string GenerateExpressionToString(Node.Expression expr) => GenerateExpression(new(), expr).ToString();

    private StringBuilder GenerateLiteralBoolean(StringBuilder output, bool isTrue)
    {
        _includes.Ensure(IncludeSet.StdBool);
        return output.Append(isTrue ? "true" : "false");
    }

    private StringBuilder GenerateMainProgram(StringBuilder output, Node.Declaration.MainProgram mainProgram)
    {
        _indentation.Indent(output).AppendLine("int main()");
        _indentation.Indent(output).AppendLine("{");

        _scope.Push();
        _indentation.Increase();

        foreach (ParseResult<Node.Statement> statementNode in mainProgram.Block) {
            GetValueOrAddSyntaxError(statementNode).MatchSome(statement =>
                GenerateStatement(output, statement, statementNode.SourceTokens));
        }

        _indentation.Indent(output).AppendLine("return 0;");

        _indentation.Decrease();
        _scope.Pop();

        _indentation.Indent(output).AppendLine("}");

        return output;
    }

    private StringBuilder GenerateOperationBinary(StringBuilder output, Node.Expression.OperationBinary operation)
    {
        GetValueOrAddSyntaxError(operation.Operand1).MatchSome(op1 => GenerateExpression(output, op1));
        output.Append($" {GetOperationOperator(operation.Operator)} ");
        GetValueOrAddSyntaxError(operation.Operand2).MatchSome(op2 => GenerateExpression(output, op2));
        return output;
    }

    private StringBuilder GenerateOperationUnary(StringBuilder output, Node.Expression.OperationUnary operation)
    {
        output.Append(GetOperationOperator(operation.Operator));
        GetValueOrAddSyntaxError(operation.Operand).MatchSome(op1 => GenerateExpression(output, op1));
        return output;
    }

    private StringBuilder GeneratePrintStatement(StringBuilder output, Node.Statement.Print print)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(print.Arguments);

        _indentation.Indent(output).Append($@"printf(""{format}\n""");

        foreach (Node.Expression arg in arguments) {
            GenerateExpression(output.Append(", "), arg);
        }

        return output.AppendLine(");");
    }

    private StringBuilder GenerateReadStatement(StringBuilder output, Node.Statement.Read read)
    {
        _includes.Ensure(IncludeSet.StdIo);

        var (format, arguments) = BuildFormatString(read.Argument.Yield());

        _indentation.Indent(output).Append($@"scanf(""{format}""");

        foreach (Node.Expression arg in arguments) {
            GenerateExpression(output.Append(", "), arg);
        }

        return output.AppendLine(");");
    }

    private StringBuilder GenerateStatement(StringBuilder output, Node.Statement statement, IReadOnlyCollection<Token> sourceTokens) => statement switch {
        Node.Statement.Print print => GeneratePrintStatement(output, print),
        Node.Statement.Read read => GenerateReadStatement(output, read),
        Node.Statement.VariableDeclaration variableDeclaration => GenerateVariableDeclaration(output, variableDeclaration, sourceTokens),
        Node.Statement.Assignment assignment => GenerateAssignment(output, assignment),
        _ => throw statement.ToUnmatchedException(),
    };

    private StringBuilder GenerateAssignment(StringBuilder output, Node.Statement.Assignment assignment)
    {
        _indentation.Indent(output);
        GetValueOrAddSyntaxError(assignment.Target).Map(output.Append);
        output.Append(" = ");
        GetValueOrAddSyntaxError(assignment.Value).Map(expr => GenerateExpression(output, expr));
        output.AppendLine(";");

        return output;
    }

    private StringBuilder GenerateVariableDeclaration(StringBuilder output, Node.Statement.VariableDeclaration variableDeclaration, IReadOnlyCollection<Token> sourceTokens)
    {
        _indentation.Indent(output);

        GetValueOrAddSyntaxError(variableDeclaration.Type).FlatMap(CreateType).MatchSome(variableType => {
            List<string> names = GetValuesOrAddSyntaxErrors(variableDeclaration.Names).ToList();
            foreach (var name in names.Where(name => !_scope.TryAdd(name, new Symbol.Variable(variableType)))) {
                AddMessage(Message.RedefinedSymbol<Symbol.Variable>(sourceTokens, name));
            }
            foreach (string header in variableType.RequiredHeaders) {
                _includes.Ensure(header);
            }
            output.Append(variableType.CreateDeclaration(names)).AppendLine(";");
        });

        return output;
    }

    private Option<T> GetValueOrAddSyntaxError<T>(ParseResult<T> result)
    {
        if (result.HasValue) {
            return result.Value.Some();
        }

        AddMessage(Message.SyntaxError<T>(result.SourceTokens, result.Error));

        return Option.None<T>();
    }

    /// <remarks>The returned collection should only be enumerated once, otherwise we may get duplicate errors.</remarks>
    private IEnumerable<T> GetValuesOrAddSyntaxErrors<T>(IEnumerable<ParseResult<T>> results)
     => results.Select(GetValueOrAddSyntaxError).WhereSome();
}
