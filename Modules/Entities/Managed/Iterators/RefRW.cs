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
    /// RefRW stores a reference (with write access) to native memory
    /// </summary>
    /// <typeparam name="T">The type of object referenced.</typeparam>
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
    public readonly struct RefRW<T> : IQueryTypeParameter where T : struct, IComponentData
    {
        /// <summary>
        /// Convert into a read-only version RefRO of this RefRW
        /// </summary>
        /// <param name="refRW">The read-write reference to convert to read-only</param>
        /// <returns>Returns a RefRO</returns>
        public static unsafe explicit operator RefRO<T>(RefRW<T> refRW)
            => new RefRO<T>(refRW._Data, refRW._Safety);

        readonly private unsafe byte*      _Data;
        readonly AtomicSafetyHandle _Safety;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void OutOfBoundsArrayConstructor(int index, int length)
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException("index is out of bounds of NativeArray<>.Length.");
        }

        /// <summary>
        /// Stores a safe reference a pointer to T
        /// </summary>
        /// <param name="ptr">Pointer pointing to an instance of T</param>
        /// <param name="safety">AtomicSafetyHandle protecting access to 'ptr'</param>
        public unsafe RefRW(byte* ptr, AtomicSafetyHandle safety)
        {
            _Data = ptr;
            _Safety = safety;
        }

        /// <summary>
        /// Stores a safe reference to a component from an array of components at the index.
        /// </summary>
        /// <param name="componentDataNativeArray">The array of components.</param>
        /// <param name="index">The index of the array.</param>
        public unsafe RefRW(NativeArray<T> componentDataNativeArray, int index)
        {
            _Data = (byte*) NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(componentDataNativeArray);
            _Data += UnsafeUtility.SizeOf<T>() * index;
            _Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(componentDataNativeArray);
            OutOfBoundsArrayConstructor(index, componentDataNativeArray.Length);
        }

        /// <summary>
        /// Stores a safe reference to a component from an array of components at the index.
        /// If the array is empty stores a null reference.
        /// </summary>
        /// <param name="componentDataNativeArray">The array of components.</param>
        /// <param name="index">The index of the array.</param>
        /// <returns>Read-write optional reference to component</returns>
        public static RefRW<T> Optional(NativeArray<T> componentDataNativeArray, int index)
        {
            return componentDataNativeArray.Length == 0 ? default : new RefRW<T>(componentDataNativeArray, index);
        }

        internal unsafe RefRW(void* ptr, AtomicSafetyHandle safety)
        {
            _Data = (byte*)ptr;
            _Safety = safety;
        }
        /// <summary>
        /// Checks if the component exists on this entity.
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
        /// Returns a writable reference to the component value itself.
        /// </summary>
        /// <remarks>
        /// This value is a reference to the actual component data.  It is safe to use this field directly, e.g.
        /// "Data.ValueRW.SomeField = 123".  It is also safe to make a copy of this value, e.g. "var myComponent = Data.ValueRW".
        /// Keeping a ref ("ref var myref = Data.ValueRW" is inherently unsafe as any structural change may invalidate this
        /// reference, and there is no way to detect this. It is safe to use this reference locally if you can guarantee
        /// that no structural changes will occur in between acquiring it and using it. Do not hold on to such a reference
        /// for any extended amount of time.
        /// </remarks>
        public unsafe ref T ValueRW
        {
            get
            {
                AtomicSafetyHandle.CheckWriteAndThrow(_Safety);
                return ref UnsafeUtility.AsRef<T>(_Data);
            }
        }

        /// <summary>
        /// Returns a read-only reference to the component value itself.
        /// </summary>
        /// <remarks>
        /// This value is a reference to the actual component data.  It is safe to use this field directly, e.g.
        /// "Data.ValueRO.SomeField".  It is also safe to make a copy of this value, e.g. "var myComponent = Data.ValueRO".
        /// Keeping a ref ("ref var myref = Data.ValueRO" is inherently unsafe as any structural change may invalidate this
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
