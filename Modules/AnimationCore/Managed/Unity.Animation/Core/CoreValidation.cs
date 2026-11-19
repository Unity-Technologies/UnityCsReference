// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Buffers;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.IntegerTime;
using UnityEngine;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    static public partial class Core
    {
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated<T>(BlobAssetReference<T> blob) where T : unmanaged
        {
            if (!blob.IsCreated)
                throw new System.NullReferenceException($"BlobAssetReference of {typeof(T).Name} is not created.");
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateArgumentIsCreated<T>(BlobAssetReference<T> blob) where T : unmanaged
        {
            if (!blob.IsCreated)
                throw new System.ArgumentNullException($"BlobAssetReference of {typeof(T).Name} is not created.");
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated<T>(NativeArray<T> array) where T : unmanaged
        {
            if (!array.IsCreated)
                throw new System.NullReferenceException($"Array of {typeof(T).Name} is not created.");
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated<T>(NativeList<T> list) where T : unmanaged
        {
            if (!list.IsCreated)
                throw new System.NullReferenceException($"List of {typeof(T).Name} is not created.");
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated<T>(UnsafeList<T> list) where T : unmanaged
        {
            if (!list.IsCreated)
                throw new System.NullReferenceException($"List of {typeof(T).Name} is not created.");
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateArgumentIsCreated<T>(NativeList<T> list) where T : unmanaged
        {
            if (!list.IsCreated)
                throw new System.ArgumentNullException($"List of {typeof(T).Name} is not created.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateAreEqual(int expected, int value)
        {
            if (expected != value)
                throw new System.InvalidOperationException($"Value must match: '{expected}' and '{value}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreaterOrEqual(int value, int expected)
        {
            if (!(value >= expected))
                throw new System.InvalidOperationException($"Value '{value}' must be greater or equal to '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreaterOrEqual(float value, float expected)
        {
            if (!(value >= expected))
                throw new System.InvalidOperationException($"Value '{value}' must be greater or equal to '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreaterOrEqual(DiscreteTime value, DiscreteTime expected) => ValidateGreaterOrEqual(value.Value, expected.Value);


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateLessOrEqual(int value, int expected)
        {
            if (!(value <= expected))
                throw new System.InvalidOperationException($"Value '{value}' must be less or equal to '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateLessOrEqual(DiscreteTime value, DiscreteTime expected) => ValidateLessOrEqual(value.Value, expected.Value);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateLessOrEqual(float value, float expected)
        {
            if (!(value <= expected))
                throw new System.InvalidOperationException($"Value '{value}' must be less or equal to '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreater(int value, int expected)
        {
            if (!(value > expected))
                throw new System.InvalidOperationException($"Value '{value}' must be greater than '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreater(float value, float expected)
        {
            if (!(value > expected))
                throw new System.InvalidOperationException($"Value '{value}' must be greater than '{expected}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateGreater(DiscreteTime value, DiscreteTime expected) => ValidateGreater(value.Value, expected.Value);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateFreeformDataIsCreated(in FreeformBlendSpace2DData data)
        {
            if (!data.IsCreated)
                throw new System.InvalidOperationException("The freeform blend space data is not created or was disposed.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateBufferLengthsAreEqual(int expected, int value)
        {
            if (expected != value)
                throw new System.InvalidOperationException($"Buffer length must match: expected '{expected}' but was '{value}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateBufferIndexBounds(int index, int length)
        {
            if ((uint)index >= length)
                throw new System.IndexOutOfRangeException($"index '{index}' is out of range of '{length}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsFinite(float value)
        {
            if (!math.isfinite(value))
                throw new System.NotFiniteNumberException("Value is not finite.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void NotImplementedException()
        {
            throw new System.NotImplementedException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsTrue(bool condition)
        {
            if (!condition)
                throw new System.InvalidOperationException("Condition was false.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsFalse(bool condition)
        {
            if (condition)
                throw new System.InvalidOperationException("Condition was true.");
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static unsafe void ValidateIsNotNull<T>(T* ptr) where T : unmanaged
        {
            if (ptr == null)
                throw new System.NullReferenceException($"Pointer of type {typeof(T).Name} is null.");
        }

        [ExcludeFromBurstCompatTesting("GenerateTestsForBurstCompatibility does not work with nullable inputs, but we generate tests in ValidationTests.ValidateIsNotNullBurstCompatible")]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsNotNull<T>(T? ptr) where T : unmanaged
        {
            if (ptr == null)
                throw new System.NullReferenceException($"Nullable of type {typeof(T).Name} is null.");
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsUnmanaged<T>() where T : struct
        {
            if (!UnsafeUtility.IsUnmanaged<T>())
                throw new System.InvalidOperationException($"Data [{typeof(T).Name}] contains managed type.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsMultipleOf4(int value)
        {
            if ((value & 3) != 0)
                throw new System.InvalidOperationException($"Value [{value}] is not a multiple of 4.");
        }
    }
}
