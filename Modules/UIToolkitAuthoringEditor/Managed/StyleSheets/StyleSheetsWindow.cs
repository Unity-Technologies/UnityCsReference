// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class StyleSheetsWindow : EditorWindow
{
    const string k_MenuPath = "Window/UI Toolkit/Style Sheets";
    const int k_MenuPriority = 3020;

    Hierarchy.Hierarchy m_Hierarchy;
    HierarchyView m_HierarchyView;
    StyleSheetNodeTypeHandler m_Handler;
    Dictionary<StyleSheet, HierarchyNode> m_StyleSheetNodes = new();

    VisualTreeAsset m_TrackedVTA;
    ILiveReloadSystem m_LiveReloadSystem;
    StyleSheetsVisualTreeAssetTracker m_Tracker;

    // Used for tests
    internal int LoadedStyleSheetCount => m_StyleSheetNodes.Count;

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        UIToolkitAuthoringSettings.UIStagesChanged += OnUIStagesChanged;
        EditorApplication.delayCall += () => OnUIStagesChanged(UIToolkitAuthoringSettings.EnableUIStages);
    }

    static void OnUIStagesChanged(bool enabled)
    {
        if (enabled)
        {
            Menu.AddMenuItem(k_MenuPath, "", false, k_MenuPriority, ShowWindow, null);
            return;
        }

        Menu.RemoveMenuItem(k_MenuPath);
    }

    public static void ShowWindow()
    {
        GetWindow<StyleSheetsWindow>().Show();
    }

    void OnEnable()
    {
        titleContent.text = "Style Sheets";

        // Create tracker instance
        m_Tracker = new StyleSheetsVisualTreeAssetTracker(this);

        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += PrepareUI;
    }

    void OnDestroy()
    {
        if (rootVisualElement != null)
        {
            rootVisualElement.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            rootVisualElement.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        CleanUpLiveReload();
        DisposeHierarchyResources();
        m_Tracker = null;
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= PrepareUI;
    }

    void DisposeHierarchyResources()
    {
        m_Hierarchy?.Dispose();
        m_HierarchyView?.Dispose();
        m_Hierarchy = null;
        m_HierarchyView = null;
        m_Handler = null;
        m_StyleSheetNodes.Clear();
    }

    void CreateGUI()
    {
        rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        PrepareUI(StageUtility.GetCurrentStage());
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        SetupLiveReload();
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        CleanUpLiveReload();
    }

    void PrepareUI(Stage newStage)
    {
        rootVisualElement.Clear();

        if (newStage is not VisualElementEditingStage stage)
        {
            CleanUpLiveReload();
            DisposeHierarchyResources();
            rootVisualElement.Add(new Label { text = "Enter staging mode to have access to this feature.", style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter } });
            return;
        }

        var visualTree = EditorGUIUtility.Load("UIToolkitAuthoring/StyleSheets/StyleSheetsWindow.uxml") as VisualTreeAsset;
        if (visualTree == null)
        {
            throw new System.InvalidOperationException("Failed to load StyleSheetsWindow.uxml. The UXML file may be missing or corrupted.");
        }
        visualTree.CloneTree(rootVisualElement);

        var toolbarMenu = rootVisualElement.Q<ToolbarMenu>("add-uss-menu");
        toolbarMenu.menu.AppendAction(
            "Create New USS",
            _ =>
            {
                var ussPath = StyleSheetAssetUtilities.DisplaySaveFileDialogForUSS();
                if (string.IsNullOrEmpty(ussPath))
                    return;

                var command = new CreateNewStyleSheetCommand(stage.EditedVisualTreeAsset, ussPath);
                command.Execute();
            });

        toolbarMenu.menu.AppendAction(
            "Add Existing USS",
            _ =>
            {
                var ussPath = StyleSheetAssetUtilities.DisplayOpenFileDialogForUSS();
                if (string.IsNullOrEmpty(ussPath))
                    return;

                var command = new AddStyleSheetCommand(stage.EditedVisualTreeAsset, ussPath);
                command.Execute();
            });

        m_Hierarchy = new Hierarchy.Hierarchy();
        m_Handler = m_Hierarchy.GetOrCreateNodeTypeHandler<StyleSheetNodeTypeHandler>();

        m_HierarchyView = new HierarchyView();
        m_HierarchyView.NameColumn.title = "Rules";

        var specificityColumn = new Column
        {
            title = "Specificity",
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var node = m_HierarchyView.ViewModel[index];

                if (m_Handler.Mappings.TryGetNode(node, out var styleNode) &&
                    styleNode.Rule?.complexSelectors?.Length > 0)
                {
                    label.text = $"({styleNode.Rule.complexSelectors[0].specificity})";
                }
                else
                {
                    label.text = "";
                }
            }
        };

        // Add the column after setting source
        var columns = new List<Column>
        {
            m_HierarchyView.NameColumn,
            specificityColumn
        };

        m_HierarchyView.SetColumns(columns);
        m_HierarchyView.SetSourceHierarchy(m_Hierarchy);

        rootVisualElement.Add(m_HierarchyView);

        // Load initial data from current stage
        UpdateFromCurrentStage();
    }

    void Update()
    {
        if (m_HierarchyView?.UpdateNeeded == true)
            m_HierarchyView.UpdateIncremental();
    }

    void SetupLiveReload()
    {
        if (rootVisualElement.panel == null)
            return;

        if (StageUtility.GetCurrentStage() is VisualElementEditingStage uiStage)
        {
            m_TrackedVTA = uiStage.EditedVisualTreeAsset;
        }

        m_LiveReloadSystem = (rootVisualElement.panel as Panel)?.liveReloadSystem;

        if (m_LiveReloadSystem != null)
        {
            m_LiveReloadSystem.enable = true;
            m_LiveReloadSystem.enabledTrackers |= LiveReloadTrackers.StyleSheet;
            m_LiveReloadSystem.RegisterTrackerForAsset(m_Tracker, m_TrackedVTA);
        }
    }

    void CleanUpLiveReload()
    {
        if (m_LiveReloadSystem != null)
        {
            if (m_TrackedVTA != null)
            {
                m_LiveReloadSystem.UnregisterTrackerForAsset(m_Tracker, m_TrackedVTA);
                m_LiveReloadSystem.enable = false;
                m_TrackedVTA = null;
            }
        }
    }

    void UpdateFromCurrentStage()
    {
        // Return early if UI hasn't been created yet
        if (m_Handler == null)
            return;

        // Clear existing stylesheets
        foreach (var node in m_StyleSheetNodes.Values)
        {
            m_Handler.RemoveStyleSheet(node);
        }
        m_StyleSheetNodes.Clear();

        // Check if we're in UI editing mode
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage uiStage)
        {
            SetupLiveReload();

            // Get all stylesheets referenced by this VTA
            var stylesheets = m_TrackedVTA?.GetAllReferencedStyleSheets();

            // Add stylesheets to the hierarchy
            if (stylesheets != null)
            {
                foreach (var stylesheet in stylesheets)
                {
                    if (stylesheet == null) continue;
                    var rootNode = m_Handler.AddStyleSheet(stylesheet);
                    m_StyleSheetNodes[stylesheet] = rootNode;
                }
            }
        }
        else
        {
            CleanUpLiveReload();
        }
    }

    internal class StyleSheetsVisualTreeAssetTracker : BaseLiveReloadVisualTreeAssetTracker
    {
        StyleSheetsWindow m_Owner;

        public StyleSheetsVisualTreeAssetTracker(StyleSheetsWindow owner)
        {
            m_Owner = owner;
        }

        internal override void OnVisualTreeAssetChanged()
        {
            m_Owner.UpdateFromCurrentStage();
        }
    }
}
