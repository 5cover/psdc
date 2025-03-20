namespace Scover.Psdc.Messages;

/// <summary>
/// An interface to report messages to the user.
/// </summary>
public interface Messenger
{
    /// <summary>
    /// Report a message.
    /// </summary>
    /// <param name="message">The message to report.</param>
    void Report(Message message);
    int GetMessageCount(MessageSeverity severity);
    IEnumerable<Message> Messages { get; }
}
