using System.Text;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

/// <summary>
/// A type evaluated during the static analysis.
/// </summary>
internal abstract class EvaluatedType(Identifier? alias) : EquatableSemantics<EvaluatedType>
{
    public abstract EvaluatedType ToAliasReference(Identifier alias);

    public override string ToString() => Representation;

    /// <summary>
    /// Gets the Pseudocode representation of this type.
    /// </summary>
    /// <value>The Pseudocode code that would result in an equal <see cref="EvaluatedType"/> object if parsed.</value>
    public string Representation => Alias?.Name ?? ActualRepresentation;

    protected abstract string ActualRepresentation { get; }

    /// <summary>
    /// Gets the alias used to refer to this type indirectly.
    /// </summary>
    /// <value>The alias used to refer to this type indirectly or <see langword="null"/> if this type is not aliased.</value>
    public Identifier? Alias { get; } = alias;

    public abstract bool SemanticsEqual(EvaluatedType other);

    /// <summary>
    /// Determines whether a value of this type can be assigned to a variable of type <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The type to compare with the current type</param>
    /// <returns>Whether a value of this type can be assigned to a variable of type <paramref name="other"/>.</returns>
    public virtual bool IsAssignableTo(EvaluatedType other) => SemanticsEqual(other);

    internal sealed class Unknown(SourceTokens sourceTokens, Identifier? alias = null) : EvaluatedType(alias)
    {
        protected override string ActualRepresentation => sourceTokens.SourceCode;

        // Unknown types are semantically equal to every other type.
        // This is to prevent cascading errors when an object of an unknown type is used.
        public override bool SemanticsEqual(EvaluatedType other) => true;
        public override EvaluatedType ToAliasReference(Identifier alias) => new Unknown(sourceTokens, alias);
    }

    internal class String : EvaluatedType
    {
        private String(Identifier? alias) : base(alias) { }

        public static String Instance { get; } = new(null);

        protected override string ActualRepresentation => "chaîne";

        public override bool SemanticsEqual(EvaluatedType other) => other is String;

        public override EvaluatedType ToAliasReference(Identifier alias) => new String(alias);
    }

    internal sealed class Array : EvaluatedType
    {
        public static Option<Array, IEnumerable<Message>> Create(EvaluatedType elementType, IReadOnlyCollection<Node.Expression> dimensionsExpressions, ReadOnlyScope scope)
         => dimensionsExpressions.Select(d => d.EvaluateValue<Node.Expression.Literal.Integer, int>(scope)).Accumulate()
         .Map(dimensions => new Array(elementType, dimensionsExpressions, dimensions.ToList(), null));

        private Array(EvaluatedType elementType, IReadOnlyCollection<Node.Expression> dimensionExprs, IReadOnlyCollection<int> dimensions, Identifier? alias) : base(alias)
         => (ElementType, DimensionExpressions, Dimensions) = (elementType, dimensionExprs, dimensions);

        public EvaluatedType ElementType { get; }
        public IReadOnlyCollection<Node.Expression> DimensionExpressions { get; }
        public IReadOnlyCollection<int> Dimensions { get; }

        public override bool SemanticsEqual(EvaluatedType other) => other is Array o
         && o.ElementType.SemanticsEqual(ElementType)
         && o.Dimensions.AllZipped(Dimensions, (od, d) => od == d);

        protected override string ActualRepresentation
         => $"tableau [{string.Join(", ", Dimensions)}] de {ElementType.ActualRepresentation}";

        public override EvaluatedType ToAliasReference(Identifier alias)
         => new Array(ElementType, DimensionExpressions, Dimensions, alias);

        // Arrays can't be reassigned.
        public override bool IsAssignableTo(EvaluatedType other) => false;
    }

    internal sealed class File : EvaluatedType
    {
        private File(Identifier? alias) : base(alias) { }
        public static File Instance { get; } = new(null);
        protected override string ActualRepresentation => "nomFichierLog";

        public override bool SemanticsEqual(EvaluatedType other) => other is File;

        public override EvaluatedType ToAliasReference(Identifier alias) => new File(alias);
    }

    internal sealed class Numeric : EvaluatedType
    {
        // Precision represents how many values the type can represent.
        // The higher, the wider the value range.
        private readonly int _precision;
        private Numeric(NumericType type, int precision, string representation, Identifier? alias = null) : base(alias)
         => (Type, _precision, ActualRepresentation) = (type, precision, representation);

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
        public override bool SemanticsEqual(EvaluatedType other) => other is Numeric o
         && o.Type == Type;
        public override bool IsAssignableTo(EvaluatedType other) => other is Numeric o
         && o._precision >= _precision;
        public override EvaluatedType ToAliasReference(Identifier alias) => new Numeric(Type, _precision, ActualRepresentation, alias);

        protected override string ActualRepresentation { get; }
    }

    internal sealed class StringLengthed : EvaluatedType
    {
        public static Option<StringLengthed, Message> Create(Node.Expression lengthExpression, ReadOnlyScope scope)
         => lengthExpression.EvaluateValue<Node.Expression.Literal.Integer, int>(scope).Map(l => new StringLengthed(lengthExpression.Some(), l, null));

        public static StringLengthed Create(int length)
         => new(Option.None<Node.Expression>(), length, null);

        private StringLengthed(Option<Node.Expression> lengthExpression, int length, Identifier? alias) : base(alias)
         => (LengthExpression, Length) = (lengthExpression, length);

        public Option<Node.Expression> LengthExpression { get; }
        public int Length { get; }

        protected override string ActualRepresentation => $"chaîne({Length})";

        public override bool SemanticsEqual(EvaluatedType other) => other is StringLengthed o
         && o.Length == Length;

        public override bool IsAssignableTo(EvaluatedType other)
         => other is String || SemanticsEqual(other);
        public override EvaluatedType ToAliasReference(Identifier alias) => new StringLengthed(LengthExpression, Length, alias);
    }

    internal sealed class Structure(IReadOnlyDictionary<Identifier, EvaluatedType> components, Identifier? alias = null) : EvaluatedType(alias)
    {
        public IReadOnlyDictionary<Identifier, EvaluatedType> Components => components;
        private readonly Lazy<string> _representation = new(() => {
            StringBuilder sb = new();
            sb.AppendLine("structure début");
            foreach (var comp in components) {
                sb.AppendLine($"{comp.Key} : {comp.Value.ActualRepresentation};");
            }
            sb.Append("fin");
            return sb.ToString();
        });

        public override bool SemanticsEqual(EvaluatedType other) => other is Structure o
         && o.Components.Keys.AllSemanticsEqual(Components.Keys)
         && o.Components.Values.AllSemanticsEqual(Components.Values);
        public override EvaluatedType ToAliasReference(Identifier alias) => new Structure(Components, alias);

        protected override string ActualRepresentation => _representation.Value;
    }
}
