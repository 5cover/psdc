using System.Text;

using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.CodeGeneration.C;

sealed class TypeInfo : CodeGeneration.TypeInfo
{
    public readonly record struct Help(
        Scope Scope,
        Messenger Msger,
        CodeGeneration.KeywordTable KwTable,
        Generator<SemanticNode.Expression> GenExpr,
        Generator<SemanticNode.Expression> GenExprAdd1);

    readonly string _stars;

    readonly string _typeName, _postModifier, _typeQualifier;

    TypeInfo(string typeName, ValueOption<string> formatComponent = default, IEnumerable<string>? requiredHeaders = null, int starCount = 0, string postModifier = "", string? typeQualifier = null)
     => (_stars, _typeName, _postModifier, _typeQualifier, FormatComponent, RequiredHeaders)
        = (new string('*', starCount),
           typeName,
           postModifier,
           AddSpaceBefore(typeQualifier),
           formatComponent,
           requiredHeaders ?? []);

    public ValueOption<string> FormatComponent { get; }

    public IEnumerable<string> RequiredHeaders { get; }

    public string DecorateExpression(string expr)
     => string.Concat(_stars, expr);
    public override string ToString() => string.Concat(_typeName, _stars, _postModifier, _typeQualifier);

    public string GenerateDeclaration(IEnumerable<string> declarators)
     => string.Concat(_typeName, _typeQualifier,
            string.Join(", ", declarators.Select(name => string.Concat(_stars, name, _postModifier))));

    public string GenerateDeclaration(string declarator) => GenerateDeclaration(declarator.Yield());

    public TypeInfo ToConst()
     => new(_typeName, FormatComponent, RequiredHeaders, _stars.Length, _postModifier,
        "const");

    public TypeInfo ToPointer(int level)
     => new(_typeName, FormatComponent, RequiredHeaders, _stars.Length + level,
        _postModifier, null);

    static string AddSpaceBefore(string? str) => str is null ? "" : string.Concat(" ", str);

    public static TypeInfo Create(EvaluatedType type, Help help)
     => Create(type, new(), help);

    static TypeInfo Create(EvaluatedType type, Indentation indent, Help help)
    {
        TypeInfo typeInfo = type switch {
            UnknownType u => new(help.KwTable.Validate(help.Scope, u.SourceTokens, u.ToString(Format.Code), help.Msger)),
            FileType => new("FILE", starCount: 1, requiredHeaders: IncludeSet.StdIo.Yield()),
            BooleanType => new("bool", requiredHeaders: IncludeSet.StdBool.Yield()),
            CharacterType => new("char", "%c"),
            RealType real => new("float", "%g"),
            IntegerType integer => new("int", "%d"),
            StringType => new("char const", "%s", starCount: 1),
            ArrayType array => CreateArrayType(array, help),
            LengthedStringType strlen => CreateLengthedString(strlen, help),
            StructureType structure => CreateStructure(structure, help),
            _ => throw type.ToUnmatchedException(),
        };

        return type.Alias.Match(
            a => new TypeInfo(help.KwTable.Validate(help.Scope, a, help.Msger), typeInfo.FormatComponent, typeInfo.RequiredHeaders),
            () => typeInfo);

        TypeInfo CreateArrayType(ArrayType array, Help help)
        {
            var arrayType = Create(array.ItemType, indent, help);
            StringBuilder postModifier = new(arrayType._postModifier);
            foreach (var dimension in array.Dimensions.Select(dim => help.GenExpr(dim.Expression))) {
                postModifier.Append(Format.Code, $"[{dimension}]");
            }
            return new(arrayType._typeName,
                requiredHeaders: arrayType.RequiredHeaders,
                starCount: arrayType._stars.Length,
                postModifier: postModifier.ToString());
        }

        TypeInfo CreateLengthedString(LengthedStringType strlen, Help help)
        {
            // add 1 to the length for null terminator
            string lengthPlus1 = strlen.LengthConstantExpression
                .Map(help.GenExpr.Invoke)
                .ValueOr((strlen.Length + 1).ToString(Format.Code));
            return new("char const", "%s", postModifier: $"[{lengthPlus1}]");
        }

        TypeInfo CreateStructure(StructureType structure, Help help)
        {
            StringBuilder sb = new("struct {");
            sb.AppendLine();
            indent.Increase();
            var components = structure.Components.Map.ToDictionary(kv => kv.Key,
                kv => Create(kv.Value, indent, help));
            foreach (var comp in components) {
                indent.Indent(sb).Append(comp.Value.GenerateDeclaration(help.KwTable.Validate(help.Scope, comp.Key, help.Msger).Yield())).AppendLine(";");
            }
            indent.Decrease();
            sb.Append('}');
            return new(sb.ToString(),
                // it's ok if there are duplicate headers, since IncludeSet.Ensure will ignore duplicates.
                requiredHeaders: components.Values.SelectMany(type => type.RequiredHeaders));
        }
    }
}
