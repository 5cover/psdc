namespace Scover.Psdc.Library;

/// <summary>
/// Extends <see cref="IFormattable"/> with convenience method to avoid explicit <see langword="null"/>-passing.
/// </summary>
interface IFormattableUsable : IFormattable
{
    string ToString();
    string ToString(IFormatProvider? fmtProvider);
    string ToString(string? format);
}

/// <summary>
/// Implements <see cref="IFormattableUsable"/>
/// </summary>
abstract class FormattableUsableImpl : IFormattableUsable
{
    public string ToString(IFormatProvider? fmtProvider) => ToString(null, fmtProvider);
    public string ToString(string? format) => ToString(format, null);
    public sealed override string ToString() => ToString(null, null);
    public abstract string ToString(string? format, IFormatProvider? fmtProvider);
}
