using System.Diagnostics;

namespace Scover.Psdc.Parsing;

public sealed class Ident : Node, IEquatable<Ident?>
{
    public Ident(Range location, string name)
    {
        Debug.Assert(
            name.Length > 0
                && (name[0] == '_' || char.IsLetter(name[0]))
                && name.Skip(1).All(c => c == '_' || char.IsLetterOrDigit(c)),
            $"`{name}` is not a valid identifier");
        (Location, Name) = (location, name);
    }

    public Range Location { get; }
    public string Name { get; }

    // Equals and GetHashCode implementation for usage in dictionaries.
    public override bool Equals(object? obj) => Equals(obj as Ident);
    public bool Equals(Ident? other) => other is not null && other.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}
