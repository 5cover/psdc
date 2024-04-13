using System.Text;
using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.CodeGeneration;

internal partial class CodeGeneratorC
{
    private (string format, IReadOnlyList<Node.Expression> arguments) BuildFormatString(
        IEnumerable<Node.Expression> parts)
    {
        List<Node.Expression> arguments = new();
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Node.Expression.Literal literal) {
                format.Append(literal.Value
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                part.EvaluateType(_scope).MatchSome(partType => {
                    partType.FormatComponent.MatchSome(c => format.Append(c));
                    arguments.Add(part);
                });
            }
        }

        return (format.ToString(), arguments);
    }
}
