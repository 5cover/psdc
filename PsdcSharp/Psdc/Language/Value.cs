namespace Scover.Psdc.Language;

enum OperationMessage
{
    ErrorUnsupportedOperator,
    WarningDivisionByZero,
    WarningFloatingPointEquality,
}

interface Value<out TSelf> : Value
{
    /// <summary>
    /// Get the idiomatic expected evaluated type of a constant value of this type.
    /// </summary>
    static abstract EvaluatedType ExpectedType { get; }

    static abstract TSelf NoValue { get; }
}

interface Value<TSelf, TUnderlying> : Value where TSelf : Value<TSelf, TUnderlying>
{
    Option<TUnderlying> Value { get; }

    /// <summary>Perform a binary operation.</summary>
    public OperationResult<TResult> OperateWith<TResult>(TSelf other,
        Func<TUnderlying, TUnderlying, OperationResult<TResult>> transform)
        where TResult : Value<TResult>;

    /// <summary>Perform a binary infallible operation.</summary>
    public TResult OperateWith<TResult>(TSelf other,
        Func<TUnderlying, TUnderlying, TResult> transform)
        where TResult : Value<TResult>;

    /// <summary>Perform a binary internal operation.</summary>
    public OperationResult<TSelf> OperateWith(TSelf other,
        Func<TUnderlying, TUnderlying, OperationResult<TSelf>> transform);

    /// <summary>Perform a binary internal infallible operation.</summary>
    public TSelf OperateWith(TSelf other,
       Func<TUnderlying, TUnderlying, TUnderlying> transform);

    /// <summary>Perform an unary operation.</summary>
    public OperationResult<TResult> Operate<TResult>(
        Func<TUnderlying, OperationResult<TResult>> transform)
        where TResult : Value<TResult>;

    /// <summary>Perform an unary infallible operation.</summary>
    public TResult Operate<TResult>(
        Func<TUnderlying, TResult> transform)
        where TResult : Value<TResult>;

    /// <summary>Perform an unary internal operation.</summary>
    public OperationResult<TSelf> Operate(
        Func<TUnderlying, OperationResult<TSelf>> transform);

    /// <summary>Perform an unary internal infallible operation.</summary>
    public TSelf Operate(
        Func<TUnderlying, TUnderlying> transform);
}

interface Value : EquatableSemantics<Value>
{
    bool IsConstant { get; }

    public static Value Of(Option<EvaluatedType> optType)
     => optType.HasValue ? Of(optType.Value) : UnknownInferred;

    /// <summary>
    /// Create a non-constant value of the specified type.
    /// </summary>
    /// <param name="type">The type of the value to create.</param>
    /// <returns>A new <see cref="Value"/> object.</returns>
    public static Value Of(EvaluatedType type) => type switch {
        EvaluatedType.Boolean => Boolean.NoValue,
        EvaluatedType.Character => Character.NoValue,
        EvaluatedType.Integer => Integer.NoValue,
        EvaluatedType.String => String.NoValue,
        EvaluatedType.LengthedString ls => new String(ls),
        EvaluatedType.Real => Real.NoValue,
        EvaluatedType.Unknown u => new Unknown(u.Some()),
        // We could just add the non const types to the default pattern, but we want to fail fast if we addded a new EvaluatedType and forgot to add it here.
        EvaluatedType.Array
        or EvaluatedType.File
        or EvaluatedType.Structure => new NonConst(type),

        _ => throw type.ToUnmatchedException(),
    };

    public static Value UnknownInferred { get; } = new Unknown(Option.None<EvaluatedType.Unknown>());

    public Option<EvaluatedType> Type { get; }

    internal sealed class Boolean : ValueImpl<Boolean, bool>, Value<Boolean>
    {
        public Boolean(bool val) : base(val) { }
        Boolean() { }
        public static EvaluatedType ExpectedType => EvaluatedType.Boolean.Instance;
        public static Boolean NoValue { get; } = new();
        public override EvaluatedType Type => ExpectedType;
        protected override Boolean Create(bool val) => new(val);
    }

