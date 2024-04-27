namespace Scover.Psdc;

/// <summary>
/// An interface to report messages to the user.
/// </summary>
internal interface Messenger
{
    /// <summary>
    /// Report a message.
    /// </summary>
    /// <param name="message">The message to report.</param>
    public void Report(Message message);
}
