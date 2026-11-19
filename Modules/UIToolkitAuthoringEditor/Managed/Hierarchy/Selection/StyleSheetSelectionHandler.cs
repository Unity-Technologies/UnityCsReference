// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
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
internal sealed partial class StyleSheetSelectionHandler : IStyleSheetSelectionHandler
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

    readonly Dictionary<(StyleSheet styleSheet, bool isReadOnly), RefCountedSelection> m_SelectionMappings = new();
    AuthoringContext m_Context = AuthoringContext.Global;

    /// <summary>
    /// The context the selection objects created here belong to.
    /// </summary>
    public AuthoringContext Context
    {
        get => m_Context;
        set
        {
            m_Context = value ?? AuthoringContext.Global;
            foreach (var kvp in m_SelectionMappings)
            {
                if (kvp.Value.Alive)
                    kvp.Value.SelectionObject.OwnerContext = m_Context;
            }
        }
    }

    private StyleSheetSelection Acquire(StyleSheet styleSheet, bool isReadOnly)
    {
        var key = (styleSheet, isReadOnly);
        if (m_SelectionMappings.TryGetValue(key, out var refCounted))
        {
            m_SelectionMappings[key] = refCounted.Acquire();
            var existing = (StyleSheetSelection)refCounted.SelectionObject;
            existing.IsReadOnly = isReadOnly;
            return existing;
        }

        var selectionObject = ScriptableObject.CreateInstance<StyleSheetSelection>();
        selectionObject.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
        selectionObject.StyleSheet = styleSheet;
        selectionObject.IsReadOnly = isReadOnly;
        selectionObject.OwnerContext = m_Context;
        m_SelectionMappings[key] = new RefCountedSelection(selectionObject, 1);
        return selectionObject;
    }

    private void Remap(StyleSheet styleSheet, UISelectionObject instance)
    {
        // Remapping only applies to editable selections as inherited (read-only) stylesheets are never remapped.
        var key = (styleSheet, false);
        if (m_SelectionMappings.TryGetValue(key, out var refCounted))
        {
            if (refCounted.Count > 0 || !refCounted.Alive)
                Debug.LogError("Trying to remap something that is already mapped");
            return;
        }

        if (instance is StyleSheetSelection styleSheetSelection)
        {
            styleSheetSelection.StyleSheet = styleSheet;
        }

        m_SelectionMappings[key] = new RefCountedSelection(instance, 1);
    }

    private bool Release(StyleSheet styleSheet, bool isReadOnly)
    {
        var key = (styleSheet, isReadOnly);
        if (m_SelectionMappings.TryGetValue(key, out var refCounted))
        {
            if (refCounted.Count == 1 || !refCounted.Alive)
            {
                if (refCounted.Alive)
                {
                    Undo.ClearUndo(refCounted.SelectionObject);
                    Object.DestroyImmediate(refCounted.SelectionObject);
                }
                m_SelectionMappings.Remove(key);
                return true;
            }

            m_SelectionMappings[key] = refCounted.Release();
        }

        return false;
    }

    public EntityId AcquireInstanceId(StyleSheet styleSheet, bool isReadOnly)
    {
        var selection = Acquire(styleSheet, isReadOnly);
        return selection.GetEntityId();
    }

    public void ReleaseInstanceId(StyleSheet styleSheet, bool isReadOnly)
    {
        Release(styleSheet, isReadOnly);
    }

    public void Remap(List<StyleSheetRemap> remappings)
    {
        foreach (var remap in remappings)
        {
            var key = (remap.Previous, false);
            if (m_SelectionMappings.TryGetValue(key, out var selection))
            {
                m_SelectionMappings[key] = selection.Kill();
                Remap(remap.Remapped, selection.SelectionObject);
                Release(remap.Previous, false);
            }
        }
    }

    public void Clear()
    {
        foreach (var kvp in m_SelectionMappings)
        {
            if (kvp.Value.Alive)
            {
                Undo.ClearUndo(kvp.Value.SelectionObject);
                Object.DestroyImmediate(kvp.Value.SelectionObject);
            }
        }
        m_SelectionMappings.Clear();
    }
}
