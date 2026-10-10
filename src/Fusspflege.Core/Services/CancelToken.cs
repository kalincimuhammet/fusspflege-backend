using System.Security.Cryptography;
using System.Text;

namespace Fusspflege.Core.Services;

public static class CancelToken
{
    // 32 zufällige Bytes, URL-sicher kodiert (für den Link in der Mail)
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    // SHA-256 als Hex-String (64 Zeichen), das landet in der Datenbank
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}