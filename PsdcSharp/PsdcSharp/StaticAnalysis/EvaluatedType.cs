using System.Text;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal interface EvaluatedType
{
    /// <summary>
    /// Gets the Pseudocode representation of this evaluated type.
    /// </summary>
    /// <value>The Pseudocode code that would result in an equal <see cref="EvaluatedType"/> object if parsed.</value>
    public string Representation { get; }

    public EvaluatedType Unwrap()
    {
        switch (this) {
        case AliasReference alias: {
                var target = alias.Target;
                while (target is AliasReference a) {
                    target = a.Target;
                }
                return target;
            }
        default:
            return this;
        }
    }

    internal sealed record Unknown(SourceTokens SourceTokens) : EvaluatedType
    {
        public string Representation => SourceTokens.SourceCode;
    }

    internal sealed class String : EvaluatedType
    {
        private String() { }

        public static String Instance { get; } = new();

        public string Representation => "chaîne";
    }

    internal sealed record Array(EvaluatedType ElementType, IReadOnlyCollection<Node.Expression> Dimensions) : EvaluatedType
    {
        public bool Equals(Array? other) => other is not null
         && other.ElementType.Equals(ElementType)
         && other.Dimensions.AllSemanticsEqual(Dimensions);

        public override int GetHashCode() => HashCode.Combine(ElementType, Dimensions.GetSequenceHashCode());

        public string Representation
         => $"tableau [{string.Join(", ", Dimensions.Select(dim => dim.SourceTokens.SourceCode))}] de {ElementType.Representation}";
    }

    internal sealed class File : EvaluatedType
    {
        private File() { }
        public static File Instance { get; } = new();
        public string Representation => "nomFichierLog";
    }

    internal sealed class Numeric : EvaluatedType
    {

        // Precision represents how many values the type can represent.
        // The higher, the wider the value range.
        private readonly int _precision;
        private Numeric(NumericType type, int precision, string representation)
         => (Type, _precision, Representation) = (type, precision, representation);

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

        public string Representation { get; }
    }

    internal sealed record AliasReference(Identifier Name, EvaluatedType Target) : EvaluatedType
    {
        public string Representation => Name.Name;
    }

    internal sealed record LengthedString(Node.Expression Length) : EvaluatedType
    {
        public string Representation => $"chaîne({Length.SourceTokens.SourceCode})";
    }

    internal sealed record StringLengthedKnown(int Length) : EvaluatedType
    {
        public string Representation => $"chaîne({Length})";
    }

    internal sealed record Structure(IReadOnlyDictionary<Identifier, EvaluatedType> Components) : EvaluatedType
    {
        private readonly Lazy<string> _representation = new(() => {
            StringBuilder sb = new();
            sb.AppendLine("structure début");
            foreach (var comp in Components) {
                sb.AppendLine($"{comp.Key} : {comp.Value.Representation};");
            }
            sb.Append("fin");

            return sb.ToString();
        });

        public bool Equals(Structure? other) => other is not null
         && other.Components.SequenceEqual(Components);

        public override int GetHashCode() => Components.GetSequenceHashCode();

        public string Representation => _representation.Value;
    }
}
