using Scover.Psdc.Messages;

namespace Scover.Psdc.Benchmark;

sealed class IgnoreMessenger : Messenger
{
    IgnoreMessenger() { }
    public static IgnoreMessenger Instance { get; } = new();
    public string Input => Program.Code;

    public void Report(Message message)
    {
    }
}
