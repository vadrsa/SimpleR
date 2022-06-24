namespace SimpleR.Protocol
{
    public abstract class EndOfMessageDelimitedProtocol<TMessage> : DelimitedProtocol<TMessage>, IEndOfMessageDelimitedProtocol
    {
        public byte[] EndOfMessageBytes => CachedDelimiter;
    }
}
