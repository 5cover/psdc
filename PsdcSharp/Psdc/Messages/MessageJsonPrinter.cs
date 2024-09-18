
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Scover.Psdc.Messages;

public sealed class MessageJsonPrinter(TextWriter output, string input) : MessagePrinter
{
    readonly TextWriter _output = output;
    readonly string _input = input;

    public void Conclude(Func<MessageSeverity, int> msgCount)
    {
    }

    public void PrintMessageList(IEnumerable<Message> messages)
    {
        _output.Write('[');
        bool notFirst = false;
        foreach (var msg in messages) {
            if (notFirst) {
                _output.Write(',');
            }
            notFirst = true;

            PrintMessage(msg);
        }
        _output.Write(']');
    }

    private void PrintMessage(Message msg)
    {
        var (start, end) = msg.InputRange.Apply(_input);
        _output.Write(string.Create(CultureInfo.InvariantCulture, @$"{{""code"":{msg.Code:d},""content"":""{JsonStr(msg.Content.Get(_input))}"",""start"":{FormatPosition(start)},""end"":{FormatPosition(end)},""severity"":""{JsonStr(msg.Severity.ToString("g"))}""}}"));
    }

    private static FormattableString FormatPosition(Position pos) => @$"{{""line"":{pos.Line},""col"":{pos.Column}}}";

    private static string JsonStr(string input)
     => JsonEncodedText.Encode(input, System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping).Value;

}
