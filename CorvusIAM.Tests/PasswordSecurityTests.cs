using Xunit;

namespace CorvusIAM.Tests;

public class PasswordSecurityTests
{
    [Fact]
    public void BCrypt_hash_verifies_the_original_password()
    {
        const string password = "SenhaForte!2026";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.True(BCrypt.Net.BCrypt.Verify(password, hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("senha-incorreta", hash));
    }
}
