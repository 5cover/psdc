using System.Text;

namespace Scover.Psdc.CodeGeneration;

internal sealed class IncludeSet
{
    public const string StdBool = "<stdbool.h>";
    public const string StdIo = "<stdio.h>";
    public const string StdLib = "<stdlib.h>";

    private readonly HashSet<string> _headers = [];

    public void Ensure(string name) => _headers.Add(name);

    public StringBuilder AppendIncludeSection(StringBuilder o)
    {
        if (_headers.Count == 0) {
            return o;
        }

        o.AppendLine();
        foreach (string header in _headers) {
            o.AppendLine($"#include {header}");
        }
        return o.AppendLine();
    }
}
