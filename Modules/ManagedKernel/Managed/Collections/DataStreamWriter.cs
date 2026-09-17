// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Collections
{
    /// <summary>
    /// Writes data in an endian format to serialize data.
    /// </summary>
    /// <remarks>
    /// Data streams can be used to serialize data (e.g. over the network). The
    /// DataStreamWriter and <see cref="DataStreamReader"/> classes work together
    /// to serialize data for sending and then to deserialize when receiving.
    ///
    /// DataStreamWriter writes data in the endian format native to the current machine architecture.
    /// For network byte order use the so named methods.
    /// <br/>
    /// The reader can be used to deserialize the data from a NativeArray&lt;byte&gt;, writing data
    /// to a NativeArray&lt;byte&gt; and reading it back can be done like this:
    /// <code>
    /// using (var data = new NativeArray&lt;byte&gt;(16, Allocator.Persistent))
    /// {
    ///     var dataWriter = new DataStreamWriter(data);
    ///     dataWriter.WriteInt(42);
    ///     dataWriter.WriteInt(1234);
    ///     // Length is the actual amount of data inside the writer,
    ///     // Capacity is the total amount.
    ///     var dataReader = new DataStreamReader(data.GetSubArray(0, dataWriter.Length));
    ///     var myFirstInt = dataReader.ReadInt();
    ///     var mySecondInt = dataReader.ReadInt();
    /// }
    /// </code>
    ///
    /// There are a number of functions for various data types. If a copy of the writer
    /// is stored it can be used to overwrite the data later on at any bit alignment.
    /// This is particularly useful when the size of the data is written at the start and you want to write it at
    /// the end when you know the value.
    /// <seealso cref="IsLittleEndian"/>
    ///
    /// <code>
    /// using (var data = new NativeArray&lt;byte&gt;(16, Allocator.Persistent))
    /// {
    ///     var dataWriter = new DataStreamWriter(data);
    ///     // My header data
    ///     var headerSizeMark = dataWriter;
    ///     dataWriter.WriteUShort((ushort)0);
    ///     var payloadSizeMark = dataWriter;
    ///     dataWriter.WriteUShort((ushort)0);
    ///     dataWriter.WriteInt(42);
    ///     dataWriter.WriteInt(1234);
    ///     var headerSize = data.Length;
    ///     // Update header size to correct value
    ///     headerSizeMark.WriteUShort((ushort)headerSize);
    ///     // My payload data
    ///     dataWriter.WriteInt(99);
    ///     // Update payload size to correct value
    ///     payloadSizeMark.WriteUShort((ushort)(dataWriter.Length - headerSize));
    /// }
    /// </code>
    /// </remarks>
    [MovedFrom(true, "Unity.Networking.Transport", "Unity.Networking.Transport")]
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility]
    public unsafe struct DataStreamWriter
    {
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
        public static bool IsLittleEndian
        {
            get
            {
                uint test = 1;
                byte* testPtr = (byte*)&test;
                return testPtr[0] == 1;
            }
        }

        struct StreamData
        {
            public byte* buffer;
            public int capacity;
            public int bitPos;
            public int failedWrites;
        }

        [NativeDisableUnsafePtrRestriction] StreamData m_Data;
        /// <summary>
        /// Used for sending data asynchronously.
        /// </summary>
        public IntPtr m_SendHandleData;

        AtomicSafetyHandle m_Safety;

        /// <summary>
        /// Initializes a new instance of the DataStreamWriter struct.
        /// </summary>
        /// <param name="length">The number of bytes available in the buffer.</param>
        /// <param name="allocator">The <see cref="Allocator"/> used to allocate the memory.</param>
        public DataStreamWriter(int length, AllocatorManager.AllocatorHandle allocator)
        {
            CheckAllocator(allocator);
            Initialize(out this, CollectionHelper.CreateNativeArray<byte>(length, allocator));
        }

        /// <summary>
        /// Initializes a new instance of the DataStreamWriter struct with a NativeArray&lt;byte&gt;
        /// </summary>
        /// <param name="data">The buffer to attach to the DataStreamWriter.</param>
        public DataStreamWriter(NativeArray<byte> data)
        {
            Initialize(out this, data);
        }

        /// <summary>
        /// Initializes a new instance of the DataStreamWriter struct with a memory we don't own
        /// </summary>
        /// <param name="data">Pointer to the data</param>
        /// <param name="length">Length of the data</param>
        public DataStreamWriter(byte* data, int length)
        {
            var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, length, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref na, AtomicSafetyHandle.GetTempMemoryHandle());
            Initialize(out this, na);
        }

        /// <summary>
        /// Convert internal data buffer to NativeArray for use in entities APIs.
        /// </summary>
        /// <returns>NativeArray representation of internal buffer.</returns>
        public NativeArray<byte> AsNativeArray()
        {
            var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(m_Data.buffer, Length, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref na, m_Safety);
            return na;
        }

        static void Initialize(out DataStreamWriter self, NativeArray<byte> data)
        {
            CheckEndianness();
            CheckCapacity(data.Length);

            self.m_SendHandleData = IntPtr.Zero;

            self.m_Data.capacity = data.Length;
            self.m_Data.buffer = (byte*)data.GetUnsafePtr();
            self.m_Data.bitPos = 0;
            self.m_Data.failedWrites = 0;

            self.m_Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(data);
        }

        static short ByteSwap(short val)
        {
            return (short)(((val & 0xff) << 8) | ((val >> 8) & 0xff));
        }

        static int ByteSwap(int val)
        {
            return (int)(((val & 0xff) << 24) | ((val & 0xff00) << 8) | ((val >> 8) & 0xff00) | ((val >> 24) & 0xff));
        }

        /// <summary>
        /// True if there is a valid data buffer present. This would be false
        /// if the writer was created with no arguments.
        /// </summary>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Data.buffer != null; }
        }

        /// <summary>
        /// If there is a write failure this returns true.
        /// A failure might happen if an attempt is made to write more than there is capacity for.
        /// </summary>
        public readonly bool HasFailedWrites
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.failedWrites > 0;
        }

        /// <summary>
        /// The total size of the data buffer, see <see cref="Length"/> for
        /// the size of space used in the buffer.
        /// </summary>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return m_Data.capacity;
            }
        }

        /// <summary>
        /// The size of the buffer used. See <see cref="Capacity"/> for the total size.
        /// </summary>
        public int Length
        {
            get
            {
                CheckRead();
                return (m_Data.bitPos + 7) >> 3;
            }
        }
        /// <summary>
        /// The size of the buffer used in bits. See <see cref="Length"/> for the length in bytes.
        /// </summary>
        public int LengthInBits
        {
            get
            {
                CheckRead();
                return m_Data.bitPos;
            }
        }

        /// <summary>
        /// Advances the write position to the next byte boundary, writing zero bits into any padding.
        /// </summary>
        public void Flush()
        {
            WriteRawBits(0, (8 - (m_Data.bitPos & 7)) & 7);
        }

        bool WriteBytesInternal(byte* data, int bytes)
        {
            CheckWrite();

            if (Hint.Unlikely(((m_Data.bitPos + 7) >> 3) + bytes > m_Data.capacity))
            {
                ++m_Data.failedWrites;
                return false;
            }
            Flush();
            UnsafeUtility.MemCpy(m_Data.buffer + (m_Data.bitPos >> 3), data, bytes);
            m_Data.bitPos += bytes << 3;
            return true;
        }

        /// <summary>
        /// Writes an unsigned byte to the current stream and advances the stream position by one byte.
        /// </summary>
        /// <param name="value">The unsigned byte to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteByte(byte value)
        {
            return WriteBytesInternal((byte*)&value, sizeof(byte));
        }

        /// <summary>
        /// Copy NativeArray of bytes into the writer's data buffer.
        /// </summary>
        /// <param name="value">Source byte array</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteBytes(NativeArray<byte> value)
        {
            return WriteBytesInternal((byte*)value.GetUnsafeReadOnlyPtr(), value.Length);
        }

        /// <summary>
        /// Copy <c>Span</c> of bytes into the writer's data buffer.
        /// </summary>
        /// <param name="value">Source byte span</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteBytes(Span<byte> value)
        {
            fixed (byte* data = value)
            {
                return WriteBytesInternal(data, value.Length);
            }
        }

        /// <summary>
        /// Writes a 2-byte signed short to the current stream and advances the stream position by two bytes.
        /// </summary>
        /// <param name="value">The 2-byte signed short to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteShort(short value)
        {
            return WriteBytesInternal((byte*)&value, sizeof(short));
        }

        /// <summary>
        /// Writes a 2-byte unsigned short to the current stream and advances the stream position by two bytes.
        /// </summary>
        /// <param name="value">The 2-byte unsigned short to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteUShort(ushort value)
        {
            return WriteBytesInternal((byte*)&value, sizeof(ushort));
        }

        /// <summary>
        /// Writes a 4-byte signed integer from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <param name="value">The 4-byte signed integer to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteInt(int value)
        {
            return WriteBytesInternal((byte*)&value, sizeof(int));
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <param name="value">The 4-byte unsigned integer to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteUInt(uint value)
        {
            return WriteBytesInternal((byte*)&value, sizeof(uint));
        }

        /// <summary>
        /// Writes an 8-byte signed long from the stream and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <param name="value">The 8-byte signed long to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteLong(long value)
        {
            return WriteBytesInternal((byte*)&value, sizeof(long));
        }

        /// <summary>
        /// Reads an 8-byte unsigned long from the stream and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <param name="value">The 8-byte unsigned long to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteULong(ulong value)
        {
            return WriteBytesInternal((byte*)&value, sizeof(ulong));
        }

        /// <summary>
        /// Writes a 2-byte signed short to the current stream using Big-endian byte order and advances the stream position by two bytes.
        /// If the stream is in little-endian order, the byte order will be swapped.
        /// </summary>
        /// <param name="value">The 2-byte signed short to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteShortNetworkByteOrder(short value)
        {
            short netValue = IsLittleEndian ? ByteSwap(value) : value;
            return WriteBytesInternal((byte*)&netValue, sizeof(short));
        }


        /// <summary>
        /// Writes a 2-byte unsigned short to the current stream using Big-endian byte order and advances the stream position by two bytes.
        /// If the stream is in little-endian order, the byte order will be swapped.
        /// </summary>
        /// <param name="value">The 2-byte unsigned short to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteUShortNetworkByteOrder(ushort value)
        {
            return WriteShortNetworkByteOrder((short)value);
        }

        /// <summary>
        /// Writes a 4-byte signed integer from the current stream using Big-endian byte order and advances the current position of the stream by four bytes.
        /// If the current machine is in little-endian order, the byte order will be swapped.
        /// </summary>
        /// <param name="value">The 4-byte signed integer to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteIntNetworkByteOrder(int value)
        {
            int netValue = IsLittleEndian ? ByteSwap(value) : value;
            return WriteBytesInternal((byte*)&netValue, sizeof(int));
        }

        /// <summary>
        /// Writes a 4-byte unsigned integer from the current stream using Big-endian byte order and advances the current position of the stream by four bytes.
        /// If the stream is in little-endian order, the byte order will be swapped.
        /// </summary>
        /// <param name="value">The 4-byte unsigned integer to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteUIntNetworkByteOrder(uint value)
        {
            return WriteIntNetworkByteOrder((int)value);
        }

        /// <summary>
        /// Writes a 4-byte floating point value to the data stream.
        /// </summary>
        /// <param name="value">The 4-byte floating point value to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteFloat(float value)
        {
            UIntFloat uf = new UIntFloat();
            uf.floatValue = value;
            return WriteInt((int)uf.intValue);
        }

        /// <summary>
        /// Writes a 8-byte floating point value to the data stream.
        /// </summary>
        /// <param name="value">The 8-byte floating point value to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteDouble(double value)
        {
            UIntFloat uf = new UIntFloat();
            uf.doubleValue = value;
            return WriteLong((long)uf.longValue);
        }

        void WriteRawBitsInternal(uint value, int numbits)
        {
            CheckBits(value, numbits);
            WriteBitsInternal(value, numbits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void WriteBitsInternal(ulong value, int numbits)
        {
            CheckBits64(value, numbits);
            if (Hint.Unlikely(numbits == 0))
                return;

            int byteIndex = m_Data.bitPos >> 3;
            int bitOffset = m_Data.bitPos & 7;
            ulong eraseMask = ((1UL << numbits) - 1UL) << bitOffset;
            ulong shifted = value << bitOffset;

            if (Hint.Likely(byteIndex + 8 <= m_Data.capacity))
            {
                // Unaligned intrinsics rather than a *(ulong*) dereference: byteIndex is an arbitrary byte offset, and an
                // unaligned 64-bit access is undefined behavior on 32-bit ARM (armeabi-v7a) under IL2CPP, where it silently
                // corrupts data (UUM-138845). They compile to a single load/store on targets that allow unaligned access
                // (same idiom as xxHash3.Read64LE).
                byte* wordPtr = m_Data.buffer + byteIndex;
                ulong word = (Unsafe.ReadUnaligned<ulong>(wordPtr) & ~eraseMask) | shifted;
                Unsafe.WriteUnaligned(wordPtr, word);
            }
            else
            {
                int spannedBytes = (bitOffset + numbits + 7) >> 3;
                ulong current = 0;
                for (int i = 0; i < spannedBytes; ++i)
                    current |= (ulong)m_Data.buffer[byteIndex + i] << (i * 8);
                current = (current & ~eraseMask) | shifted;
                for (int i = 0; i < spannedBytes; ++i)
                    m_Data.buffer[byteIndex + i] = (byte)(current >> (i * 8));
            }

            m_Data.bitPos += numbits;
        }

        /// <summary>
        /// Appends a specified number of bits to the data stream.
        /// </summary>
        /// <param name="value">The bits to write.</param>
        /// <param name="numbits">A positive number of bytes to write.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WriteRawBits(uint value, int numbits)
        {
            CheckWrite();

            if (Hint.Unlikely(((m_Data.bitPos + numbits + 7) >> 3) > m_Data.capacity))
            {
                ++m_Data.failedWrites;
                return false;
            }
            WriteRawBitsInternal(value, numbits);
            return true;
        }

        /// <summary>
        /// Writes a 4-byte unsigned integer value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="value">The 4-byte unsigned integer to write.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedUInt(uint value, in StreamCompressionModel model)
        {
            CheckWrite();
            int bucket = model.CalculateBucket(value);
            ulong fused = model.bucketFused[bucket];
            uint offset = (uint)fused;
            int codeLen = (int)((fused >> 48) & 0xFF);
            int bits = (int)((fused >> 56) & 0xFF);

            if (Hint.Unlikely(((m_Data.bitPos + codeLen + bits + 7) >> 3) > m_Data.capacity))
            {
                ++m_Data.failedWrites;
                return false;
            }
            uint code = (uint)((fused >> 32) & 0xFFFF);
            WriteBitsInternal(code | ((ulong)(value - offset) << codeLen), codeLen + bits);
            return true;
        }

        /// <summary>
        /// Writes an 8-byte unsigned long value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="value">The 8-byte unsigned long to write.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedULong(ulong value, in StreamCompressionModel model)
        {
            var data = (uint*)&value;
            return WritePackedUInt(data[0], model) &
                   WritePackedUInt(data[1], model);
        }

        /// <summary>
        /// Writes a 4-byte signed integer value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// Negative values are interleaved between positive values, i.e. (0, -1, 1, -2, 2)
        /// </summary>
        /// <param name="value">The 4-byte signed integer to write.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedInt(int value, in StreamCompressionModel model)
        {
            uint interleaved = (uint)((value >> 31) ^ (value << 1));      // interleave negative values between positive values: 0, -1, 1, -2, 2
            return WritePackedUInt(interleaved, model);
        }

        /// <summary>
        /// Writes a 8-byte signed long value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="value">The 8-byte signed long to write.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedLong(long value, in StreamCompressionModel model)
        {
            ulong interleaved = (ulong)((value >> 63) ^ (value << 1));      // interleave negative values between positive values: 0, -1, 1, -2, 2
            return WritePackedULong(interleaved, model);
        }

        /// <summary>
        /// Writes a 4-byte floating point value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="value">The 4-byte floating point value to write.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedFloat(float value, in StreamCompressionModel model)
        {
            return WritePackedFloatDelta(value, 0, model);
        }

        /// <summary>
        /// Writes a 8-byte floating point value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="value">The 8-byte floating point value to write.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedDouble(double value, in StreamCompressionModel model)
        {
            return WritePackedDoubleDelta(value, 0, model);
        }

        /// <summary>
        /// Writes a delta 4-byte unsigned integer value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// Note that the Uint values are cast to an Int after computing the diff.
        /// </summary>
        /// <param name="value">The current 4-byte unsigned integer value.</param>
        /// <param name="baseline">The previous 4-byte unsigned integer value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedUIntDelta(uint value, uint baseline, in StreamCompressionModel model)
        {
            int diff = (int)(baseline - value);
            return WritePackedInt(diff, model);
        }

        /// <summary>
        /// Writes a delta 4-byte signed integer value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="value">The current 4-byte signed integer value.</param>
        /// <param name="baseline">The previous 4-byte signed integer value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedIntDelta(int value, int baseline, in StreamCompressionModel model)
        {
            int diff = (int)(baseline - value);
            return WritePackedInt(diff, model);
        }

        /// <summary>
        /// Writes a delta 8-byte signed long value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="value">The current 8-byte signed long value.</param>
        /// <param name="baseline">The previous 8-byte signed long value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedLongDelta(long value, long baseline, in StreamCompressionModel model)
        {
            long diff = (long)(baseline - value);
            return WritePackedLong(diff, model);
        }

        /// <summary>
        /// Writes a delta 8-byte unsigned long value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// Note that the unsigned long values are cast to a signed long after computing the diff.
        /// </summary>
        /// <param name="value">The current 8-byte unsigned long value.</param>
        /// <param name="baseline">The previous 8-byte unsigned long, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedULongDelta(ulong value, ulong baseline, in StreamCompressionModel model)
        {
            long diff = (long)(baseline - value);
            return WritePackedLong(diff, model);
        }

        /// <summary>
        /// Writes a 4-byte floating point value to the data stream.
        ///
        /// If the data did not change a zero bit is prepended, otherwise a 1 bit is prepended.
        /// When reading back the data, the first bit is then checked for whether the data was changed or not.
        /// </summary>
        /// <param name="value">The current 4-byte floating point value.</param>
        /// <param name="baseline">The previous 4-byte floating value, used to compute the diff.</param>
        /// <param name="model">Not currently used.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedFloatDelta(float value, float baseline, in StreamCompressionModel model)
        {
            CheckWrite();
            var bits = 0;
            if (value != baseline)
                bits = 32;
            if (Hint.Unlikely(((m_Data.bitPos + 1 + bits + 7) >> 3) > m_Data.capacity))
            {
                ++m_Data.failedWrites;
                return false;
            }
            if (bits == 0)
                WriteBitsInternal(0, 1);
            else
            {
                UIntFloat uf = new UIntFloat();
                uf.floatValue = value;
                WriteBitsInternal(1UL | ((ulong)uf.intValue << 1), 1 + bits);
            }
            return true;
        }

        /// <summary>
        /// Writes a 8-byte floating point value to the data stream.
        ///
        /// If the data did not change a zero bit is prepended, otherwise a 1 bit is prepended.
        /// When reading back the data, the first bit is then checked for whether the data was changed or not.
        /// </summary>
        /// <param name="value">The current 8-byte floating point value.</param>
        /// <param name="baseline">The previous 8-byte floating value, used to compute the diff.</param>
        /// <param name="model">Not currently used.</param>
        /// <returns>Whether the write was successful</returns>
        public bool WritePackedDoubleDelta(double value, double baseline, in StreamCompressionModel model)
        {
            CheckWrite();
            var bits = 0;
            if (value != baseline)
                bits = 64;
            if (Hint.Unlikely(((m_Data.bitPos + 1 + bits + 7) >> 3) > m_Data.capacity))
            {
                ++m_Data.failedWrites;
                return false;
            }
            if (bits == 0)
                WriteBitsInternal(0, 1);
            else
            {
                UIntFloat uf = new UIntFloat();
                uf.doubleValue = value;
                var data = (uint*)&uf.longValue;
                WriteBitsInternal(1UL | ((ulong)data[0] << 1), 33);
                WriteBitsInternal(data[1], 32);
            }
            return true;
        }


        /// <summary>
        /// Writes a <c>FixedString32Bytes</c> value to the data stream.
        /// </summary>
        /// <param name="str">The <c>FixedString32Bytes</c> to write.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WriteFixedString32(FixedString32Bytes str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytesInternal(data, length);
        }

        /// <summary>
        /// Writes a <c>FixedString64Bytes</c> value to the data stream.
        /// </summary>
        /// <param name="str">The <c>FixedString64Bytes</c> to write.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WriteFixedString64(FixedString64Bytes str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytesInternal(data, length);
        }

        /// <summary>
        /// Writes a <c>FixedString128Bytes</c> value to the data stream.
        /// </summary>
        /// <param name="str">The <c>FixedString128Bytes</c> to write.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WriteFixedString128(FixedString128Bytes str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytesInternal(data, length);
        }

        /// <summary>
        /// Writes a <c>FixedString512Bytes</c> value to the data stream.
        /// </summary>
        /// <param name="str">The <c>FixedString512Bytes</c> to write.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WriteFixedString512(FixedString512Bytes str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytesInternal(data, length);
        }

        /// <summary>
        /// Writes a <c>FixedString4096Bytes</c> value to the data stream.
        /// </summary>
        /// <param name="str">The <c>FixedString4096Bytes</c> to write.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WriteFixedString4096(FixedString4096Bytes str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytesInternal(data, length);
        }

        /// <summary>
        /// Writes a <c>FixedString32Bytes</c> delta value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="str">The current <c>FixedString32Bytes</c> value.</param>
        /// <param name="baseline">The previous <c>FixedString32Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WritePackedFixedString32Delta(FixedString32Bytes str, FixedString32Bytes baseline, in StreamCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }

        /// <summary>
        /// Writes a delta <c>FixedString64Bytes</c> value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="str">The current <c>FixedString64Bytes</c> value.</param>
        /// <param name="baseline">The previous <c>FixedString64Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WritePackedFixedString64Delta(FixedString64Bytes str, FixedString64Bytes baseline, in StreamCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }

        /// <summary>
        /// Writes a delta <c>FixedString128Bytes</c> value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="str">The current <c>FixedString128Bytes</c> value.</param>
        /// <param name="baseline">The previous <c>FixedString128Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WritePackedFixedString128Delta(FixedString128Bytes str, FixedString128Bytes baseline, in StreamCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }

        /// <summary>
        /// Writes a delta <c>FixedString512Bytes</c> value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="str">The current <c>FixedString512Bytes</c> value.</param>
        /// <param name="baseline">The previous <c>FixedString512Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WritePackedFixedString512Delta(FixedString512Bytes str, FixedString512Bytes baseline, in StreamCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }

        /// <summary>
        /// Writes a delta <c>FixedString4096Bytes</c> value to the data stream using a <see cref="StreamCompressionModel"/>.
        /// </summary>
        /// <param name="str">The current <c>FixedString4096Bytes</c> value.</param>
        /// <param name="baseline">The previous <c>FixedString4096Bytes</c> value, used to compute the diff.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public unsafe bool WritePackedFixedString4096Delta(FixedString4096Bytes str, FixedString4096Bytes baseline, in StreamCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }

        /// <summary>
        /// Writes a delta FixedString value to the data stream using a <see cref="StreamCompressionModel"/>.
        ///
        /// If the value cannot be written <see cref="HasFailedWrites"/> will return true. This state can be cleared by
        /// calling <see cref="Clear"/>.
        /// </summary>
        /// <param name="data">Pointer to a packed fixed string.</param>
        /// <param name="length">The length of the new value.</param>
        /// <param name="baseData">The previous value, used to compute the diff.</param>
        /// <param name="baseLength">The length of the previous value.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        unsafe bool WritePackedFixedStringDelta(byte* data, uint length, byte* baseData, uint baseLength, in StreamCompressionModel model)
        {
            var oldData = m_Data;
            if (!WritePackedUIntDelta(length, baseLength, model))
                return false;
            bool didFailWrite = false;
            if (length <= baseLength)
            {
                for (uint i = 0; i < length; ++i)
                    didFailWrite |= !WritePackedUIntDelta(data[i], baseData[i], model);
            }
            else
            {
                for (uint i = 0; i < baseLength; ++i)
                    didFailWrite |= !WritePackedUIntDelta(data[i], baseData[i], model);
                for (uint i = baseLength; i < length; ++i)
                    didFailWrite |= !WritePackedUInt(data[i], model);
            }
            // Rewind; bits are already committed, so scrub the stale high bits in the shared partial byte.
            if (Hint.Unlikely(didFailWrite))
            {
                m_Data = oldData;
                int bitOffset = m_Data.bitPos & 7;
                if (bitOffset != 0)
                    m_Data.buffer[m_Data.bitPos >> 3] &= (byte)((1u << bitOffset) - 1);
                ++m_Data.failedWrites;
            }
            return !didFailWrite;
        }

        /// <summary>
        /// Moves the write position to the start of the data buffer used.
        /// </summary>
        public void Clear()
        {
            m_Data.bitPos = 0;
            m_Data.failedWrites = 0;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CheckRead()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckWrite()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckAllocator(AllocatorManager.AllocatorHandle allocator)
        {
            if (allocator.ToAllocator != Allocator.Temp)
                throw new InvalidOperationException("DataStreamWriters can only be created with temp memory");
        }

        // The largest guard adds a packed-double header (1 + 64 + 7 bits) to bitPos before its range check; cap capacity so neither the bit position nor that arithmetic can overflow a signed int.
        const int k_MaxCapacity = (int.MaxValue - 72) >> 3;

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
                throw new NotSupportedException("DataStreamWriter packs bits assuming a little-endian host; only the NetworkByteOrder methods are endian-independent.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckBits(uint value, int numBits)
        {
            if (numBits < 0 || numBits > 32)
                throw new ArgumentOutOfRangeException($"Invalid number of bits specified: {numBits}! Valid range is (0, 32) inclusive.");
            var errValue = (1UL << numBits);
            if (value >= errValue)
                throw new ArgumentOutOfRangeException($"Value {value} does not fit in the specified number of bits: {numBits}! Range (inclusive) is (0, {errValue-1})!");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckBits64(ulong value, int numBits)
        {
            if (numBits < 0 || numBits > 56)
                throw new ArgumentOutOfRangeException($"Invalid number of bits specified: {numBits}! Valid range is (0, 56) inclusive.");
            if (value >= (1UL << numBits))
                throw new ArgumentOutOfRangeException($"Value {value} does not fit in the specified number of bits: {numBits}!");
        }
    }
}
