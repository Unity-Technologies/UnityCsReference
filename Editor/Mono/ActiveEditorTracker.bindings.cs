// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    // This is a strain for the garbage collector (Small memory for GC, big overhead for engine)
    // Might want to add a manual dispose
    [NativeHeader("Editor/Src/Utility/ActiveEditorTracker.bindings.h")]
    [Serializable]
    public sealed class ActiveEditorTracker
    {
        #pragma warning disable 649
        MonoReloadableIntPtrClear m_Property;
        #pragma warning restore

        public ActiveEditorTracker()
        {
            Internal_Create(this);
        }

        [FreeFunction(IsThreadSafe = true)]
        static extern void Internal_Create(ActiveEditorTracker self);

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
        static extern void Internal_Dispose(ActiveEditorTracker self);
        ~ActiveEditorTracker() { Internal_Dispose(this); }

        [FreeFunction]
        static extern void Internal_Destroy(ActiveEditorTracker self);
        public void Destroy() { Internal_Destroy(this); }

        [FreeFunction]
        static extern Array Internal_GetActiveEditors(ActiveEditorTracker self);
        public Editor[] activeEditors { get { return (Editor[])Internal_GetActiveEditors(this); } }

        // List<T> version
        internal void GetObjectsLockedByThisTracker(List<UnityObject> lockedObjects)
        {
            GetObjectsLockedByThisTrackerInternal(lockedObjects);
        }

        [FreeFunction]
        static extern void Internal_GetObjectsLockedByThisTrackerInternal(ActiveEditorTracker self, [NotNull] object lockedObjects);
        internal void GetObjectsLockedByThisTrackerInternal(object lockedObjects)
        {
            Internal_GetObjectsLockedByThisTrackerInternal(this, lockedObjects);
        }

        // List<T> version
        internal void SetObjectsLockedByThisTracker(List<UnityObject> toBeLocked)
        {
            if (toBeLocked == null)
                throw new ArgumentNullException("The list 'toBeLocked' cannot be null");
            SetObjectsLockedByThisTrackerInternal(toBeLocked);
        }

        [FreeFunction]
        static extern void Internal_SetObjectsLockedByThisTrackerInternal(ActiveEditorTracker self, object toBeLocked);
        internal void SetObjectsLockedByThisTrackerInternal(object toBeLocked)
        {
            Internal_SetObjectsLockedByThisTrackerInternal(this, toBeLocked);
        }

        [FreeFunction]
        static extern int Internal_GetVisible(ActiveEditorTracker self, int index);
        public int GetVisible(int index) { return Internal_GetVisible(this, index); }

        [FreeFunction]
        static extern void Internal_SetVisible(ActiveEditorTracker self, int index, int visible);
        public void SetVisible(int index, int visible) { Internal_SetVisible(this, index, visible); }

        [FreeFunction]
        static extern bool Internal_GetIsDirty(ActiveEditorTracker self);
        public bool isDirty { get { return Internal_GetIsDirty(this); } }

        [FreeFunction]
        static extern void Internal_ClearDirty(ActiveEditorTracker self);
        public void ClearDirty() { Internal_ClearDirty(this); }

        [FreeFunction]
        static extern bool Internal_GetIsLocked(ActiveEditorTracker self);
        [FreeFunction]
        static extern void Internal_SetIsLocked(ActiveEditorTracker self, bool value);
        public bool isLocked
        {
            get { return Internal_GetIsLocked(this); }
            set { Internal_SetIsLocked(this, value); }
        }

        [FreeFunction]
        static extern InspectorMode Internal_GetInspectorMode(ActiveEditorTracker self);
        [FreeFunction]
        static extern void Internal_SetInspectorMode(ActiveEditorTracker self, InspectorMode value);
        public InspectorMode inspectorMode
        {
            get { return Internal_GetInspectorMode(this); }
            set { Internal_SetInspectorMode(this, value); }
        }

        [FreeFunction]
        static extern bool Internal_GetHasComponentsWhichCannotBeMultiEdited(ActiveEditorTracker self);
        public bool hasComponentsWhichCannotBeMultiEdited
        {
            get { return Internal_GetHasComponentsWhichCannotBeMultiEdited(this); }
        }

        [FreeFunction]
        static extern void Internal_RebuildIfNecessary(ActiveEditorTracker self);
        public void RebuildIfNecessary() { Internal_RebuildIfNecessary(this); }

        [FreeFunction]
        static extern void Internal_ForceRebuild(ActiveEditorTracker self);
        public void ForceRebuild() { Internal_ForceRebuild(this); }

        [FreeFunction]
        static extern void Internal_VerifyModifiedMonoBehaviours(ActiveEditorTracker self);
        public void VerifyModifiedMonoBehaviours() { Internal_VerifyModifiedMonoBehaviours(this); }

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
                SetupSharedTracker(tracker);
                return tracker;
            }
        }

        [FreeFunction("Internal_SetupSharedTracker")]
        static extern void SetupSharedTracker(ActiveEditorTracker sharedTracker);
    }
}
