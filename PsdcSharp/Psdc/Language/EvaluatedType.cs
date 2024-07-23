using System.Text;

using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.Language;

static class ConstantExpression
{
    public static ConstantExpression<TValue> Create<TValue>(Expression expression, TValue value)
     => new(expression, value);
}

sealed record ConstantExpression<TValue>(Expression Expression, TValue Value);

/// <summary>
/// A type evaluated during static analysis.
/// </summary>
abstract class EvaluatedType(Identifier? alias) : EquatableSemantics<EvaluatedType>
{
    /// <summary>
    /// Gets the alias used to refer to this type indirectly.
    /// </summary>
    /// <value>The alias used to refer to this type indirectly or <see langword="null"/> if this type is not aliased.</value>
    public Identifier? Alias { get; } = alias;

    /// <summary>
    /// Get the Pseudocode representation of this type.
    /// </summary>
    /// <value>The Pseudocode code that would result in an equal <see cref="EvaluatedType"/> object if parsed.</value>
    public string Representation => Alias?.Name ?? ActualRepresentation;

    protected abstract string ActualRepresentation { get; }

    // Static methods used for method groups
    public static bool IsConvertibleTo(EvaluatedType me, EvaluatedType other) => me.IsConvertibleTo(other);
    public static bool IsAssignableTo(EvaluatedType me, EvaluatedType other) => me.IsAssignableTo(other);

    /// <summary>
    /// Determine whether this type implicitly converts to type <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The type to compare with the current type.</param>
    /// <returns>Whether this type is implicitly convertible to <paramref name="other"/>.</returns>
    /// <remarks>Overrides will usually want to call base method in <see cref="EvaluatedType"/> in an logical disjunction.</remarks>
    public virtual bool IsConvertibleTo(EvaluatedType other) => SemanticsEqual(other);

    /// <summary>
    /// Determine whether a value of this type can be assigned to a variable of the specified type.
    /// </summary>
    /// <param name="other">The type of the variable a value of this type would be assigned to.</param>
    /// <returns>Whether this type is assignable to <paramref name="other"/></returns>
    /// <remarks>Overrides will usually want to call base in <see cref="EvaluatedType"/> an logical disjunction.</remarks>
    public virtual bool IsAssignableTo(EvaluatedType other) => IsConvertibleTo(other) || other.IsConvertibleFrom(this);

    /// <summary>
    /// Determine whether <paramref name="other"/> implicitly converts to this type.
    /// </summary>
    /// <param name="other">The type to compare with the current type.</param>
    /// <returns>Whether <paramref name="other"/> is implicitly convertinle to this type.</returns>
    protected virtual bool IsConvertibleFrom(EvaluatedType other) => false;

    public abstract bool SemanticsEqual(EvaluatedType other);

    public abstract EvaluatedType ToAliasReference(Identifier alias);

    public override string ToString() => Representation;

    internal sealed class Array : EvaluatedType
    {
        Array(EvaluatedType elementType, IReadOnlyList<ConstantExpression<int>> dimensions, Identifier? alias) : base(alias)
         => (ElementType, Dimensions) = (elementType, dimensions);

        public IReadOnlyList<ConstantExpression<int>> Dimensions { get; }

        public EvaluatedType ElementType { get; }

        protected override string ActualRepresentation
         => $"tableau [{string.Join(", ", Dimensions)}] de {ElementType.ActualRepresentation}";

        public static Array Create(EvaluatedType elementType,
            IReadOnlyList<ConstantExpression<int>> dimensions)
          => new(elementType, dimensions, null);

        // Arrays can't be reassigned for now.
        public override bool IsAssignableTo(EvaluatedType other) => false;

        public override EvaluatedType ToAliasReference(Identifier alias)
         => new Array(ElementType, Dimensions, alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is Array o
         && o.ElementType.SemanticsEqual(ElementType)
         && o.Dimensions.AllZipped(Dimensions, (od, d) => od == d);
    }

