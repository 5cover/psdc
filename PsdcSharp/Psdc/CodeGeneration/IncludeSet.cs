using System.Text;

namespace Scover.Psdc.CodeGeneration;

sealed class IncludeSet
{
    public const string StdBool = "<stdbool.h>";
    public const string StdIo = "<stdio.h>";
    public const string StdLib = "<stdlib.h>";
    public const string String = "<string.h>";

    readonly HashSet<string> _headers = [];

    public StringBuilder AppendIncludeSection(StringBuilder o)
    {
        if (_headers.Count == 0) {
            return o;
        }

        foreach (string header in _headers) {
            o.AppendLine(Format.Code, $"#include {header}");
        }
        return o;
    }

    public void Ensure(string name) => _headers.Add(name);
}
