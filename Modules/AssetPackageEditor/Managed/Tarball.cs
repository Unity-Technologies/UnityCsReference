// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace UnityEditor.AssetPackage
{
    internal class MalformedTarballException(string message) : Exception(message);

    /// <summary>
    /// Contains methods for working with tarballs.
    /// </summary>
    internal static class Tarball
    {
        const int k_CopyBufferSize = 100 * 1024;

        /// <summary>
        /// Creates a GZip-compressed tarball from a folder.
        /// </summary>
        /// <param name="sourceDirectory">The folder to archive.</param>
        /// <param name="outputTarGzPath">The output .tar.gz file path.</param>
        public static void CreateTarballFromFolder(string sourceDirectory, string outputTarGzPath)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");

            // Write through a temporary file so a failure never leaves a partial tarball at the output path
            var tempPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputTarGzPath)), Path.GetRandomFileName());
            try
            {
                using (var outStream = File.Create(tempPath))
                using (var gzip = new GZipStream(outStream, CompressionLevel.Fastest))
                {
                    var files = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories);
                    foreach (var filePath in files)
                    {
                        var relativePath = Path.GetRelativePath(sourceDirectory, filePath).Replace('\\', '/');
                        // Use last write time as file mod time (Unix timestamp, clamped as pre-1970 times are not encodable)
                        var fileModTime = Math.Max(0, new DateTimeOffset(File.GetLastWriteTimeUtc(filePath)).ToUnixTimeSeconds());
                        using var file = File.OpenRead(filePath);
                        TarballCore.WriteEntry(gzip, relativePath, file, file.Length, fileModTime);
                    }
                    // Write two empty blocks at the end of the tarball (standard tar format)
                    gzip.Write(new byte[TarballCore.RecordSize * 2]);
                }
                if (File.Exists(outputTarGzPath))
                    File.Replace(tempPath, outputTarGzPath, null);
                else
                    File.Move(tempPath, outputTarGzPath);
            }
            catch
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (IOException)
                {
                    // Never mask the original exception with a cleanup failure
                }
                throw;
            }
        }

        /// <summary>
        /// Inserts a file at the start of a compressed tarball.
        /// </summary>
        /// <param name="tarballPath">The path to the source tarball.</param>
        /// <param name="destinationPath">The output tarball path (may be the same as the source path).</param>
        /// <param name="filename">The filename of the file to insert.</param>
        /// <param name="file">The file to insert.</param>
        /// <param name="fileModTime">The inserted file modification time (seconds since the Unix epoch).</param>
        /// <param name="expectedSourceSha256">When non-empty, the SHA-256 digest the uncompressed source content must
        /// match; the destination is left untouched on mismatch.</param>
        public static void InsertFileAtStart(string tarballPath, string destinationPath, string filename, ReadOnlySpan<byte> file, long fileModTime = 0, ReadOnlySpan<byte> expectedSourceSha256 = default)
        {
            // Stream through a temporary file so the source is never buffered in memory
            // and the destination can safely be the source itself.
            var tempPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(destinationPath)), Path.GetRandomFileName());
            try
            {
                using (var sha256 = SHA256.Create())
                using (var tarball = OpenUncompressedTarball(tarballPath))
                using (var output = new GZipStream(File.Create(tempPath), CompressionLevel.Fastest))
                {
                    TarballCore.WriteEntry(output, filename, file, fileModTime);

                    long copied = 0;
                    var buffer = new byte[k_CopyBufferSize];
                    int read;
                    while ((read = tarball.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, read);
                        sha256.TransformBlock(buffer, 0, read, null, 0);
                        copied += read;
                    }
                    if (copied < 2 * TarballCore.RecordSize)
                        throw new MalformedTarballException($"Input is not a tarball: {tarballPath}");

                    sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    if (!expectedSourceSha256.IsEmpty && !expectedSourceSha256.SequenceEqual(sha256.Hash))
                        throw new IOException($"'{tarballPath}' changed after its content digest was computed");
                }
                // Atomically replace the destination so a failure cannot destroy an existing file (e.g. an in-place source)
                if (File.Exists(destinationPath))
                    File.Replace(tempPath, destinationPath, null);
                else
                    File.Move(tempPath, destinationPath);
            }
            catch
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (IOException)
                {
                    // Never mask the original exception with a cleanup failure
                }
                throw;
            }
        }

        /// <summary>
        /// Opens a read-only stream over the uncompressed content of a compressed tarball.
        /// </summary>
        /// <param name="tarballPath">The path to the tarball.</param>
        /// <returns>A stream over the uncompressed content of the tarball.</returns>
        public static Stream OpenUncompressedTarball(string tarballPath)
            => new GZipStream(File.OpenRead(tarballPath), CompressionMode.Decompress);
    }
}
