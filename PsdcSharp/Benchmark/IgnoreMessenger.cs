using Scover.Psdc.Messages;

namespace Scover.Psdc.Benchmark;

sealed class IgnoreMessenger(string input) : Messenger
{
    public string Input { get; } = input;

    public void Report(Message message)
    {
    }
}
