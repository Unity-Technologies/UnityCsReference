using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ConstUTF8String
{
    internal ConstUTF8String(ConstAccessor accessor)
    {
        Field = default;

        Schema schema = accessor.GetSchema();
        if (schema.IsValid() && schema.IsUTF8String())
        {
            Field = accessor.Data;
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
        fixed (ConstUTF8String* stringAccessorPtr = &this)
        {
            return UdmInterop.Instance.udm_const_utf8string_string_length(stringAccessorPtr);
        }
    }

    public override string ToString()
    {
        ThrowIfInvalid();
        return Encoding.UTF8.GetString(AsSpanInternalUnsafe());
    }

    internal ReadOnlySpan<byte> AsSpan()
    {
        ThrowIfInvalid();
        return AsSpanInternalUnsafe();
    }

    private ReadOnlySpan<byte> AsSpanInternalUnsafe()
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
}
}
