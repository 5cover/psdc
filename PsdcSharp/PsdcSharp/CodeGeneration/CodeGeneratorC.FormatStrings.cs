using System.Text;

using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.CodeGeneration;

internal partial class CodeGeneratorC
{
    private (string format, IReadOnlyList<Node.Expression> arguments) BuildFormatString(
        IEnumerable<ParseResult<Node.Expression>> parts)
    {
        List<Node.Expression> arguments = new();
        StringBuilder format = new();

        foreach (var partNode in parts) {
            GetValueOrSyntaxError(partNode).MatchSome(part => {
                if (part is Node.Expression.Literal literal) {
                    format.Append(literal.Value.Replace("%", "%%"));
                } else {
                    part.EvaluateType(_scope).Match(partType => {
                        partType.FormatComponent.MatchSome(c => format.Append(c));
                        arguments.Add(part);
                    }, error => AddMessage(error(partNode.SourceTokens)));
                }
            });
        }

        return (format.ToString(), arguments);
    }
}
