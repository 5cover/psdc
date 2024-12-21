namespace Scover.Psdc.Library;

public readonly record struct FixedRange(int Start, int Length)
{
    /// <summary>
    /// Exclusive end bound of the range.
    /// </summary>
    public int End => Start + Length;
    public static implicit operator Range(FixedRange o) => o.Start..o.End;
}
