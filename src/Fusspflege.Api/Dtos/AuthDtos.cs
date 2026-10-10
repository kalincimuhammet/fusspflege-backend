using System.ComponentModel.DataAnnotations;

namespace Fusspflege.Api.Dtos;

public class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string Email, IReadOnlyList<string> Roles);