namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Security.Cryptography;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SecretProtectorTests
	{
		// Decodes to "01234567890123456789012345678901" (32 ASCII bytes).
		private const string TestEncryptionKeyBase64 = "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=";

		[TestInitialize]
		public void TestInitialize()
		{
			SecretProtector.EncryptionKeyBase64Override = TestEncryptionKeyBase64;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			SecretProtector.EncryptionKeyBase64Override = null;
		}

		// ─── IsProtected ───────────────────────────────────────────────────────────

		[TestMethod]
		public void GivenEncryptedValue_WhenIsProtectedCalled_ThenReturnsTrue()
		{
			var encrypted = SecretProtector.EncryptedPrefix + "AAAA";

			SecretProtector.IsProtected(encrypted).Should().BeTrue();
		}

		[TestMethod]
		public void GivenPlainTextValue_WhenIsProtectedCalled_ThenReturnsFalse()
		{
			SecretProtector.IsProtected("my-plain-secret").Should().BeFalse();
		}

		[TestMethod]
		public void GivenEmptyString_WhenIsProtectedCalled_ThenReturnsFalse()
		{
			SecretProtector.IsProtected(string.Empty).Should().BeFalse();
		}

		[TestMethod]
		public void GivenNullString_WhenIsProtectedCalled_ThenReturnsFalse()
		{
			SecretProtector.IsProtected(null).Should().BeFalse();
		}

		// ─── Encrypt ───────────────────────────────────────────────────────────────

		[TestMethod]
		public void GivenNullSecret_WhenEncryptCalled_ThenThrowsArgumentNullException()
		{
			Action act = () => SecretProtector.Encrypt(null);

			act.Should().Throw<ArgumentNullException>();
		}

		[TestMethod]
		public void GivenEmptySecret_WhenEncryptCalled_ThenThrowsArgumentException()
		{
			Action act = () => SecretProtector.Encrypt(string.Empty);

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void GivenWhiteSpaceSecret_WhenEncryptCalled_ThenThrowsArgumentException()
		{
			Action act = () => SecretProtector.Encrypt("   ");

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void GivenValidSecret_WhenEncryptCalled_ThenResultStartsWithEncryptedPrefix()
		{
			var result = SecretProtector.Encrypt("my-secret");

			result.Should().StartWith(SecretProtector.EncryptedPrefix);
		}

		[TestMethod]
		public void GivenValidSecret_WhenEncryptCalled_ThenResultIsMarkedAsProtected()
		{
			var result = SecretProtector.Encrypt("my-secret");

			SecretProtector.IsProtected(result).Should().BeTrue();
		}

		// ─── Decrypt ───────────────────────────────────────────────────────────────

		[TestMethod]
		public void GivenPlainTextValue_WhenDecryptCalled_ThenValueIsReturnedUnchanged()
		{
			// Regression: Issue #867 — backward compatibility: plaintext values must pass through.
			var plainText = "my-plain-secret";

			var result = SecretProtector.Decrypt(plainText);

			result.Should().Be(plainText);
		}

		[TestMethod]
		public void GivenEmptyString_WhenDecryptCalled_ThenEmptyStringIsReturned()
		{
			var result = SecretProtector.Decrypt(string.Empty);

			result.Should().BeEmpty();
		}

		[TestMethod]
		public void GivenEncryptedSecret_WhenDecryptCalled_ThenOriginalSecretIsReturned()
		{
			// Regression: Issue #867
			const string original = "super-secret-password";
			var encrypted = SecretProtector.Encrypt(original);

			var result = SecretProtector.Decrypt(encrypted);

			result.Should().Be(original);
		}

		[TestMethod]
		public void GivenMalformedEncryptedSecret_WhenDecryptCalled_ThenThrowsCryptographicException()
		{
			// Regression: Issue #916
			var malformedPayload = SecretProtector.EncryptedPrefix + Convert.ToBase64String(new byte[] { 1, 2, 3 });

			Action act = () => SecretProtector.Decrypt(malformedPayload);

			act.Should().Throw<CryptographicException>()
				.WithMessage("The encrypted secret is malformed.");
		}

		[TestMethod]
		public void GivenUnsupportedEncryptedSecretVersion_WhenDecryptCalled_ThenThrowsCryptographicException()
		{
			// Regression: Issue #916
			var payload = new byte[1 + 12 + 16 + 1];
			payload[0] = 2;
			payload[^1] = 1;
			var encrypted = SecretProtector.EncryptedPrefix + Convert.ToBase64String(payload);

			Action act = () => SecretProtector.Decrypt(encrypted);

			act.Should().Throw<CryptographicException>()
				.WithMessage("Unsupported encrypted secret version: 2.");
		}
	}
}
