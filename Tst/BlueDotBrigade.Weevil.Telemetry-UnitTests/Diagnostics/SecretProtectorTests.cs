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

		// ─── Protect ───────────────────────────────────────────────────────────────

		[TestMethod]
		public void GivenNullSecret_WhenProtectCalled_ThenThrowsArgumentNullException()
		{
			if (!OperatingSystem.IsWindows())
			{
				Assert.Inconclusive("DPAPI is only supported on Windows.");
				return;
			}

			Action act = () => SecretProtector.Protect(null);

			act.Should().Throw<ArgumentNullException>();
		}

		[TestMethod]
		public void GivenEmptySecret_WhenProtectCalled_ThenThrowsArgumentException()
		{
			if (!OperatingSystem.IsWindows())
			{
				Assert.Inconclusive("DPAPI is only supported on Windows.");
				return;
			}

			Action act = () => SecretProtector.Protect(string.Empty);

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void GivenWhiteSpaceSecret_WhenProtectCalled_ThenThrowsArgumentException()
		{
			if (!OperatingSystem.IsWindows())
			{
				Assert.Inconclusive("DPAPI is only supported on Windows.");
				return;
			}

			Action act = () => SecretProtector.Protect("   ");

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void GivenValidSecret_WhenProtectCalled_ThenResultStartsWithEncryptedPrefix()
		{
			if (!OperatingSystem.IsWindows())
			{
				Assert.Inconclusive("DPAPI is only supported on Windows.");
				return;
			}

			var result = SecretProtector.Protect("my-secret");

			result.Should().StartWith(SecretProtector.EncryptedPrefix);
		}

		[TestMethod]
		public void GivenValidSecret_WhenProtectCalled_ThenResultIsMarkedAsProtected()
		{
			if (!OperatingSystem.IsWindows())
			{
				Assert.Inconclusive("DPAPI is only supported on Windows.");
				return;
			}

			var result = SecretProtector.Protect("my-secret");

			SecretProtector.IsProtected(result).Should().BeTrue();
		}

		// ─── Unprotect ─────────────────────────────────────────────────────────────

		[TestMethod]
		public void GivenPlainTextValue_WhenUnprotectCalled_ThenValueIsReturnedUnchanged()
		{
			// Regression: Issue #867 — backward compatibility: plaintext values must pass through.
			var plainText = "my-plain-secret";

			var result = SecretProtector.Unprotect(plainText);

			result.Should().Be(plainText);
		}

		[TestMethod]
		public void GivenEmptyString_WhenUnprotectCalled_ThenEmptyStringIsReturned()
		{
			var result = SecretProtector.Unprotect(string.Empty);

			result.Should().BeEmpty();
		}

		[TestMethod]
		public void GivenEncryptedSecret_WhenUnprotectCalled_ThenOriginalSecretIsReturned()
		{
			// Regression: Issue #867
			if (!OperatingSystem.IsWindows())
			{
				Assert.Inconclusive("DPAPI is only supported on Windows.");
				return;
			}

			const string original = "super-secret-password";
			var encrypted = SecretProtector.Protect(original);

			var result = SecretProtector.Unprotect(encrypted);

			result.Should().Be(original);
		}
	}
#pragma warning restore CA1416
}
