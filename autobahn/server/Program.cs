using SimpleR.Authobahn.Server;
using SimpleR.Protocol;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSimpleR();

var app = builder.Build();

app.UseRouting();

app.UseAuthorization();

app.MapSimpleR<byte[]>("/", b =>
{
    b.UseEndOfMessageDelimitedProtocol(new EchoProtocol()).UseDispatcher<EchoDispatcher>();
});

app.Run();
