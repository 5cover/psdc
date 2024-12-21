namespace Scover.Psdc.Messages;

public sealed class MessageTextPrinter(
    TextWriter output,
    string sourceFile,
    string input,
    MessageTextPrinter.Style style)
 : MessagePrinter
{
    public enum Style
    {
        Gnu,
        VSCode,
    }

    readonly TextWriter _output = output;
    readonly string _sourceFile = sourceFile;
    readonly string _input = input;
    readonly Style _style = style;
    const string LineNoMargin = "    ";
    const string Bar = " | ";
    const int MaxMultilineErrorLines = 10;

    public void PrintMessageList(IEnumerable<Message> messages)
    {
        foreach (var msg in messages) {
            PrintMessage(msg);
        }
    }

    public void Conclude(Func<MessageSeverity, int> msgCount)
    {
        _output.Write("Compilation ");

        if (msgCount(MessageSeverity.Error) == 0) {
            new ConsoleColors(ConsoleColor.Green).DoInColor(() => _output.Write("succeeded"));
        } else {
            new ConsoleColors(ConsoleColor.Red).DoInColor(() => _output.Write("failed"));
        }

        _output.WriteLine(string.Create(Format.Msg, $" ({msgCount
        (MessageSeverity.Error).ToQuantity("error")}, {msgCount
        (MessageSeverity.Warning).ToQuantity("warning")}, {msgCount
        (MessageSeverity.Hint).ToQuantity("hint")})."));
    }

    void PrintMessage(Message message)
    {
        var (start, end) = message.InputRange.Apply(_input);
        var msgColor = ConsoleColors.ForMessageSeverity(message.Severity);
        var lineNoPadding = (end.Line + 1).DigitCount();

        switch (_style) {
        case Style.Gnu:
            PrintLocationGnu(message);
            break;
        case Style.VSCode:
            PrintLocationVsCode(message);
            break;
        default:
            throw _style.ToUnmatchedException();
        }

        // If the error spans over only 1 line, show it with carets underneath
        if (start.Line == end.Line) {
            ReadOnlySpan<char> badLine = _input.GetLine(end.Line);

            // Bad line
            StartLine(lineNoPadding, end.Line);
            {
                _output.Write(badLine[..start.Column]); // keep indentation
                msgColor.SetColor();
                _output.Write(badLine[start.Column..end.Column]);
                Console.ResetColor();
                EndLine(badLine[end.Column..]);
            }

            // Caret line
            StartLine(lineNoPadding);
            {
                var offset = Math.Max(badLine.GetLeadingWhitespaceCount(), start.Column);
                _output.WriteNTimes(offset, ' ');
                msgColor.DoInColor(() => _output.WriteNTimes(end.Column - offset, '^'));
                _output.WriteLine();
            }
        }
        // Otherwise, show all the bad code
        else {
            StartLine(lineNoPadding, start.Line);
            {
                ReadOnlySpan<char> badLine = _input.GetLine(start.Line);
                _output.Write(badLine[..start.Column]);
                msgColor.SetColor();
                EndLine(badLine[start.Column..]);
                Console.ResetColor();
            }

            int badLineCount = end.Line - start.Line - 1;
            var badLines = Enumerable.Range(start.Line + 1, badLineCount);
            const int MaxBadLines = MaxMultilineErrorLines - 2;
            foreach (var line in badLines.Take(MaxBadLines)) {
                ReadOnlySpan<char> badLine = _input.GetLine(line);
                StartLine(lineNoPadding, line);
                msgColor.SetColor();
                EndLine(badLine);
                Console.ResetColor();
            };

            badLines.FirstOrNone().Tap(l => {
                if (badLineCount > MaxBadLines) {
                    StartLine(lineNoPadding, l);
                    EndLine(string.Create(Format.Msg, $"({badLineCount - MaxBadLines} more lines...)"));
                }
            });

            StartLine(lineNoPadding, end.Line);
            {
                ReadOnlySpan<char> badLine = _input.GetLine(end.Line);
                msgColor.SetColor();
                _output.Write(badLine[..end.Column]);
                Console.ResetColor();
                EndLine(badLine[end.Column..]);
            }
        }

        // Advice lines
        foreach (var advice in message.AdvicePieces) {
            StartLine(lineNoPadding);
            _output.WriteLine(advice);
        };

        _output.WriteLine();
    }

    void PrintLocationGnu(Message msg)
    {
        var (start, end) = msg.InputRange.Apply(_input);

        if (start.Line == end.Line) {
            _output.Write($"{_sourceFile}:{start.Line + 1}.{start.Column + 1}-{end.Column + 1}: ");
        } else {
            _output.Write($"{_sourceFile}:{start.Line + 1}.{start.Column + 1}-{end.Line + 1}.{end.Column + 1}: ");
        }
        var msgColor = ConsoleColors.ForMessageSeverity(msg.Severity);
        msgColor.DoInColor(() => _output.Write(
            string.Create(Format.Msg, $"P{(int)msg.Code:d4}: {msg.Severity.ToString().ToLower(Format.Msg)}:")));

        _output.WriteLine($" {msg.Content.Get(_input)}");
    }

    void PrintLocationVsCode(Message msg)
    {
        var (start, end) = msg.InputRange.Apply(_input);

        var msgColor = ConsoleColors.ForMessageSeverity(msg.Severity);
        msgColor.DoInColor(() => _output.Write(string.Create(Format.Msg, $"[P{(int)msg.Code:d4}] ")));

        _output.WriteLine(string.Create(Format.Msg, $"{start}: {msg.Severity.ToString().ToLower(Format.Msg)}: {msg.Content.Get(_input)}"));
    }

    void EndLine(ReadOnlySpan<char> content) => _output.WriteLine(content.TrimEnd());

    void StartLine(int padding, int line)
    {
        _output.Write(LineNoMargin);
        _output.Write((line + 1).ToString(Format.Msg).PadLeft(padding));
        _output.Write(Bar);
    }

    void StartLine(int padding)
    {
        _output.Write(LineNoMargin);
        _output.WriteNTimes(padding, ' ');
        _output.Write(Bar);
    }
}
