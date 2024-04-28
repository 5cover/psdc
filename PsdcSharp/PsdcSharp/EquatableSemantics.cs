namespace Scover.Psdc;

internal interface EquatableSemantics<in T>
{
    /// <summary>
    /// Determines whether this object and another carry equal semantics.
    /// </summary>
    /// <param name="other">The object to compare this one to</param>
    /// <returns>Whether this type and <paramref name="other"/> are considered semantically equal.</returns>

    public bool SemanticsEqual(T other);
}
