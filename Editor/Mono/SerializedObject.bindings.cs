// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Utility/SerializedProperty.h")]
    [NativeHeader("Editor/Src/Utility/SerializedObject.bindings.h")]
    [NativeHeader("Editor/Src/Utility/SerializedObjectCache.h")]

    // SerializedObject and [[SerializedProperty]] are classes for editing properties on objects in a completely generic way that automatically handles undo and styling UI for prefabs.

    public class SerializedObject : IDisposable
    {
        #pragma warning disable 414
        internal IntPtr m_NativeObjectPtr;

        // Create SerializedObject for inspected object.
        public SerializedObject(Object obj)
        {
            m_NativeObjectPtr = InternalCreate(new Object[] {obj}, null);
        }

        public SerializedObject(Object obj, Object context)
        {
            m_NativeObjectPtr = InternalCreate(new Object[] {obj}, context);
        }

        // Create SerializedObject for inspected object.
        public SerializedObject(Object[] objs)
        {
            m_NativeObjectPtr = InternalCreate(objs, null);
        }

        public SerializedObject(Object[] objs, Object context)
        {
            m_NativeObjectPtr = InternalCreate(objs, context);
        }

        ~SerializedObject() { Dispose(); }

        [ThreadAndSerializationSafe()]
        public void Dispose()
        {
            if (m_NativeObjectPtr != IntPtr.Zero)
            {
                Internal_Destroy(m_NativeObjectPtr);
                m_NativeObjectPtr = IntPtr.Zero;
            }
        }

        [FreeFunction("SerializedObjectBindings::Internal_Destroy", IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        // Get the first serialized property.
        public SerializedProperty GetIterator()
        {
            SerializedProperty i = GetIterator_Internal();
            // This is so the garbage collector won't clean up SerializedObject behind the scenes,
            // when we are still iterating properties
            i.m_SerializedObject = this;
            return i;
        }

        // Find serialized property by name.
        public SerializedProperty FindProperty(string propertyPath)
        {
            SerializedProperty i = GetIterator_Internal();
            // This is so the garbage collector won't clean up SerializedObject behind the scenes,
            // when we are still iterating properties
            i.m_SerializedObject = this;
            if (i.FindPropertyInternal(propertyPath))
                return i;
            else
                return null;
        }

        extern public bool ApplyModifiedProperties();

        // Update /hasMultipleDifferentValues/ cache on the next /Update()/ call.
        extern public void SetIsDifferentCacheDirty();

        [FreeFunction(Name = "SerializedObjectBindings::GetIteratorInternal", HasExplicitThis = true)]
        extern private SerializedProperty GetIterator_Internal();

        // Update serialized object's representation.
        extern public void Update();

        // Update serialized object's representation, only if the object has been modified since the last call to Update or if it is a script.
        [Obsolete("UpdateIfDirtyOrScript has been deprecated. Use UpdateIfRequiredOrScript instead.", false)]
        [NativeName("UpdateIfRequiredOrScript")]
        extern public void UpdateIfDirtyOrScript();

        // Update serialized object's representation, only if the object has been modified since the last call to Update or if it is a script.
        extern public bool UpdateIfRequiredOrScript();

        // Updates this serialized object's isExpanded value to the global inspector's expanded state for this object
        extern internal void UpdateExpandedState();

        [NativeMethod(Name = "SerializedObjectBindings::InternalCreate", IsFreeFunction = true, ThrowsException = true)]
        extern static IntPtr InternalCreate(Object[] monoObjs, Object context);

        internal PropertyModification ExtractPropertyModification(string propertyPath)
        {
            return InternalExtractPropertyModification(propertyPath) as PropertyModification;
        }

        [FreeFunction("SerializedObjectBindings::ExtractPropertyModification", HasExplicitThis = true)]
        extern private object InternalExtractPropertyModification(string propertyPath);

        // The inspected object (RO).
        public extern Object targetObject { get; }

        // The inspected objects (RO).
        public extern Object[] targetObjects { get; }

        // The inspected objects (RO).
        internal extern int targetObjectsCount { get; }

        // The context object (used to resolve scene references via ExposedReference<>)
        [NativeProperty("ContextObject")]
        public extern Object context { get; }

        internal void Cache(int instanceID)
        {
            CacheInternal(instanceID);
            m_NativeObjectPtr = IntPtr.Zero;
        }

        [FreeFunction("SerializedObjectCache::SaveToCache", HasExplicitThis = true)]
        private extern void CacheInternal(int instanceID);

        [FreeFunction("SerializedObjectCache::LoadFromCache")]
        internal extern static SerializedObject LoadFromCache(int instanceID);

        public extern bool ApplyModifiedPropertiesWithoutUndo();

        // Copies a value from a SerializedProperty to the same serialized property on this serialized object.
        public void CopyFromSerializedProperty(SerializedProperty prop)
        {
            if (prop == null)
                throw new ArgumentNullException("prop");

            prop.Verify(SerializedProperty.VerifyFlags.IteratorNotAtEnd);
            CopyFromSerializedPropertyInternal(prop);
        }

        [FreeFunction("SerializedObjectBindings::CopyFromSerializedPropertyInternal", HasExplicitThis = true)]
        private extern void CopyFromSerializedPropertyInternal(SerializedProperty prop);

        // Copies a value from a SerializedProperty to the same serialized property on this serialized object.
        public bool CopyFromSerializedPropertyIfDifferent(SerializedProperty prop)
        {
            if (prop == null)
                throw new ArgumentNullException("prop");

            prop.Verify(SerializedProperty.VerifyFlags.IteratorNotAtEnd);
            return CopyFromSerializedPropertyIfDifferentInternal(prop);
        }

        [FreeFunction("SerializedObjectBindings::CopyFromSerializedPropertyIfDifferentInternal", HasExplicitThis = true)]
        private extern bool CopyFromSerializedPropertyIfDifferentInternal(SerializedProperty prop);

        public extern bool hasModifiedProperties
        {
            [NativeMethod("HasModifiedProperties")]
            get;
        }

        internal extern InspectorMode inspectorMode
        {
            get;
            set;
        }

        // Does the serialized object represents multiple objects due to multi-object editing? (RO)
        public extern bool isEditingMultipleObjects
        {
            [NativeMethod("IsEditingMultipleObjects")]
            get;
        }

        public extern int maxArraySizeForMultiEditing
        {
            get;
            set;
        }

        internal extern bool isValid
        {
            [NativeMethod("IsValid")]
            get;
        }
    }
}
