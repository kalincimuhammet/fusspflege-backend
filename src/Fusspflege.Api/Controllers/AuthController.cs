using Fusspflege.Api.Auth;
using Fusspflege.Api.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fusspflege.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _users;
    private readonly JwtTokenService _tokens;

    public AuthController(UserManager<IdentityUser> users, JwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        const string wrongCredentials = "E-Mail oder Passwort ist falsch.";

        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null)
            return Problem(statusCode: 401, title: wrongCredentials);

        if (await _users.IsLockedOutAsync(user))
            return Problem(statusCode: 401, title: "Zu viele Fehlversuche. Bitte versuchen Sie es in einigen Minuten erneut.");

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            // Zählt Fehlversuche, nach 5 Versuchen wird das Konto 15 Minuten gesperrt
            await _users.AccessFailedAsync(user);
            return Problem(statusCode: 401, title: wrongCredentials);
        }

        await _users.ResetAccessFailedCountAsync(user);

        var roles = (await _users.GetRolesAsync(user)).ToList();
        var (token, expiresAtUtc) = _tokens.Create(user, roles);

        return Ok(new LoginResponse(token, expiresAtUtc, user.Email ?? request.Email, roles));
    }
}