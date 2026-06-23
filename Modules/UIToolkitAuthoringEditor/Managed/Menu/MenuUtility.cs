// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

static class MenuUtility
{
    const string k_UndoCreatePanelRenderer = "Create Panel Renderer";
    const string k_NewVisualTreeAssetDefaultName = "NewUXMLTemplate";
    const string k_StageEntryOptOutKey = "UIToolkit.AutoEnterEditingStage";
    const string k_StageEntryDialogTitle = "Open visual element editing stage";
    const string k_StageEntryDialogMessage =
        "To add this element, Unity will open the Visual Element editing stage to edit the underlying UI Document. " +
        "You can return to the previous stage by clicking the breadcrumb or pressing the back button.";

    /// <summary>
    /// When adding from a top menu, we will prioritize to find an existing <see cref="IPanelComponent"/> component in the
    /// scene or create a new one a the last root sibling and add the element to its targeted <see cref="VisualTreeAsset"/>.
    /// </summary>
    public static void AddElementAsSibling(Type elementType)
        => AddElement(elementType, addAsSibling: true, parentNewGameObjectUnderSelection: false);

    /// <summary>
    /// While adding from a context menu, we will prioritize trying to find (and possible add) a <see cref="IPanelComponent"/>
    /// component on the selected game object and add the element to its targeted <see cref="VisualTreeAsset"/>.
    /// </summary>
    public static void AddElementAsLastChild(Type elementType)
        => AddElement(elementType, addAsSibling: false, parentNewGameObjectUnderSelection: true);

    static void AddElement(Type elementType, bool addAsSibling, bool parentNewGameObjectUnderSelection)
    {
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage activeStage)
        {
            var parentVea = ResolveStageParent(activeStage, Selection.activeObject as VisualElementSelection, addAsSibling);
            ExecuteAdd(activeStage, elementType, parentVea);
            return;
        }

        if (!ConfirmStageEntry())
            return;

        UIToolkitAuthoringSettings.EnableInSceneUIAuthoring = true;

        switch (Selection.activeObject)
        {
            case VisualTreeAssetSelection { panelComponent: not null } vtaSelection
                when vtaSelection.panelComponent.visualTreeAsset:
            {
                var sceneContext = new VisualTreeAssetEditingContext(vtaSelection.panelComponent.visualTreeAsset, vtaSelection.panelComponent.panelSettings);
                EnterStageAndAdd(sceneContext, elementType, parentVea: null);
                return;
            }
            case VisualElementSelection { Element: not null } ves
                when TryBuildContextFromElement(ves.Element, out var elementContext):
            {
                var vea = GetFirstSuitableVisualElementAsset(ves.Element, elementContext.EditedVisualTreeAsset);
                if (addAsSibling)
                    vea = (VisualElementAsset)vea?.parentAsset;
                EnterStageAndAdd(elementContext, elementType, vea);
                return;
            }
        }

