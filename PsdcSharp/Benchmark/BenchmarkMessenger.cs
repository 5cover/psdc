using System.Collections.Immutable;
using Scover.Psdc.Messages;

sealed class BenchmarkMessenger(string input) : Messenger
{
    readonly PrintMessenger _backingMsger = new(TextWriter.Null, input,
        ImmutableDictionary<MessageCode, bool>.Empty);

    public string Input => _backingMsger.Input;

    public void Report(Message message) => _backingMsger.Report(message);
}
