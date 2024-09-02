using System.Collections.Immutable;
using Scover.Psdc;
using Scover.Psdc.Messages;

sealed class BenchmarkMessenger(string sourceFile, string input, MessageStyle style) : Messenger
{
    readonly PrintMessenger _backingMsger = new(TextWriter.Null, sourceFile, input, style,
        ImmutableDictionary<MessageCode, bool>.Empty);

    public string Input => _backingMsger.Input;

    public void Report(Message message) => _backingMsger.Report(message);
}
