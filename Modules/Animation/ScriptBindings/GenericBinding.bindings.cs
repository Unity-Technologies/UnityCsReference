// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using UnityEngine.Bindings;
using UnityEngine.Scripting;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

namespace UnityEngine.Animations
{
    internal enum Flags
    {
        kNone = 0,
        kDiscrete = 1 << 0,
        kPPtr = 1 << 1,
        kSerializeReference = 1 << 2,
        kPhantom = 1 << 3,
        kUnknown = 1 << 4
    };

    [NativeType(CodegenOptions.Custom, "UnityEngine::Animation::MonoGenericBinding")]
    [UsedByNativeCode]
    public readonly struct GenericBinding
    {
        public bool isObjectReference => (m_Flags & Flags.kPPtr) == Flags.kPPtr;
        public bool isDiscrete => (m_Flags & Flags.kDiscrete) != 0;
        public bool isSerializeReference => (m_Flags & Flags.kSerializeReference) == Flags.kSerializeReference;

        public uint transformPathHash => m_Path;
        public uint propertyNameHash => m_PropertyName;
        public EntityId scriptEntityId => m_ScriptEntityId;
        [Obsolete("scriptInstanceID is deprecated. Use scriptEntityId instead.", false)]
        public int scriptInstanceID => m_ScriptEntityId;
        public int typeID => m_TypeID;
        public byte customTypeID => m_CustomType;

        readonly uint m_Path;
        readonly uint m_PropertyName;
        readonly EntityId m_ScriptEntityId;
        readonly int m_TypeID;
        readonly byte m_CustomType;

        internal readonly Flags m_Flags;
    }

    [NativeHeader("Modules/Animation/ScriptBindings/GenericBinding.bindings.h")]
    [StaticAccessor("UnityEngine::Animation::GenericBindingUtility", StaticAccessorType.DoubleColon)]
    public static partial class GenericBindingUtility
    {
        public static bool CreateGenericBinding(UnityEngine.Object targetObject, string property, GameObject root, bool isObjectReference, out GenericBinding genericBinding)
        {
            if (targetObject == null)
                throw new ArgumentNullException(nameof(targetObject));

            if (typeof(Transform).IsAssignableFrom(targetObject.GetType()))
                throw new ArgumentException($"Unsupported type for {nameof(targetObject)}. Cannot create a generic binding from a Transform component.");

            if (targetObject is Component component)
            {
                return CreateGenericBindingForComponent(component, property, root, isObjectReference, out genericBinding);
            }
            else if (targetObject is GameObject gameObject)
            {
                return CreateGenericBindingForGameObject(gameObject, property, root, out genericBinding);
            }

            throw new ArgumentException($"Type {targetObject.GetType()} for {nameof(targetObject)} is unsupported. Expecting either a GameObject or a Component");
        }

        [NativeMethod(IsThreadSafe = false)]
        extern private static bool CreateGenericBindingForGameObject([NotNull] GameObject gameObject, string property, [NotNull] GameObject root, out GenericBinding genericBinding);
        [NativeMethod(IsThreadSafe = false)]
        extern private static bool CreateGenericBindingForComponent([NotNull] Component component, string property, [NotNull] GameObject root, bool isObjectReference, out GenericBinding genericBinding);

        // Discover bindings
        [NativeMethod(IsThreadSafe = false)]
        extern public static GenericBinding[] GetAnimatableBindings([NotNull] GameObject targetObject, [NotNull] GameObject root);
        [NativeMethod(IsThreadSafe = false)]
        extern public static GenericBinding[] GetCurveBindings([NotNull] AnimationClip clip);

        // Bind animatable properties
        [Obsolete("This version of BindProperties is deprecated. Use the overload which includes `out instanceIDProperties` instead.", false)]
        public static unsafe void BindProperties(GameObject rootGameObject, NativeArray<GenericBinding> genericBindings, out NativeArray<BoundProperty> floatProperties, out NativeArray<BoundProperty> discreteProperties, Allocator allocator)
            => BindProperties(rootGameObject, genericBindings, out floatProperties, out discreteProperties, out _, allocator);

        public static unsafe void BindProperties(GameObject rootGameObject, NativeArray<GenericBinding> genericBindings, out NativeArray<BoundProperty> floatProperties, out NativeArray<BoundProperty> discreteProperties, out NativeArray<BoundProperty> instanceIDProperties, Allocator allocator)
        {
            const int transformTypeID = 4;

            ValidateIsCreated(genericBindings);

            int floatPropertyCount = 0;
            int discretePropertiesCount = 0;
            int instanceIDPropertiesCount = 0;
            for (int i = 0; i < genericBindings.Length; i++)
            {
                // Transform bindings is not supported
                if (genericBindings[i].typeID == transformTypeID)
                    continue;

                if (genericBindings[i].isDiscrete)
                    discretePropertiesCount++;
                if (genericBindings[i].isObjectReference)
                    instanceIDPropertiesCount++;
                else
                    floatPropertyCount++;
            }

            floatProperties = new NativeArray<BoundProperty>(floatPropertyCount, allocator);
            discreteProperties = new NativeArray<BoundProperty>(discretePropertiesCount, allocator);
            instanceIDProperties = new NativeArray<BoundProperty>(instanceIDPropertiesCount, allocator);

            void* genericBidingsPtr = genericBindings.GetUnsafePtr();
            void* floatPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(floatProperties);
            void* discretePropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(discreteProperties);
            void* instanceIDPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(instanceIDProperties);

            Internal_BindProperties(rootGameObject, genericBidingsPtr, genericBindings.Length, floatPropertiesPtr, discretePropertiesPtr, instanceIDPropertiesPtr);
        }

        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void Internal_BindProperties([NotNull] GameObject gameObject, void* genericBindings, int genericBindingsCount, void* floatProperties, void* discreteProperties, void* instanceIDProperties);

