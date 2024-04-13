using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc;

internal abstract class MessageProvider
{
    private readonly Queue<Message> _messages = new();
    public void AddMessage(Message message) => _messages.Enqueue(message);
    public bool TryDequeueMessage([NotNullWhen(true)] out Message? message) => _messages.TryDequeue(out message);
}
