using System.Collections.Immutable;
using Scover.Psdc.Pseudocode;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using static Scover.Psdc.StaticAnalysis.SemanticNode;

namespace Scover.Psdc.StaticAnalysis;

using ValueTransform = Func<Value, Option<Value, Message>>;

public sealed partial class StaticAnalyzer
{
    ValueOption<ValueOption<InitializerPath>, Message> EvaluatePath(Scope scope, ReadOnlySpan<Designator> designators, EvaluatedType targetType)
     => designators.IsEmpty
        ? Option.Some<ValueOption<InitializerPath>, Message>(default)
        : designators[0] switch {
            Designator.Structure s => targetType is StructureType t
                ? EvaluateStructurePath(scope, s, designators[1..], t).Map(s => s.Some<InitializerPath>())
                : Message.ErrorUnsupportedDesignator(s.Meta.Location, targetType),
            Designator.Array a => targetType is ArrayType t
                ? EvaluateArrayPath(scope, a, designators[1..], t).Map(s => s.Some<InitializerPath>())
                : Message.ErrorUnsupportedDesignator(a.Meta.Location, targetType),
            _ => throw designators[0].ToUnmatchedException(),
        };

    ValueOption<InitializerPath.Structure, Message> EvaluateStructurePath(Scope scope, Designator.Structure first, ReadOnlySpan<Designator> rest, StructureType targetType)
     => !targetType.Components.Map.TryGetValue(first.Component, out var componentType)
        ? Message.ErrorStructureComponentDoesntExist(first.Component, targetType)
        : EvaluatePath(scope, rest, componentType).Map(restPath => new InitializerPath.Structure(new(first.Component), restPath, targetType));

    ValueOption<InitializerPath.Array, Message> EvaluateArrayPath(Scope scope, Designator.Array first, ReadOnlySpan<Designator> rest, ArrayType targetType)
     => first.Index.Value.SomeIndexes(targetType.Length.Value, 1) is { HasValue: true } i
        ? EvaluatePath(scope, rest, targetType.ItemType).Map(restPath => new InitializerPath.Array(new(i.Value), restPath, targetType))
        : Message.ErrorIndexOutOfBounds(first.Index, targetType.Length.Value);

    interface DesignatorInfo
    {
        readonly record struct Array(int Index) : DesignatorInfo;
        readonly record struct Structure(Ident Component) : DesignatorInfo;
    }

    interface InitializerPath
    {
        DesignatorInfo First { get; }
        ValueOption<InitializerPath> Rest { get; }
        EvaluatedType Type { get; }

        /// <summary>Advances this path to the next subobject in natural order.</summary>
        /// <returns>An altered copy of this path.</returns>
        Option<InitializerPath, Message> Advance(Range location);

        Option<Value, Message> SetValue(Range location, object haystack, Value needle);

