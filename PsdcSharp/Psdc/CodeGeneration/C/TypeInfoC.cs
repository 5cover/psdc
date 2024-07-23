using System.Text;

using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

sealed class TypeInfoC : TypeInfo
{
    readonly string _stars;

    readonly string _typeName, _postModifier, _typeQualifier;

    TypeInfoC(string typeName, Option<string> formatComponent, IEnumerable<string> requiredHeaders, int starCount = 0, string postModifier = "", string? typeQualifier = null)
     => (_stars, _typeName, _postModifier, _typeQualifier, FormatComponent, RequiredHeaders)
        = (new string('*', starCount),
           typeName,
           postModifier,
           AddSpaceBefore(typeQualifier),
           formatComponent,
           requiredHeaders);

    TypeInfoC(string typeName, string? formatCompnent = null, IEnumerable<string>? requiredHeaders = null, int starCount = 0, string postModifier = "", string? typeQualifier = null)
    : this(typeName,
           formatCompnent.SomeNotNull(),
           requiredHeaders ?? [],
           starCount,
           postModifier,
           typeQualifier)
    {
    }

    public Option<string> FormatComponent { get; }

    public IEnumerable<string> RequiredHeaders { get; }

    public static TypeInfoC Create(EvaluatedType type, Messenger messenger, Func<Expression, string> generateExpression, KeywordTable keywordTable)
     => Create(type, messenger, generateExpression, keywordTable, new());

    public string DecorateExpression(string expr)
     => $"{_stars}{expr}";

    public string Generate() => $"{_typeName}{_stars}{_postModifier}{_typeQualifier}";

    public string GenerateDeclaration(IEnumerable<string> names)
     => $"{_typeName}{_typeQualifier} {string.Join(", ", names.Select(name => _stars + name + _postModifier))}";

    public TypeInfoC ToConst()
     => new(_typeName, FormatComponent, RequiredHeaders, _stars.Length, _postModifier,
        "const");

    public TypeInfoC ToPointer(int level)
     => new(_typeName, FormatComponent, RequiredHeaders, _stars.Length + level,
        _postModifier, null);

    static string AddSpaceBefore(string? str) => str is null ? "" : $" {str}";

    static TypeInfoC Create(EvaluatedType type, Messenger msger, Func<Expression, string> generateExpression, KeywordTable keywordTable, Indentation indent)
    {
        switch (type) {
        case EvaluatedType.Unknown u:
            return new(AliasNameOr(keywordTable.Validate(u.SourceTokens, u.Representation, msger)));
        case EvaluatedType.File:
            return new(AliasNameOr("FILE"), starCount: 1, requiredHeaders: IncludeSet.StdIo.Yield());
        case EvaluatedType.Boolean:
            return new(AliasNameOr("bool"), requiredHeaders: IncludeSet.StdBool.Yield());
        case EvaluatedType.Character:
            return new(AliasNameOr("char"), "%c");
        case EvaluatedType.Real real:
            return new(AliasNameOr("float"), "%g");
        case EvaluatedType.Integer integer:
            return new(AliasNameOr("int"), "%d");
        case EvaluatedType.String:
            return new(AliasNameOr("char"), "%s", starCount: 1);
        case EvaluatedType.Array array:
            var arrayType = Create(array.ElementType, msger, generateExpression, keywordTable, indent);
            StringBuilder postModifier = new(arrayType._postModifier);
            foreach (var dimension in array.Dimensions.Select(dim => generateExpression(dim.Expression))) {
                postModifier.Append($"[{dimension}]");
            }
            return new(AliasNameOr(arrayType._typeName),
                requiredHeaders: arrayType.RequiredHeaders,
                starCount: arrayType._stars.Length,
                postModifier: postModifier.ToString());
        case EvaluatedType.LengthedString strlen:
            // add 1 to the length for null terminator
            string lengthPlus1 = strlen.LengthConstantExpression
                .Map(len => {
                    var (expression, messages) = len.Alter(BinaryOperator.Add, 1);
                    msger.ReportAll(messages);
                    return generateExpression(expression);
                })
                .ValueOr((strlen.Length + 1).ToString());
            return new(AliasNameOr("char"), "%s", postModifier: $"[{lengthPlus1}]");
        case EvaluatedType.Structure structure:
            StringBuilder sb = new("struct {");
            sb.AppendLine();
            indent.Increase();
            var components = structure.Components.ToDictionary(kv => kv.Key,
                kv => Create(kv.Value, msger, generateExpression, keywordTable, indent));
            foreach (var comp in components) {
                indent.Indent(sb).Append(comp.Value.GenerateDeclaration(keywordTable.Validate(comp.Key, msger).Yield())).AppendLine(";");
            }
            indent.Decrease();
            sb.Append('}');
            return new(AliasNameOr(sb.ToString()),
                // it's ok if there are duplicate headers, since IncludeSet.Ensure will ignore duplicates.
                requiredHeaders: components.Values.SelectMany(type => type.RequiredHeaders));
        default:
            throw type.ToUnmatchedException();
        }

        string AliasNameOr(string baseName)
         => type.Alias is null ? baseName : keywordTable.Validate(type.Alias, msger);
    }
}
