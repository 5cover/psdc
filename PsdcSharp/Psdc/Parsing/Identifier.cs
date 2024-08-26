using System.Diagnostics;

namespace Scover.Psdc.Parsing;

public sealed class Identifier : Node, IEquatable<Identifier?>
{
    public Identifier(SourceTokens sourceTokens, string name)
    {
        Debug.Assert(
            name.Length > 0
                && (name[0] == '_' || char.IsLetter(name[0]))
                && name.Skip(1).All(c => c == '_' || char.IsLetterOrDigit(c)),
            $"`{name}` is not a valid identifier");
        (SourceTokens, _name) = (sourceTokens, name);
    }

    public SourceTokens SourceTokens { get; }
    private readonly string _name;

    public bool SemanticsEqual(Node other) => Equals(other as Identifier);

    // Equals and GetHashCode implementation for usage in dictionaries.
    public override bool Equals(object? obj) => Equals(obj as Identifier);
    public bool Equals(Identifier? other) => other is not null && other._name == _name;
    public override int GetHashCode() => _name.GetHashCode();
    public override string ToString() => _name;
}
