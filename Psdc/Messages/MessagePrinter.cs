namespace Scover.Psdc.Messages;

public interface MessagePrinter
{
    void PrintMessageList(IEnumerable<Message> messages);
    void Conclude(Func<MessageSeverity, int> msgCount);
}
