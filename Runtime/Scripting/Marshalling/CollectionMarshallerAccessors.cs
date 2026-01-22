// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;

namespace UnityEngine.Bindings
{
    // Note on TManagedElementType and TMarshalledElementType
    // These types will almost always be the same, but we support some cases where the types are different
    // Specifically this is for T[] GameObject.GetComponents<T>() where we need to generate code to work
    // with any valid T (which may be an GameObject derived type, or an interface type)
    // TManagedElementType is the type in the managed method definition (which can be a generic parameter!)
    // TMarshalledElementType is the type that the marshalling code uses - that is the type that this is marshalled as
    // So for T[] GameObject.GetComponents<T>(), TManagedElementType is T, and TMarshalledElementType is UnityEngine.Object
    [VisibleToOtherModules]
    [Il2CppEagerStaticClassConstruction]
    internal readonly struct ArrayMarshallerAccessor<TManagedElementType, TMarshalledElementType> : ICollectionMarshallingAccessor<TMarshalledElementType>
    {
        private readonly TMarshalledElementType[] array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArrayMarshallerAccessor(TManagedElementType[] array)
        {
            CollectionMarshallingAccessorsAsserts.AssertElementTypePunning<TManagedElementType, TMarshalledElementType>();
            this.array = UnsafeUtility.As<TMarshalledElementType[]>(array);
        }

        public bool IsNull => array == null;
        public int Length => array.Length;
        public int Capacity => array.Length;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<TMarshalledElementType> AsSpan() => array ?? default;
        public TMarshalledElementType this[int i] { get => array[i]; set => array[i] = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TMarshalledElementType GetRef(int i) => ref array[i];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TMarshalledElementType GetPinnableReference()
        {
            if (array == null || array.Length == 0)
                return ref UnsafeUtility.NullRef<TMarshalledElementType>();

            return ref array[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectionChanged(int newSize)
        {
            AssertArraySize(newSize);
            if (array != null && RuntimeHelpers.IsReferenceOrContainsReferences<TMarshalledElementType>() && newSize < array.Length)
                Array.Clear(array, newSize, array.Length - newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNull() => CollectionChanged(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEmpty() => CollectionChanged(0);

        #region Error and Asserts

        // These error cases are written the way that they are to prevent allocations during Mono Jitting
        // In Mono an ldstr of a constant string allocates a string object at jit time
        // We have tests that assert no GC allocations and the string allocation causes them to fail (The jit allocations go through the normal string API's so the are profiled)
        // The static string load needs to go through an extra function call to prevent Roslyn from converting optimizing the static string into a constant interpolated string

        private const string ArraySizeAssertMessage = "Native code returned {0}, but the managed array is only has {1} item(s), the last {2} value(s) will be dropped";

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static string GetArraySizeAssertMessage()
        {
            return ArraySizeAssertMessage;
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void AssertArraySize(int size)
        {
            if (size <= (array?.Length ?? 0))
                return;
            Debug.Assert(size <= (array?.Length ?? 0), string.Format(GetArraySizeAssertMessage(), size, array?.Length, size - array?.Length));
        }

        #endregion
    }

    [VisibleToOtherModules]
    struct ArrayByRefMarshallingAccessor<TManagedElementType, TMarshalledElementType> : ICollectionMarshallingAccessor<TMarshalledElementType>
    {
        private TMarshalledElementType[] array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArrayByRefMarshallingAccessor(TManagedElementType[] array)
        {
            CollectionMarshallingAccessorsAsserts.AssertElementTypePunning<TManagedElementType, TMarshalledElementType>();
            this.array = UnsafeUtility.As<TMarshalledElementType[]>(array);
        }

        public bool IsNull => array == null;
        public int Length => array.Length;
        public int Capacity => array.Length;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<TMarshalledElementType> AsSpan() => array ?? default;
        public TMarshalledElementType this[int i] { get => array[i]; set => array[i] = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TMarshalledElementType GetRef(int i) => ref array[i];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TMarshalledElementType GetPinnableReference()
        {
            if (array == null || array.Length == 0)
                return ref UnsafeUtility.NullRef<TMarshalledElementType>();

            return ref array[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectionChanged(int newSize) => array = UnsafeUtility.As<TMarshalledElementType[]>(new TManagedElementType[newSize]);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNull() => array = null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEmpty() => array = UnsafeUtility.As<TMarshalledElementType[]>(Array.Empty<TManagedElementType>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TManagedElementType[] GetArray() => UnsafeUtility.As<TManagedElementType[]>(array);
    }

    [VisibleToOtherModules]
    internal struct ListMarshallingAccessor<TManagedElementType, TMarshalledElementType> : ICollectionMarshallingAccessor<TMarshalledElementType>
    {
        private List<TManagedElementType> list;
        private TMarshalledElementType[] array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ListMarshallingAccessor(List<TManagedElementType> list)
        {
            CollectionMarshallingAccessorsAsserts.AssertElementTypePunning<TManagedElementType, TMarshalledElementType>(); 
            this.list = list;
            this.array = UnsafeUtility.As<TMarshalledElementType[]>(NoAllocHelpers.ExtractArrayFromList(list)) ?? Array.Empty<TMarshalledElementType>();
        }

        public bool IsNull => list == null;
        public int Length => list.Count;
        public int Capacity => list.Capacity;
        public TMarshalledElementType this[int i] { get => array[i]; set => array[i] = value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TMarshalledElementType GetRef(int i) => ref array[i];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TMarshalledElementType GetPinnableReference()
        {
            if (array.Length == 0)
                return ref UnsafeUtility.NullRef<TMarshalledElementType>();
            return ref array[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<TMarshalledElementType> AsSpan() => array.AsSpan().Slice(0, list.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectionChanged(int newSize)
        {
            if (list == null)
                list = new List<TManagedElementType>(newSize);
            else if (list.Capacity < newSize)
                list.Capacity = newSize;

            if (list.Count != newSize)
                NoAllocHelpers.ResetListSize(list, newSize);
            else // We assume that a marshaller could have modified the contents in place - so always invalid the enumerators
                NoAllocHelpers.InvalidateListEnumerators(list);

            array = UnsafeUtility.As<TMarshalledElementType[]>(NoAllocHelpers.ExtractArrayFromList(list));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNull() => list = null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEmpty()
        {
            if (list == null)
                list = new List<TManagedElementType>();
            else
                list.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<TManagedElementType> GetList() => list;
    }

    internal static class CollectionMarshallingAccessorsAsserts
    {
        public static void AssertElementTypePunning<TManagedElementType, TMarsahlledElementType>()
        {
            // The collection marshallers allow type punning for reference types/interfaces
            // This is to support marshalling of UnityEngine.Object types
            // GameObject.FindObjectOfType<T> allows users to search for GameObjects that implement a specific interface
            // and the expected array return type is an array of the interface type.

            Debug.Assert(typeof(TManagedElementType).IsValueType == typeof(TMarsahlledElementType).IsValueType);
            Debug.Assert(!typeof(TManagedElementType).IsValueType || typeof(TManagedElementType) == typeof(TMarsahlledElementType));
        }
    }
}
