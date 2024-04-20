using System.Text;

using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.SemanticAnalysis;

namespace Scover.Psdc.CodeGeneration;

internal sealed class TypeInfoC : TypeInfo
{
    public static TypeInfoC Create(EvaluatedType type, Func<Node.Expression, string> generateExpression)
    {
        switch (type) {
        case EvaluatedType.Primitive primitive:
            return primitive.Type switch {
                PrimitiveType.Boolean => new("bool", requiredHeaders: IncludeSet.StdBool),
                PrimitiveType.Character => new("char", "%c"),
                PrimitiveType.Integer => new("int", "%d"),
                PrimitiveType.File => new("FILE", preModifier: "*", requiredHeaders: IncludeSet.StdIo),
                PrimitiveType.Real => new("double", "%g"),
                _ => throw primitive.Type.ToUnmatchedException(),
            };
        case EvaluatedType.String:
            return new("char", "%s", preModifier: "*");
        case EvaluatedType.AliasReference alias:
            var target = Create(alias.Target, generateExpression);
            return new(alias.Name, target.FormatComponent, target.RequiredHeaders);
        case EvaluatedType.Array array:
            var arrayType = Create(array.Type, generateExpression);
            StringBuilder postModifier = new(arrayType._postModifier);

            foreach (var dimension in array.Dimensions) {
                postModifier.Append($"[{generateExpression(dimension)}]");
            }

            return new(arrayType._typeName, null, arrayType._preModifier, postModifier.ToString());
        case EvaluatedType.StringLengthed lengthedString:
            return new("char", "%s", postModifier: $"[{generateExpression(lengthedString.Length)}]");
        case EvaluatedType.StringLengthedKnown slk:
            return new("char", "%s", postModifier: $"[{slk.Length}]");
        default:
            throw type.ToUnmatchedException();
        }
    }

    private readonly string _preModifier, _typeName, _postModifier;
    private TypeInfoC(string typeName, Option<string> formatComponent, IEnumerable<string> requiredHeaders, string preModifier = "", string postModifier = "", int indirectionLevel = 0)
     => (_preModifier, _typeName, _postModifier, FormatComponent, RequiredHeaders)
        = (preModifier + new string('*', indirectionLevel), typeName, postModifier, formatComponent, requiredHeaders);

    private TypeInfoC(string typeName, string? formatComponent = null, string preModifier = "", string postModifier = "", params string[] requiredHeaders)
        : this(typeName, formatComponent.SomeNotNull(), requiredHeaders, preModifier, postModifier)
    {
    }

    public Option<string> FormatComponent { get; }
    public IEnumerable<string> RequiredHeaders { get; }

    public TypeInfoC ToPointer(int level)
     => new(_typeName, FormatComponent, RequiredHeaders, _preModifier, _postModifier, level);

    public string GenerateDeclaration(IEnumerable<string> names)
     => $"{_typeName} {string.Join(", ", names.Select(name => _preModifier + name + _postModifier))}";

    public string Generate() => _typeName + _preModifier + _postModifier;
}
