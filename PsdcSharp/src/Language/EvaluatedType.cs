using System.Text;

using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.Language;

/// <summary>
/// A type evaluated during the static analysis.
/// </summary>
abstract class EvaluatedType(Identifier? alias) : EquatableSemantics<EvaluatedType>
{
    /// <summary>
    /// Gets the alias used to refer to this type indirectly.
    /// </summary>
    /// <value>The alias used to refer to this type indirectly or <see langword="null"/> if this type is not aliased.</value>
    public Identifier? Alias { get; } = alias;

    /// <summary>
    /// Gets the Pseudocode representation of this type.
    /// </summary>
    /// <value>The Pseudocode code that would result in an equal <see cref="EvaluatedType"/> object if parsed.</value>
    public string Representation => Alias?.Name ?? ActualRepresentation;

    protected abstract string ActualRepresentation { get; }

    /// <summary>
    /// Determines whether a value of this type can be assigned to a variable of type <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The type to compare with the current type</param>
    /// <returns>Whether a value of this type can be assigned to a variable of type <paramref name="other"/>.</returns>
    public virtual bool IsAssignableTo(EvaluatedType other) => ActualSemanticsEqual(other);

    public bool SemanticsEqual(EvaluatedType other)
     => ActualSemanticsEqual(other) || other.ActualSemanticsEqual(this);

    public abstract EvaluatedType ToAliasReference(Identifier alias);

    public override string ToString() => Representation;

    protected abstract bool ActualSemanticsEqual(EvaluatedType other);

    internal sealed class Array : EvaluatedType
    {
        Array(EvaluatedType elementType, IReadOnlyCollection<Expression> dimensionExprs, IReadOnlyCollection<int> dimensions, Identifier? alias) : base(alias)
         => (ElementType, DimensionConstantExpressions, Dimensions) = (elementType, dimensionExprs, dimensions);

        public IReadOnlyCollection<Expression> DimensionConstantExpressions { get; }

        public IReadOnlyCollection<int> Dimensions { get; }

        public EvaluatedType ElementType { get; }

        protected override string ActualRepresentation
         => $"tableau [{string.Join(", ", Dimensions)}] de {ElementType.ActualRepresentation}";

        public static Option<Array, IEnumerable<Message>> Create(EvaluatedType elementType, IReadOnlyCollection<Expression> dimensionsExpressions, ReadOnlyScope scope)
                                                 => dimensionsExpressions.Select(d
             => d.EvaluateConstantValue<ConstantValue.Integer>(scope, Numeric.GetInstance(NumericType.Integer))).Accumulate()
            .Map(dimensions => new Array(elementType, dimensionsExpressions,
                                          dimensions.Select(d => d.Value).ToList(), null));

        // Arrays can't be reassigned.
        public override bool IsAssignableTo(EvaluatedType other) => false;

        public override EvaluatedType ToAliasReference(Identifier alias)
         => new Array(ElementType, DimensionConstantExpressions, Dimensions, alias);

        protected override bool ActualSemanticsEqual(EvaluatedType other) => other is Array o
                         && o.ElementType.ActualSemanticsEqual(ElementType)
         && o.Dimensions.AllZipped(Dimensions, (od, d) => od == d);
    }

    internal sealed class Boolean : EvaluatedType
    {
        Boolean(Identifier? alias) : base(alias) { }

        public static Boolean Instance { get; } = new(null);

        protected override string ActualRepresentation => "booléen";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Boolean(alias);

        protected override bool ActualSemanticsEqual(EvaluatedType other) => other is Boolean;
    }

    internal sealed class Character : EvaluatedType
    {
        Character(Identifier? alias) : base(alias) { }

        public static Character Instance { get; } = new(null);

        protected override string ActualRepresentation => "caractère";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Character(alias);

        protected override bool ActualSemanticsEqual(EvaluatedType other) => other is Character;
    }

    internal sealed class File : EvaluatedType
    {
        File(Identifier? alias) : base(alias) { }

        public static File Instance { get; } = new(null);
        protected override string ActualRepresentation => "nomFichierLog";

        public override EvaluatedType ToAliasReference(Identifier alias) => new File(alias);

        protected override bool ActualSemanticsEqual(EvaluatedType other) => other is File;
    }

