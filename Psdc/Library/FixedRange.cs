using System.Diagnostics;

namespace Scover.Psdc.Library;

/// <inheritdoc />
/// <summary>
/// A range with a known length.
/// </summary>
public readonly record struct FixedRange
{
    public FixedRange(int start, int end)
    {
        Debug.Assert(start <= end);
        (Start, End) = (start, end);
    }

    public static FixedRange Of(int start, int length) => new(start, start + length);

    public (Position Start, Position End) Apply(string str) => (str.GetPositionAt(Start), str.GetPositionAt(End));

    public static explicit operator Range(FixedRange fixedRange) => fixedRange.Start..fixedRange.End;

    public int Start { get; }
    public int End { get; }
    public int Length => End - Start;
}
