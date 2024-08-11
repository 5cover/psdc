using System.Diagnostics;

namespace Scover.Psdc.Parsing;

public sealed class Identifier : Node, IEquatable<Identifier?>
{
    public Identifier(SourceTokens sourceTokens, string name)
    {
        (SourceTokens, Name) = (sourceTokens, name);
        Debug.Assert(
            name.Length > 0
                && (name[0] == '_' || char.IsLetter(name[0]))
                && name.Skip(1).All(c => c == '_' || char.IsLetterOrDigit(c)),
            $"`{name}` is not a valid identifier");
    }

    public SourceTokens SourceTokens { get; }
    public string Name { get; }

    public bool SemanticsEqual(Node other) => other is Identifier o
     && o.Name == Name;

    // Equals and GetHashCode implementation for usage in dictionaries.
    public override bool Equals(object? obj) => Equals(obj as Identifier);

    public bool Equals(Identifier? other) => other is not null && other.Name == Name;

    public override int GetHashCode() => Name.GetHashCode();

    public override string ToString() => Name;
}
