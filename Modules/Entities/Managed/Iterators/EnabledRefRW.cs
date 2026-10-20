// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Threading;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Entities
{
    /// <summary>
    /// Stores a safe reference to a read-writable component enable bit.
    /// Also keeps a pointer to the chunk disabled count, which is updated when the enabled bit is written to.
    /// </summary>
    /// <remarks>Do not store outside of stack</remarks>
    /// <typeparam name="T">Type of enabled component</typeparam>
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
    public readonly struct EnabledRefRW<T> : IQueryTypeParameter where T : unmanaged, IEnableableComponent
    {
        /// <summary>
        /// Convert into a read-only version EnabledRefRO of this EnabledRefRW
        /// </summary>
        /// <param name="componentEnabledRefRW">The read-write reference to convert to read-only</param>
        /// <returns>The EnabledRefRO</returns>
        public static unsafe implicit operator EnabledRefRO<T>(EnabledRefRW<T> componentEnabledRefRW)
            => new EnabledRefRO<T>(componentEnabledRefRW.m_Ptr);
        readonly SafeBitRef m_Ptr;
        readonly unsafe int* m_PtrChunkDisabledCount;

        /// <summary>
        /// Constructor for writable enable reference to enableable component.
        /// </summary>
        /// <param name="ptr">Pointer to single bit and safety handle</param>
        /// <param name="ptrChunkDisabledCount"></param>
        public unsafe EnabledRefRW(SafeBitRef ptr, int* ptrChunkDisabledCount)
        {
            m_Ptr = ptr;
            m_PtrChunkDisabledCount = ptrChunkDisabledCount;
        }

        /// <summary>
        /// Null value for this reference.
        /// </summary>
        public static unsafe EnabledRefRW<T> Null => new EnabledRefRW<T>(SafeBitRef.Null, null);

        /// <summary>
        /// Property that returns true if the reference is valid, false otherwise.
        /// </summary>
        public bool IsValid => m_Ptr.IsValid;

        /// <summary>
        /// Property to get enabled value of this reference (true if enabled, false otherwise).
        /// </summary>
        public bool ValueRO => m_Ptr.GetBit();

        /// <summary>
        /// Property to get or set enabled value of this reference  (true if enabled, false otherwise).
        /// </summary>
        public unsafe bool ValueRW
        {
            get => m_Ptr.GetBit();
            set
            {
                if (Hint.Unlikely(m_PtrChunkDisabledCount == null))
                    throw new InvalidOperationException(
                        "This EnabledRefRW was created from a read-only type handle or is uninitialized, and can not be used to set bit values.");
                bool wasSet = m_Ptr.GetBit();
                m_Ptr.SetBit(value);
                if (Hint.Likely(wasSet != value))
                {
                    var adjustment = math.select(1, -1, value);
                    Interlocked.Add(ref UnsafeUtility.AsRef<int>(m_PtrChunkDisabledCount), adjustment);
                }
            }
        }
    }
}
