// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine
{
    internal static class ManagedStreamHelpers
    {
        internal static void ValidateLoadFromStream(System.IO.Stream stream)
        {
            if (stream == null)
                throw new System.ArgumentNullException("ManagedStream object must be non-null", "stream");
            if (!stream.CanRead)
                throw new System.ArgumentException("ManagedStream object must be readable (stream.CanRead must return true)", "stream");
            if (!stream.CanSeek)
                throw new System.ArgumentException("ManagedStream object must be seekable (stream.CanSeek must return true)", "stream");
        }

        [RequiredByNativeCode]
        unsafe internal static void ManagedStreamRead(byte[] buffer, int offset, int count, System.IO.Stream stream, IntPtr returnValueAddress)
        {
            if (returnValueAddress == IntPtr.Zero)
                throw new ArgumentException("Return value address cannot be 0.", "returnValueAddress");
            ValidateLoadFromStream(stream);
            (*(int*)returnValueAddress) = stream.Read(buffer, offset, count);
        }

        [RequiredByNativeCode]
        unsafe internal static void ManagedStreamSeek(long offset, uint origin, System.IO.Stream stream, IntPtr returnValueAddress)
        {
            if (returnValueAddress == IntPtr.Zero)
                throw new ArgumentException("Return value address cannot be 0.", "returnValueAddress");
            ValidateLoadFromStream(stream);
            (*(long*)returnValueAddress) = stream.Seek(offset, (System.IO.SeekOrigin)origin);
        }

        [RequiredByNativeCode]
        unsafe internal static void ManagedStreamLength(System.IO.Stream stream, IntPtr returnValueAddress)
        {
            if (returnValueAddress == IntPtr.Zero)
                throw new ArgumentException("Return value address cannot be 0.", "returnValueAddress");
            ValidateLoadFromStream(stream);
            (*(long*)returnValueAddress) = stream.Length;
        }
    }
}
