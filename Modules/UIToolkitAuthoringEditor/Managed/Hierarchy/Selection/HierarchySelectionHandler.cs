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

    private T Acquire<T>(VisualElement element)
        where T : UISelectionObject
    {
        if (s_SelectionMappings.TryGetValue(element, out var refCounted))
        {
            s_SelectionMappings[element] = refCounted.Acquire();
            return (T)refCounted.SelectionObject;
        }

        var selectionObject = ScriptableObject.CreateInstance<T>();
        selectionObject.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
        s_SelectionMappings[element] = new RefCountedSelection(selectionObject, 1);
        return selectionObject;
    }

    private void Remap(VisualElement element, UISelectionObject instance)
    {
        if (s_SelectionMappings.TryGetValue(element, out var refCounted))
        {
            if (refCounted.Count > 0 || !refCounted.Alive)
                Debug.LogError("Trying to remap something that is already mapped");
            return;
        }

        if (instance is VisualElementSelection visualElementSelection)
        {
            visualElementSelection.Element = element;
        }

        s_SelectionMappings[element] = new RefCountedSelection(instance, 0);
    }

    private bool Release(VisualElement element)
    {
        if (s_SelectionMappings.TryGetValue(element, out var refCounted))
        {
            if (refCounted.Count == 1)
            {
                if (refCounted.Alive)
                {
                    Undo.ClearUndo(refCounted.SelectionObject);
                    Object.DestroyImmediate(refCounted.SelectionObject);
                }
                s_SelectionMappings.Remove(element);
                return true;
            }

            s_SelectionMappings[element] = refCounted.Release();
        }

        return false;
    }

    public EntityId AcquireInstanceId(VisualElement element)
    {
        if (element is UIDocumentRootElement uiDocumentRootElement)
        {
            var vtaSelection = Acquire<VisualTreeAssetSelection>(element);
            element.SetProperty(VisualElementRemapper.k_UIDocumentId, GlobalObjectId.GetGlobalObjectIdSlow(uiDocumentRootElement.document));
            vtaSelection.Document = uiDocumentRootElement.document;
            return vtaSelection.GetEntityId();
        }

        var veSelection = Acquire<VisualElementSelection>(element);
        veSelection.Element = element;
        return veSelection.GetEntityId();
    }

    public void ReleaseInstanceId(VisualElement element)
    {
        if (element is UIDocumentRootElement uiDocumentRootElement)
        {
            element.ClearProperty(VisualElementRemapper.k_UIDocumentId);
        }
        Release(element);
    }

   public void Remap(List<VisualElementRemap> remappings)
    {
        foreach (var remap in remappings)
        {
            if (s_SelectionMappings.TryGetValue(remap.Previous, out var selection))
            {
                s_SelectionMappings[remap.Previous] = selection.Kill();
                Remap(remap.Remapped, selection.SelectionObject);
            }
        }
    }
}
