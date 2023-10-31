// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace UnityEditor.Search
{
    [ExcludeFromPreset, CustomEditor(typeof(SearchQueryAsset), editorForChildClasses: false, isFallback = false)]
    class SearchQueryEditor : Editor
    {
        private SearchView m_ResultView;
        private SerializedProperty m_DescriptionProperty;
        private SerializedProperty m_TextProperty;
        private SerializedProperty m_IsSearchTemplateProperty;
        private SerializedProperty m_IconProperty;
        private SerializedProperty m_ViewStateProperty;
        private ScrollView m_InspectorScrollView;
        private VisualElement m_EditorContainer;
        private VisualElement m_HeaderElement;
        private bool m_HasPreview;
        private SearchQueryAsset query => target as SearchQueryAsset;

        VisualElement m_Body;

        public override bool RequiresConstantRepaint() => false;
        public override bool UseDefaultMargins() => false;
        internal override bool CanOpenMultipleObjects() => false;
        internal override bool HasLargeHeader() => false;
        internal override bool ShouldTryToMakeEditableOnOpen() => false;
        internal override string targetTitle => query.displayName;
        protected override bool ShouldHideOpenButton() => false;

        public void OnEnable()
        {
            m_DescriptionProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.description));
            m_TextProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.text));
            m_IsSearchTemplateProperty = serializedObject.FindProperty("m_IsSearchTemplate");
            m_IconProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.icon));
            m_ViewStateProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.viewState));

            var newViewState = new SearchViewState();
            newViewState.Assign(query.GetViewState());
            newViewState.flags |= UnityEngine.Search.SearchViewFlags.DisableQueryHelpers;
            m_ResultView = new SearchView(newViewState, GetInstanceID());
            m_ResultView.AddToClassList("result-view");
            m_HasPreview = true;
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Body = new VisualElement();
            m_Body.AddToClassList("search-inspector");
            SearchElement.AppendStyleSheets(m_Body);

            m_HeaderElement = new VisualElement();
            m_HeaderElement.AddToClassList("search-inspector-header");
            m_HeaderElement.style.flexDirection = FlexDirection.Column;

            m_HeaderElement.Add(new UIElements.PropertyField(m_IsSearchTemplateProperty));
            m_HeaderElement.Add(new UIElements.PropertyField(m_IconProperty));

            var searchTextElement = new VisualElement();
            searchTextElement.style.flexDirection = FlexDirection.Row;
            var searchQueryTextEditor = new UIElements.PropertyField(m_TextProperty);
            searchQueryTextEditor.style.flexGrow = 1;
            searchQueryTextEditor.RegisterValueChangeCallback(evt => {
                if (evt.changedProperty.stringValue != m_ResultView.context.searchText)
                {
                    m_ResultView.context.searchText = evt.changedProperty.stringValue;
                    m_ResultView.Refresh();
                }
            });
            searchTextElement.Add(searchQueryTextEditor);
            var refreshButton = new Button(() =>
            {
                m_ResultView.Refresh();
            });
            refreshButton.text = L10n.Tr("Refresh");
            searchTextElement.Add(refreshButton);

            m_HeaderElement.Add(searchTextElement);
            m_HeaderElement.Add(new UIElements.PropertyField(m_DescriptionProperty));

            if (Unsupported.IsSourceBuild())
                m_HeaderElement.Add(new UIElements.PropertyField(m_ViewStateProperty, "Debug View State") { });
            m_HeaderElement.Add(new SearchGroupBar("", m_ResultView));

            m_Body.Add(m_HeaderElement);
            m_Body.Add(m_ResultView);

            m_ResultView.RegisterCallback<AttachToPanelEvent>(OnBodyAttached);
            m_ResultView.RegisterCallback<DetachFromPanelEvent>(OnBodyDetached);
            return m_Body;
        }

        private void OnBodyAttached(AttachToPanelEvent evt)
        {
            var body = (VisualElement)evt.target;

            // We can get in a situation where 2 scrollbars appear in the inspector if
            // the result view has lots of results, because the listview has a maximum height
            // of 4000. Because of that, we choose to have the result view exactly as high as
            // the available space in the inspector. Therefore, we query the scrollview of the inspector
            // and resize the result view based its size.
            m_InspectorScrollView = body?.GetFirstAncestorOfType<ScrollView>();
            var searchDetails = body?.GetFirstAncestorWhere(el => el.name == "SearchDetailViewContainer");
            m_EditorContainer = searchDetails ?? m_InspectorScrollView;
            m_HasPreview = searchDetails == null;
            if (m_InspectorScrollView != null)
            {
                m_InspectorScrollView.RegisterCallback<GeometryChangedEvent>(ResizeResultView);
                m_InspectorScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                m_InspectorScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                Utils.CallDelayed(ResizeResultView);
            }
        }

        private void OnBodyDetached(DetachFromPanelEvent evt)
        {
            m_EditorContainer = null;
            if (m_InspectorScrollView != null)
            {
                m_InspectorScrollView.UnregisterCallback<GeometryChangedEvent>(ResizeResultView);
                m_InspectorScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
                m_InspectorScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
                m_InspectorScrollView = null;
            }
        }

        private void ResizeResultView(GeometryChangedEvent evt)
        {
            ResizeResultView();
        }

        private void ResizeResultView()
        {
            if (m_EditorContainer == null || m_ResultView == null || m_HeaderElement == null)
                return;

            // To avoid the 2 scrollbars situation, we resize the result view according to the size available in the scrollview.
            var newHeight = Mathf.Max(m_EditorContainer.worldBound.yMax - m_HeaderElement.worldBound.yMax, 100);
            if (m_ResultView.style.height != newHeight)
                m_ResultView.style.height = newHeight;
        }

        public override bool HasPreviewGUI() => m_HasPreview;

        public override void DrawPreview(Rect previewArea)
        {
            if (m_ResultView == null)
                return;

            var selectedItem = m_ResultView.selection.FirstOrDefault();
            if (selectedItem == null)
                return;

            var preview = selectedItem.GetPreview(m_ResultView.context, previewArea.size, FetchPreviewOptions.Large | FetchPreviewOptions.Preview2D);
            if (preview)
                GUI.DrawTexture(previewArea, preview, scaleMode: ScaleMode.ScaleToFit);
        }

        public override GUIContent GetPreviewTitle()
        {
            if (m_ResultView == null)
                return GUIContent.none;

            var selectedItem = m_ResultView.selection.FirstOrDefault();
            if (selectedItem == null)
                return GUIContent.Temp("No selection");

            return new GUIContent(selectedItem.GetLabel(m_ResultView.context, stripHTML: true), selectedItem.GetThumbnail(m_ResultView.context));
        }

        public override string GetInfoString()
        {
            if (m_ResultView == null)
                return string.Empty;

            var selectedItem = m_ResultView.selection.FirstOrDefault();
            if (selectedItem == null)
                return query.description;

            return selectedItem.GetDescription(m_ResultView.context, stripHTML: true);
        }

        public void OnDisable()
        {
            // Clear body to prevent accessing disposed properties.
            m_Body.Clear();

            m_ResultView?.Dispose();
            m_DescriptionProperty.Dispose();
            m_TextProperty.Dispose();
            m_IsSearchTemplateProperty.Dispose();
            m_IconProperty.Dispose();
            m_ViewStateProperty.Dispose();
        }
    }
}
