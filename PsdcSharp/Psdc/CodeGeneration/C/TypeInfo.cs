using System.Text;

using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

sealed class TypeInfo : CodeGeneration.TypeInfo
{
    readonly string _stars;

    readonly string _typeName, _postModifier, _typeQualifier;

    TypeInfo(string typeName, Option<string> formatComponent, IEnumerable<string> requiredHeaders, int starCount = 0, string postModifier = "", string? typeQualifier = null)
     => (_stars, _typeName, _postModifier, _typeQualifier, FormatComponent, RequiredHeaders)
        = (new string('*', starCount),
           typeName,
           postModifier,
           AddSpaceBefore(typeQualifier),
           formatComponent,
           requiredHeaders);

    TypeInfo(string typeName, string? formatComponent = null, IEnumerable<string>? requiredHeaders = null, int starCount = 0, string postModifier = "", string? typeQualifier = null)
    : this(typeName,
           formatComponent.SomeNotNull(),
           requiredHeaders ?? [],
           starCount,
           postModifier,
           typeQualifier)
    {
    }

    public Option<string> FormatComponent { get; }

    public IEnumerable<string> RequiredHeaders { get; }

    public static TypeInfo Create(EvaluatedType type, Messenger messenger, Func<Expression, string> generateExpression, CodeGeneration.KeywordTable keywordTable)
     => Create(type, messenger, generateExpression, keywordTable, new());

    public string DecorateExpression(string expr)
     => $"{_stars}{expr}";

    public string Generate() => $"{_typeName}{_stars}{_postModifier}{_typeQualifier}";

    public string GenerateDeclaration(IEnumerable<string> names)
     => $"{_typeName}{_typeQualifier} {string.Join(", ", names.Select(name => _stars + name + _postModifier))}";

    public TypeInfo ToConst()
     => new(_typeName, FormatComponent, RequiredHeaders, _stars.Length, _postModifier,
        "const");

    public TypeInfo ToPointer(int level)
     => new(_typeName, FormatComponent, RequiredHeaders, _stars.Length + level,
        _postModifier, null);

    static string AddSpaceBefore(string? str) => str is null ? "" : $" {str}";

    static TypeInfo Create(EvaluatedType type, Messenger msger, Func<Expression, string> generateExpression, CodeGeneration.KeywordTable keywordTable, Indentation indent)
    {
        TypeInfo typeInfo = type switch {
            EvaluatedType.Unknown u => new(keywordTable.Validate(u.SourceTokens, u.Representation, msger)),
            EvaluatedType.File => new("FILE", starCount: 1, requiredHeaders: IncludeSet.StdIo.Yield()),
            EvaluatedType.Boolean => new("bool", requiredHeaders: IncludeSet.StdBool.Yield()),
            EvaluatedType.Character => new("char", "%c"),
            EvaluatedType.Real real => new("float", "%g"),
            EvaluatedType.Integer integer => new("int", "%d"),
            EvaluatedType.String => new("char", "%s", starCount: 1),
            EvaluatedType.Array array => CreateArrayType(array),
            EvaluatedType.LengthedString strlen => CreateLengthedString(strlen),
            EvaluatedType.Structure structure => CreateStructure(structure),
            _ => throw type.ToUnmatchedException(),
        };

        return type.Alias is { } alias
            ? new TypeInfo(keywordTable.Validate(alias, msger), typeInfo.FormatComponent, typeInfo.RequiredHeaders)
            : typeInfo;

        TypeInfo CreateArrayType(EvaluatedType.Array array)
        {
            var arrayType = Create(array.ElementType, msger, generateExpression, keywordTable, indent);
            StringBuilder postModifier = new(arrayType._postModifier);
            foreach (var dimension in array.Dimensions.Select(dim => generateExpression(dim.Expression))) {
                postModifier.Append($"[{dimension}]");
            }
            return new(arrayType._typeName,
                requiredHeaders: arrayType.RequiredHeaders,
                starCount: arrayType._stars.Length,
                postModifier: postModifier.ToString());
        }

        TypeInfo CreateLengthedString(EvaluatedType.LengthedString strlen)
        {
            // add 1 to the length for null terminator
            string lengthPlus1 = strlen.LengthConstantExpression
                .Map(len => {
                    var (expression, messages) = len.Alter(BinaryOperator.Add, 1);
                    msger.ReportAll(messages);
                    return generateExpression(expression);
                })
                .ValueOr((strlen.Length + 1).ToString());
            return new("char", "%s", postModifier: $"[{lengthPlus1}]");
        }

        TypeInfo CreateStructure(EvaluatedType.Structure structure)
        {
            StringBuilder sb = new("struct {");
            sb.AppendLine();
            indent.Increase();
            var components = structure.Components.Map.ToDictionary(kv => kv.Key,
                kv => Create(kv.Value, msger, generateExpression, keywordTable, indent));
            foreach (var comp in components) {
                indent.Indent(sb).Append(comp.Value.GenerateDeclaration(keywordTable.Validate(comp.Key, msger).Yield())).AppendLine(";");
            }
            indent.Decrease();
            sb.Append('}');
            return new(sb.ToString(),
                // it's ok if there are duplicate headers, since IncludeSet.Ensure will ignore duplicates.
                requiredHeaders: components.Values.SelectMany(type => type.RequiredHeaders));
        }
    }
}
