// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor.Utilities;

static class VisualElementUtility
{
    const string k_SelectionObjectPropertyKey = "unity-selection-object";

    public static void SetSelectionObject(this VisualElement element, UISelectionObject selectionObject)
    {
        element.SetProperty(k_SelectionObjectPropertyKey, selectionObject);
    }

    public static T GetSelectionObject<T>(this VisualElement element)
        where T : UISelectionObject
    {
        return element.GetProperty(k_SelectionObjectPropertyKey) as T;
    }

    public static UISelectionObject GetSelectionObject(this VisualElement element)
    {
        return element.GetProperty(k_SelectionObjectPropertyKey) as UISelectionObject;
    }

    public static void ClearSelectionObject(this VisualElement element)
    {
        element.ClearProperty(k_SelectionObjectPropertyKey);
    }

    public static void SetInlineBorderColor(this VisualElement element, StyleColor color)
    {
        element.style.borderTopColor = color;
        element.style.borderRightColor = color;
        element.style.borderBottomColor = color;
        element.style.borderLeftColor = color;
    }

    public static bool CanReceiveChildren(VisualElement element) => element?.contentContainer != null;

    public static void UpdateInlineRuleOnAllClones(VisualElement element, StyleSheet inlineSheet, StyleRule rule)
    {
        var asset = element.visualElementAsset;
        var ownPanel = element.panel as BaseVisualElementPanel;

        // The asset's other clones can live in any tracked panel, not just the edited element's
        using var _ = ListPool<BaseVisualElementPanel>.Get(out var panels);
        if (ownPanel != null)
            panels.Add(ownPanel);

        var registry = VisualElementSelectionRegistry.Instance;
        if (asset != null && registry != null)
        {
            using var _tracked = ListPool<Panel>.Get(out var tracked);
            registry.CollectTrackedPanels(tracked);
            foreach (var panel in tracked)
            {
                if (panel != null && !panels.Contains(panel))
                    panels.Add(panel);
            }
        }

        if (asset == null || panels.Count == 0)
        {
            Apply(element);
            return;
        }

        foreach (var panel in panels)
            panel.visualTree?.Query<VisualElement>().Where(e => e.visualElementAsset == asset).ForEach(Apply);

        // A detached element is not found by the walks but still holds the rule
        if (ownPanel == null)
            Apply(element);

        void Apply(VisualElement e)
        {
            e.UpdateInlineRule(inlineSheet, rule);
            e.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);
        }
    }

    public static void GenerateSubDocumentPath(this VisualElement element, List<TemplateAsset> templateAssetPath)
    {
        Assert.IsNotNull(templateAssetPath);

        VisualTreeAsset currentVisualTreeAsset = null;

        while (element != null)
        {
            if (element is { visualElementAsset: TemplateAsset subDocument } && currentVisualTreeAsset != element.visualTreeAssetSource)
            {
                var templateSource = (element as TemplateContainer)?.templateSource;

                // If a template source cannot be found in the hierarchy then the path is invalid
                if (templateSource == null)
                {
                    templateAssetPath.Clear();
                    return;
                }

                templateAssetPath.Add(subDocument);
                currentVisualTreeAsset = element.visualTreeAssetSource;
            }

            element = element.hierarchy.parent;
        }
        templateAssetPath.Reverse();
    }


    public static PanelSettings GetPanelSettings(this VisualElement element)
    {
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage stage)
        {
            return stage.Context.PanelSettings;
        }

        var panelComponent = element.GetFirstOfType<IPanelComponentRootElement>()?.panelComponent;
        return panelComponent.IsAlive() ? panelComponent.panelSettings : null;
    }

    // Finds the first descendant of `root` (including root itself) whose visualElementAsset
    // matches the given asset by both id AND source VisualTreeAsset. The VTA check is essential:
    // visualElementAsset.id is only stable within its containing VisualTreeAsset, so two
    // unrelated UXMLs can easily produce id collisions. Without this check, a SceneView click on
    // a panel using a different UXML could falsely map to an unrelated element in the stage.
    public static VisualElement FindElementByAsset(this VisualElement root, VisualElementAsset asset)
    {
        if (root == null || asset == null)
            return null;

        return root.Query<VisualElement>()
            .Where(e => e.visualElementAsset != null
                && e.visualElementAsset.id == asset.id
                && e.visualElementAsset.visualTreeAsset == asset.visualTreeAsset)
            .First();
    }

    // Convenience for the SceneView picker: given a scene-original element and the panel of an active
    // editing stage, returns the matching clone in the stage panel; null when the staged document is
    // unrelated (callers keep the scene element).
    public static VisualElement FindCorrespondingStageClone(this VisualElement sceneElement, Panel stagePanel)
    {
        return stagePanel?.visualTree.FindCorrespondingElement(sceneElement);
    }

    // Maps `reference` to the descendant of `root` with the same asset and in-memory id path (the
    // exact repeated-UXML instance). Paths align on the leaf: a tree rooted at the nested document
    // itself (an isolation stage, a direct panel) just lacks the outer instance ids.
    public static VisualElement FindCorrespondingElement(this VisualElement root, VisualElement reference)
    {
        var asset = reference?.visualElementAsset;
        if (root == null || asset == null)
            return null;

        using var _ = ListPool<int>.Get(out var referencePath);
        if (!VisualElementReferenceTools.TryGetInMemoryPath(reference, referencePath))
            return null;

        using var __ = ListPool<int>.Get(out var candidatePath);
        VisualElement exact = null;
        VisualElement corresponding = null;

        root.Query<VisualElement>().Where(e => AssetsMatch(e.visualElementAsset, asset)).ForEach(candidate =>
        {
            if (exact != null || !VisualElementReferenceTools.TryGetInMemoryPath(candidate, candidatePath))
                return;
            if (!PathsCorrespond(referencePath, candidatePath))
                return;

            if (candidatePath.Count == referencePath.Count)
                exact = candidate;
            else
                corresponding ??= candidate;
        });

        return exact ?? corresponding;
    }

    // Root-to-leaf id paths, compared leaf-aligned.
    static bool PathsCorrespond(List<int> a, List<int> b)
    {
        var n = Mathf.Min(a.Count, b.Count);
        for (var i = 1; i <= n; ++i)
        {
            if (a[^i] != b[^i])
                return false;
        }

        return true;
    }

    static bool AssetsMatch(VisualElementAsset a, VisualElementAsset b)
        => a != null && b != null && a.id == b.id && a.visualTreeAsset == b.visualTreeAsset;
}
