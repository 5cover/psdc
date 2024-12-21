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
    Message(Range location, MessageCode code, FuncOrValue<string, string> content)
     => (Code, Location, Content) = (code, location, content);

    Message(Range location, MessageCode code, FuncOrValue<string, string> content, IReadOnlyList<string> advicePieces)
     => (Code, Location, Content, AdvicePieces) = (code, location, content, advicePieces);

    public MessageCode Code { get; }
    public FuncOrValue<string, string> Content { get; }

    public IReadOnlyList<string> AdvicePieces { get; } = ImmutableList<string>.Empty;
    public Range Location { get; }
    public MessageSeverity Severity {
        get {
            var severity = (MessageSeverity)((int)Code / 1000);
            Debug.Assert(Enum.IsDefined(severity));
            return severity;
        }
    }

    static string Fmt(ref DefaultInterpolatedStringHandler dish) => string.Create(Format.Msg, ref dish);

    static FormattableString Quantity(int quantity, string singular, string plural)
     => $"{quantity} {(quantity == 1 ? singular : plural)}";

    internal static Message ErrorCallParameterMismatch(Range location, Symbol.Callable f, IReadOnlyList<string> problems)
     => new(location, MessageCode.CallParameterMismatch,
        Fmt($"call to {f.Kind} `{f.Name}` does not correspond to signature"),
        problems);

    internal static Message ErrorAssertionFailed(Range location, Option<string> message)
     => new(location, MessageCode.AssertionFailed,
        message.Match(ms => Fmt($"compile-time assertion failed: {ms}"),
                      () => "compile-time assertion failed"));
    internal static Message HintExpressionValueUnused(Range location)
     => new(location, MessageCode.ExpressionValueUnused,
        "value of expression unused");
    internal static Message ErrorIndexOutOfBounds(Range location, int index, int length)
     => new(location, MessageCode.IndexOutOfBounds,
        Fmt($"index out of bounds for array"),
        [Fmt($"indexed at {index}, length is {length}")]);
    internal static Message ErrorSwitchDefaultIsNotLast(Range location)
     => new(location, MessageCode.SwitchDefaultIsNotLast,
        "default not last in switch; all cases below are unreachable",
        ["move the default case to the end of the switch statement"]);
    internal static Message ErrorIndexOutOfBounds(ComptimeExpression<int> index, int length)
     => ErrorIndexOutOfBounds(index.Expression.Meta.Location, index.Value, length);
    internal static Message ErrorFeatureComingSoon(Range location, string feature)
     => new(location, MessageCode.FeatureNotAvailable,
        $"language feature '{feature}' not yet available");
    internal static Message ErrorConstantAssignment(Range location, Symbol.Constant constant)
     => new(location, MessageCode.ConstantAssignment,
        Fmt($"reassigning constant `{constant.Name}`"));
    internal static Message DebugEvaluateExpression(Range location, Value value)
     => new(location, MessageCode.EvaluateExpression,
        Fmt($"evaluated value: {value}"));
    internal static Message DebugEvaluateType(Range location, EvaluatedType type)
     => new(location, MessageCode.EvaluateType,
        Fmt($"evaluated type: {type:f}"));
    internal static Message ErrorUnsupportedInitializer(Range location, EvaluatedType initializerTargetType)
     => new(location, MessageCode.UnsupportedInitializer,
        Fmt($"unsupported initializer for type `{initializerTargetType}`"));

    internal static Message ErrorComptimeExpressionExpected(Range location)
     => new(location, MessageCode.ConstantExpressionExpected,
        "constant expression expected");

    internal static Message ErrorExpressionHasWrongType(Range location,
        EvaluatedType expected, EvaluatedType actual)
     => new(location, MessageCode.ExpressionHasWrongType,
        Fmt($"can't convert expression of type '{actual}' to '{expected}'"));

    internal static Message ErrorCallableNotDefined(Symbol.Callable f)
     => new(f.Location, MessageCode.CallableNotDefined,
        Fmt($"{f.Kind} `{f.Name}` declared but not defined"),
        [Fmt($"provide a definition for `{f.Name}`")]);

    internal static Message WarningTargetLanguageReservedKeyword(Range location, string targetLanguageName, string ident, string adjustedIdent)
     => CreateTargetLanguageFormat(location, MessageCode.TargetLanguageReservedKeyword, targetLanguageName,
        $"identifier `{ident}` is a reserved {targetLanguageName} keyword, renamed to `{adjustedIdent}`");

    internal static Message ErrorReturnInNonReturnable(Range location)
     => new(location, MessageCode.ReturnInNonReturnable,
        "return in something not a function, a procedure or a main program");
    internal static Message ErrorReturnExpectsValue(Range location, EvaluatedType targetType)
     => new(location, MessageCode.ReturnExpectsValue,
        Fmt($"return here requires a value of type '{targetType}'"));
    internal static Message ErrorRedefinedMainProgram(Range location)
     => new(location, MessageCode.RedefinedMainProgram,
        "more than one main program");

    internal static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol)
     => new(newSymbol.Name.Location, MessageCode.RedefinedSymbol,
        Fmt($"{newSymbol.Kind} `{existingSymbol.Name}` redefines a {existingSymbol.Kind} of the same name"));

    internal static Message ErrorCannotSwitchOnString(Range location)
     => new(location, MessageCode.CannotSwitchOnString,
        "cannot switch on string");

    internal static Message ErrorSignatureMismatch<TSymbol>(TSymbol newSig, TSymbol expectedSig) where TSymbol : Symbol
     => new(newSig.Location, MessageCode.SignatureMismatch, new(input =>
        Fmt($"this signature of {newSig.Kind} `{newSig.Name}` differs from previous signature (`{input[expectedSig.Location]}`)")));

    internal static Message ErrorStructureComponentDoesntExist(Identifier component,
        StructureType structType)
     => new(component.Location, MessageCode.StructureComponentDoesntExist,
        structType.Alias is null // avoid the long struct representation
            ? Fmt($"no component named `{component}` in structure")
            : Fmt($"`{structType}` has no component named `{component}`"));

    internal static Message ErrorCharacterLiteralContainsMoreThanOneCharacter(Range location, char firstChar)
     => new(location, MessageCode.CharacterLiteralContainsMoreThanOneCharacter,
        "character literal contains more than one character",
        [Fmt($"only first character '{firstChar}' is considered")]);

    internal static Message ErrorUnsupportedDesignator(Range location, EvaluatedType targetType)
     => new(location, MessageCode.UnsupportedDesignator,
        Fmt($"unsupported designator in '{targetType}' initializer"));

    internal static Message ErrorStructureDuplicateComponent(Range location, Identifier component)
     => new(location, MessageCode.StructureDuplicateComponent,
        Fmt($"duplicate component `{component}` in structure is ignored"));

    internal static Message ErrorNonIntegerIndex(Range location, EvaluatedType actualIndexType)
     => new(location, MessageCode.NonIntegerIndex,
        Fmt($"non integer ('{actualIndexType}') array index"));

    internal static Message ErrorSubscriptOfNonArray(Range location, EvaluatedType actualArrayType)
     => new(location, MessageCode.SubscriptOfNonArray,
        Fmt($"subscripted value ('{actualArrayType}') is not an array"));

    internal static Message ErrorSyntax(Range location, ParseError error)
    {
        StringBuilder msgContent = new("syntax: ");

        if (error.ExpectedProductions.Count > 0) {
            msgContent.Append(Format.Msg, $"on {error.FailedProduction}: expected ").AppendJoin(" or ", error.ExpectedProductions);
        } else if (location.IsEmpty()) {
            // show expected tokens only if failure token isn't the first, or if we successfully read at least 1 token.
            msgContent.Append(Format.Msg, $"on {error.FailedProduction}: expected ").AppendJoin(", ", error.ExpectedTokens);
        } else {
            msgContent.Append(Format.Msg, $"expected {error.FailedProduction}");
        }

        error.ErroneousToken.Tap(t => msgContent.Append(Format.Msg, $", got {t}"));

        return new(error.ErroneousToken
            .Map(t => (Range)t.Position)
            .ValueOr(location),
            MessageCode.SyntaxError, msgContent.ToString());
    }

    internal static Message ErrorTargetLanguageFormat(Range location, string targetLanguageName, FormattableString content)
     => CreateTargetLanguageFormat(location, MessageCode.TargetLanguageError, targetLanguageName, content);

    internal static Message ErrorTargetLanguage(Range location, string targetLanguageName, string content)
     => CreateTargetLanguage(location, MessageCode.TargetLanguageError, targetLanguageName, content);

    internal static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier) where TSymbol : Symbol
     => new(identifier.Location, MessageCode.UndefinedSymbol,
        Fmt($"undefined {Symbol.GetKind<TSymbol>()} `{identifier}`"));

    internal static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier, Symbol existingSymbol) where TSymbol : Symbol
     => new(identifier.Location, MessageCode.UndefinedSymbol,
        Fmt($"`{identifier}` is a {existingSymbol.Kind}, {Symbol.GetKind<TSymbol>()} expected"));
    internal static Message ErrorUnknownToken(Range location)
     => new(location, MessageCode.UnknownToken, new(input =>
        Fmt($"stray `{input[location]}` in program")));

    internal static Message ErrorUnsupportedOperation(Expression.BinaryOperation opBin, EvaluatedType leftType, EvaluatedType rightType)
     => new(opBin.Location, MessageCode.UnsupportedOperation,
        Fmt($"unsupported operand types for {opBin.Operator.Representation}: '{leftType}' and '{rightType}'"));
    internal static Message ErrorInvalidCast(Range location, EvaluatedType sourceType, EvaluatedType targetType)
     => new(location, MessageCode.InvalidCast,
        Fmt($"Invalid cast: there is no implicit or explicit conversion from '{sourceType}' to '{targetType}'."));
    internal static Message ErrorUnsupportedOperation(Expression.UnaryOperation opUn, EvaluatedType operandType)
     => new(opUn.Location, MessageCode.UnsupportedOperation,
        Fmt($"unsupported operand type for {opUn.Operator.Representation}: '{operandType}'"));
    internal static Message HintRedundantCast(Range location, EvaluatedType sourceType, EvaluatedType targetType)
     => new(location, MessageCode.RedundantCast,
        Fmt($"Rendundant cast from '{sourceType}' to '{targetType}': an implicit conversion exists"));
    internal static Message ErrrorComponentAccessOfNonStruct(Expression.Lvalue.ComponentAccess compAccess, EvaluatedType actualStructType)
     => new(compAccess.Location, MessageCode.ComponentAccessOfNonStruct,
        Fmt($"request for component `{compAccess.ComponentName}` in something ('{actualStructType}') not a structure"));

    internal static Message ErrorExcessElementInInitializer(Range location)
     => new(location, MessageCode.ExcessElementInInitializer,
        "excess element in initializer");

    internal static string ProblemWrongArgumentMode(Identifier name, string expected, string actual)
     => Fmt($"wrong mode for `{name}`: expected '{expected}', got '{actual}'");

    internal static string ProblemWrongArgumentType(Identifier name, EvaluatedType expected, EvaluatedType actual)
     => Fmt($"wrong type for `{name}`: expected '{expected}', got '{actual}'");

    internal static string ProblemWrongNumberOfArguments(int expected, int actual)
     => Fmt($"expected {Quantity(expected, "argument", "arguments")}, got {actual}");

    internal static Message WarningDivisionByZero(Range location)
     => new(location, MessageCode.DivisionByZero,
        "division by zero will cause runtime error");

    internal static Message WarningFloatingPointEquality(Range location)
     => new(location, MessageCode.FloatingPointEquality,
        "floating point equality may be inaccurate",
        ["consider comparing absolute difference to an epsilon value instead"]);

    internal static Message HintUnofficialFeature(Range location, string feature, string? alternativeSolution = null)
     => new(location, MessageCode.UnofficialFeature,
        Fmt($"{(Random.Shared.Test(.1) ? "careful, my friend... " : "")}language feature '{feature}' is not official"),
        alternativeSolution is null ? [] : [alternativeSolution]);

    internal static Message HintUnofficialFeatureScalarInitializers(Range location)
     => HintUnofficialFeature(location, "scalar initializers",
        "consider separating the initialization from the declaration in an assignent statement");

    static Message CreateTargetLanguageFormat(Range location, MessageCode code,
        string targetLanguageName, FormattableString content)
     => new(location, code,
        Fmt($"{targetLanguageName}: {content}"));

    static Message CreateTargetLanguage(Range location, MessageCode code,
            string targetLanguageName, string content)
     => new(location, code,
        Fmt($"{targetLanguageName}: {content}"));
}
