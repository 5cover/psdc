using System.Text;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.CodeGeneration.C;

internal sealed class TypeInfoC : TypeInfo
{
    public static TypeInfoC Create(EvaluatedType type, Func<Node.Expression, string> generateExpression)
     => Create(type, generateExpression, new());
    private static TypeInfoC Create(EvaluatedType type, Func<Node.Expression, string> generateExpression, Indentation indent)
    {
        switch (type) {
        case EvaluatedType.Unknown u:
            return new(type, u.Representation);
        case EvaluatedType.File:
            return new(type, "FILE", starCount: 1, requiredHeaders: IncludeSet.StdIo.Yield());
        case EvaluatedType.Boolean:
            return new(type, "bool", requiredHeaders: IncludeSet.StdBool.Yield());
        case EvaluatedType.Character:
            return new(type, "char", "%c");
        case EvaluatedType.Numeric numeric:
            return numeric.Type switch {
                NumericType.Integer => new(type, "int", "%d"),
                NumericType.Real => new(type, "float", "%g"),
                _ => throw numeric.Type.ToUnmatchedException(),
            };
        case EvaluatedType.String:
            return new(type, "char", "%s", starCount: 1);
        case EvaluatedType.Array array:
            var arrayType = Create(array.ElementType, generateExpression, indent);
            StringBuilder postModifier = new(arrayType._postModifier);
            foreach (var dimension in array.DimensionConstantExpressions.Select(generateExpression)) {
                postModifier.Append($"[{dimension}]");
            }
            return new(type, arrayType._typeName,
                requiredHeaders: arrayType.RequiredHeaders,
                starCount: arrayType._stars.Length,
                postModifier: postModifier.ToString());
        case EvaluatedType.StringLengthed strlen:
            // add 1 to the length for null terminator
            string lengthPlus1 = strlen.LengthConstantExpression
                .Map(len => generateExpression(
                    // This feels like cheating, creatig AST nodes with placeholder source tokens during code generation
                    // But this is the only way without massive abstraction
                    // This will be improved if it ever becomes a problem.
                    new Node.Expression.OperationBinary(len.SourceTokens, len, BinaryOperator.Plus,
                        new Node.Expression.Literal.Integer(SourceTokens.Empty, "1"))))
                .ValueOr((strlen.Length + 1).ToString());
            return new(type, "char", "%s", postModifier: $"[{lengthPlus1}]");
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
            return new(type, sb.ToString(),
                // it's ok if there are duplicate headers, since IncludeSet.Ensure will ignore duplicates.
                requiredHeaders: components.Values.SelectMany(type => type.RequiredHeaders));
        default:
            throw type.ToUnmatchedException();
        }
    }

    private readonly string _typeName, _postModifier, _typeQualifier;
    private readonly string _stars;
    private TypeInfoC(string typeName, Option<string> formatComponent, IEnumerable<string> requiredHeaders, int starCount = 0, string postModifier = "", string? typeQualifier = null)
     => (_stars, _typeName, _postModifier, _typeQualifier, FormatComponent, RequiredHeaders)
        = (new string('*', starCount),
           typeName,
           postModifier,
           AddSpaceBefore(typeQualifier),
           formatComponent,
           requiredHeaders);

    private TypeInfoC(EvaluatedType type, string actualTypeName, string? formatCompnent = null, IEnumerable<string>? requiredHeaders = null, int starCount = 0, string postModifier = "", string? typeQualifier = null)
    : this(type.Alias?.Name ?? actualTypeName,
           formatCompnent.SomeNotNull(),
           requiredHeaders ?? [],
           starCount,
           postModifier,
           typeQualifier)
    {
    }

    public Option<string> FormatComponent { get; }
    public IEnumerable<string> RequiredHeaders { get; }

    public TypeInfoC ToPointer(int level)
     => new(_typeName, FormatComponent, RequiredHeaders, _stars.Length + level,
        _postModifier, null);

    public TypeInfoC ToConst()
     => new(_typeName, FormatComponent, RequiredHeaders, _stars.Length, _postModifier,
        "const");

    public string DecorateExpression(string expr)
     => $"{_stars}{expr}";

    public string GenerateDeclaration(IEnumerable<Identifier> names)
     => $"{_typeName}{_typeQualifier} {string.Join(", ", names.Select(name => _stars + name.Name + _postModifier))}";

    public string Generate() => $"{_typeName}{_stars}{_postModifier}{_typeQualifier}";

    private static string AddSpaceBefore(string? str) => str is null ? "" : $" {str}";
}
