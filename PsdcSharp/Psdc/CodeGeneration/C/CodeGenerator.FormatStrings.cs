using System.Globalization;
using System.Text;
using Scover.Psdc.Messages;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

partial class CodeGenerator
{
    (string format, IReadOnlyList<Expression> arguments) BuildFormatString(IEnumerable<Expression> parts)
    {
        List<Expression> arguments = [];
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Expression.Literal literal) {
                format.Append(literal.Value.ToString(CultureInfo.InvariantCulture)
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                CreateTypeInfo(_ast.InferredTypes[part]).FormatComponent.Match(fmtComp => {
                    format.Append(fmtComp);
                    arguments.Add(part);
                }, () => _msger.Report(Message.ErrorTargetLanguage(part.SourceTokens, LanguageName.C,
                    $"type '{_ast.InferredTypes[part]}' cannot be used in a format string")));
            }
        }

        return (format.ToString(), arguments);
    }
}
