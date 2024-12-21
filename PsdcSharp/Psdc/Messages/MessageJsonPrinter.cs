
using System.Globalization;
using System.Text.Json;

namespace Scover.Psdc.Messages;

public sealed class MessageJsonPrinter(TextWriter output, string input) : MessagePrinter
{
    readonly TextWriter _output = output;
    readonly string _input = input;

    public void Conclude(Func<MessageSeverity, int> msgCount) { }

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

    void PrintMessage(Message msg)
    {
        var (start, end) = msg.Location.Apply(_input);
        _output.Write(string.Create(CultureInfo.InvariantCulture, @$"{{""code"":{msg.Code:d},""content"":""{JsonStr(msg.Content.Get(_input))}"",""start"":{FormatPosition(start)},""end"":{FormatPosition(end)},""severity"":{(int)msg.Severity},""advice"":["));
        bool notFirst = false;
        foreach (var adv in msg.AdvicePieces) {
            if (notFirst) {
                _output.Write(',');
            }
            notFirst = true;

            _output.Write(@$"""{JsonStr(adv)}""");
        }
        _output.Write("]}");
    }

    static FormattableString FormatPosition(Position pos) => @$"{{""line"":{pos.Line},""col"":{pos.Column}}}";

    static string JsonStr(string input)
     => JsonEncodedText.Encode(input, System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping).Value;

}
