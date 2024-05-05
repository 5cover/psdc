namespace Scover.Psdc.Messages;

/// <summary>
/// An interface to report messages to the user.
/// </summary>
interface Messenger
{
    /// <summary>
    /// Report a message.
    /// </summary>
    /// <param name="message">The message to report.</param>
    public void Report(Message message);
}
