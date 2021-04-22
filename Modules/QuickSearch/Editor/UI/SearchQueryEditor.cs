// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.Search
{
    [CustomEditor(typeof(SearchQuery), editorForChildClasses: false, isFallback = false)]
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

        public void OnEnable()
        {
            m_IconProperty = serializedObject.FindProperty(nameof(SearchQuery.icon));
            m_DescriptionProperty = serializedObject.FindProperty(nameof(SearchQuery.description));
            m_TextProperty = serializedObject.FindProperty(nameof(SearchQuery.text));
            m_ProvidersProperty = serializedObject.FindProperty(nameof(SearchQuery.providerIds));

            m_ProvidersList = new ReorderableList(serializedObject, m_ProvidersProperty, false, true, true, true)
            {
                onAddCallback = AddProvider,
                onCanAddCallback = list => m_ProvidersProperty.arraySize < SearchService.Providers.Count(p => p.active),
                onRemoveCallback = RemoveProvider,
                onCanRemoveCallback = list => m_ProvidersProperty.arraySize > 1,
                drawElementCallback = DrawProviderElement,
                elementHeight = EditorGUIUtility.singleLineHeight,
                drawHeaderCallback = DrawProvidersHeader
            };

            SetupContext(GetEnabledProviders());
        }

        public void OnDisable()
        {
            m_SearchContext.asyncItemReceived -= OnAsyncItemsReceived;
            m_SearchContext.Dispose();
            m_Results.Dispose();
        }

        private static bool ContainsString(SerializedProperty arrayProperty, string value)
        {
            for (var i = 0; i < arrayProperty.arraySize; i++)
            {
                if (arrayProperty.GetArrayElementAtIndex(i).stringValue == value)
                    return true;
            }

            return false;
        }

        private IEnumerable<SearchProvider> GetEnabledProviders()
        {
            return SearchService.OrderedProviders.Where(p => ContainsString(m_ProvidersProperty, p.id));
        }

        private IEnumerable<SearchProvider> GetDisabledProviders()
        {
            return SearchService.OrderedProviders.Where(p => !ContainsString(m_ProvidersProperty, p.id));
        }

        private void SetupContext(IEnumerable<SearchProvider> providers)
        {
            m_Results?.Dispose();
            m_ResultView?.Dispose();
            if (m_SearchContext != null)
            {
                m_SearchContext.Dispose();
                m_SearchContext.asyncItemReceived -= OnAsyncItemsReceived;
            }

            m_SearchContext = SearchService.CreateContext(providers, m_TextProperty.stringValue, SearchSettings.GetContextOptions());
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
                    EditorGUILayout.DelayedTextField(m_TextProperty, new GUIContent("Search Text"));
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

            void SelectIcon(UnityEngine.Object obj)
            {
                iconProperty.objectReferenceValue = obj;
                iconProperty.serializedObject.ApplyModifiedProperties();
                SearchService.RefreshWindows();
            }

            if (EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Passive, s_IconButtonStyle))
            {
                ObjectSelector.get.Show(typeof(Texture2D), iconProperty, false, null, SelectIcon, SelectIcon);
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


        private static void DrawProvidersHeader(Rect headerRect)
        {
            GUI.Label(headerRect, "Providers");
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
            var provider = providerObj as SearchProvider;
            var enabledProviders = GetEnabledProviders().ToList();
            enabledProviders.Add(provider);
            UpdateEnabledProviders(enabledProviders);
        }

        void RemoveProvider(ReorderableList list)
        {
            var index = list.index;
            if (index != -1)
            {
                var toRemove = SearchService.GetProvider(m_ProvidersProperty.GetArrayElementAtIndex(index).stringValue);
                if (toRemove == null)
                    return;
                var enabledProviders = GetEnabledProviders().ToList();
                enabledProviders.Remove(toRemove);
                UpdateEnabledProviders(enabledProviders);

                if (index >= list.count)
                    list.index = list.count - 1;
            }
        }

        void UpdateEnabledProviders(List<SearchProvider> enabledProviders)
        {
            m_ProvidersProperty.arraySize = enabledProviders.Count;
            for (var i = 0; i < enabledProviders.Count; ++i)
            {
                m_ProvidersProperty.GetArrayElementAtIndex(i).stringValue = enabledProviders[i].id;
            }
            serializedObject.ApplyModifiedProperties();
            SetupContext(enabledProviders);
        }

        void DrawProviderElement(Rect rect, int index, bool selected, bool focused)
        {
            if (index >= 0 && index < m_ProvidersProperty.arraySize)
                GUI.Label(rect, SearchService.GetProvider(m_ProvidersProperty.GetArrayElementAtIndex(index).stringValue)?.name ?? "<unknown>");
        }

        void CheckContext()
        {
            if (m_SearchContext.searchText != m_TextProperty.stringValue)
                SetupContext(GetEnabledProviders());
        }
    }
}
