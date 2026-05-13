// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;

namespace UnityEditor.AssetPackage
{
    /// <summary>
    /// Low-level tar definitions, with a goal of being agnostic about error
    /// handling and avoiding memory allocations.
    /// </summary>
    /// <remarks>
    /// Implements the "v7" tar file format, which is an older subset of the "ustar" tar file format as defined in
    /// <a href="https://pubs.opengroup.org/onlinepubs/9799919799/utilities/pax.html#tag_20_94_13_06">POSIX.1-2024: the "pax" utility</a>
    /// </remarks>
    internal static class TarballCore
    {
        /// <summary>Select tar v7/ustar entry header fields, as byte indices or ranges inside the header record.</summary>
        internal static class Fields
        {
            public static readonly Range Name = ..100;
            public static readonly Range Size = 124..136;
            public static readonly Range MTime = 136..148;
            public static readonly Range Checksum = 148..156;
            public const int TypeFlag = 156;
            public static readonly Range Magic = 257..263;
            public static readonly Range Prefix = 345..500;
        }

        /// <summary>
        /// In tar v7/ustar, 11 octal digits is the limit for the individual entry
        /// file size (8.5 GB) and modification time (year 2242).
        /// </summary>
        internal const long ElevenOctalDigitLimit = (1L << 11 * 3) - 1;

        /// <summary>A tar file consists of a sequence of 512 byte records.</summary>
        internal const int RecordSize = 512;

        /// <summary>
        /// Encodes a non-negative octal number to a span of bytes.
        /// </summary>
        /// <param name="dest">The destination span.</param>
        /// <param name="value">The value to write.</param>
        private static void EncodeNonNegativeOctal(Span<byte> dest, long value)
        {
            var octalString = Convert.ToString(value, 8);
            var octalBytes = Encoding.ASCII.GetBytes(octalString);
            if (octalBytes.Length > dest.Length)
                throw new ArgumentOutOfRangeException("Destination span too small in EncodeNonNegativeOctal.");
            dest[..(dest.Length - octalBytes.Length)].Fill((byte)'0');
            octalBytes.CopyTo(dest[(dest.Length - octalBytes.Length)..]);
        }

        /// <summary>
        /// Encodes a v7 tar entry header for a regular file to a span of (at least) 512 bytes.
        /// </summary>
        /// <param name="dest">The destination span.</param>
        /// <param name="filename">The filename (written as UTF-8, but only ASCII is POSIX compliant).</param>
        /// <param name="fileSize">The file size.</param>
        /// <param name="fileModTime">The file modification time (seconds since the Unix epoch).</param>
        private static void EncodeEntryHeader(Span<byte> dest, string filename, long fileSize, long fileModTime)
        {
            var record = dest[..RecordSize];
            record.Clear();

            // filename
            var nameSpan = record[Fields.Name];
            int bytesWritten = Encoding.UTF8.GetBytes(filename, nameSpan);
            if (bytesWritten > nameSpan.Length)
                throw new ArgumentException("Filename too long", nameof(filename));

            // mode, uid, gid, size, mtime, chksum (initially, all spaces), file type
            "0000644\00000000\00000000\000000000000\000000000000\0        0"u8.CopyTo(record[100..]);
            EncodeNonNegativeOctal(record[Fields.Size][..^1], fileSize);
            EncodeNonNegativeOctal(record[Fields.MTime][..^1], fileModTime);

            // magic, version
            "ustar\0 \0"u8.CopyTo(record[Fields.Magic.Start..]);

            // Calculate and write checksum.
            var checksum = 0;
            foreach (var b in record) checksum += b;
            "000000\0 "u8.CopyTo(record[Fields.Checksum]);
            EncodeNonNegativeOctal(record[Fields.Checksum][..^2], checksum);
        }

        /// <summary>
        /// Gets the padding size for a tar file. A tar file is padded to a multiple of 512 bytes.
        /// </summary>
        /// <param name="fileSize">The size of the file.</param>
        /// <returns>The padding size.</returns>
        private static int GetTarFilePaddingSize(int fileSize)
            => 511 - (fileSize + 511) % 512;

        /// <summary>Writes a tar entry (header, contents and padding) to a stream.</summary>
        public static void WriteEntry(Stream stream, string filename, ReadOnlySpan<byte> fileData, long fileModTime)
        {
            Span<byte> header = stackalloc byte[RecordSize];
            EncodeEntryHeader(header, filename, fileData.Length, fileModTime);
            stream.Write(header);
            stream.Write(fileData);
            var padding = header[..GetTarFilePaddingSize(fileData.Length)];
            padding.Clear();
            stream.Write(padding);
        }
    }
}
