using System.Text;
using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.SemanticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal partial class CodeGeneratorC
{
    private (string format, IReadOnlyList<Node.Expression> arguments) BuildFormatString(
        ReadOnlyScope scope, IEnumerable<Node.Expression> parts)
    {
        List<Node.Expression> arguments = [];
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Node.Expression.Literal literal) {
                format.Append(literal.Value
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                part.EvaluateType(scope).MatchSome(partType => {
                    CreateTypeInfo(partType).FormatComponent.MatchSome(c => format.Append(c));
                    arguments.Add(part);
                });
            }
        }

        return (format.ToString(), arguments);
    }
}
