// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal class StyleSheetsWindow : EditorWindow
{
    const string k_MenuPath = "Window/UI Toolkit/Style Sheets";
    const int k_MenuPriority = 3020;

    Hierarchy.Hierarchy m_Hierarchy;
    HierarchyView m_HierarchyView;
    StyleSheetNodeTypeHandler m_Handler;
    Dictionary<StyleSheet, HierarchyNode> m_StyleSheetNodes = new();
    ILiveReloadSystem m_LiveReloadSystem;
    VisualTreeAsset m_TrackedVTA;
    StyleSheetAssetTracker m_StyleSheetTracker;
    VisualTreeAssetTracker m_VisualTreeAssetTracker;
    HierarchyGlobalSelectionHandler m_GlobalSelectionHandler;

    // Used in tests
    internal Hierarchy.Hierarchy Hierarchy => m_Hierarchy;
    internal HierarchyView HierarchyView => m_HierarchyView;
    internal StyleSheetNodeTypeHandler Handler => m_Handler;
    internal HierarchyGlobalSelectionHandler GlobalSelectionHandler => m_GlobalSelectionHandler;

    [SerializeField]
    StyleSheet m_ActiveStyleSheet;

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

    static void ShowWindow()
    {
        GetWindow<StyleSheetsWindow>().Show();
    }

    void OnEnable()
    {
        titleContent.text = "Style Sheets";

        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;

        m_VisualTreeAssetTracker = new VisualTreeAssetTracker(RefreshStyleSheets);
        m_StyleSheetTracker = new StyleSheetAssetTracker(RefreshStyleSheets);

        rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    void OnDisable()
    {
        rootVisualElement?.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        rootVisualElement?.UnregisterCallback<KeyUpEvent>(OnKeyUp);
        m_HierarchyView?.ListView.UnregisterCallback<DragPerformEvent>(OnHierarchyWindowDragPerformed,
            TrickleDown.TrickleDown);
    }

    void OnDestroy()
    {
        rootVisualElement.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        rootVisualElement.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        RemoveAssetTrackers();
        DisposeHierarchyResources();
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;

        m_LiveReloadSystem = null;
        m_ActiveStyleSheet = null;
    }

    void DisposeHierarchyResources()
    {
        m_Hierarchy?.Dispose();
        m_HierarchyView?.Dispose();
        m_GlobalSelectionHandler?.Dispose();
        m_Hierarchy = null;
        m_HierarchyView = null;
        m_Handler = null;
        m_GlobalSelectionHandler = null;
        m_StyleSheetNodes.Clear();
    }

    void CreateGUI()
    {
        m_LiveReloadSystem ??= ((Panel)rootVisualElement.panel).liveReloadSystem;
        OnStageChanged(StageUtility.GetCurrentStage());
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        var panelLiveReloadSystem = ((Panel)rootVisualElement.panel).liveReloadSystem;
        // If we are docking or undocking, the panel will be different.
        if (m_LiveReloadSystem != null && m_LiveReloadSystem != panelLiveReloadSystem)
            m_LiveReloadSystem = panelLiveReloadSystem;

        UpdateAssetTrackers();
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        RemoveAssetTrackers();
    }

    void OnStageChanged(Stage newStage)
    {
        if (newStage is not VisualElementEditingStage stage)
        {
            RemoveAssetTrackers();
            DisposeHierarchyResources();

            rootVisualElement.Clear();
            // TODO: Move this style to a stylesheet
            rootVisualElement.Add(new Label() { text = "Enter staging mode to have access to this feature.", style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter } });
            return;
        }

        if (m_Hierarchy == null)
        {
            PrepareUI(stage);
        }

        UpdateAssetTrackers();
    }

    void Update()
    {
        if (m_HierarchyView?.UpdateNeeded == true)
            m_HierarchyView.Update();
    }

    void PrepareUI(VisualElementEditingStage stage)
    {
        rootVisualElement.Clear();

        var visualTree = EditorGUIUtility.Load("UIToolkitAuthoring/StyleSheets/StyleSheetsWindow.uxml") as VisualTreeAsset;
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

                RefreshStyleSheets();
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

                RefreshStyleSheets();
            });

        var searchField = rootVisualElement.Q<ToolbarSearchField>("stylesheet-search-field");
        searchField.RegisterValueChangedCallback(evt =>
        {
            if (m_HierarchyView == null)
                return;

            m_HierarchyView.Filter = evt.newValue ?? string.Empty;
        });

        var newSelectorField = rootVisualElement.Q<NewSelectorField>("new-selector-field");
        newSelectorField.RegisterCallback<NewSelectorSubmitEvent>(OnCreateNewSelector);

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

                if (m_Handler.Mappings.TryGetValue(node, out var styleNode) &&
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

        m_GlobalSelectionHandler =
            new HierarchyGlobalSelectionHandler(m_HierarchyView, new EditorGUIUtility.EditorLockTracker());
        rootVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
        rootVisualElement.RegisterCallback<KeyUpEvent>(OnKeyUp);

        m_HierarchyView.ListView.RegisterCallback<DragPerformEvent>(OnHierarchyWindowDragPerformed,
            TrickleDown.TrickleDown);

        rootVisualElement.Add(m_HierarchyView);
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
    }

    void OnKeyUp(KeyUpEvent evt)
    {
        switch (evt.keyCode)
        {
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
            case KeyCode.Home:
            case KeyCode.End:
            case KeyCode.PageUp:
            case KeyCode.PageDown:
            case KeyCode.Escape:
                m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
                break;
        }
    }

    void OnHierarchyWindowDragPerformed(DragPerformEvent evt)
    {
        if (!m_HierarchyView.ViewModel.HasFlags(HierarchyNodeFlags.Selected))
            return;
        m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
    }

    void RemoveAssetTrackers()
    {
        if (m_LiveReloadSystem == null)
            return;

        if (!m_TrackedVTA)
            return;

        m_LiveReloadSystem.UnregisterTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);
        m_TrackedVTA = null;

        foreach (var stylesheet in m_StyleSheetNodes.Keys)
        {
            m_LiveReloadSystem?.UnregisterTrackerForAsset(m_StyleSheetTracker, stylesheet);
        }
    }

    void UpdateAssetTrackers()
    {
        if (m_LiveReloadSystem == null)
            return;

        if (StageUtility.GetCurrentStage() is not VisualElementEditingStage uiStage)
            return;

        if (m_TrackedVTA)
            m_LiveReloadSystem.UnregisterTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);

        m_TrackedVTA = uiStage.EditedVisualTreeAsset;
        m_LiveReloadSystem.RegisterTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);

        RefreshStyleSheets();
    }

    void OnCreateNewSelector(NewSelectorSubmitEvent evt)
    {
        if (m_ActiveStyleSheet == null)
        {
            throw new ArgumentException("StyleSheet cannot be null.");
        }

        if (!StyleSheetExtensions.ValidateSelector(evt.selectorStr, out var error))
        {
            Debug.LogError( $"Invalid selector string '{evt.selectorStr}': {error}.");
            return;
        }

        var command = new AddStyleRuleCommand(m_ActiveStyleSheet, evt.selectorStr);
        command.Execute();
    }

    void RefreshStyleSheets()
    {
        // Update the hierarchy nodes in case there any pending changes.
        Update();

        if (m_Handler == null || m_Hierarchy == null)
            return;

        foreach (var (stylesheet, node) in m_StyleSheetNodes)
        {
            m_Handler.RemoveStyleSheet(node);
            m_LiveReloadSystem?.UnregisterTrackerForAsset(m_StyleSheetTracker, stylesheet);
        }
        m_StyleSheetNodes.Clear();

        var stylesheets = m_TrackedVTA?.GetAllReferencedStyleSheets();
        if (stylesheets == null)
            return;

        foreach (var stylesheet in stylesheets)
        {
            // For now, we just want to save the first stylesheet as the active stylesheet.
            if (m_ActiveStyleSheet == null)
                m_ActiveStyleSheet = stylesheet;

            m_LiveReloadSystem?.RegisterTrackerForAsset(m_StyleSheetTracker, stylesheet);

            var rootNode = m_Handler.AddStyleSheet(stylesheet);
            m_StyleSheetNodes[stylesheet] = rootNode;
        }
    }

    class VisualTreeAssetTracker(Action onVisualTreeAssetChanged) : BaseLiveReloadVisualTreeAssetTracker
    {
        internal override void OnVisualTreeAssetChanged()
        {
            onVisualTreeAssetChanged?.Invoke();
        }
    }

    class StyleSheetAssetTracker(Action onStyleSheetChanged) : LiveReloadStyleSheetAssetTracker
    {
        public override void OnTrackedAssetChanged()
        {
            onStyleSheetChanged?.Invoke();
        }
    }
}
