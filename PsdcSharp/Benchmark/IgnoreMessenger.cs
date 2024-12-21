using Scover.Psdc.Messages;

namespace Scover.Psdc.Benchmark;

sealed class IgnoreMessenger : Messenger
{
    private IgnoreMessenger() { }
    public static IgnoreMessenger Instance { get; } = new();
    public IEnumerable<Message> Messages => [];

    public int GetMessageCount(MessageSeverity severity) => 0;

    public void Report(Message message)
    {
    }
}