        AddInAppropriatePanelRendererComponent(Selection.activeGameObject, elementType, parentNewGameObjectUnderSelection);
    }

    static bool TryResolveNewVisualTreeAssetPath(out string assetPath)
    {
        if (UIToolkitAuthoringSettings.NewVisualTreeAssetLocation == NewVisualTreeAssetLocation.DefaultLocation)
        {
            var folder = "Assets/UI Toolkit";
            Directory.CreateDirectory(folder);
            assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{k_NewVisualTreeAssetDefaultName}.uxml");
            return !string.IsNullOrEmpty(assetPath);
        }

        assetPath = EditorUtility.SaveFilePanelInProject(
            "Create UI Document",
            k_NewVisualTreeAssetDefaultName,
            "uxml",
            "Choose a location for the new UI Document.");
        return !string.IsNullOrEmpty(assetPath);
    }

    static bool ConfirmStageEntry()
    {
        var postFix = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring ? "" : "\n\nthis is an experimental feature that is currently disabled, continuing will automatically enable the feature.";

        return EditorDialog.DisplayDecisionDialogWithOptOut(
            k_StageEntryDialogTitle,
            k_StageEntryDialogMessage + postFix,
            yesButtonText: "Continue",
            noButtonText: "Cancel",
            DialogOptOutDecisionType.ForThisMachine,
            k_StageEntryOptOutKey,
            DialogIconType.Info);
    }

    static void AddInAppropriatePanelRendererComponent(GameObject selectedGo, Type elementType, bool parentNewGameObjectUnderSelection)
    {
        var existingPanel = (IPanelComponent)selectedGo?.GetComponent<PanelRenderer>()
                            ?? selectedGo?.GetComponent<UIDocument>();
        var existingPanelAsComponent = (Component)existingPanel;

        if (existingPanelAsComponent)
        {
            if (existingPanel?.visualTreeAsset != null)
            {
                var ctx = new VisualTreeAssetEditingContext(existingPanel.visualTreeAsset, existingPanel.panelSettings);
                EnterStageAndAdd(ctx, elementType, parentVea: null);
                return;
            }

            // No usable VTA on the GameObject. Reuse the existing component if there is one (so we
            // don't leave an empty Panel sitting next to the new one); otherwise create a new
            // GameObject - parented under the selection only when explicitly requested.
            if (TryCreatePanelRendererAndAsset(null, existingPanel, out var newContext))
                EnterStageAndAdd(newContext, elementType, parentVea: null);
        }
        else if (parentNewGameObjectUnderSelection)
        {
            if (TryCreatePanelRendererAndAsset(selectedGo, null, out var newContext))
                EnterStageAndAdd(newContext, elementType, parentVea: null);
        }
        else
        {
            var panelComponent = FindFirstScenePanelComponent();
            if (panelComponent != null)
            {
                var sceneContext = new VisualTreeAssetEditingContext(panelComponent.visualTreeAsset, panelComponent.panelSettings);
                EnterStageAndAdd(sceneContext, elementType, parentVea: null);
                return;
            }

            if (TryCreatePanelRendererAndAsset(null, reusableComponent: null, out var newContext))
                EnterStageAndAdd(newContext, elementType, parentVea: null);
        }
    }

    static VisualElementAsset ResolveStageParent(VisualElementEditingStage stage, VisualElementSelection selection, bool addAsSibling)
    {
        var element = selection?.Element;
        if (element == null)
            return null;

        if (stage.Context.GetElementEditFlags(element) != VisualElementEditFlags.FullyEditable)
        {
            var vea = GetFirstSuitableVisualElementAsset(element, stage.EditedVisualTreeAsset);
            if (addAsSibling)
                vea = (VisualElementAsset)vea?.parentAsset;

            return vea;
        }

        var selectedVea = element.visualElementAsset;
        if (addAsSibling)
            return (VisualElementAsset) selectedVea?.parentAsset;
        return selectedVea;
    }

    static VisualElementAsset GetFirstSuitableVisualElementAsset(VisualElement candidate, VisualTreeAsset targetVta)
    {
        var current = candidate;
        while (current != null)
        {
            if (current.visualElementAsset != null && current.visualElementAsset.visualTreeAsset == targetVta)
                return current.visualElementAsset;
            current = current.hierarchy.parent;
        }

        return null;
    }

    static bool TryBuildContextFromElement(VisualElement element, out VisualTreeAssetEditingContext context)
    {
        var rootElement = element.GetFirstOfType<IPanelComponentRootElement>();
        if (rootElement?.panelComponent?.visualTreeAsset != null)
        {
            var panelComponent = rootElement.panelComponent;
            using var pathHandle = ListPool<TemplateAsset>.Get(out var path);
            element.GenerateSubDocumentPath(path);

            context = path.Count > 0
                ? new VisualTreeAssetEditingContext(panelComponent.visualTreeAsset, path.ToArray(), SubDocumentOptions.InContext, panelComponent.panelSettings)
                : new VisualTreeAssetEditingContext(panelComponent.visualTreeAsset, panelComponent.panelSettings);
            return true;
        }

        // The selection may have outlived its stage; the element is detached but still
        // remembers the VisualTreeAsset it was cloned from. Walk up to find the closest one.
        var current = element;
        while (current != null)
        {
            if (current.visualTreeAssetSource != null)
            {
                context = new VisualTreeAssetEditingContext(current.visualTreeAssetSource);
                return true;
            }
            current = current.hierarchy.parent;
        }

        context = default;
        return false;
    }

    static IPanelComponent FindFirstScenePanelComponent()
    {
        foreach (var scene in EnumerateScenesInPriorityOrder())
        {
            var renderer = FindFirstUsablePanelComponentInScene(scene);
            if (renderer != null)
                return renderer;
        }
        return null;
    }

    static IEnumerable<Scene> EnumerateScenesInPriorityOrder()
    {
        var active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.isLoaded)
            yield return active;

        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene == active || !scene.IsValid() || !scene.isLoaded)
                continue;
            yield return scene;
        }
    }

    static IPanelComponent FindFirstUsablePanelComponentInScene(Scene scene)
    {
        using var rootHandle = ListPool<GameObject>.Get(out var roots);
        scene.GetRootGameObjects(roots);

        using var componentHandle = ListPool<IPanelComponent>.Get(out var components);
        foreach (var root in roots)
        {
            components.Clear();
            root.GetComponentsInChildren(true, components);
            foreach (var component in components)
            {
                if (component.visualTreeAsset != null)
                    return component;
            }
        }
        return null;
    }

    static bool TryCreatePanelRendererAndAsset(GameObject parent, IPanelComponent reusableComponent, out VisualTreeAssetEditingContext context)
    {
        context = default;

        if (!TryResolveNewVisualTreeAssetPath(out var assetPath))
            return false;

        var folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        var contents = UIElementsTemplate.CreateUXMLTemplate(folder);
        File.WriteAllText(assetPath, contents);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        var newVta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
        if (newVta == null)
            return false;

        var defaultPanelSettings = PlayModeMenuItems.GetPanelSettingsFromProjectOrCreate();

        if ((Component)reusableComponent)
        {
            // The right-clicked GameObject already has an empty Panel*; assign the new VTA there
            // instead of creating a sibling component.
            Undo.RecordObject((Object)reusableComponent, k_UndoCreatePanelRenderer);
            reusableComponent.visualTreeAsset = newVta;
            if (reusableComponent.panelSettings == null)
                reusableComponent.panelSettings = defaultPanelSettings;
            context = new VisualTreeAssetEditingContext(newVta, reusableComponent.panelSettings);
            return true;
        }

        var name = Path.GetFileNameWithoutExtension(assetPath);
        var go = new GameObject(name);
        var panelRenderer = go.AddComponent<PanelRenderer>();
        panelRenderer.visualTreeAsset = newVta;
        panelRenderer.panelSettings = defaultPanelSettings;

        StageUtility.PlaceGameObjectInCurrentStage(go);
        Undo.RegisterCreatedObjectUndo(go, k_UndoCreatePanelRenderer);

        if (parent != null)
        {
            Undo.SetTransformParent(go.transform, parent.transform, k_UndoCreatePanelRenderer);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.layer = parent.layer;
        }

        Selection.activeGameObject = go;
        context = new VisualTreeAssetEditingContext(newVta, defaultPanelSettings);
        return true;
    }

    static void EnterStageAndAdd(VisualTreeAssetEditingContext context, Type elementType, VisualElementAsset parentVea)
    {
        var stage = VisualElementEditingStage.GoToStage(context, BreadcrumbBar.SeparatorStyle.Arrow);
        ExecuteAdd(stage, elementType, parentVea);
    }

    static void ExecuteAdd(VisualElementEditingStage stage, Type elementType, VisualElementAsset parentVea)
    {
        AddElementCommand.Execute(CommandSources.Menus, elementType, stage.EditedVisualTreeAsset, parentVea);
        stage.RequestRefresh();
    }
}
