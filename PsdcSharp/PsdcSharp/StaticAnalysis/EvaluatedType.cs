using System.Text;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal interface EvaluatedType
{
    /// <summary>
    /// Gets the Pseudocode representation of this evaluated type.
    /// </summary>
    /// <param name="input">The input code (used for retrieving code extracts from source tokens)</param>
    /// <returns>The Pseudocode code that would result in an equals <see cref="EvaluatedType"/> object if parsed.</returns>
    public string GetRepresentation(string input);

    internal sealed class String : EvaluatedType
    {
        private String() { }

        public static String Instance { get; } = new();

        public string GetRepresentation(string input) => "chaîne";
    }

    internal sealed record Array(EvaluatedType ElementType, IReadOnlyCollection<Node.Expression> Dimensions) : EvaluatedType
    {
        public bool Equals(Array? other) => other is not null
         && other.ElementType.Equals(ElementType)
         && other.Dimensions.AllSemanticsEqual(Dimensions);

        public override int GetHashCode() => HashCode.Combine(ElementType, Dimensions.GetSequenceHashCode());

        public string GetRepresentation(string input)
         => $"tableau [{string.Join(", ", Dimensions.Select(dim => dim.SourceTokens.GetSourceCode(input)))}] de {ElementType.GetRepresentation(input)}";
    }

    internal sealed class File : EvaluatedType
    {
        private File() { }
        public static File Instance { get; } = new();
        public string GetRepresentation(string input) => "nomFichierLog";
    }

    internal sealed class Numeric : EvaluatedType
    {
        private readonly string _representation;

        // Precision represents how many values the type can represent.
        // The higher, the wider the value range.
        private readonly int _precision;
        private Numeric(NumericType type, int precision, string representation)
         => (Type, _precision, _representation) = (type, precision, representation);

        public NumericType Type { get; }
        public static Numeric GetInstance(NumericType type) => instances.First(i => i.Type == type);

        private static readonly Numeric[] instances = [
           new(NumericType.Boolean, 0, "booléen"),
           new(NumericType.Character, 1, "caractère"),
           new(NumericType.Integer, 2, "entier"),
           new(NumericType.Real, 3, "réel"),
        ];

        public static EvaluatedType GetMostPreciseType(Numeric t1, Numeric t2)
        => t1._precision > t2._precision ? t1 : t2;

        public string GetRepresentation(string input) => _representation;
    }

    internal sealed record AliasReference(Identifier Name, EvaluatedType Target) : EvaluatedType
    {
        public string GetRepresentation(string input) => Name.Name;
    }

    internal sealed record LengthedString(Node.Expression Length) : EvaluatedType
    {
        public string GetRepresentation(string input) => $"chaîne({Length.SourceTokens.GetSourceCode(input)})";
    }

    internal sealed record StringLengthedKnown(int Length) : EvaluatedType
    {
        public string GetRepresentation(string input) => $"chaîne({Length})";
    }

    internal sealed record Structure(IReadOnlyDictionary<Identifier, EvaluatedType> Components) : EvaluatedType
    {
        public bool Equals(Structure? other) => other is not null
         && other.Components.SequenceEqual(Components);

        public override int GetHashCode() => Components.GetSequenceHashCode();

        public string GetRepresentation(string input)
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
