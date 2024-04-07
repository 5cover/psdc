using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.CodeGeneration;

internal sealed partial class CodeGeneratorC : CodeGenerator
{
    private readonly IncludeSet _includes = new();
    private readonly Indentation _indent = new();
    private readonly ParseResult<Node.Algorithm> _root;
    private readonly Scope _scope = new();

    public CodeGeneratorC(ParseResult<Node.Algorithm> root) => _root = root;

    public override string Generate()
    {
        StringBuilder output = new();
        GetValueOrSyntaxError(_root).MatchSome(root => GenerateAlgorithm(output, root));

        StringBuilder head = new();
        _includes.Generate(head);

        return head.Append(output).ToString();
    }

    private StringBuilder GenerateAlgorithm(StringBuilder output, Node.Algorithm algorithm)
    {
        _scope.Push(); // push the first scope: the global scope

        foreach (Node.Declaration declaration in GetValuesOrSyntaxErrors(algorithm.Declarations)) {
            GenerateDeclaration(output, declaration);
        }

        _scope.Pop();

        return output;
    }

    private StringBuilder GenerateBlock(StringBuilder output, IReadOnlyCollection<ParseResult<Node.Statement>> block)
    {
        _scope.Push();
        foreach (ParseResult<Node.Statement> statementNode in block) {
            GetValueOrSyntaxError(statementNode).MatchSome(statement =>
                GenerateStatement(output, statement, statementNode.SourceTokens));
        }
        _scope.Pop();
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
        GetValueOrSyntaxError(mainProgram.ProgramName).MatchSome(name => _indent.Indent(output).AppendLine($"// {name}"));
        output.AppendLine("int main() {");

        _indent.Increase();
        GenerateBlock(output, mainProgram.Block);
        output.AppendLine("return 0;");
        _indent.Decrease();

        output.AppendLine("}");

        return output;
    }

    private StringBuilder GenerateFunctionDefinition(StringBuilder output, Node.Declaration.FunctionDefinition functionDefinition)
    {
        GetValueOrSyntaxError(functionDefinition.Signature).MatchSome(signature => GenerateFunctionSignature(output, signature));
        _scope.Push();
        output.AppendLine(" {");
        _indent.Increase();
        functionDefinition.Signature.MatchSome(signature => {
            foreach (var parameter in signature.ParameterList.WhereSome()) {
                parameter.Type.DiscardError().FlatMap(CreateType).MatchSome(type
                 => parameter.Name.DiscardError().MatchSome(name
                  => parameter.Mode.DiscardError().MatchSome(mode
                   => {
                       bool added = _scope.TryAdd(name, new Symbol.Variable(mode != ParameterMode.In ? type.ToPointer(1) : type));
                       Debug.Assert(added, "Failed to add parameter variable to scope");
                   })));
            }
        });
        GenerateBlock(output, functionDefinition.Block);
        _indent.Decrease();
        output.AppendLine("}");
        _scope.Pop();
        return output;
    }
    private StringBuilder GenerateProcedureDefinition(StringBuilder output, Node.Declaration.ProcedureDefinition procedureDefinition)
    {
        GetValueOrSyntaxError(procedureDefinition.Signature).MatchSome(signature => GenerateProcedureSignature(output, signature));
        _scope.Push();
        _indent.Increase();
        procedureDefinition.Signature.MatchSome(signature => {
            foreach (var parameter in signature.ParameterList.WhereSome()) {
                parameter.Type.DiscardError().FlatMap(CreateType).MatchSome(type
                 => parameter.Name.DiscardError().MatchSome(name
                  => parameter.Mode.DiscardError().MatchSome(mode
                   => {
                       bool added = _scope.TryAdd(name, new Symbol.Variable(mode != ParameterMode.In ? type.ToPointer(1) : type));
                       Debug.Assert(added, "Failed to add parameter variable to scope");
                   })));
            }
        });
        GenerateBlock(output, procedureDefinition.Block);
        _indent.Decrease();
        _scope.Pop();
        return output;
    }

    private StringBuilder GenerateFunctionDeclaration(StringBuilder output, Node.Declaration.FunctionDeclaration functionDeclaration)
    {
        GetValueOrSyntaxError(functionDeclaration.Signature).MatchSome(signature => GenerateFunctionSignature(output, signature));
        output.AppendLine(";");
        return output;
    }

    private StringBuilder GenerateProcedureDeclaration(StringBuilder output, Node.Declaration.ProcedureDeclaration procedure)
    {
        GetValueOrSyntaxError(procedure.Signature).MatchSome(signature => GenerateProcedureSignature(output, signature));
        output.AppendLine(";");
        return output;
    }

