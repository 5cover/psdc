using System.Text;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal sealed class TypeInfoC : TypeInfo
{
    public static TypeInfoC Create(EvaluatedType type, Func<Node.Expression, string> generateExpression)
     => Create(type, generateExpression, new());
    private static TypeInfoC Create(EvaluatedType type, Func<Node.Expression, string> generateExpression, Indentation indent)
    {
        switch (type) {
        case EvaluatedType.Unknown u:
            return new(u.Representation);
        case EvaluatedType.File file:
            return new("FILE", preModifier: "*", requiredHeaders: IncludeSet.StdIo.Yield());
        case EvaluatedType.Numeric numeric:
            return numeric.Type switch {
                NumericType.Boolean => new("bool", requiredHeaders: IncludeSet.StdBool.Yield()),
                NumericType.Character => new("char", "%c"),
                NumericType.Integer => new("int", "%d"),
                NumericType.Real => new("float", "%g"),
                _ => throw numeric.Type.ToUnmatchedException(),
            };
        case EvaluatedType.String:
            return new("char", "%s", preModifier: "*");
        case EvaluatedType.AliasReference alias:
            var target = Create(alias.Target, generateExpression, indent);
            return new(alias.Name.Name, target.FormatComponent, target.RequiredHeaders);
        case EvaluatedType.Array array:
            var arrayType = Create(array.ElementType, generateExpression, indent);
            StringBuilder postModifier = new(arrayType._postModifier);
            foreach (var dimension in array.Dimensions) {
                postModifier.Append($"[{generateExpression(dimension)}]");
            }
            return new(arrayType._typeName,
                requiredHeaders: arrayType.RequiredHeaders,
                preModifier: arrayType._preModifier,
                postModifier: postModifier.ToString());
        case EvaluatedType.LengthedString lengthedString:
            return new("char", "%s", postModifier: $"[{generateExpression(lengthedString.Length)}]");
        case EvaluatedType.StringLengthedKnown slk:
            return new("char", "%s", postModifier: $"[{slk.Length}]");
        case EvaluatedType.Structure structure:
            StringBuilder sb = new("struct {");
            sb.AppendLine();
            indent.Increase();
            var components = structure.Components.ToDictionary(kv => kv.Key, kv => Create(kv.Value, generateExpression, indent));
            foreach (var comp in components) {
                indent.Indent(sb).Append(comp.Value.GenerateDeclaration(comp.Key.Yield())).AppendLine(";");
            }
            indent.Decrease();
            sb.Append('}');
            return new(sb.ToString(),
                // it's ok if there are duplicate headers, since IncludeSet.Ensure will ignore duplicates.
                requiredHeaders: components.Values.SelectMany(type => type.RequiredHeaders));
        default:
            throw type.ToUnmatchedException();
        }
    }

    private readonly string _preModifier, _typeName, _postModifier, _typeQualifier;
    private TypeInfoC(string typeName, Option<string> formatComponent, IEnumerable<string> requiredHeaders, string preModifier = "", string postModifier = "", string? typeQualifier = null)
     => (_preModifier, _typeName, _postModifier, _typeQualifier, FormatComponent, RequiredHeaders)
        = (preModifier,
           typeName,
           postModifier,
           AddSpaceBefore(typeQualifier),
           formatComponent,
           requiredHeaders);

    private TypeInfoC(string typeName, string? formatCompnent = null, IEnumerable<string>? requiredHeaders = null, string preModifier = "", string postModifier = "", string? typeQualifier = null)
    : this(typeName, formatCompnent.SomeNotNull(), requiredHeaders ?? [], preModifier, postModifier, typeQualifier)
    {
    }

    public Option<string> FormatComponent { get; }
    public IEnumerable<string> RequiredHeaders { get; }

    public TypeInfoC ToPointer(int level)
     => new(_typeName, FormatComponent, RequiredHeaders, _preModifier,
        _postModifier + new string('*', level), null);

    public TypeInfoC ToConst()
     => new(_typeName, FormatComponent, RequiredHeaders, _preModifier, _postModifier,
        "const");

    public string GenerateDeclaration(IEnumerable<Identifier> names)
     => $"{_typeName}{_typeQualifier} {string.Join(", ", names.Select(name => _preModifier + name.Name + _postModifier))}";

    public string Generate() => $"{_typeName}{_preModifier}{_postModifier}{_typeQualifier}";

    private static string AddSpaceBefore(string? str) => str is null ? "" : $" {str}";
}
