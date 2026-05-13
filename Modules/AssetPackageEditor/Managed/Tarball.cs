// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.IO.Compression;

namespace UnityEditor.AssetPackage
{
    using ContentHandler = System.Action<byte[]>;

    internal class MalformedTarball : Exception;

    /// <summary>
    /// Contains methods for working with tarballs.
    /// </summary>
    internal static class Tarball
    {
        /// <summary>
        /// Creates a GZip-compressed tarball from a folder.
        /// </summary>
        /// <param name="sourceDirectory">The folder to archive.</param>
        /// <param name="outputTarGzPath">The output .tar.gz file path.</param>
        public static void CreateTarballFromFolder(string sourceDirectory, string outputTarGzPath)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");

            using var outStream = File.Create(outputTarGzPath);
            using var gzip = new GZipStream(outStream, CompressionLevel.Fastest);

            var files = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            foreach (var filePath in files)
            {
                var relativePath = Path.GetRelativePath(sourceDirectory, filePath).Replace('\\', '/');
                var fileBytes = File.ReadAllBytes(filePath);
                // Use last write time as file mod time (Unix timestamp)
                var fileModTime = new DateTimeOffset(File.GetLastWriteTimeUtc(filePath)).ToUnixTimeSeconds();
                TarballCore.WriteEntry(gzip, relativePath, fileBytes, fileModTime);
            }
            // Write two empty blocks at the end of the tarball (standard tar format)
            gzip.Write(new byte[TarballCore.RecordSize * 2]);
        }

        /// <summary>
        /// Inserts a file at the start of a tarball.
        /// </summary>
        /// <param name="tarball">The tarball.</param>
        /// <param name="path">The path to the tarball.</param>
        /// <param name="filename">The filename of the file to insert.</param>
        /// <param name="file">The file to insert.</param>
        public static void InsertFileAtStart(ReadOnlyMemory<byte> tarball, string path, string filename, ReadOnlySpan<byte> file, long fileModTime = 0)
        {
            using var output = new GZipStream(File.Create(path), CompressionLevel.Fastest);
            TarballCore.WriteEntry(output, filename, file, fileModTime);
            output.Write(tarball.Span);
        }

        /// <summary>
        /// Gets the content of a compressed tarball.
        /// </summary>
        /// <param name="tarballPath">The path to the tarball.</param>
        /// <returns>The content of the tarball.</returns>
        public static ReadOnlyMemory<byte> GetUncompressedTarball(string tarballPath)
        {
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(File.OpenRead(tarballPath), CompressionMode.Decompress))
            {
                gz.CopyTo(ms);
            }
            var output = ms.ToArray();

            if (output.Length < 2*TarballCore.RecordSize)
                throw new ArgumentException("input is not a tarball", tarballPath);

            return new ReadOnlyMemory<byte>(output);
        }
    }
}
