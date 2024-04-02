using Microsoft.AspNetCore.Authentication;
using SimpleR.Protocol;
using SimpleThermostat.Protocol;
using SimpleThermostat.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSimpleR();
builder.Services.AddSingleton<ThermostatConnectionManager>();
builder.Services.AddSingleton<ThermostatRepository>();

builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, DeviceAuthenticationHandler>("BasicAuthentication", null);

var app = builder.Build();

app.MapPost("/thermostat/{deviceId}/settings", (string deviceId, ThermostatSettings settings, ThermostatRepository repository) =>
{
    repository.GetSettings(deviceId).TargetTemperature =  settings.TargetTemperature;
    return Results.Ok();
});

app.MapGet("/thermostat/{deviceId}/state", (string deviceId, ThermostatRepository repository) =>
{
    return Results.Json(repository.GetState(deviceId));
});

app.MapPost("/thermostat/{deviceId}/turnOff", async (string deviceId, ThermostatConnectionManager connectionManager) =>
{
    if(connectionManager.TryGetConnection(deviceId, out var connection))
    {
        await connection.WriteAsync(new SetThermostatModeCommand(ThermostatMode.Off));
        return Results.Ok();
    }

    return Results.NotFound("Device not connected");
});

app.MapSimpleR<ThermostatMetric, ThermostatCommand>("thermostat/{deviceId}/socket", b =>
{
    b.UseDispatcher<ThermostatMessageDispatcher>()
        .UseEndOfMessageDelimitedProtocol(
            MessageProtocol.From(new ThermostatMessageReader(), new ThermostatMessageWriter()));
})
.RequireAuthorization();

app.Run();
