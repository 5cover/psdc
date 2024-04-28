using System.Text;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal interface EvaluatedType : EquatableSemantics<EvaluatedType>
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

    internal sealed class Unknown(SourceTokens sourceTokens) : EvaluatedType
    {
        public string Representation => sourceTokens.SourceCode;

        // Unknown types are semantically equal to every other type.
        // This is to prevent cascading errors when an object of an unknown type is used.
        public bool SemanticsEqual(EvaluatedType other) => true;
    }

    internal sealed class String : EvaluatedType
    {
        private String() { }

        public static String Instance { get; } = new();

        public string Representation => "chaîne";

        public bool SemanticsEqual(EvaluatedType other) => other is String;
    }

    internal sealed record Array(EvaluatedType ElementType, IReadOnlyCollection<Node.Expression> Dimensions) : EvaluatedType
    {
        public override int GetHashCode() => HashCode.Combine(ElementType, Dimensions.GetSequenceHashCode());
        public bool SemanticsEqual(EvaluatedType other) => other is Array o
         && o.ElementType.SemanticsEqual(ElementType)
         && o.Dimensions.AllSemanticsEqual(Dimensions);

        public string Representation
         => $"tableau [{string.Join(", ", Dimensions.Select(dim => dim.SourceTokens.SourceCode))}] de {ElementType.Representation}";
    }

    internal sealed class File : EvaluatedType
    {
        private File() { }
        public static File Instance { get; } = new();
        public string Representation => "nomFichierLog";

        public bool SemanticsEqual(EvaluatedType other) => other is File;
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
        public bool SemanticsEqual(EvaluatedType other) => other is Numeric o
         && o.Type == Type;

        public string Representation { get; }
    }

    internal sealed record AliasReference(Identifier Name, EvaluatedType Target) : EvaluatedType
    {
        public string Representation => Name.Name;

        public bool SemanticsEqual(EvaluatedType other) => other is AliasReference o
         && o.Name.SemanticsEqual(Name)
         && o.Target.SemanticsEqual(Target);
    }

    internal sealed record LengthedString(Node.Expression Length) : EvaluatedType
    {
        public string Representation => $"chaîne({Length.SourceTokens.SourceCode})";

        public bool SemanticsEqual(EvaluatedType other) => other is LengthedString o
         && o.Length.SemanticsEqual(Length);
    }

    internal sealed record StringLengthedKnown(int Length) : EvaluatedType
    {
        public string Representation => $"chaîne({Length})";
        
        public bool SemanticsEqual(EvaluatedType other) => other is StringLengthedKnown o
         && o.Length == Length;
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
        public bool SemanticsEqual(EvaluatedType other) => other is Structure o
         && o.Components.Keys.AllSemanticsEqual(Components.Keys)
         && o.Components.Values.AllSemanticsEqual(Components.Values);

        public string Representation => _representation.Value;
    }
}
