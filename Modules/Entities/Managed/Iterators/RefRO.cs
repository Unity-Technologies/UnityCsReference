// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// ReadOnlyRef stores a safe read-only reference to a component data.
    /// </summary>
    /// <typeparam name="T">Type of this component</typeparam>
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
    public readonly struct RefRO<T> : IQueryTypeParameter where T : struct, IComponentData
    {
        readonly unsafe byte* _Data;
        readonly AtomicSafetyHandle _Safety;

        /// <summary>
        /// Stores a safe reference to a component from an array of components at the index.
        /// </summary>
        /// <param name="componentDataArray">The NativeArray of components.</param>
        /// <param name="index">The index of the the components.</param>
        public unsafe RefRO(NativeArray<T> componentDataArray, int index)
        {
            _Data = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(componentDataArray);
            _Data += UnsafeUtility.SizeOf<T>() * index;
            _Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(componentDataArray);
            OutOfBoundsArrayConstructor(index, componentDataArray.Length);
        }
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void OutOfBoundsArrayConstructor(int index, int length)
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException("index is out of bounds of NativeArray<>.Length.");
        }

        /// <summary>
        /// Stores a safe reference to a component from an array of components at the index.
        /// If the array is empty stores a null reference.
        /// </summary>
        /// <param name="componentDataArray">The NativeArray of components.</param>
        /// <param name="index">The index of the components.</param>
        /// <returns>Read-only optional reference to component</returns>
        public static RefRO<T> Optional(NativeArray<T> componentDataArray, int index) => componentDataArray.Length == 0 ? default : new RefRO<T>(componentDataArray, index);

        internal unsafe RefRO(void* ptr, AtomicSafetyHandle safety)
        {
            _Data = (byte*)ptr;
            _Safety = safety;
        }

        /// <summary>
        /// Property that returns true if the reference is valid, false otherwise.
        /// </summary>
        /// <remarks>
        /// This property does not take into account if the component is enabled or not.
        ///
        /// This property is intended to indicate whether it is safe to read or write this reference's value.
        /// It is not safe to use this value as a synonym for "does the target entity have component T?".
        /// For zero-size "tag" components (which have no addressable value in chunk memory), IsValid will always
        /// return false, even if the component is present. It is safer to use existing HasComponent features to test
        /// for component presence.
        /// </remarks>
        public unsafe bool IsValid => _Data != null;

        /// <summary>
        /// Returns a read-only reference to the component value itself.
        /// </summary>
        /// <remarks>
        /// This value is a reference to the actual component data.  It is safe to use this field directly, e.g.
        /// "Data.Value.SomeField".  It is also safe to make a copy of this value, e.g. "var myComponent = Data.Value".
        /// Keeping a ref ("ref var myref = Data.Value" is inherently unsafe as any structural change may invalidate this
        /// reference, and there is no way to detect this. It is safe to use this reference locally if you can guarantee
        /// that no structural changes will occur in between acquiring it and using it. Do not hold on to such a reference
        /// for any extended amount of time.
        /// </remarks>
        public unsafe ref readonly T ValueRO
        {
            get
            {
                AtomicSafetyHandle.CheckReadAndThrow(_Safety);
                return ref UnsafeUtility.AsRef<T>(_Data);
            }
        }
    }
}
