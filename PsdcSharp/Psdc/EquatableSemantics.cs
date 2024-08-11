namespace Scover.Psdc;

public interface EquatableSemantics<in T>
{
    /// <summary>
    /// Are this object and another semantically equal?
    /// </summary>
    /// <param name="other">The object to compare this one to.</param>
    /// <returns>This object and <paramref name="other"/> are semantically equal.</returns>
    public bool SemanticsEqual(T other);
}
