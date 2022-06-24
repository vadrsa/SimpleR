using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PingPong.Server.Binary;
using PingPong.Server.NoOp;
using PingPong.Server.Text;
using SimpleR.Protocol;

namespace PingPong.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
            { new("Logging:LogLevel:Default", "Trace") });
        builder.Services.AddSimpleR();
        builder.Services.AddSingleton<ConnectionManager>();

        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.MapSimpleR<EmptyMessage>("/noop",
            b =>
            {
                b.UseEndOfMessageDelimitedProtocol(new NoOpMessageProtocol())
                    .UseDispatcher<NoOpMessageDispatcher>();
            }
        );
        
        app.MapSimpleR<PingPongBinary>("/ping/binary/custom",
            b =>
            {
                b.UseCustomProtocol(new CustomBinaryPingProtocol())
                    .UseDispatcher<BinaryMessageDispatcher>();
            },
            options => options.WebSockets.TransferFormat = TransferFormat.Binary
        );

        app.MapSimpleR<PingPongBinary>("/ping/binary/eom",
            b =>
            {
                b.UseEndOfMessageDelimitedProtocol(new DelimitedBinaryPingProtocol())
                    .UseDispatcher<BinaryMessageDispatcher>();
            },
            options => options.WebSockets.TransferFormat = TransferFormat.Binary
        );

        app.MapSimpleR<PingPongText>("/ping/text/custom",
            b =>
            {
                b.UseCustomProtocol(new CustomTextPingProtocol())
                    .UseDispatcher<TextMessageDispatcher>();
            },
            options => options.WebSockets.TransferFormat = TransferFormat.Text
        );

        app.MapSimpleR<PingPongText>("/ping/text/eom",
            b =>
            {
                b.UseEndOfMessageDelimitedProtocol(new DelimitedTextPingProtocol())
                    .UseDispatcher<TextMessageDispatcher>();
            },
            options => options.WebSockets.TransferFormat = TransferFormat.Text
        );

        app.Run();
    }
}
