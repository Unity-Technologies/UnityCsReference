// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Entities
{
    internal unsafe partial struct WorldUnmanagedImpl
    {
        // Manages a hierarchical bit set of 64 x 64 bits for that many system states.
        // The intent is to be able to do two trailing zero counts to find a free slot,
        // without maintaining a free list
        internal struct StateAllocator
        {
            internal ulong m_FreeBits;
            internal StateAllocLevel1* m_Level1;

            public void Init()
            {
                m_FreeBits = ~0ul;

                int allocSize = sizeof(StateAllocLevel1) * 64;
                var l1 = m_Level1 = (StateAllocLevel1*)Memory.Unmanaged.Allocate(allocSize, 16, Allocator.Persistent);
                UnsafeUtility.MemClear(l1, allocSize);

                for (int i = 0; i < 64; ++i)
                {
                    l1[i].FreeBits = ~0ul;
                }
            }

            public void Dispose()
            {
                var l1 = m_Level1;

                for (int i = 0; i < 64; ++i)
                {
                    if (l1[i].States != null)
                    {
                        Memory.Unmanaged.Free(l1[i].States, Allocator.Persistent);
                    }
                }

                Memory.Unmanaged.Free(l1, Allocator.Persistent);
                m_Level1 = null;

                this = default;
            }

            public SystemState* Resolve(ushort handle, ushort version)
            {
                int index = handle >> 6;
                int subIndex = handle & 63;

                ref var leaf = ref m_Level1[index];
                return leaf.Version[subIndex] == version ? leaf.States + subIndex : null;
            }

            public SystemState* ResolveNoCheck(ushort handle)
            {
                int index = handle >> 6;
                int subIndex = handle & 63;
                return m_Level1[index].States + subIndex;
            }

            public long GetTypeHashNoCheck(ushort handle)
            {
                int index = handle >> 6;
                int subIndex = handle & 63;
                return m_Level1[index].TypeHash[subIndex];
            }

            public SystemState* Alloc(out ushort outHandle, out ushort outVersion, long typeHash)
            {
                CheckFull();

                int index = math.tzcnt(m_FreeBits);

                ref var leaf = ref *(m_Level1 + index);

                int subIndex = math.tzcnt(leaf.FreeBits);

                CheckSubIndex(subIndex);

                if (leaf.States == null)
                {
                    leaf.States = (SystemState*)Memory.Unmanaged.Allocate(64 * sizeof(SystemState), 16, Allocator.Persistent);
                }

                leaf.FreeBits &= ~(1ul << subIndex);

                // Branch-free clear of our bit in parent cascade if we're empty
                ulong empty = leaf.FreeBits == 0 ? 1ul : 0ul;
                m_FreeBits &= ~(empty << index);

                var resultPtr = leaf.States + subIndex;

                UnsafeUtility.MemClear(resultPtr, sizeof(SystemState));

                outHandle = (ushort)((index << 6) + subIndex);

                IncVersion(ref leaf.Version[subIndex]);
                outVersion = leaf.Version[subIndex];
                leaf.TypeHash[subIndex] = typeHash;

                return resultPtr;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void CheckFull()
            {
                if (0 == m_FreeBits)
                    throw new InvalidOperationException("out of system state slots; maximum is 4,096");
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private static void CheckSubIndex(int subIndex)
            {
                if ((uint)subIndex > 63)
                    throw new InvalidOperationException("data structure corrupted");
            }

            public void Free(ushort handle)
            {
                int index = handle >> 6;
                int subIndex = handle & 63;

                CheckIndex(index);

                m_FreeBits |= 1ul << index;

                ref var leaf = ref *(m_Level1 + index);

                CheckIsAllocated(ref leaf, subIndex);

                leaf.FreeBits |= (1ul << subIndex);
                IncVersion(ref leaf.Version[subIndex]);
                leaf.TypeHash[subIndex] = 0;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private static void CheckIsAllocated(ref StateAllocLevel1 leaf, int subIndex)
            {
                if ((leaf.FreeBits & (1ul << subIndex)) != 0)
                {
                    throw new InvalidOperationException("slot is not allocated");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private static void CheckIndex(int index)
            {
                if (index > 63)
                    throw new ArgumentException("bad index");
            }

            public static void IncVersion(ref ushort v)
            {
                uint m = v;
                m += 1;
                m = (m >> 16) | m; // Fold overflow bit down to make 0xffff wrap to 0x0001, avoiding zero which is reserved for "unused"
                v = (ushort)m;
            }

            public SystemState* GetState(ushort handle)
            {
                return GetBlock(handle, out var subIndex)->States + subIndex;
            }

            public StateAllocLevel1* GetBlock(ushort handle, out ushort subIndex)
            {
                ushort index = (ushort)(handle >> 6);
                subIndex = (ushort)(handle & 63);
                return m_Level1 + index;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StateAllocLevel1
        {
            public ulong FreeBits;
            public SystemState* States;
            public fixed ushort Version[64];
            public fixed long TypeHash[64];
        }
    }

    internal struct SystemInstance : IComponentData
    {
        unsafe internal SystemState* state;
    }
}