        abstract record Impl<TSelf, TDesignator, TType, TValue, TUnderlying>(TDesignator First, ValueOption<InitializerPath> Rest, TType Type) : InitializerPath
        where TSelf : Impl<TSelf, TDesignator, TType, TValue, TUnderlying>
        where TDesignator : DesignatorInfo
        where TType : InstantiableType<TValue, TUnderlying>
        where TValue : Value
        where TUnderlying : notnull
        {
            EvaluatedType InitializerPath.Type => Type;
            DesignatorInfo InitializerPath.First => First;
            public Option<TSelf, Message> Advance(Range location)
            {
                return Rest.Map(r => r.Advance(location).Map(r => WithRest(r.Some())).Or(ByFirst)).ValueOr(ByFirst);
                Option<TSelf, Message> ByFirst() => AdvanceFirst(location).Map(WithFirst);
            }

            public Option<TValue, Message> SetValue(Range location, TUnderlying haystack, Value needle)
             => SetValueFirst(location, haystack, Rest.Match(
                    rest => inner => inner.Status.ComptimeValue.Match(
                        v => rest.SetValue(location, v, needle),
                        () => inner.Some<Value, Message>()), // If the default value is non-comptime, just leave it as it is.
                    () => (ValueTransform)(_ => needle.Some<Value, Message>())))
                .Map(Type.Instanciate);

            protected abstract ValueOption<TDesignator, Message> AdvanceFirst(Range location);
            protected abstract Option<TUnderlying, Message> SetValueFirst(Range location, TUnderlying haystack, ValueTransform transform);

            protected abstract TSelf WithRest(ValueOption<InitializerPath> rest);
            protected abstract TSelf WithFirst(TDesignator first);

            Option<InitializerPath, Message> InitializerPath.Advance(Range location) => Advance(location);
            Option<Value, Message> InitializerPath.SetValue(Range location, object haystack, Value needle)
             => SetValue(location, (TUnderlying)haystack, needle).Map(v => (Value)v);
        }
        sealed record Array(DesignatorInfo.Array First, ValueOption<InitializerPath> Rest, ArrayType Type)
        : Impl<Array, DesignatorInfo.Array, ArrayType, ArrayValue, ImmutableArray<Value>>(First, Rest, Type)
        {
            public static ValueOption<Array, Message> OfFirstObject(ArrayType type, Range location)
             => 0.Indexes(type.Length.Value)
                ? new Array(new(0), default, type)
                : Message.ErrorExcessElementInInitializer(location);
            protected override ValueOption<DesignatorInfo.Array, Message> AdvanceFirst(Range location)
             => (First.Index + 1).Indexes(Type.Length.Value)
                ? new DesignatorInfo.Array(First.Index + 1)
                : Message.ErrorIndexOutOfBounds(location, First.Index + 2, Type.Length.Value);
            protected override Option<ImmutableArray<Value>, Message> SetValueFirst(Range location, ImmutableArray<Value> haystack, ValueTransform transform)
             => haystack.ElementAtOrNone(First.Index)
                .OrWithError(Message.ErrorIndexOutOfBounds(location, First.Index + 1, haystack.Length))
                .Bind(transform)
                .Map(inner => haystack.SetItem(First.Index, inner));
            protected override Array WithFirst(DesignatorInfo.Array first) => this with { First = first };
            protected override Array WithRest(ValueOption<InitializerPath> rest) => this with { Rest = rest };
        }
        sealed record Structure(DesignatorInfo.Structure First, ValueOption<InitializerPath> Rest, StructureType Type)
        : Impl<Structure, DesignatorInfo.Structure, StructureType, StructureValue, ImmutableOrderedMap<Ident, Value>>(First, Rest, Type)
        {
            public static ValueOption<Structure, Message> OfFirstObject(StructureType type, Range location)
             => type.Components.List.FirstOrNone().Map(comp => new Structure(new(comp.Key), default, type))
                .OrWithError(Message.ErrorExcessElementInInitializer(location));

            protected override ValueOption<DesignatorInfo.Structure, Message> AdvanceFirst(Range location)
             => Type.Components.List.TryGetAt(1 + Type.Components.List
                                                  .IndexOfFirst(i => i.Key.Equals(First.Component)).Unwrap(),
                                              out var c)
                ? new DesignatorInfo.Structure(c.Key)
                : Message.ErrorExcessElementInInitializer(location);

            protected override Option<ImmutableOrderedMap<Ident, Value>, Message> SetValueFirst(Range location, ImmutableOrderedMap<Ident, Value> haystack, ValueTransform transform)
             => haystack.Map.GetValueOrNone(First.Component)
                .OrWithError(Message.ErrorStructureComponentDoesntExist(First.Component, Type))
                .Bind(transform)
                .Map(inner => haystack.SetItem(First.Component, inner));

            protected override Structure WithFirst(DesignatorInfo.Structure first) => this with { First = first };
            protected override Structure WithRest(ValueOption<InitializerPath> rest) => this with { Rest = rest };
        }
    }
}
