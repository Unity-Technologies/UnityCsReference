// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.GraphToolkit.Editor;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Unity.GraphToolkit.ItemLibrary.Editor
{
    /// <summary>
    /// Main Item Library element: search bar, list view and preview/details panel
    /// </summary>
    class ItemLibraryControl : VisualElement
    {
        static readonly string k_TemplateFileName = $"{VisualElementsHelpers.k_ItemLibraryUxmlPath}/ItemLibraryWindow.uxml";
        const string k_StylesheetName = "ItemLibrary/ItemLibrary.uss";

        // Window constants.
        const string k_WindowTitleContainer = "windowTitleContainer";
        const string k_DetailsPanelToggleName = "detailsPanelToggle";
        const string k_WindowTitleLabel = "windowTitleLabel";
        const string k_SearchBoxContainerName = "windowSearchBoxVisualContainer";
        const string k_WindowDetailsPanel = "windowDetailsVisualContainer";
        const string k_WindowResultsScrollViewName = "windowResultsScrollView";
        const string k_WindowSearchTextFieldName = "searchBox";
        const string k_WindowAutoCompleteLabelName = "autoCompleteLabel";
        const string k_SearchPlaceholderLabelName = "searchPlaceholderLabel";
        const string k_WindowResizerName = "windowResizer";
        const string k_LibraryPanel = "libraryViewContainer";
        const string k_ConfirmButtonName = "confirmButton";
        const string k_StatusLabelName = "statusLabel";
        const int k_TabCharacter = 9;

        public static float DefaultSearchPanelWidth => 300f;
        public static float DefaultDetailsPanelWidth => 200f;
        public static float DefaultHeight => 300f;

        const int k_DefaultExtraWidthForDetailsPanel = 12;

        const string k_ControlClassName = "unity-item-library-control";
        const string k_ControlMultiSelectClassName = k_ControlClassName + "--multiselect";
        const string k_PreviewToggleClassName = "unity-item-library-preview-toggle";
        const string k_DetailsToggleCheckedClassName = k_PreviewToggleClassName + "--checked";

        const string k_InfoPanelTooltip = "Toggle Info Panel";
        const string k_DetailsPanelHiddenClassName = "hidden";
        const string k_SearchPlaceholderLabelHiddenClassName = "searchPlaceholderLabel--hidden";

        const string k_IndexingDatabaseStatusText = "Indexing Databases...";

        public ItemLibraryLibrary Library => m_Library;
        public VisualElement DetailsPanel => m_DetailsPanel;
        public VisualElement Resizer => m_Resizer;
        public VisualElement TitleContainer => m_TitleContainer;
        public Label TitleLabel => m_TitleLabel;
        public Toggle DetailsToggle => m_DetailsToggle;
        public TextField SearchTextField => m_SearchTextField;
        public LibraryTreeView TreeView => m_TreeView;
        public static string DetailsPanelHiddenClassName => k_DetailsPanelHiddenClassName;
        public bool IsIndexing { get; private set; }
        public static string IndexingDatabaseStatusText => k_IndexingDatabaseStatusText;

        string m_PendingStatusBarText;
        public string StatusBarText
        {
            get => m_StatusLabel.text;
            set
            {
                if (IsIndexing)
                    m_PendingStatusBarText = value;
                else
                    m_StatusLabel.text = value;
            }
        }

        float m_DetailsPanelExtraWidth;
        Label m_AutoCompleteLabel;
        Label m_SearchPlaceholderLabel;
        Label m_StatusLabel;
        ItemLibraryLibrary m_Library;
        string m_SuggestedCompletion;
        string m_Text = string.Empty;

        public event Action<ItemLibraryItem> itemChosen;

        public event Action<float> detailsPanelWidthChanged;

        VisualElement m_DetailsPanel;
        LibraryTreeView m_TreeView;
        TextField m_SearchTextField;
        VisualElement m_TitleContainer;
        VisualElement m_SearchTextInput;
        VisualElement m_LibraryPanel;
        Toggle m_DetailsToggle;
        VisualElement m_Resizer;
        Label m_TitleLabel;

        public ItemLibraryControl(TypeHandleInfos typeHandleInfos)
        {
            IsIndexing = true;
            this.AddStylesheetWithSkinVariants(k_StylesheetName);

            var windowUxmlTemplate = EditorGUIUtility.Load(k_TemplateFileName) as VisualTreeAsset;
            VisualElement rootElement = windowUxmlTemplate.CloneTree();
            rootElement.AddToClassList("content");
            rootElement.AddToClassList("unity-theme-env-variables");
            rootElement.AddToClassList("item-library-theme");
            rootElement.StretchToParentSize();
            Add(rootElement);

            var listView = this.Q<ListView>(k_WindowResultsScrollViewName);

            if (listView != null)
            {
                m_TreeView = new LibraryTreeView(typeHandleInfos)
                {
                    fixedItemHeight = 25,
                    focusable = true,
                    tabIndex = 1
                };
                m_TreeView.OnModelViewSelectionChange += OnTreeviewSelectionChange;
                var listViewParent = listView.parent;
                listViewParent.Insert(0, m_TreeView);
                listView.RemoveFromHierarchy();
            }

            m_TitleContainer = this.Q(k_WindowTitleContainer);

            var searchBox = this.Q(k_SearchBoxContainerName);
            searchBox.AddToClassList(SearchFieldBase<TextField, string>.ussClassName);

            m_DetailsPanel = this.Q(k_WindowDetailsPanel);

            m_TitleLabel = this.Q<Label>(k_WindowTitleLabel);

            m_LibraryPanel = this.Q(k_LibraryPanel);

            m_DetailsToggle = this.Q<Toggle>(k_DetailsPanelToggleName);
            m_DetailsToggle.AddToClassList(k_PreviewToggleClassName);

            m_SearchTextField = this.Q<TextField>(k_WindowSearchTextFieldName);
            if (m_SearchTextField != null)
            {
                m_SearchTextField.focusable = true;
                m_SearchTextField.RegisterCallback<InputEvent>(OnSearchTextFieldTextChanged);
                m_SearchTextField.RegisterCallback<KeyDownEvent>(OnSearchTextFieldKeyDown, TrickleDown.TrickleDown);
                // Register navigation events in TrickleDown to catch them before our TextElement child stops them
                m_SearchTextField.RegisterCallback<NavigationMoveEvent>(OnSearchTextFieldNavigationMove, TrickleDown.TrickleDown);
                m_SearchTextField.RegisterCallback<NavigationSubmitEvent>(OnSearchTextFieldNavigationSubmit, TrickleDown.TrickleDown);
                m_SearchTextField.RegisterCallback<NavigationCancelEvent>(OnSearchTextFieldNavigationCancel, TrickleDown.TrickleDown);
                m_SearchTextField.AddToClassList(TextInputBaseField<string>.ussClassName);

                m_SearchTextInput = m_SearchTextField.Q(TextInputBaseField<string>.textInputUssName);
            }

            m_AutoCompleteLabel = this.Q<Label>(k_WindowAutoCompleteLabelName);
            m_SearchPlaceholderLabel = this.Q<Label>(k_SearchPlaceholderLabelName);
            m_StatusLabel = this.Q<Label>(k_StatusLabelName);

            m_Resizer = this.Q(k_WindowResizerName);

            var confirmButton = this.Q<Button>(k_ConfirmButtonName);
            confirmButton.clicked += m_TreeView.ConfirmMultiselect;

            // TODO: HACK - ListView's scroll view steals focus using the scheduler.
            EditorApplication.update += HackDueToListViewScrollViewStealingFocus;

            style.flexGrow = 1;
            m_DetailsPanelExtraWidth = k_DefaultExtraWidthForDetailsPanel;
        }

        void OnTreeviewSelectionChange(IReadOnlyList<ITreeItemView> selection)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selectedItems = selection
#pragma warning restore UA2001
                .OfType<IItemView>()
                .Select(siv => siv.Item)
                .ToList();
            m_Library.Adapter.OnSelectionChanged(selectedItems);
            if (m_Library.Adapter.HasDetailsPanel)
            {
                var selectedItem = selectedItems.FirstOrDefault();
                m_Library.Adapter.UpdateDetailsPanel(selectedItem);
                if (m_Library.SourcePort != null && selectedItem is GraphNodeModelLibraryItem)
                    AddPortSubItems(selectedItem);
            }
        }

        void AddPortSubItems(ItemLibraryItem selectedItem)
        {
            if (selectedItem is not GraphNodeModelLibraryItem nodeItem)
                return;

            var currentGraphElement = (m_Library.Adapter as GraphNodeLibraryAdapter)?.CurrentElement;
            if (currentGraphElement == null
                || m_Library.SourcePort == null
                || currentGraphElement.Model is not NodeModel nodeModel
                || nodeModel is ISingleOutputPortNodeModel
                || nodeModel is ISingleInputPortNodeModel)
                return;

            var graphModel = nodeModel.GraphModel;
            var ports = m_Library.SourcePort.Direction == PortDirection.Input ? nodeModel.OutputsByDisplayOrder : nodeModel.InputsByDisplayOrder;
            var compatiblePorts = graphModel.GetCompatiblePorts(ports, m_Library.SourcePort);

            var portItems = new List<GraphNodeModelLibraryItem>();
            var portWithEmptyTitleCount = 0;
            foreach (var portToAdd in compatiblePorts)
            {
                var type = nodeModel.GetType();
                var portEntryName = string.IsNullOrEmpty(portToAdd.Title) ? $"Port {portWithEmptyTitleCount++}" : portToAdd.Title;
                var portItem = new GraphNodeModelLibraryItem(portEntryName, new NodeItemLibraryData(type, portToAdd), nodeItem)
                {
                    CategoryPath = selectedItem.FullName,
                    Help = selectedItem.Help
                };
                portItems.Add(portItem);
            }
            m_TreeView.AddPortItemsToVisibleResults(portItems);
        }

        void HackDueToListViewScrollViewStealingFocus()
        {
            m_SearchTextInput?.Focus();
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= HackDueToListViewScrollViewStealingFocus;
        }

        public void Setup(ItemLibraryLibrary library)
        {
            m_Library = library;

            this.AddStylesheetWithSkinVariants("View.uss");
            this.AddPackageStylesheet("TypeIcons.uss");

            if (!string.IsNullOrEmpty(library?.Adapter.CustomStyleSheetPath))
                this.AddStylesheetAssetWithSkinVariant(library.Adapter.CustomStyleSheetPath);

            m_TreeView.Setup(library, OnItemChosen);

            if (m_Library.Adapter.MultiSelectEnabled)
            {
                AddToClassList(k_ControlMultiSelectClassName);
            }

            m_DetailsPanel.EnableInClassList(k_DetailsPanelHiddenClassName, !m_Library.Adapter.HasDetailsPanel);

            if (m_Library.Adapter.HasDetailsPanel)
            {
                m_Library.Adapter.InitDetailsPanel(m_DetailsPanel);
                ResetSplitterRatio();
                m_DetailsPanel.style.flexGrow = m_Library.Adapter.InitialSplitterDetailRatio;
                m_LibraryPanel.style.flexGrow = 1;

                var showPreview = m_Library.IsPreviewPanelVisible();
                m_DetailsToggle.SetValueWithoutNotify(showPreview);
                SetDetailsPanelVisibility(showPreview);
                m_DetailsToggle.RegisterValueChangedCallback(OnDetailsToggleValueChange);

                // Setup can be called multiple times if the same library is opened multiple times, so we need to make sure we don't register the callback multiple times
                m_DetailsToggle.UnregisterCallback<KeyDownEvent>(OnDetailsToggleKeyDown, TrickleDown.TrickleDown);
                m_DetailsToggle.RegisterCallback<KeyDownEvent>(OnDetailsToggleKeyDown, TrickleDown.TrickleDown);
                m_TitleContainer.Add(m_DetailsToggle);
            }
            else
            {
                var splitter = m_DetailsPanel.parent;

                splitter.parent.Insert(0, m_LibraryPanel);
                splitter.parent.Insert(1, m_DetailsPanel);

                splitter.RemoveFromHierarchy();

                m_DetailsToggle.RemoveFromHierarchy();
            }

            m_TitleLabel.text = m_Library.Adapter.Title;
            if (string.IsNullOrEmpty(m_TitleLabel.text))
            {
                m_TitleLabel.parent.style.visibility = Visibility.Hidden;
                m_TitleLabel.parent.style.position = Position.Absolute;
            }

            // leave some time for library to have time to display before refreshing
            // Otherwise if Refresh takes long, the library isn't shown at all until refresh is done
            // this happens the first time you open the library and your items have a lengthy "Indexing" process
            if (!IsIndexing && !string.IsNullOrEmpty(m_StatusLabel.text)) // edge case where label could have been set
                m_PendingStatusBarText = m_StatusLabel.text;
            IsIndexing = true;
            m_StatusLabel.text = k_IndexingDatabaseStatusText;
            schedule.Execute(delegate ()
            {
                Refresh();
                IsIndexing = false;
                StatusBarText = m_PendingStatusBarText;
            }).ExecuteLater(100);
        }

        void OnItemChosen(ItemLibraryItem item)
        {
            itemChosen?.Invoke(item);
        }

        void ResetSplitterRatio()
        {
            m_DetailsPanel.style.flexGrow = 1;
            m_LibraryPanel.style.flexGrow = 1;
        }

        void SetDetailsPanelVisibility(bool showDetails)
        {
            // if details panel is still visible, store width that isn't taken into account by splitter ratio
            if (!m_DetailsPanel.ClassListContains("hidden"))
            {
                var widthDiff = m_DetailsPanel.resolvedStyle.paddingLeft + m_DetailsPanel.resolvedStyle.paddingRight +
                    m_DetailsPanel.resolvedStyle.marginLeft + m_DetailsPanel.resolvedStyle.marginRight +
                    m_DetailsPanel.resolvedStyle.borderLeftWidth +
                    m_DetailsPanel.resolvedStyle.borderRightWidth;
                if (widthDiff > 0.4f || widthDiff < -0.4f)
                {
                    m_DetailsPanelExtraWidth = widthDiff;
                }
            }

            m_DetailsToggle.EnableInClassList(k_DetailsToggleCheckedClassName, showDetails);
            m_DetailsToggle.tooltip = k_InfoPanelTooltip;

            // hide or show the details/preview element
            m_DetailsPanel.EnableInClassList("hidden", !showDetails);

            // Move elements in or out of the splitter and disable it depending on visibility
            VisualElement splitter;
            if (!showDetails)
            {
                splitter = m_DetailsPanel.parent;
                splitter.parent.Add(m_LibraryPanel);
                splitter.SetEnabled(false);
                m_LibraryPanel.style.flexGrow = 1;
            }
            else
            {
                splitter = m_DetailsPanel.parent.Q("splitter");
                splitter.SetEnabled(true);
                splitter.Insert(0, m_LibraryPanel);
            }
        }

        void OnDetailsToggleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;

            OnItemChosen(null);
            evt.StopPropagation();
        }

        void OnDetailsToggleValueChange(ChangeEvent<bool> evt)
        {
            var showDetails = evt.newValue;
            var rightPartWidth = m_DetailsPanel.resolvedStyle.width;
            SetDetailsPanelVisibility(showDetails);
            m_Library.SetPreviewPanelVisibility(showDetails);
            if (showDetails)
            {
                var leftPartWidth = m_LibraryPanel.resolvedStyle.width;
                var panelsRatio = m_LibraryPanel.resolvedStyle.flexGrow / m_DetailsPanel.resolvedStyle.flexGrow;
                rightPartWidth = leftPartWidth / panelsRatio + m_DetailsPanelExtraWidth;
            }

            detailsPanelWidthChanged?.Invoke(showDetails ? +rightPartWidth : -rightPartWidth);
        }

        void Refresh()
        {
            var query = m_Text;
            var noQuery = string.IsNullOrEmpty(query);

            m_SearchPlaceholderLabel.EnableInClassList(k_SearchPlaceholderLabelHiddenClassName, !string.IsNullOrEmpty(query));
            var results = m_Library.Search(query);

            m_SuggestedCompletion = string.Empty;

            if (results.Count > 0 && !noQuery)
            {
                m_SuggestedCompletion = GetAutoCompletionSuggestion(query, results);
            }

            m_TreeView.ViewMode =
                noQuery ? ResultsViewMode.Hierarchy : ResultsViewMode.Flat;
            m_TreeView.SetResults(results);
        }

        static string GetAutoCompletionSuggestion(string query, IReadOnlyList<ItemLibraryItem> results)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var bestMatch = results
