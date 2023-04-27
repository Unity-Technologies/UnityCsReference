// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using System;
using Object = UnityEngine.Object;

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor
{
    [RequiredByNativeCode]
    public struct UndoPropertyModification
    {
        public PropertyModification previousValue;
        public PropertyModification currentValue;
        [NativeName("keepPrefabOverride")]
        int m_KeepPrefabOverride;

        public bool keepPrefabOverride { get { return m_KeepPrefabOverride != 0; } set { m_KeepPrefabOverride = value ? 1 : 0; } }
    }

    [RequiredByNativeCode]
    public struct UndoRedoInfo
    {
        public string undoName;
        public int undoGroup;
        [NativeName("isRedo")]
        int m_IsRedo;

        public bool isRedo { get { return m_IsRedo != 0; } set { m_IsRedo = value ? 1 : 0; } }

    }

    internal class AtomicUndoScope : IDisposable
    {
        bool m_Disposed;

        public AtomicUndoScope()
        {
            Undo.BeginAtomicUndoGroup();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;
            Undo.EndAtomicUndoGroup();
        }
    }

    // Lets you register undo operations on specific objects you are about to perform changes on.

    [NativeHeader("Editor/Src/Undo/Undo.h")]
    [NativeHeader("Editor/Src/Undo/UndoManager.h")]
    [NativeHeader("Editor/Src/Undo/PropertyUndoManager.h")]
    [NativeHeader("Editor/Src/Undo/ObjectUndo.h")]
    [NativeHeader("Editor/Mono/Undo/Undo.bindings.h")]
    [NativeHeader("Editor/Src/Undo/AssetUndo.h")]
    public partial class Undo
    {
        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        public static extern bool isProcessing
        {
            [NativeMethod("GetIsProcessing")]
            get;
        }

        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        private static extern void GetRecordsInternal(object undoRecords, out int undoCursorPos);

        internal static void GetRecords(List<string> undoRecords, out int undoCursor)
        {
            GetRecordsInternal(undoRecords, out undoCursor);
        }

        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        private static extern void GetTimelineRecordsInternal(object undoRecords, object redoRecords);

        internal static void GetRecords(List<string> undoRecords, List<string> redoRecords)
        {
            GetTimelineRecordsInternal(undoRecords, redoRecords);
        }

        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        private static extern void GetUndoListInternal(object undoList, out int undoCursorPos);

        internal static void GetUndoList(List<string> undoList, out int undoCursor)
        {
            GetUndoListInternal(undoList, out undoCursor);
        }

        public static void RegisterCompleteObjectUndo(Object objectToUndo, string name)
        {
            Object[] objects = { objectToUndo };
            RegisterCompleteObjectUndoMultiple(objectToUndo, objects, name, 0);
        }

        public static void RegisterCompleteObjectUndo(Object[] objectsToUndo, string name)
        {
            if (objectsToUndo.Length > 0)
                RegisterCompleteObjectUndoMultiple(objectsToUndo[0], objectsToUndo, name, 0);
        }

        [NativeThrows]
        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        private static extern void RegisterCompleteObjectUndoMultiple([NotNull] Object identifier, Object[] objectsToUndo, string name, int namePriority);

        public static void SetTransformParent(Transform transform, Transform newParent, string name)
        {
            SetTransformParent(transform, newParent, true, name);
        }

        [FreeFunction("SetTransformParentUndo")]
        public static extern void SetTransformParent([NotNull] Transform transform, Transform newParent, bool worldPositionStays, string name);

        [NativeThrows]
        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        public static extern void MoveGameObjectToScene([NotNull] GameObject go, Scene scene, string name);

        // Register the state of a Unity Object so the user can later undo back to that state.
        public static void RegisterCreatedObjectUndo(Object objectToUndo, string name)
        {
            if (objectToUndo.GetType() == typeof(Transform) || objectToUndo.GetType().IsSubclassOf(typeof(Transform)))
                throw new ArgumentException("Cannot call 'RegisterCreatedObjectUndo' on Transform components, as transforms cannot be created/destroyed independently from their game object");
            RecordObjectCreation(objectToUndo, name, true);
        }

        internal static void RegisterCreatedObjectUndoToFrontOfUndoQueue(Object objectToUndo, string name)
        {
            RecordObjectCreationToFrontOfUndoQueue(objectToUndo, name);
        }

        internal static void RecordCreatedObject(Object objectToUndo, string name)
        {
            RecordObjectCreation(objectToUndo, name, false);
        }

        [FreeFunction]
        private static extern void RecordObjectCreation([NotNull] Object objectToUndo, string name, bool withLegacyHierarchyRegistration);

        [FreeFunction]
        private static extern void RecordObjectCreationToFrontOfUndoQueue([NotNull] Object objectToUndo, string name);

        public static void DestroyObjectImmediate(Object objectToUndo)
        {
            DestroyObjectUndoable(objectToUndo, "Destroy Object");
        }

        [NativeThrows]
        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        internal static extern void DestroyObjectUndoable([NotNull] Object objectToUndo, string name);

        [NativeThrows]
        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        public static extern Component AddComponent([NotNull] GameObject gameObject, Type type);

        public static T AddComponent<T>(GameObject gameObject) where T : Component
        {
            return AddComponent(gameObject, typeof(T)) as T;
        }

        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        public static extern void RegisterImporterUndo(string path, string name);

        [FreeFunction("RegisterChildrenOrderUndo")]
        public static extern void RegisterChildrenOrderUndo([NotNull] Object objectToUndo, string name);

        [FreeFunction("RegisterFileChangeUndo_Internal")]
        internal static extern void RegisterFileChangeUndo(GUID prefabToUndo, [NotNull] byte[] oldFileContent, [NotNull] byte[] newFileContent);

        [FreeFunction("RegisterFullObjectHierarchyUndo")]
        public static extern void RegisterFullObjectHierarchyUndo([NotNull] Object objectToUndo, string name);

        [Obsolete("Use Undo.RegisterFullObjectHierarchyUndo(Object, string) instead")]
        public static void RegisterFullObjectHierarchyUndo(UnityEngine.Object objectToUndo)
        {
            RegisterFullObjectHierarchyUndo(objectToUndo, "Full Object Hierarchy");
        }

        [FreeFunction("RecordUndoDiff")]
        public static extern void RecordObject(Object objectToUndo, string name);
        public static void RecordObjects(Object[] objectsToUndo, string name)
        {
            RecordObjectsInternal(objectsToUndo, objectsToUndo.Length, name);
        }

        [NativeThrows]
        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        private static extern void RecordObjectsInternal(Object[] objectToUndo, int size, string name);

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        [NativeMethod("ClearUndoIdentifier")]
        public static extern void ClearUndo(Object identifier);

        // Perform an Undo operation.
        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        [NativeMethod("Undo")]
        public static extern void PerformUndo();

        // Perform an Redo operation.
        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        [NativeMethod("Redo")]
        public static extern void PerformRedo();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern bool HasUndo();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern bool HasRedo();

        // *undocumented*
        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern void IncrementCurrentGroup();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern void BeginAtomicUndoGroup();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern void EndAtomicUndoGroup();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern int GetGroupFromStack(int undo);

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern int GetCurrentGroup();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern string GetCurrentGroupName();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern void SetCurrentGroupName(string name);

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern void RevertAllInCurrentGroup();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern void RevertAllDownToGroup(int group);

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern void CollapseUndoOperations(int groupIndex);

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern void ClearAll();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern bool ConvertSerializedData();

        [Obsolete("Use Undo.RegisterCompleteObjectUndo instead")]
        public static void RegisterUndo(Object objectToUndo, string name)
        {
            RegisterCompleteObjectUndo(objectToUndo, name);
        }

        [Obsolete("Use Undo.RegisterCompleteObjectUndo instead")]
        public static void RegisterUndo(Object[] objectsToUndo, string name)
        {
            RegisterCompleteObjectUndo(objectsToUndo, name);
        }

        // Undo/redo performed
        public delegate void UndoRedoCallback();

        /* TODO_UNDO
           This can't be properly deprecated until all packages being tested through Katana that use the old callback have been updated to use the new callback.
        [Obsolete("Use Undo.undoRedoEvent instead which provides Undo Event information", false)] */
        public static UndoRedoCallback undoRedoPerformed;
        [Obsolete("Use m_UndoRedoEvent instead", false)]
        private static DelegateWithPerformanceTracker<UndoRedoCallback> m_UndoRedoPerformedEvent = new DelegateWithPerformanceTracker<UndoRedoCallback>($"{nameof(Undo)}.{nameof(undoRedoPerformed)}");

        // Undo event
        public delegate void UndoRedoEventCallback(in UndoRedoInfo undo);

        public static UndoRedoEventCallback undoRedoEvent;
        private static DelegateWithPerformanceTracker<UndoRedoEventCallback> m_UndoRedoEvent = new DelegateWithPerformanceTracker<UndoRedoEventCallback>($"{nameof(Undo)}.{nameof(undoRedoEvent)}");

        // Called when about to flush undo recording
        public delegate void WillFlushUndoRecord();

        public static WillFlushUndoRecord willFlushUndoRecord;
        private static DelegateWithPerformanceTracker<WillFlushUndoRecord> m_WillFlushUndoRecordEvent = new DelegateWithPerformanceTracker<WillFlushUndoRecord>($"{nameof(Undo)}.{nameof(willFlushUndoRecord)}");

        [StaticAccessor("GetPropertyUndoManager()", StaticAccessorType.Dot)]
        [NativeMethod("Flush")]
        public static extern void FlushUndoRecordObjects();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        [NativeMethod("FlushTrackedObjects")]
        internal static extern void FlushTrackedObjects();

        public delegate UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications);

        public static PostprocessModifications postprocessModifications;
        private static DelegateWithPerformanceTracker<PostprocessModifications> m_PostprocessModificationsEvent = new DelegateWithPerformanceTracker<PostprocessModifications>($"{nameof(Undo)}.{nameof(postprocessModifications)}");

        internal static UndoPropertyModification[] InvokePostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (postprocessModifications == null)
                return modifications;

            var remainingModifications = modifications;

            foreach (var deleg in m_PostprocessModificationsEvent.UpdateAndInvoke(postprocessModifications))
            {
                remainingModifications = deleg.Invoke(remainingModifications);
            }

            return remainingModifications;
        }

        private static void Internal_CallWillFlushUndoRecord()
        {
            foreach (var evt in m_WillFlushUndoRecordEvent.UpdateAndInvoke(willFlushUndoRecord))
                evt();
        }

        [Obsolete("Use Undo.Internal_CallUndoRedoEvent instead")]
        private static void Internal_CallUndoRedoPerformed()
        {
            foreach (var evt in m_UndoRedoPerformedEvent.UpdateAndInvoke(undoRedoPerformed))
                evt();
        }

        private static void Internal_CallUndoRedoEvent(UndoRedoInfo undoInfo)
        {
            foreach (var evt in m_UndoRedoEvent.UpdateAndInvoke(undoRedoEvent))
                evt(undoInfo);
        }

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        [NativeMethod("ClearUndoSceneHandle")]
        internal static extern void ClearUndoSceneHandle(UnityEngine.SceneManagement.Scene scene);

        [FreeFunction("RegisterAssetsMoveUndo")]
        internal static extern void RegisterAssetsMoveUndo(string[] assetPaths);

        [Obsolete("Use Undo.RecordObject instead")]
        public static void SetSnapshotTarget(Object objectToUndo, string name) {}

        [Obsolete("Use Undo.RecordObject instead")]
        public static void SetSnapshotTarget(Object[] objectsToUndo, string name) {}

        [Obsolete("Use Undo.RecordObject instead")]
        public static void ClearSnapshotTarget() {}

        [Obsolete("Use Undo.RecordObject instead")]
        public static void CreateSnapshot() {}

        [Obsolete("Use Undo.RecordObject instead")]
        public static void RestoreSnapshot() {}

        [Obsolete("Use Undo.RecordObject instead")]
        public static void RegisterSnapshot() {}

        [Obsolete("Use DestroyObjectImmediate, RegisterCreatedObjectUndo or RegisterUndo instead.")]
        public static void RegisterSceneUndo(string name) {}
    }

    [Obsolete("Use Undo.RecordObject before modifying the object instead")]
    public class UndoSnapshot
    {
        public UndoSnapshot(Object[] objectsToUndo) {}

        public void Restore() {}

        public void Dispose() {}
    }
}
