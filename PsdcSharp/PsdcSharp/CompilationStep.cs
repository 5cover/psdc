using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc;

internal abstract class CompilationStep
{
    private readonly Queue<Message> _messages = new();
    protected void AddMessage(Message message) => _messages.Enqueue(message);

    public bool TryDequeueMessage([NotNullWhen(true)] out Message? message) => _messages.TryDequeue(out message);
}
