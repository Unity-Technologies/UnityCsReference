// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Mathematics;

namespace Unity.Collections
{
    /// <summary>
    /// A type that uses Huffman encoding to encode values in a lossless manner.
    /// </summary>
    /// <remarks>
    /// This type puts values into a manageable number of power-of-two-sized buckets.
    /// It codes the bucket index with Huffman, and uses several raw bits that correspond
    /// to the size of the bucket to code the position in the bucket.
    ///
    /// For example, if you want to send a 32-bit integer over the network, it's
    /// impractical to create a Huffman tree that encompasses every value the integer
    /// can take because it requires a tree with 2^32 leaves. This type manages that situation.
    ///
    /// The buckets are small, around 0, and become progressively larger as the data moves away from zero.
    /// Because most data is deltas against predictions, most values are small and most of the redundancy
    /// is in the error's size and not in the values of that size we end up hitting.
    ///
    /// The context is as a sub-model that has its own statistics and uses its own Huffman tree.
    /// When using the context to read and write a specific value, the context must always be the same.
    /// The benefit of using multiple contexts is that it allows you to separate the statistics of things that have
    /// different expected distributions, which leads to more precise statistics, which again yields better compression.
    /// More contexts does, however, result in a marginal cost of a slightly larger model.
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct StreamCompressionModel
    {
        internal static readonly byte[] k_BucketSizes =
        {
            0, 0, 1, 2, 3, 4, 6, 8, 10, 12, 15, 18, 21, 24, 27, 32
        };

        internal static readonly uint[] k_BucketOffsets =
        {
            0, 1, 2, 4, 8, 16, 32, 96, 352, 1376, 5472, 38240, 300384, 2397536, 19174752, 153392480
        };
        internal static readonly int[] k_FirstBucketCandidate =
        {
            // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15  16  17  18 19 20 21 22 23 24 25 26 27 28 29 30 31 32
            15, 15, 15, 15, 14, 14, 14, 13, 13, 13, 12, 12, 12, 11, 11, 11, 10, 10, 10, 9, 9, 8, 8, 7, 7, 6, 5, 4, 3, 2, 1, 1, 0
        };
        internal static readonly byte[] k_DefaultModelData = { 16, // 16 symbols
                                                               2, 3, 3, 3,   4, 4, 4, 5,     5, 5, 6, 6,     6, 6, 6, 6,
                                                               0, 0 }; // no contexts
        internal const int k_AlphabetSize = 16;
        internal const int k_MaxHuffmanSymbolLength = 6;
        internal const int k_MaxContexts = 1;
        byte m_Initialized;

        static class SharedStaticCompressionModel
        {
            internal static readonly SharedStatic<StreamCompressionModel> Default = SharedStatic<StreamCompressionModel>.GetOrCreate<StreamCompressionModel>();
        }

        /// <summary>
        /// A shared singleton instance of <see cref="StreamCompressionModel"/>, this instance is initialized using
        /// hardcoded bucket parameters and model.
        /// </summary>
        public static StreamCompressionModel Default {
            get
            {
                if (SharedStaticCompressionModel.Default.Data.m_Initialized == 1)
                {
                    return SharedStaticCompressionModel.Default.Data;
                }
                Initialize();
                SharedStaticCompressionModel.Default.Data.m_Initialized = 1;

                return SharedStaticCompressionModel.Default.Data;
            }
        }

        static void Initialize()
        {
            for (int i = 0; i < k_AlphabetSize; ++i)
            {
                SharedStaticCompressionModel.Default.Data.bucketSizes[i] = k_BucketSizes[i];
                SharedStaticCompressionModel.Default.Data.bucketOffsets[i] = k_BucketOffsets[i];
            }
            var modelData = new NativeArray<byte>(k_DefaultModelData.Length, Allocator.Temp);
            for (var index = 0; index < k_DefaultModelData.Length; index++)
            {
                modelData[index] = k_DefaultModelData[index];
            }

            //int numContexts = NetworkConfig.maxContexts;
            int numContexts = 1;
            var symbolLengths = new NativeArray<byte>(numContexts * k_AlphabetSize, Allocator.Temp);

            int readOffset = 0;
            {
                // default model
                int defaultModelAlphabetSize = modelData[readOffset++];
                CheckAlphabetSize(defaultModelAlphabetSize);

                for (int i = 0; i < k_AlphabetSize; i++)
                {
                    byte length = modelData[readOffset++];
                    for (int context = 0; context < numContexts; context++)
                    {
                        symbolLengths[numContexts * context + i] = length;
                    }
                }

                // other models
                int numModels = modelData[readOffset] | (modelData[readOffset + 1] << 8);
                readOffset += 2;
                for (int model = 0; model < numModels; model++)
                {
                    int context = modelData[readOffset] | (modelData[readOffset + 1] << 8);
                    readOffset += 2;

                    int modelAlphabetSize = modelData[readOffset++];
                    CheckAlphabetSize(modelAlphabetSize);
                    for (int i = 0; i < k_AlphabetSize; i++)
                    {
                        byte length = modelData[readOffset++];
                        symbolLengths[numContexts * context + i] = length;
                    }
                }
            }

            // generate tables
            var tmpSymbolLengths = new NativeArray<byte>(k_AlphabetSize, Allocator.Temp);
            var tmpSymbolDecodeTable = new NativeArray<ushort>(1 << k_MaxHuffmanSymbolLength, Allocator.Temp);
            var symbolCodes = new NativeArray<byte>(k_AlphabetSize, Allocator.Temp);

            for (int context = 0; context < numContexts; context++)
            {
                for (int i = 0; i < k_AlphabetSize; i++)
                    tmpSymbolLengths[i] = symbolLengths[numContexts * context + i];

                GenerateHuffmanCodes(symbolCodes, 0, tmpSymbolLengths, 0, k_AlphabetSize, k_MaxHuffmanSymbolLength);
                GenerateHuffmanDecodeTable(tmpSymbolDecodeTable, 0, tmpSymbolLengths, symbolCodes, k_AlphabetSize, k_MaxHuffmanSymbolLength);
                for (int i = 0; i < k_AlphabetSize; i++)
                {
                    SharedStaticCompressionModel.Default.Data.encodeTable[context * k_AlphabetSize + i] = (ushort)((symbolCodes[i] << 8) | symbolLengths[numContexts * context + i]);
                }
                for (int i = 0; i < (1 << k_MaxHuffmanSymbolLength); i++)
                {
                    SharedStaticCompressionModel.Default.Data.decodeTable[context * (1 << k_MaxHuffmanSymbolLength) + i] = tmpSymbolDecodeTable[i];
                }
            }

            for (int i = 0; i < k_AlphabetSize; i++)
            {
                ushort enc = SharedStaticCompressionModel.Default.Data.encodeTable[i];
                SharedStaticCompressionModel.Default.Data.bucketFused[i] =
                    (ulong)SharedStaticCompressionModel.Default.Data.bucketOffsets[i]
                    | ((ulong)(uint)(enc >> 8) << 32)
                    | ((ulong)(enc & 0xFF) << 48)
                    | ((ulong)SharedStaticCompressionModel.Default.Data.bucketSizes[i] << 56);
            }

            for (int p = 0; p < (1 << k_MaxHuffmanSymbolLength); p++)
            {
                ushort dec = SharedStaticCompressionModel.Default.Data.decodeTable[p];   // (symbol << 8) | length
                int symbol = dec >> 8;
                SharedStaticCompressionModel.Default.Data.decodeFused[p] =
                    (ulong)(uint)(dec & 0xFF)
                    | ((ulong)SharedStaticCompressionModel.Default.Data.bucketSizes[symbol] << 8)
                    | ((ulong)SharedStaticCompressionModel.Default.Data.bucketOffsets[symbol] << 16);
            }
        }

        static void GenerateHuffmanCodes(NativeArray<byte> symbolCodes, int symbolCodesOffset, NativeArray<byte> symbolLengths, int symbolLengthsOffset, int alphabetSize, int maxCodeLength)
        {
            CheckAlphabetAndMaxCodeLength(alphabetSize, maxCodeLength);

            var lengthCounts = new NativeArray<byte>(maxCodeLength + 1, Allocator.Temp);
            var symbolList = new NativeArray<byte>((maxCodeLength + 1) * alphabetSize, Allocator.Temp);

            //byte[] symbol_list[(MAX_HUFFMAN_CODE_LENGTH + 1u) * MAX_NUM_HUFFMAN_SYMBOLS];
            for (int symbol = 0; symbol < alphabetSize; symbol++)
            {
                int symbolLength = symbolLengths[symbol + symbolLengthsOffset];
                CheckExceedMaxCodeLength(symbolLength, maxCodeLength);
                symbolList[(maxCodeLength + 1) * symbolLength + lengthCounts[symbolLength]++] = (byte)symbol;
            }

            uint nextCodeWord = 0;
            for (int length = 1; length <= maxCodeLength; length++)
            {
                int length_count = lengthCounts[length];
                for (int i = 0; i < length_count; i++)
                {
                    int symbol = symbolList[(maxCodeLength + 1) * length + i];
                    CheckSymbolLength(symbolLengths, symbolLengthsOffset, symbol, length);
                    symbolCodes[symbol + symbolCodesOffset] = (byte)ReverseBits(nextCodeWord++, length);
                }
                nextCodeWord <<= 1;
            }
        }

        static uint ReverseBits(uint value, int num_bits)
        {
            value = ((value & 0x55555555u) << 1) | ((value & 0xAAAAAAAAu) >> 1);
            value = ((value & 0x33333333u) << 2) | ((value & 0xCCCCCCCCu) >> 2);
            value = ((value & 0x0F0F0F0Fu) << 4) | ((value & 0xF0F0F0F0u) >> 4);
            value = ((value & 0x00FF00FFu) << 8) | ((value & 0xFF00FF00u) >> 8);
            value = (value << 16) | (value >> 16);
            return value >> (32 - num_bits);
        }

        // decode table entries: (symbol << 8) | length
        static void GenerateHuffmanDecodeTable(NativeArray<ushort> decodeTable, int decodeTableOffset, NativeArray<byte> symbolLengths, NativeArray<byte> symbolCodes, int alphabetSize, int maxCodeLength)
        {
            CheckAlphabetAndMaxCodeLength(alphabetSize, maxCodeLength);

            uint maxCode = 1u << maxCodeLength;
            for (int symbol = 0; symbol < alphabetSize; symbol++)
            {
                int length = symbolLengths[symbol];
                CheckExceedMaxCodeLength(length, maxCodeLength);
                if (length > 0)
                {
                    uint code = symbolCodes[symbol];
                    uint step = 1u << length;
                    do
                    {
                        decodeTable[(int)(decodeTableOffset + code)] = (ushort)(symbol << 8 | length);
                        code += step;
                    }
                    while (code < maxCode);
                }
            }
        }

        /// <summary>
        /// Bucket n starts at bucketOffsets[n] and ends at bucketOffsets[n] + (1 &lt;&lt; bucketSizes[n]).
        /// (code &lt;&lt; 8) | length
        /// </summary>
        internal fixed ushort encodeTable[k_MaxContexts * k_AlphabetSize];
        /// <summary>
        /// Bucket n starts at bucketOffsets[n] and ends at bucketOffsets[n] + (1 &lt;&lt; bucketSizes[n]).
        /// (symbol &lt;&lt; 8) | length
        /// </summary>
        internal fixed ushort decodeTable[k_MaxContexts * (1 << k_MaxHuffmanSymbolLength)];
        /// <summary>
        /// Specifies the sizes of the buckets in bits, so a bucket of n bits has 2^n values.
        /// </summary>
        internal fixed byte bucketSizes[k_AlphabetSize];
        /// <summary>
        /// Specifies the starting positions of the bucket.
        /// </summary>
        internal fixed uint bucketOffsets[k_AlphabetSize];
        /// <summary>
        /// Per-bucket encode data fused into one word so a packed write needs a single load instead of three:
        /// bucketOffsets (bits 0-31) | huffman code (32-47) | code length (48-55) | bucket size in bits (56-63).
        /// </summary>
        internal fixed ulong bucketFused[k_AlphabetSize];
        /// <summary>
        /// Per-peek decode data fused into one word so a packed read needs a single load instead of three
        /// (decodeTable + bucketSizes + bucketOffsets): code length (bits 0-7) | bucket size in bits (8-15) |
        /// bucketOffsets (16-47). Indexed by the maxSymbolLength-bit peek value.
        /// </summary>
        internal fixed ulong decodeFused[k_MaxContexts * (1 << k_MaxHuffmanSymbolLength)];

        /// <summary>
        /// Calculates the bucket index into the <see cref="encodeTable"/> where the specified value should be written.
        /// </summary>
        /// <param name="value">A 4-byte unsigned integer value to find a bucket for.</param>
        /// <returns>The bucket index where to put the value.</returns>
        readonly public int CalculateBucket(uint value)
        {
            int bucketIndex = k_FirstBucketCandidate[math.lzcnt(value)];
            if (bucketIndex + 1 < k_AlphabetSize && value >= bucketOffsets[bucketIndex + 1])
                bucketIndex++;

            return bucketIndex;
        }

        /// <summary>
        /// The compressed size in bits of the given unsigned integer value
        /// </summary>
        /// <param name="value">the unsigned int value you want to compress</param>
        /// <returns></returns>
        public readonly int GetCompressedSizeInBits(uint value)
        {
            int bucket = CalculateBucket(value);
            int bits = bucketSizes[bucket];
            ushort encodeEntry = encodeTable[bucket];
            return (encodeEntry & 0xff) + bits;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckAlphabetSize(int alphabetSize)
        {
            if (alphabetSize != k_AlphabetSize)
            {
                throw new InvalidOperationException("The alphabet size of compression models must be " + k_AlphabetSize);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckSymbolLength(NativeArray<byte> symbolLengths, int symbolLengthsOffset, int symbol, int length)
        {
            if (symbolLengths[symbol + symbolLengthsOffset] != length)
                throw new InvalidOperationException("Incorrect symbol length");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckAlphabetAndMaxCodeLength(int alphabetSize, int maxCodeLength)
        {
            if (alphabetSize > 256 || maxCodeLength > 8)
                throw new InvalidOperationException("Can only generate huffman codes up to alphabet size 256 and maximum code length 8");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckExceedMaxCodeLength(int length, int maxCodeLength)
        {
            if (length > maxCodeLength)
                throw new InvalidOperationException("Maximum code length exceeded");
        }
    }
}
