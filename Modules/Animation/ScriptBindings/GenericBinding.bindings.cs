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

    ///<summary>Defines an animatable property on a Unity Component.</summary>
    ///<remarks>
    ///  <see cref="GenericBinding" /> and <see cref="BoundProperty" /> are classes for animating properties on objects in a completely generic way.
    ///
    ///See also <see cref="GenericBindingUtility" />.</remarks>
    [NativeType(CodegenOptions.Custom, "UnityEngine::Animation::MonoGenericBinding")]
    [UsedByNativeCode]
    public readonly struct GenericBinding
    {
        ///<summary>This property is True when this GenericBinding target is an animatable Unity object reference, such as a sprite.</summary>
        public bool isObjectReference => (m_Flags & Flags.kPPtr) == Flags.kPPtr;
        ///<summary>This property is true when the GenericBinding target is an animatable public integer.</summary>
        public bool isDiscrete => (m_Flags & Flags.kDiscrete) != 0;
        ///<summary>This property is True when this GenericBinding target is a serialized reference property.</summary>
        public bool isSerializeReference => (m_Flags & Flags.kSerializeReference) == Flags.kSerializeReference;

        ///<summary>Hash ID that represents the transform path. Use this property to locate the component in the transform hierarchy.</summary>
        public uint transformPathHash => m_Path;
        ///<summary>Hash ID that represents the name of the property.</summary>
        public uint propertyNameHash => m_PropertyName;
        ///<summary>The EntityId of the script.</summary>
        ///<remarks>Use GenericBinding to target a MonoBehaviour animatable property. In this case, scriptEntityId should be non-zero.</remarks>
        public EntityId scriptEntityId => m_ScriptEntityId;
        ///<summary>The instance ID of the script.</summary>
        ///<remarks>Use GenericBinding to target a MonoBehaviour animatable property. In this case, scriptInstanceID should be non-zero.</remarks>
        [Obsolete("scriptInstanceID is deprecated. Use scriptEntityId instead.", true)]
        public int scriptInstanceID => m_ScriptEntityId;
        ///<summary>The Unity component type ID.</summary>
        public int typeID => m_TypeID;
        ///<summary>The internal ID for custom animation bindings.</summary>
        ///<remarks>Some Unity systems, like shader, are more dynamic and require custom bindings to expose their properties.</remarks>
        public byte customTypeID => m_CustomType;

        readonly uint m_Path;
        readonly uint m_PropertyName;
        readonly EntityId m_ScriptEntityId;
        readonly int m_TypeID;
        readonly byte m_CustomType;

        internal readonly Flags m_Flags;
    }

    ///<summary>Animation utility functions for reading and writing values from Unity components.</summary>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Collections.Generic;
    ///using UnityEngine;
    ///using UnityEngine.Animations;
    ///using UnityEditor;
    ///using Unity.Collections;
    ///using System.Linq;
    ///
    ///public class AnimationClipPlayer : MonoBehaviour
    ///{
    ///    public AnimationClip        animationClip;
    ///    public float                time;
    ///
    ///    List<AnimationCurve>        curves;
    ///
    ///    NativeArray<BoundProperty>  floatProperties;
    ///    NativeArray<BoundProperty>  intProperties;
    ///    NativeArray<BoundProperty>  instanceIDProperties;
    ///    NativeArray<float>          floatValues;
    ///    NativeArray<int>            intValues;
    ///
    ///    void Start()
    ///    {
    ///        var editorCurveBindings = AnimationUtility.GetCurveBindings(animationClip);
    ///        editorCurveBindings = editorCurveBindings.Where(editorCurveBinding =>
    ///            editorCurveBinding.type != typeof(Transform) && !editorCurveBinding.isPPtrCurve && !editorCurveBinding.isDiscreteCurve
    ///            ).ToArray();
    ///
    ///        curves = new List<AnimationCurve>(editorCurveBindings.Length);
    ///        for (var i = 0; i < editorCurveBindings.Length; i++)
    ///        {
    ///            curves.Add(AnimationUtility.GetEditorCurve(animationClip, editorCurveBindings[i]));
    ///        }
    ///
    ///        var genericBindings = new NativeArray<GenericBinding>(AnimationUtility.EditorCurveBindingsToGenericBindings(editorCurveBindings), Allocator.Temp);
    ///        GenericBindingUtility.BindProperties(gameObject, genericBindings, out floatProperties, out intProperties, out instanceIDProperties, Allocator.Persistent);
    ///
    ///        floatValues = new NativeArray<float>(floatProperties.Length, Allocator.Persistent);
    ///        intValues = new NativeArray<int>(intProperties.Length, Allocator.Persistent);
    ///    }
    ///
    ///    private void OnDestroy()
    ///    {
    ///        floatProperties.Dispose();
    ///        floatValues.Dispose();
    ///        intProperties.Dispose();
    ///        intValues.Dispose();
    ///    }
    ///
    ///    // Update is called once per frame
    ///    void Update()
    ///    {
    ///        for (int i = 0; i < curves.Count; i++)
    ///        {
    ///            floatValues[i] = curves[i].Evaluate(time);
    ///        }
    ///
    ///        GenericBindingUtility.SetValues(floatProperties, floatValues);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [NativeHeader("Modules/Animation/ScriptBindings/GenericBinding.bindings.h")]
    [StaticAccessor("UnityEngine::Animation::GenericBindingUtility", StaticAccessorType.DoubleColon)]
    public static partial class GenericBindingUtility
    {
        ///<summary>Retrieves the <see cref="GenericBinding" /> that represents a specific property associated with a GameObject or one of its components.</summary>
        ///<param name="targetObject">The target <see cref="GameObject" /> to extract the bindings from.</param>
        ///<param name="property">The name of the property.</param>
        ///<param name="root">A <see cref="GameObject" /> ancestor of targetObject. Use the ancestor to locate the targetObject within a transform hierarchy.</param>
        ///<param name="genericBinding">Returns the <see cref="GenericBinding" /> representing the property on the GameObject to animate.</param>
        ///<param name="isObjectReference">Specifies whether the property is an object reference. If you do not identify the property correctly, the method fails. Set this parameter to True if the property references an object. Set to False if the property is a float or an integer.</param>
        ///<returns>Returns True if the operation is successful, otherwise False.</returns>
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
        ///<summary>Retrieves the animatable bindings for a specific GameObject.</summary>
        ///<param name="targetObject">The target <see cref="GameObject" /> to extract the bindings from.</param>
        ///<param name="root">A <see cref="GameObject" /> ancestor of targetObject. Use the ancestor to locate the targetObject within a transform hierarchy.</param>
        ///<returns>Returns an array of <see cref="GenericBinding" />. Returns an empty array if the targetObject has no animatable properties.</returns>
        [NativeMethod(IsThreadSafe = false)]
        extern public static GenericBinding[] GetAnimatableBindings([NotNull] GameObject targetObject, [NotNull] GameObject root);
        ///<summary>Retrieves the curve bindings from an animation clip.</summary>
        ///<param name="clip">The animation clip.</param>
        ///<returns>Returns an array of <see cref="GenericBinding" /> for the animation curves. Returns an empty array if the animation clip has no animation curves.</returns>
        [NativeMethod(IsThreadSafe = false)]
        extern public static GenericBinding[] GetCurveBindings([NotNull] AnimationClip clip);

        // Bind animatable properties
        ///<summary>Retrieves the list of <see cref="BoundProperty" /> defined by the list of <see cref="GenericBinding" />.</summary>
        ///<remarks>BoundProperty allocates resources that must be unallocated. See <see cref="GenericBindingUtility.UnbindProperties" />.
        ///
        ///This method throws an ArgumentException if the genericBindings NativeArray is not created.</remarks>
        ///<param name="rootGameObject">The root GameObject.</param>
        ///<param name="genericBindings">The list of <see cref="GenericBinding" /> to bind. See <see cref="GenericBindingUtility.GetAnimatableBindings" />, <see cref="GenericBindingUtility.GetCurveBindings" />.</param>
        ///<param name="floatProperties">Returns the list of float bound properties for all valid generic binding. If there is an invalid binding, this param returns <see cref="BoundProperty.Null" />.</param>
        ///<param name="discreteProperties">Returns the list of discrete bound properties for all valid generic bindings. If there is an invalid binding, this param returns <see cref="BoundProperty.Null" /></param>
        ///<param name="allocator">Allocator for allocating NativeArray memory.</param>
        [Obsolete("This version of BindProperties is deprecated. Use the overload which includes `out instanceIDProperties` instead.", true)]
        public static unsafe void BindProperties(GameObject rootGameObject, NativeArray<GenericBinding> genericBindings, out NativeArray<BoundProperty> floatProperties, out NativeArray<BoundProperty> discreteProperties, Allocator allocator)
            => BindProperties(rootGameObject, genericBindings, out floatProperties, out discreteProperties, out _, allocator);

        public static unsafe void BindProperties(GameObject rootGameObject, NativeArray<GenericBinding> genericBindings, out NativeArray<BoundProperty> floatProperties, out NativeArray<BoundProperty> discreteProperties, out NativeArray<BoundProperty> instanceIDProperties, Allocator allocator)
        {
            const int transformTypeID = 4;

            ValidateIsCreated(genericBindings);

            int floatPropertyCount = 0;
            int discretePropertiesCount = 0;
            int entityIdPropertiesCount = 0;
            for (int i = 0; i < genericBindings.Length; i++)
            {
                // Transform bindings is not supported
                if (genericBindings[i].typeID == transformTypeID)
                    continue;

                if (genericBindings[i].isDiscrete)
                    discretePropertiesCount++;
                if (genericBindings[i].isObjectReference)
                    entityIdPropertiesCount++;
                else
                    floatPropertyCount++;
            }

            floatProperties = new NativeArray<BoundProperty>(floatPropertyCount, allocator);
            discreteProperties = new NativeArray<BoundProperty>(discretePropertiesCount, allocator);
            instanceIDProperties = new NativeArray<BoundProperty>(entityIdPropertiesCount, allocator);

            void* genericBidingsPtr = genericBindings.GetUnsafePtr();
            void* floatPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(floatProperties);
            void* discretePropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(discreteProperties);
            void* entityIdPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(instanceIDProperties);

            Internal_BindProperties(rootGameObject, genericBidingsPtr, genericBindings.Length, floatPropertiesPtr, discretePropertiesPtr, entityIdPropertiesPtr);
        }

        [NativeMethod(IsThreadSafe = false)]
        extern internal static unsafe void Internal_BindProperties([NotNull] GameObject gameObject, void* genericBindings, int genericBindingsCount, void* floatProperties, void* discreteProperties, void* instanceIDProperties);

        // Bind animatable properties
        ///<summary>Unbinds and frees all resources used by a list of <see cref="BoundProperty" />.</summary>
        ///<remarks>Creating a BoundProperty that targets a serialized reference avoids garbage collection for its objects. If you forget to unbind and free the resources used by this BoundProperty, it may result in a memory leak.
        ///
        ///This method throws an ArgumentException if the boundProperties's NativeArray is not yet created.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to unbind.</param>
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
        ///<summary>Sets the float or integer value for each [[BoundProperty].</summary>
        ///<remarks>This method throws an ArgumentException if the NativeArray is not yet created.
        ///
        ///This method throws an ArgumentException if the indices list does not match the length of the boundProperties list.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to set the values for.</param>
        ///<param name="values">The list of float or integer values to set.</param>
        public static unsafe void SetValues(NativeArray<BoundProperty> boundProperties, NativeArray<float> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            SetFloatValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }

        ///<summary>Sets the float/integer values for each [[BoundProperty] and uses the value at the index define in indices.</summary>
        ///<remarks>This method throws an ArgumentException if the NativeArray is not yet created.
        ///
        ///This method throws an ArgumentException if the indices list does not match the length of the boundProperties list.
        ///
        ///This method throws an IndexOutOfRangeException if an index in the indices list is out of range.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to set the values for.</param>
        ///<param name="indices">The list of indices where each <see cref="BoundProperty" /> value will be read.</param>
        ///<param name="values">The list of float or integer values.</param>
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

        ///<summary>Sets the float or integer value for each [[BoundProperty].</summary>
        ///<remarks>This method throws an ArgumentException if the NativeArray is not yet created.
        ///
        ///This method throws an ArgumentException if the indices list does not match the length of the boundProperties list.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to set the values for.</param>
        ///<param name="values">The list of float or integer values to set.</param>
        public static unsafe void SetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            SetDiscreteValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }
        ///<summary>Sets the float/integer values for each [[BoundProperty] and uses the value at the index define in indices.</summary>
        ///<remarks>This method throws an ArgumentException if the NativeArray is not yet created.
        ///
        ///This method throws an ArgumentException if the indices list does not match the length of the boundProperties list.
        ///
        ///This method throws an IndexOutOfRangeException if an index in the indices list is out of range.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to set the values for.</param>
        ///<param name="indices">The list of indices where each <see cref="BoundProperty" /> value will be read.</param>
        ///<param name="values">The list of float or integer values.</param>
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

        ///<summary>Retrieves the float or integer value for each [[BoundProperty].</summary>
        ///<remarks>This method throws an ArgumentException if the NativeArray is not yet created.
        ///
        ///This method throws an ArgumentException if the values list does not match the length of the boundProperties list.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to get the values from.</param>
        ///<param name="values">Returns the list of float or integer values.</param>
        public static unsafe void GetValues(NativeArray<BoundProperty> boundProperties, NativeArray<float> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            GetFloatValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }

        ///<summary>Retrieves the float or integer value for each [[BoundProperty] and writes the value at a different index specified by the indices list.</summary>
        ///<remarks>This method throws an ArgumentException if the NativeArray is not yet created.
        ///
        ///This method throws an ArgumentException if the indices list does not match the length of the boundProperties list.
        ///
        ///This method throws an IndexOutOfRangeException if an index in the indices list is out of range.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to get the values from.</param>
        ///<param name="indices">The list of indices where each <see cref="BoundProperty" /> value will be written.</param>
        ///<param name="values">Returns the list of float or integer values.</param>
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

        ///<summary>Retrieves the float or integer value for each [[BoundProperty].</summary>
        ///<remarks>This method throws an ArgumentException if the NativeArray is not yet created.
        ///
        ///This method throws an ArgumentException if the values list does not match the length of the boundProperties list.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to get the values from.</param>
        ///<param name="values">Returns the list of float or integer values.</param>
        public static unsafe void GetValues(NativeArray<BoundProperty> boundProperties, NativeArray<int> values)
        {
            ValidateIsCreated(boundProperties);
            ValidateIsCreated(values);
            ValidateLengthMatch(boundProperties, values);

            void* boundPropertiesPtr = NativeArrayUnsafeUtility.GetUnsafePtr(boundProperties);
            void* valuesPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(values);

            GetDiscreteValues(boundPropertiesPtr, boundProperties.Length, valuesPtr, values.Length);
        }


        ///<summary>Retrieves the float or integer value for each [[BoundProperty] and writes the value at a different index specified by the indices list.</summary>
        ///<remarks>This method throws an ArgumentException if the NativeArray is not yet created.
        ///
        ///This method throws an ArgumentException if the indices list does not match the length of the boundProperties list.
        ///
        ///This method throws an IndexOutOfRangeException if an index in the indices list is out of range.</remarks>
        ///<param name="boundProperties">The list of <see cref="BoundProperty" /> to get the values from.</param>
        ///<param name="indices">The list of indices where each <see cref="BoundProperty" /> value will be written.</param>
        ///<param name="values">Returns the list of float or integer values.</param>
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
