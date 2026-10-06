// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Security.Cryptography;
using System.Text;

namespace UnityEditorInternal
{
    // The container format must stay in sync with OtaConfigCipher.cpp, which is what writes the
    // persisted OTA config the Editor reads back here.
    static class ManagedCaptureOtaCipher
    {
        const byte k_FormatVersion = 1;
        const int k_BlockSize = 16;
        const int k_IVSize = 16;
        const int k_MacSize = 32;
        const string k_EncryptionPurpose = "ManagedCaptureOtaConfig encryption";
        const string k_MacPurpose = "ManagedCaptureOtaConfig authentication";

        static readonly byte[] k_Magic = { (byte)'U', (byte)'O', (byte)'T', (byte)'A' };

        // This ships inside the Editor binary, so it obfuscates the persisted configuration rather
        // than keeping it secret. Must match kMasterKey in OtaConfigCipher.cpp.
        static readonly byte[] k_MasterKey =
        {
            0x7A, 0x1F, 0xC4, 0x0B, 0x93, 0x6E, 0x28, 0xD5,
            0x41, 0xB7, 0x0A, 0xEC, 0x5D, 0x39, 0xF2, 0x86,
            0xC1, 0x74, 0xAB, 0x30, 0x9F, 0x62, 0xE8, 0x15,
            0x4D, 0xD0, 0x87, 0x2B, 0x56, 0xF9, 0x3C, 0xA4,
        };

        static int headerSize
        {
            get { return k_Magic.Length + 1; }
        }

        internal static string Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = DeriveKey(k_EncryptionPurpose);
                aes.GenerateIV();

                byte[] cipherText;
                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }

                var authenticatedSize = headerSize + k_IVSize + cipherText.Length;
                var payload = new byte[authenticatedSize + k_MacSize];
                Buffer.BlockCopy(k_Magic, 0, payload, 0, k_Magic.Length);
                payload[k_Magic.Length] = k_FormatVersion;
                Buffer.BlockCopy(aes.IV, 0, payload, headerSize, k_IVSize);
                Buffer.BlockCopy(cipherText, 0, payload, headerSize + k_IVSize, cipherText.Length);

                using (var hmac = new HMACSHA256(DeriveKey(k_MacPurpose)))
                    Buffer.BlockCopy(hmac.ComputeHash(payload, 0, authenticatedSize), 0, payload, authenticatedSize, k_MacSize);

                return Convert.ToBase64String(payload);
            }
        }

        internal static bool TryDecrypt(string encrypted, out string plainText)
        {
            plainText = null;

            byte[] payload;
            try
            {
                payload = Convert.FromBase64String(encrypted);
            }
            catch (FormatException)
            {
                return false;
            }

            if (payload.Length < headerSize + k_IVSize + k_BlockSize + k_MacSize)
                return false;

            for (int i = 0; i < k_Magic.Length; ++i)
            {
                if (payload[i] != k_Magic[i])
                    return false;
            }

            if (payload[k_Magic.Length] != k_FormatVersion)
                return false;

            var authenticatedSize = payload.Length - k_MacSize;
            var cipherTextSize = authenticatedSize - headerSize - k_IVSize;
            if ((cipherTextSize % k_BlockSize) != 0)
                return false;

            using (var hmac = new HMACSHA256(DeriveKey(k_MacPurpose)))
            {
                if (!EqualsInFixedTime(hmac.ComputeHash(payload, 0, authenticatedSize), payload, authenticatedSize))
                    return false;
            }

            var iv = new byte[k_IVSize];
            Buffer.BlockCopy(payload, headerSize, iv, 0, k_IVSize);

            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = DeriveKey(k_EncryptionPurpose);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    try
                    {
                        var plainBytes = decryptor.TransformFinalBlock(payload, headerSize + k_IVSize, cipherTextSize);
                        plainText = Encoding.UTF8.GetString(plainBytes);
                        return true;
                    }
                    catch (CryptographicException)
                    {
                        return false;
                    }
                }
            }
        }

        static byte[] DeriveKey(string purpose)
        {
            using (var hmac = new HMACSHA256(k_MasterKey))
                return hmac.ComputeHash(Encoding.ASCII.GetBytes(purpose));
        }

        static bool EqualsInFixedTime(byte[] expected, byte[] payload, int payloadOffset)
        {
            var difference = 0;
            for (int i = 0; i < expected.Length; ++i)
                difference |= expected[i] ^ payload[payloadOffset + i];
            return difference == 0;
        }
    }
}