#pragma warning restore UA2001
                .Select(si => si.Name)
                .FirstOrDefault(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase));
            if (bestMatch != null && bestMatch.Length > query.Length && bestMatch[query.Length] != ' ')
            {
                var lastSpace = bestMatch.IndexOf(' ', query.Length);
                var completionSize = lastSpace == -1 ? bestMatch.Length : lastSpace;
                var autoCompletionSuggestion = bestMatch.Substring(query.Length, completionSize - query.Length);
                return autoCompletionSuggestion;
            }
            return string.Empty;
        }

        void OnSearchTextFieldTextChanged(InputEvent inputEvent)
        {
            var text = inputEvent.newData;

            // This is necessary due to OnTextChanged(...) being called after user inputs that have no impact on the text.
            // Ex: Moving the caret.
            if (string.Equals(text, m_Text))
                return;

            m_Text = text;

            // If backspace is pressed and no text remain, clear the suggestion label.
            if (string.IsNullOrEmpty(text))
            {
                // Display the unfiltered results list.
                Refresh();

                m_AutoCompleteLabel.text = String.Empty;
                m_SuggestedCompletion = String.Empty;

                return;
            }

            Refresh();

            if (!string.IsNullOrEmpty(m_SuggestedCompletion))
            {
                m_AutoCompleteLabel.text = text + m_SuggestedCompletion;
            }
            else
            {
                m_AutoCompleteLabel.text = String.Empty;
            }
        }

        void OnSearchTextFieldKeyDown(KeyDownEvent keyDownEvent)
        {
            // KeyDown event is raised twice when entering a character: once with a key code, once with a character.
            // As such, we ignore one of the duplicate event.
            if (keyDownEvent.character == k_TabCharacter)
            {
                // Prevent switching focus to another visual element.
                keyDownEvent.StopPropagation();
                return;
            }

            switch (keyDownEvent.keyCode)
            {
                case KeyCode.Escape:
                {
                    // Close window with no selection and prevent text field from processing the event.
                    itemChosen?.Invoke(null);
                    keyDownEvent.StopPropagation();
                    break;
                }
                case KeyCode.Tab:
                {
                    // Complete the query with the suggested term and prevent text field from processing the event.

                    if (!string.IsNullOrEmpty(m_SuggestedCompletion))
                    {
                        SelectAndReplaceCurrentWord();
                        m_AutoCompleteLabel.text = string.Empty;

                        Refresh();

                        m_SuggestedCompletion = string.Empty;
                    }

                    keyDownEvent.StopPropagation();
                    break;
                }
                case KeyCode.Home:
                case KeyCode.End:
                case KeyCode.PageUp:
                case KeyCode.PageDown:
                {
                    // Forward a copy of the event to the tree view and prevent text field from processing the event.
                    using (var eKeyDown = KeyDownEvent.GetPooled(keyDownEvent.character, keyDownEvent.keyCode,
                        keyDownEvent.modifiers))
                    {
                        eKeyDown.target = m_TreeView.scrollView.contentContainer;
                        SendEvent(eKeyDown);
                    }

                    keyDownEvent.StopPropagation();
                    break;
                }
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                {
                    // Forward a copy of the event to the tree view and prevent text field from processing the event
                    // if the tree view handled it. If it was not handled by the tree view, we want the text field
                    // to move the cursor left or right.
                    using (var eKeyDown = KeyDownEvent.GetPooled(keyDownEvent.character, keyDownEvent.keyCode,
                        keyDownEvent.modifiers))
                    {
                        eKeyDown.target = m_TreeView;
                        SendEvent(eKeyDown);

                        if (eKeyDown.isPropagationStopped)
                        {
                            keyDownEvent.StopPropagation();
                        }
                    }

                    break;
                }
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                {
                    // Ignore those events. They will be translated to NavigationMoveEvent.
                    keyDownEvent.StopPropagation();
                    break;
                }
                case KeyCode.Space:
                {
                    // We do not want space to trigger a submit event to the tree view.
                    m_SkipSubmit = true;
                    break;
                }
            }
        }

        bool m_SkipSubmit;

        void OnSearchTextFieldNavigationCancel(NavigationCancelEvent evt)
        {
            // Forward a copy of the event to the tree view and prevent text field from processing the event.
            using (var ee = EventBridge.GetPooleNavigationCancelEvent(evt))
            {
                ee.target = m_TreeView.scrollView.contentContainer;
                SendEvent(ee);
            }
            evt.StopPropagation();
        }

        void OnSearchTextFieldNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (m_SkipSubmit)
            {
                m_SkipSubmit = false;
                return;
            }

            // Forward a copy of the event to the tree view and prevent text field from processing the event.
            using (var ee = EventBridge.GetPooledNavigationSubmitEvent(evt))
            {
                ee.target = m_TreeView.scrollView.contentContainer;
                SendEvent(ee);
            }
            evt.StopPropagation();
        }

        void OnSearchTextFieldNavigationMove(NavigationMoveEvent evt)
        {
            // Forward a copy of the event to the tree view and prevent text field from processing the event.
            using (var ee = EventBridge.GetPooledNavigationMoveEvent(evt))
            {
                ee.target = m_TreeView.scrollView.contentContainer;
                SendEvent(ee);
            }
            evt.StopPropagation();
        }

        void SelectAndReplaceCurrentWord()
        {
            var newText = m_SearchTextField.value + m_SuggestedCompletion;
            m_Text = newText;

            m_SearchTextField.value = newText;
            m_SearchTextField.textSelection.cursorIndex = 0;
            m_SearchTextField.textSelection.selectIndex = newText.Length;
        }
    }
}
