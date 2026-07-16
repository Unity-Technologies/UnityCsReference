// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Unity.Burst.Intrinsics.Arm.Neon;
using static Unity.Burst.Intrinsics.X86.F16C;

namespace Unity.Burst.Intrinsics
{
    /// <summary>
    /// Represents a 16-bit floating point value (half precision)
    /// Warning: this type may not be natively supported by your hardware, or its usage may be suboptimal
    /// </summary>
    public readonly struct f16 : System.IEquatable<f16>
    {
        /// <summary>
        /// The container for the actual 16-bit half precision floating point value
        /// </summary>
        private readonly ushort value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint f32tof16(float x)
        {
            if (IsF16CSupported)
            {
                var v = new v128();
                v.Float0 = x;
                var result = cvtps_ph(v, (int)X86.RoundingMode.FROUND_TRUNC_NOEXC);
                return result.UShort0;
            }
            else if (IsNeonHalfFPSupported)
            {
                var v = new v128();
                v.Float0 = x;
                var result = vcvt_f16_f32(v);
                return result.UShort0;
            }
            // Managed fallback
            const int infinity_32 = 255 << 23;
            const uint msk = 0x7FFFF000u;

            uint ux = asuint(x);
            uint uux = ux & msk;
            uint h = (uint)(asuint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >> 13;   // Clamp to signed infinity if overflowed
            h = select(h,
                select(0x7c00u, 0x7e00u, (int)uux > infinity_32),
                (int)uux >= infinity_32);   // NaN->qNaN and Inf->Inf
            return h | (ux & ~msk) >> 16;
        }

        /// <summary>Returns the bit pattern of a float as a uint.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint asuint(float x) { return (uint)asint(x); }

        /// <summary>Returns the minimum of two float values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float min(float x, float y) { return float.IsNaN(y) || x < y ? x : y; }

        /// <summary>Returns b if c is true, a otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint select(uint a, uint b, bool c) { return c ? b : a; }

        /// <summary>Returns the bit pattern of a uint as a float.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float asfloat(uint x) { return asfloat((int)x); }

        [StructLayout(LayoutKind.Explicit)]
        private struct IntFloatUnion
        {
            [FieldOffset(0)]
            public int intValue;
            [FieldOffset(0)]
            public float floatValue;
        }

        /// <summary>Returns the bit pattern of an int as a float.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float asfloat(int x)
        {
            IntFloatUnion u;
            u.floatValue = 0;
            u.intValue = x;

            return u.floatValue;
        }

        /// <summary>Returns the bit pattern of a float as an int.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int asint(float x)
        {
            IntFloatUnion u;
            u.intValue = 0;
            u.floatValue = x;
            return u.intValue;
        }

        /// <summary>Constructs a half value from a half value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public f16(f16 x)
        {
            value = x.value;
        }

        /// <summary>Constructs a half value from a float value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public f16(float v)
        {
            value = (ushort)f32tof16(v);
        }

        /// <summary>Returns whether two f16 values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(f16 lhs, f16 rhs)
        {
            return lhs.value == rhs.value;
        }

        /// <summary>Returns whether two f16 values are different.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(f16 lhs, f16 rhs)
        {
            return lhs.value != rhs.value;
        }

        /// <summary>Returns true if the f16 is equal to a given f16, false otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(f16 rhs)
        {
            return value == rhs.value;
        }

        /// <summary>Returns true if the half is equal to a given half, false otherwise.</summary>
        public override bool Equals(object o) { return Equals((f16)o); }

        /// <summary>Returns a hash code for the half.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return (int)value; }

    }
}
