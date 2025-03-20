namespace Scover.Psdc.Library;

/// <inheritdoc />
/// <summary>
/// A range with a known length.
/// </summary>
/// <param name="Start">The start of the range.</param>
/// <param name="Length">The end of the range.</param>

public readonly record struct LengthRange(int Start, int Length)
{
    /// <summary>
    /// Exclusive end bound of the range.
    /// </summary>
    public int End => Start + Length;
    public static implicit operator Range(LengthRange o) => o.Start..o.End;
}
