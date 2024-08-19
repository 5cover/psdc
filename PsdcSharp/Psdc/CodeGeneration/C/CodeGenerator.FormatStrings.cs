using System.Text;
using Scover.Psdc.Messages;

using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.CodeGeneration.C;

partial class CodeGenerator
{
    (string format, IReadOnlyList<Expression> arguments) BuildFormatString(IEnumerable<Expression> parts)
    {
        List<Expression> arguments = [];
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Expression.Literal l) {
                format.Append(l.UnderlyingValue.ToStringFmt(Format.Code)?
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                CreateTypeInfo(part.Meta.Scope, part.Value.Type).FormatComponent.Tap(fmtComp => {
                    format.Append(fmtComp);
                    arguments.Add(part);
                }, () => _msger.Report(Message.ErrorTargetLanguageFormat(part.Meta.SourceTokens, LanguageName.C,
                    $"type '{part.Value.Type}' cannot be used in a format string")));
            }
        }

        return (format.ToString(), arguments);
    }
}
