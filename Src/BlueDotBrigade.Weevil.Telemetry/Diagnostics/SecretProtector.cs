namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Runtime.Versioning;
	using System.Security.Cryptography;
	using System.Text;

	/// <summary>
	/// Encrypts and decrypts credential secrets using the Windows Data Protection API (DPAPI).
	/// </summary>
	/// <remarks>
	/// <para>
	/// Encrypted values are prefixed with <c>ENC:</c> so that plaintext values stored before
	/// encryption was introduced continue to work without modification.
	/// </para>
	/// <para>
	/// Encryption is scoped to the current Windows user account: only the same user who
	/// encrypted the value can decrypt it.
	/// </para>
	/// <para>
	/// This class is Windows-only. On other platforms, <see cref="Protect"/> throws
	/// <see cref="PlatformNotSupportedException"/> and <see cref="Unprotect"/> returns
	/// the value unchanged.
	/// </para>
	/// </remarks>
	public static class SecretProtector
	{
		/// <summary>
		/// The prefix that identifies an encrypted value produced by <see cref="Protect"/>.
		/// </summary>
		public const string EncryptedPrefix = "ENC:";

		/// <summary>
		/// Returns <see langword="true"/> when <paramref name="value"/> was produced by <see cref="Protect"/>.
		/// </summary>
		public static bool IsProtected(string value) =>
			!string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

		/// <summary>
		/// Encrypts <paramref name="plainText"/> using DPAPI and returns a portable,
		/// prefixed Base64 string that can be stored in an environment variable.
		/// </summary>
		/// <param name="plainText">The secret to encrypt. Must not be null or empty.</param>
		/// <returns>An encrypted string prefixed with <see cref="EncryptedPrefix"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="plainText"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="plainText"/> is empty or whitespace.</exception>
		/// <exception cref="PlatformNotSupportedException">Thrown on non-Windows platforms.</exception>
		[SupportedOSPlatform("windows")]
		public static string Protect(string plainText)
		{
			ArgumentNullException.ThrowIfNull(plainText);

			if (string.IsNullOrWhiteSpace(plainText))
			{
				throw new ArgumentException("Secret must not be empty or whitespace.", nameof(plainText));
			}

			var plainBytes = Encoding.UTF8.GetBytes(plainText);
			var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
			return EncryptedPrefix + Convert.ToBase64String(encryptedBytes);
		}

		/// <summary>
		/// Returns the plaintext secret from <paramref name="value"/>.
		/// </summary>
		/// <param name="value">
		/// An encrypted value produced by <see cref="Protect"/> (prefixed with <see cref="EncryptedPrefix"/>),
		/// or a plaintext value. Plaintext values are returned unchanged to support backward compatibility.
		/// </param>
		/// <returns>The decrypted plaintext, or <paramref name="value"/> if it was not encrypted.</returns>
		public static string Unprotect(string value)
		{
			if (!IsProtected(value))
			{
				return value;
			}

			if (!OperatingSystem.IsWindows())
			{
				return value;
			}

			var base64 = value[EncryptedPrefix.Length..];
			var encryptedBytes = Convert.FromBase64String(base64);
			var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
			return Encoding.UTF8.GetString(plainBytes);
		}
	}
}
