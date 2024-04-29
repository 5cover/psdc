using System.Text;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal partial class CodeGeneratorC
{
    private (string format, IReadOnlyList<Node.Expression> arguments) BuildFormatString(
        ReadOnlyScope scope, IEnumerable<Node.Expression> parts)
    {
        List<Node.Expression> arguments = [];
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Node.Expression.Literal<object> literal) {
                format.Append((literal.Value.ToString() ?? "")
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                part.EvaluateType(scope).MapError(_messenger.Report).MatchSome(partType => {
                    CreateTypeInfo(partType).FormatComponent.MatchSome(c => format.Append(c));
                    arguments.Add(part);
                });
            }
        }

        return (format.ToString(), arguments);
    }
}
