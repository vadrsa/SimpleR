using Microsoft.AspNetCore.Authentication;
using SimpleOcpp.Server;
using SimpleR.Ocpp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSimpleR();
builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, OcppAuthenticationHandler>("OCPP", null);

builder.Services.AddAuthorization(c =>
{
    c.AddPolicy("ChargerPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("OCPP");
        policy.RequireAuthenticatedUser();
    });
});

var app = builder.Build();

app.MapSimpleRToOcpp("/ocpp/{chargePointId}/", b =>
{
    b.UseOcppProtocol()
        .UseDispatcher<SimpleOcppDispatcher>();
})
.RequireAuthorization("ChargerPolicy");

app.Run();
