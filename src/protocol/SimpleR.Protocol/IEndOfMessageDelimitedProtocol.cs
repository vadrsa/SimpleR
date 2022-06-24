namespace SimpleR.Protocol
{
    public interface IEndOfMessageDelimitedProtocol
    {
        /// <summary>
        /// Sets the bytes that will be set for signaling end of message
        /// </summary>
        byte[] EndOfMessageBytes { get; }
    }
}