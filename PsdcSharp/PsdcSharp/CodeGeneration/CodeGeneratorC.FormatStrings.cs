using System.Globalization;
using System.Text;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration;

internal partial class CodeGeneratorC
{
    private (string format, IReadOnlyList<Expression> arguments) BuildFormatString(
        ReadOnlyScope scope, IEnumerable<Expression> parts)
    {
        List<Expression> arguments = [];
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Expression.Literal literal) {
                format.Append(FormatValue(literal.Value)
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                part.EvaluateType(scope)
                    .Match(none: _messenger.Report,
                    some: type => CreateTypeInfo(type).FormatComponent.Match(
                    fmtComp => {
                        format.Append(fmtComp);
                        arguments.Add(part);
                    }, () => _messenger.Report(Message.ErrorTargetLanguage("C", part.SourceTokens,
                        $"type {type} does not have a format component and as such cannot be used in a format string."))));
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
