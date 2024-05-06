namespace Scover.Psdc.Language;

enum OperationError
{
    UnsupportedOperator,
    DivisionByZero,
    FloatingPointEquality,
}

interface Value<out TSelf> : Value
{
    /// <summary>
    /// Get the idiomatic expected evaluated type of a constant value of this type.
    /// </summary>
    static abstract EvaluatedType ExpectedType { get; }

    static abstract TSelf NoValue { get; }
}

interface Value<TSelf, TVal> : Value where TSelf : Value<TSelf, TVal>
{
    Option<TVal> Value { get; }

    // Binary
    public OperationResult<TResult> OperateWith<TResult>(TSelf other,
        Func<TVal, TVal, OperationResult<TResult>> transform)
        where TResult : Value<TResult>;

    // Binary error-less
    public TResult OperateWith<TResult>(TSelf other,
        Func<TVal, TVal, TResult> transform)
        where TResult : Value<TResult>;

    // Binary internal
    public OperationResult<TSelf> OperateWith(TSelf other,
        Func<TVal, TVal, OperationResult<TSelf>> transform);

    // Binary internal error-less
    public TSelf OperateWith(TSelf other,
       Func<TVal, TVal, TVal> transform);

    // Unary
    public OperationResult<TResult> Operate<TResult>(
        Func<TVal, OperationResult<TResult>> transform)
        where TResult : Value<TResult>;

    // Unary error-less
    public TResult Operate<TResult>(
        Func<TVal, TResult> transform)
        where TResult : Value<TResult>;

    // Unary internal
    public OperationResult<TSelf> Operate(
        Func<TVal, OperationResult<TSelf>> transform);

    // Unary internal error-less
    public TSelf Operate(
        Func<TVal, TVal> transform);
}

interface Value : EquatableSemantics<Value>
{
    bool IsConstant { get; }

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
        EvaluatedType.Array
        or EvaluatedType.File
        or EvaluatedType.Structure
        or EvaluatedType.Unknown => new Unknown(type),

        _ => throw type.ToUnmatchedException(),
    };

    public EvaluatedType Type { get; }

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
        Option<int> Value<Integer, int>.Value => _impl.Value;

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

    internal sealed class Unknown(EvaluatedType type) : Value
    {
        public EvaluatedType Type => type;
        public bool IsConstant => false;
        public bool SemanticsEqual(Value other) => other is Unknown o
         && o.Type.SemanticsEqual(Type);
    }
}
