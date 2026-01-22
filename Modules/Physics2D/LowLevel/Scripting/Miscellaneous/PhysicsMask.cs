// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A 64-bit mask, effectively 64 flags. The default enumerator will iterate all the bits that are set (1).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsMask : IEnumerable<int>
    {
        /// <summary>
        /// Create a PhysicsMask by specifying multiple bits to set (1).
        /// </summary>
        /// <param name="bitIndicies">The indices of the bits to set in the mask. An index must be in the range [0, 63].</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if any of the provided bits are out of the range [0, 63].</exception>
        public PhysicsMask(params int[] bitIndicies)
        {
            UInt64 mask = 0;

            foreach (var bitIndex in bitIndicies)
            {
                if (bitIndex >= 0 && bitIndex <= 63)
                {
                    mask |= (UInt64)1 << bitIndex;
                    continue;
                }

                // Invalid.
                throw new ArgumentOutOfRangeException(nameof(bitIndex), $"Bit index is out of range [0, 63]: {bitIndex}.");
            }

            bitMask = mask;
        }

        /// <summary>
        /// Create a PhysicsMask from a LayerMask.
        /// A <see cref="UnityEngine.LayerMask"/> is only 32-bits wide so the PhysicsMask will have the upper 32-bits set to zero.
        /// </summary>
        /// <param name="layerMask">The LayerMask to use.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PhysicsMask(LayerMask layerMask)
        {
            bitMask = (UInt64)layerMask.value;
        }

        /// <summary>
        /// A 64-bit mask, effectively 64 flags.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public System.UInt64 bitMask;

        /// <summary>
        /// Convert the lower 32-bits of the 64-bit mask to the 32-bit <see cref="UnityEngine.LayerMask"/>.
        /// A <see cref="UnityEngine.LayerMask"/> is only 32-bits wide so the upper 32-bits of the PhysicsMask will be ignored.
        /// </summary>
        /// <returns>A 32-bit layer-mask converted from the lower 32-bits of the 64-bit mask.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly LayerMask ToLayerMask()
        {
            return (LayerMask)(int)(UInt32)bitMask;
        }

        /// <summary>
        /// Set (1) the specified bit.
        /// </summary>
        /// <param name="bitIndex">The bit index in the range [0, 63].</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if <paramref name="bitIndex"/> is out of range [0, 63].</exception>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetBit(int bitIndex)
        {
            if (bitIndex >= 0 && bitIndex <= 63)
            {
                bitMask |= ((UInt64)1 << bitIndex);
                return;
            }

            // Invalid.
            throw new ArgumentOutOfRangeException(nameof(bitIndex), $"Bit index is out of range (0 to 63): {bitIndex}.");
        }

        /// <summary>
        /// Reset (0) the specified bit.
        /// </summary>
        /// <param name="bitIndex">The bit index in the range [0, 63].</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if <paramref name="bitIndex"/> is out of range [0, 63].</exception>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void ResetBit(int bitIndex)
        {
            if (bitIndex >= 0 && bitIndex <= 63)
            {
                bitMask &= ~((UInt64)1 << bitIndex);
                return;
            }

            // Invalid.
            throw new ArgumentOutOfRangeException(nameof(bitIndex), $"Bit index is out of range [0, 63]: {bitIndex}.");
        }

        /// <summary>
        /// Is the specified bit set.
        /// </summary>
        /// <param name="bitIndex">The bit index in the range [0, 63].</param>
        /// <returns>Whether the specified bit is set or not.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if <paramref name="bitIndex"/> is out of range [0, 63].</exception>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool IsBitSet(int bitIndex)
        {
            if (bitIndex >= 0 && bitIndex <= 63)
                return (bitMask & ((UInt64)1 << bitIndex)) != 0;

            // Invalid.
            throw new ArgumentOutOfRangeException(nameof(bitIndex), $"Bit index is out of range [0, 63]: {bitIndex}.");
        }

        /// <summary>
        /// Checks if the provided PhysicsMask set bits are also set in this PhysicsMask.
        /// </summary>
        /// <param name="physicsMask">The PhysicsMask bits to compare to this PhysicsMask. If this is zero, false will always be returned.</param>
        /// <returns>True if any set bits in the specified PhysicsMask are also set in this PhysicsMask, false otherwise.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool AreBitsSet(PhysicsMask physicsMask) => (bitMask & physicsMask) == physicsMask;

        /// <summary>
        /// Gets an enumerable group of bits that are currently reset (0).
        /// The bits are returned in ascending bit-index order.
        /// This uses <see cref="LowLevelPhysics2D.PhysicsMask.ResetBitIterator"/>.
        /// </summary>
        public readonly ResetBitIterator resetBits => new(this);

        /// <summary>
        /// Gets an enumerable group of bits that are currently set (1).
        /// The bits are returned in ascending bit-index order.
        /// This uses <see cref="LowLevelPhysics2D.PhysicsMask.SetBitIterator"/>.
        /// </summary>
        public readonly SetBitIterator setBits => new(this);

        /// <undoc/>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator PhysicsMask(System.UInt64 value)
        {
            return new PhysicsMask { bitMask = value };
        }

        /// <undoc/>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator System.UInt64(PhysicsMask bitMask)
        {
            return bitMask.bitMask;
        }

        /// <summary>
        /// Bitwise OR operator for PhysicsMask.
        /// </summary>
        /// <param name="bitMaskA">The first PhysicsMask to perform the operation with.</param>
        /// <param name="bitMaskB">The second PhysicsMask to perform the operation with.</param>
        /// <returns>The bitwise operation using both BitMasks.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static PhysicsMask operator |(PhysicsMask bitMaskA, PhysicsMask bitMaskB)
        {
            return bitMaskA.bitMask | bitMaskB.bitMask;
        }

        /// <summary>
        /// Bitwise AND operator for PhysicsMask.
        /// </summary>
        /// <param name="bitMaskA">The first PhysicsMask to perform the operation with.</param>
        /// <param name="bitMaskB">The second PhysicsMask to perform the operation with.</param>
        /// <returns>The bitwise operation using both BitMasks.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static PhysicsMask operator &(PhysicsMask bitMaskA, PhysicsMask bitMaskB)
        {
            return bitMaskA.bitMask & bitMaskB.bitMask;
        }

        /// <summary>
        /// Bitwise XOR operator for PhysicsMask.
        /// </summary>
        /// <param name="bitMaskA">The first PhysicsMask to perform the operation with.</param>
        /// <param name="bitMaskB">The second PhysicsMask to perform the operation with.</param>
        /// <returns>The bitwise operation using both BitMasks.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static PhysicsMask operator ^(PhysicsMask bitMaskA, PhysicsMask bitMaskB)
        {
            return bitMaskA.bitMask ^ bitMaskB.bitMask;
        }

        /// <summary>
        /// Bitwise COMPLEMENT operator for PhysicsMask.
        /// </summary>
        /// <param name="bitMask">The PhysicsMask to perform the operation with.</param>
        /// <returns>The bitwise operation using both BitMasks.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static PhysicsMask operator ~(PhysicsMask bitMask)
        {
            return ~bitMask.bitMask;
        }

        /// <summary>
        /// Bitwise LEFT-SHIFT operator for PhysicsMask.
        /// </summary>
        /// <param name="bitMask">The PhysicsMask to perform the operation with.</param>
        /// <param name="bitShift">The number of bits to shift the bitmask.</param>
        /// <returns>The bitwise operation using both BitMasks.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static PhysicsMask operator <<(PhysicsMask bitMask, int bitShift)
        {
            return bitMask.bitMask << bitShift;
        }

        /// <summary>
        /// Bitwise RIGHT-SHIFT operator for PhysicsMask.
        /// </summary>
        /// <param name="bitMask">The PhysicsMask to perform the operation with.</param>
        /// <param name="bitShift">The number of bits to shift the bitmask.</param>
        /// <returns>The bitwise operation using both BitMasks.</returns>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static PhysicsMask operator >>(PhysicsMask bitMask, int bitShift)
        {
            return bitMask.bitMask >> bitShift;
        }

        /// <summary>
        /// No bits set in the PhysicsMask, effectively zero.
        /// </summary>
        public static readonly PhysicsMask None = default;

        /// <summary>
        /// Bit #0 set (1) in the PhysicsMask. The remaining bits are reset (0).
        /// </summary>
        public static readonly PhysicsMask One = new() { bitMask = 0x1 };

        /// <summary>
        /// All 64 bits set (1) in the PhysicsMask.
        /// </summary>
        public static readonly PhysicsMask All = new() { bitMask = UInt64.MaxValue };

        /// <summary>
        /// An interator that will iterate only the bits that are reset (0) in a <see cref="LowLevelPhysics2D.PhysicsMask"/>
        /// </summary>
        public struct ResetBitIterator : IEnumerable<int>, IEnumerator<int>
        {
            private int m_BitIndex = -1;
            private UInt64 bitMask;

            /// <undoc/>
            public ResetBitIterator(PhysicsMask bitMask) => this.bitMask = bitMask;

            /// <undoc/>
            readonly int IEnumerator<int>.Current => m_BitIndex;

            /// <undoc/>
            readonly object IEnumerator.Current => m_BitIndex;

            /// <undoc/>
            bool IEnumerator.MoveNext()
            {
                if (m_BitIndex >= 63)
                    return false;

                // Find the next reset bit.
                while (++m_BitIndex < 64)
                {
                    if ((bitMask & ((UInt64)1 << m_BitIndex)) == 0)
                        break;
                }

                return m_BitIndex < 64;
            }

            /// <undoc/>
            void IEnumerator.Reset()
            {
                m_BitIndex = -1;
            }

            /// <undoc/>
            public IEnumerator<int> GetEnumerator() => this;

            /// <undoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <undoc/>
            readonly void IDisposable.Dispose() { }
        }

        /// <summary>
        /// An interator that will iterate only the bits that are set (1) in a <see cref="LowLevelPhysics2D.PhysicsMask"/>
        /// </summary>
        public struct SetBitIterator : IEnumerable<int>, IEnumerator<int>
        {
            private int m_BitIndex = -1;
            private UInt64 bitMask;

            /// <undoc/>
            public SetBitIterator(PhysicsMask bitMask) => this.bitMask = bitMask;

            /// <undoc/>
            readonly int IEnumerator<int>.Current => m_BitIndex;

            /// <undoc/>
            readonly object IEnumerator.Current => m_BitIndex;

            /// <undoc/>
            bool IEnumerator.MoveNext()
            {
                if (bitMask == 0 || m_BitIndex >= 63)
                    return false;

                // Find the next set bit.
                while (++m_BitIndex < 64)
                {
                    if ((bitMask & ((UInt64)1 << m_BitIndex)) != 0)
                        break;
                }

                return m_BitIndex < 64;
            }

            /// <undoc/>
            void IEnumerator.Reset()
            {
                m_BitIndex = -1;
            }

            /// <undoc/>
            public IEnumerator<int> GetEnumerator() => this;

            /// <undoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <undoc/>
            readonly void IDisposable.Dispose() { }
        }

        /// <undoc/>
        public readonly IEnumerator<int> GetEnumerator() => new SetBitIterator(this);

        /// <undoc/>
        readonly IEnumerator IEnumerable.GetEnumerator() => new SetBitIterator(this);

        /// <undoc/>
        public override readonly string ToString() => $"bitMask={bitMask}";

        /// <summary>
        /// When applied to a field/property of type <see cref="LowLevelPhysics2D.PhysicsMask"/>, the field/property drawer will not be display it as <see cref="LowLevelPhysics2D.PhysicsLayers"/>.
        /// Instead, the field/property will be displayed as bit numbers only i.e. a raw 64-bit mask allowing each bit to be (de)selected.
        /// This is only used when full layer editing is active (see <see cref="PhysicsLowLevelSettings2D.useFullLayers"/>).
        /// </summary>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public class ShowAsPhysicsMaskAttribute : PropertyAttribute
        {
            /// <undoc/>
            public ShowAsPhysicsMaskAttribute() : base(applyToCollection: true) {}
        }
    }
}
