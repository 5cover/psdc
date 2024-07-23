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
    public void Report(Message message);
    public void ReportAll(IEnumerable<Message> messages)
    {
        foreach (var m in messages) {
            Report(m);
        }
    }

    /// <summary>
    /// Get the input code.
    /// </summary>
    public string Input { get; }
}
