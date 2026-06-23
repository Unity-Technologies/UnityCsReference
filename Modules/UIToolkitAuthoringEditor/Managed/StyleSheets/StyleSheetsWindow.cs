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
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class StyleSheetsWindow : EditorWindow
{
    const string k_MenuPath = "Window/UI Toolkit/Style Sheets";
    const int k_MenuPriority = 3020;
    const string k_StyleSheetDark = "UIToolkitAuthoring/StyleSheets/StyleSheetsWindowDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/StyleSheets/StyleSheetsWindowLight.uss";

    Hierarchy.Hierarchy m_Hierarchy;
    HierarchyView m_HierarchyView;
    StyleSheetEditingNodeTypeHandler m_Handler;
    Dictionary<StyleSheet, HierarchyNode> m_StyleSheetNodes = new();
    ILiveReloadSystem m_LiveReloadSystem;
    VisualTreeAsset m_TrackedVTA;
    StyleSheetAssetTracker m_StyleSheetTracker;
    VisualTreeAssetTracker m_VisualTreeAssetTracker;
    HierarchyGlobalSelectionHandler m_GlobalSelectionHandler;
    VisualElement m_EmptyLabelElement;
    VisualElement m_NoResultsLabelElement;
    VisualElement m_StagingModeContainerElement;
    Button m_OpenSettingsButton;
    VisualElement m_ContainerElement;

    [NonSerialized]
    VisualElementEditingStage m_CurrentStage;

    [SerializeReference]
    List<StyleSheet> m_LastStyleSheets = new();

    // Used in tests
    internal Hierarchy.Hierarchy Hierarchy => m_Hierarchy;
    internal HierarchyView HierarchyView => m_HierarchyView;
    internal StyleSheetNodeTypeHandler Handler => m_Handler;
    internal HierarchyGlobalSelectionHandler GlobalSelectionHandler => m_GlobalSelectionHandler;

    [SerializeField]
    StyleSheet m_ActiveStyleSheet;

    [SerializeField]
    readonly Dictionary<VisualTreeAsset, StyleSheet> m_ActiveStyleSheetInVisualTreeAsset = new();

    public StyleSheet ActiveStyleSheet
    {
        get => m_ActiveStyleSheet;
        private set
        {
            if (m_ActiveStyleSheet == value)
                return;
            m_ActiveStyleSheet = value;
            ActiveStyleSheetChangedMessage.Execute(CommandSources.StyleSheets, m_ActiveStyleSheet);
        }
    }

    [MenuItem(k_MenuPath, false, 3010, secondaryPriority = 4)]
    static void ShowWindow()
    {
        GetWindow<StyleSheetsWindow>().Show();
    }

    void OnEnable()
    {
        titleContent.text = "Style Sheets";
        titleContent.image = EditorGUIUtility.Load("StyleSheet Icon") as Texture2D;

        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged += OnEnableInSceneAuthoringChanged;

        m_VisualTreeAssetTracker = new VisualTreeAssetTracker(OnVisualTreeAssetChanged);
        m_StyleSheetTracker = new StyleSheetAssetTracker(OnStyleSheetChanged);

        rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    void OnDisable()
    {
        rootVisualElement?.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        rootVisualElement?.UnregisterCallback<KeyUpEvent>(OnKeyUp);
        m_HierarchyView?.ListView.UnregisterCallback<PointerDownEvent>(OnHierarchyWindowMouseDown);
        m_HierarchyView?.ListView.UnregisterCallback<DragPerformEvent>(OnHierarchyWindowDragPerformed,
            TrickleDown.TrickleDown);
        rootVisualElement?.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        rootVisualElement?.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        RemoveAssetTrackers();
        DisposeHierarchyResources();
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged -= OnEnableInSceneAuthoringChanged;

        m_LiveReloadSystem = null;
        m_ActiveStyleSheet = null;
        m_ActiveStyleSheetInVisualTreeAsset.Clear();
    }

    void DisposeHierarchyResources()
    {
        m_HierarchyView?.Dispose();
        m_Hierarchy?.Dispose();
        m_GlobalSelectionHandler?.Dispose();
        m_Hierarchy = null;
        m_HierarchyView = null;
        m_Handler = null;
        m_GlobalSelectionHandler = null;
        m_StyleSheetNodes.Clear();
    }

    void CreateGUI()
    {
        PrepareUI();
        m_LiveReloadSystem ??= ((Panel)rootVisualElement.panel).liveReloadSystem;

        // This redundant call is required since on Editor load, the visual tree is not available - therefore the
        // keyboard callback fails to register.
        var visualTree = rootVisualElement.panel?.visualTree;
        if (visualTree != null)
        {
            visualTree.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            visualTree.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            visualTree.RegisterCallback<KeyDownEvent>(OnDelete);
        }

        OnStageChanged(StageUtility.GetCurrentStage());
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        var panelLiveReloadSystem = ((Panel)rootVisualElement.panel).liveReloadSystem;
        // If we are docking or undocking, the panel will be different.
        if (m_LiveReloadSystem != null && m_LiveReloadSystem != panelLiveReloadSystem)
            m_LiveReloadSystem = panelLiveReloadSystem;

        evt.destinationPanel.visualTree.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
        evt.destinationPanel.visualTree.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        evt.destinationPanel.visualTree.RegisterCallback<KeyDownEvent>(OnDelete);

        UpdateAssetTrackers();
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        RemoveAssetTrackers();

        evt.originPanel.visualTree.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
        evt.originPanel.visualTree.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        evt.originPanel.visualTree.UnregisterCallback<KeyDownEvent>(OnDelete);
    }

    void OnStageChanged(Stage newStage)
    {
        UICommandQueue.UnregisterHandler<GetActiveStyleSheetQuery>(GetActiveStyleSheetRequest);
        UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Selection, OnSelectionRequested);

        if (m_ContainerElement == null || m_EmptyLabelElement == null)
            return;

        if (newStage is not VisualElementEditingStage editingStage)
        {
            m_CurrentStage = null;

            RemoveAssetTrackers();
            foreach (var node in m_StyleSheetNodes.Values)
            {
                m_Handler.RemoveStyleSheet(node);
            }
            m_StyleSheetNodes.Clear();
            m_ActiveStyleSheetInVisualTreeAsset.Clear();

            if (m_ContainerElement != null)
                m_ContainerElement.style.display = DisplayStyle.None;

            if (m_EmptyLabelElement != null)
                m_EmptyLabelElement.style.display = DisplayStyle.None;

            if (m_NoResultsLabelElement != null)
                m_NoResultsLabelElement.style.display = DisplayStyle.None;

            if (m_StagingModeContainerElement != null)
                m_StagingModeContainerElement.style.display = DisplayStyle.Flex;

            UpdateOpenSettingsButton();

            return;
        }

        // Track when we first enter staging mode from a non-staging stage
        if (m_CurrentStage == null && m_Handler != null)
            m_Handler.SetEnteringStagingMode();

        m_CurrentStage = editingStage;
        m_ContainerElement.style.display = DisplayStyle.Flex;
        m_StagingModeContainerElement.style.display = DisplayStyle.None;

        UpdateAssetTrackers();
        UICommandQueue.RegisterHandler<GetActiveStyleSheetQuery>(GetActiveStyleSheetRequest);
        UICommandQueue.RegisterHandlerForCategory(CommandCategory.Selection, OnSelectionRequested);
    }

    void OnEnableInSceneAuthoringChanged(bool enabled)
    {
        UpdateOpenSettingsButton();
    }

    void UpdateOpenSettingsButton()
    {
        if (m_OpenSettingsButton == null)
            return;

        m_OpenSettingsButton.style.display = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring
            ? DisplayStyle.None
            : DisplayStyle.Flex;
    }

    void OnSelectionRequested(in CommandContext context)
    {
        // Make sure we are up to date.
        OnStyleSheetChanged();
        var toSelect = EntityId.None;
        switch (context.Command)
        {
            case RequestSelectionQuery<StyleSheet> styleSheetRequest:
                if (m_Handler?.Mappings?.TryGetValue(styleSheetRequest.ToSelect, out var styleSheetNode) ?? false)
                    m_Handler.Mappings.TryGetSelectionHandle(styleSheetNode, out toSelect);
                break;
            case RequestSelectionQuery<StyleRule> styleRuleRequest:
                if (m_Handler?.Mappings?.TryGetValue(styleRuleRequest.ToSelect, out var ruleNode) ?? false)
                    m_Handler.Mappings.TryGetSelectionHandle(ruleNode, out toSelect);
                break;
        }

        if(toSelect != EntityId.None)
            Selection.activeEntityId = toSelect;
    }

    void GetActiveStyleSheetRequest(in CommandContext context)
    {
        var styleSheet = ActiveStyleSheet;
        // It is possible that the window has not yet processed a change, so in that case, we directly check on the
        // tracked VTA to see it it contains any style sheet.
        if (styleSheet == null)
        {
            var styleSheets = m_TrackedVTA?.GetAllReferencedStyleSheets();
            if (styleSheets is { Count: > 0 })
                styleSheet = styleSheets[0];
        }

        GetActiveStyleSheetQuery.QueryPayload.Execute(CommandSources.StyleSheets, styleSheet);
    }

    void Update()
    {
        if (m_HierarchyView?.UpdateNeeded == true)
        {
            m_HierarchyView.Update();
            UpdateSearchResultsDisplay();
        }
    }

    void UpdateSearchResultsDisplay()
    {
        if (m_HierarchyView == null || m_NoResultsLabelElement == null)
            return;

        if (m_HierarchyView.UpdateNeeded)
        {
            rootVisualElement.schedule.Execute(UpdateSearchResultsDisplay);
            return;
        }

        var noStyleSheets = m_StyleSheetNodes.Count == 0;
        var noNodesDisplayed = m_HierarchyView.ViewModel.Count == 0;

        if (m_HierarchyView.Filtering)
        {
            m_NoResultsLabelElement.style.display= noNodesDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
            m_HierarchyView.style.display = noNodesDisplayed ? DisplayStyle.None : DisplayStyle.Flex;
        }
        else
        {
            m_NoResultsLabelElement.style.display = DisplayStyle.None;
            m_HierarchyView.style.display = noStyleSheets ? DisplayStyle.None : DisplayStyle.Flex;
        }

        m_EmptyLabelElement.style.display = noStyleSheets && !m_HierarchyView.Filtering ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void PrepareUI()
    {
        var visualTree = EditorGUIUtility.Load("UIToolkitAuthoring/StyleSheets/StyleSheetsWindow.uxml") as VisualTreeAsset;
        visualTree.CloneTree(rootVisualElement);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        rootVisualElement.styleSheets.Add(styleSheet);

        m_StagingModeContainerElement = rootVisualElement.Q<VisualElement>("unity-style-sheets-window-staging-mode-container");
        m_OpenSettingsButton = rootVisualElement.Q<Button>("unity-style-sheets-window-open-settings-button");
        m_OpenSettingsButton.clicked += UIToolkitAuthoringSettingsProvider.OpenSettings;
        m_EmptyLabelElement = rootVisualElement.Q<Label>("unity-style-sheets-window-empty-label");
        m_NoResultsLabelElement = rootVisualElement.Q<Label>("unity-style-sheets-window-no-results-label");
        m_ContainerElement = rootVisualElement.Q<VisualElement>("unity-style-sheets-window-container");

        var toolbarMenu = rootVisualElement.Q<ToolbarMenu>("add-uss-menu");
        toolbarMenu.menu.AppendAction("Create New USS", _ => CreateStyleSheet());
        toolbarMenu.menu.AppendAction("Add Existing USS", _ => AddStyleSheet());

        var searchField = rootVisualElement.Q<ToolbarSearchField>("stylesheet-search-field");
        searchField.RegisterValueChangedCallback(evt =>
        {
            if (m_HierarchyView == null)
                return;

            m_HierarchyView.Filter = evt.newValue ?? string.Empty;
            UpdateSearchResultsDisplay();
        });

        var newStyleRuleField = rootVisualElement.Q<NewSelectorField>("new-selector-field");
        newStyleRuleField.RegisterCallback<NewSelectorSubmitEvent>(OnCreateNewStyleRule);

        m_Hierarchy = new Hierarchy.Hierarchy();
        m_Handler = m_Hierarchy.GetOrCreateNodeTypeHandler<StyleSheetEditingNodeTypeHandler>();
        m_Handler.Window = this;

        m_HierarchyView = new HierarchyView();
        m_HierarchyView.ListView.fixedItemHeight = 20;
        m_HierarchyView.AddToClassList(StyleSheetNodeTypeHandler.StyleSheetsWindowHierarchyViewUssClassName);
        m_HierarchyView.NameColumn.title = "Rules";
        m_HierarchyView.NameColumn.stretchable = true;

        var specificityColumn = new Column
        {
            title = "Specificity",
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var node = m_HierarchyView.ViewModel[index];

                if (m_Handler.Mappings.TryGetValue(node, out var styleNode) && styleNode.Rule?.complexSelectors?.Length > 0)
                {
                    label.text = $"({styleNode.Rule.complexSelectors[0].specificity})";
                }
                else
                {
                    label.text = "";
                }
            },
            visible = false,
            width = 80,
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
        m_HierarchyView?.ListView.RegisterCallback<PointerDownEvent>(OnHierarchyWindowMouseDown);

        m_ContainerElement.Add(m_HierarchyView);
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

        m_LiveReloadSystem.UnregisterAuthoringTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);
        m_TrackedVTA = null;

        foreach (var stylesheet in m_StyleSheetNodes.Keys)
        {
            m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
        }
    }

    void UpdateAssetTrackers()
    {
        if (m_LiveReloadSystem == null)
            return;

        if (StageUtility.GetCurrentStage() is not VisualElementEditingStage uiStage)
            return;

        if (m_TrackedVTA)
            m_LiveReloadSystem.UnregisterAuthoringTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);

        m_TrackedVTA = uiStage.EditedVisualTreeAsset;
        m_LiveReloadSystem.RegisterAuthoringTrackerForAsset(m_VisualTreeAssetTracker, m_TrackedVTA);

        // Re-register all existing stylesheets to the live reload system
        // This is necessary when the panel changes (e.g., docking/undocking)
        foreach (var stylesheet in m_StyleSheetNodes.Keys)
        {
            m_LiveReloadSystem.RegisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
        }

        RefreshStyleSheetList();
    }

    void OnCreateNewStyleRule(NewSelectorSubmitEvent evt)
    {
        if (!StyleSheetExtensions.ValidateStyleRule(evt.selectorStr, out var error))
        {
            Debug.LogError($"Invalid selector string '{evt.selectorStr}': {error}.");
            return;
        }

        if (!TryEnsureActiveStyleSheetBeforeNewRule())
            return;

        AddStyleRuleCommand.Execute(CommandSources.StyleSheets, ActiveStyleSheet, evt.selectorStr);
        SelectAndScrollTo([m_ActiveStyleSheet.rules[^1]]);
    }

    /// <summary>
    /// When adding a rule but the document has no USS yet, prompts for a save path and creates one
    /// (same idea as the Builder Style Sheets UX for new files).
    /// </summary>
    bool TryEnsureActiveStyleSheetBeforeNewRule()
    {
        if (m_TrackedVTA == null)
            return false;

        using var _ = ListPool<StyleSheet>.Get(out var sheets);
        m_TrackedVTA.GetAllReferencedStyleSheets(sheets);

        if (ActiveStyleSheet != null && sheets.Contains(ActiveStyleSheet))
            return true;

        if (sheets.Count > 0)
        {
            ActiveStyleSheet = sheets[0];
            m_ActiveStyleSheetInVisualTreeAsset[m_TrackedVTA] = ActiveStyleSheet;
            return true;
        }

        return TryCreateStyleSheet() && ActiveStyleSheet != null;
    }

    void OnVisualTreeAssetChanged()
    {
        // VTA changed - check if the stylesheet list actually changed
        var stylesheets = m_TrackedVTA?.GetAllReferencedStyleSheets();
        if (stylesheets == null)
            stylesheets = new List<StyleSheet>();

        // Early out if the stylesheet list hasn't changed
        if (StyleSheetListsEqual(stylesheets, m_LastStyleSheets))
            return;

        m_LastStyleSheets = new List<StyleSheet>(stylesheets);
        RefreshStyleSheetList();
    }

    bool StyleSheetListsEqual(List<StyleSheet> a, List<StyleSheet> b)
    {
        if (a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    void OnStyleSheetChanged()
    {
        RefreshStyleSheetList();
        RefreshStyleSheetRules();
    }

    void RefreshStyleSheetList()
    {
        // Update the hierarchy nodes in case there any pending changes.
        Update();

        if (m_Handler == null || m_Hierarchy == null)
            return;

        // Clean up stale nodes that no longer exist in hierarchy (e.g., after domain reload)
        var nodesToRemove = new List<StyleSheet>();
        foreach (var (stylesheet, node) in m_StyleSheetNodes)
        {
            if (!m_Hierarchy.Exists(node))
            {
                nodesToRemove.Add(stylesheet);
            }
        }
        foreach (var stylesheet in nodesToRemove)
        {
            m_StyleSheetNodes.Remove(stylesheet);
        }

        var stylesheets = m_TrackedVTA?.GetAllReferencedStyleSheets();
        var emptyStyleSheetWindow = stylesheets == null || stylesheets.Count == 0;

        m_EmptyLabelElement.style.display = emptyStyleSheetWindow ? DisplayStyle.Flex : DisplayStyle.None;
        m_NoResultsLabelElement.style.display = DisplayStyle.None;
        m_HierarchyView.style.display = emptyStyleSheetWindow ? DisplayStyle.None : DisplayStyle.Flex;

        if (emptyStyleSheetWindow)
        {
            // Clear all nodes if no stylesheets remain
            foreach (var (stylesheet, node) in m_StyleSheetNodes)
            {
                m_Handler.RemoveStyleSheet(node);
                m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
            }
            m_StyleSheetNodes.Clear();

            ActiveStyleSheet = null;
            if (m_TrackedVTA)
                m_ActiveStyleSheetInVisualTreeAsset.Remove(m_TrackedVTA);

            return;
        }

        using var remappingsHandle = ListPool<StyleSheetRemap>.Get(out var remappings);
        using var addedHandle = HashSetPool<StyleSheet>.Get(out var added);
        using var removedHandle = HashSetPool<StyleSheet>.Get(out var removed);

        // Separate stylesheets into added and removed sets
        var newStylesheets = new HashSet<StyleSheet>(stylesheets);
        foreach (var stylesheet in newStylesheets)
        {
            if (!m_StyleSheetNodes.ContainsKey(stylesheet))
                added.Add(stylesheet);
        }

        foreach (var stylesheet in m_StyleSheetNodes.Keys)
        {
            if (!newStylesheets.Contains(stylesheet))
                removed.Add(stylesheet);
        }

        // Check for remappings (same asset GUID, different instance due to reload)
        if (added.Count > 0 && removed.Count > 0)
        {
            StyleSheetRemapper.Remap(added, removed, remappings);
        }

        // Handle remapped stylesheets
        if (remappings.Count > 0)
        {
            foreach (var remap in remappings)
            {
                if (m_StyleSheetNodes.TryGetValue(remap.Previous, out var node))
                {
                    // Update tracking dictionary
                    m_StyleSheetNodes.Remove(remap.Previous);
                    m_StyleSheetNodes[remap.Remapped] = node;

                    // Update asset trackers
                    m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, remap.Previous);
                    m_LiveReloadSystem?.RegisterAuthoringTrackerForAsset(m_StyleSheetTracker, remap.Remapped);

                    // Remove from added/removed since they're remapped
                    added.Remove(remap.Remapped);
                    removed.Remove(remap.Previous);
                }
            }

            // Update handler mappings
            m_Handler.Mappings.Remap(remappings);
            m_Handler.Remap(remappings);
        }

        // Add truly new stylesheets
        foreach (var stylesheet in added)
        {
            m_LiveReloadSystem?.RegisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
            var rootNode = m_Handler.AddStyleSheet(stylesheet);
            m_StyleSheetNodes[stylesheet] = rootNode;
        }

        // Remove truly removed stylesheets
        foreach (var stylesheet in removed)
        {
            if (m_StyleSheetNodes.TryGetValue(stylesheet, out var node))
            {
                m_Handler.RemoveStyleSheet(node);
                m_LiveReloadSystem?.UnregisterAuthoringTrackerForAsset(m_StyleSheetTracker, stylesheet);
                m_StyleSheetNodes.Remove(stylesheet);
            }
        }

        // Set and store the active stylesheet for the current visual tree asset
        if (m_ActiveStyleSheetInVisualTreeAsset.TryGetValue(m_TrackedVTA, out var savedActive) && stylesheets.Contains(savedActive))
        {
            ActiveStyleSheet = savedActive;
        }
        else if ((ActiveStyleSheet == null || !stylesheets.Contains(ActiveStyleSheet)) && stylesheets.Count > 0)
        {
            ActiveStyleSheet = stylesheets[0];
            m_ActiveStyleSheetInVisualTreeAsset[m_TrackedVTA] = ActiveStyleSheet;
        }

        // Update sort order for all stylesheets to match UXML order
        m_Handler.Sort(m_Hierarchy.Root, stylesheets);
    }

    void RefreshStyleSheetRules()
    {
        // Update the hierarchy nodes in case there any pending changes.
        Update();

        if (m_Handler == null || m_Hierarchy == null)
            return;

        var stylesheets = m_TrackedVTA?.GetAllReferencedStyleSheets();
        if (stylesheets == null || stylesheets.Count == 0)
            return;

        // Refresh rules for all existing stylesheets
        foreach (var styleSheet in stylesheets)
        {
            if (m_StyleSheetNodes.TryGetValue(styleSheet, out var ruleNode))
            {
                m_Handler.RefreshStyleSheetRules(ruleNode, styleSheet);
            }
        }
    }

    public void SetActiveStyleSheet(HierarchyNode hierarchyNode)
    {
        if (!m_Handler.Mappings.TryGetValue(hierarchyNode, out var node))
            return;

        if (node.Rule != null)
            return;

        ActiveStyleSheet = node.StyleSheet;
        m_ActiveStyleSheetInVisualTreeAsset[m_TrackedVTA] = ActiveStyleSheet;
    }

    public void CreateStyleSheet()
    {
        TryCreateStyleSheet();
    }

    public void AddStyleSheet()
    {
        var ussPath = StyleSheetAssetUtilities.DisplayOpenFileDialogForUSS();
        if (string.IsNullOrEmpty(ussPath))
            return;

        AddStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, ussPath);
        RefreshStyleSheetList();
    }

    public void RemoveStyleSheet(HierarchyNode hierarchyNode)
    {
        if (!m_Handler.Mappings.TryGetValue(hierarchyNode, out var node))
            return;

        var styleSheet = node.StyleSheet;
        if (m_CurrentStage.hasUnsavedChanges)
        {
            // If changes were made, make sure they are saved *before* we remove the stylesheet,
            // otherwise the changes to the style sheet won't be saved.
            if (!m_CurrentStage.AskUserToSaveModifiedStage())
                return;
        }

        RemoveStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, styleSheet);
        RefreshStyleSheetList();
    }

    public void SelectAndScrollTo(IReadOnlyList<StyleRule> targetRules)
    {
        if (targetRules == null || targetRules.Count == 0)
            return;

        RefreshStyleSheetRules();
        Update();

        using var _ = ListPool<HierarchyNode>.Get(out var nodesToSelect);

        foreach (var targetRule in targetRules)
        {
            var styleSheet = targetRule.styleSheet;
            if (styleSheet == null)
                continue;

            if (!m_StyleSheetNodes.TryGetValue(styleSheet, out var styleSheetNode))
                continue;

            m_HierarchyView.ViewModel.SetFlags(styleSheetNode, HierarchyNodeFlags.Expanded);

            var children = m_Hierarchy.GetChildren(styleSheetNode);
            foreach (var child in children)
            {
                if (!m_Handler.Mappings.TryGetValue(child, out var styleNode) || styleNode.Rule != targetRule)
                    continue;

                nodesToSelect.Add(child);
                break;
            }
        }

        if (nodesToSelect.Count <= 0)
            return;

        m_HierarchyView.SetSelection(nodesToSelect.ToArray());
        m_HierarchyView.Frame(nodesToSelect[0]);
        m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
    }

    bool TryCreateStyleSheet()
    {
        var ussPath = StyleSheetAssetUtilities.DisplaySaveFileDialogForUSS();
        if (string.IsNullOrEmpty(ussPath))
            return false;

        CreateStyleSheetCommand.Execute(CommandSources.StyleSheets, m_TrackedVTA, ussPath);
        RefreshStyleSheetList();
        return true;
    }

    void OnHierarchyWindowMouseDown(PointerDownEvent evt)
    {
        // Synchronize global selection from view model on right-click.
        // This makes sure the element currently selected by the right click in the view
        // is set to the global selection. This is important since some context-menu operations
        // don't take parameters and instead are reading directly from Selection.activeObject or Selection.entityIds.
        if (IsRightClick((MouseButton)evt.button, evt.modifiers))
        {
            // Place the focus on the window, otherwise interactions like the text field will not work
            Focus();
            m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
        }

        static bool IsRightClick(MouseButton btn, EventModifiers modifiers)
        {
            // on OSX a right click can be either right click or ctrl+left click
            if (UIElementsUtility.isOSXContextualMenuPlatform)
            {
                return btn == MouseButton.RightMouse
                       || (btn == MouseButton.LeftMouse
                           && modifiers == EventModifiers.Control);
            }

            return btn == MouseButton.RightMouse;
        }
    }

    void OnValidateCommand(ValidateCommandEvent evt)
    {
        switch (evt.commandName)
        {
            case EventCommandNames.Cut:
            case EventCommandNames.Copy:
            case EventCommandNames.Paste:
            case EventCommandNames.Rename:
            case EventCommandNames.Duplicate:
            case EventCommandNames.Delete:
            case EventCommandNames.SoftDelete:
            case EventCommandNames.SelectAll:
                evt.StopPropagation();
                break;
        }
    }

    void OnExecuteCommand(ExecuteCommandEvent evt)
    {
        switch (evt.commandName)
        {
            case EventCommandNames.Copy:
                m_HierarchyView.OnCopy();
                evt.StopPropagation();
                break;

            case EventCommandNames.Paste:
                m_HierarchyView.OnPaste();
                evt.StopPropagation();
                break;

            case EventCommandNames.Rename:
                var count = m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
                if (count == 1)
                {
                    Span<HierarchyNode> nodes = stackalloc HierarchyNode[1];
                    m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);
                    m_HierarchyView.OnSetName(nodes[0]);
                }
                evt.StopPropagation();
                break;

            case EventCommandNames.Duplicate:
                m_HierarchyView.OnDuplicate();
                evt.StopPropagation();
                break;

            case EventCommandNames.Delete:
            case EventCommandNames.SoftDelete:
                m_HierarchyView.OnDelete();
                evt.StopPropagation();
                break;

            case EventCommandNames.SelectAll:
                m_HierarchyView.SelectAll(exposedOnly: true);
                m_GlobalSelectionHandler.SyncGlobalSelectionFromViewModel();
                evt.StopPropagation();
                break;

            case EventCommandNames.UndoRedoPerformed:
                m_Hierarchy.SetDirty();
                evt.StopPropagation();
                break;
        }
    }

    void OnDelete(KeyDownEvent evt)
    {
        // HACK: This must be a bug. TextField leaks its key events to everyone!
        if (evt.target is TextElement)
            return;

        var keyCode = evt.keyCode;
        if (keyCode != KeyCode.Delete && keyCode != KeyCode.Backspace)
            return;

        var nodes = m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected);
        // Will delete all the selected StyleSheets.
        foreach (var node in nodes)
        {
            if (Handler.IsStyleSheet(node))
                RemoveStyleSheet(node);
        }

        // The StyleSheets window's hierarchy view OnDelete workflow only handle StyleRules and will early out on
        // StyleSheets. This handles the case where we multi-select stylesheets with style rules.
        m_HierarchyView.OnDelete();
        evt.StopPropagation();
    }

    class VisualTreeAssetTracker(Action onVisualTreeAssetChanged) : BaseLiveReloadVisualTreeAssetTracker, IAuthoringLiveReloadAssetTracker<VisualTreeAsset>
    {
        internal override void OnVisualTreeAssetChanged()
        {
            onVisualTreeAssetChanged?.Invoke();
        }
    }

    class StyleSheetAssetTracker(Action onStyleSheetChanged) : LiveReloadStyleSheetAssetTracker, IAuthoringLiveReloadAssetTracker<StyleSheet>
    {
        public override void OnTrackedAssetChanged()
        {
            onStyleSheetChanged?.Invoke();
        }
    }
}
