namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Security.Cryptography;
	using System.Text;

	/// <summary>
	/// Encrypts and decrypts credential secrets using AES-256-GCM.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Encrypted values are prefixed with <c>ENC:</c> so that plaintext values stored before
	/// encryption was introduced continue to work without modification.
	/// </para>
	/// <para>
	/// The production encryption key must be supplied by the private client build configuration
	/// and must not be committed to the public repository.
	/// </para>
	/// </remarks>
	public static class SecretProtector
	{
		private const int KeySize = 32;
		private const int NonceSize = 12;
		private const int TagSize = 16;

		/*
		 * IMPORTANT:
		 *
		 * This is a SAMPLE key only.
		 *
		 * The production key must not be committed to the public Weevil
		 * repository. It must be supplied automatically by the private
		 * client build configuration.
		 *
		 * The decoded value must contain exactly 32 bytes.
		 */
		private const string EncryptionKeyBase64 =
			"REPLACE_WITH_PRIVATE_32_BYTE_BASE64_KEY";

		/// <summary>
		/// The prefix that identifies an encrypted value produced by <see cref="Encrypt"/>.
		/// </summary>
		public const string EncryptedPrefix = "ENC:";

		internal static string EncryptionKeyBase64Override { get; set; }

		/// <summary>
		/// Returns <see langword="true"/> when <paramref name="value"/> was produced by <see cref="Encrypt"/>.
		/// </summary>
		public static bool IsProtected(string value) =>
			!string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

		/// <summary>
		/// Encrypts <paramref name="plainText"/> using AES-256-GCM and returns a portable,
		/// prefixed Base64 string that can be stored in an environment variable.
		/// </summary>
		/// <param name="plainText">The secret to encrypt. Must not be null or empty.</param>
		/// <returns>An encrypted string prefixed with <see cref="EncryptedPrefix"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="plainText"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="plainText"/> is empty or whitespace.</exception>
		public static string Encrypt(string plainText)
		{
			ArgumentNullException.ThrowIfNull(plainText);

			if (string.IsNullOrWhiteSpace(plainText))
			{
				throw new ArgumentException("Secret must not be empty or whitespace.", nameof(plainText));
			}

			var key = GetEncryptionKey();
			var nonce = RandomNumberGenerator.GetBytes(NonceSize);
			var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
			var cipherText = new byte[plainTextBytes.Length];
			var authenticationTag = new byte[TagSize];

			using (var aes = new AesGcm(key, TagSize))
			{
				aes.Encrypt(
					nonce,
					plainTextBytes,
					cipherText,
					authenticationTag);
			}

			var payload = new byte[1 + NonceSize + TagSize + cipherText.Length];
			var offset = 0;

			payload[offset++] = 1;

			Buffer.BlockCopy(
				nonce,
				0,
				payload,
				offset,
				nonce.Length);

			offset += nonce.Length;

			Buffer.BlockCopy(
				authenticationTag,
				0,
				payload,
				offset,
				authenticationTag.Length);

			offset += authenticationTag.Length;

			Buffer.BlockCopy(
				cipherText,
				0,
				payload,
				offset,
				cipherText.Length);

			return EncryptedPrefix + Convert.ToBase64String(payload);
		}

		/// <summary>
		/// Returns the plaintext secret from <paramref name="value"/>.
		/// </summary>
		/// <param name="value">
		/// An encrypted value produced by <see cref="Encrypt"/> (prefixed with <see cref="EncryptedPrefix"/>),
		/// or a plaintext value. Plaintext values are returned unchanged to support backward compatibility.
		/// </param>
		/// <returns>The decrypted plaintext, or <paramref name="value"/> if it was not encrypted.</returns>
		public static string Decrypt(string value)
		{
			if (!IsProtected(value))
			{
				return value;
			}

			var encodedPayload = value[EncryptedPrefix.Length..];
			var payload = Convert.FromBase64String(encodedPayload);
			var minimumPayloadSize = 1 + NonceSize + TagSize + 1;

			if (payload.Length < minimumPayloadSize)
			{
				throw new CryptographicException("The encrypted secret is malformed.");
			}

			var offset = 0;
			var version = payload[offset++];

			if (version != 1)
			{
				throw new CryptographicException($"Unsupported encrypted secret version: {version}.");
			}

			ReadOnlySpan<byte> nonce = payload.AsSpan(offset, NonceSize);
			offset += NonceSize;

			ReadOnlySpan<byte> authenticationTag = payload.AsSpan(offset, TagSize);
			offset += TagSize;

			ReadOnlySpan<byte> cipherText = payload.AsSpan(offset);
			var plainTextBytes = new byte[cipherText.Length];
			var key = GetEncryptionKey();

			using (var aes = new AesGcm(key, TagSize))
			{
				aes.Decrypt(
					nonce,
					cipherText,
					authenticationTag,
					plainTextBytes);
			}

			return Encoding.UTF8.GetString(plainTextBytes);
		}

		private static byte[] GetEncryptionKey()
		{
			var encodedKey = EncryptionKeyBase64Override ?? EncryptionKeyBase64;
			byte[] key;

			try
			{
				key = Convert.FromBase64String(encodedKey);
			}
			catch (FormatException exception)
			{
				throw new InvalidOperationException(
					"The telemetry encryption key is not valid Base64.",
					exception);
			}

			if (key.Length != KeySize)
			{
				throw new InvalidOperationException(
					$"The telemetry encryption key must contain exactly {KeySize} bytes.");
			}

			return key;
		}
	}
}
