namespace Scover.Psdc;

public static class Globals
{
    static string? input = null;

    public static void Initialize(string input)
    {
        if (Globals.input is not null) {
            throw new InvalidOperationException($"Globals already initialized");
        }
        Globals.input = input;
    }

    /// <summary>
    /// The original, unmodified input code.
    /// </summary>
    public static string Input => input.NotNull($"{nameof(Input)} not initialized");
}
