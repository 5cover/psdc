using System.Globalization;
using System.Text;
using Scover.Psdc.Language;
using Scover.Psdc.Messages;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

partial class CodeGeneratorC
{
    (string format, IReadOnlyList<Expression> arguments) BuildFormatString(IEnumerable<Expression> parts)
    {
        List<Expression> arguments = [];
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Expression.Literal literal) {
                format.Append(literal.ActualValue.ToString(CultureInfo.InvariantCulture)
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                _ast.InferredTypes[part].MatchSome(t => CreateTypeInfo(t).FormatComponent.Match(fmtComp => {
                    format.Append(fmtComp);
                    arguments.Add(part);
                }, () => _msger.Report(Message.ErrorTargetLanguage("C", part.SourceTokens,
                    $"type '{t}' cannot be used in a format string"))));
            }
        }

        return (format.ToString(), arguments);
    }
}
