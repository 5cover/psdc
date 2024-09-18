namespace Scover.Psdc.Messages;

public interface MessagePrinter
{
    public void PrintMessageList(IEnumerable<Message> messages);
    public void Conclude(Func<MessageSeverity, int> msgCount);
}
