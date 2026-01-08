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

internal sealed partial class HierarchySelectionHandler : IVisualElementSelectionHandler
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
    private static readonly Dictionary<VisualElement, RefCountedSelection> s_SelectionMappings = new();

    Dictionary<VisualElement, RefCountedSelection> SelectionMapping => s_SelectionMappings;

    private IVisualElementEditingManager m_Manager;

    private T Acquire<T>(VisualElement element)
        where T : UISelectionObject
    {
        if (SelectionMapping.TryGetValue(element, out var refCounted))
        {
            SelectionMapping[element] = refCounted.Acquire();
            return (T)refCounted.SelectionObject;
        }

        var selectionObject = ScriptableObject.CreateInstance<T>();
        if (m_Manager == null)
            selectionObject.IsReadOnly = true;
        else
        {
            var flags = m_Manager.GetEditFlags(element);
            selectionObject.IsReadOnly = flags == VisualElementEditFlags.None;
        }
        selectionObject.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
        SelectionMapping[element] = new RefCountedSelection(selectionObject, 1);
        return selectionObject;
    }

    private void Remap(VisualElement element, UISelectionObject instance)
    {
        if (SelectionMapping.TryGetValue(element, out var refCounted))
        {
            if (refCounted.Count > 0 || !refCounted.Alive)
                Debug.LogError("Trying to remap something that is already mapped");
            return;
        }

        if (instance is VisualElementSelection visualElementSelection)
        {
            visualElementSelection.Element = element;
        }

        SelectionMapping[element] = new RefCountedSelection(instance, 0);
    }

    private bool Release(VisualElement element)
    {
        if (SelectionMapping.TryGetValue(element, out var refCounted))
        {
            if (refCounted.Count == 1)
            {
                if (refCounted.Alive)
                {
                    Undo.ClearUndo(refCounted.SelectionObject);
                    Object.DestroyImmediate(refCounted.SelectionObject);
                }
                SelectionMapping.Remove(element);
                return true;
            }

            SelectionMapping[element] = refCounted.Release();
        }

        return false;
    }

    public void SetEditingManager(IVisualElementEditingManager manager)
    {
        m_Manager = manager;
    }

    public EntityId AcquireInstanceId(VisualElement element)
    {
        if (element is IPanelComponentRootElement panelComponentRootElement)
        {
            var vtaSelection = Acquire<VisualTreeAssetSelection>(element);
            element.SetProperty(VisualElementRemapper.k_PanelComponentId, GlobalObjectId.GetGlobalObjectIdSlow((Object)panelComponentRootElement.panelComponent));
            vtaSelection.panelComponent = panelComponentRootElement.panelComponent;
            return vtaSelection.GetEntityId();
        }

        var veSelection = Acquire<VisualElementSelection>(element);
        veSelection.EditFlags = m_Manager?.GetEditFlags(element) ?? VisualElementEditFlags.None;
        veSelection.Element = element;
        return veSelection.GetEntityId();
    }

    public void ReleaseInstanceId(VisualElement element)
    {
        if (element is IPanelComponentRootElement)
        {
            element.ClearProperty(VisualElementRemapper.k_PanelComponentId);
        }
        Release(element);
    }

   public void Remap(List<VisualElementRemap> remappings)
    {
        foreach (var remap in remappings)
        {
            if (SelectionMapping.TryGetValue(remap.Previous, out var selection))
            {
                SelectionMapping[remap.Previous] = selection.Kill();
                Remap(remap.Remapped, selection.SelectionObject);
            }
        }
    }
}
