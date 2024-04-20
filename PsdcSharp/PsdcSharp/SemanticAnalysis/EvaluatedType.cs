using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.SemanticAnalysis;

internal abstract record EvaluatedType(bool IsNumeric = false)
{
    internal sealed record String : EvaluatedType
    {
        private String() : base(false) { }

        public static String Instance { get; } = new();
    }

    internal sealed record Array(EvaluatedType Type, IReadOnlyCollection<Node.Expression> Dimensions) : EvaluatedType
    {
        public bool Equals(Array? other) => other is not null
         && other.Type.Equals(Type)
         && other.Dimensions.SequenceEqual(Dimensions);

        public override int GetHashCode() => HashCode.Combine(Type, Dimensions.GetSequenceHashCode());
    }

    internal sealed record Primitive(PrimitiveType Type) : EvaluatedType(Type is not PrimitiveType.File);

    internal sealed record AliasReference(string Name, EvaluatedType Target) : EvaluatedType;

    internal sealed record StringLengthed(Node.Expression Length) : EvaluatedType;
    internal sealed record StringLengthedKnown(int Length) : EvaluatedType;

    internal sealed record StructureDefinition(IReadOnlyDictionary<string, EvaluatedType> Components) : EvaluatedType
    {
        public bool Equals(StructureDefinition? other) => other is not null
         && other.Components.SequenceEqual(Components);

        public override int GetHashCode() => Components.GetSequenceHashCode();
    }
}
