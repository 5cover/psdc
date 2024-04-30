using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal enum OperationError
{
    UnsupportedOperator,
    DivisionByZero
}

internal interface ConstantValue
{
    public EvaluatedType Type { get; }
    public Option<ConstantValue, OperationError> Operate(UnaryOperator op);
    public Option<ConstantValue, OperationError> Operate(BinaryOperator op, ConstantValue o);

    internal readonly struct Integer(int val) : ConstantValue
    {
        public EvaluatedType Type => EvaluatedType.Numeric.GetInstance(NumericType.Integer);
        public int Value => val;

        public Option<ConstantValue, OperationError> Operate(UnaryOperator op) => (op switch {
            UnaryOperator.Minus => new Integer(-val),
            UnaryOperator.Plus => this,
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator);

        public Option<ConstantValue, OperationError> Operate(BinaryOperator op, ConstantValue other) => other is Integer o
        ? op switch {
            BinaryOperator.Divide => o.Value == 0
                ? OperationError.DivisionByZero.None<ConstantValue, OperationError>()
                : new Integer(val / o.Value).Some<ConstantValue, OperationError>(),
            _ => (op switch {
                BinaryOperator.Equal => new Boolean(val == o.Value),
                BinaryOperator.GreaterThan => new Boolean(val > o.Value),
                BinaryOperator.GreaterThanOrEqual => new Boolean(val >= o.Value),
                BinaryOperator.LessThan => new Boolean(val < o.Value),
                BinaryOperator.LessThanOrEqual => new Boolean(val <= o.Value),
                BinaryOperator.Minus => new Integer(val - o.Value),
                BinaryOperator.Modulus => new Integer(val % o.Value),
                BinaryOperator.Multiply => new Integer(val * o.Value),
                BinaryOperator.NotEqual => new Boolean(val != o.Value),
                BinaryOperator.Plus => new Integer(val + o.Value),
                _ => default(ConstantValue),
            }).SomeNotNull(OperationError.UnsupportedOperator),
        } : OperationError.UnsupportedOperator.None<ConstantValue, OperationError>();
    }

    internal readonly struct Boolean(bool val) : ConstantValue
    {
        public EvaluatedType Type => EvaluatedType.Boolean.Instance;
        public bool Value => val;

        public Option<ConstantValue, OperationError> Operate(UnaryOperator op) => (op switch {
            UnaryOperator.Not => new Boolean(!val),
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator);

        public Option<ConstantValue, OperationError> Operate(BinaryOperator op, ConstantValue other) => other is Boolean o
        ? (op switch {
            BinaryOperator.And => new Boolean(val && o.Value),
            BinaryOperator.Equal => new Boolean(val == o.Value),
            BinaryOperator.NotEqual => new Boolean(val != o.Value),
            BinaryOperator.Or => new Boolean(val || o.Value),
            BinaryOperator.Xor => new Boolean(val ^ o.Value),
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator)
        : OperationError.UnsupportedOperator.None<ConstantValue, OperationError>();
    }

    internal readonly struct Character(char val) : ConstantValue
    {
        public EvaluatedType Type => EvaluatedType.Character.Instance;
        public char Value => val;

        public Option<ConstantValue, OperationError> Operate(UnaryOperator op) => (op switch {
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator);

        public Option<ConstantValue, OperationError> Operate(BinaryOperator op, ConstantValue other) => other is Character o
        ? (op switch {
            BinaryOperator.Equal => new Boolean(val == o.Value),
            BinaryOperator.NotEqual => new Boolean(val != o.Value),
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator)
        : OperationError.UnsupportedOperator.None<ConstantValue, OperationError>();
    }

    internal readonly struct Real(decimal val) : ConstantValue
    {
        public EvaluatedType Type => EvaluatedType.Numeric.GetInstance(NumericType.Real);
        public decimal Value => val;

        public Option<ConstantValue, OperationError> Operate(UnaryOperator op) => (op switch {
            UnaryOperator.Minus => new Real(-val),
            UnaryOperator.Plus => this,
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator);

        public Option<ConstantValue, OperationError> Operate(BinaryOperator op, ConstantValue other) => other is Real o
        ? (op switch {
            BinaryOperator.Equal => new Boolean(val == o.Value),
            BinaryOperator.GreaterThan => new Boolean(val > o.Value),
            BinaryOperator.GreaterThanOrEqual => new Boolean(val >= o.Value),
            BinaryOperator.LessThan => new Boolean(val < o.Value),
            BinaryOperator.LessThanOrEqual => new Boolean(val <= o.Value),
            BinaryOperator.Minus => new Real(val - o.Value),
            BinaryOperator.Modulus => new Real(val % o.Value),
            BinaryOperator.Multiply => new Real(val * o.Value),
            BinaryOperator.NotEqual => new Boolean(val != o.Value),
            BinaryOperator.Plus => new Real(val + o.Value),
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator)
        : OperationError.UnsupportedOperator.None<ConstantValue, OperationError>();
    }

    internal readonly struct String(string val) : ConstantValue
    {
        public EvaluatedType Type { get; } = EvaluatedType.StringLengthed.Create(val.Length);
        public string Value => val;

        public Option<ConstantValue, OperationError> Operate(UnaryOperator op) => (op switch {
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator);

        public Option<ConstantValue, OperationError> Operate(BinaryOperator op, ConstantValue other) => other is String o
        ? (op switch {
            BinaryOperator.Equal => new Boolean(val == o.Value),
            BinaryOperator.NotEqual => new Boolean(val == o.Value),
            _ => default(ConstantValue),
        }).SomeNotNull(OperationError.UnsupportedOperator)
        : OperationError.UnsupportedOperator.None<ConstantValue, OperationError>();
    }
}