    internal sealed class Character : ValueImpl<Character, char>, Value<Character>
    {
        public Character(char val) : base(val) { }
        Character() { }
        public static Character NoValue { get; } = new();
        public static EvaluatedType ExpectedType => EvaluatedType.Character.Instance;
        public override EvaluatedType Type => ExpectedType;
        protected override Character Create(char val) => new(val);
    }

    internal sealed class Integer : Real, Value<Integer, int>, Value<Integer>
    {
        public Integer(int val) : base(val) => _impl = new(val);
        Integer() => _impl = new();
        readonly IntegerImpl _impl;
        public static new Integer NoValue { get; } = IntegerImpl.NoValue;
        public static new EvaluatedType ExpectedType => IntegerImpl.ExpectedType;
        public override EvaluatedType Type => _impl.Type;
        public new Option<int> Value => _impl.Value;

        sealed class IntegerImpl : ValueImpl<Integer, int>, Value<Integer>
        {
            public IntegerImpl(int val) : base(val) { }
            public IntegerImpl() { }
            public static EvaluatedType ExpectedType => EvaluatedType.Integer.Instance;
            public static Integer NoValue { get; } = new();
            public override EvaluatedType Type => ExpectedType;
            protected override Integer Create(int val) => new(val);
        }

        public OperationResult<TResult> OperateWith<TResult>(Integer other, Func<int, int, OperationResult<TResult>> transform) where TResult : Value<TResult> => _impl.OperateWith(other, transform);
        public TResult OperateWith<TResult>(Integer other, Func<int, int, TResult> transform) where TResult : Value<TResult> => _impl.OperateWith(other, transform);
        public OperationResult<Integer> OperateWith(Integer other, Func<int, int, OperationResult<Integer>> transform) => _impl.OperateWith(other, transform);
        public Integer OperateWith(Integer other, Func<int, int, int> transform) => _impl.OperateWith(other, transform);
        public OperationResult<TResult> Operate<TResult>(Func<int, OperationResult<TResult>> transform) where TResult : Value<TResult> => _impl.Operate(transform);
        public TResult Operate<TResult>(Func<int, TResult> transform) where TResult : Value<TResult> => _impl.Operate(transform);
        public OperationResult<Integer> Operate(Func<int, OperationResult<Integer>> transform) => _impl.Operate(transform);
        public Integer Operate(Func<int, int> transform) => _impl.Operate(transform);
    }

    internal class Real : ValueImpl<Real, decimal>, Value<Real>
    {
        public Real(decimal val) : base(val) { }
        protected Real() { }
        public static Real NoValue { get; } = new();
        public static EvaluatedType ExpectedType => EvaluatedType.Real.Instance;
        public override EvaluatedType Type => ExpectedType;
        protected override Real Create(decimal val) => new(val);
    }

    internal sealed class String : ValueImpl<String, string>, Value<String>
    {
        public String(EvaluatedType.LengthedString type) => Type = type;
        public String(string val) : base(val) => Type = EvaluatedType.LengthedString.Create(val.Length);
        String() => Type = ExpectedType;
        public static EvaluatedType ExpectedType => EvaluatedType.String.Instance;
        public static String NoValue { get; } = new();
        public override EvaluatedType Type { get; }
        protected override String Create(string val) => new();
    }

    internal sealed class NonConst(EvaluatedType type) : Value
    {
        public Option<EvaluatedType> Type { get; } = type.Some();
        public bool IsConstant => false;
        public bool SemanticsEqual(Value other) => other is NonConst o
         && o.Type.OptionSemanticsEqual(Type);
    }

    internal sealed class Unknown(Option<EvaluatedType.Unknown> type) : Value
    {
        public Option<EvaluatedType> Type => type;
        public bool IsConstant => false;
        public bool SemanticsEqual(Value other) => other is NonConst o
         && o.Type.OptionSemanticsEqual(Type);
    }
}
