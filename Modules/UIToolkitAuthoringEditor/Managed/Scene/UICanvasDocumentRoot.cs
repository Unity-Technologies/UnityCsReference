// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

enum CanvasEventMode
{
    None = 0,
    Pick = 1,
    Forward = 2
}

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class UICanvasDocumentRoot : VisualElement, IVisualElementChangeProcessor
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        public new static void Register()
            => UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [], true);

        public override object CreateInstance() => new UICanvasDocumentRoot();
    }

    readonly List<VisualElementSelection> m_ElementSelections = new();
    readonly VisualElement m_HandlesContainer;
    readonly SelectionHandleManager m_HandleManager;
    PanelElement m_PanelElement;

    CanvasEventMode m_EventMode = CanvasEventMode.Pick;

    public CanvasEventMode EventMode
    {
        get => m_EventMode;
        set
        {
            if (m_EventMode == value)
                return;
            m_EventMode = value;
            switch (m_EventMode)
            {
                case CanvasEventMode.Pick:
                    m_HandlesContainer.style.display = DisplayStyle.Flex;
                    m_HandleManager.UpdateAllHandles();
                    break;
                case CanvasEventMode.Forward:
                    m_HandlesContainer.style.display = DisplayStyle.None;
                    break;
            }
        }
    }

    public PanelElement PanelElement
    {
        get => m_PanelElement;
        internal set
        {
            if (m_PanelElement == value)
                return;
            Release(m_PanelElement);
            m_PanelElement = value;
            Acquire(m_PanelElement);
        }
    }

    public override VisualElement contentContainer => null;

    public UICanvasDocumentRoot()
    {
        focusable = true;
        m_HandlesContainer = new VisualElement();
        hierarchy.Add(m_HandlesContainer);
        m_HandlesContainer.StretchToParentSize();
        m_HandleManager = new SelectionHandleManager(m_HandlesContainer);
    }

    protected override void HandleEventTrickleDown(EventBase evt)
    {
        if (EventMode == CanvasEventMode.Forward)
            PanelElement?.ForwardEventTrickleDown(evt);

        base.HandleEventTrickleDown(evt);
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                Selection.selectionChanged += OnSelectionChanged;
                break;
            case DetachFromPanelEvent:
                Selection.selectionChanged -= OnSelectionChanged;
                break;
            case GeometryChangedEvent:
                m_HandleManager.UpdateAllHandles();
                break;
            case PointerUpEvent pointerUpEvent when EventMode == CanvasEventMode.Pick:
                if (pointerUpEvent.button != 0)
                    break;

                using (ListPool<EntityId>.Get(out var nextSelection))
                using (ListPool<VisualElement>.Get(out var pickedElements))
                {
                    m_PanelElement?.SubPanel.PickAll(m_PanelElement.ConvertPosition(pointerUpEvent), pickedElements);
                    foreach (var candidate in pickedElements)
                    {
                        var element = candidate;
                        if (element.visualElementAsset == null)
                            element = element.GetFirstAncestorWhere(e => e.visualElementAsset != null);
                        var selectionObject = element?.GetSelectionObject();
                        if (selectionObject)
                            nextSelection.Add(selectionObject.GetEntityId());
                        break;
                    }

                    Selection.entityIds = nextSelection.ToArray();
                }

                break;
            default:
                if (EventMode == CanvasEventMode.Forward)
                    PanelElement?.ForwardEventBubbleUp(evt);
                break;
        }

        base.HandleEventBubbleUp(evt);
    }

    void OnSelectionChanged()
    {
        ClearSelection();

        var selectedIds = Selection.GetEntityIdsUnsafe();
        foreach (var selectedId in selectedIds)
        {
            if (EditorUtility.EntityIdToObject(selectedId) is VisualElementSelection selection)
                AddToSelection(selection);
        }
    }

    void AddToSelection(VisualElementSelection selection)
    {
        if (m_ElementSelections.Contains(selection))
            return;
        m_ElementSelections.Add(selection);
        m_HandleManager.AcquireSelectionHandle(selection);
    }

    void RemoveFromSelection(int index)
    {
        var selection = m_ElementSelections[index];
        m_ElementSelections.RemoveAt(index);
        m_HandleManager.ReleaseSelectionHandle(selection);
    }

    void ClearSelection()
    {
        for(var i = m_ElementSelections.Count - 1; i >= 0; --i)
            RemoveFromSelection(i);
    }

    void Release(PanelElement panelElement)
    {
        if (panelElement == null)
            return;

        panelElement.SubPanel?.UnregisterChangeProcessor(this);
    }

    void Acquire(PanelElement panelElement)
    {
        if (panelElement == null)
            return;

        panelElement.SubPanel?.RegisterChangeProcessor(this);
        m_HandleManager.UpdateAllHandles();
    }

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel panelElementPanel)
    {
        m_HandleManager.UpdateAllHandles();
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel panelElementPanel, AuthoringChanges changes)
    {
        using var _ = HashSetPool<VisualElementSelection>.Get(out var updateSet);
        if (panelElementPanel is PanelElement.RuntimePanel runtimePanel && changes.styleChanged.Contains(runtimePanel.Root))
        {
            foreach (var selection in m_ElementSelections)
                updateSet.Add(selection);
        }
        else
        {
            PopulateUpdateSet(m_ElementSelections, changes.styleChanged, updateSet);

        }
        foreach (var selection in updateSet)
            m_HandleManager.UpdateSelectionHandle(selection);
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel panelElementPanel)
    {
    }

    static void PopulateUpdateSet(List<VisualElementSelection> selectedList, HashSet<VisualElement> set, HashSet<VisualElementSelection> updateSet)
    {
        foreach (var selected in selectedList)
        {
            if (set.Contains(selected.Element))
                updateSet.Add(selected);
        }
    }
}
