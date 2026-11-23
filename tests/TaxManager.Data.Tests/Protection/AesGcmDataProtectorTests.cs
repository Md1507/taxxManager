using System.Security.Cryptography;
using System.Text;
using TaxManager.Data.Protection;
using Xunit;

namespace TaxManager.Data.Tests.Protection;

public class AesGcmDataProtectorTests
{
    [Fact]
    public void RoundTripUsesFreshNonceAndAuthenticatesContext()
    {
        using var protector = new AesGcmDataProtector("synthetic", RandomNumberGenerator.GetBytes(32));
        var context = new ProtectionContext(Guid.NewGuid(), Guid.NewGuid(), "Dependent", "row-1", "DisplayName");
        var input = Encoding.UTF8.GetBytes("Tên giả 🐱");
        var first = protector.Protect(input, context);
        var second = protector.Protect(input, context);
        Assert.NotEqual(first, second);
        Assert.Equal(input, protector.Unprotect(first, context));
        Assert.Throws<DataProtectionException>(() => protector.Unprotect(first, context with { Field = "other" }));
        Assert.Throws<DataProtectionException>(() => protector.Unprotect(first, context with { ProfileId = Guid.NewGuid() }));
        Assert.Throws<DataProtectionException>(() => protector.Unprotect(first, context with { DatabaseId = Guid.NewGuid() }));
        Assert.Throws<DataProtectionException>(() => protector.Unprotect(first, context with { RowKey = "row-2" }));
    }

    [Fact]
    public void WrongKeyAndTamperedEnvelopeFailClosed()
    {
        using var protector = new AesGcmDataProtector("synthetic", RandomNumberGenerator.GetBytes(32));
        using var wrong = new AesGcmDataProtector("synthetic", RandomNumberGenerator.GetBytes(32));
        var context = new ProtectionContext(Guid.NewGuid(), Guid.NewGuid(), "Income", "2026-01", "Gross");
        var envelope = protector.Protect([], context);
        Assert.Empty(protector.Unprotect(envelope, context));
        Assert.Throws<DataProtectionException>(() => wrong.Unprotect(envelope, context));
        for (var i = 0; i < envelope.Length; i++)
        {
            var tampered = (byte[])envelope.Clone();
            tampered[i] ^= 1;
            Assert.Throws<DataProtectionException>(() => protector.Unprotect(tampered, context));
        }
        Assert.Throws<DataProtectionException>(() => protector.Unprotect([], context));
        Assert.Throws<DataProtectionException>(() => new AesGcmDataProtector("synthetic", new byte[31]));
    }
}
