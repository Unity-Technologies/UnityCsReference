// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchDetailView : SearchElement, ISearchPanel
    {
        /// <summary>
        ///  The elements that can be made visible or invisible on the panel.
        /// </summary>
        private List<VisualElement> m_ActiveElements;
        private Button[] m_ActionButtons;
        private Button m_MoreButton;
        private Image m_PreviewImage;
        private IMGUIContainer m_InteractivePreviewElement;
        private Label m_SelectionLabel;
        private Label m_DescriptionLabel;
        private VisualElement m_EditorContainer;
        private Editor[] m_Editors;
        private int m_EditorsHash = 0;
        private Action m_RefreshOff;
        private IVisualElementScheduledItem m_PreviewImageRefreshCallback;
        private IVisualElementScheduledItem m_ActionsRefreshCallback;
        private ScrollView m_ScrollView;

        public static readonly string ussClassName = "search-details-view";
        public static readonly string inspectorLabelContainerClassName = ussClassName.WithUssElement("header");
        public static readonly string selectionLabelClassName = ussClassName.WithUssElement("selection-label");
        public static readonly string itemPreviewImageClassName = ussClassName.WithUssElement("item-preview-image");
        public static readonly string itemPreviewClassName = ussClassName.WithUssElement("item-preview");
        public static readonly string itemDescriptionClassName = ussClassName.WithUssElement("item-description");
        public static readonly string itemEditorClassName = ussClassName.WithUssElement("item-editor");
        public static readonly string actionButtonContainerClassName = ussClassName.WithUssElement("action-button-container");
        public static readonly string fixedActionButtonContainerClassName = ussClassName.WithUssElement("fixed-action-button-container");
        public static readonly string extraActionButtonContainerClassName = ussClassName.WithUssElement("extra-action-button-container");
        public static readonly string actionButtonClassName = ussClassName.WithUssElement("action-button");
        public static readonly string actionButtonMoreClassName = ussClassName.WithUssElement("action-button-more");

        public static readonly GUIContent previewInspectorContent = EditorGUIUtility.TrTextContentWithIcon("Inspector", "Open Inspector (F4)", EditorGUIUtility.FindTexture("UnityEditor.InspectorWindow"));
        public static readonly GUIContent moreActionsContent = EditorGUIUtility.TrTextContentWithIcon(string.Empty, "Open actions menu", Icons.more);

        public SearchDetailView(string name, ISearchView viewModel, params string[] classes) : base(name, viewModel, classes)
        {
            AddToClassList(ussClassName);

            m_ActiveElements = new List<VisualElement>();

            m_ScrollView = new ScrollView(ScrollViewMode.Vertical);
            m_ScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            var labelWithIcon = Create<VisualElement>(null, inspectorLabelContainerClassName);
            labelWithIcon.Add(new Image() { image = previewInspectorContent.image });
            labelWithIcon.Add(new Label
            {
                text = previewInspectorContent.text,
                tooltip = previewInspectorContent.tooltip
            });
            var actionsContainer = CreateActionButtons();
            m_SelectionLabel = Create<Label>(null, selectionLabelClassName);
            m_PreviewImage = Create<Image>("SearchItemPreviewImage", itemPreviewImageClassName);
            m_InteractivePreviewElement = Create<IMGUIContainer>("ItemPreview", itemPreviewClassName);
            m_DescriptionLabel = Create<Label>(null, itemDescriptionClassName);
            m_EditorContainer = Create<VisualElement>(null, itemEditorClassName);

            m_ActiveElements.Add(m_SelectionLabel);
            m_ActiveElements.Add(m_PreviewImage);
            m_ActiveElements.Add(m_InteractivePreviewElement);
            m_ActiveElements.Add(m_DescriptionLabel);
            m_ActiveElements.Add(m_EditorContainer);

            m_ScrollView.Add(labelWithIcon);
            m_ScrollView.Add(actionsContainer);
            m_ScrollView.Add(m_SelectionLabel);
            m_ScrollView.Add(m_PreviewImage);
            m_ScrollView.Add(m_InteractivePreviewElement);
            m_ScrollView.Add(m_DescriptionLabel);
            m_ScrollView.Add(m_EditorContainer);

            Add(m_ScrollView);

            HideElements(m_ActiveElements);

            Refresh();
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            On(SearchEvent.RefreshContent, OnRefreshed);
            On(SearchEvent.SelectionHasChanged, OnSelectionChanged);
            RegisterCallback<GeometryChangedEvent>(OnPreviewSizeChanged);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(OnPreviewSizeChanged);

            Off(SearchEvent.RefreshContent, OnRefreshed);
            Off(SearchEvent.SelectionHasChanged, OnSelectionChanged);

            ResetEditors();

            m_PreviewImageRefreshCallback?.Pause();
            m_ActionsRefreshCallback?.Pause();

            base.OnDetachFromPanel(evt);
        }

        private void OnPreviewSizeChanged(GeometryChangedEvent evt)
        {
            // when the carousel is active for the store provider,
            // the preview image will update on a timer and there is no need to act on the geometry event
            if (m_PreviewImageRefreshCallback?.isActive == true)
                return;

            var selection = context.selection;
            var lastItem = selection.Last();
            if (lastItem != null)
                DrawPreview(context, lastItem);
        }

        private void OnSelectionChanged(ISearchEvent evt)
        {
            int selectionCount;
            if (evt.argumentCount == 1)
            {
                selectionCount = evt.GetArgument<IEnumerable<int>>(0).ToList().Count;
                if (selectionCount == 0)
                {
                    SetVisibleDetail(false);
                    ResetEditors();
                    m_EditorContainer.Clear();
                    return;
                }
            }

            Refresh();
        }

        VisualElement CreateActionButtons()
        {
            var actionsContainer = Create(null, actionButtonContainerClassName);

            var fixedActionsContainer = Create(null, fixedActionButtonContainerClassName);
            var fixedAction1 = Create<Button>(null, actionButtonClassName, "fixed-action-button1");
            var fixedAction2 = Create<Button>(null, actionButtonClassName, "fixed-action-button2");
            m_MoreButton = Create<Button>("MoreButton", actionButtonMoreClassName);
            m_MoreButton.text = moreActionsContent.text;
            m_MoreButton.tooltip = moreActionsContent.tooltip;
            m_MoreButton.Insert(0, new Image() { image = moreActionsContent.image });

            fixedActionsContainer.Add(fixedAction1);
            fixedActionsContainer.Add(fixedAction2);
            fixedActionsContainer.Add(m_MoreButton);

            var extraActionsContainer = Create(null, extraActionButtonContainerClassName);

            var extraAction1 = Create<Button>(null, actionButtonClassName, "action-button1");
            var extraAction2 = Create<Button>(null, actionButtonClassName, "action-button2");
            var extraAction3 = Create<Button>(null, actionButtonClassName, "action-button3");
            var extraAction4 = Create<Button>(null, actionButtonClassName, "action-button4");
            extraActionsContainer.Add(extraAction1);
            extraActionsContainer.Add(extraAction2);
            extraActionsContainer.Add(extraAction3);
            extraActionsContainer.Add(extraAction4);

            m_ActionButtons = new[] { fixedAction1, fixedAction2, extraAction1, extraAction2, extraAction3, extraAction4 };

            m_ActiveElements.AddRange(m_ActionButtons);
            m_ActiveElements.Add(m_MoreButton);

            actionsContainer.Add(fixedActionsContainer);
            actionsContainer.Add(extraActionsContainer);

            return actionsContainer;
        }

        private void OnRefreshed(ISearchEvent evt)
        {
            m_RefreshOff?.Invoke();
            m_RefreshOff = Utils.CallDelayed(Refresh, 0.05d);
        }

        private void Refresh()
        {
            var selection = context.selection;
            var lastItem = selection.Last();
            var showOptions = lastItem?.provider.showDetailsOptions ?? ShowDetailsOptions.None;

            var selectionCount = selection.Count;

            SetVisibleDetail(false);

            SetupEditors(selection, showOptions);

            if (selectionCount == 0)
            {
                return;
            }

            SetVisibleDetail(true);
            if (selectionCount > 1)
            {
                ShowElements(m_SelectionLabel);

                // Do not render anything else if the selection is composed of items with different providers
                if (selection.GroupBy(item => item.provider.id).Count() > 1)
                {
                    m_SelectionLabel.text = $"Selected {selectionCount} items from different types.";
                    return;
                }
                else
                {
                    m_SelectionLabel.text = $"Selected {selectionCount} items";
                }
            }

            m_ActionsRefreshCallback?.Pause();
            if (showOptions.HasAny(ShowDetailsOptions.Actions) && !m_ViewModel.IsPicker())
            {
                RefreshActions(context);
                m_ActionsRefreshCallback = schedule.Execute(() => RefreshActions(context)).StartingIn(100).Every(100);
            }

            if (selectionCount >= 1)
            {
                if (showOptions.HasAny(ShowDetailsOptions.Preview) && lastItem != null)
                    DrawPreview(context, lastItem);

                if (showOptions.HasAny(ShowDetailsOptions.Description) && lastItem != null)
                    DrawDescription(context, lastItem);
            }

            if (showOptions.HasAny(ShowDetailsOptions.Inspector))
                DrawInspector(selection, showOptions);

            m_RefreshOff?.Invoke();
            m_RefreshOff = null;
        }

        private void SetVisibleDetail(bool visible)
        {
            if (visible)
            {
                m_ScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            }
            else
            {
                HideElements(m_ActiveElements);
                m_ScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            }
        }

        private void RefreshActions(SearchContext context)
        {
            if (context == null || context.selection == null)
                return;

            HideElements(m_ActionButtons);
            HideElements(m_MoreButton);
            var selection = context.selection;
            var firstItem = selection.First();
            if (firstItem == null)
                return;

            var fixedActions = new string[] { "select", "open" };
            var actions = firstItem.provider.actions.Where(a => a.enabled(selection));
            var remainingActions = actions.Where(a => !fixedActions.Contains(a.id)).ToArray();

            if (remainingActions.Length <= 3)
            {
                RefreshActions(selection, actions, m_ActionButtons);
            }
            else if (remainingActions.Length > 3)
            {
                RefreshActions(selection, actions.Where(a => fixedActions.Contains(a.id)), m_ActionButtons);
                RefreshMoreMenu(selection, remainingActions);
            }
        }

        struct ActionButtonData
        {
            public SearchSelection selection;
            public SearchAction action;
        }

        private void RefreshActions(SearchSelection selection, IEnumerable<SearchAction> actions, Button[] buttonElements)
        {
            if (actions.Count() > buttonElements.Length)
                throw new ArgumentException($"The size of {nameof(actions)} = {actions.Count()} is greater than the number of {nameof(buttonElements)} = {buttonElements.Length}");

            int i = 0;
            foreach (var action in actions)
            {
                if (action == null || action.content == null)
                    continue;

                if (selection.Count > 1 && action.execute == null)
                    continue;

                var button = buttonElements[i++];
                button.text = action.content.text;
                button.tooltip = action.content.tooltip;
                // We are testing for userData to make sure the clickable is only set once.
                // If not, because we keep refreshing the actions every 100 ms, the click event seems
                // to get stuck and the whole search window freezes.
                if (button.userData == null)
                {
                    button.clickable = new Clickable(() =>
                    {
                        var data = (ActionButtonData)button.userData;
                        m_ViewModel.ExecuteAction(data.action, data.selection.ToArray(), false);
                    });
                }
                button.userData = new ActionButtonData() { action = action, selection = selection };
                ShowElements(button);
            }
        }

        struct MoreButtonData
        {
            public SearchSelection selection;
            public IEnumerable<SearchAction> actions;
        }

        private void RefreshMoreMenu(SearchSelection selection, IEnumerable<SearchAction> actions)
        {
            if (!actions.Any())
            {
                return;
            }

            // We are testing for userData to make sure the clickable is only set once.
            // If not, because we keep refreshing the actions every 100 ms, the click event seems
            // to get stuck and the whole search window freezes.
            if (m_MoreButton.userData == null)
            {
                m_MoreButton.clickable = new Clickable(() =>
                {
                    var menu = new GenericMenu();
                    var data = (MoreButtonData)m_MoreButton.userData;
                    foreach (var action in data.actions)
                    {
                        if (action == null || action.content == null)
                            continue;
                        if (data.selection.Count > 1 && action.execute == null)
                            continue;

                        var itemName = !string.IsNullOrWhiteSpace(action.content.text)
                            ? action.content.text
                            : action.content.tooltip;
                        menu.AddItem(new GUIContent(itemName, action.content.image), false,
                            () => m_ViewModel.ExecuteAction(action, data.selection.ToArray(), false));
                    }

                    menu.ShowAsContext();
                });
            }
            m_MoreButton.userData = new MoreButtonData() { actions = actions, selection = selection };
            ShowElements(m_MoreButton);
        }

        private void DrawDescription(SearchContext ctx, SearchItem item)
        {
            item.options |= SearchItemOptions.FullDescription;
            var description = item.GetDescription(ctx, false);
            item.options &= ~SearchItemOptions.FullDescription;
            m_DescriptionLabel.text = description;
            ShowElements(m_DescriptionLabel);
        }

        private void DrawPreview(SearchContext ctx, SearchItem item)
        {
            if (m_PreviewImageRefreshCallback?.isActive == true)
                m_PreviewImageRefreshCallback.Pause();

            var editorWithPreview = m_Editors?.FirstOrDefault(x => x.HasPreviewGUI());
            if (editorWithPreview != null)
            {
                m_InteractivePreviewElement.style.height = 256;
                m_InteractivePreviewElement.onGUIHandler = () =>
                {
                    if (editorWithPreview.targets.All(t => t))
                        editorWithPreview.DrawPreview(m_InteractivePreviewElement.contentRect);
                };
                ShowElements(m_InteractivePreviewElement);
            }
            else
            {
                UpdatePreviewImage(ctx, item, resolvedStyle.width, this);
                if (item.provider?.id.Equals("store", StringComparison.Ordinal) == true)
                {
                    m_PreviewImageRefreshCallback =
                        m_PreviewImage.schedule.Execute(() => UpdatePreviewImage(ctx, item, resolvedStyle.width, this));
                    m_PreviewImageRefreshCallback
                        .StartingIn(500)
                        .Every(500)
                        .Until(() => m_PreviewImage?.style.display == DisplayStyle.None || context.selection?.Any() == false);
                }
            }
        }

        static void UpdatePreviewImage(SearchContext ctx, SearchItem item, float width, SearchDetailView view)
        {
            if (view.context.selection.Count == 0)
                return;

            var previewImage = view.m_PreviewImage;
            Texture2D thumbnail = null;
            if (SearchSettings.fetchPreview && width >= 32)
            {
                // TODO: Use SearchPreviewManager
                thumbnail = item.GetPreview(ctx, new Vector2(width, 256f),
                    FetchPreviewOptions.Large | FetchPreviewOptions.Preview2D, cacheThumbnail: false);
            }

            if (thumbnail == null)
                thumbnail = item.GetThumbnail(ctx, cacheThumbnail: false);

            if (!IsBuiltInIcon(thumbnail))
            {
                previewImage.image = thumbnail;
                ShowElements(previewImage);
            }
        }

        private static bool IsBuiltInIcon(Texture icon)
        {
            return Utils.IsBuiltInResource(icon);
        }

        private void DrawInspector(SearchSelection selection, ShowDetailsOptions showOptions)
        {
            m_EditorContainer.Clear();

            if (m_Editors == null)
                return;

            bool hasValidEditor = false;
            for (int i = 0; i < m_Editors.Length; ++i)
            {
                var e = m_Editors[i];
                if (!Utils.IsEditorValid(e))
                    continue;

                var editorElement = new SearchItemEditorElement(null, e);
                editorElement.HideHeader(showOptions.HasFlag(ShowDetailsOptions.InspectorWithoutHeader));
                m_EditorContainer.Add(editorElement);
                hasValidEditor = true;
            }

            if (hasValidEditor)
            {
                ShowElements(m_EditorContainer);
            }
        }

        private bool SetupEditors(SearchSelection selection, ShowDetailsOptions showOptions)
        {
            int selectionHash = 0;
            foreach (var s in selection)
                selectionHash ^= s.id.GetHashCode();

            if (selectionHash == m_EditorsHash)
                return false;

            ResetEditors();

            if (!showOptions.HasAny(ShowDetailsOptions.Inspector))
                return true;

            var targets = new List<UnityEngine.Object>();
            foreach (var s in selection)
            {
                var item = s;
                GetTargetsFromSearchItem(item, targets);

                if (item.GetFieldCount() > 0)
                {
                    var targetItem = ScriptableObject.CreateInstance<SearchServiceItem>();
                    targetItem.hideFlags |= HideFlags.DontSaveInEditor;
                    targetItem.name = item.label ?? item.value.ToString();
                    targetItem.item = item;
                    targets.Add(targetItem);
                }
            }

            if (targets.Count > 0)
            {
                int maxGroupCount = targets.GroupBy(t => t.GetType()).Max(g => g.Count());
                m_Editors = targets.GroupBy(t => t.GetType()).Where(g => g.Count() == maxGroupCount).Select(g =>
                {
                    var editor = Editor.CreateEditor(g.ToArray());
                    Utils.SetFirstInspectedEditor(editor);
                    return editor;
                }).ToArray();
            }
            m_EditorsHash = selectionHash;
            return true;
        }

        private void ResetEditors()
        {
            if (m_Editors != null)
            {
                foreach (var e in m_Editors)
                    UnityEngine.Object.DestroyImmediate(e);
            }
            m_Editors = null;
            m_EditorsHash = 0;
        }

        private bool GetTargetsFromSearchItem(SearchItem item, List<UnityEngine.Object> targets)
        {
            item.options |= SearchItemOptions.FullDescription;
            var itemObject = item.ToObject();
            item.options &= ~SearchItemOptions.FullDescription;
            if (!itemObject)
                return false;

            if (itemObject is GameObject go)
            {
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                        continue;

                    targets.Add(c);
                }
            }
            else
            {
                targets.Add(itemObject);

                if (item.provider.id == "asset")
                {
                    var importer = AssetImporter.GetAtPath(item.id);
                    if (importer && importer.GetType() != typeof(AssetImporter))
                        targets.Add(importer);
                }
            }

            return true;
        }
    }
}

