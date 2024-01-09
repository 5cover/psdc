using System.Text;

using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.CodeGeneration;

internal abstract class TypeInfo : IEquatable<TypeInfo?>
{
    private readonly string _preModifier, _typeName, _postModifier;
    protected TypeInfo(string typeName, Option<string> formatComponent, IEnumerable<string> requiredHeaders, string preModifier = "", string postModifier = "", bool isNumeric = false)
     => (_preModifier, _typeName, _postModifier, FormatComponent, IsNumeric, RequiredHeaders)
            = (preModifier, typeName, postModifier, formatComponent, isNumeric, requiredHeaders);

    protected TypeInfo(string typeName, string? formatComponent, string preModifier = "", string postModifier = "", bool isNumeric = false, IEnumerable<string>? requiredHeaders = null)
        : this(typeName, formatComponent.SomeNotNull(), requiredHeaders ?? Enumerable.Empty<string>(), preModifier, postModifier, isNumeric)
    {
    }

    public Option<string> FormatComponent { get; }
    public bool IsNumeric { get; }
    public IEnumerable<string> RequiredHeaders { get; }
    public bool Equals(TypeInfo? other)
     => ReferenceEquals(this, other) || other is not null
        && _preModifier == other._preModifier
        && _typeName == other._typeName
        && _postModifier == other._postModifier;
    public override bool Equals(object? obj) => Equals(obj as TypeInfo);
    public override int GetHashCode() => HashCode.Combine(_preModifier, _typeName, _postModifier);

    public string CreateDeclaration(IEnumerable<string> names)
     => $"{_typeName} {string.Join(", ", names.Select(name => $"{_preModifier}{name}{_postModifier}"))}";

    public override string ToString() => $"{_preModifier}{_typeName}{_postModifier}";

    public sealed class String : TypeInfo
    {
        private String() : base("char", "%s", "*")
        {
        }
        public static String Create() => new();
    }

    public sealed class Alias : TypeInfo
    {
        private Alias(string typeName, Option<string> formatComponent, IEnumerable<string> requiredHeaders) : base(typeName, formatComponent, requiredHeaders)
        {
        }
        public static Alias Create(string name, TypeInfo target) => new(name, target.FormatComponent, target.RequiredHeaders);
    }

    public sealed class Primitive : TypeInfo
    {
        private Primitive(PrimitiveType type, string typeName, string? formatComponent = null, string preModifier = "", params string[] requiredHeaders)
            : base(typeName, formatComponent, preModifier, isNumeric: type is not PrimitiveType.File, requiredHeaders: requiredHeaders)
         => Type = type;

        public PrimitiveType Type { get; }

        public static Primitive Create(PrimitiveType type) => type switch {
            PrimitiveType.Boolean => new(type, "bool", requiredHeaders: IncludeSet.StdBool),
            PrimitiveType.Character => new(type, "char", "%c"),
            PrimitiveType.Integer => new(type, "int", "%d"),
            PrimitiveType.File => new(type, "FILE", preModifier: "*", requiredHeaders: IncludeSet.StdIo),
            PrimitiveType.Real => new(type, "double", "%g"),
            _ => throw type.ToUnmatchedException(),
        };
    }

    public sealed class Array : TypeInfo
    {
        private Array(string typeName, string? formatComponent = null, string preModifier = "", string postModifier = "")
            : base(typeName, formatComponent, preModifier, postModifier)
        {
        }

        public static Array Create(TypeInfo type, IEnumerable<string> dimensions)

        {
            StringBuilder postModifier = new(type._postModifier);

            foreach (string dimension in dimensions) {
                postModifier.Append($"[{dimension}]");
            }

            return new(type._typeName, type._preModifier, postModifier.ToString());
        }
    }

    public sealed class LengthedString : TypeInfo
    {
        private LengthedString(string formatComponent, string postModifier) : base("char", formatComponent, postModifier: postModifier)
        {
        }

        public static LengthedString Create(string length) => new("%s", $"[{length}]");
        public static LengthedString Create(int length) => new($"%{length}s", $"[{length}]");
    }
}
