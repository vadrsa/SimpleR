using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SimpleThermostat.Server;

public class DeviceAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DeviceAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var deviceId = Request.RouteValues["deviceId"]?.ToString();

        if (deviceId == "1234")
        {
            return Task.FromResult(CreateAuthenticatedResult(deviceId));
        }
        else
        {
            return Task.FromResult(CreateNonAuthenticatedResult());
        }
    }
    
    private AuthenticateResult CreateAuthenticatedResult(string deviceId)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, deviceId), new Claim(ClaimTypes.Role, "Device") };
        var identity = new ClaimsIdentity(claims, "Basic");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
    }

    private AuthenticateResult CreateNonAuthenticatedResult()
    {
        Response.StatusCode = 401;
        return AuthenticateResult.Fail($"Invalid device id");
    }
}