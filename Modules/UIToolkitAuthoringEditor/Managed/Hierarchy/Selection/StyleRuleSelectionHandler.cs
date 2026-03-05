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

internal sealed class StyleRuleSelectionHandler : IStyleRuleSelectionHandler
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
    private static readonly Dictionary<StyleRule, RefCountedSelection> s_SelectionMappings = new();

    Dictionary<StyleRule, RefCountedSelection> SelectionMapping => s_SelectionMappings;

    private StyleRuleSelection Acquire(StyleRule rule)
    {
        if (SelectionMapping.TryGetValue(rule, out var refCounted))
        {
            SelectionMapping[rule] = refCounted.Acquire();
            return (StyleRuleSelection)refCounted.SelectionObject;
        }

        var selectionObject = ScriptableObject.CreateInstance<StyleRuleSelection>();
        selectionObject.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
        selectionObject.StyleRule = rule;
        SelectionMapping[rule] = new RefCountedSelection(selectionObject, 1);
        return selectionObject;
    }

    private void Remap(StyleRule rule, UISelectionObject instance)
    {
        if (SelectionMapping.TryGetValue(rule, out var refCounted))
        {
            if (refCounted.Count > 0 || !refCounted.Alive)
                Debug.LogError("Trying to remap something that is already mapped");
            return;
        }

        if (instance is StyleRuleSelection styleRuleSelection)
        {
            styleRuleSelection.StyleRule = rule;
        }

        SelectionMapping[rule] = new RefCountedSelection(instance, 1);
    }

    private bool Release(StyleRule rule)
    {
        if (SelectionMapping.TryGetValue(rule, out var refCounted))
        {
            if (refCounted.Count == 1 || !refCounted.Alive)
            {
                if (refCounted.Alive)
                {
                    Undo.ClearUndo(refCounted.SelectionObject);
                    Object.DestroyImmediate(refCounted.SelectionObject);
                }
                SelectionMapping.Remove(rule);
                return true;
            }

            SelectionMapping[rule] = refCounted.Release();
        }

        return false;
    }

    public EntityId AcquireInstanceId(StyleRule rule)
    {
        var selection = Acquire(rule);
        return selection.GetEntityId();
    }

    public void ReleaseInstanceId(StyleRule rule)
    {
        Release(rule);
    }

    public void Remap(List<StyleRuleRemap> remappings)
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
