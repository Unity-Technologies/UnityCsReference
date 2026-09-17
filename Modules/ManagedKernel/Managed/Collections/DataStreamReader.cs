// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Collections
{
    /// <summary>
    /// Reads data serialized by a <see cref="DataStreamWriter"/>.
    /// </summary>
    /// <remarks>
    /// The DataStreamReader class is the counterpart of the
    /// <see cref="DataStreamWriter"/> class and can be used to deserialize
    /// data which was prepared with it.
    ///
    /// Data is stored in the endian format native to the current machine architecture.
    /// <br/>
    /// For network byte order use the so named methods.
    /// <br/>
    /// Simple usage example:
    /// <code>
    /// using (var data = new NativeArray&lt;byte&gt;(16, Allocator.Persistent))
    /// {
    ///     var dataWriter = new DataStreamWriter(data);
    ///     dataWriter.WriteInt(42);
    ///     dataWriter.WriteInt(1234);
    ///     dataWriter.Flush();
    ///     var dataReader = new DataStreamReader(dataWriter.AsNativeArray());
    ///     var myFirstInt = dataReader.ReadInt();
    ///     var mySecondInt = dataReader.ReadInt();
    /// }
    /// </code>
    ///
    /// DataStreamReader carries the position of the read pointer inside the struct,
    /// taking a copy of the reader will also copy the read position. This includes passing the
    /// reader to a method by value instead of by ref.
    ///
    /// <seealso cref="DataStreamWriter"/>
    /// <seealso cref="IsLittleEndian"/>
    /// </remarks>
    [MovedFrom(true, "Unity.Networking.Transport")]
    [GenerateTestsForBurstCompatibility]
    public unsafe struct DataStreamReader
    {
        [NativeDisableUnsafePtrRestriction] internal byte* m_BufferPtr;
        int m_BitPos;
        int m_FailedReads;
        int m_Length;
        AtomicSafetyHandle m_Safety;

        /// <summary>
        /// Initializes a new instance of the DataStreamReader struct with a NativeArray&lt;byte&gt;
        /// </summary>
        /// <param name="array">The buffer to attach to the DataStreamReader.</param>
        public DataStreamReader(NativeArray<byte> array)
        {
            Initialize(out this, array);
        }

        static void Initialize(out DataStreamReader self, NativeArray<byte> array)
        {
            CheckEndianness();
            CheckCapacity(array.Length);
            self.m_Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array);
            self.m_BufferPtr = (byte*)array.GetUnsafeReadOnlyPtr();
            self.m_Length = array.Length;
            self.m_BitPos = 0;
            self.m_FailedReads = 0;
        }

        /// <summary>
        /// Show the byte order in which the current computer architecture stores data.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders.
        /// <list type="bullet">
        /// <item>Big-endian: the most significant byte is at the left end of a word.</item>
        /// <item>Little-endian: means the most significant byte is at the right end of a word.</item>
        /// </list>
        /// </remarks>
        public static bool IsLittleEndian { get { return DataStreamWriter.IsLittleEndian; } }

        static short ByteSwap(short val)
        {
            return (short)(((val & 0xff) << 8) | ((val >> 8) & 0xff));
        }

        static int ByteSwap(int val)
        {
            return (int)(((val & 0xff) << 24) | ((val & 0xff00) << 8) | ((val >> 8) & 0xff00) | ((val >> 24) & 0xff));
        }

        /// <summary>
        /// If there is a read failure this returns true. A read failure might happen if this attempts to read more than there is capacity for.
        /// </summary>
        public readonly bool HasFailedReads => m_FailedReads > 0;

        /// <summary>
        /// The total size of the buffer space this reader is working with.
        /// </summary>
        public readonly int Length
        {
            get
            {
                CheckRead();
                return m_Length;
            }
        }

        /// <summary>
        /// True if the reader has been pointed to a valid buffer space. This
        /// would be false if the reader was created with no arguments.
        /// </summary>
        public readonly bool IsCreated
        {
            get { return m_BufferPtr != null; }
        }

        void ReadBytesInternal(byte* data, int length)
        {
            CheckRead();
            int startByte = (m_BitPos + 7) >> 3;
            if (Hint.Unlikely(startByte + length > m_Length))
            {
                ++m_FailedReads;
                Unity.Scripting.LowLevel.Debug.LogError($"Trying to read {length} bytes from a stream where only {m_Length - startByte} are available");
                UnsafeUtility.MemClear(data, length);
                return;
            }
            Flush();
            UnsafeUtility.MemCpy(data, m_BufferPtr + (m_BitPos >> 3), length);
            m_BitPos += length << 3;
        }

        /// <summary>
        /// Read and copy data into the given NativeArray of bytes. An error will
        /// be logged if not enough bytes are available to fill the array, and
        /// <see cref="HasFailedReads"/> will then be true.
        /// </summary>
        /// <param name="array">Array to copy data into.</param>
        public void ReadBytes(NativeArray<byte> array)
        {
            ReadBytesInternal((byte*)array.GetUnsafePtr(), array.Length);
        }

        /// <summary>
        /// Read and copy data into the given <c>Span</c> of bytes. An error will
        /// be logged if not enough bytes are available to fill the array, and
        /// <see cref="HasFailedReads"/> will then be true.
        /// </summary>
        /// <param name="span">Span to copy data into.</param>
        public void ReadBytes(Span<byte> span)
        {
            fixed (byte* ptr = span)
            {
                ReadBytesInternal(ptr, span.Length);
            }
        }

        /// <summary>
        /// Gets the number of bytes read from the data stream.
        /// </summary>
        /// <returns>Number of bytes read.</returns>
        public int GetBytesRead()
        {
            return (m_BitPos + 7) >> 3;
        }

        /// <summary>
        /// Gets the number of bits read from the data stream.
        /// </summary>
        /// <returns>Number of bits read.</returns>
        public int GetBitsRead()
        {
            return m_BitPos;
        }

        /// <summary>
        /// Sets the current position of this stream to the given value.
        /// An error will be logged if <paramref name="pos"/> is outside the length of the stream.
        /// <br/>
        /// The read position is set to the start of the given byte.
        /// </summary>
        /// <param name="pos">Absolute byte offset to seek to.</param>
        public void SeekSet(int pos)
        {
            if (Hint.Unlikely(pos > m_Length))
            {
                ++m_FailedReads;
                Unity.Scripting.LowLevel.Debug.LogError($"Trying to seek to {pos} in a stream of length {m_Length}");
                return;
            }
            m_BitPos = pos << 3;
        }

        /// <summary>
        /// Reads an unsigned byte from the current stream and advances the current position of the stream by one byte.
        /// </summary>
        /// <returns>The next byte read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public byte ReadByte()
        {
            byte data;
            ReadBytesInternal((byte*)&data, sizeof(byte));
            return data;
        }

        /// <summary>
        /// Reads a 2-byte signed short from the current stream and advances the current position of the stream by two bytes.
        /// </summary>
        /// <returns>A 2-byte signed short read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public short ReadShort()
        {
            short data;
            ReadBytesInternal((byte*)&data, sizeof(short));
            return data;
        }

        /// <summary>
        /// Reads a 2-byte unsigned short from the current stream and advances the current position of the stream by two bytes.
        /// </summary>
        /// <returns>A 2-byte unsigned short read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public ushort ReadUShort()
        {
            ushort data;
            ReadBytesInternal((byte*)&data, sizeof(ushort));
            return data;
        }

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <returns>A 4-byte signed integer read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public int ReadInt()
        {
            int data;
            ReadBytesInternal((byte*)&data, sizeof(int));
            return data;
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <returns>A 4-byte unsigned integer read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public uint ReadUInt()
        {
            uint data;
            ReadBytesInternal((byte*)&data, sizeof(uint));
            return data;
        }

        /// <summary>
        /// Reads an 8-byte signed long from the stream and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <returns>An 8-byte signed long read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public long ReadLong()
        {
            long data;
            ReadBytesInternal((byte*)&data, sizeof(long));
            return data;
        }

        /// <summary>
        /// Reads an 8-byte unsigned long from the stream and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <returns>An 8-byte unsigned long read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public ulong ReadULong()
        {
            ulong data;
            ReadBytesInternal((byte*)&data, sizeof(ulong));
            return data;
        }

        /// <summary>
        /// Aligns the read pointer to the next byte-aligned position. Does nothing if already aligned.
        /// </summary>
        /// <remarks>If you call <see cref="DataStreamWriter.Flush"/>, call this to bit-align the reader.</remarks>
        public void Flush()
        {
            m_BitPos = (m_BitPos + 7) & ~7;
        }

        /// <summary>
        /// Reads a 2-byte signed short from the current stream in Big-endian byte order and advances the current position of the stream by two bytes.
        /// If the current endianness is in little-endian order, the byte order will be swapped.
        /// </summary>
        /// <returns>A 2-byte signed short read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public short ReadShortNetworkByteOrder()
        {
            short data;
            ReadBytesInternal((byte*)&data, sizeof(short));
            return IsLittleEndian ? ByteSwap(data) : data;
        }

        /// <summary>
        /// Reads a 2-byte unsigned short from the current stream in Big-endian byte order and advances the current position of the stream by two bytes.
        /// If the current endianness is in little-endian order, the byte order will be swapped.
        /// </summary>
        /// <returns>A 2-byte unsigned short read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public ushort ReadUShortNetworkByteOrder()
        {
            return (ushort)ReadShortNetworkByteOrder();
        }

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream in Big-endian byte order and advances the current position of the stream by four bytes.
        /// If the current endianness is in little-endian order, the byte order will be swapped.
        /// </summary>
        /// <returns>A 4-byte signed integer read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public int ReadIntNetworkByteOrder()
        {
            int data;
            ReadBytesInternal((byte*)&data, sizeof(int));
            return IsLittleEndian ? ByteSwap(data) : data;
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream in Big-endian byte order and advances the current position of the stream by four bytes.
        /// If the current endianness is in little-endian order, the byte order will be swapped.
        /// </summary>
        /// <returns>A 4-byte unsigned integer read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public uint ReadUIntNetworkByteOrder()
        {
            return (uint)ReadIntNetworkByteOrder();
        }

        /// <summary>
        /// Reads a 4-byte floating point value from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <returns>A 4-byte floating point value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public float ReadFloat()
        {
            UIntFloat uf = new UIntFloat();
            uf.intValue = (uint)ReadInt();
            return uf.floatValue;
        }

        /// <summary>
        /// Reads a 8-byte floating point value from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <returns>A 8-byte floating point value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public double ReadDouble()
        {
            UIntFloat uf = new UIntFloat();
            uf.longValue = (ulong)ReadLong();
            return uf.doubleValue;
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream using a <see cref="StreamCompressionModel"/> and advances the current position the number of bits depending on the model.
        /// </summary>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>A 4-byte unsigned integer read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public uint ReadPackedUInt(in StreamCompressionModel model)
        {
            return ReadPackedUIntInternal(StreamCompressionModel.k_MaxHuffmanSymbolLength, model);
        }

        uint ReadPackedUIntInternal(int maxSymbolLength, in StreamCompressionModel model)
        {
            CheckRead();
            ulong window = LoadWord(m_BitPos >> 3) >> (m_BitPos & 7);
            uint peek = (uint)window & ((1u << maxSymbolLength) - 1u);
            ulong fused = model.decodeFused[(int)peek];
            int codeLen = (int)(fused & 0xFF);
            int bits = (int)((fused >> 8) & 0xFF);
            if (Hint.Unlikely((long)m_BitPos + codeLen + bits > (long)m_Length * 8))
            {
                ++m_FailedReads;
                Unity.Scripting.LowLevel.Debug.LogError($"Trying to read {codeLen} bits from a stream where only {(long)m_Length * 8 - m_BitPos} are available");
                return 0;
            }
            uint offset = (uint)(fused >> 16);
            uint payload = (uint)((window >> codeLen) & ((1UL << bits) - 1UL));
            m_BitPos += codeLen + bits;
            return payload + offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ulong LoadWord(int byteIndex)
        {
            if (Hint.Likely(byteIndex + 8 <= m_Length))
            {
                // Unaligned intrinsic, not a raw *(ulong*) deref: an unaligned 64-bit load at byteIndex is undefined behavior
                // on 32-bit ARM (armeabi-v7a) under IL2CPP and silently returns wrong bytes (UUM-138845).
                return Unsafe.ReadUnaligned<ulong>(m_BufferPtr + byteIndex);
            }
            ulong w = 0;
            for (int i = 0, avail = m_Length - byteIndex; i < avail && i < 8; ++i)
                w |= (ulong)m_BufferPtr[byteIndex + i] << (i << 3);
            return w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint ReadRawBitsInternal(int numbits)
        {
            CheckBits(numbits);
            if (Hint.Unlikely((long)m_BitPos + numbits > (long)m_Length * 8))
            {
                ++m_FailedReads;
                Unity.Scripting.LowLevel.Debug.LogError($"Trying to read {numbits} bits from a stream where only {(long)m_Length * 8 - m_BitPos} are available");
                return 0;
            }
            ulong window = LoadWord(m_BitPos >> 3) >> (m_BitPos & 7);
            m_BitPos += numbits;
            return (uint)(window & ((1UL << numbits) - 1UL));
        }

        /// <summary>
        /// Reads a specified number of bits from the data stream.
        /// </summary>
        /// <param name="numbits">A positive number of bytes to write.</param>
        /// <returns>A 4-byte unsigned integer read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public uint ReadRawBits(int numbits)
        {
            CheckRead();
            return ReadRawBitsInternal(numbits);
        }

        /// <summary>
        /// Reads an 8-byte unsigned long value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>An 8-byte unsigned long read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public ulong ReadPackedULong(in StreamCompressionModel model)
        {
            ulong value = ReadPackedUInt(model);
            value |= (ulong)ReadPackedUInt(model) << 32;
            return value;
        }

        /// <summary>
        /// Reads a 4-byte signed integer value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// <br/>
        /// Negative values de-interleaves from positive values before returning, for example (0, -1, 1, -2, 2) -> (-2, -1, 0, 1, 2)
        /// </summary>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>A 4-byte signed integer read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public int ReadPackedInt(in StreamCompressionModel model)
        {
            uint folded = ReadPackedUInt(model);
            return (int)(folded >> 1) ^ -(int)(folded & 1);    // Deinterleave values from [0, -1, 1, -2, 2...] to [..., -2, -1, -0, 1, 2, ...]
        }

        /// <summary>
        /// Reads an 8-byte signed long value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// <br/>
        /// Negative values de-interleaves from positive values before returning, for example (0, -1, 1, -2, 2) -> (-2, -1, 0, 1, 2)
        /// </summary>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>An 8-byte signed long read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public long ReadPackedLong(in StreamCompressionModel model)
        {
            ulong folded = ReadPackedULong(model);
            return (long)(folded >> 1) ^ -(long)(folded & 1);    // Deinterleave values from [0, -1, 1, -2, 2...] to [..., -2, -1, -0, 1, 2, ...]
        }

        /// <summary>
        /// Reads a 4-byte floating point value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>A 4-byte floating point value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public float ReadPackedFloat(in StreamCompressionModel model)
        {
            return ReadPackedFloatDelta(0, model);
        }

        /// <summary>
        /// Reads a 8-byte floating point value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>A 8-byte floating point value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public double ReadPackedDouble(in StreamCompressionModel model)
        {
            return ReadPackedDoubleDelta(0, model);
        }

        /// <summary>
        /// Reads a 4-byte signed integer delta value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous 4-byte signed integer value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>A 4-byte signed integer read from the current stream, or 0 if the end of the stream has been reached.
        /// If the data did not change, this also returns 0.
        /// <br/>
        /// See: <see cref="HasFailedReads"/> to verify if the read failed.</returns>
        public int ReadPackedIntDelta(int baseline, in StreamCompressionModel model)
        {
            int delta = ReadPackedInt(model);
            return baseline - delta;
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer delta value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous 4-byte unsigned integer value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>A 4-byte unsigned integer read from the current stream, or 0 if the end of the stream has been reached.
        /// If the data did not change, this also returns 0.
        /// <br/>
        /// See: <see cref="HasFailedReads"/> to verify if the read failed.</returns>
        public uint ReadPackedUIntDelta(uint baseline, in StreamCompressionModel model)
        {
            uint delta = (uint)ReadPackedInt(model);
            return baseline - delta;
        }

        /// <summary>
        /// Reads an 8-byte signed long delta value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous 8-byte signed long value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>An 8-byte signed long read from the current stream, or 0 if the end of the stream has been reached.
        /// If the data did not change, this also returns 0.
        /// <br/>
        /// See: <see cref="HasFailedReads"/> to verify if the read failed.</returns>
        public long ReadPackedLongDelta(long baseline, in StreamCompressionModel model)
        {
            long delta = ReadPackedLong(model);
            return baseline - delta;
        }

        /// <summary>
        /// Reads an 8-byte unsigned long delta value from the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous 8-byte unsigned long value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for reading value in a packed manner.</param>
        /// <returns>An 8-byte unsigned long read from the current stream, or 0 if the end of the stream has been reached.
        /// If the data did not change, this also returns 0.
        /// <br/>
        /// See: <see cref="HasFailedReads"/> to verify if the read failed.</returns>
        public ulong ReadPackedULongDelta(ulong baseline, in StreamCompressionModel model)
        {
            ulong delta = (ulong)ReadPackedLong(model);
            return baseline - delta;
        }

        /// <summary>
        /// Reads a 4-byte floating point value from the data stream.
        ///
        /// If the first bit is 0, the data did not change and <paramref name="baseline"/> will be returned.
        /// </summary>
        /// <param name="baseline">The previous 4-byte floating point value.</param>
        /// <param name="model">Not currently used.</param>
        /// <returns>A 4-byte floating point value read from the current stream, or <paramref name="baseline"/> if there are no changes to the value.
        /// <br/>
        /// See: <see cref="HasFailedReads"/> to verify if the read failed.</returns>
        public float ReadPackedFloatDelta(float baseline, in StreamCompressionModel model)
        {
            CheckRead();
            if (ReadRawBitsInternal(1) == 0)
                return baseline;

            var bits = 32;
            UIntFloat uf = new UIntFloat();
            uf.intValue = ReadRawBitsInternal(bits);
            return uf.floatValue;
        }

        /// <summary>
        /// Reads a 8-byte floating point value from the data stream.
        ///
        /// If the first bit is 0, the data did not change and <paramref name="baseline"/> will be returned.
        /// </summary>
        /// <param name="baseline">The previous 8-byte floating point value.</param>
        /// <param name="model">Not currently used.</param>
        /// <returns>A 8-byte floating point value read from the current stream, or <paramref name="baseline"/> if there are no changes to the value.
        /// <br/>
        /// See: <see cref="HasFailedReads"/> to verify if the read failed.</returns>
        public double ReadPackedDoubleDelta(double baseline, in StreamCompressionModel model)
        {
            CheckRead();
            if (ReadRawBitsInternal(1) == 0)
                return baseline;

            var bits = 32;
            UIntFloat uf = new UIntFloat();
            var data = (uint*)&uf.longValue;
            data[0] = ReadRawBitsInternal(bits);
            data[1] |= ReadRawBitsInternal(bits);
            return uf.doubleValue;
        }

        /// <summary>
        /// Reads a <c>FixedString32Bytes</c> value from the current stream and advances the current position of the stream by the length of the string.
        /// </summary>
        /// <returns>A <c>FixedString32Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString32Bytes ReadFixedString32()
        {
            FixedString32Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedStringInternal(data, str.Capacity);
            return str;
        }

        /// <summary>
        /// Reads a <c>FixedString64Bytes</c> value from the current stream and advances the current position of the stream by the length of the string.
        /// </summary>
        /// <returns>A <c>FixedString64Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString64Bytes ReadFixedString64()
        {
            FixedString64Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedStringInternal(data, str.Capacity);
            return str;
        }

        /// <summary>
        /// Reads a <c>FixedString128Bytes</c> value from the current stream and advances the current position of the stream by the length of the string.
        /// </summary>
        /// <returns>A <c>FixedString128Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString128Bytes ReadFixedString128()
        {
            FixedString128Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedStringInternal(data, str.Capacity);
            return str;
        }

        /// <summary>
        /// Reads a <c>FixedString512Bytes</c> value from the current stream and advances the current position of the stream by the length of the string.
        /// </summary>
        /// <returns>A <c>FixedString512Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString512Bytes ReadFixedString512()
        {
            FixedString512Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedStringInternal(data, str.Capacity);
            return str;
        }

        /// <summary>
        /// Reads a <c>FixedString4096Bytes</c> value from the current stream and advances the current position of the stream by the length of the string.
        /// </summary>
        /// <returns>A <c>FixedString4096Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString4096Bytes ReadFixedString4096()
        {
            FixedString4096Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedStringInternal(data, str.Capacity);
            return str;
        }

        /// <summary>
        /// Read and copy data into the given NativeArray of bytes, an error will
        /// be logged if not enough bytes are available in the array.
        /// </summary>
        /// <param name="array">Buffer to write the string bytes to.</param>
        /// <returns>Length of data read into byte array, or zero if error occurred.</returns>
        public ushort ReadFixedString(NativeArray<byte> array)
        {
            return ReadFixedStringInternal((byte*)array.GetUnsafePtr(), array.Length);
        }

        unsafe ushort ReadFixedStringInternal(byte* data, int maxLength)
        {
            ushort length = ReadUShort();
            if (Hint.Unlikely(length > maxLength))
            {
                Unity.Scripting.LowLevel.Debug.LogError($"Trying to read a string of length {length} but max length is {maxLength}");
                return 0;
            }
            ReadBytesInternal(data, length);
            return length;
        }

        /// <summary>
        /// Reads a <c>FixedString32Bytes</c> delta value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous <c>FixedString32Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>A <c>FixedString32Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString32Bytes ReadPackedFixedString32Delta(FixedString32Bytes baseline, in StreamCompressionModel model)
        {
            FixedString32Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDeltaInternal(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }

        /// <summary>
        /// Reads a <c>FixedString64Bytes</c> delta value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous <c>FixedString64Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>A <c>FixedString64Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString64Bytes ReadPackedFixedString64Delta(FixedString64Bytes baseline, in StreamCompressionModel model)
        {
            FixedString64Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDeltaInternal(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }

        /// <summary>
        /// Reads a <c>FixedString128Bytes</c> delta value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous <c>FixedString128Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>A <c>FixedString128Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString128Bytes ReadPackedFixedString128Delta(FixedString128Bytes baseline, in StreamCompressionModel model)
        {
            FixedString128Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDeltaInternal(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }

        /// <summary>
        /// Reads a <c>FixedString512Bytes</c> delta value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous <c>FixedString512Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>A <c>FixedString512Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString512Bytes ReadPackedFixedString512Delta(FixedString512Bytes baseline, in StreamCompressionModel model)
        {
            FixedString512Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDeltaInternal(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }

        /// <summary>
        /// Reads a <c>FixedString4096Bytes</c> delta value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="baseline">The previous <c>FixedString4096Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>A <c>FixedString4096Bytes</c> value read from the current stream, or 0 if the end of the stream has been reached.</returns>
        public unsafe FixedString4096Bytes ReadPackedFixedString4096Delta(FixedString4096Bytes baseline, in StreamCompressionModel model)
        {
            FixedString4096Bytes str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDeltaInternal(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }

        /// <summary>
        /// Read and copy data into the given NativeArray of bytes, an error will
        /// be logged if not enough bytes are available in the array.
        /// </summary>
        /// <param name="data">Array for the current fixed string.</param>
        /// <param name="baseData">Array containing the previous value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Length of data read into byte array, or zero if error occurred.</returns>
        public ushort ReadPackedFixedStringDelta(NativeArray<byte> data, NativeArray<byte> baseData, in StreamCompressionModel model)
        {
            return ReadPackedFixedStringDeltaInternal((byte*)data.GetUnsafePtr(), data.Length, (byte*)baseData.GetUnsafePtr(), (ushort)baseData.Length, model);
        }

        unsafe ushort ReadPackedFixedStringDeltaInternal(byte* data, int maxLength, byte* baseData, ushort baseLength, in StreamCompressionModel model)
        {
            uint length = ReadPackedUIntDelta(baseLength, model);
            if (Hint.Unlikely(length > (uint)maxLength))
            {
                Unity.Scripting.LowLevel.Debug.LogError($"Trying to read a string of length {length} but max length is {maxLength}");
                return 0;
            }
            if (length <= baseLength)
            {
                for (int i = 0; i < length; ++i)
                    data[i] = (byte)ReadPackedUIntDelta(baseData[i], model);
            }
            else
            {
                for (int i = 0; i < baseLength; ++i)
                    data[i] = (byte)ReadPackedUIntDelta(baseData[i], model);
                for (int i = baseLength; i < length; ++i)
                    data[i] = (byte)ReadPackedUInt(model);
            }
            return (ushort)length;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void CheckRead()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckBits(int numBits)
        {
            if (numBits < 0 || numBits > 32)
                throw new ArgumentOutOfRangeException($"Invalid number of bits specified: {numBits}! Valid range is (0, 32) inclusive.");
        }

        // Cap capacity so the bit position (a signed int) and its (+7) byte rounding can't overflow.
        const int k_MaxCapacity = (int.MaxValue - 7) >> 3;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckCapacity(int capacity)
        {
            if (capacity > k_MaxCapacity)
                throw new ArgumentException($"Buffer capacity {capacity} exceeds the maximum {k_MaxCapacity} bytes addressable by a signed-int bit position.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckEndianness()
        {
            if (!IsLittleEndian)
                throw new NotSupportedException("DataStreamReader unpacks bits assuming a little-endian host; only the NetworkByteOrder methods are endian-independent.");
        }
    }
}
