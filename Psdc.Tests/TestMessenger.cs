using Scover.Psdc.Messages;

namespace Scover.Psdc.Tests;

sealed class TestMessenger : Messenger
{
    /// <inheritdoc />
    public void Report(Message message) => _messages.Add(message);
    /// <inheritdoc />
    public int GetMessageCount(MessageSeverity severity) => _messages.Count(m => m.Severity == severity);
    readonly List<Message> _messages = [];
    public IReadOnlyList<Message> Messages => _messages;
    /// <inheritdoc />
    IEnumerable<Message> Messenger.Messages => _messages;
    public void Clear() => _messages.Clear();
}
