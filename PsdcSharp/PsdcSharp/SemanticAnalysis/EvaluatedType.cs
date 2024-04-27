using System.Text;
using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.StaticAnalysis;

internal abstract record EvaluatedType(bool IsNumeric = false)
{
    /// <summary>
    /// Gets the Pseudocode representation of this evaluated type.
    /// </summary>
    /// <param name="input">The input code (used for retrieving code extracts from source tokens)</param>
    /// <returns>The Pseudocode code that would result in an equals <see cref="EvaluatedType"/> object if parsed.</returns>
    public abstract string GetRepresentation(string input);

    internal sealed record String : EvaluatedType
    {
        private String() : base(false) { }

        public static String Instance { get; } = new();

        public override string GetRepresentation(string input) => "chaîne";
    }

    internal sealed record Array(EvaluatedType ElementType, IReadOnlyCollection<Node.Expression> Dimensions) : EvaluatedType
    {
        public bool Equals(Array? other) => other is not null
         && other.ElementType.Equals(ElementType)
         && other.Dimensions.SequenceEqual(Dimensions);

        public override int GetHashCode() => HashCode.Combine(ElementType, Dimensions.GetSequenceHashCode());

        public override string GetRepresentation(string input)
         => $"tableau [{string.Join(", ", Dimensions.Select(dim => dim.SourceTokens.GetSourceCode(input)))}] de {ElementType.GetRepresentation(input)}";
    }

    internal sealed record Primitive(PrimitiveType Type) : EvaluatedType(Type is not PrimitiveType.File)
    {
        public override string GetRepresentation(string input) => Type switch {
            PrimitiveType.Boolean => "booléen",
            PrimitiveType.Character => "caractère",
            PrimitiveType.Integer => "entier",
            PrimitiveType.File => "nomFichierLog",
            PrimitiveType.Real => "réel",
            _ => throw Type.ToUnmatchedException(),
        };
    }

    internal sealed record AliasReference(Node.Identifier Name, EvaluatedType Target) : EvaluatedType
    {
        public override string GetRepresentation(string input) => Name.Name;
    }

    internal sealed record LengthedString(Node.Expression Length) : EvaluatedType
    {
        public override string GetRepresentation(string input) => $"chaîne({Length.SourceTokens.GetSourceCode(input)})";
    }

    internal sealed record StringLengthedKnown(int Length) : EvaluatedType
    {
        public override string GetRepresentation(string input) => $"chaîne({Length})";
    }

    internal sealed record Structure(IReadOnlyDictionary<Node.Identifier, EvaluatedType> Components) : EvaluatedType
    {
        public bool Equals(Structure? other) => other is not null
         && other.Components.SequenceEqual(Components);

        public override int GetHashCode() => Components.GetSequenceHashCode();

        public override string GetRepresentation(string input)
        {
            StringBuilder sb = new();
            sb.AppendLine("structure début");
            foreach (var comp in Components) {
                sb.AppendLine($"{comp.Key} : {comp.Value.GetRepresentation(input)};");
            }
            sb.Append("fin");

            return sb.ToString();
        }
    }
}
