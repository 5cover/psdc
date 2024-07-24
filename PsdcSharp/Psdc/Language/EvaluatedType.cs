using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.Language;

interface InstantiableType<TValue, TUnderlying>
where TValue : Value
{
    public TValue CreateValue(Option<TUnderlying> value);
}

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
    public abstract Value UnknownValue { get; }

    internal sealed class Array : EvaluatedType
    {
        Array(EvaluatedType elementType, IReadOnlyList<ConstantExpression<int>> dimensions, Identifier? alias) : base(alias) => (ElementType, Dimensions, UnknownValue) = (elementType, dimensions, new(this, Option.None<System.Array>()));

        public override Value UnknownValue { get; }
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

        public Value CreateValue(Option<System.Array> value)
        {
            Debug.Assert(value.Value is not { } v || v.Shape().SequenceEqual(Dimensions.Select(d => d.Value)));
            return new(this, value);
        }

        public sealed class Value(Array type, Option<System.Array> value) : ValueImpl<Array, System.Array>(type, value);
    }

    internal sealed class Boolean : EvaluatedType, InstantiableType<Boolean.Value, bool>
    {
        Boolean(Identifier? alias) : base(alias) => UnknownValue = new(this, Option.None<bool>());
        public override Value UnknownValue { get; }

        public static Boolean Instance { get; } = new(null);

        protected override string ActualRepresentation => "booléen";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Boolean(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is Boolean;

        public Value CreateValue(Option<bool> value) => new(this, value);
        public sealed class Value(Boolean type, Option<bool> value) : ValueImpl<Boolean, bool>(type, value);
    }

    internal sealed class Character : EvaluatedType, InstantiableType<Character.Value, char>
    {
        Character(Identifier? alias) : base(alias) => UnknownValue = new(this, Option.None<char>());
        public override Value UnknownValue { get; }

        public static Character Instance { get; } = new(null);

        protected override string ActualRepresentation => "caractère";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Character(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is Character;

        public Value CreateValue(Option<char> value) => new(this, value);

        public sealed class Value(Character type, Option<char> value) : ValueImpl<Character, char>(type, value);
    }

    internal sealed class File : EvaluatedType
    {
        File(Identifier? alias) : base(alias) => UnknownValue = new(this);
        public override Value UnknownValue { get; }
        public static File Instance { get; } = new(null);
        protected override string ActualRepresentation => "nomFichierLog";

        public override EvaluatedType ToAliasReference(Identifier alias) => new File(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is File;

        public sealed class Value(EvaluatedType type) : Language.Value
        {
            public EvaluatedType Type => type;
            public bool IsKnown => false;

            public bool SemanticsEqual(Language.Value other) => other.Type.SemanticsEqual(Type);
        }
    }

    internal sealed class LengthedString : EvaluatedType, InstantiableType<LengthedString.Value, string>
    {
        LengthedString(Option<Expression> lengthExpression, int length, Identifier? alias = null) : base(alias)
         => (LengthConstantExpression, Length, UnknownValue) = (lengthExpression, length, new(this, Option.None<string>()));

        public int Length { get; }
        public override Value UnknownValue { get; }
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

        public Value CreateValue(Option<string> value)
        {
            Debug.Assert(value.Value is not { } v || v.Length == Length);
            return new(this, value);
        }

        public sealed class Value(LengthedString type, Option<string> value)
        : String.Value(String.Instance, value), Value<LengthedString, string>
        {
            private readonly ValueImpl _impl = new(type, value);
            public new LengthedString Type => _impl.Type;
            public bool SemanticsEqual(Value<LengthedString, string> other) => _impl.SemanticsEqual(other);
            sealed class ValueImpl(LengthedString type, Option<string> value) : ValueImpl<LengthedString, string>(type, value);
        }
    }

    internal sealed class Integer : EvaluatedType, InstantiableType<Integer.Value, int>
    {
        public override Value UnknownValue { get; }
        Integer(Identifier? alias) : base(alias) => UnknownValue = new(this, Option.None<int>());

        public static Integer Instance { get; } = new(null);
        protected override string ActualRepresentation => "entier";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Integer(alias);

        public override bool IsConvertibleTo(EvaluatedType other) => base.IsConvertibleTo(other) || other is Real;

        public override bool SemanticsEqual(EvaluatedType other) => other is Integer;

        public Value CreateValue(Option<int> value) => new(this, value);

        public sealed class Value(Integer type, Option<int> value)
        : Real.Value(Real.Instance, value.Map(v => (decimal)v)), Value<Integer, int>
        {
            private readonly ValueImpl _impl = new(type, value);

            public new Integer Type => _impl.Type;
            public new Option<int> UnderlyingValue => _impl.UnderlyingValue;

            public bool SemanticsEqual(Value<Integer, int> other) => _impl.SemanticsEqual(other);

            sealed class ValueImpl(Integer type, Option<int> value) : ValueImpl<Integer, int>(type, value);
        }
    }

    internal sealed class Real : EvaluatedType, InstantiableType<Real.Value, decimal>
    {
        Real(Identifier? alias) : base(alias) => UnknownValue = new(this, Option.None<decimal>());
        public override Value UnknownValue { get; }
        public static Real Instance { get; } = new(null);
        protected override string ActualRepresentation => "réel";

        public override EvaluatedType ToAliasReference(Identifier alias) => new Real(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is Real;

        public Value CreateValue(Option<decimal> value) => new(this, value);

        public class Value(Real type, Option<decimal> value) : ValueImpl<Real, decimal>(type, value);
    }

    internal class String : EvaluatedType, InstantiableType<String.Value, string>
    {
        String(Identifier? alias) : base(alias) => UnknownValue = new(this, Option.None<string>());
        public override Value UnknownValue { get; }
        public static String Instance { get; } = new(null);

        protected override string ActualRepresentation => "chaîne";

        public override EvaluatedType ToAliasReference(Identifier alias) => new String(alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is String;

        public Value CreateValue(Option<string> value) => new(this, value);

        public class Value(String type, Option<string> value) : ValueImpl<String, string>(type, value);
    }

    internal sealed class Structure : EvaluatedType, InstantiableType<Structure.Value, IReadOnlyDictionary<Identifier, Value>>
    {
        public Structure(ReadOnlyOrderedMap<Identifier, EvaluatedType> components, Identifier? alias = null) : base(alias)
        {
            (Components, _representation, UnknownValue) = (components, alias is null
                ? new(() => {
                    StringBuilder sb = new();
                    sb.AppendLine("structure début");
                    foreach (var comp in components.Map) {
                        sb.AppendLine($"{comp.Key} : {comp.Value.Representation};");
                    }
                    sb.Append("fin");
                    return sb.ToString();
                })
                : new(alias.Name), // To avoid long type representations, use the alias name if available.
                new(this, Option.None<IReadOnlyDictionary<Identifier, Language.Value>>()));
        }

        readonly Lazy<string> _representation;

        public ReadOnlyOrderedMap<Identifier, EvaluatedType> Components { get; }
        protected override string ActualRepresentation => _representation.Value;
        public override Value UnknownValue { get; }
        public override EvaluatedType ToAliasReference(Identifier alias) => new Structure(Components, alias);

        public override bool SemanticsEqual(EvaluatedType other) => other is Structure o
         && o.Components.Map.Keys.AllSemanticsEqual(Components.Map.Keys)
         && o.Components.Map.Values.AllSemanticsEqual(Components.Map.Values);

        public Value CreateValue(Option<IReadOnlyDictionary<Identifier, Language.Value>> value)
        {
            Debug.Assert(value.Value is not { } v || Components.Map.Count == v.Count
                    && Components.Map.All(kv
                        => v.TryGetValue(kv.Key, out var componentValue)
                        && componentValue.Type.SemanticsEqual(kv.Value)
                    ));
            return new(this, value);
        }

        public sealed class Value(Structure type, Option<IReadOnlyDictionary<Identifier, Language.Value>> value) : ValueImpl<Structure, IReadOnlyDictionary<Identifier, Language.Value>>(type, value);
    }

    internal sealed class Unknown : EvaluatedType
    {
        Unknown(SourceTokens sourceTokens, string repr, Identifier? alias = null) : base(alias)
         => (SourceTokens, ActualRepresentation, UnknownValue) = (sourceTokens, repr, new(this));
        public override Value UnknownValue { get; }
        protected override string ActualRepresentation { get; }
        public SourceTokens SourceTokens { get; }

        public static Unknown Inferred { get; } = new Unknown(SourceTokens.Empty, "<unknown-type>");

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

        public sealed class Value(EvaluatedType type) : Language.Value
        {
            public EvaluatedType Type => type;
            public bool IsKnown => false;

            public bool SemanticsEqual(Language.Value other) => other.Type.SemanticsEqual(Type);
        }
    }
}
