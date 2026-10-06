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

        /// <summary>
        /// Create a SerializedObject targeting the object identified by <paramref name="id"/>.
        /// The id may be a Unity object's EntityId (TypeId == 0) or an ECS component EntityId
        /// (TypeId != 0). Pure entities throw
        /// <see cref="ArgumentException"/>.
        ///
        /// Note: for ECS-component targets, <see cref="UpdateIfRequiredOrScript"/> does not
        /// auto-detect external chunk mutation until the chunk change-version accessor is wired.
        /// Callers that need to pick up externally-driven changes must call <see cref="Update"/>
        /// explicitly.
        /// </summary>
        public SerializedObject(EntityId id)
        {
            m_NativeObjectPtr = InternalCreateFromEntityIds(new EntityId[] { id }, null);
        }

        /// <summary>
        /// Create a SerializedObject targeting the object identified by <paramref name="id"/>,
        /// with an undo/redo context object. <paramref name="context"/> is the target context
        /// used for undo bookkeeping, mirroring the Object-based overloads. See
        /// <see cref="SerializedObject(EntityId)"/> for the TypeId contract and the
        /// change-version limitation.
        /// </summary>
        public SerializedObject(EntityId id, Object context)
        {
            m_NativeObjectPtr = InternalCreateFromEntityIds(new EntityId[] { id }, context);
        }

        /// <summary>
        /// Create a multi-target SerializedObject over homogeneous targets identified by
        /// <paramref name="ids"/>. All entries must share the same TypeId; heterogeneous arrays
        /// (mixed Unity-object and component EntityIds, or two different component types) are
        /// rejected with <see cref="ArgumentException"/>. Pure entities are also rejected.
        ///
        /// Note: for ECS-component targets, <see cref="UpdateIfRequiredOrScript"/> does not
        /// auto-detect external chunk mutation until the chunk change-version accessor is wired.
        /// Callers that need to pick up externally-driven changes must call <see cref="Update"/>
        /// explicitly.
        /// </summary>
        public SerializedObject(EntityId[] ids)
        {
            m_NativeObjectPtr = InternalCreateFromEntityIds(ids, null);
        }

        /// <summary>
        /// Create a multi-target SerializedObject over the objects identified by
        /// <paramref name="ids"/>, with an undo/redo context object. <paramref name="context"/>
        /// is the target context used for undo bookkeeping, mirroring the Object-based overloads.
        /// See <see cref="SerializedObject(EntityId[])"/> for the homogeneity contract and the
        /// change-version limitation.
        /// </summary>
        public SerializedObject(EntityId[] ids, Object context)
        {
            m_NativeObjectPtr = InternalCreateFromEntityIds(ids, context);
        }

        internal SerializedObject(IntPtr nativeObjectPtr)
        {
            m_NativeObjectPtr = nativeObjectPtr;
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~SerializedObject() { Dispose(); }
#pragma warning restore UA5000

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

        // Find serialized property by name ignoring case.
        internal SerializedProperty FindPropertyIgnoreCase(string propertyPath)
        {
            SerializedProperty i = GetIterator_Internal();
            // This is so the garbage collector won't clean up SerializedObject behind the scenes,
            // when we are still iterating properties
            i.m_SerializedObject = this;
            if (i.FindPropertyIgnoreCaseInternal(propertyPath))
                return i;
            else
                return null;
        }

        /// <summary>
        /// Given a path of the form "managedReferences[refid].field" this finds the first field
        /// that references that reference id and returns a serialized property based on that path.
        /// For example if an object "Foo" with id 1 is referenced by multiple fields "a", "b" and
        /// "c.nested" on a MonoBehaviour (declared in that order), then calling this method with
        /// "managedReferences[1].x" would return the SerializedProperty with property path "a.x".
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal SerializedProperty FindFirstPropertyFromManagedReferencePath(string propertyPath)
        {
            SerializedProperty i = GetIterator_Internal();
            // This is so the garbage collector won't clean up SerializedObject behind the scenes,
            // when we are still iterating properties
            i.m_SerializedObject = this;
            if (i.FindFirstPropertyFromManagedReferencePathInternal(propertyPath))
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

        [NativeMethod(Name = "SerializedObjectBindings::InternalCreateFromEntityIds", IsFreeFunction = true, ThrowsException = true)]
        extern static IntPtr InternalCreateFromEntityIds(EntityId[] ids, Object context);

        // Current dense component TypeId for the managed type, or 0 if the type
        // isn't registered with TypeManagerV2. Called by Editor's post-reload
        // repair path to rewrite the stale TypeId slice of m_TargetEntityIds
        // after TypeManagerV2 reassigns TypeIds across a domain reload.
        internal static uint LookupDataComponentTypeId(Type componentType)
        {
            if (componentType == null)
                return 0;
            return LookupDataComponentTypeIdNative(componentType.TypeHandle.Value);
        }

        [NativeMethod(Name = "SerializedObjectBindings::LookupDataComponentTypeId", IsFreeFunction = true)]
        extern static uint LookupDataComponentTypeIdNative(IntPtr backendTypePtr);

        internal PropertyModification ExtractPropertyModification(string propertyPath)
        {
            return InternalExtractPropertyModification(propertyPath) as PropertyModification;
        }

        [FreeFunction("SerializedObjectBindings::ExtractPropertyModification", HasExplicitThis = true)]
        extern private object InternalExtractPropertyModification(string propertyPath);

        // The inspected object (RO). Returns null when the SerializedObject targets a
        // component (EntityId with TypeId != 0); use targetEntityId in that case.
        public extern Object targetObject { get; }

        // The inspected objects (RO). Returns an empty array when the SerializedObject
        // targets components; use targetEntityIds in that case. Mirrors targetObject's
        // null-for-component behavior at the managed layer so callers see consistent
        // Object-vs-EntityId projection across the pair.
        public Object[] targetObjects
        {
            get => isTargetDataComponent ? Array.Empty<Object>() : GetTargetObjectsInternal();
        }

        [NativeMethod("GetTargetObjects")]
        private extern Object[] GetTargetObjectsInternal();

        // True when this SerializedObject was constructed from an ECS component EntityId
        internal extern bool isTargetDataComponent
        {
            [NativeMethod("IsTargetDataComponent")]
            get;
        }

        // The inspected objects (RO).
        internal extern int targetObjectsCount { get; }

        /// <summary>
        /// The first target's [[EntityId]]. Populated for every target kind — Unity objects
        /// (TypeId == 0) and ECS components (TypeId != 0). Returns <see cref="EntityId.None"/>
        /// only when the SerializedObject has no targets. Prefer this over
        /// <see cref="targetObject"/>.GetEntityId() when the target may be a component with
        /// no backing Object.
        /// </summary>
        public extern EntityId targetEntityId { get; }

        /// <summary>
        /// Per-target EntityIds in declaration order — parallel to <see cref="targetObjects"/>
        /// for Object targets, or the sole source of target identity for component targets.
        /// Homogeneity is enforced at construction, so all entries share the same TypeId.
        /// </summary>
        public extern EntityId[] targetEntityIds { get; }

        // The context object (used to resolve scene references via ExposedReference<>)
        [NativeProperty("ContextObject")]
        public extern Object context { get; }

        internal void Cache(EntityId entityId)
        {
            CacheInternal(entityId);
            m_NativeObjectPtr = IntPtr.Zero;
        }

        [FreeFunction("SerializedObjectCache::SaveToCache", HasExplicitThis = true)]
        private extern void CacheInternal(EntityId instanceID);

        [FreeFunction("SerializedObjectCache::LoadFromCache")]
        internal extern static SerializedObject LoadFromCache(EntityId instanceID);

        public extern bool ApplyModifiedPropertiesWithoutUndo();

        // Enable/Disable live property feature globally.
        internal extern static void EnableLivePropertyFeatureGlobally(bool value);

        // Get live property global state.
        internal extern static bool GetLivePropertyFeatureGlobalState();

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

        internal extern DataMode inspectorDataMode
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

        internal extern bool IsValid();

        internal bool isValid
        {
            [VisibleToOtherModules("UnityEditor.BurstModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => m_NativeObjectPtr != IntPtr.Zero && IsValid();
        }

        internal extern uint objectVersion
        {
            [NativeMethod("GetObjectVersion")]
            get;
        }

        public extern bool forceChildVisibility
        {
            [NativeMethod("GetForceChildVisibiltyFlag")]
            get;
            [NativeMethod("SetForceChildVisibiltyFlag")]
            set;
        }

        [NativeMethod("HasAnyInstantiatedPrefabsWithValidAsset")]
        internal extern bool HasAnyInstantiatedPrefabsWithValidAsset();

        internal static bool VersionEquals(SerializedObject x, SerializedObject y)
        {
            if (x == null || y == null || x.m_NativeObjectPtr == IntPtr.Zero || y.m_NativeObjectPtr == IntPtr.Zero
                || !x.isValid || !y.isValid) return false;

            return VersionEqualsInternal(x, y);
        }

        [FreeFunction("SerializedObjectBindings::VersionEqualsInternal")]
        extern static bool VersionEqualsInternal(SerializedObject x, SerializedObject y);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(SerializedObject obj) => obj.m_NativeObjectPtr;
            public static SerializedObject ConvertToManaged(IntPtr ptr) => new SerializedObject(ptr);
        }

    }
}