    internal sealed class LengthedString : EvaluatedType
    {
        LengthedString(Option<Expression> lengthExpression, int length, Identifier? alias) : base(alias)
         => (LengthConstantExpression, Length) = (lengthExpression, length);

        public int Length { get; }

        public Option<Expression> LengthConstantExpression { get; }

        protected override string ActualRepresentation => $"chaîne({Length})";

        public static Option<LengthedString, Message> Create(Expression lengthExpression, ReadOnlyScope scope)
         => lengthExpression.EvaluateConstantValue<ConstantValue.Integer>(scope, Numeric.GetInstance(NumericType.Integer))
            .Map(l => new LengthedString(lengthExpression.Some(), l.Value, null));

        public static LengthedString Create(int length)
         => new(Option.None<Expression>(), length, null);

        public override bool IsAssignableTo(EvaluatedType other)
         => other is String || ActualSemanticsEqual(other);

        public override EvaluatedType ToAliasReference(Identifier alias) => new LengthedString(LengthConstantExpression, Length, alias);

        protected override bool ActualSemanticsEqual(EvaluatedType other) => other is LengthedString o
            && o.Length == Length;
    }

    internal sealed class Numeric : EvaluatedType
    {
        static readonly Numeric[] instances = [
            new(NumericType.Integer, 2, "entier"),
            new(NumericType.Real, 3, "réel"),
        ];

        // Precision represents how many values the type can represent.
        // The higher, the wider the value range.
        readonly int _precision;

        Numeric(NumericType type, int precision, string representation, Identifier? alias = null) : base(alias)
         => (Type, _precision, ActualRepresentation) = (type, precision, representation);

        public NumericType Type { get; }
        protected override string ActualRepresentation { get; }

        public static Numeric GetInstance(NumericType type) => instances.First(i => i.Type == type);

        public static EvaluatedType GetMostPreciseType(Numeric t1, Numeric t2)
        => t1._precision > t2._precision ? t1 : t2;

        public override bool IsAssignableTo(EvaluatedType other) => other is Numeric o
         && o._precision >= _precision;

        public override EvaluatedType ToAliasReference(Identifier alias) => new Numeric(Type, _precision, ActualRepresentation, alias);

        protected override bool ActualSemanticsEqual(EvaluatedType other) => other is Numeric o
                         && o.Type == Type;
    }

    internal class String : EvaluatedType
    {
        String(Identifier? alias) : base(alias) { }

        public static String Instance { get; } = new(null);

        protected override string ActualRepresentation => "chaîne";

        public override EvaluatedType ToAliasReference(Identifier alias) => new String(alias);

        protected override bool ActualSemanticsEqual(EvaluatedType other) => other is String || other.ActualSemanticsEqual(this);
    }

    internal sealed class Structure(IReadOnlyDictionary<Identifier, EvaluatedType> components, Identifier? alias = null) : EvaluatedType(alias)
    {
        readonly Lazy<string> _representation = alias is null ? new(() => {
            StringBuilder sb = new();
            sb.AppendLine("structure début");
            foreach (var comp in components) {
                sb.AppendLine($"{comp.Key} : {comp.Value.ActualRepresentation};");
            }
            sb.Append("fin");
            return sb.ToString();
        })
        // To avoid long type representations, use the alias name if available.
        : new(alias.Name);

        public IReadOnlyDictionary<Identifier, EvaluatedType> Components => components;
        protected override string ActualRepresentation => _representation.Value;

        public override EvaluatedType ToAliasReference(Identifier alias) => new Structure(Components, alias);

        protected override bool ActualSemanticsEqual(EvaluatedType other) => other is Structure o
                         && o.Components.Keys.AllSemanticsEqual(Components.Keys)
         && o.Components.Values.AllSemanticsEqual(Components.Values);
    }

    internal sealed class Unknown(SourceTokens sourceTokens, Identifier? alias = null) : EvaluatedType(alias)
    {
        protected override string ActualRepresentation => Globals.Input[sourceTokens.InputRange];

        public override EvaluatedType ToAliasReference(Identifier alias) => new Unknown(sourceTokens, alias);

        // Unknown types are semantically equal to every other type.
        // This is to prevent cascading errors when an object of an unknown type is used.
        protected override bool ActualSemanticsEqual(EvaluatedType other) => true;
    }
}
