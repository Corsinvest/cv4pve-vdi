/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Security.Cryptography;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

/// <summary>
/// PBKDF2 (HMAC-SHA256) password hashing for the kiosk admin password.
/// Output format: <c>iterations.saltBase64.hashBase64</c>.
/// </summary>
internal static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        if (string.IsNullOrEmpty(encoded)) { return false; }

        var parts = encoded.Split('.');
        if (parts.Length != 3) { return false; }
        if (!int.TryParse(parts[0], out var iterations)) { return false; }

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch { return false; }
    }
}
