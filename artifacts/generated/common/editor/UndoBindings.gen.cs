// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{


[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct UndoPropertyModification
{
    public PropertyModification previousValue;
    public PropertyModification currentValue;
    private int                 m_KeepPrefabOverride;
    
    
    public bool keepPrefabOverride { get { return m_KeepPrefabOverride != 0; } set { m_KeepPrefabOverride = value ? 1 : 0; } }
}

public sealed partial class Undo
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetRecordsInternal (object undoRecords, object redoRecords) ;

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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void RegisterCompleteObjectUndoMultiple (Object identifier, Object[] objectsToUndo, string name, int namePriority) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetTransformParent (Transform transform, Transform newParent, string name) ;

    public static void MoveGameObjectToScene (GameObject go, Scene scene, string name) {
        INTERNAL_CALL_MoveGameObjectToScene ( go, ref scene, name );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_MoveGameObjectToScene (GameObject go, ref Scene scene, string name);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RegisterCreatedObjectUndo (Object objectToUndo, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DestroyObjectImmediate (Object objectToUndo) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Component AddComponent (GameObject gameObject, Type type) ;

    public static T AddComponent<T>(GameObject gameObject) where T : Component
        {
            return AddComponent(gameObject, typeof(T)) as T;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RegisterFullObjectHierarchyUndo (Object objectToUndo, string name) ;

    [System.Obsolete ("Use Undo.RegisterFullObjectHierarchyUndo(Object, string) instead")]
public static void RegisterFullObjectHierarchyUndo(UnityEngine.Object objectToUndo)
        {
            RegisterFullObjectHierarchyUndo(objectToUndo, "Full Object Hierarchy");
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RecordObject (Object objectToUndo, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RecordObjects (Object[] objectsToUndo, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearUndo (Object identifier) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void PerformUndo () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void PerformRedo () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void IncrementCurrentGroup () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetCurrentGroup () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetCurrentGroupName () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetCurrentGroupName (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RevertAllInCurrentGroup () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RevertAllDownToGroup (int group) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CollapseUndoOperations (int groupIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearAll () ;

    [System.Obsolete ("Use Undo.RegisterCompleteObjectUndo instead")]
public static void RegisterUndo(Object objectToUndo, string name)
        {
            RegisterCompleteObjectUndo(objectToUndo, name);
        }
    
    
    [System.Obsolete ("Use Undo.RegisterCompleteObjectUndo instead")]
public static void RegisterUndo(Object[] objectsToUndo, string name)
        {
            RegisterCompleteObjectUndo(objectsToUndo, name);
        }
    
    
    public delegate void UndoRedoCallback();
    public static UndoRedoCallback undoRedoPerformed;
    
    
    public delegate void WillFlushUndoRecord();
    public static WillFlushUndoRecord willFlushUndoRecord;
    
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void FlushUndoRecordObjects () ;

    public delegate UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications);
    
    
    public static PostprocessModifications postprocessModifications;
    
    
    private static UndoPropertyModification[] InvokePostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (postprocessModifications != null)
                return postprocessModifications(modifications);
            else
                return modifications;
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
    
    
    [System.Obsolete ("Use Undo.RecordObject instead")]
public static void SetSnapshotTarget(Object objectToUndo, string name) {}
    [System.Obsolete ("Use Undo.RecordObject instead")]
public static void SetSnapshotTarget(Object[] objectsToUndo, string name) {}
    [System.Obsolete ("Use Undo.RecordObject instead")]
public static void ClearSnapshotTarget() {}
    [System.Obsolete ("Use Undo.RecordObject instead")]
public static void CreateSnapshot() {}
    [System.Obsolete ("Use Undo.RecordObject instead")]
public static void RestoreSnapshot() {}
    [System.Obsolete ("Use Undo.RecordObject instead")]
public static void RegisterSnapshot() {}
    [System.Obsolete ("Use DestroyObjectImmediate, RegisterCreatedObjectUndo or RegisterUndo instead.")]
public static void RegisterSceneUndo(string name) {}
    
    
}

[System.Obsolete ("Use Undo.RecordObject before modifying the object instead")]
public sealed partial class UndoSnapshot
{
    public UndoSnapshot(Object[] objectsToUndo) {}
    
    
    public void Restore() {}
    
    
    public void Dispose() {}
}

}
