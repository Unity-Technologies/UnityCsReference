// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Collections.LowLevel.Unsafe.NotBurstCompatible
{
    /// <summary>
    /// Provides some extension methods for various collections.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns a new managed array with all the elements copied from a set.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="set">The set whose elements are copied to the array.</param>
        /// <returns>A new managed array with all the elements copied from a set.</returns>
        public static T[] ToArray<T>(this UnsafeParallelHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            var array = set.ToNativeArray(Allocator.TempJob);
            var managed = array.ToArray();
            array.Dispose();
            return managed;
        }

        /// <summary>
        /// Adds the content of a string to this append buffer.
        /// </summary>
        /// <remarks>The length of the string is written as an int to the buffer before the characters are written.</remarks>
        /// <param name="buffer">The buffer to which to add the string.</param>
        /// <param name="value">The string to copy.</param>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static unsafe void AddNBC(ref this UnsafeAppendBuffer buffer, string value)
        {
            if (value != null)
            {
                buffer.Add(value.Length);
                fixed (char* ptr = value)
                {
                    buffer.Add(ptr, sizeof(char) * value.Length);
                }
            }
            else
            {
                buffer.Add(-1);
            }
        }

        /// <summary>
        /// Returns an unmanaged byte array with a copy of this buffer's contents.
        /// </summary>
        /// <param name="buffer">This buffer.</param>
        /// <returns>An unmanaged byte array with a copy of this buffer's contents.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed array")]
        public static unsafe byte[] ToBytesNBC(ref this UnsafeAppendBuffer buffer)
        {
            var dst = new byte[buffer.Length];
            fixed (byte* dstPtr = dst)
            {
                UnsafeUtility.MemCpy(dstPtr, buffer.Ptr, buffer.Length);
            }
            return dst;
        }

        /// <summary>
        /// Reads a string from this buffer reader.
        /// </summary>
        /// <param name="value">Outputs the string.</param>
        /// <param name="reader">This reader.</param>
        [ExcludeFromBurstCompatTesting("Managed string out argument")]
        public static unsafe void ReadNextNBC(ref this UnsafeAppendBuffer.Reader reader, out string value)
        {
            int length;
            reader.ReadNext(out length);

            if (length != -1)
            {
                value = new string('0', length);

                fixed (char* buf = value)
                {
                    int bufLen = length * sizeof(char);
                    UnsafeUtility.MemCpy(buf, reader.ReadNext(bufLen), bufLen);
                }
            }
            else
            {
                value = null;
            }
        }
    }
}
