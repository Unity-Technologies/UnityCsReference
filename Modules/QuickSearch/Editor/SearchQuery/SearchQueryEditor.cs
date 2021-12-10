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
        private SerializedProperty m_ProvidersProperty;
        private ReorderableList m_ProvidersList;
        private List<string> m_EnabledProviderIds;
        private bool m_ProviderFoldout;

        public void OnEnable()
        {
            m_IconProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.icon));
            m_DescriptionProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.description));
            m_TextProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.text));
            m_ProvidersProperty = serializedObject.FindProperty(nameof(SearchQueryAsset.providerIds));

            PopulateValidEnabledProviderIds();
            m_ProviderFoldout = EditorPrefs.GetBool("SearchQuery.ShowProviderList", false);

            m_ProvidersList = new ReorderableList(m_EnabledProviderIds, typeof(string), false, false, true, true)
            {
                onAddCallback = AddProvider,
                onCanAddCallback = list => true,
                onRemoveCallback = RemoveProvider,
                onCanRemoveCallback = list => m_ProvidersProperty.arraySize > 0,
                drawElementCallback = DrawProviderElement,
                elementHeight = EditorGUIUtility.singleLineHeight
            };

            SetupContext();
        }

        public void OnDisable()
        {
            m_SearchContext.asyncItemReceived -= OnAsyncItemsReceived;
            m_SearchContext.Dispose();
            m_Results.Dispose();
        }

        private void PopulateValidEnabledProviderIds()
        {
            m_EnabledProviderIds = new List<string>(m_ProvidersProperty.arraySize);
            for (int i = 0; i < m_ProvidersProperty.arraySize; i++)
            {
                var id = m_ProvidersProperty.GetArrayElementAtIndex(i).stringValue;
                if (SearchService.GetProvider(id) != null && !m_EnabledProviderIds.Contains(id))
                {
                    m_EnabledProviderIds.Add(id);
                }
            }
        }

        private IEnumerable<SearchProvider> GetDisabledProviders()
        {
            return SearchService.OrderedProviders.Where(p => !m_EnabledProviderIds.Contains(p.id));
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

            var providerIds = m_EnabledProviderIds.Count != 0 ? m_EnabledProviderIds : SearchService.GetActiveProviders().Select(p => p.id);
            m_SearchContext = SearchService.CreateContext(providerIds, m_TextProperty.stringValue, SearchSettings.GetContextOptions());
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
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUI.BeginChangeCheck();
            m_ProviderFoldout = EditorGUILayout.Foldout(m_ProviderFoldout, L10n.Tr("Providers"), true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("SearchQuery.ShowProviderList", m_ProviderFoldout);
            }
            EditorGUILayout.EndHorizontal();

            if (m_ProviderFoldout)
                m_ProvidersList.DoLayoutList();

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
                SearchQuery.ShowQueryIconPicker(SelectIcon);
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


        void AddProvider(ReorderableList list)
        {
            var menu = new GenericMenu();
            var disabledProviders = GetDisabledProviders().ToList();
            for (var i = 0; i < disabledProviders.Count; ++i)
            {
                var provider = disabledProviders[i];
                menu.AddItem(new GUIContent(provider.name), false, AddProvider, provider);
                if (!provider.isExplicitProvider && i + 1 < disabledProviders.Count && disabledProviders[i + 1].isExplicitProvider)
                {
                    menu.AddSeparator(string.Empty);
                }
            }
            menu.ShowAsContext();
        }

        void AddProvider(object providerObj)
        {
            if (providerObj is SearchProvider provider && !m_EnabledProviderIds.Contains(provider.id))
            {
                m_EnabledProviderIds.Add(provider.id);
                UpdateEnabledProviders();
            }
        }

        void RemoveProvider(ReorderableList list)
        {
            var index = list.index;
            if (index != -1 && index < m_EnabledProviderIds.Count)
            {
                var toRemove = SearchService.GetProvider(m_EnabledProviderIds[index]);
                if (toRemove == null)
                    return;
                m_EnabledProviderIds.Remove(toRemove.id);
                UpdateEnabledProviders();

                if (index >= list.count)
                    list.index = list.count - 1;
            }
        }

        void UpdateEnabledProviders()
        {
            m_ProvidersProperty.arraySize = m_EnabledProviderIds.Count;
            for (var i = 0; i < m_EnabledProviderIds.Count; ++i)
            {
                m_ProvidersProperty.GetArrayElementAtIndex(i).stringValue = m_EnabledProviderIds[i];
            }
            serializedObject.ApplyModifiedProperties();
            SetupContext();
        }

        void DrawProviderElement(Rect rect, int index, bool selected, bool focused)
        {
            if (index >= 0 && index < m_EnabledProviderIds.Count)
                GUI.Label(rect, SearchService.GetProvider(m_EnabledProviderIds[index])?.name ?? "<unknown>");
        }

        void CheckContext()
        {
            if (m_SearchContext.searchText != m_TextProperty.stringValue)
                SetupContext();
        }
    }
}
