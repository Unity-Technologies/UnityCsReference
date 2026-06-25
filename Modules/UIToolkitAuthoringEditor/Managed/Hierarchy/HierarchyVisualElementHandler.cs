// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;

namespace Unity.UIToolkit.Editor;

[UsedImplicitly]
internal sealed partial class HierarchyVisualElementHandler : VisualElementNodeTypeHandler
{
    [OnCodeLoaded, UsedImplicitly]
    private static void RegisterHierarchyHandlers()
    {
        HierarchyWindow.RegisterNodeTypeHandler<HierarchyVisualElementHandler>();
    }

    [InitializeOnLoadMethod, UsedImplicitly]
    private static void RegisterStageHandlers()
    {
        StageNavigationManager.instance.stageChanging += OnStageWillChange;
    }

    [OnCodeUnloading, UsedImplicitly]
    private static void UnregisterHierarchyHandlers()
    {
        HierarchyWindow.UnregisterNodeTypeHandler<HierarchyVisualElementHandler>();
        HierarchyWindow.UnregisterNodeTypeHandler<VisualElementEditingNodeHandler>();
    }

    [/*BeforeManagedObjectsDisabled,*/ UsedImplicitly]
    private static void UnregisterStageHandlers()
    {
        StageNavigationManager.instance.stageChanging -= OnStageWillChange;
    }

    private HierarchyGameObjectHandler m_GameObjectHandler;

    public HierarchyVisualElementHandler()
        : base(new HierarchySelectionHandler())
    {
        UIElementsRuntimeUtility.onCreatePanel += RegisterPanelIfEnabled;
        UIElementsRuntimeUtility.onWillDestroyPanel += UnregisterPanelIfEnabled;

        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged += EnableHierarchyIntegration;
    }

    protected override void Initialize()
    {
        base.Initialize();
        m_GameObjectHandler = Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>();
        EnableHierarchyIntegration(UIToolkitAuthoringSettings.EnableInSceneUIAuthoring, false);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        UIElementsRuntimeUtility.onCreatePanel -= RegisterPanelIfEnabled;
        UIElementsRuntimeUtility.onWillDestroyPanel -= UnregisterPanelIfEnabled;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged -= EnableHierarchyIntegration;
    }

    protected override NodeCreationType ShouldCreateNode(VisualElement element)
    {
        return element is PanelRootElement
            ? NodeCreationType.CreateChildren
            : base.ShouldCreateNode(element);
    }

    protected override string GetDisplayName(HierarchyView view, in HierarchyNode node, VisualElement target)
    {
        if (target is IPanelComponentRootElement rootElement)
        {
            var vta = rootElement.panelComponent.visualTreeAsset;
            if (!vta)
            {
                return "<none>.uxml";
            }

            var path = AssetDatabase.GetAssetPath(vta);
            if (string.IsNullOrEmpty(path))
            {
                if (string.IsNullOrEmpty(vta.name))
                    return "<unsaved file>.uxml";

                return $"{vta.name}.uxml";
            }

            return Path.GetFileName(path);
        }

        return base.GetDisplayName(view, in node, target);
    }

    protected override void Bind(HierarchyViewItem item, VisualElement element)
    {
        if (element is IPanelComponentRootElement)
        {
            // We just want to display the name of the file.
            return;
        }

        base.Bind(item, element);
    }

    protected override void BindNavigation(HierarchyViewItem item, VisualElement container)
    {
        base.BindNavigation(item, container);
        SetStageNodeNavigation(item, container);
    }

    protected override void UnbindNavigation(HierarchyViewItem item, VisualElement container)
    {
        base.UnbindNavigation(item, container);
        UnsetStageNodeNavigation(item);
    }

