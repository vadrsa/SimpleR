using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SimpleOcpp.Server;

public class OcppAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public OcppAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.Upgrade != "websocket")
        {
            return AuthenticateResult.NoResult();
        }
        
        var chargePointId = Request.RouteValues["chargePointId"]?.ToString();
        try
        {
            // Basic authentication header
            var authHeader = Request.Headers["Authorization"].ToString();

            // Check if basic authentication is used
            if (!authHeader.StartsWith("basic", StringComparison.OrdinalIgnoreCase))
                return CreateNonAuthenticatedResult("");
            
            var token = authHeader[6..].Trim();
            var credentialsString = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var credentials = credentialsString.Split(':');

            var chargePointIdentification = credentials[0];
            var password = credentials[1];

            return await AuthenticateChargerAsync(chargePointIdentification, password);

        }
        catch
        {
            return CreateNonAuthenticatedResult(chargePointId ?? "");
        }
    }
    
    private async Task<AuthenticateResult> AuthenticateChargerAsync(string chargerId, string password)
    {
        // hardcode for example
        var success = chargerId == "charger1" && password == "pass1";
        if (success)
        {
            return CreateAuthenticatedResult(chargerId);
        }

        return CreateNonAuthenticatedResult(chargerId);
    }

    private AuthenticateResult CreateAuthenticatedResult(string chargerId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, chargerId), new Claim(ClaimTypes.Role, "Charger") };
        var identity = new ClaimsIdentity(claims, "Basic");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
    }

    private AuthenticateResult CreateNonAuthenticatedResult(string chargerId)
    {
        Response.StatusCode = 401;
        return AuthenticateResult.Fail($"Invalid Authorization Header for {chargerId}");
    }
}