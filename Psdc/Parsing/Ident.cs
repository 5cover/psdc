using System.Diagnostics;
using Scover.Psdc.Lexing;
using Scover.Psdc.Pseudocode;

namespace Scover.Psdc.Parsing;

public readonly record struct Ident : Node
{
    public Ident(string value)
    {
        Debug.Assert(
            value.Length > 0
         && (value[0] == '_' || char.IsLetter(value[0]))
         && value.Skip(1).All(c => c == '_' || char.IsLetterOrDigit(c)),
            $"`{value}` is not a valid identifier");
        Value = value;
    }

    public Ident(Token token) : this((string)token.Value.NotNull())
    {

    }

    public string Value { get; }

    public string WrapInCode() => Value.WrapInCode();
}
