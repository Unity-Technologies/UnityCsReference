// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    partial class EntitiesProfiler
    {
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        [StructLayout(LayoutKind.Explicit, Size = 1008)]
        public unsafe struct FixedComponentTypeDataList
        {
            const int k_BufferSize = 1006;
            static int s_ComponentTypeDataSize => sizeof(ulong) + sizeof(ComponentTypeFlags);

            [FieldOffset(0)] // 2 bytes
            ushort m_Length;

            [FieldOffset(2)] // 1006 bytes
            fixed byte m_Buffer[k_BufferSize];

            public int Length => m_Length;

            public int Capacity => k_BufferSize / s_ComponentTypeDataSize;

            public ComponentTypeData this[int index]
            {
                get
                {
                    CollectionHelper.CheckIndexInRange(index, m_Length);
                    unsafe
                    {
                        fixed (byte* ptr = m_Buffer)
                        {
                            var offset = index * s_ComponentTypeDataSize;
                            var stableTypeHash = UnsafeUtility.AsRef<ulong>(ptr + offset);
                            var flags = UnsafeUtility.AsRef<ComponentTypeFlags>(ptr + offset + sizeof(ulong));
                            return new ComponentTypeData(stableTypeHash, flags);
                        }
                    }
                }
            }

            public void Add(ulong stableTypeHash, ComponentTypeFlags flags)
            {
                fixed (byte* ptr = m_Buffer)
                {
                    var offset = m_Length++ * s_ComponentTypeDataSize;
                    UnsafeUtility.MemCpy(ptr + offset, &stableTypeHash, sizeof(ulong));
                    UnsafeUtility.MemCpy(ptr + offset + sizeof(ulong), &flags, sizeof(ComponentTypeFlags));
                }
            }
        }
    }
}
