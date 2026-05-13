// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.AssetPackage
{
    internal class SignedAssetPackage
    {
        public const int Sha256IntegrityStringLength = 51;
        public const int Sha256DigestLength = 32;
        const string Sha256Prefix = "sha256-";
        public static string AttestationFilename => "package/.attestation.p7m";

        [RequiredByNativeCode]
        public static void CreateSignedAssetPackage(string sourcePath, string destinationPath, string ownerOrgId)
        {
            Tarball.CreateTarballFromFolder(sourcePath, destinationPath);

            if (!string.IsNullOrEmpty(ownerOrgId))
            {
                var signatureService = new SignatureService();
                Task.Run(async () =>
                {
                    try
                    {
                        await InsertAttestationFileIntoTarball(destinationPath, destinationPath, ownerOrgId, signatureService);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(L10n.Tr($"Package signature failed: {ex.Message}"));
                    }
                }).Wait();
            }
        }

        internal static async Task InsertAttestationFileIntoTarball(string sourcePath, string destinationPath, string ownerOrgId, SignatureService signatureService)
        {
            var tarball = Tarball.GetUncompressedTarball(sourcePath);
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(tarball.Span.ToArray());
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

                Tarball.InsertFileAtStart(tarball, destinationPath, AttestationFilename, attestationFile);
            }
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
