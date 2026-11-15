using System.Globalization;
using System.Security.Cryptography;

namespace GridKrafter.Demo.Api.Data;

/// <summary>Hashes passwords with PBKDF2 (SHA-256), as "pbkdf2-sha256$iterations$salt$hash".</summary>
public static class PasswordHasher
{
	private const string Scheme = "pbkdf2-sha256";
	private const int Iterations = 210_000;
	private const int SaltSize = 16;
	private const int HashSize = 32;

	/// <summary>The fewest characters a password may have.</summary>
	public const int MinimumLength = 10;

	/// <summary>The stored form of a password.</summary>
	public static string Hash(string password)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
		byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
		return string.Join('$', Scheme, Iterations.ToString(CultureInfo.InvariantCulture), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
	}

	/// <summary>Whether the password matches the stored form, compared in constant time.</summary>
	public static bool Verify(string password, string stored)
	{
		string[] parts = stored.Split('$');
		if (parts.Length != 4 || parts[0] != Scheme || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int iterations))
			return false;

		byte[] salt = Convert.FromBase64String(parts[2]);
		byte[] expected = Convert.FromBase64String(parts[3]);
		byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
		return CryptographicOperations.FixedTimeEquals(actual, expected);
	}

	/// <summary>Why a new password is not acceptable, or <see langword="null"/> when it is.</summary>
	public static string? Weakness(string? password)
	{
		if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
			return "A password needs at least " + MinimumLength + " characters.";

		int kinds = (password.Any(char.IsUpper) ? 1 : 0) + (password.Any(char.IsLower) ? 1 : 0) +
			(password.Any(char.IsDigit) ? 1 : 0) + (password.Any(c => !char.IsLetterOrDigit(c)) ? 1 : 0);
		return kinds < 3 ? "A password needs three of: capital letters, small letters, digits and symbols." : null;
	}
}
