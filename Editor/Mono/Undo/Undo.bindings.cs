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
    [NativeHeader("Editor/Src/Undo/PropertyDiffUndoRecorder.h")]
    [NativeHeader("Editor/Src/Undo/ObjectUndo.h")]
    [NativeHeader("Editor/Mono/Undo/Undo.bindings.h")]
    public partial class Undo
    {
        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        private static extern void GetRecordsInternal(object undoRecords, object redoRecords);

        internal static void GetRecords(List<string> undoRecords, List<string> redoRecords)
        {
            GetRecordsInternal(undoRecords, redoRecords);
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

        [FreeFunction("SetTransformParentUndo")]
        public static extern void SetTransformParent(Transform transform, Transform newParent, string name);

        [NativeThrows]
        [StaticAccessor("UndoBindings", StaticAccessorType.DoubleColon)]
        public static extern void MoveGameObjectToScene([NotNull] GameObject go, Scene scene, string name);

        // Register the state of a Unity Object so the user can later undo back to that state.
        [FreeFunction]
        public static extern void RegisterCreatedObjectUndo([NotNull] Object objectToUndo, string name);

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

        // *undocumented*
        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        public static extern void IncrementCurrentGroup();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern void BeginAtomicUndoGroup();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern void EndAtomicUndoGroup();

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

        public static UndoRedoCallback undoRedoPerformed;

        // Called when about to flush undo recording
        public delegate void WillFlushUndoRecord();

        public static WillFlushUndoRecord willFlushUndoRecord;

        [StaticAccessor("GetPropertyDiffUndoRecorder()", StaticAccessorType.Dot)]
        [NativeMethod("Flush")]
        public static extern void FlushUndoRecordObjects();

        public delegate UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications);

        public static PostprocessModifications postprocessModifications;

        internal static UndoPropertyModification[] InvokePostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (postprocessModifications == null)
                return modifications;

            var delegates = postprocessModifications.GetInvocationList();
            var remainingModifications = modifications;

            for (int i = 0, n = delegates.Length; i < n; ++i)
            {
                remainingModifications = ((Undo.PostprocessModifications)delegates[i]).Invoke(remainingModifications);
            }

            return remainingModifications;
        }

        private static void Internal_CallWillFlushUndoRecord()
        {
            if (willFlushUndoRecord != null)
                willFlushUndoRecord();
        }

        private static void Internal_CallUndoRedoPerformed()
        {
            if (undoRedoPerformed != null)
                undoRedoPerformed();
        }

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        [NativeMethod("ClearUndoSceneHandle")]
        internal static extern void ClearUndoSceneHandle(UnityEngine.SceneManagement.Scene scene);

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