    protected override void PopulateContextMenu(HierarchyView view, in HierarchyNode node, VisualElement element, DropdownMenu menu)
    {
        IPanelComponent panelComponent;
        VisualTreeAsset vtaSource;
        VisualElementAsset vea;

        if (element is IPanelComponentRootElement rootElement)
        {
            panelComponent = rootElement.panelComponent;
            vtaSource = panelComponent.visualTreeAsset;
            vea = vtaSource?.visualTree;
        }
        else
        {
            panelComponent = element.GetFirstAncestorOfType<IPanelComponentRootElement>().panelComponent;
            vtaSource = panelComponent.visualTreeAsset;
            vea = element.visualElementAsset;

            if (vea == null)
            {
                vea = element.GetFirstAncestorWhere(ve => ve.visualElementAsset != null).visualElementAsset;
            }
        }

        var isPanelComponentRootElement = element is IPanelComponentRootElement;

        var ancestorInstances = new List<TemplateAsset>();

        element.GenerateSubDocumentPath(ancestorInstances);

        if (isPanelComponentRootElement)
        {
            menu.AppendAction(
                "Select VisualTreeAsset Asset",
                ma => { EditorGUIUtility.PingObject(ma.userData as VisualTreeAsset); },
                ma => (ma.userData as VisualTreeAsset) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                vtaSource);
        }

        if (vtaSource != null)
        {
            // For a TemplateContainer, Open in UI Builder navigates to the template's own file, not the containing document.
            // The root element must be checked first: it is also a TemplateContainer but has no backing TemplateAsset.
            VisualTreeAsset openInBuilderVta;
            int openInBuilderSelectedId;
            if (isPanelComponentRootElement)
            {
                openInBuilderVta = vtaSource;
                openInBuilderSelectedId = vea?.id ?? -1;
            }
            else if (element is TemplateContainer templateContainer)
            {
                openInBuilderVta = (templateContainer.visualElementAsset as TemplateAsset)?.ResolveTemplate() ?? templateContainer.templateSource ?? vtaSource;
                openInBuilderSelectedId = -1;
            }
            else
            {
                openInBuilderVta = element.visualTreeAssetSource
                    ? element.visualTreeAssetSource
                    : element.GetFirstAncestorWhere(ve => ve.visualTreeAssetSource)?.visualTreeAssetSource ?? vtaSource;
                openInBuilderSelectedId = vea?.id ?? -1;
            }

            StageContextMenuUtility.PopulateOpenActions(menu, element, openInBuilderVta, openInBuilderSelectedId, ancestorInstances);

            if (ancestorInstances.Count == 0)
            {
                menu.AppendAction(
                    "Open Asset",
                    _ =>
                    {
                        VisualElementEditingStage.GoToStage(new VisualTreeAssetEditingContext(
                            vtaSource,
                            element.GetPanelSettings()
                        ), BreadcrumbBar.SeparatorStyle.Arrow);
                        UIToolkitStageUtility.RequestSelectionOnNextUpdate(new[] { vea });
                    });
            }

            var canBeReferenced = VisualElementReferenceTools.TryCreateReference(element, out var pr, out var authoringIdPath, false, true) && authoringIdPath.path.Length > 0;
            menu.AppendAction(
                "Find References In Scene",
                a =>
                {
                    var prId = pr.GetEntityId().GetHashCode();
                    var pathString = authoringIdPath.PathToCsvString(VisualElementReferenceSceneQueryEngineFilter.PathSeperatorToken);
                    var filter = $"ref={prId} {VisualElementReferenceSceneQueryEngineFilter.FilterId}=[{pathString}]";
                    SearchableEditorWindow.SetSearchText(filter, HierarchyType.GameObjects);
                },
                canBeReferenced ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        menu.AppendSeparator();
        StageContextMenuUtility.PopulateElementOperations(menu);
    }
    protected override bool TryGetParentNode(VisualElement element, out HierarchyNode parentNode)
    {
        if (element is not IPanelComponentRootElement rootElement)
            return base.TryGetParentNode(element, out parentNode);

        var panelComponentGameObject = rootElement.panelComponent.gameObject;
        parentNode = m_GameObjectHandler.GetOrCreateNode(panelComponentGameObject);
        return parentNode != HierarchyNode.Null;
    }

    private (List<VisualTreeAsset> vtAssets, List<TemplateAsset> instances) GetTemplateChain(VisualElement element)
    {
        var vtAssets = new List<VisualTreeAsset>();
        var instances = new List<TemplateAsset>();
        if (element is not TemplateContainer) return (vtAssets, instances);

        var elementTemplateSource = (element as TemplateContainer)?.templateSource;
        var elementVEATemplate = element.visualElementAsset as TemplateAsset;

        vtAssets.Add(elementTemplateSource);
        instances.Add(elementVEATemplate);

        var leafElement = element;
        while (leafElement?.GetFirstAncestorOfType<TemplateContainer>() != null)
        {
            var templateContainer = leafElement.GetFirstAncestorOfType<TemplateContainer>();
            if (templateContainer is IPanelComponentRootElement) break;

            if (templateContainer != null && templateContainer.templateSource != null)
                vtAssets.Add(templateContainer.templateSource);

            if (templateContainer?.visualElementAsset is TemplateAsset templateAsset)
                instances.Add(templateAsset);

            leafElement = templateContainer;
        }

        return (vtAssets, instances);
    }

    private void EnableHierarchyIntegration(bool value)
    {
        EnableHierarchyIntegration(value, true);
    }

    private void EnableHierarchyIntegration(bool enabled, bool ping)
    {
        if (enabled)
            RegisterExistingRuntimePanels(ping);
        else
            UnregisterAllPanels();
    }

    private void RegisterPanelIfEnabled(IRuntimePanel panel)
    {
        if (UIToolkitAuthoringSettings.EnableInSceneUIAuthoring)
            RegisterPanel((Panel)panel);
    }

    private void UnregisterPanelIfEnabled(IRuntimePanel panel)
    {
        if (UIToolkitAuthoringSettings.EnableInSceneUIAuthoring)
            UnregisterPanel((Panel)panel);
    }

    private void RegisterExistingRuntimePanels(bool pingRoot = false)
    {
        using var listHandle = ListPool<Panel>.Get(out var panels);
        UIElementsUtility.GetAllPanels(panels, ContextType.Player);
        for (var i = 0; i < panels.Count; ++i)
        {
            if (panels[i] is BaseRuntimePanel panel)
            {
                // Although the Panel Element can create a runtime panel, we don't want them showing
                // up in the main stage for now.
                if (panel.ownerObject is PanelElement.PanelOwner)
                    continue;
                RegisterPanel(panel);

                if (pingRoot)
                {
                    var selectionObject = panel.visualTree.Q<PanelRendererRootElement>()?.GetSelectionObject();
                    if (selectionObject)
                    {
                        var id = selectionObject.GetEntityId();
                        EditorApplication.delayCall += () => EditorGUIUtility.PingObject(id);
                        pingRoot = false;
                    }
                }
            }
        }


    }

    private static void OnStageWillChange(Stage previousStage, Stage nextStage)
    {
        switch (nextStage)
        {
            case MainStage:
                HierarchyWindow.RegisterNodeTypeHandler<HierarchyVisualElementHandler>();
                HierarchyWindow.UnregisterNodeTypeHandler<VisualElementEditingNodeHandler>();
                break;
            case VisualElementEditingStage:
                HierarchyWindow.RegisterNodeTypeHandler<VisualElementEditingNodeHandler>();
                HierarchyWindow.UnregisterNodeTypeHandler<HierarchyVisualElementHandler>();
                break;
            default:
                HierarchyWindow.UnregisterNodeTypeHandler<HierarchyVisualElementHandler>();
                HierarchyWindow.UnregisterNodeTypeHandler<VisualElementEditingNodeHandler>();
                break;
        }
        IntegratedAuthoringWorkflow.OnStageChanged(previousStage, nextStage);
    }
}
