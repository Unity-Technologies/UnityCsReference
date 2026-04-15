// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    // This is a strain for the garbage collector (Small memory for GC, big overhead for engine)
    // Might want to add a manual dispose
    [NativeHeader("Editor/Src/Selection/ActiveEditorTracker.bindings.h")]
    [Serializable]
    [RequiredByNativeCode]
    public sealed class ActiveEditorTracker
    {
        #pragma warning disable 649
        MonoReloadableIntPtrClear m_Property;
        #pragma warning restore

        // Clears the native pointer without calling dispose.
        // Used when the pointer is borrowed and should not be freed by the finalizer.
        internal void ClearNativeHandle()
        {
            m_Property.m_IntPtr = IntPtr.Zero;
        }

        internal static event Action editorTrackerRebuilt;

        internal static class NativeHandleMarshaller
        {
            public static IntPtr ConvertToUnmanaged(ActiveEditorTracker tracker)
                => tracker != null ? tracker.m_Property.m_IntPtr : IntPtr.Zero;
        }

        // Internal constructor that skips native allocation, for creating
        // temporary wrappers around borrowed native pointers.
        internal ActiveEditorTracker(IntPtr borrowedNativeHandle)
        {
            m_Property.m_IntPtr = borrowedNativeHandle;
        }

        [FreeFunction]
        private static extern IntPtr Internal_AllocateNative();

        private void EnsureNativeAllocated()
        {
            if (m_Property.m_IntPtr == IntPtr.Zero)
                m_Property.m_IntPtr = Internal_AllocateNative();
        }

        public ActiveEditorTracker()
        {
            // m_Property is zero-initialized by MonoReloadableIntPtrClear.
            // Native allocation is lazy via EnsureNativeAllocated on first use.
        }

        public override bool Equals(object o)
        {
            var other = o as ActiveEditorTracker;
            if (ReferenceEquals(other, null))
                return false;
            return m_Property.m_IntPtr == other.m_Property.m_IntPtr;
        }

        public override int GetHashCode()
        {
            return m_Property.m_IntPtr.GetHashCode();
        }

        [FreeFunction(IsThreadSafe = true)]
        static extern void Internal_Dispose(ref IntPtr nativeHandle);
        ~ActiveEditorTracker() { Internal_Dispose(ref m_Property.m_IntPtr); }

        [FreeFunction]
        static extern void Internal_Destroy(ref IntPtr nativeHandle);
        public void Destroy() { Internal_Destroy(ref m_Property.m_IntPtr); }

        [FreeFunction]
        static extern Editor[] Internal_GetActiveEditors(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        public Editor[] activeEditors
        {
            get
            {
                EnsureNativeAllocated();
                return Internal_GetActiveEditors(this);
            }
        }

        [FreeFunction]
        [NativeName("Internal_GetActiveEditorsNonAllocInternal")]
        static extern void GetActiveEditorsNonAllocInternal(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self,
            [NotNull, Out] List<Editor> editors);

        internal static void Internal_GetActiveEditorsNonAlloc(ActiveEditorTracker self, List<Editor> editors)
        {
            self.EnsureNativeAllocated();
            GetActiveEditorsNonAllocInternal(self, editors);
        }

        // List<T> version
        internal void GetObjectsLockedByThisTracker(List<UnityObject> lockedObjects)
        {
            GetObjectsLockedByThisTrackerInternal(this, lockedObjects);
        }

        [FreeFunction]
        [NativeName("Internal_GetObjectsLockedByThisTrackerInternal")]
        static extern void GetObjectsLockedByThisTrackerInternal(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self,
            [NotNull, Out] List<UnityObject> lockedObjects);

        // List<T> version
        internal void SetObjectsLockedByThisTracker(List<UnityObject> toBeLocked)
        {
            if (toBeLocked == null)
                throw new ArgumentNullException("The list 'toBeLocked' cannot be null");
            Internal_SetObjectsLockedByThisTrackerInternal(this, toBeLocked);
        }

        [FreeFunction]
        static extern void Internal_SetObjectsLockedByThisTrackerInternal(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self,
            [NotNull] List<UnityObject> toBeLocked);

        [FreeFunction]
        static extern int Internal_GetVisible(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self, int index);
        public int GetVisible(int index)
        {
            EnsureNativeAllocated();
            return Internal_GetVisible(this, index);
        }

        [FreeFunction]
        static extern void Internal_SetVisible(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self, int index, int visible);
        public void SetVisible(int index, int visible)
        {
            EnsureNativeAllocated();
            Internal_SetVisible(this, index, visible);
        }

        [FreeFunction]
        static extern bool Internal_GetIsDirty(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        public bool isDirty
        {
            get
            {
                EnsureNativeAllocated();
                return Internal_GetIsDirty(this);
            }
        }

        [FreeFunction]
        static extern void Internal_ClearDirty(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        public void ClearDirty()
        {
            EnsureNativeAllocated();
            Internal_ClearDirty(this);
        }

        [FreeFunction]
        static extern bool Internal_GetIsLocked(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        [FreeFunction]
        static extern void Internal_SetIsLocked(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self, bool value);
        public bool isLocked
        {
            get
            {
                EnsureNativeAllocated();
                return Internal_GetIsLocked(this);
            }
            set
            {
                EnsureNativeAllocated();
                Internal_SetIsLocked(this, value);
            }
        }

        [FreeFunction]
        static extern bool Internal_HasUnsavedChanges(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker activeEditorTracker);
        public bool hasUnsavedChanges
        {
            get
            {
                EnsureNativeAllocated();
                return Internal_HasUnsavedChanges(this);
            }
        }

        [FreeFunction]
        static extern void Internal_UnsavedChangesStateChanged(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self, EntityId editorInstance, bool value);
        internal void UnsavedChangesStateChanged(Editor editor, bool value)
        {
            EnsureNativeAllocated();
            Internal_UnsavedChangesStateChanged(this, editor.GetEntityId(), value);
        }

        [FreeFunction]
        static extern bool Internal_GetDelayFlushDirtyRebuild();

        [FreeFunction]
        static extern void Internal_SetDelayFlushDirtyRebuild(bool value);

        // Enable or disable the ActiveEditorTracker rebuilding from ISceneInspector.DidFlushDirty calls.
        internal static bool delayFlushDirtyRebuild
        {
            get { return Internal_GetDelayFlushDirtyRebuild(); }
            set { Internal_SetDelayFlushDirtyRebuild(value); }
        }

        [FreeFunction]
        static extern InspectorMode Internal_GetInspectorMode(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        [FreeFunction]
        static extern void Internal_SetInspectorMode(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self, InspectorMode value);
        public InspectorMode inspectorMode
        {
            get
            {
                EnsureNativeAllocated();
                return Internal_GetInspectorMode(this);
            }
            set
            {
                EnsureNativeAllocated();
                Internal_SetInspectorMode(this, value);
            }
        }

        [FreeFunction]
        static extern bool Internal_GetHasComponentsWhichCannotBeMultiEdited(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        public bool hasComponentsWhichCannotBeMultiEdited
        {
            get
            {
                EnsureNativeAllocated();
                return Internal_GetHasComponentsWhichCannotBeMultiEdited(this);
            }
        }

        [FreeFunction]
        static extern void Internal_RebuildIfNecessary(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        public void RebuildIfNecessary()
        {
            EnsureNativeAllocated();
            Internal_RebuildIfNecessary(this);
        }

        [FreeFunction]
        static extern void Internal_RebuildAllIfNecessary();
        internal static void RebuildAllIfNecessary() { Internal_RebuildAllIfNecessary(); }

        [FreeFunction]
        static extern void Internal_ForceRebuild(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        public void ForceRebuild()
        {
            EnsureNativeAllocated();
            Internal_ForceRebuild(this);
        }

        [FreeFunction]
        static extern void Internal_VerifyModifiedMonoBehaviours(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        public void VerifyModifiedMonoBehaviours()
        {
            EnsureNativeAllocated();
            Internal_VerifyModifiedMonoBehaviours(this);
        }

        [FreeFunction]
        static extern DataMode Internal_GetDataMode(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self);
        [FreeFunction]
        static extern void Internal_SetDataMode(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ActiveEditorTracker self, DataMode mode);
        internal DataMode dataMode
        {
            get
            {
                EnsureNativeAllocated();
                return Internal_GetDataMode(this);
            }
            set
            {
                EnsureNativeAllocated();
                Internal_SetDataMode(this, value);
            }
        }

        [Obsolete("Use Editor.CreateEditor instead")]
        public static Editor MakeCustomEditor(UnityObject obj)
        {
            return Editor.CreateEditor(obj);
        }

        // Is there a custom editor for this object?
        public static bool HasCustomEditor(UnityObject obj)
        {
            return CustomEditorAttributes.FindCustomEditorType(obj, false) != null;
        }

        public static ActiveEditorTracker sharedTracker
        {
            get
            {
                var tracker = new ActiveEditorTracker();
                SetupSharedTracker(ref tracker.m_Property.m_IntPtr);
                return tracker;
            }
        }

        // Only valid and rebuilds when sharedTracker is locked
        internal static ActiveEditorTracker fallbackTracker
        {
            get
            {
                var tracker = new ActiveEditorTracker();
                SetupFallbackTracker(ref tracker.m_Property.m_IntPtr);
                return tracker;
            }
        }

        [FreeFunction("Internal_SetupSharedTracker")]
        static extern void SetupSharedTracker(ref IntPtr nativeHandle);

        [FreeFunction("Internal_SetupFallbackTracker")]
        static extern void SetupFallbackTracker(ref IntPtr nativeHandle);

        [RequiredByNativeCode]
        static void Internal_OnTrackerRebuild()
        {
            if (editorTrackerRebuilt != null)
                editorTrackerRebuilt();
        }
    }
}
