using System.Globalization;
using System.Text;
using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

internal partial class CodeGeneratorC
{
    private (string format, IReadOnlyList<Expression> arguments) BuildFormatString(IEnumerable<Expression> parts)
    {
        List<Expression> arguments = [];
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Expression.Literal literal) {
                format.Append(FormatValue(literal.Value)
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else if (_ast.InferredTypes.TryGetValue(part, out var type)) {
                    CreateTypeInfo(type).FormatComponent.Match(fmtComp => {
                        format.Append(fmtComp);
                        arguments.Add(part);
                    }, () => _messenger.Report(Message.ErrorTargetLanguage("C", part.SourceTokens,
                             $"type '{type}' cannot be used in a format string")));
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
