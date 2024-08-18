using Scover.Psdc.Messages;

namespace Scover.Psdc.Library;

readonly record struct ConsoleColorInfo(ValueOption<ConsoleColor> Foreground, ValueOption<ConsoleColor> Background)
{
    public void DoInColor(Action action)
    {
        SetColor();
        action();
        Console.ResetColor();
    }

    public void SetColor()
    {
        Foreground.Map(fg => Console.ForegroundColor = fg);
        Background.Map(bg => Console.BackgroundColor = bg);
    }

    public static ConsoleColorInfo ForMessageSeverity(MessageSeverity msgSeverity) => msgSeverity switch {
        MessageSeverity.Error => new(ConsoleColor.Red, default),
        MessageSeverity.Warning => new(ConsoleColor.Yellow, default),
        MessageSeverity.Suggestion => new(ConsoleColor.Blue, default),
        MessageSeverity.Debug => new(ConsoleColor.Green, default),
        _ => throw msgSeverity.ToUnmatchedException(),
    };
}
