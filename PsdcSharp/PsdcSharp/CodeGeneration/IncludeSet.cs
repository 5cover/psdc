using System.Text;

namespace Scover.Psdc.CodeGeneration;

internal sealed class IncludeSet
{
    public const string StdBool = "<stdbool.h>";
    public const string StdIo = "<stdio.h>";
    public const string StdLib = "<stdlib.h>";

    private readonly HashSet<string> _headers = new();

    public void Ensure(string name) => _headers.Add(name);

    public StringBuilder AppendTo(StringBuilder output)
    {
        foreach (string header in _headers) {
            output.AppendLine($"#include {header}");
        }
        return output;
    }
}
