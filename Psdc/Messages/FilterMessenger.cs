namespace Scover.Psdc.Messages;

public sealed class FilterMessenger(Func<MessageCode, bool> enableMsg) : Messenger
{
    readonly List<Message> _msgs = [];
    readonly Func<MessageCode, bool> _enableMsg = enableMsg;
    readonly DefaultDictionary<MessageSeverity, int> _msgCount = new(0);

    public IEnumerable<Message> Messages => _msgs;

    public int GetMessageCount(MessageSeverity severity) => _msgCount[severity];

    public void Report(Message message)
    {
        if (_enableMsg(message.Code)) {
            _msgs.Add(message);
            if (!_msgCount.TryAdd(message.Severity, 1)) {
                ++_msgCount[message.Severity];
            }
        }
    }
}
