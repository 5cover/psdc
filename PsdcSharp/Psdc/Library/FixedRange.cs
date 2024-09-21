namespace Scover.Psdc.Library;

public readonly record struct FixedRange(int Start, int Length)
{
    public int End => Start + Length;
    public static implicit operator Range(FixedRange o) => o.Start..o.End;
}
