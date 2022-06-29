// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Unsafe/UTF8StringView.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct UTF8StringView
    {
        public readonly IntPtr utf8Ptr;
        public readonly int utf8Length;

        public UTF8StringView(ReadOnlySpan<byte> prefixUtf8Span)
        {
            unsafe
            {
                // Span<T> is ref struct, it is not movable by definition, so it is safe to use fixed here
                // The fact that C# asks to use fixed is discussed here: https://github.com/dotnet/csharplang/issues/1792
                fixed (byte* ptr = &prefixUtf8Span[0])
                    utf8Ptr = new IntPtr(ptr);
            }
            utf8Length = prefixUtf8Span.Length;
        }

        public UTF8StringView(IntPtr ptr, int lengthUtf8)
        {
            utf8Ptr = ptr;
            utf8Length = lengthUtf8;
        }

        public unsafe UTF8StringView(byte* ptr, int lengthUtf8)
        {
            utf8Ptr = new IntPtr(ptr);
            utf8Length = lengthUtf8;
        }

        public override string ToString()
        {
            unsafe
            {
                return Encoding.UTF8.GetString((byte*)utf8Ptr.ToPointer(), utf8Length);
            }
        }
    }
}
