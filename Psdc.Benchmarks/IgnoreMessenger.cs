using Scover.Psdc.Messages;

namespace Scover.Psdc.Benchmarks;

sealed class IgnoreMessenger : Messenger
{
    IgnoreMessenger() { }
    public static IgnoreMessenger Instance { get; } = new();
    public IEnumerable<Message> Messages => [];

    public int GetMessageCount(MessageSeverity severity) => 0;

    public void Report(Message message) { }
}
