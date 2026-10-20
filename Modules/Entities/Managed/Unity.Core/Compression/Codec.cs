// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace Unity.Core.Compression
{
    internal enum Codec : int
    {
        None = 0,
        LZ4
    }

    /// <summary>
    /// Provides codec agnostic helper functions for compression/decompression
    /// </summary>
    internal static class CodecService
    {
        /// <summary>
        /// Return the maximum size that a codec may output in a "worst case" scenario when compressing data
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        static public unsafe int CompressUpperBound(Codec codec, int size)
        {
            switch (codec)
            {
                case Codec.None: return size;
                case Codec.LZ4:  return CompressBoundLZ4(size);
                default: throw new ArgumentException($"Invalid codec '{codec}' specified");
            }
        }

        /// <summary>
        /// Compresses the passed in `src` data into newly allocated `dst` buffer. Users must free `dst` manually after calling `Compress`
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="src"></param>
        /// <param name="size"></param>
        /// <param name="dst"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        static public unsafe int Compress(Codec codec, in byte* src, int srcSize, out byte* dst, Allocator allocator = Allocator.Temp)
        {
            return Compress(codec, src, srcSize, out dst, (AllocatorManager.AllocatorHandle) allocator);
        }

        /// <summary>
        /// Compresses the passed in `src` data into newly allocated `dst` buffer. Users must free `dst` manually after calling `Compress`
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="src"></param>
        /// <param name="size"></param>
        /// <param name="dst"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        static public unsafe int Compress(Codec codec, in byte* src, int srcSize, out byte* dst, AllocatorManager.AllocatorHandle allocator)
        {
            int boundedSize = CompressUpperBound(codec, srcSize);
            dst = (byte*)Memory.Unmanaged.Allocate(boundedSize, 16, allocator);

            int compressedSize = 0;
            switch (codec)
            {
                case Codec.LZ4: compressedSize = CompressLZ4(src, dst, srcSize, boundedSize); break;

                case Codec.None: // Surely this is an error/unintentional
                default: throw new ArgumentException($"Invalid codec '{codec}' specified");
            }

            if (compressedSize < 0)
            {
                Memory.Unmanaged.Free(dst, allocator);
                dst = null;
            }

            return compressedSize;
        }

        /// <summary>
        /// Decompresses data in `src` buffer and returns true with the decompressed data stored in the passed in, previously allocated `decompressedData` buffer.
        /// Users thus should know ahead of time how large a `decompressedData` buffer to use before calling this function. Not
        /// passing a large enough buffer will result in this function failing and returning false.
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="compressedData"></param>
        /// <param name="compressedSize"></param>
        /// <param name="decompressedData"></param>
        /// <param name="decompressedSize"></param>
        /// <returns></returns>
        static public unsafe bool Decompress(Codec codec, in byte* compressedData, int compressedSize, byte* decompressedData, int decompressedSize)
        {
            switch (codec)
            {
                case Codec.LZ4: return DecompressLZ4(compressedData, decompressedData, compressedSize, decompressedSize) > 0;

                case Codec.None: // Surely this is an error/unintentional
                default: throw new ArgumentException($"Invalid codec '{codec}' specified");
            }
        }

        const string DllName = "liblz4";
        [DllImport(DllName, EntryPoint = "LZ4_compressBound")]
        static extern unsafe int CompressBoundLZ4(int srcSize);
        [DllImport(DllName, EntryPoint = "LZ4_compress_default")]
        static extern unsafe int CompressLZ4(byte* src, byte* dst, int srcSize, int dstCapacity);
        [DllImport(DllName, EntryPoint = "LZ4_decompress_safe")]
        static extern unsafe int DecompressLZ4(byte* src, byte* dst, int compressedSize, int dstCapacity);
    }
}
