namespace SimpleR.Ocpp;

public static class MessageDispatcherBuilderOcppExtensions
{
    public static MessageDispatcherBuilder<IOcppMessage> UseOcppProtocol(
        this MessageDispatcherBuilder<IOcppMessage> builder, OcppMessageProtocolOptions options)
    {
        builder.UseEndOfMessageDelimitedProtocol(new OcppMessageProtocol(options));
        return builder;
    }
    
    public static MessageDispatcherBuilder<IOcppMessage> UseOcppProtocol(
        this MessageDispatcherBuilder<IOcppMessage> builder)
    {
        builder.UseEndOfMessageDelimitedProtocol(new OcppMessageProtocol());
        return builder;
    }
}