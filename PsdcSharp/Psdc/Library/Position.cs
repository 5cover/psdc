namespace Scover.Psdc.Library;

public readonly record struct Position(int Line, int Column) : IFormattableUsable
{
    public string ToString(string? format, IFormatProvider? fmtProvider)
     => string.Create(fmtProvider, $"L {Line + 1}, col {Column + 1}");
    public override string ToString() => ToString(null, null);
    public string ToString(IFormatProvider? fmtProvider) => ToString(null, fmtProvider);
    public string ToString(string? format) => ToString(format, null);
}
