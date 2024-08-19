using Scover.Psdc.Messages;

sealed class BenchmarkMessenger(string input) : Messenger
{
    private readonly PrintMessenger _backingMsger = new(TextWriter.Null, input);

    public string Input => _backingMsger.Input;

    public void Report(Message message) => _backingMsger.Report(message);
}
