// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Animations
{
    // An avatar mask in a versioned byte format, viewing bytes owned by the caller.
    [NativeHeader("Modules/Animation/ScriptBindings/AvatarMaskConstant.bindings.h")]
    [StaticAccessor("AvatarMaskConstantBindings", StaticAccessorType.DoubleColon)]
    internal readonly ref struct AvatarMaskConstant
    {
        readonly ReadOnlySpan<byte> m_Bytes;

        AvatarMaskConstant(ReadOnlySpan<byte> bytes)
        {
            m_Bytes = bytes;
        }

        public ReadOnlySpan<byte> Bytes => m_Bytes;

        public static AvatarMaskConstant FromBytes(ReadOnlySpan<byte> source)
        {
            if (!IsValid(source))
                throw new ArgumentException("The bytes are not a valid AvatarMaskConstant.", nameof(source));

            return new AvatarMaskConstant(source);
        }

        internal static AvatarMaskConstant FromValidatedBytes(ReadOnlySpan<byte> source)
        {
            return new AvatarMaskConstant(source);
        }

        // Throws InvalidOperationException when the mask has more transforms than the format allows.
        public static int GetByteCount(AvatarMask mask)
        {
            return GetByteCountFromAvatarMaskInternal(mask);
        }

        // Writes mask into destination; returns false when destination is smaller than GetByteCount.
        public static bool TryFromAvatarMask(AvatarMask mask, Span<byte> destination, out AvatarMaskConstant constant)
        {
            int bytesWritten;
            unsafe
            {
                fixed (byte* ptr = destination)
                    bytesWritten = WriteFromAvatarMaskInternal(mask, ptr, destination.Length);
            }

            if (bytesWritten == 0)
            {
                constant = default;
                return false;
            }

            constant = new AvatarMaskConstant(destination.Slice(0, bytesWritten));
            return true;
        }

        static bool IsValid(ReadOnlySpan<byte> bytes)
        {
            unsafe
            {
                fixed (byte* ptr = bytes)
                    return IsValidInternal(ptr, bytes.Length);
            }
        }

        extern private static unsafe bool IsValidInternal(byte* source, int size);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetByteCountFromAvatarMaskInternal([NotNull] AvatarMask mask);

        [NativeMethod(ThrowsException = true)]
        extern private static unsafe int WriteFromAvatarMaskInternal([NotNull] AvatarMask mask, byte* destination, int capacity);
    }
}
