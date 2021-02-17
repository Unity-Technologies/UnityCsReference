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
        private SerializedProperty m_DescriptionProperty;
        private SerializedProperty m_TextProperty;
        private SerializedProperty m_ProvidersProperty;
        private ReorderableList m_ProvidersList;

        public void OnEnable()
        {
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

            var originalText = m_TextProperty.stringValue;
            EditorGUILayout.DelayedTextField(m_TextProperty, new GUIContent("Search Text"));
            if (originalText != m_TextProperty.stringValue)
            {
                m_SearchContext.searchText = m_TextProperty.stringValue;
                RefreshResults();
            }

            EditorGUILayout.PropertyField(m_DescriptionProperty);
            m_ProvidersList.DoLayoutList();

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
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
