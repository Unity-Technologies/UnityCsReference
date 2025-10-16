// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using System.IO;
using UnityEditor.SceneManagement;

namespace Unity.UIToolkit.Editor;

[UsedImplicitly]
internal sealed class HierarchyVisualElementHandler : VisualElementNodeTypeHandler
{
    [InitializeOnLoadMethod, UsedImplicitly]
    private static void RegisterHierarchyHandlers()
    {
        EditorApplication.delayCall += () =>
        {
            HierarchyWindow.RegisterNodeTypeHandler<HierarchyVisualElementHandler>();
        };
    }

    [InitializeOnLoadMethod, UsedImplicitly]
    private static void RegisterStageHandlers()
    {
        StageNavigationManager.instance.stageChanging += OnStageWillChange;
    }

    [/*FIX: BeforeCodeUnloading,*/ UsedImplicitly]
    private static void UnregisterHierarchyHandlers()
    {
        HierarchyWindow.UnregisterNodeTypeHandler<HierarchyVisualElementHandler>();
    }

    [/*FIX BeforeManagedObjectsDisabled,*/ UsedImplicitly]
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

        UIToolkitAuthoringSettings.HierarchyIntegrationChanged += EnableHierarchyIntegration;
        EnableHierarchyIntegration(UIToolkitAuthoringSettings.EnableHierarchyIntegration);
    }

    protected override void Initialize()
    {
        base.Initialize();
        m_GameObjectHandler = Hierarchy.GetNodeTypeHandler<HierarchyGameObjectHandler>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        UIElementsRuntimeUtility.onCreatePanel -= RegisterPanelIfEnabled;
        UIElementsRuntimeUtility.onWillDestroyPanel -= UnregisterPanelIfEnabled;
        UIToolkitAuthoringSettings.HierarchyIntegrationChanged -= EnableHierarchyIntegration;
    }

    protected override NodeCreationType ShouldCreateNode(VisualElement element)
    {
        return element is PanelRootElement
            ? NodeCreationType.CreateChildren
            : base.ShouldCreateNode(element);
    }

    protected override string GetDisplayName(HierarchyView view, in HierarchyNode node, VisualElement target)
    {
        if (target is UIDocumentRootElement uiDocumentRootElement)
        {
            var vta = uiDocumentRootElement.document.visualTreeAsset;
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
        if (element is UIDocumentRootElement)
        {
            // We just want to display the name of the file.
            return;
        }

        base.Bind(item, element);
    }

    protected override void PopulateContextMenu(in HierarchyNode node, VisualElement element, DropdownMenu menu)
    {
        UIDocument uiDoc;
        VisualTreeAsset vtaSource;
        VisualElementAsset vea;
        if (element is UIDocumentRootElement documentRootElement)
        {
            uiDoc = documentRootElement.document;
            vtaSource = uiDoc.visualTreeAsset;
            vea = vtaSource?.visualTree;
        }
        else
        {
            uiDoc = element.GetFirstAncestorOfType<UIDocumentRootElement>().document;
            vtaSource = uiDoc.visualTreeAsset;
            vea = element.visualElementAsset;

            if (vea == null)
            {
                vea = element.GetFirstAncestorWhere(ve => ve.visualElementAsset != null).visualElementAsset;
            }
        }
        bool isUIDocumentRootElement = element is UIDocumentRootElement;

        var (ancestorVTAs, ancestorInstances) = GetTemplateChain(element);

        if (isUIDocumentRootElement)
        {
            menu.AppendAction(
                "Select VisualTreeAsset Asset",
                ma => { EditorGUIUtility.PingObject(ma.userData as VisualTreeAsset); },
                ma => (ma.userData as VisualTreeAsset) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                vtaSource);
        }

        if (vtaSource != null)
        {
            menu.AppendAction(
                "Open in UI Builder",
                a =>
                {
                    var cmd = new LoadUIDocumentCommand { selectedId = vea?.id ?? -1 };
                    SessionState.SetString(LoadUIDocumentCommand.CommandId, EditorJsonUtility.ToJson(cmd));
                    AssetDatabase.OpenAsset(vtaSource.GetEntityId());
                    SessionState.EraseString(LoadUIDocumentCommand.CommandId);
                });
        }

        if (vtaSource != null && ancestorVTAs.Count > 0 && ancestorInstances.Count > 0 && !isUIDocumentRootElement)
        {
            menu.AppendAction(
                "Open Instance in UI Builder/in Isolation",
                a =>
                {
                    var cmd = new LoadUIDocumentCommand
                    {
                        subDocumentOptions = SubDocumentOptions.Isolation,
                        subDocuments = ancestorVTAs
                    };
                    SessionState.SetString(LoadUIDocumentCommand.CommandId, EditorJsonUtility.ToJson(cmd));
                    AssetDatabase.OpenAsset(vtaSource.GetEntityId());
                    SessionState.EraseString(LoadUIDocumentCommand.CommandId);
                });
            menu.AppendAction(
                "Open Instance in UI Builder/in Context",
                a =>
                {
                    var cmd = new LoadUIDocumentCommand
                    {
                        selectedId = ancestorInstances[0].id,
                        subDocumentOptions = SubDocumentOptions.InContext,
                        subDocuments = ancestorVTAs,
                        contextInstances = ancestorInstances
                    };
                    SessionState.SetString(LoadUIDocumentCommand.CommandId, EditorJsonUtility.ToJson(cmd));
                    AssetDatabase.OpenAsset(vtaSource.GetEntityId());
                    SessionState.EraseString(LoadUIDocumentCommand.CommandId);
                });
        }
    }
    protected override bool TryGetParentNode(VisualElement element, out HierarchyNode parentNode)
    {
        if (element is not UIDocumentRootElement uiDocumentRootElement)
            return base.TryGetParentNode(element, out parentNode);

        var uiDocumentGameObject = uiDocumentRootElement.document.gameObject;
        parentNode = m_GameObjectHandler.GetOrCreateNode(uiDocumentGameObject);
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
            if (templateContainer is UIDocumentRootElement) break;

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
        if (value)
            RegisterExistingRuntimePanels();
        else
            UnregisterAllPanels();
    }

    private void RegisterPanelIfEnabled(Panel panel)
    {
        if (UIToolkitAuthoringSettings.EnableHierarchyIntegration)
            RegisterPanel(panel);
    }

    private void UnregisterPanelIfEnabled(Panel panel)
    {
        if (UIToolkitAuthoringSettings.EnableHierarchyIntegration)
            UnregisterPanel(panel);
    }

    private void RegisterExistingRuntimePanels()
    {
        using var listHandle = ListPool<Panel>.Get(out var panels);
        UIElementsUtility.GetAllPanels(panels, ContextType.Player);
        for (var i = 0; i < panels.Count; ++i)
        {
            if (panels[i] is BaseRuntimePanel panel)
                RegisterPanel(panel);
        }
    }

    private static void OnStageWillChange(Stage previousStage, Stage nextStage)
    {
        if (nextStage is MainStage mainStage)
            HierarchyWindow.RegisterNodeTypeHandler<HierarchyVisualElementHandler>();
        else
            HierarchyWindow.UnregisterNodeTypeHandler<HierarchyVisualElementHandler>();
    }
}