        // Bind animatable properties
        public static unsafe void UnbindProperties(NativeArray<BoundProperty> boundProperties)
        {
            ValidateIsCreated(boundProperties);
            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);

            Internal_UnbindProperties(boundPropertiesPtr, boundProperties.Length);
        }

        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void Internal_UnbindProperties(void* boundProperties, int boundPropertiesCount);


        // Read/Write to/from animatable properties
        // Not thread safe
        public static unsafe void SetValues(NativeArray<BoundProperty> boundProperties, NativeArray<float> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            SetFloatValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }

        public static unsafe void SetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> indices, NativeArray<float> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(indices);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, indices);
            ValidateIndicesAreInRange(indices, values.Length);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* indicesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(indices);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            SetScatterFloatValues(boundPropertiesPtr, boundProperties.Length, indicesPtr, indices.Length, valuesPtr, values.Length);
        }

        public static unsafe void SetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            SetDiscreteValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }
        public static unsafe void SetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> indices, NativeArray<int> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(indices);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, indices);
            ValidateIndicesAreInRange(indices, values.Length);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* indicesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(indices);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            SetScatterDiscreteValues(boundPropertiesPtr, boundProperties.Length, indicesPtr, indices.Length, valuesPtr, values.Length);
        }

        public static unsafe void SetValues(NativeArray<BoundProperty> boundProperties, NativeArray<EntityId> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            SetEntityIdValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }
        public static unsafe void SetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> indices, NativeArray<EntityId> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(indices);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, indices);
            ValidateIndicesAreInRange(indices, values.Length);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* indicesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(indices);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            SetScatterEntityIdValues(boundPropertiesPtr, boundProperties.Length, indicesPtr, indices.Length, valuesPtr, values.Length);
        }

        public static unsafe void GetValues(NativeArray<BoundProperty> boundProperties, NativeArray<float> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            GetFloatValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }

        public static unsafe void GetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> indices, NativeArray<float> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(indices);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, indices);
            ValidateIndicesAreInRange(indices, values.Length);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* indicesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(indices);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            GetScatterFloatValues(boundPropertiesPtr, boundProperties.Length, indicesPtr, indices.Length, valuesPtr, values.Length);
        }

        public static unsafe void GetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            GetDiscreteValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }


        public static unsafe void GetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> indices, NativeArray<int> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(indices);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, indices);
            ValidateIndicesAreInRange(indices, values.Length);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* indicesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(indices);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            GetScatterDiscreteValues(boundPropertiesPtr, boundProperties.Length, indicesPtr, indices.Length, valuesPtr, values.Length);
        }

        public static unsafe void GetValues(NativeArray<BoundProperty> boundProperties, NativeArray<EntityId> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            GetEntityIdValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }

        public static unsafe void GetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> indices, NativeArray<EntityId> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(indices);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, indices);
            ValidateIndicesAreInRange(indices, values.Length);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* indicesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(indices);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            GetScatterEntityIdValues(boundPropertiesPtr, boundProperties.Length, indicesPtr, indices.Length, valuesPtr, values.Length);
        }

        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void SetFloatValues(void* boundProperties, int boundPropertiesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void SetScatterFloatValues(void* boundProperties, int boundPropertiesCount, void* indices, int indicesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void SetDiscreteValues(void* boundProperties, int boundPropertiesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void SetScatterDiscreteValues(void* boundProperties, int boundPropertiesCount, void* indices, int indicesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void SetEntityIdValues(void* boundProperties, int boundPropertiesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void SetScatterEntityIdValues(void* boundProperties, int boundPropertiesCount, void* indices, int indicesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void GetFloatValues(void* boundProperties, int boundPropertiesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void GetScatterFloatValues(void* boundProperties, int boundPropertiesCount, void* indices, int indicesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void GetDiscreteValues(void* boundProperties, int boundPropertiesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void GetScatterDiscreteValues(void* boundProperties, int boundPropertiesCount, void* indices, int indicesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void GetEntityIdValues(void* boundProperties, int boundPropertiesCount, void* values, int valuesCount);
        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void GetScatterEntityIdValues(void* boundProperties, int boundPropertiesCount, void* indices, int indicesCount, void* values, int valuesCount);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIsCreated<T>(NativeArray<T> array) where T : unmanaged
        {
            if (!array.IsCreated)
                throw new System.ArgumentException($"NativeArray of {typeof(T).Name} is not created.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateIndicesAreInRange(NativeArray<int> indices, int maxValue)
        {
            for(int i = 0; i < indices.Length; i++)
            {
                if(indices[i] < 0 || indices[i] >= maxValue)
                    throw new System.IndexOutOfRangeException($"NativeArray of indices contain element out of range at index '{i}': value '{indices[i]}' is not in the range 0 to {maxValue}.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateLengthMatch<T1, T2>(NativeArray<T1> array1, NativeArray<T2> array2)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            if (array1.Length != array2.Length )
                throw new System.ArgumentException($"Length must be equals for NativeArray<{typeof(T1).Name}> and NativeArray<{typeof(T2).Name}>.");
        }
    }
}
