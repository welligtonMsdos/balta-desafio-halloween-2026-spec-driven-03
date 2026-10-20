using System.Security.Cryptography;
using System.Text;

namespace LinkShortener.Application;

public static class LinkCodeGenerator
{
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private const int GeneratedLength = 7;

    public static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O código é obrigatório.", nameof(value));
        }

        var code = value.Trim();
        if (code.Length is < 4 or > 20)
        {
            throw new ArgumentException("O código deve ter entre 4 e 20 caracteres.", nameof(value));
        }

        if (!code.All(ch => char.IsAsciiLetterOrDigit(ch)))
        {
            throw new ArgumentException("O código deve conter apenas letras e números ASCII.", nameof(value));
        }

        return code.ToUpperInvariant();
    }

    public static string GenerateUniqueCode()
    {
        var builder = new StringBuilder(GeneratedLength);
        var bytes = RandomNumberGenerator.GetBytes(GeneratedLength);

        for (var i = 0; i < GeneratedLength; i++)
        {
            builder.Append(Alphabet[bytes[i] % Alphabet.Length]);
        }

        return builder.ToString();
    }
}