    private StringBuilder GenerateFunctionSignature(StringBuilder output, Node.Declaration.FunctionSignature functionSignature)
    {
        GetValueOrSyntaxError(functionSignature.ReturnType).FlatMap(CreateType).MatchSome(type => output.Append($"{type} "));
        GetValueOrSyntaxError(functionSignature.Name).MatchSome(name => output.Append(name));
        output.Append('(');
        if (functionSignature.ParameterList.Any()) {
            output.AppendJoin(", ", GetValuesOrSyntaxErrors(functionSignature.ParameterList).Select(GenerateFormalParameterToString));
        } else {
            output.Append("void");
        }
        output.Append(')');
        return output;
    }

    private StringBuilder GenerateProcedureSignature(StringBuilder output, Node.Declaration.ProcedureSignature procedureSignature)
    {
        output.Append("void ");
        GetValueOrSyntaxError(procedureSignature.Name).MatchSome(name => output.Append(name));
        output.Append('(');
        if (procedureSignature.ParameterList.Any()) {
            output.AppendJoin(", ", GetValuesOrSyntaxErrors(procedureSignature.ParameterList).Select(GenerateFormalParameterToString));
        } else {
            output.Append("void");
        }
        output.Append(')');
        return output;
    }

    private string GenerateFormalParameterToString(Node.Declaration.FormalParameter formalParameter)
    {
        StringBuilder output = new();
        GetValueOrSyntaxError(formalParameter.Type).FlatMap(CreateType).MatchSome(type => output.Append($"{type} "));
        GetValueOrSyntaxError(formalParameter.Mode).MatchSome(mode
         => GetValueOrSyntaxError(formalParameter.Name).MatchSome(name => {
             if (mode is not ParameterMode.In) {
                 output.Append('*');
             }
             output.Append(name);
         }));
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

    private StringBuilder GenerateStatement(StringBuilder output, Node.Statement statement, IReadOnlyCollection<Token> sourceTokens) => statement switch {
        Node.Statement.Print print => GeneratePrintStatement(output, print),
        Node.Statement.Read read => GenerateReadStatement(output, read),
        Node.Statement.VariableDeclaration variableDeclaration => GenerateVariableDeclaration(output, variableDeclaration, sourceTokens),
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
        GenerateAlternativeClause(output, alternative.If, "if");

        foreach (var elseIf in GetValuesOrSyntaxErrors(alternative.ElseIfs)) {
            GenerateAlternativeClause(output, elseIf, " else if");
        }

        alternative.Else.MatchSome(elseBlock => {
            _indent.Indent(output).AppendLine(" else {");
            _indent.Increase();
            GenerateBlock(output, elseBlock);
            _indent.Decrease();
            _indent.Indent(output).AppendLine("}");
        });

        return output;
    }

    private StringBuilder GenerateAlternativeClause(StringBuilder output, Node.Statement.Alternative.Clause clause, string keyword)
    {
        _indent.Indent(output).Append($"{keyword} (");
        GetValueOrSyntaxError(clause.Condition).MatchSome(cond => GenerateExpression(output, cond));
        output.AppendLine(") {");

        _indent.Increase();
        GenerateBlock(output, clause.Block);
        _indent.Decrease();

        return _indent.Indent(output).Append('}');
    }

    private StringBuilder GenerateAssignment(StringBuilder output, Node.Statement.Assignment assignment)
    {
        _indent.Indent(output);
        GetValueOrSyntaxError(assignment.Target).Map(output.Append);
        output.Append(" = ");
        GetValueOrSyntaxError(assignment.Value).Map(expr => GenerateExpression(output, expr));
        output.AppendLine(";");

        return output;
    }

    private StringBuilder GenerateVariableDeclaration(StringBuilder output, Node.Statement.VariableDeclaration variableDeclaration, IReadOnlyCollection<Token> sourceTokens)
    {
        _indent.Indent(output);

        GetValueOrSyntaxError(variableDeclaration.Type).FlatMap(CreateType).MatchSome(variableType => {
            List<string> names = GetValuesOrSyntaxErrors(variableDeclaration.Names).ToList();
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

    private StringBuilder GenerateWhileLoop(StringBuilder output, Node.Statement.WhileLoop whileLoop)
    {
        _indent.Indent(output).Append("while ");
        GetValueOrSyntaxError(whileLoop.Condition).MatchSome(cond => GenerateExpressionEnclosedInBrackets(output, cond));
        output.AppendLine(" {");

        _indent.Increase();
        GenerateBlock(output, whileLoop.Block);
        _indent.Decrease();

        return _indent.Indent(output).AppendLine("}");
    }

    private StringBuilder GenerateDoWhileLoop(StringBuilder output, Node.Statement.DoWhileLoop doWhileLoop)
    {
        _indent.Indent(output).AppendLine("do {");

        _indent.Increase();
        GenerateBlock(output, doWhileLoop.Block);
        _indent.Decrease();

        _indent.Indent(output).Append("} while ");
        GetValueOrSyntaxError(doWhileLoop.Condition).MatchSome(cond => GenerateExpressionEnclosedInBrackets(output, cond));
        output.AppendLine(";");

        return output;
    }

    private StringBuilder GenerateRepeatLoop(StringBuilder output, Node.Statement.RepeatLoop repeatLoop)
    {
        _indent.Indent(output).AppendLine("do {");

        _indent.Increase();
        GenerateBlock(output, repeatLoop.Block);
        _indent.Decrease();

        _indent.Indent(output).Append("} while (!");
        GetValueOrSyntaxError(repeatLoop.Condition).MatchSome(cond => GenerateExpressionEnclosedInBrackets(output, cond));
        output.AppendLine(");");

        return output;
    }

    private StringBuilder GenerateForLoop(StringBuilder output, Node.Statement.ForLoop forLoop)
    {
        string variant = GetValueOrSyntaxError(forLoop.VariantName).ValueOr("");

        _indent.Indent(output).Append($"for ({variant} = ");
        GetValueOrSyntaxError(forLoop.Start).MatchSome(start => GenerateExpression(output, start));

        output.Append($"; {variant} <= ");
        GetValueOrSyntaxError(forLoop.End).MatchSome(end => GenerateExpression(output, end));

        output.Append($"; {variant}");
        forLoop.Step.Match(
            step => GenerateExpression(output.Append(" += "), step),
            none: () => output.Append("++"));
        output.AppendLine(") {");

        _indent.Increase();
        GenerateBlock(output, forLoop.Block);
        _indent.Decrease();

        return _indent.Indent(output).AppendLine("}");
    }

    private StringBuilder GenerateReturn(StringBuilder output, Node.Statement.Return @return)
    {
        _indent.Indent(output).Append($"return ");
        GetValueOrSyntaxError(@return.Value).MatchSome(returnValue => GenerateExpression(output, returnValue));
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
        Node.Expression.Bracketed bracketed => GetValueOrSyntaxError(bracketed.Expression)
            .Map(expr => GenerateExpression(output.Append('('), expr).Append(')')).ValueOr(output),
        Node.Expression.Variable variable => output.Append(variable.Name),
        _ => throw expression.ToUnmatchedException(),
    };

    private string GenerateExpressionToString(Node.Expression expr) => GenerateExpression(new(), expr).ToString();

    private StringBuilder GenerateArraySubscript(StringBuilder output, Node.Expression.ArraySubscript arraySubscript)
    {
        GetValueOrSyntaxError(arraySubscript.Array).MatchSome(array => GenerateExpression(output, array));

        foreach (Node.Expression index in GetValuesOrSyntaxErrors(arraySubscript.Indices)) {
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
        GetValueOrSyntaxError(operation.Operand1).MatchSome(op1 => GenerateExpression(output, op1));
        output.Append($" {GetOperationOperator(operation.Operator)} ");
        GetValueOrSyntaxError(operation.Operand2).MatchSome(op2 => GenerateExpression(output, op2));
        return output;
    }

    private StringBuilder GenerateOperationUnary(StringBuilder output, Node.Expression.OperationUnary operation)
    {
        output.Append(GetOperationOperator(operation.Operator));
        GetValueOrSyntaxError(operation.Operand).MatchSome(op1 => GenerateExpression(output, op1));
        return output;
    }

    #endregion Expressions

    #region Helpers

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
        Node.Type.String => TypeInfo.CreateString().Some(),
        Node.Type.Primitive p => TypeInfo.Primitive.Create(p.Type).Some(),
        Node.Type.AliasReference alias => GetValueOrSyntaxError(alias.Name)
            .FlatMap(aliasName => _scope.GetSymbol<Symbol.TypeAlias>(aliasName)
                .Map(aliasType => TypeInfo.CreateAlias(aliasName, aliasType.TargetType))),
        Node.Type.Array array => GetValueOrSyntaxError(array.Type).FlatMap(CreateType).Map(elementType
         => TypeInfo.CreateArray(elementType, GetValuesOrSyntaxErrors(array.Dimensions).Select(GenerateExpressionToString))),
        Node.Type.LengthedString str => GetValueOrSyntaxError(str.Length).Map(length
         => EvaluateValue(length)
            .FlatMap(Parse.ToInt32)
            .Map(Convert.ToString)
            .Map(TypeInfo.CreateLengthedString)
            .ValueOr(() => TypeInfo.CreateLengthedString(GenerateExpressionToString(length)))),
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

    private Option<T> GetValueOrSyntaxError<T>(ParseResult<T> result)
    {
        if (result.HasValue) {
            return result.Value.Some();
        }

        AddMessage(Message.SyntaxError<T>(result.SourceTokens, result.Error));

        return Option.None<T>();
    }

    /// <remarks>The returned collection should only be enumerated once, otherwise we may get duplicate errors.</remarks>
    private IEnumerable<T> GetValuesOrSyntaxErrors<T>(IEnumerable<ParseResult<T>> results)
     => results.Select(GetValueOrSyntaxError).WhereSome();

    #region Terminals

    #endregion Terminals

}
