using System.Diagnostics;

using Scover.Psdc.Pseudocode;

namespace Scover.Psdc.Parsing;

public readonly record struct Ident : Node
{
    public Ident(string name)
    {
        Debug.Assert(
            name.Length > 0
         && (name[0] == '_' || char.IsLetter(name[0]))
         && name.Skip(1).All(c => c == '_' || char.IsLetterOrDigit(c)),
            $"`{name}` is not a valid identifier");
        Name = name;
    }

    public string Name { get; }

    public string WrapInCode() => Name.WrapInCode();
}
