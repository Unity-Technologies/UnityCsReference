// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.AssetPackage
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SignatureResult
    {
        public bool succeeded;
        public string errorMessage;
    }

    internal class SignedAssetPackage
    {
        public const int Sha256IntegrityStringLength = 51;
        public const int Sha256DigestLength = 32;
        const string Sha256Prefix = "sha256-";
        public static string AttestationFilename => "package/.attestation.p7m";

        [RequiredByNativeCode]
        public static SignatureResult CreateSignedAssetPackage(string sourcePath, string destinationPath,
            string ownerOrgId)
        {
            var result = new SignatureResult { succeeded = false, errorMessage = null };

            try
            {
                Tarball.CreateTarballFromFolder(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                result.errorMessage = ex.Message;
                Debug.LogError(L10n.Tr($"Package tarball creation failed: {ex.Message}", null));
                return result;
            }

            if (string.IsNullOrEmpty(ownerOrgId))
                return result;

            var signatureService = new SignatureService();
            Task.Run(async () =>
            {
                try
                {
                    await InsertAttestationFileIntoTarball(destinationPath, destinationPath, ownerOrgId,
                        signatureService);
                    result.succeeded = true;
                }
                catch (Exception ex)
                {
                    result.errorMessage = ex.Message;
                    Debug.LogError(L10n.Tr($"Package signature failed: {ex.Message}", null));
                }
            }).Wait();
            return result;
        }

        internal static async Task InsertAttestationFileIntoTarball(string sourcePath, string destinationPath, string ownerOrgId, SignatureService signatureService)
        {
            byte[] hash;
            using (var sha256 = SHA256.Create())
            using (var tarball = Tarball.OpenUncompressedTarball(sourcePath))
            {
                hash = sha256.ComputeHash(tarball);
            }
            var integrity = GetIntegrityStringFromSha256Digest(hash);

            var attestation = await signatureService.RequestAttestationFromPackageRegistry(integrity, ownerOrgId);

            byte[] attestationFile;
            try
            {
                attestationFile = Convert.FromBase64String(attestation);
            }
            catch (FormatException)
            {
                throw new FormatException("Attestation file is not a valid base64 string");
            }

            Tarball.InsertFileAtStart(sourcePath, destinationPath, AttestationFilename, attestationFile, expectedSourceSha256: hash);
        }

        internal static string GetIntegrityStringFromSha256Digest(ReadOnlySpan<byte> digest)
        {
            if (digest.Length != Sha256DigestLength) throw new ArgumentException();
            Span<char> integrity = stackalloc char[Sha256IntegrityStringLength];
            Sha256Prefix.AsSpan().CopyTo(integrity);
            if (!Convert.TryToBase64Chars(digest, integrity[Sha256Prefix.Length..], out var charsWritten) || Sha256Prefix.Length + charsWritten != integrity.Length)
                throw new ArgumentOutOfRangeException("Unreachable code reached in GetIntegrityStringFromSha256Digest.");
            return new(integrity);
        }

    }

}
