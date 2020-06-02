// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

using UnityObject = UnityEngine.Object;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityEditor
{
    // This MUST be kept synchronized with the enum in SerializedProperty.h
    // Type of a [[SerializedProperty]].
    public enum SerializedPropertyType
    {
        //*undocumented
        Generic = -1,
        // Integer property.
        Integer = 0,
        // Boolean property.
        Boolean = 1,
        // Float property.
        Float = 2,
        // String property.
        String = 3,
        // Color property.
        Color = 4,
        // Reference to another object.
        ObjectReference = 5,
        // [[LayerMask]] property.
        LayerMask = 6,
        // Enumeration property.
        Enum = 7,
        // 2D vector property.
        Vector2 = 8,
        // 3D vector property.
        Vector3 = 9,
        // 4D vector property.
        Vector4 = 10,
        // Rectangle property.
        Rect = 11,
        // Array size property.
        ArraySize = 12,
        // Character property.
        Character = 13,
        // AnimationCurve property.
        AnimationCurve = 14,

        // Bounds property.
        Bounds = 15,

        // Gradient property.
        Gradient = 16,

        // Quaternion property.
        Quaternion = 17,

        ExposedReference = 18,

        // Fixed Buffer size property
        FixedBufferSize = 19,

        // 2D int vector property.
        Vector2Int = 20,

        // 3D int vector property.
        Vector3Int = 21,

        // Rectangle with int property.
        RectInt = 22,

        // Bounds with int property.
        BoundsInt = 23,

        // Managed reference property.
        ManagedReference = 24,
    }

    [NativeHeader("Editor/Src/Utility/SerializedProperty.h")]
    [NativeHeader("Editor/Src/Utility/SerializedProperty.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class SerializedProperty : IDisposable
    {
        IntPtr m_NativePropertyPtr;

        // This is so the garbage collector won't clean up SerializedObject behind the scenes.
        internal SerializedObject m_SerializedObject;
        internal string m_CachedLocalizedDisplayName = "";

        internal SerializedProperty() {}
        ~SerializedProperty() { Dispose(); }

        // [[SerializedObject]] this property belongs to (RO).
        public SerializedObject serializedObject { get { return m_SerializedObject; } }

        public UnityObject exposedReferenceValue
        {
            get
            {
                if (propertyType != SerializedPropertyType.ExposedReference)
                    return null;

                var defaultValue = FindPropertyRelative("defaultValue");
                if (defaultValue == null)
                    return null;

                var returnedValue = defaultValue.objectReferenceValue;

                var exposedPropertyTable = serializedObject.context as IExposedPropertyTable;
                if (exposedPropertyTable != null)
                {
                    SerializedProperty exposedName = FindPropertyRelative("exposedName");
                    var propertyName = new PropertyName(exposedName.stringValue);

                    bool propertyFoundInTable = false;
                    var objReference = exposedPropertyTable.GetReferenceValue(propertyName, out propertyFoundInTable);
                    if (propertyFoundInTable == true)
                        returnedValue = objReference;
                }
                return returnedValue;
            }

            set
            {
                if (propertyType != SerializedPropertyType.ExposedReference)
                {
                    throw new System.InvalidOperationException("Attempting to set the reference value on a SerializedProperty that is not an ExposedReference");
                }

                var defaultValue = FindPropertyRelative("defaultValue");

                var exposedPropertyTable = serializedObject.context as IExposedPropertyTable;
                if (exposedPropertyTable == null)
                {
                    defaultValue.objectReferenceValue = value;
                    defaultValue.serializedObject.ApplyModifiedProperties();
                    return;
                }

                SerializedProperty exposedName = FindPropertyRelative("exposedName");

                var exposedId = exposedName.stringValue;
                if (String.IsNullOrEmpty(exposedId))
                {
                    exposedId = UnityEditor.GUID.Generate().ToString();
                    exposedName.stringValue = exposedId;
                }
                var propertyName = new PropertyName(exposedId);
                exposedPropertyTable.SetReferenceValue(propertyName, value);
            }
        }

        internal bool isScript
        {
            get { return type == "PPtr<MonoScript>"; }
        }

        internal string localizedDisplayName
        {
            get
            {
                if (!this.isValidDisplayNameCache)
                {
                    this.isValidDisplayNameCache = true;
                    m_CachedLocalizedDisplayName = L10n.Tr(displayName);
                }
                return m_CachedLocalizedDisplayName;
            }
        }

        internal string[] enumLocalizedDisplayNames
        {
            get
            {
                string[] names = enumDisplayNames;
                var res = new string[names.Length];
                using (new LocalizationGroup(m_SerializedObject.targetObject))
                {
                    for (var i = 0; i < res.Length; ++i)
                    {
                        res[i] = L10n.Tr(names[i]);
                    }
                }
                return res;
            }
        }

        // Returns a copy of the SerializedProperty iterator in its current state. This is useful if you want to keep a reference to the current property but continue with the iteration.
        public SerializedProperty Copy()
        {
            SerializedProperty property = CopyInternal();
            property.m_SerializedObject = m_SerializedObject;
            return property;
        }

        // Retrieves the SerializedProperty at a relative path to the current property.
        public SerializedProperty FindPropertyRelative(string relativePropertyPath)
        {
            SerializedProperty prop = Copy();
            if (prop.FindPropertyRelativeInternal(relativePropertyPath))
                return prop;
            else
                return null;
        }

        // Retrieves an iterator that allows you to iterator over the current nexting of a serialized property.
        public System.Collections.IEnumerator GetEnumerator()
        {
            if (isArray)
            {
                for (int i = 0; i < arraySize; i++)
                {
                    yield return GetArrayElementAtIndex(i);
                }
            }
            else
            {
                var end = GetEndProperty();
                while (NextVisible(true) && !SerializedProperty.EqualContents(this, end))
                {
                    yield return this;
                }
            }
        }

        // Returns the element at the specified index in the array.
        public SerializedProperty GetArrayElementAtIndex(int index)
        {
            SerializedProperty prop = Copy();
            if (prop.GetArrayElementAtIndexInternal(index))
                return prop;
            else
                return null;
        }

        internal void SetToValueOfTarget(UnityObject target)
        {
            SerializedProperty targetProperty = new SerializedObject(target).FindProperty(propertyPath);
            if (targetProperty == null)
            {
                Debug.LogError(target.name + " does not have the property " + propertyPath);
                return;
            }
            switch (propertyType)
            {
                case SerializedPropertyType.Integer: intValue = targetProperty.intValue; break;
                case SerializedPropertyType.Boolean: boolValue = targetProperty.boolValue; break;
                case SerializedPropertyType.Float: floatValue = targetProperty.floatValue; break;
                case SerializedPropertyType.String: stringValue = targetProperty.stringValue; break;
                case SerializedPropertyType.Color: colorValue = targetProperty.colorValue; break;
                case SerializedPropertyType.ObjectReference: objectReferenceValue = targetProperty.objectReferenceValue; break;
                case SerializedPropertyType.LayerMask: intValue = targetProperty.intValue; break;
                case SerializedPropertyType.Enum: enumValueIndex = targetProperty.enumValueIndex; break;
                case SerializedPropertyType.Vector2: vector2Value = targetProperty.vector2Value; break;
                case SerializedPropertyType.Vector3: vector3Value = targetProperty.vector3Value; break;
                case SerializedPropertyType.Vector4: vector4Value = targetProperty.vector4Value; break;
                case SerializedPropertyType.Vector2Int: vector2IntValue = targetProperty.vector2IntValue; break;
                case SerializedPropertyType.Vector3Int: vector3IntValue = targetProperty.vector3IntValue; break;
                case SerializedPropertyType.Rect: rectValue = targetProperty.rectValue; break;
                case SerializedPropertyType.RectInt: rectIntValue = targetProperty.rectIntValue; break;
                case SerializedPropertyType.ArraySize: intValue = targetProperty.intValue; break;
                case SerializedPropertyType.Character: intValue = targetProperty.intValue; break;
                case SerializedPropertyType.AnimationCurve: animationCurveValue = targetProperty.animationCurveValue; break;
                case SerializedPropertyType.Bounds: boundsValue = targetProperty.boundsValue; break;
                case SerializedPropertyType.BoundsInt: boundsIntValue = targetProperty.boundsIntValue; break;
                case SerializedPropertyType.Gradient: gradientValue = targetProperty.gradientValue; break;
                case SerializedPropertyType.ExposedReference: exposedReferenceValue = targetProperty.exposedReferenceValue; break;
            }
        }


        extern private bool EndOfData();
        extern private void SyncSerializedObjectVersion();

        [Flags]
        internal enum VerifyFlags
        {
            None = 0,
            IteratorNotAtEnd = 1 << 1,
        }

        [MethodImpl(256)]
        internal void Verify(VerifyFlags verifyFlags = VerifyFlags.None)
        {
            if (unsafeMode)
                return;

            if (m_NativePropertyPtr == IntPtr.Zero || m_SerializedObject == null || m_SerializedObject.m_NativeObjectPtr == IntPtr.Zero)
                throw new NullReferenceException("SerializedObject of SerializedProperty has been Disposed.");

            SyncSerializedObjectVersion();

            if ((verifyFlags & VerifyFlags.IteratorNotAtEnd) == VerifyFlags.IteratorNotAtEnd && EndOfData())
                throw new InvalidOperationException("The operation is not possible when moved past all properties (Next returned false)");
        }

        // Move to next visible property.
        public bool NextVisible(bool enterChildren)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return NextVisibleInternal(enterChildren);
        }

        [NativeName("NextVisible")]
        extern private bool NextVisibleInternal(bool enterChildren);

        // Remove all elements from the array.
        public void ClearArray()
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            ClearArrayInternal();
        }

        [FreeFunction(Name = "SerializedPropertyBindings::ClearArray", HasExplicitThis = true)]
        extern private void ClearArrayInternal();

        [NativeName("FindProperty")]
        extern internal bool FindPropertyInternal(string propertyPath);

        [ThreadAndSerializationSafe()]
        public void Dispose()
        {
            if (m_NativePropertyPtr != IntPtr.Zero)
            {
                Internal_Destroy(m_NativePropertyPtr);
                m_NativePropertyPtr = IntPtr.Zero;
            }
        }

        [FreeFunction("SerializedPropertyBindings::Internal_Destroy", IsThreadSafe = true)]
        private extern static void Internal_Destroy(IntPtr ptr);

        // See if contained serialized properties are equal.
        public static bool EqualContents(SerializedProperty x, SerializedProperty y)
        {
            if (x == null)
                return (y == null || y.m_NativePropertyPtr == IntPtr.Zero);
            if (y == null)
                return (x == null || x.m_NativePropertyPtr == IntPtr.Zero);

            x.Verify();
            y.Verify();

            return EqualContentsInternal(x, y);
        }

        [FreeFunction("SerializedPropertyBindings::EqualContentsInternal")]
        private extern static bool EqualContentsInternal(SerializedProperty x, SerializedProperty y);


        // See if raw data inside both serialized property is equal.
        public static bool DataEquals(SerializedProperty x, SerializedProperty y)
        {
            if (x == null)
                return (y == null || y.m_NativePropertyPtr == IntPtr.Zero);
            if (y == null)
                return (x == null || x.m_NativePropertyPtr == IntPtr.Zero);

            x.Verify();
            y.Verify();

            return DataEqualsInternal(x, y);
        }

        [FreeFunction("SerializedPropertyBindings::DataEqualsInternal")]
        private extern static bool DataEqualsInternal(SerializedProperty x, SerializedProperty y);

        // Does this property represent multiple different values due to multi-object editing? (RO)
        public bool hasMultipleDifferentValues
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return HasMultipleDifferentValuesInternal() != 0;
            }
        }

        internal int hasMultipleDifferentValuesBitwise
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return HasMultipleDifferentValuesInternal();
            }
        }

        [NativeName("HasMultipleDifferentValues")]
        private extern int HasMultipleDifferentValuesInternal();

        internal void SetBitAtIndexForAllTargetsImmediate(int index, bool value)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            SetBitAtIndexForAllTargetsImmediateInternal(index, value);
        }

        [NativeName("SetBitAtIndexForAllTargetsImmediate")]
        private extern void SetBitAtIndexForAllTargetsImmediateInternal(int index, bool value);

        // Nice display name of the property (RO)
        public string displayName
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetMangledNameInternal();
            }
        }

        [NativeName("GetMangledName")]
        private extern string GetMangledNameInternal();

        // Name of the property (RO)
        public string name
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetNameInternal();
            }
        }

        [FreeFunction("SerializedPropertyBindings::GetNameInternal", HasExplicitThis = true)]
        private extern string GetNameInternal();

        // Type name of the property (RO)
        public string type
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetSerializedPropertyTypeNameInternal();
            }
        }

        [NativeName("GetSerializedPropertyTypeName")]
        private extern string GetSerializedPropertyTypeNameInternal();

        // Type name of the element of an Array property (RO)
        public string arrayElementType
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetSerializedPropertyArrayElementTypeNameInternal();
            }
        }

        [NativeName("GetSerializedPropertyArrayElementTypeName")]
        private extern string GetSerializedPropertyArrayElementTypeNameInternal();

        // Tooltip of the property (RO)
        public string tooltip
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetTooltipInternal();
            }
        }

        [NativeName("GetTooltip")]
        private extern string GetTooltipInternal();

        // Nesting depth of the property (RO)
        public int depth
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetDepthInternal();
            }
        }

        [NativeName("GetDepth")]
        private extern int GetDepthInternal();

        // Full path of the property (RO)
        public string propertyPath
        {
            get
            {
                Verify();
                return GetPropertyPathInternal();
            }
        }

        [NativeName("GetPropertyPath")]
        private extern string GetPropertyPathInternal();

        internal int hashCodeForPropertyPathWithoutArrayIndex
        {
            get
            {
                Verify();
                return GetHashCodeForPropertyPathWithoutArrayIndexInternal();
            }
        }

        [NativeName("GetHashCodeForPropertyPathWithoutArrayIndex")]
        private extern int GetHashCodeForPropertyPathWithoutArrayIndexInternal();

        // Is this property editable? (RO)
        public bool editable
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetEditableInternal();
            }
        }

        [NativeName("GetEditable")]
        private extern bool GetEditableInternal();

        [NativeName("IsReorderable")]
        internal extern bool IsReorderable();

        // Is this property animated? (RO)
        public bool isAnimated
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return IsAnimatedInternal();
            }
        }

        [NativeName("IsAnimated")]
        private extern bool IsAnimatedInternal();

        // Is this property candidate? (RO)
        internal bool isCandidate
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return IsCandidateInternal();
            }
        }

        [NativeName("IsCandidate")]
        private extern bool IsCandidateInternal();

        // Is this property keyed? (RO)
        internal bool isKey
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return IsKeyInternal();
            }
        }

        [NativeName("IsKey")]
        private extern bool IsKeyInternal();

        // Is this property expanded in the inspector?
        public bool isExpanded
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetIsExpandedInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetIsExpandedInternal(value);
            }
        }

        [NativeName("GetIsExpanded")]
        private extern bool GetIsExpandedInternal();

        [NativeName("SetIsExpanded")]
        private extern void SetIsExpandedInternal(bool value);

        // Does it have child properties? (RO)
        public bool hasChildren
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return HasChildrenInternal();
            }
        }

        [NativeName("HasChildren")]
        private extern bool HasChildrenInternal();

        // Does it have visible child properties? (RO)
        public bool hasVisibleChildren
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return HasVisibleChildrenInternal();
            }
        }

        [NativeName("HasVisibleChildren")]
        private extern bool HasVisibleChildrenInternal();

        // Is property part of a prefab instance? (RO)
        public bool isInstantiatedPrefab
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetIsInstantiatedPrefabInternal();
            }
        }

        [NativeName("GetIsInstantiatedPrefab")]
        private extern bool GetIsInstantiatedPrefabInternal();

        /// <summary>
        /// A property can reference any element in the parent SerializedObject.
        /// In the context of polymorphic serialization, those elements might be dynamic instances
        /// not statically discoverable from the class type.
        /// We need to take a very specific code path when we try to get the type of a field
        /// inside such a dynamic instance through a SerializedProperty.
        ///
        /// @see UnityEditor.ScriptAttributeUtility.GetFieldInfoAndStaticTypeFromProperty
        /// </summary>
        internal bool isReferencingAManagedReferenceField
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return IsReferencingAManagedReferenceFieldInternal();
            }
        }

        // Useful in the same context as 'isReferencingAManagedReferenceField'.
        [NativeName("IsReferencingAManagedReferenceField")]
        private extern bool IsReferencingAManagedReferenceFieldInternal();

        /// <summary>
        /// Returns the FQN in the format "<assembly name> <full class name>" for the current dynamic managed reference.
        /// </summary>
        /// <returns></returns>
        // Useful in the same context as 'isReferencingAManagedReferenceField'.
        [NativeName("GetFullyQualifiedTypenameForCurrentTypeTree")]
        internal extern string GetFullyQualifiedTypenameForCurrentTypeTreeInternal();

        /// <summary>
        /// Returns the path of the current field on the dynamic reference class.
        /// </summary>
        // Useful in the same context as 'isReferencingAManagedReferenceField'.
        [NativeName("GetPropertyPathInCurrentManagedTypeTree")]
        internal extern string GetPropertyPathInCurrentManagedTypeTreeInternal();

        // Is property's value different from the prefab it belongs to?
        public bool prefabOverride
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetPrefabOverrideInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetPrefabOverrideInternal(value);
            }
        }

        [NativeName("GetPrefabOverride")]
        private extern bool GetPrefabOverrideInternal();

        [NativeName("SetPrefabOverride")]
        private extern void SetPrefabOverrideInternal(bool value);

        // Is property a default override property which is enforced to always be overridden? (RO)
        public bool isDefaultOverride
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetIsDefaultOverrideInternal();
            }
        }

        [NativeName("IsDefaultOverride")]
        private extern bool GetIsDefaultOverrideInternal();

        // Is property a driven property (using RectTransform driven properties)? (RO)
        internal bool isDrivenRectTransformProperty
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetIsDrivenRectTransformPropertyInternal();
            }
        }

        [NativeName("IsDrivenRectTransformProperty")]
        private extern bool GetIsDrivenRectTransformPropertyInternal();

        // Type of this property (RO).
        public SerializedPropertyType propertyType
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return (SerializedPropertyType)GetSerializedPropertyTypeInternal();
            }
        }

        [NativeName("GetSerializedPropertyType")]
        private extern int GetSerializedPropertyTypeInternal();

        // Value of an integer property.
        public int intValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return (int)GetIntValueInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetIntValueInternal(value);
            }
        }

        [NativeName("GetIntValue")]
        private extern long GetIntValueInternal();

        [NativeName("SetIntValue")]
        private extern void SetIntValueInternal(long value);

        // Value of an long property.
        public long longValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetIntValueInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetIntValueInternal(value);
            }
        }

        // Value of a boolean property.
        public bool boolValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetBoolValueInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetBoolValueInternal(value);
            }
        }

        [NativeName("GetBoolValue")]
        private extern bool GetBoolValueInternal();

        [NativeName("SetBoolValue")]
        private extern void SetBoolValueInternal(bool value);

        // Value of a float property.
        public float floatValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return (float)GetFloatValueInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetFloatValueInternal(value);
            }
        }

        [NativeName("GetFloatValue")]
        private extern double GetFloatValueInternal();

        [NativeName("SetFloatValue")]
        private extern void SetFloatValueInternal(double value);

        // Value of a double property.
        public double doubleValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetFloatValueInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetFloatValueInternal(value);
            }
        }

        // Value of a string property.
        public string stringValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetStringValueInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetStringValueInternal(value);
            }
        }

        [NativeName("GetStringValue")]
        private extern string GetStringValueInternal();

        [NativeName("SetStringValue")]
        private extern void SetStringValueInternal(string value);

        // Value of a color property.
        public Color colorValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetColorValueInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetColorValueInternal(value);
            }
        }

        [NativeName("GetColorValue")]
        private extern Color GetColorValueInternal();

        [NativeName("SetColorValue")]
        private extern void SetColorValueInternal(Color value);

        // Value of a animation curve property.
        public AnimationCurve animationCurveValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetAnimationCurveValueCopyInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetAnimationCurveValueInternal(value);
            }
        }

        [NativeName("GetAnimationCurveValueCopy")]
        private extern AnimationCurve GetAnimationCurveValueCopyInternal();

        [NativeName("SetAnimationCurveValue")]
        private extern void SetAnimationCurveValueInternal(AnimationCurve value);

        // Value of a gradient property.
        internal Gradient gradientValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetGradientValueCopyInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetGradientValueInternal(value);
            }
        }

        [NativeName("GetGradientValueCopy")]
        private extern Gradient GetGradientValueCopyInternal();

        [NativeName("SetGradientValue")]
        private extern void SetGradientValueInternal(Gradient value);

        // Value of an object reference property.
        public UnityObject objectReferenceValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetPPtrValueInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetPPtrValueInternal(value);
            }
        }

        // Value of an object reference property.
        public object managedReferenceValue
        {
            set
            {
                if (propertyType != SerializedPropertyType.ManagedReference)
                {
                    throw new System.InvalidOperationException(
                        $"Attempting to set the managed reference value on a SerializedProperty that is set to a '{this.type}'");
                }

                // Make sure that the underlying base type is compatible with the current object
                Type type;
                var fieldInfo = UnityEditor.ScriptAttributeUtility.GetFieldInfoAndStaticTypeFromProperty(this, out type);
                var propertyBaseType = type;

                if (value != null)
                {
                    var valueType = value.GetType();
                    if (valueType == typeof(UnityObject) || valueType.IsSubclassOf(typeof(UnityObject)))
                    {
                        throw new System.InvalidOperationException(
                            $"Cannot assign an object deriving from UnityEngine.Object to a managed reference. This is not supported.");
                    }
                    else if (!propertyBaseType.IsAssignableFrom(valueType))
                    {
                        throw new System.InvalidOperationException(
                            $"Cannot assign an object of type '{valueType.Name}' to a managed reference with a base type of '{propertyBaseType.Name}': types are not compatible");
                    }
                }

                Verify(VerifyFlags.IteratorNotAtEnd);
                SetManagedReferenceValueInternal(value);
            }
            // No getter for managed reference since it adds a few notions that might be hard to reason about from the user's perspective.
            // A serializedobject is a serialized version of a UnityObject. A serialized property is a view on that serialized object.
            // All operations and all that a serialized knows about are around that serialzied view of the managed world.
            // Managed references in the context of SO/SP does not have the same semantics as C# references. All the references are
            // serialized properly but the original C# reference is lost in the process and cannot be retrieved unless e.g. cached locally
            // on the C# side (i.e. in this file). BUT this would not world properly or involve tricky reconstruction with domain reloads
            // after which all those C# references would have to be reconstructed etc.
            // So for now we dont allow getting a managed reference.

            // If we ever wanted to add one though, the approach would be to add an explicit setting that takes an reference ID and an object instance as
            // input, this would allow serialized properties to edit relations between managed references. We would also need a method to get
            // an instance from a managed reference id along with a nice safe API around it.
        }

        // Dynamic type for the current managed reference.
        public string managedReferenceFullTypename
        {
            get
            {
                if (propertyType != SerializedPropertyType.ManagedReference)
                {
                    throw new System.InvalidOperationException(
                        $"Attempting to get the managed reference full typename on a SerializedProperty that is set to a '{this.type}'");
                }
                if (serializedObject.targetObject == null)
                {
                    return null;
                }
                return GetManagedReferenceFullTypeNameInternal();
            }
        }

        // Static type for the current managed reference.
        public string managedReferenceFieldTypename
        {
            get
            {
                if (propertyType != SerializedPropertyType.ManagedReference)
                {
                    throw new System.InvalidOperationException(
                        $"Attempting to get the managed reference full typename on a SerializedProperty that is set to a '{this.type}'");
                }

                Type type;
                var fieldInfo = UnityEditor.ScriptAttributeUtility.GetFieldInfoAndStaticTypeFromProperty(this, out type);

                return $"{type.Assembly.GetName().Name} {type.FullName.Replace("+", "/")}";
            }
        }

        [NativeName("GetManagedReferenceFullTypeName")]
        private extern string GetManagedReferenceFullTypeNameInternal();

        [NativeName("SetManagedReferenceValue")]
        private extern void SetManagedReferenceValueInternal(object value);

        [NativeName("GetPPtrValue")]
        private extern UnityObject GetPPtrValueInternal();

        [NativeName("SetPPtrValue")]
        private extern void SetPPtrValueInternal(UnityObject value);

        public int objectReferenceInstanceIDValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetPPtrValueFromInstanceIDInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetPPtrValueFromInstanceIDInternal(value);
            }
        }

        [NativeName("GetPPtrInstanceID")]
        private extern int GetPPtrValueFromInstanceIDInternal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetPPtrValueFromInstanceIDInternal", HasExplicitThis = true)]
        private extern void SetPPtrValueFromInstanceIDInternal(int instanceID);

        internal string objectReferenceStringValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetPPtrStringValueInternal();
            }
        }

        [NativeName("GetPPtrStringValue")]
        private extern string GetPPtrStringValueInternal();

        internal bool ValidateObjectReferenceValue(UnityObject obj)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return ValidatePPtrValueInternal(obj);
        }

        [NativeName("ValidatePPtrValue")]
        private extern bool ValidatePPtrValueInternal(UnityObject obj);

        internal bool ValidateObjectReferenceValueExact(UnityObject obj)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return ValidatePPtrValueExact(obj);
        }

        [NativeName("ValidatePPtrValueExact")]
        private extern bool ValidatePPtrValueExact(UnityObject obj);

        internal string objectReferenceTypeString
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetPPtrClassNameInternal();
            }
        }

        [NativeName("GetPPtrClassName")]
        private extern string GetPPtrClassNameInternal();

        internal void AppendFoldoutPPtrValue(UnityObject obj)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            AppendFoldoutPPtrValueInternal(obj);
        }

        [NativeName("AppendFoldoutPPtrValue")]
        private extern void AppendFoldoutPPtrValueInternal(UnityObject obj);

        internal extern static string GetLayerMaskStringValue(UInt32 layers);

        internal UInt32 layerMaskBits
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetLayerMaskBitsInternal();
            }
        }

        [NativeName("GetLayerMaskBits")]
        private extern UInt32 GetLayerMaskBitsInternal();

        // Enum index of an enum property.
        public int enumValueIndex
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetEnumValueIndexInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetEnumValueIndexInternal(value);
            }
        }

        [NativeName("GetEnumValueIndex")]
        private extern int GetEnumValueIndexInternal();

        [NativeName("SetEnumValueIndex")]
        private extern void SetEnumValueIndexInternal(int value);

        // Names of enumeration of an enum property.
        public string[] enumNames
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetEnumNamesInternal(false);
            }
        }

        // Names of enumeration of an enum property, nicified.
        public string[] enumDisplayNames
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetEnumNamesInternal(true);
            }
        }

        [NativeName("GetEnumNames")]
        private extern string[] GetEnumNamesInternal(bool nicify);

        // Value of a 2D vector property.
        public Vector2 vector2Value
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueVector2Internal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueVector2Internal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueVector2Internal", HasExplicitThis = true)]
        extern private Vector2 GetValueVector2Internal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueVector2Internal", HasExplicitThis = true)]
        extern private void SetValueVector2Internal(Vector2 value);

        // Value of a 3D vector property.
        public Vector3 vector3Value
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueVector3Internal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueVector3Internal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueVector3Internal", HasExplicitThis = true)]
        extern private Vector3 GetValueVector3Internal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueVector3Internal", HasExplicitThis = true)]
        extern private void SetValueVector3Internal(Vector3 value);

        // Value of a 4D vector property.
        public Vector4 vector4Value
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueVector4Internal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueVector4Internal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueVector4Internal", HasExplicitThis = true)]
        extern private Vector4 GetValueVector4Internal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueVector4Internal", HasExplicitThis = true)]
        extern private void SetValueVector4Internal(Vector4 value);

        // Value of a 2D int vector property.
        public Vector2Int vector2IntValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueVector2IntInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueVector2IntInternal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueVector2IntInternal", HasExplicitThis = true)]
        extern private Vector2Int GetValueVector2IntInternal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueVector2IntInternal", HasExplicitThis = true)]
        extern private void SetValueVector2IntInternal(Vector2Int value);

        // Value of a 3D int vector property.
        public Vector3Int vector3IntValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueVector3IntInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueVector3IntInternal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueVector3IntInternal", HasExplicitThis = true)]
        extern private Vector3Int GetValueVector3IntInternal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueVector3IntInternal", HasExplicitThis = true)]
        extern private void SetValueVector3IntInternal(Vector3Int value);

        // Value of a quaternion property.
        public Quaternion quaternionValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueQuaternionInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueQuaternionInternal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueQuaternionInternal", HasExplicitThis = true)]
        extern private Quaternion GetValueQuaternionInternal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueQuaternionInternal", HasExplicitThis = true)]
        extern private void SetValueQuaternionInternal(Quaternion value);

        // Value of a rectangle property.
        public Rect rectValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueRectInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueRectInternal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueRectInternal", HasExplicitThis = true)]
        extern private Rect GetValueRectInternal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueRectInternal", HasExplicitThis = true)]
        extern private void SetValueRectInternal(Rect value);

        // Value of a rectangle int property.
        public RectInt rectIntValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueRectIntInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueRectIntInternal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueRectIntInternal", HasExplicitThis = true)]
        extern private RectInt GetValueRectIntInternal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueRectIntInternal", HasExplicitThis = true)]
        extern private void SetValueRectIntInternal(RectInt value);

        // Value of bounds property.
        public Bounds boundsValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueBoundsInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueBoundsInternal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueBoundsInternal", HasExplicitThis = true)]
        extern private Bounds GetValueBoundsInternal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueBoundsInternal", HasExplicitThis = true)]
        extern private void SetValueBoundsInternal(Bounds value);

        // Value of bounds int property.
        public BoundsInt boundsIntValue
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetValueBoundsIntInternal();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetValueBoundsIntInternal(value);
            }
        }

        [FreeFunction(Name = "SerializedPropertyBindings::GetValueBoundsIntInternal", HasExplicitThis = true)]
        extern private BoundsInt GetValueBoundsIntInternal();

        [FreeFunction(Name = "SerializedPropertyBindings::SetValueBoundsIntInternal", HasExplicitThis = true)]
        extern private void SetValueBoundsIntInternal(BoundsInt value);


        // Move to next property.
        public bool Next(bool enterChildren)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return NextInternal(enterChildren);
        }

        [FreeFunction(Name = "SerializedPropertyBindings::NextInternal", HasExplicitThis = true)]
        extern private bool NextInternal(bool enterChildren);

        // Move to first property of the object.
        public void Reset()
        {
            Verify();
            ResetInternal();
        }

        [FreeFunction(Name = "SerializedPropertyBindings::ResetInternal", HasExplicitThis = true)]
        extern private void ResetInternal();

        // Count remaining visible properties.
        public int CountRemaining()
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return CountRemainingInternal();
        }

        [NativeName("CountRemaining")]
        extern private int CountRemainingInternal();

        // Count visible children of this property, including this property itself.
        public int CountInProperty()
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return CountInPropertyInternal();
        }

        [NativeName("CountInProperty")]
        extern private int CountInPropertyInternal();

        private SerializedProperty CopyInternal()
        {
            Verify();
            return CopyInternalImpl();
        }

        [FreeFunction(Name = "SerializedPropertyBindings::CopyInternal", HasExplicitThis = true)]
        extern private SerializedProperty CopyInternalImpl();

        // Duplicates the serialized property.
        public bool DuplicateCommand()
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return DuplicateCommandInternal();
        }

        [NativeName("DuplicateCommand")]
        extern private bool DuplicateCommandInternal();

        // Deletes the serialized property.
        public bool DeleteCommand()
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return DeleteCommandInternal();
        }

        [NativeName("DeleteCommand")]
        extern private bool DeleteCommandInternal();

        // Retrieves the SerializedProperty that defines the end range of this property.
        public SerializedProperty GetEndProperty()
        {
            return GetEndProperty(false);
        }

        // Retrieves the SerializedProperty that defines the end range of this property.
        public SerializedProperty GetEndProperty(bool includeInvisible)
        {
            SerializedProperty prop = Copy();
            if (includeInvisible)
                prop.Next(false);
            else
                prop.NextVisible(false);
            return prop;
        }

        internal bool FindPropertyRelativeInternal(string propertyPath)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return FindRelativeProperty(propertyPath);
        }

        extern private bool FindRelativeProperty(string propertyPath);

        // Is this property an array? (RO)
        public bool isArray
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return IsArray();
            }
        }

        extern private bool IsArray();

        // The number of elements in the array.
        public int arraySize
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetArraySize();
            }
            set
            {
                Verify();
                ResizeArray(value);
            }
        }

        extern private int GetArraySize();
        extern private void ResizeArray(int value);

        private bool GetArrayElementAtIndexInternal(int index)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return GetArrayElementAtIndexImpl(index);
        }

        [NativeName("GetArrayElementAtIndex")]
        extern private bool GetArrayElementAtIndexImpl(int index);

        // Insert an empty element at the specified index in the array.
        // @TODO: What is the value of the element when it hasn't been set yet?
        // SA: ::ref::isArray
        public void InsertArrayElementAtIndex(int index)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            InsertArrayElementAtIndexInternal(index);
        }

        [NativeName("InsertArrayElementAtIndex")]
        extern private void InsertArrayElementAtIndexInternal(int index);

        // Delete the element at the specified index in the array.
        public void DeleteArrayElementAtIndex(int index)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            DeleteArrayElementAtIndexInternal(index);
        }

        [NativeName("DeleteArrayElementAtIndex")]
        extern private void DeleteArrayElementAtIndexInternal(int index);

        // Move an array element from srcIndex to dstIndex.
        public bool MoveArrayElement(int srcIndex, int dstIndex)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return MoveArrayElementInternal(srcIndex, dstIndex);
        }

        [NativeName("MoveArrayElement")]
        extern private bool MoveArrayElementInternal(int srcIndex, int dstIndex);

        // Is this property a fixed buffer? (RO)
        public bool isFixedBuffer
        {
            get { return IsFixedBuffer(); }
        }

        extern private bool IsFixedBuffer();

        // The number of elements in the fixed buffer (RO).
        public int fixedBufferSize
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetFixedBufferSize();
            }
        }

        extern private int GetFixedBufferSize();

        // Is the cache for a display name string valid?
        internal bool isValidDisplayNameCache
        {
            get
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                return GetIsValidDisplayNameCache();
            }
            set
            {
                Verify(VerifyFlags.IteratorNotAtEnd);
                SetIsValidDisplayNameCache(value);
            }
        }

        extern private bool GetIsValidDisplayNameCache();
        extern private void SetIsValidDisplayNameCache(bool value);

        public SerializedProperty GetFixedBufferElementAtIndex(int index)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            SerializedProperty prop = Copy();
            if (prop.GetFixedBufferAtIndexInternal(index))
                return prop;
            else
                return null;
        }

        [NativeName("GetFixedBufferElementAtIndex")]
        extern private bool GetFixedBufferAtIndexInternal(int index);

        [NativeName("AnimationCurveValueEquals")]
        extern private bool AnimationCurveValueEquals(AnimationCurve curve);

        internal bool ValueEquals(AnimationCurve curve)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return AnimationCurveValueEquals(curve);
        }

        [NativeName("GradientValueEquals")]
        extern private bool GradientValueEquals(Gradient gradient);

        internal bool ValueEquals(Gradient gradient)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return GradientValueEquals(gradient);
        }

        [NativeName("StringValueEquals")]
        extern private bool StringValueEquals(string value);

        internal bool ValueEquals(string value)
        {
            Verify(VerifyFlags.IteratorNotAtEnd);
            return StringValueEquals(value);
        }

        internal bool unsafeMode {get; set; }
        internal extern bool isValid
        {
            [NativeMethod("IsValid")]
            get;
        }
    }
}
