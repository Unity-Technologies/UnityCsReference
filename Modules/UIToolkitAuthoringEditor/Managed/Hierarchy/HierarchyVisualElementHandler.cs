// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        if (element is UIDocumentRootElement uiDocumentRootElement)
        {
            var document = uiDocumentRootElement.document;
            var vta = document.visualTreeAsset;

            menu.AppendAction(
                "Select VisualTreeAsset Asset",
                ma => { EditorGUIUtility.PingObject(ma.userData as VisualTreeAsset); },
                ma => (ma.userData as VisualTreeAsset) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                vta);
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
