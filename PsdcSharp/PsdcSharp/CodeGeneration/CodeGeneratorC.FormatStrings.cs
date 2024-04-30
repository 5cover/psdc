using System.Globalization;
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
            if (part is Node.Expression.Literal literal) {
                format.Append(FormatValue(literal.Value)
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                part.EvaluateType(scope).MatchError(_messenger.Report).MatchSome(partType => {
                    CreateTypeInfo(partType).FormatComponent.MatchSome(c => format.Append(c));
                    arguments.Add(part);
                });
            }
        }

        return (format.ToString(), arguments);
    }

    private static string FormatValue(ConstantValue value) => value switch {
        ConstantValue.Integer i => i.Value.ToString(CultureInfo.InvariantCulture),
        ConstantValue.Real i => i.Value.ToString(CultureInfo.InvariantCulture),
        ConstantValue.Boolean i => i.Value.ToString(CultureInfo.InvariantCulture),
        ConstantValue.Character i => i.Value.ToString(CultureInfo.InvariantCulture),
        ConstantValue.String i => i.Value.ToString(CultureInfo.InvariantCulture),
        _ => throw value.ToUnmatchedException(),
    };
}
