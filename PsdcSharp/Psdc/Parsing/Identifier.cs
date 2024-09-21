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
        (SourceTokens, Value) = (sourceTokens, name);
    }

    public SourceTokens SourceTokens { get; }
    public string Value { get; }

    public bool SemanticsEqual(Node other) => Equals(other as Identifier);

    // Equals and GetHashCode implementation for usage in dictionaries.
    public override bool Equals(object? obj) => Equals(obj as Identifier);
    public bool Equals(Identifier? other) => other is not null && other.Value == Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}
