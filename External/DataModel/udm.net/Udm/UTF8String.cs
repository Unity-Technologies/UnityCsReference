using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System;

namespace Unity.DataModel
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UTF8StringField
    {
        internal ulong Size;
        internal long Location;

        internal unsafe Span<byte> AsSpan()
        {
            byte* dataPtr = UnsafeHelper.AsBytePointer(ref this) + Location;
            return new Span<byte>(dataPtr, (int)Size);
        }

        internal readonly unsafe ReadOnlySpan<byte> AsReadOnlySpan()
        {
            byte* dataPtr = UnsafeHelper.AsBytePointerFromReadOnly(in this)+ Location;
            return new ReadOnlySpan<byte>(dataPtr, (int)Size);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct UTF8String
    {
        internal UTF8String(Accessor accessor)
        {
            Field = default;
            DocumentModel = default;

            Schema schema = accessor.GetSchema();
            if (schema.IsValid() && schema.IsUTF8String())
            {
                Field = accessor.Data;
                DocumentModel = accessor.DocumentModel;
            }
        }

        internal ulong GetByteCount()
        {
            ThrowIfInvalid();
            return GetByteCountInternalUnsafe();
        }

        private ulong GetByteCountInternalUnsafe()
        {
            return ((UTF8StringField*)Field)->Size;
        }

        internal ulong GetStringLength()
        {
            ThrowIfInvalid();
            return GetStringLengthInternalUnsafe();
        }

        private ulong GetStringLengthInternalUnsafe()
        {
            fixed (UTF8String* stringAccessorPtr = &this)
            {
                return UdmInterop.Instance.udm_utf8string_string_length(stringAccessorPtr);
            }
        }

        internal void Set(string value)
        {
            ThrowIfInvalid();
            unsafe
            {
                fixed (UTF8String* stringAccessorPtr = &this)
                {
                    // Calculate the amount of bytes needed for the encoding
                    int byteCount = Encoding.UTF8.GetByteCount(value);
                    UdmInterop.Instance.udm_utf8string_replace_uninitialized(stringAccessorPtr, (ulong)(byteCount));
                    Encoding.UTF8.GetBytes(value.AsSpan(), AsSpan());
                }
            }
        }

        internal void Set(ConstUTF8String value)
        {
            ThrowIfInvalid();
            var span = value.AsSpan();
            var dataPtr = (byte*)UnsafeHelper.AsPointer(ref MemoryMarshal.GetReference(span));
            fixed (UTF8String* stringAccessorPtr = &this)
            {
                UdmInterop.Instance.udm_utf8string_assign(stringAccessorPtr, dataPtr, (ulong)span.Length);
            }
        }

        internal void SetBytes(byte[] value)
        {
            ThrowIfInvalid();

            fixed (byte* memoryPtr = value)
            {
                fixed (UTF8String* stringAccessorPtr = &this)
                {
                    UdmInterop.Instance.udm_utf8string_assign(stringAccessorPtr, memoryPtr, (ulong)value.Length);
                }
            }
        }

        internal void Append(string value)
        {
            ThrowIfInvalid();
            unsafe
            {
                int byteCount = Encoding.UTF8.GetByteCount(value);
                fixed (UTF8String* stringAccessorPtr = &this)
                {
                    var originalSize = (int)GetByteCountInternalUnsafe();
                    UdmInterop.Instance.udm_utf8string_append_uninitialized(stringAccessorPtr, (ulong)byteCount);
                    var span = AsSpan().Slice(originalSize, byteCount);
                    Encoding.UTF8.GetBytes(value.AsSpan(), span);
                }
            }
        }

        internal void Clear()
        {
            ThrowIfInvalid();
            fixed (UTF8String* stringAccessorPtr = &this)
            {
                UdmInterop.Instance.udm_utf8string_clear(stringAccessorPtr);
            }
        }

        internal void Reserve(ulong capacity)
        {
            ThrowIfInvalid();
            fixed (UTF8String* stringAccessorPtr = &this)
            {
                UdmInterop.Instance.udm_utf8string_reserve(stringAccessorPtr, capacity);
            }
        }

        public override string ToString()
        {
            ThrowIfInvalid();
            return Encoding.UTF8.GetString(AsReadOnlySpanInternalUnsafe());
        }

        internal Span<byte> AsSpan()
        {
            ThrowIfInvalid();
            return AsSpanInternalUnsafe();
        }

        private Span<byte> AsSpanInternalUnsafe()
        {
            return ((UTF8StringField*)Field)->AsSpan();
        }

        internal ReadOnlySpan<byte> AsReadOnlySpan()
        {
            ThrowIfInvalid();
            return AsReadOnlySpanInternalUnsafe();
        }

        private ReadOnlySpan<byte> AsReadOnlySpanInternalUnsafe()
        {
            return ((UTF8StringField*)Field)->AsReadOnlySpan();
        }

        internal bool IsValid() => Field != IntPtr.Zero;

        internal void ThrowIfInvalid()
        {
            if (!IsValid())
                throw new InvalidOperationException("Trying to use an invalid UTF8String");
        }

        // Pointers to blittable types are not considered blittable by the bindings generator
        // internal UTF8StringField* Field;
        internal IntPtr Field;
        internal DocumentModel DocumentModel;
    }
}
