namespace Scover.Psdc.Messages;

public sealed class PrintMessenger(
    TextWriter output,
    string sourceFile,
    string input,
    MessageStyle style,
    IReadOnlyDictionary<MessageCode, bool> enabledMessages) : Messenger
{
    public string Input => _input;
    readonly string _input = input;
    readonly string _srcFile = sourceFile;
    readonly MessageStyle _style = style;
    readonly DefaultDictionary<MessageSeverity, int> _msgCounts = new(0);
    readonly TextWriter _output = output;
    readonly IReadOnlyDictionary<MessageCode, bool> _enableMsg = enabledMessages;

    /// <summary>
    /// Get the amount of messages of a given severity.
    /// </summary>
    /// <param name="severity">The severity of the messages to count.</param>
    /// <returns>The amount of messages of the given severity that have been reported.</returns>
    public int GetMessageCount(MessageSeverity severity) => _msgCounts[severity];

    const string LineNoMargin = "    ";
    const string Bar = " | ";
    const int MaxMultilineErrorLines = 10;

    private readonly List<Message> _msgs = [];

    public void Report(Message message)
    {
        if (_enableMsg.GetValueOrNone(message.Code).ValueOr(true)) {
            _msgs.Add(message);
        }
    }

    public void PrintMessageList()
    {
        foreach (var msg in _msgs) {
            PrintMessage(msg);
        }
    }

    void PrintMessage(Message message)
    {
        if (!_msgCounts.TryAdd(message.Severity, 1)) {
            ++_msgCounts[message.Severity];
        }

        var (start, end) = GetPositions(message.InputRange);
        var msgColor = ConsoleColors.ForMessageSeverity(message.Severity);
        var lineNoPadding = (end.Line + 1).DigitCount();

        switch (_style) {
        case MessageStyle.Gnu:
            PrintLocationGnu(message);
            break;
        case MessageStyle.VsCode:
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

    (Position Start, Position End) GetPositions(Range range)
     => (_input.GetPositionAt(range.Start), _input.GetPositionAt(range.End));

    void PrintLocationGnu(Message msg)
    {
        var (start, end) = GetPositions(msg.InputRange);

        if (start.Line == end.Line) {
            _output.Write($"{_srcFile}:{start.Line + 1}.{start.Column + 1}-{end.Column + 1}: ");
        } else {
            _output.Write($"{_srcFile}:{start.Line + 1}.{start.Column + 1}-{end.Line + 1}.{end.Column + 1}: ");
        }
        var msgColor = ConsoleColors.ForMessageSeverity(msg.Severity);
        msgColor.DoInColor(() => _output.Write(
            string.Create(Format.Msg, $"P{(int)msg.Code:d4}: {msg.Severity.ToString().ToLower(Format.Msg)}:")));

        _output.WriteLine($" {msg.Content.Get(_input)}");
    }

    void PrintLocationVsCode(Message msg)
    {
        var (start, end) = GetPositions(msg.InputRange);

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
