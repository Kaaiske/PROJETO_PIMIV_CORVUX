using System;
using CorvusIAM.Services;
using Xunit;

namespace CorvusIAM.Tests.Services
{
    public class TotpServiceTests
    {
        private readonly TotpService _totpService = new();

        [Fact]
        public void GenerateSecretKey_RetornaChaveBase32Valida()
        {
            var key = _totpService.GenerateSecretKey();

            Assert.NotNull(key);
            Assert.True(key.Length >= 26);
            Assert.Matches("^[A-Z2-7]+$", key);
        }

        [Fact]
        public void GenerateQrCodeUri_RetornaFormatoValidoOtpauth()
        {
            string email = "admin@corvuxiam.tech";
            string secret = "JBSWY3DPEHPK3PXP";

            var uri = _totpService.GenerateQrCodeUri(email, secret);

            Assert.Contains("otpauth://totp/CorvuxIAM:admin%40corvuxiam.tech", uri);
            Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri);
            Assert.Contains("issuer=CorvuxIAM", uri);
        }

        [Fact]
        public void ValidateCode_RetornaFalse_ParaCodigoInvalido()
        {
            string secret = "JBSWY3DPEHPK3PXP";
            string fakeCode = "000000";

            // Um código fixo 000000 não deve ser válido para uma chave arbitrária
            bool resultado = _totpService.ValidateCode(secret, fakeCode);

            // Código errado deve falhar
            Assert.False(resultado);
        }

        [Fact]
        public void ValidateCode_RetornaFalse_ParaChaveOuCodigoVazio()
        {
            Assert.False(_totpService.ValidateCode("", "123456"));
            Assert.False(_totpService.ValidateCode("JBSWY3DPEHPK3PXP", ""));
            Assert.False(_totpService.ValidateCode("JBSWY3DPEHPK3PXP", "abc"));
        }
    }
}

