// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// - Implements reference-counted selection tracking
/// - Creates and manages StyleSheetSelection ScriptableObjects
/// - Returns EntityId from selection objects for hierarchy integration
/// - Handles lifecycle with AcquireInstanceId() and ReleaseInstanceId()
/// </summary>
internal sealed class StyleSheetSelectionHandler : IStyleSheetSelectionHandler
{
    private readonly struct RefCountedSelection
    {
        public RefCountedSelection(UISelectionObject selectionObject, int count, bool alive = true)
        {
            this.SelectionObject = selectionObject;
            this.Count = count;
            this.Alive = alive;
        }

        public readonly UISelectionObject SelectionObject;
        public readonly int Count;
        public readonly bool Alive;

        public RefCountedSelection Acquire()
            => new(SelectionObject, Count + 1);

        public RefCountedSelection Release()
            => new(SelectionObject, Count - 1);

        public RefCountedSelection Kill()
            => new(SelectionObject, Count, false);
    }

    [AutoStaticsCleanupOnCodeReload]
    private static readonly Dictionary<StyleSheet, RefCountedSelection> s_SelectionMappings = new();

    Dictionary<StyleSheet, RefCountedSelection> SelectionMapping => s_SelectionMappings;

    private StyleSheetSelection Acquire(StyleSheet styleSheet)
    {
        if (SelectionMapping.TryGetValue(styleSheet, out var refCounted))
        {
            SelectionMapping[styleSheet] = refCounted.Acquire();
            return (StyleSheetSelection)refCounted.SelectionObject;
        }

        var selectionObject = ScriptableObject.CreateInstance<StyleSheetSelection>();
        selectionObject.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
        selectionObject.StyleSheet = styleSheet;
        SelectionMapping[styleSheet] = new RefCountedSelection(selectionObject, 1);
        return selectionObject;
    }

    private void Remap(StyleSheet styleSheet, UISelectionObject instance)
    {
        if (SelectionMapping.TryGetValue(styleSheet, out var refCounted))
        {
            if (refCounted.Count > 0 || !refCounted.Alive)
                Debug.LogError("Trying to remap something that is already mapped");
            return;
        }

        if (instance is StyleSheetSelection styleSheetSelection)
        {
            styleSheetSelection.StyleSheet = styleSheet;
        }

        SelectionMapping[styleSheet] = new RefCountedSelection(instance, 1);
    }

    private bool Release(StyleSheet styleSheet)
    {
        if (SelectionMapping.TryGetValue(styleSheet, out var refCounted))
        {
            if (refCounted.Count == 1 || !refCounted.Alive)
            {
                if (refCounted.Alive)
                {
                    Undo.ClearUndo(refCounted.SelectionObject);
                    Object.DestroyImmediate(refCounted.SelectionObject);
                }
                SelectionMapping.Remove(styleSheet);
                return true;
            }

            SelectionMapping[styleSheet] = refCounted.Release();
        }

        return false;
    }

    public EntityId AcquireInstanceId(StyleSheet styleSheet)
    {
        var selection = Acquire(styleSheet);
        return selection.GetEntityId();
    }

    public void ReleaseInstanceId(StyleSheet styleSheet)
    {
        Release(styleSheet);
    }

    public void Remap(List<StyleSheetRemap> remappings)
    {
        foreach (var remap in remappings)
        {
            if (SelectionMapping.TryGetValue(remap.Previous, out var selection))
            {
                SelectionMapping[remap.Previous] = selection.Kill();
                Remap(remap.Remapped, selection.SelectionObject);
                Release(remap.Previous);
            }
        }
    }

    public void Clear()
    {
        foreach (var kvp in SelectionMapping)
        {
            if (kvp.Value.Alive)
            {
                Undo.ClearUndo(kvp.Value.SelectionObject);
                Object.DestroyImmediate(kvp.Value.SelectionObject);
            }
        }
        SelectionMapping.Clear();
    }
}