    internal sealed class Boolean : EvaluatedType
    {
        Boolean(Identifier? alias) : base(alias) { }

        public static Boolean Instance { get; } = new(null);

        protected override string ActualRepresentation => "booléen";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Boolean(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is Boolean;
    }

    internal sealed class Character : EvaluatedType
    {
        Character(Identifier? alias) : base(alias) { }

        public static Character Instance { get; } = new(null);

        protected override string ActualRepresentation => "caractère";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Character(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is Character;
    }

    internal sealed class File : EvaluatedType
    {
        File(Identifier? alias) : base(alias) { }

        public static File Instance { get; } = new(null);
        protected override string ActualRepresentation => "nomFichierLog";

        public override EvaluatedType ToAliasReference(Identifier alias) => new File(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is File;
    }

    internal sealed class LengthedString : EvaluatedType
    {
        LengthedString(Option<Expression> lengthExpression, int length, Identifier? alias = null) : base(alias)
         => (LengthConstantExpression, Length) = (lengthExpression, length);

        public int Length { get; }

        public Option<Expression> LengthConstantExpression { get; }

        protected override string ActualRepresentation => $"chaîne({Length})";

        public static LengthedString Create(ConstantExpression<int> length)
         => new(length.Expression.Some(), length.Value);

        public static LengthedString Create(int length)
         => new(Option.None<Expression>(), length, null);

        public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is String;
        public override bool IsAssignableTo(EvaluatedType other) => base.IsAssignableTo(other) || other is LengthedString o && o.Length > Length;
        public override bool SemanticsEqual(EvaluatedType other) => other is LengthedString o && o.Length == Length;

        public override EvaluatedType ToAliasReference(Identifier alias) => new LengthedString(LengthConstantExpression, Length, alias);
    }

    internal sealed class Integer : EvaluatedType
    {
        Integer(Identifier? alias) : base(alias) { }

        public static Integer Instance { get; } = new(null);
        protected override string ActualRepresentation => "entier";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Integer(alias);

        public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is Real;

        public override bool SemanticsEqual(EvaluatedType other) => other is Integer;
    }

    internal sealed class Real : EvaluatedType
    {
        Real(Identifier? alias) : base(alias) { }

        public static Real Instance { get; } = new(null);
        protected override string ActualRepresentation => "réel";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Real(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is Real;
    }

    internal class String : EvaluatedType
    {
        String(Identifier? alias) : base(alias) { }

        public static String Instance { get; } = new(null);

        protected override string ActualRepresentation => "chaîne";

        public override EvaluatedType ToAliasReference(Identifier alias) => new String(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is String;
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

        public override bool SemanticsEqual(EvaluatedType other) => other is Structure o
         && o.Components.Keys.AllSemanticsEqual(Components.Keys)
         && o.Components.Values.AllSemanticsEqual(Components.Values);
    }

    internal sealed class Unknown : EvaluatedType
    {
        Unknown(SourceTokens sourceTokens, string repr, Identifier? alias = null) : base(alias)
         => (SourceTokens, ActualRepresentation) = (sourceTokens, repr);

        protected override string ActualRepresentation { get; }
        public SourceTokens SourceTokens { get; }

        public static Unknown Declared(string input, SourceTokens sourceTokens) => new(sourceTokens, input[sourceTokens.InputRange]);

        // Provied an alternative overload which returns a more generic type for implicit operators
        public static EvaluatedType Declared(string input, Node node) => Declared(input, node.SourceTokens);

        public override EvaluatedType ToAliasReference(Identifier alias) => new Unknown(SourceTokens, ActualRepresentation, alias);

        // Unknown is convertible to every other type, and every type is convertible to Unknwon.
        // This is to prevent cascading errors when an object of an unknown type is used.
        public override bool IsConvertibleTo(EvaluatedType other) => true;
        protected override bool IsConvertibleFrom(EvaluatedType other) => true;

        public override bool SemanticsEqual(EvaluatedType other) => other is Unknown o
         && o.SourceTokens.SequenceEqual(SourceTokens);
    }
}

