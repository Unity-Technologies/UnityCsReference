// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Extension methods for DataStream.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static class DataStreamExtensions
    {
        /// <summary>
        /// Initializes a new instance of the DataStreamWriter struct with externally owned memory
        /// </summary>
        /// <param name="data">Pointer to the data</param>
        /// <param name="length">Length of the data</param>
        /// <returns>A new instance of the <see cref="DataStreamWriter"/></returns>
        public static unsafe DataStreamWriter Create(byte* data, int length)
        {
            var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, length, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref na, AtomicSafetyHandle.GetTempMemoryHandle());
            return new DataStreamWriter(na);
        }

        /// <summary>
        /// Appends a specified number of bytes from the buffer to the data stream.
        /// </summary>
        /// <param name="writer">Data stream writer.</param>
        /// <param name="data">Pointer to the data.</param>
        /// <param name="bytes">A positive number of bytes to write.</param>
        /// <returns>Whether the write was successful</returns>
        public static unsafe bool WriteBytesUnsafe(this ref DataStreamWriter writer, byte* data, int bytes)
        {
            var dataArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, bytes, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref dataArray, AtomicSafetyHandle.GetTempMemoryHandle());
            return writer.WriteBytes(dataArray);
        }

        /// <summary>
        /// Read and copy data to the memory location pointed to, an error will
        /// be logged if the <paramref name="length"/> will put the reader out of bounds on the current read pointer.
        /// </summary>
        /// <param name="reader">Data stream reader.</param>
        /// <param name="data">Pointer to the data.</param>
        /// <param name="length">Number of bytes to read.</param>
        public static unsafe void ReadBytesUnsafe(this ref DataStreamReader reader, byte* data, int length)
        {
            var dataArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, length, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref dataArray, AtomicSafetyHandle.GetTempMemoryHandle());
            reader.ReadBytes(dataArray);
        }


        /// <summary>
        /// Reads a 2-byte length value from the current stream, reads the specified number of bytes
        /// to the buffer and advances the current position of the stream by the length of the string.
        /// </summary>
        /// <param name="reader">Data stream reader.</param>
        /// <param name="data">Buffer to write the string bytes to.</param>
        /// <param name="maxLength">Max number of bytes allowed to be read into the buffer.</param>
        /// <returns>The number of bytes written to the data buffer.</returns>
        public static unsafe ushort ReadFixedStringUnsafe(this ref DataStreamReader reader, byte* data, int maxLength)
        {
            var dataArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, maxLength, Allocator.Temp);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref dataArray, AtomicSafetyHandle.GetTempMemoryHandle());
            return reader.ReadFixedString(dataArray);
        }

        /// <summary>
        /// Writes a delta FixedString value to the data stream using a <see cref="StreamCompressionModel"/>.
        ///
        /// If the value cannot be read <see cref="DataStreamReader.HasFailedReads"/> will return true.
        /// </summary>
        /// <param name="reader">Data stream reader.</param>
        /// <param name="data">Pointer to a packed fixed string.</param>
        /// <param name="maxLength">Max number of bytes allowed to be read into the pointer.</param>
        /// <param name="baseData">Pointer to the previous value, used to compute the diff.</param>
        /// <param name="baseLength">The length of the previous value.</param>
        /// <param name="model"><see cref="StreamCompressionModel"/> model for writing value in a packed manner.</param>
        /// <returns>Whether the write was successful</returns>
        public static unsafe ushort ReadPackedFixedStringDeltaUnsafe(this ref DataStreamReader reader, byte* data, int maxLength, byte* baseData, ushort baseLength, StreamCompressionModel model)
        {
            var current = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, maxLength, Allocator.Temp);
            var baseline = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(baseData, baseLength, Allocator.Temp);
            var safetyHandle = AtomicSafetyHandle.GetTempMemoryHandle();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref current, safetyHandle);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref baseline, safetyHandle);
            return reader.ReadPackedFixedStringDelta(current, baseline, model);
        }

        /// <summary>
        /// Get a pointer to the stream's data. Note that the pointer always points at the
        /// beginning of the data, no matter how much was read from the stream.
        /// </summary>
        /// <remarks>Performs a job safety check for read-only access.</remarks>
        /// <param name="reader">Data stream reader.</param>
        /// <returns>A pointer to the stream's data.</returns>
        public static unsafe void* GetUnsafeReadOnlyPtr(this ref DataStreamReader reader)
        {
            reader.CheckRead();
            return reader.m_BufferPtr;
        }
    }
}
