// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.Search
{
    [CustomEditor(typeof(SearchQueryAsset), editorForChildClasses: false, isFallback = false)]
    class SearchQueryEditor : Editor
    {
        private SearchResultView m_ResultView;
        private SortedSearchList m_Results;
        private SearchContext m_SearchContext;
        private SerializedProperty m_IconProperty;
        private SerializedProperty m_DescriptionProperty;
        private SerializedProperty m_TextProperty;
        private SerializedProperty m_IsSearchTemplateProperty;
        private SearchQueryAsset m_QueryAsset;

        public void OnEnable()
        {
            m_QueryAsset = serializedObject.targetObject as SearchQueryAsset;
            m_IconProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.icon));
            m_DescriptionProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.description));
            m_TextProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.text));
            m_IsSearchTemplateProperty = serializedObject.FindProperty("m_IsSearchTemplate");

            SetupContext();
        }

        public void OnDisable()
        {
            m_SearchContext.asyncItemReceived -= OnAsyncItemsReceived;
            m_SearchContext.Dispose();
            m_Results.Dispose();
        }

        private void SetupContext()
        {
            m_Results?.Dispose();
            m_ResultView?.Dispose();
            if (m_SearchContext != null)
            {
                m_SearchContext.Dispose();
                m_SearchContext.asyncItemReceived -= OnAsyncItemsReceived;
            }

            m_SearchContext = SearchService.CreateContext(m_QueryAsset.viewState.providerIds, m_TextProperty.stringValue, SearchSettings.GetContextOptions());
            m_SearchContext.asyncItemReceived += OnAsyncItemsReceived;
            m_Results = new SortedSearchList(m_SearchContext);
            m_ResultView = new SearchResultView(m_Results);

            RefreshResults();
        }

        private void SetItems(IEnumerable<SearchItem> items)
        {
            m_Results.Clear();
            m_Results.AddItems(items);
            Repaint();
        }

        private void OnAsyncItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            m_Results.AddItems(items);
            Repaint();
        }

        private void RefreshResults()
        {
            SetItems(SearchService.GetItems(m_SearchContext));
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        // Implement to create your own interactive custom preview. Interactive custom previews are used in the preview area of the inspector and the object selector.
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            m_ResultView.OnGUI(r);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            CheckContext();

            EditorGUILayout.BeginHorizontal();
            {
                Vector2 dropDownSize = EditorGUI.GetObjectIconDropDownSize(32, 32);
                var iconRect = GUILayoutUtility.GetRect(dropDownSize.x, dropDownSize.y, GUILayout.ExpandWidth(false));
                ObjectIconDropDown(iconRect, m_IconProperty);

                EditorGUILayout.BeginVertical();
                {
                    var originalText = m_TextProperty.stringValue;
                    EditorGUILayout.DelayedTextField(m_TextProperty, EditorGUIUtility.TrTextContent("Search Text"));
                    if (originalText != m_TextProperty.stringValue)
                    {
                        m_SearchContext.searchText = m_TextProperty.stringValue;
                        RefreshResults();
                    }

                    EditorGUILayout.PropertyField(m_DescriptionProperty);
                    EditorGUILayout.PropertyField(m_IsSearchTemplateProperty);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }

        private static GUIStyle s_IconButtonStyle;
        private static Material s_IconTextureInactive;
        private static GUIContent s_IconDropDown;
        public void ObjectIconDropDown(Rect position, SerializedProperty iconProperty)
        {
            const float kDropDownArrowMargin = 2;
            const float kDropDownArrowWidth = 12;
            const float kDropDownArrowHeight = 12;

            if (s_IconTextureInactive == null)
                s_IconTextureInactive = (Material)EditorGUIUtility.LoadRequired("Inspectors/InactiveGUI.mat");

            if (s_IconButtonStyle == null)
                s_IconButtonStyle = new GUIStyle("IconButton") { fixedWidth = 0, fixedHeight = 0 };

            void SelectIcon(UnityEngine.Object obj, bool isCanceled)
            {
                if (!isCanceled)
                {
                    iconProperty.objectReferenceValue = obj;
                    iconProperty.serializedObject.ApplyModifiedProperties();
                    SearchService.RefreshWindows();
                }
            }

            if (EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Passive, s_IconButtonStyle))
            {
                SearchUtils.ShowIconPicker(SelectIcon);
                GUIUtility.ExitGUI();
            }

            if (Event.current.type == EventType.Repaint)
            {
                var contentPosition = position;

                contentPosition.xMin += kDropDownArrowMargin;
                contentPosition.xMax -= kDropDownArrowMargin + kDropDownArrowWidth / 2;

                contentPosition.yMin += kDropDownArrowMargin;
                contentPosition.yMax -= kDropDownArrowMargin + kDropDownArrowWidth / 2;

                Rect arrowRect = new Rect(
                    contentPosition.x + contentPosition.width - kDropDownArrowWidth / 2,
                    contentPosition.y + contentPosition.height - kDropDownArrowHeight / 2,
                    kDropDownArrowWidth, kDropDownArrowHeight);
                Texture2D icon = null;

                if (!iconProperty.hasMultipleDifferentValues)
                    icon = iconProperty.objectReferenceValue as Texture2D ?? AssetPreview.GetMiniThumbnail(targets[0]);
                if (icon == null)
                    icon = Icons.favorite;

                Vector2 iconSize = contentPosition.size;

                if (icon)
                {
                    iconSize.x = Mathf.Min(icon.width, iconSize.x);
                    iconSize.y = Mathf.Min(icon.height, iconSize.y);
                }
                Rect iconRect = new Rect(
                    contentPosition.x + contentPosition.width / 2 - iconSize.x / 2,
                    contentPosition.y + contentPosition.height / 2 - iconSize.y / 2,
                    iconSize.x, iconSize.y);

                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                if (s_IconDropDown == null)
                    s_IconDropDown = EditorGUIUtility.IconContent("Icon Dropdown");
                GUIStyle.none.Draw(arrowRect, s_IconDropDown, false, false, false, false);
            }
        }


        void CheckContext()
        {
            if (m_SearchContext.searchText != m_TextProperty.stringValue)
                SetupContext();
        }
    }
}
