using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;

using Scover.Psdc.Pseudocode;

using System.Runtime.CompilerServices;

namespace Scover.Psdc.Messages;

public readonly struct Message
{
    Message(FixedRange location, MessageCode code, FuncOrValue<string, string> content) => (Code, Location, Content) = (code, location, content);

    Message(FixedRange location, MessageCode code, FuncOrValue<string, string> content, IReadOnlyList<string> advicePieces) =>
        (Code, Location, Content, AdvicePieces) = (code, location, content, advicePieces);

    public MessageCode Code { get; }
    public FuncOrValue<string, string> Content { get; }

    public IReadOnlyList<string> AdvicePieces { get; } = [];
    public FixedRange Location { get; }
    public MessageSeverity Severity {
        get {
            var severity = (MessageSeverity)((int)Code / 1000);
            Debug.Assert(Enum.IsDefined(severity));
            return severity;
        }
    }

    static string Fmt(ref DefaultInterpolatedStringHandler dish) => string.Create(Format.Msg, ref dish);

    static FormattableString Quantity(int quantity, string singular, string plural) => $"{quantity} {(quantity == 1 ? singular : plural)}";

    internal static Message ErrorInvalidEscapeSequence(FixedRange location, char escape, string details = "") => new(location,
        MessageCode.InvalidEscapeSequence,
        details.Length == 0
            ? Fmt($"invalid escape sequence {$"\\{escape}".WrapInCode()}")
            : Fmt($"invalid escape sequence {$"\\{escape}".WrapInCode()}: {details}"));

    internal static Message ErrorCallParameterMismatch(FixedRange location, Symbol.Callable f, IReadOnlyList<string> problems) => new(location,
        MessageCode.CallParameterMismatch,
        Fmt($"call to {f.Kind} {f.Name.WrapInCode()} does not correspond to signature"),
        problems);

    internal static Message ErrorAssertionFailed(FixedRange location, Option<string> message) => new(location, MessageCode.AssertionFailed,
        message.Match(ms => Fmt($"compile-time assertion failed: {ms}"),
            () => "compile-time assertion failed"));
    internal static Message HintExpressionValueUnused(FixedRange location) => new(location, MessageCode.ExpressionValueUnused,
        "value of expression unused");
    internal static Message ErrorIndexOutOfBounds(FixedRange location, int index, int length) => new(location, MessageCode.IndexOutOfBounds,
        Fmt($"index out of bounds for array"),
        [Fmt($"indexed at {index}, length is {length}")]);
    internal static Message ErrorSwitchDefaultIsNotLast(FixedRange location) => new(location, MessageCode.SwitchDefaultIsNotLast,
        "default not last in switch; all cases below are unreachable",
        ["move the default case to the end of the switch statement"]);
    internal static Message ErrorIndexOutOfBounds(ComptimeExpression<int> index, int length) =>
        ErrorIndexOutOfBounds(index.Expression.Meta.Location, index.Value, length);
    internal static Message ErrorFeatureComingSoon(FixedRange location, string feature) => new(location, MessageCode.FeatureNotAvailable,
        $"language feature '{feature}' not yet available");
    internal static Message ErrorConstantAssignment(FixedRange location, Symbol.Constant constant) => new(location, MessageCode.ConstantAssignment,
        Fmt($"reassigning constant {constant.Name.WrapInCode()}"));
    internal static Message DebugEvaluateExpression(FixedRange location, Value value) => new(location, MessageCode.EvaluateExpression,
        Fmt($"evaluated value: {value}"));
    internal static Message DebugEvaluateType(FixedRange location, EvaluatedType type) => new(location, MessageCode.EvaluateType,
        Fmt($"evaluated type: {type:f}"));
    internal static Message ErrorUnsupportedInitializer(FixedRange location, EvaluatedType initializerTargetType) => new(location,
        MessageCode.UnsupportedInitializer,
        Fmt($"unsupported initializer for type {initializerTargetType.ToString().WrapInCode()}"));

    internal static Message ErrorComptimeExpressionExpected(FixedRange location) => new(location, MessageCode.ConstantExpressionExpected,
        "constant expression expected");

    internal static Message ErrorExpressionHasWrongType(
        FixedRange location,
        EvaluatedType expected,
        EvaluatedType actual
    ) => new(location, MessageCode.ExpressionHasWrongType,
        Fmt($"can't convert expression of type '{actual}' to '{expected}'"));

    internal static Message ErrorUnterminatedStringLiteral(FixedRange location) =>
        new(location, MessageCode.UnterminatedStringLiteral, "Unterminated string literal");
    internal static Message ErrorUnterminatedCharLiteral(FixedRange location) =>
        new(location, MessageCode.UnterminatedCharLiteral, "Unterminated character literal");

    internal static Message ErrorCallableNotDefined(Symbol.Callable f) => new(f.Location, MessageCode.CallableNotDefined,
        Fmt($"{f.Kind} {f.Name.WrapInCode()} declared but not defined"),
        [Fmt($"provide a definition for {f.Name.WrapInCode()}")]);

    internal static Message WarningTargetLanguageReservedKeyword(FixedRange location, string targetLanguageName, string ident, string adjustedIdent) =>
        CreateTargetLanguageFormat(location, MessageCode.TargetLanguageReservedKeyword, targetLanguageName,
            $"identifier {ident.WrapInCode()} is a reserved {targetLanguageName} keyword, renamed to {adjustedIdent.WrapInCode()}");

    internal static Message ErrorReturnInNonReturnable(FixedRange location) => new(location, MessageCode.ReturnInNonReturnable,
        "return in something not a function, a procedure or a main program");
    internal static Message ErrorReturnExpectsValue(FixedRange location, EvaluatedType targetType) => new(location, MessageCode.ReturnExpectsValue,
        Fmt($"return here requires a value of type '{targetType}'"));
    internal static Message ErrorRedefinedMainProgram(FixedRange location) => new(location, MessageCode.RedefinedMainProgram,
        "more than one main program");

    internal static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol) => new(newSymbol.Name.Location, MessageCode.RedefinedSymbol,
        Fmt($"{newSymbol.Kind} {existingSymbol.Name.WrapInCode()} redefines a {existingSymbol.Kind} of the same name"));

    internal static Message ErrorCannotSwitchOnString(FixedRange location) => new(location, MessageCode.CannotSwitchOnString,
        "cannot switch on string");

    internal static Message ErrorSignatureMismatch<TSymbol>(TSymbol newSig, TSymbol expectedSig) where TSymbol : Symbol => new(newSig.Location,
        MessageCode.SignatureMismatch, new(input =>
            Fmt($"this signature of {newSig.Kind} {newSig.Name.WrapInCode()} differs from previous signature ({input[(Range)expectedSig.Location].WrapInCode()})")));

    internal static Message ErrorStructureComponentDoesntExist(
        Ident componentName,
        StructureType structType
    ) => new(componentName.Location, MessageCode.StructureComponentDoesntExist,
        structType.Alias.HasValue // avoid the long struct representation
            ? Fmt($"{structType.ToString().WrapInCode()} has no component named {componentName.WrapInCode()}")
            : Fmt($"no component named {componentName.WrapInCode()} in structure"));

    internal static Message ErrorCharLitEmpty(FixedRange location) => new(location,
        MessageCode.CharLitEmpty,
        "character literal is empty");

    internal static Message ErrorCharLitContainsMoreThanOneChar(FixedRange location, char firstChar) => new(location,
        MessageCode.CharLitContainsMoreThanOneChar,
        "character literal contains more than one character",
        [Fmt($"only first character '{firstChar}' is considered")]);

    internal static Message ErrorUnsupportedDesignator(FixedRange location, EvaluatedType targetType) => new(location, MessageCode.UnsupportedDesignator,
        Fmt($"unsupported designator in '{targetType}' initializer"));

    internal static Message ErrorStructureDuplicateComponent(FixedRange location, Ident componentName) => new(location, MessageCode.StructureDuplicateComponent,
        Fmt($"duplicate component {componentName.WrapInCode()} in structure is ignored"));

    internal static Message ErrorNonIntegerIndex(FixedRange location, EvaluatedType actualIndexType) => new(location, MessageCode.NonIntegerIndex,
        Fmt($"non integer ('{actualIndexType}') array index"));

    internal static Message ErrorSubscriptOfNonArray(FixedRange location, EvaluatedType actualArrayType) => new(location, MessageCode.SubscriptOfNonArray,
        Fmt($"subscripted value ('{actualArrayType}') is not an array"));

    internal static Message ErrorSyntax(FixedRange location, ParseError error)
    {
        StringBuilder msgContent = new("syntax: ");

        if (error.ExpectedProductions.Count > 0) {
            msgContent.Append(Format.Msg, $"on {error.FailedProduction}: expected ").AppendJoin(" or ", error.ExpectedProductions);
        } else if (location.Length == 0) {
            // show expected tokens only if failure token isn't the first, or if we successfully read at least 1 token.
            msgContent.Append(Format.Msg, $"on {error.FailedProduction}: expected ").AppendJoin(", ", error.ExpectedTokens);
        } else {
            msgContent.Append(Format.Msg, $"expected {error.FailedProduction}");
        }

        error.ErroneousToken.Tap(t => msgContent.Append(Format.Msg, $", got {t}"));

        return new(error.ErroneousToken
               .Map(t => (FixedRange)t.Position)
               .ValueOr(location),
            MessageCode.SyntaxError, msgContent.ToString());
    }

    internal static Message ErrorTargetLanguageFormat(FixedRange location, string targetLanguageName, FormattableString content) =>
        CreateTargetLanguageFormat(location, MessageCode.TargetLanguageError, targetLanguageName, content);

    internal static Message ErrorTargetLanguage(FixedRange location, string targetLanguageName, string content) =>
        CreateTargetLanguage(location, MessageCode.TargetLanguageError, targetLanguageName, content);

    internal static Message ErrorUndefinedSymbol<TSymbol>(Ident ident) where TSymbol : Symbol => new(ident.Location, MessageCode.UndefinedSymbol,
        Fmt($"undefined {Symbol.GetKind<TSymbol>()} {ident.WrapInCode()}"));

    internal static Message ErrorUndefinedSymbol<TSymbol>(Ident ident, Symbol existingSymbol) where TSymbol : Symbol => new(ident.Location,
        MessageCode.UndefinedSymbol,
        Fmt($"{ident.WrapInCode()} is a {existingSymbol.Kind}, {Symbol.GetKind<TSymbol>()} expected"));
    public static Message ErrorUnknownToken(FixedRange location) => new(location, MessageCode.UnknownToken, new(input =>
        Fmt($"stray {input[(Range)location].WrapInCode()} in program")));

    internal static Message ErrorUnsupportedOperation(Expr.BinaryOperation opBin, EvaluatedType leftType, EvaluatedType rightType) => new(opBin.Location,
        MessageCode.UnsupportedOperation,
        Fmt($"unsupported operand types for {opBin.Operator.Representation}: '{leftType}' and '{rightType}'"));
    internal static Message ErrorInvalidCast(FixedRange location, EvaluatedType sourceType, EvaluatedType targetType) => new(location, MessageCode.InvalidCast,
        Fmt($"Invalid cast: there is no implicit or explicit conversion from '{sourceType}' to '{targetType}'."));
    internal static Message ErrorUnsupportedOperation(Expr.UnaryOperation opUn, EvaluatedType operandType) => new(opUn.Location,
        MessageCode.UnsupportedOperation,
        Fmt($"unsupported operand type for {opUn.Operator.Representation}: '{operandType}'"));
    internal static Message HintRedundantCast(FixedRange location, EvaluatedType sourceType, EvaluatedType targetType) => new(location, MessageCode.RedundantCast,
        Fmt($"Rendundant cast from '{sourceType}' to '{targetType}': an implicit conversion exists"));
    internal static Message ErrrorComponentAccessOfNonStruct(Expr.Lvalue.ComponentAccess compAccess, EvaluatedType actualStructType) => new(compAccess.Location,
        MessageCode.ComponentAccessOfNonStruct,
        Fmt($"request for component {compAccess.ComponentName.WrapInCode()} in something ('{actualStructType}') not a structure"));

    internal static Message ErrorExcessElementInInitializer(FixedRange location) => new(location, MessageCode.ExcessElementInInitializer,
        "excess element in initializer");

    internal static string ProblemWrongArgumentMode(Ident name, string expected, string actual) =>
        Fmt($"wrong mode for {name.WrapInCode()}: expected '{expected}', got '{actual}'");

    internal static string ProblemWrongArgumentType(Ident name, EvaluatedType expected, EvaluatedType actual) =>
        Fmt($"wrong type for {name.WrapInCode()}: expected '{expected}', got '{actual}'");

    internal static string ProblemWrongNumberOfArguments(int expected, int actual) =>
        Fmt($"expected {Quantity(expected, "argument", "arguments")}, got {actual}");

    internal static Message WarningDivisionByZero(FixedRange location) => new(location, MessageCode.DivisionByZero,
        "division by zero will cause runtime error");

    internal static Message WarningFloatingPointEquality(FixedRange location) => new(location, MessageCode.FloatingPointEquality,
        "floating point equality may be inaccurate",
        ["consider comparing absolute difference to an epsilon value instead"]);

    internal static Message HintUnofficialFeature(FixedRange location, string feature, string? alternativeSolution = null) => new(location,
        MessageCode.UnofficialFeature,
        Fmt($"{(Random.Shared.Test(.1) ? "careful, my friend... " : "")}language feature '{feature}' is not official"),
        alternativeSolution is null ? [] : [alternativeSolution]);

    internal static Message HintUnofficialFeatureScalarInitializers(FixedRange location) => HintUnofficialFeature(location, "scalar initializers",
        "consider separating the initialization from the declaration in an assignent statement");

    static Message CreateTargetLanguageFormat(
        FixedRange location,
        MessageCode code,
        string targetLanguageName,
        FormattableString content
    ) => new(location, code,
        Fmt($"{targetLanguageName}: {content}"));

    static Message CreateTargetLanguage(
        FixedRange location,
        MessageCode code,
        string targetLanguageName,
        string content
    ) => new(location, code,
        Fmt($"{targetLanguageName}: {content}"));
}
