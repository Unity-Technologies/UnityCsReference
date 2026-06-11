// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class AddElementDropManipulator : Manipulator
{
    const float k_EdgeThreshold = 10f;

    readonly struct Placement
    {
        public readonly VisualElementAsset ParentVea;
        public readonly int Index;
        // The hovered element and edge flags are kept for indicator positioning.
        public readonly VisualElement HoveredElement;
        public readonly bool NearLeft;
        public readonly bool NearRight;
        public readonly bool NearTop;
        public readonly bool NearBottom;

        public bool IsEdgeDrop => NearLeft || NearRight || NearTop || NearBottom;

        public Placement(VisualElementAsset parentVea, int index, VisualElement hoveredElement,
            bool nearLeft, bool nearRight, bool nearTop, bool nearBottom)
        {
            ParentVea      = parentVea;
            Index          = index;
            HoveredElement = hoveredElement;
            NearLeft       = nearLeft;
            NearRight      = nearRight;
            NearTop        = nearTop;
            NearBottom     = nearBottom;
        }
    }

    readonly ICanvasDropContext m_DropContext;
    readonly PlacementIndicator m_Indicator = new();

    public VisualTreeAsset EditedVisualTreeAsset { get; set; }
    public Action RequestRefresh { get; set; }
    public Func<VisualTreeAsset, bool> WouldCauseCircularDependency { get; set; }

    public AddElementDropManipulator(ICanvasDropContext context)
    {
        m_DropContext = context;
        m_DropContext.OverlayLayer.Add(m_Indicator);
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        target.RegisterCallback<DragPerformEvent>(OnDragPerform);
        target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
        target.RegisterCallback<DragExitedEvent>(OnDragExited);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        m_Indicator.Hide();
        target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
        target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
        target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
        target.UnregisterCallback<DragExitedEvent>(OnDragExited);
    }

    void OnDragUpdated(DragUpdatedEvent evt)
    {
        if (DragAndDrop.GetGenericData(LibraryItem.DragDataKey) is not LibraryItem
            && !IsValidUxmlDrop())
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        var placement = ComputePlacement(evt.mousePosition);
        UpdateIndicator(placement);

        evt.StopPropagation();
    }

    void OnDragPerform(DragPerformEvent evt)
    {
        if (EditedVisualTreeAsset == null)
            return;

        if (DragAndDrop.GetGenericData(LibraryItem.DragDataKey) is LibraryItem libraryItem)
        {
            DragAndDrop.AcceptDrag();
            m_Indicator.Hide();

            var placement = ComputePlacement(evt.mousePosition);

            AddElementCommand.Execute(CommandSources.Scene, libraryItem.libraryType.type, EditedVisualTreeAsset, placement.ParentVea, placement.Index);

            RequestRefresh?.Invoke();
            evt.StopPropagation();
            return;
        }

        if (TryResolveDraggedVisualTreeAssets(out var vtas) && AreAllVisualTreeAssetsValid(vtas))
        {
            DragAndDrop.AcceptDrag();
            m_Indicator.Hide();

            var placement = ComputePlacement(evt.mousePosition);
            var parentAsset = placement.ParentVea ?? EditedVisualTreeAsset.visualTree;

            AddTemplatesToElementCommand.Execute(CommandSources.Viewport, parentAsset, placement.Index, vtas);

            using var toSelectHandle = ListPool<VisualElementAsset>.Get(out var toSelect);
            for (var i = 0; i < vtas.Length; i++)
            {
                var insertIdx = placement.Index < 0
                    ? parentAsset.childCount - vtas.Length + i
                    : placement.Index + i;
                if (insertIdx >= 0 && insertIdx < parentAsset.childCount
                    && parentAsset[insertIdx] is VisualElementAsset newVea)
                    toSelect.Add(newVea);
            }
            UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelect);

            RequestRefresh?.Invoke();
            evt.StopPropagation();
        }
    }

    void OnDragLeave(DragLeaveEvent evt)
    {
        if (DragAndDrop.GetGenericData(LibraryItem.DragDataKey) is not LibraryItem
            && DragAndDrop.paths is not { Length: > 0 })
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.None;
        m_Indicator.Hide();
    }

    void OnDragExited(DragExitedEvent evt)
    {
        m_Indicator.Hide();
    }

    void UpdateIndicator(in Placement placement)
    {
        if (placement.HoveredElement != null && placement.IsEdgeDrop)
            m_Indicator.Show(
                placement.HoveredElement,
                placement.NearLeft, placement.NearRight,
                placement.NearTop,  placement.NearBottom);
        else
            m_Indicator.Hide();
    }

    bool IsValidUxmlDrop()
        => TryResolveDraggedVisualTreeAssets(out var vtas) && AreAllVisualTreeAssetsValid(vtas);

    bool TryResolveDraggedVisualTreeAssets(out VisualTreeAsset[] vtas)
    {
        var paths = DragAndDrop.paths;
        if (paths is not { Length: > 0 })
        {
            vtas = null;
            return false;
        }

        var refs = DragAndDrop.objectReferences;
        var result = new VisualTreeAsset[paths.Length];

        for (var i = 0; i < paths.Length; i++)
        {
            var vta = (refs != null && i < refs.Length ? refs[i] : null) as VisualTreeAsset
                ?? AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(paths[i]);

            if (vta == null)
            {
                vtas = null;
                return false;
            }

            result[i] = vta;
        }

        vtas = result;
        return true;
    }

    bool AreAllVisualTreeAssetsValid(VisualTreeAsset[] vtas)
    {
        foreach (var vta in vtas)
        {
            if (!vta)
                return false;

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(vta)))
                return false;

            if (WouldCauseCircularDependency?.Invoke(vta) == true)
                return false;
        }

        return true;
    }

    Placement ComputePlacement(Vector2 mousePosition)
    {
        Vector2 subPanelPos = m_DropContext.WorldToContentPosition(mousePosition);

        using var _ = ListPool<VisualElement>.Get(out var picked);
        m_DropContext.PickAll(mousePosition, picked);

        var element = picked.Count > 0 ? picked[0] : null;
        while (element != null && (element.visualElementAsset == null || element.visualTreeAssetSource != EditedVisualTreeAsset))
            element = element.parent;

        if (element?.visualElementAsset == null)
            return new Placement(null, -1, null, false, false, false, false);

        var localPos   = element.WorldToLocal(subPanelPos);
        var rect       = element.rect;
        var nearLeft   = localPos.x < k_EdgeThreshold;
        var nearRight  = localPos.x > rect.width  - k_EdgeThreshold;
        var nearTop    = localPos.y < k_EdgeThreshold;
        var nearBottom = localPos.y > rect.height - k_EdgeThreshold;

        if (!nearLeft && !nearRight && !nearTop && !nearBottom)
            return new Placement(element.visualElementAsset, -1, element, false, false, false, false);

        // Sibling insertion — derive parent from the VEA tree, not the runtime element
        // hierarchy, because the runtime root (TemplateContainer) has no visualElementAsset.
        var siblingVea = element.visualElementAsset;
        var parentVea  = siblingVea.parentAsset as VisualElementAsset;
        if (parentVea == null)
            return new Placement(null, -1, element, nearLeft, nearRight, nearTop, nearBottom);

        var siblingIndex = siblingVea.SiblingIndex();
        if (siblingIndex < 0)
            return new Placement(parentVea, -1, element, nearLeft, nearRight, nearTop, nearBottom);

        var reverseOrder =
            element.parent.resolvedStyle.flexDirection == FlexDirection.ColumnReverse ||
            element.parent.resolvedStyle.flexDirection == FlexDirection.RowReverse;

        // Mirror Show()'s left > right > top > bottom priority so the indicator
        // position always matches the actual insertion point.
        var isBefore    = nearLeft || (!nearRight && nearTop);
        var indexOffset = isBefore
            ? (reverseOrder ? 1 : 0)
            : (reverseOrder ? 0 : 1);

        return new Placement(parentVea, siblingIndex + indexOffset, element, nearLeft, nearRight, nearTop, nearBottom);
    }
}
