namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1416 // Platform-specific calls are guarded by OperatingSystem.IsWindows() in each test method.
	[TestClass]
	public class SecretProtectorTests
	{
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
			if (!OperatingSystem.IsWindows())
			{
				throw new AssertInconclusiveException("DPAPI is only supported on Windows.");
				return;
			}

			Action act = () => SecretProtector.Encrypt(null);

			act.Should().Throw<ArgumentNullException>();
		}

		[TestMethod]
		public void GivenEmptySecret_WhenEncryptCalled_ThenThrowsArgumentException()
		{
			if (!OperatingSystem.IsWindows())
			{
				throw new AssertInconclusiveException("DPAPI is only supported on Windows.");
				return;
			}

			Action act = () => SecretProtector.Encrypt(string.Empty);

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void GivenWhiteSpaceSecret_WhenEncryptCalled_ThenThrowsArgumentException()
		{
			if (!OperatingSystem.IsWindows())
			{
				throw new AssertInconclusiveException("DPAPI is only supported on Windows.");
				return;
			}

			Action act = () => SecretProtector.Encrypt("   ");

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void GivenValidSecret_WhenEncryptCalled_ThenResultStartsWithEncryptedPrefix()
		{
			if (!OperatingSystem.IsWindows())
			{
				throw new AssertInconclusiveException("DPAPI is only supported on Windows.");
				return;
			}

			var result = SecretProtector.Encrypt("my-secret");

			result.Should().StartWith(SecretProtector.EncryptedPrefix);
		}

		[TestMethod]
		public void GivenValidSecret_WhenEncryptCalled_ThenResultIsMarkedAsProtected()
		{
			if (!OperatingSystem.IsWindows())
			{
				throw new AssertInconclusiveException("DPAPI is only supported on Windows.");
				return;
			}

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
			if (!OperatingSystem.IsWindows())
			{
				throw new AssertInconclusiveException("DPAPI is only supported on Windows.");
				return;
			}

			const string original = "super-secret-password";
			var encrypted = SecretProtector.Encrypt(original);

			var result = SecretProtector.Decrypt(encrypted);

			result.Should().Be(original);
		}
	}
#pragma warning restore CA1416
}
