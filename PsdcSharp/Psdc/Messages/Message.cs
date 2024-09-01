using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;
using Scover.Psdc.Pseudocode;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Scover.Psdc.Messages;

public readonly struct Message
{
    Message(Range inputRange, MessageCode code, FuncOrValue<string, string> content)
     => (InputRange, Code, Content) = (inputRange, code, content);

    Message(SourceTokens sourceTokens, MessageCode code, FuncOrValue<string, string> content)
     => (Code, InputRange, Content) = (code, sourceTokens.InputRange, content);

    Message(SourceTokens sourceTokens, MessageCode code, FuncOrValue<string, string> content, IReadOnlyList<string> advicePieces)
     => (Code, InputRange, Content, AdvicePieces) = (code, sourceTokens.InputRange, content, advicePieces);

    public MessageCode Code { get; }
    public FuncOrValue<string, string> Content { get; }

    public IReadOnlyList<string> AdvicePieces { get; } = ImmutableList<string>.Empty;
    public Range InputRange { get; }
    public MessageSeverity Severity {
        get {
            var severity = (MessageSeverity)((int)Code / 1000);
            Debug.Assert(Enum.IsDefined(severity));
            return severity;
        }
    }

    private static string Fmt(ref DefaultInterpolatedStringHandler dish) => string.Create(Format.Msg, ref dish);

    static FormattableString Quantity(int quantity, string singular, string plural)
     => $"{quantity} {(quantity == 1 ? singular : plural)}";

    internal static Message ErrorCallParameterMismatch(SourceTokens sourceTokens, Symbol.Function f, IReadOnlyList<string> problems)
     => new(sourceTokens, MessageCode.CallParameterMismatch,
        Fmt($"call to {f.Kind} `{f.Name}` does not correspond to signature"),
        problems);

    internal static Message ErrorAssertionFailed(CompilerDirective compilerDirective, Option<string> message)
     => new(compilerDirective.SourceTokens, MessageCode.AssertionFailed,
        message.Match(ms => Fmt($"compile-time assertion failed: {ms}"),
                      () => "compile-time assertion failed"));
    internal static Message SuggestionExpressionValueUnused(Expression expr)
     => new(expr.SourceTokens, MessageCode.ExpressionValueUnused,
        "value of expression unused");
    internal static Message ErrorIndexOutOfBounds(SourceTokens sourceTokens, int index, int length)
     => new(sourceTokens, MessageCode.IndexOutOfBounds,
        Fmt($"index out of bounds for array"),
        [Fmt($"indexed at {index}, length is {length}")]);

    internal static Message ErrorIndexOutOfBounds(ComptimeExpression<int> index, int length)
     => ErrorIndexOutOfBounds(index.Expression.Meta.SourceTokens, index.Value, length);
    internal static Message ErrorFeatureComingSoon(SourceTokens sourceTokens, string feature)
     => new(sourceTokens, MessageCode.FeatureComingSoon,
        $"language feature '{feature}' not yet available");
    internal static Message ErrorConstantAssignment(Statement.Assignment assignment, Symbol.Constant constant)
     => new(assignment.SourceTokens, MessageCode.ConstantAssignment,
        Fmt($"reassigning constant `{constant.Name}`"));
    internal static Message DebugEvaluateExpression(SourceTokens sourceTokens, Value value)
     => new(sourceTokens, MessageCode.EvaluateExpression,
        Fmt($"evaluated value: {value}"));
    internal static Message DebugEvaluateType(SourceTokens sourceTokens, EvaluatedType type)
     => new(sourceTokens, MessageCode.EvaluateType,
        Fmt($"evaluated type: {type}"));
    internal static Message ErrorUnsupportedInitializer(SourceTokens sourceTokens, EvaluatedType initializerTargetType)
     => new(sourceTokens, MessageCode.UnsupportedInitializer,
        Fmt($"unsupported initializer for type `{initializerTargetType}`"));

    internal static Message ErrorComptimeExpressionExpected(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.ConstantExpressionExpected,
        "constant expression expected");

    internal static Message ErrorExpressionHasWrongType(SourceTokens sourceTokens,
        EvaluatedType expected, EvaluatedType actual)
     => new(sourceTokens, MessageCode.ExpressionHasWrongType,
        Fmt($"can't convert expression of type '{actual}' to '{expected}'"));

    internal static Message ErrorFunctionNotDefined(Symbol.Function f)
     => new(f.SourceTokens, MessageCode.CallableNotDefined,
        Fmt($"{f.Kind} `{f.Name}` declared but not defined"),
        [Fmt($"provide a definition for `{f.Name}`")]);

    internal static Message WarningTargetLanguageReservedKeyword(SourceTokens sourceTokens, string targetLanguageName, string ident, string adjustedIdent)
     => CreateTargetLanguageFormat(sourceTokens, MessageCode.TargetLanguageReservedKeyword, targetLanguageName,
        $"identifier `{ident}` is a reserved {targetLanguageName} keyword, renamed to `{adjustedIdent}`");

    internal static Message ErrorReturnInNonFunction(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.ReturnInNonFunction,
        "return in something not a function");

    internal static Message ErrorRedefinedMainProgram(Declaration.MainProgram mainProgram)
     => new(mainProgram.SourceTokens, MessageCode.RedefinedMainProgram,
        "more than one main program");

    internal static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol)
     => new(newSymbol.Name.SourceTokens, MessageCode.RedefinedSymbol,
        Fmt($"{newSymbol.Kind} `{existingSymbol.Name}` redefines a {existingSymbol.Kind} of the same name"));

    internal static Message ErrorCannotSwitchOnString(Statement.Switch @switch)
     => new(@switch.SourceTokens, MessageCode.CannotSwitchOnString,
        "cannot switch on string");

    internal static Message ErrorSignatureMismatch<TSymbol>(TSymbol newSig, TSymbol expectedSig) where TSymbol : Symbol
     => new(newSig.SourceTokens, MessageCode.SignatureMismatch, new(input =>
        Fmt($"this signature of {newSig.Kind} `{newSig.Name}` differs from previous signature (`{input[expectedSig.SourceTokens.InputRange]}`)")));

    internal static Message ErrorStructureComponentDoesntExist(Identifier component,
        StructureType structType)
     => new(component.SourceTokens, MessageCode.StructureComponentDoesntExist,
        structType.Alias is null // avoid the long struct representation
            ? Fmt($"no component named `{component}` in structure")
            : Fmt($"`{structType}` has no component named `{component}`"));

    internal static Message ErrorUnsupportedDesignator(SourceTokens sourceTokens, EvaluatedType targetType)
     => new(sourceTokens, MessageCode.UnsupportedDesignator,
        Fmt($"unsupported designator in '{targetType}' initializer"));

    internal static Message ErrorStructureDuplicateComponent(SourceTokens sourceTokens, Identifier component)
     => new(sourceTokens, MessageCode.StructureDuplicateComponent,
        Fmt($"duplicate component `{component}` in structure is ignored"));

    internal static Message ErrorNonIntegerIndex(SourceTokens sourceTokens, EvaluatedType actualIndexType)
     => new(sourceTokens, MessageCode.NonIntegerIndex,
        Fmt($"non integer ('{actualIndexType}') array index"));

    internal static Message ErrorSubscriptOfNonArray(Expression.Lvalue.ArraySubscript arrSub, EvaluatedType actualArrayType)
     => new(arrSub.SourceTokens, MessageCode.SubscriptOfNonArray,
        Fmt($"subscripted value ('{actualArrayType}') is not an array"));

    internal static Message ErrorSyntax(SourceTokens sourceTokens, ParseError error)
    {
        StringBuilder msgContent = new("syntax: ");

        if (error.ExpectedProductions.Count > 0) {
            msgContent.Append(Format.Msg, $"on {error.FailedProduction}: expected ").AppendJoin(" or ", error.ExpectedProductions);
        } else if (sourceTokens.Count > 0) {
            // show expected tokens only if failure token isn't the first, or if we successfully read at least 1 token.
            msgContent.Append(Format.Msg, $"on {error.FailedProduction}: expected ").AppendJoin(", ", error.ExpectedTokens);
        } else {
            msgContent.Append(Format.Msg, $"expected {error.FailedProduction}");
        }

        error.ErroneousToken.Tap(t => msgContent.Append(Format.Msg, $", got {t}"));

        return new(error.ErroneousToken
            .Map(t => t.InputRange)
            .ValueOr(sourceTokens.InputRange),
            MessageCode.SyntaxError, msgContent.ToString());
    }

    internal static Message ErrorTargetLanguageFormat(SourceTokens sourceTokens, string targetLanguageName, FormattableString content)
     => CreateTargetLanguageFormat(sourceTokens, MessageCode.TargetLanguageError, targetLanguageName, content);

    internal static Message ErrorTargetLanguage(SourceTokens sourceTokens, string targetLanguageName, string content)
     => CreateTargetLanguage(sourceTokens, MessageCode.TargetLanguageError, targetLanguageName, content);

    internal static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier) where TSymbol : Symbol
     => new(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        Fmt($"undefined {Symbol.GetKind<TSymbol>()} `{identifier}`"));

    internal static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier, Symbol existingSymbol) where TSymbol : Symbol
     => new(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        Fmt($"`{identifier}` is a {existingSymbol.Kind}, {Symbol.GetKind<TSymbol>()} expected"));
    internal static Message ErrorUnknownToken(Range inputRange)
     => new(inputRange, MessageCode.UnknownToken, new(input =>
        Fmt($"stray `{input[inputRange]}` in program")));

    internal static Message ErrorUnsupportedOperation(Expression.BinaryOperation opBin, EvaluatedType leftType, EvaluatedType rightType)
     => new(opBin.SourceTokens, MessageCode.UnsupportedOperation,
        Fmt($"unsupported operand types for {opBin.Operator.Representation}: '{leftType}' and '{rightType}'"));
    internal static Message ErrorInvalidCast(SourceTokens sourceTokens, EvaluatedType sourceType, EvaluatedType targetType)
     => new(sourceTokens, MessageCode.InvalidCast,
        Fmt($"Invalid cast: there is no explicit conversion from '{sourceType}' to '{targetType}'."));
    internal static Message ErrorUnsupportedOperation(Expression.UnaryOperation opUn, EvaluatedType operandType)
     => new(opUn.SourceTokens, MessageCode.UnsupportedOperation,
        Fmt($"unsupported operand type for {opUn.Operator.Representation}: '{operandType}'"));
    internal static Message SuggestionRedundantCast(SourceTokens sourceTokens, EvaluatedType sourceType, EvaluatedType targetType)
     => new(sourceTokens, MessageCode.RedundantCast,
        Fmt($"Rendundant cast from '{sourceType}' to '{targetType}': an implicit conversion exists"));
    internal static Message ErrrorComponentAccessOfNonStruct(Expression.Lvalue.ComponentAccess compAccess, EvaluatedType actualStructType)
     => new(compAccess.SourceTokens, MessageCode.ComponentAccessOfNonStruct,
        Fmt($"request for component `{compAccess.ComponentName}` in something ('{actualStructType}') not a structure"));

    internal static Message ErrorExcessElementInInitializer(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.ExcessElementInInitializer,
        "excess element in initializer");

    internal static string ProblemWrongArgumentMode(Identifier name, string expected, string actual)
     => Fmt($"wrong mode for `{name}`: expected '{expected}', got '{actual}'");

    internal static string ProblemWrongArgumentType(Identifier name, EvaluatedType expected, EvaluatedType actual)
     => Fmt($"wrong type for `{name}`: expected '{expected}', got '{actual}'");

    internal static string ProblemWrongNumberOfArguments(int expected, int actual)
     => Fmt($"expected {Quantity(expected, "argument", "arguments")}, got {actual}");

    internal static Message WarningDivisionByZero(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.DivisionByZero,
        "division by zero will cause runtime error");

    internal static Message WarningFloatingPointEquality(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.FloatingPointEquality,
        "floating point equality may be inaccurate",
        ["consider comparing absolute difference to an epsilon value instead"]);

    static Message CreateTargetLanguageFormat(SourceTokens sourceTokens, MessageCode code,
        string targetLanguageName, FormattableString content)
     => new(sourceTokens, code,
        Fmt($"{targetLanguageName}: {content}"));

    static Message CreateTargetLanguage(SourceTokens sourceTokens, MessageCode code,
            string targetLanguageName, string content)
     => new(sourceTokens, code,
        Fmt($"{targetLanguageName}: {content}"));
}
