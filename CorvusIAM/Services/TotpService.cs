using System;
using System.Security.Cryptography;
using System.Text;
using CorvusIAM.Interfaces;

namespace CorvusIAM.Services
{
    public class TotpService : ITotpService
    {
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public string GenerateSecretKey()
        {
            var bytes = new byte[20];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return ToBase32String(bytes);
        }

        public string GenerateQrCodeUri(string email, string secretKey)
        {
            string cleanEmail = Uri.EscapeDataString(email ?? "user@corvusiam.tech");
            return $"otpauth://totp/CorvuxIAM:{cleanEmail}?secret={secretKey}&issuer=CorvuxIAM&digits=6&period=30";
        }

        public string GenerateQrCodeImageUrl(string email, string secretKey)
        {
            var uri = GenerateQrCodeUri(email, secretKey);
            return $"https://api.qrserver.com/v1/create-qr-code/?size=220x220&data={Uri.EscapeDataString(uri)}";
        }

        public bool ValidateCode(string? secretKey, string code)
        {
            if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
                return false;

            code = code.Trim().Replace(" ", "").Replace("-", "");
            if (code.Length != 6 || !int.TryParse(code, out _))
                return false;

            byte[] keyBytes;
            try
            {
                keyBytes = FromBase32String(secretKey);
            }
            catch
            {
                return false;
            }

            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

            // Janela de tolerancia de +/- 1 ciclo de 30s (anti-clock-drift)
            for (long offset = -1; offset <= 1; offset++)
            {
                var expectedCode = ComputeTotp(keyBytes, currentTimestamp + offset);
                if (expectedCode.Equals(code, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ComputeTotp(byte[] key, long step)
        {
            byte[] stepBytes = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(stepBytes);
            }

            using var hmac = new HMACSHA1(key);
            byte[] hash = hmac.ComputeHash(stepBytes);

            int offset = hash[^1] & 0x0F;
            int binaryCode = ((hash[offset] & 0x7F) << 24)
                           | ((hash[offset + 1] & 0xFF) << 16)
                           | ((hash[offset + 2] & 0xFF) << 8)
                           | (hash[offset + 3] & 0xFF);

            int otp = binaryCode % 1000000;
            return otp.ToString("D6");
        }

        private static string ToBase32String(byte[] data)
        {
            var result = new StringBuilder();
            int buffer = 0;
            int bitsLeft = 0;

            foreach (byte b in data)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;

                while (bitsLeft >= 5)
                {
                    bitsLeft -= 5;
                    int index = (buffer >> bitsLeft) & 0x1F;
                    result.Append(Base32Chars[index]);
                }
            }

            if (bitsLeft > 0)
            {
                int index = (buffer << (5 - bitsLeft)) & 0x1F;
                result.Append(Base32Chars[index]);
            }

            return result.ToString();
        }

        private static byte[] FromBase32String(string input)
        {
            input = input.Trim().ToUpperInvariant();
            var output = new System.Collections.Generic.List<byte>();
            int buffer = 0;
            int bitsLeft = 0;

            foreach (char c in input)
            {
                int val = Base32Chars.IndexOf(c);
                if (val < 0) continue;

                buffer = (buffer << 5) | val;
                bitsLeft += 5;

                while (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    output.Add((byte)((buffer >> bitsLeft) & 0xFF));
                }
            }

            return output.ToArray();
        }
    }
}
