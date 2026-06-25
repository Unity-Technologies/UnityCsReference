// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
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

        var root = element.GetFirstOfType<IPanelComponentRootElement>();
        if (root != null)
            return root.panelComponent.panelSettings;
        return null;
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

    // Convenience for the SceneView picker: given a scene-original element and the panel of an
    // active editing stage, returns the matching clone element in the stage panel (or null if
    // either side has no asset / no clone of it).
    public static VisualElement FindCorrespondingStageClone(this VisualElement sceneElement, Panel stagePanel)
    {
        var asset = sceneElement?.visualElementAsset;
        if (asset == null)
            return null;
        return stagePanel?.visualTree.FindElementByAsset(asset);
    }
}
