using System.Collections.Immutable;

namespace Scover.Psdc.Messages;

public sealed class PrintMessenger(
    TextWriter output,
    string input,
    IReadOnlyDictionary<MessageCode, bool> enabledMessages) : Messenger
{
    public string Input => input;

    readonly DefaultDictionary<MessageSeverity, int> _msgCounts = new(0);
    readonly TextWriter _output = output;
    readonly IReadOnlyDictionary<MessageCode, bool> _enableMsg = enabledMessages;

    const string LineNoMargin = "    ";
    const string Bar = " | ";
    const int MaxMultilineErrorLines = 10;

    private readonly Queue<Message> _msgs = new();

    public void Report(Message message)
    {
        if (_enableMsg.GetValueOrNone(message.Code).ValueOr(true)) {
            _msgs.Enqueue(message);
        }
    }

    public void PrintMessageList()
    {
        while (_msgs.TryDequeue(out var msg)) {
            PrintMessage(msg);
        }
        PrintConclusion();
    }

    void PrintMessage(Message message)
    {
        if (!_msgCounts.TryAdd(message.Severity, 1)) {
            ++_msgCounts[message.Severity];
        }
        var msgColor = ConsoleColorInfo.ForMessageSeverity(message.Severity);
        msgColor.DoInColor(() => _output.Write(string.Create(Format.Msg, $"[P{(int)message.Code:d4}] ")));

        Position start = input.GetPositionAt(message.InputRange.Start);
        Position end = input.GetPositionAt(message.InputRange.End);

        var lineNoPadding = (end.Line + 1).DigitCount();

        _output.WriteLine(string.Create(Format.Msg, $"{start}: {message.Severity.ToString().ToLower(Format.Msg)}: {message.Content.Get(input)}"));

        // If the error spans over only 1 line, show it with carets underneath
        if (start.Line == end.Line) {
            ReadOnlySpan<char> badLine = input.GetLine(end.Line);

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
                ReadOnlySpan<char> badLine = input.GetLine(start.Line);
                _output.Write(badLine[..start.Column]);
                msgColor.SetColor();
                EndLine(badLine[start.Column..]);
                Console.ResetColor();
            }

            int badLineCount = end.Line - start.Line - 1;
            var badLines = Enumerable.Range(start.Line + 1, badLineCount);
            const int MaxBadLines = MaxMultilineErrorLines - 2;
            foreach (var line in badLines.Take(MaxBadLines)) {
                ReadOnlySpan<char> badLine = input.GetLine(line);
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
                ReadOnlySpan<char> badLine = input.GetLine(end.Line);
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

    void PrintConclusion()
     => _output.WriteLine(string.Create(Format.Msg,
        $"Compilation terminated ({_msgCounts[MessageSeverity.Error]} errors, {_msgCounts[MessageSeverity.Warning]} warnings, {_msgCounts[MessageSeverity.Suggestion]} suggestions)."));

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
