using PingPongNet8Server;
using PingPongNet8Server.Binary;
using PingPongNet8Server.Text;
using SimpleR.Protocol;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSimpleR();
builder.Services.AddSingleton<ConnectionManager>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");


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

public partial class Program { }