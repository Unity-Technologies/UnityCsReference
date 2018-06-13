// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor
{
    internal class SettingsWindow : EditorWindow
    {
        [SerializeField] private Vector2 m_PosLeft;
        [SerializeField] private Vector2 m_PosRight;
        [SerializeField] private SettingsScopes m_Scopes;
        [SerializeField] private int m_SelectedProviderId;
        [SerializeField] public float m_SplitterFlex = 0.2f;

        private SettingsProvider[] m_Providers;
        private SettingsTreeView m_TreeView;
        private VisualSplitter m_Splitter;
        private VisualElement m_SettingsPanel;
        private string m_SearchText;
        private bool m_SearchFieldGiveFocus;

        public const float k_DefaultLayoutMaxWidth = 500.0f;

        private static class Styles
        {
            public static readonly GUIStyle header;

            public const float viewMarginTop = 4;
            public const float viewMarginLeft = 3;
            public const float viewMarginRight = 9;
            public const float searchFieldHeight = 20;

            static Styles()
            {
                // TODO: replace with USS style when the styling PR lands
                header = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 18,
                    margin = { top = 4, left = 4 },
                    normal = { textColor = !EditorGUIUtility.isProSkin ? new Color(0.4f, 0.4f, 0.4f, 1.0f) : new Color(0.7f, 0.7f, 0.7f, 1.0f) }
                };
            }
        }

        public SettingsWindow()
        {
            m_SearchFieldGiveFocus = true;
            m_SelectedProviderId = 0;
            m_Scopes = SettingsScopes.None;
        }

        internal SettingsProvider[] GetProviders()
        {
            return m_Providers;
        }

        internal void SelectProviderByName(string name)
        {
            m_TreeView.FocusSelection(name.GetHashCode());
        }

        internal void FilterProviders(string search)
        {
            m_SearchText = search;
            m_TreeView.searchString = search;
            Repaint();
        }

        internal int GetVisibleProviderCount()
        {
            return m_TreeView.GetRows().Count;
        }

        internal string GetPrefKeyName(string propName)
        {
            return $"{nameof(SettingsWindow)}_{propName}";
        }

        internal void OnEnable()
        {
            if (String.IsNullOrEmpty(titleContent.text))
                titleContent.text = "Settings";
            titleContent.image = EditorGUIUtility.IconContent("Settings").image;

            Init();
            SetupUI();
            RestoreSelection();

            SettingsService.settingsProviderChanged += OnSettingsProviderChanged;
        }

        internal void OnDisable()
        {
            if (m_Splitter.childCount >= 1)
            {
                var splitLeft = m_Splitter.Children().First();
                float flexGrow = splitLeft.style.flex.value.grow;
                EditorPrefs.SetFloat(GetPrefKeyName(nameof(m_Splitter)), flexGrow);
            }

            SettingsService.settingsProviderChanged -= OnSettingsProviderChanged;
        }

        private void OnSettingsProviderChanged()
        {
            Init();
            RestoreSelection();
            Repaint();
        }

        private void RestoreSelection()
        {
            m_TreeView.SetSelection(new[] { m_SelectedProviderId }, IMGUI.Controls.TreeViewSelectionOptions.FireSelectionChanged);
        }

        private void Init()
        {
            m_Providers = SettingsService.FetchSettingsProviders().Where(p => (p.scopes & m_Scopes) != 0).ToArray();

            WarnAgainstDuplicates();

            m_SplitterFlex = EditorPrefs.GetFloat(GetPrefKeyName(nameof(m_Splitter)), m_SplitterFlex);

            foreach (var provider in m_Providers)
                provider.settingsWindow = this;
            m_TreeView = new SettingsTreeView(m_Providers);
            m_TreeView.currentProviderChanged += ProviderChanged;
            m_SearchText = String.Empty;
        }

        private void WarnAgainstDuplicates()
        {
            // Warn for providers with same id (will be supported later)
            foreach (var g in m_Providers.GroupBy(x => x.settingsPath))
            {
                if (g.Count() > 1)
                    Debug.LogWarning($"There are {g.Count()} settings providers with the same name {g.Key}.");
            }
        }

        private void ProviderChanged(SettingsProvider lastSelectedProvider, SettingsProvider newlySelectedProvider)
        {
            if (m_SettingsPanel == null)
                return;

            lastSelectedProvider?.OnDeactivate();
            m_SettingsPanel.Clear();

            newlySelectedProvider?.OnActivate(m_SearchText, m_SettingsPanel);
            if (m_SettingsPanel.childCount == 0)
            {
                m_SettingsPanel.Add(new IMGUIContainer(DrawSettingsPanel)
                {
                    style =
                    {
                        flex = new Flex(1),
                        minWidth = 150
                    }
                });
            }

            m_SelectedProviderId = m_TreeView.GetSelection().FirstOrDefault();
        }

        private void SetupUI()
        {
            var root = this.GetRootVisualContainer();
            root.style.flexDirection = FlexDirection.Column;
            m_Splitter = new VisualSplitter { style = { flex = new Flex(1), flexDirection = FlexDirection.Row } };
            root.Add(m_Splitter);
            m_Splitter.Add(new IMGUIContainer(DrawTreeView) {
                style =
                {
                    minWidth = 100,
                    flex = new Flex(m_SplitterFlex)
                }
            });

            m_SettingsPanel = new VisualElement()
            {
                style =
                {
                    minWidth = 100,
                    flex = new Flex(1.0f - m_SplitterFlex),
                    borderLeftWidth = 1.0f,
                    borderColor = new Color(0.0f, 0.0f, 0.0f)
                }
            };

            m_Splitter.Add(m_SettingsPanel);
        }

        private void DrawSettingsPanel()
        {
            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(m_PosRight, GUILayout.ExpandWidth(true)))
            {
                m_PosRight = scrollViewScope.scrollPosition;
                DrawControls();
            }
        }

        private void DrawControls()
        {
            if (m_TreeView.currentProvider == null)
                return;

            GUILayout.Label(new GUIContent(m_TreeView.currentProvider.label, m_TreeView.currentProvider.icon), Styles.header, GUILayout.MaxHeight(26.0f));
            m_TreeView.currentProvider.OnGUI(m_SearchText);
        }

        private void DrawTreeView()
        {
            var splitterRect = m_Splitter.GetSplitterRect(m_Splitter.Children().First());
            var splitterPos = splitterRect.xMax;
            var searchBoxWidth = splitterPos - Styles.viewMarginLeft - Styles.viewMarginRight;
            GUI.SetNextControlName("SettingsSearchField");
            var searchText = EditorGUI.SearchField(new Rect(Styles.viewMarginLeft, Styles.viewMarginTop, searchBoxWidth, Styles.searchFieldHeight), m_SearchText);
            if (searchText != m_SearchText)
            {
                m_SearchText = searchText;
                HandleSearchFiltering();
            }

            using (var scrollViewScope = new GUILayout.ScrollViewScope(m_PosLeft, GUILayout.Width(splitterPos), GUILayout.MaxWidth(splitterPos), GUILayout.MinWidth(splitterPos)))
            {
                m_PosLeft = scrollViewScope.scrollPosition;
                m_TreeView.OnGUI(new Rect(0, Styles.searchFieldHeight + Styles.viewMarginTop, searchBoxWidth, position.height - Styles.searchFieldHeight - Styles.viewMarginTop));
            }

            if (m_SearchFieldGiveFocus)
            {
                m_SearchFieldGiveFocus = false;
                GUI.FocusControl("SettingsSearchField");
            }
        }

        private void HandleSearchFiltering()
        {
            m_TreeView.searchString = m_SearchText;
        }

        [MenuItem("Edit/All Settings &F8", false, 260, true)]
        internal static void OpenAllSettings()
        {
            Show(SettingsScopes.Any);
        }

        [MenuItem("Edit/Settings &F6", false, 259, true)]
        internal static void OpenProjectSettings()
        {
            Show(SettingsScopes.Project);
        }

        internal static void OpenUserPreferences()
        {
            Show(SettingsScopes.User);
        }

        private static SettingsWindow Create(SettingsScopes scopes)
        {
            var settingsWindow = CreateInstance<SettingsWindow>();
            settingsWindow.m_Scopes = scopes;
            if ((scopes & SettingsScopes.Project) != 0)
                settingsWindow.titleContent.text = "Settings";
            else if ((scopes & SettingsScopes.User) != 0)
                settingsWindow.titleContent.text = "Preferences";
            else
                settingsWindow.titleContent.text = scopes.ToString();
            settingsWindow.Init();
            return settingsWindow;
        }

        internal static SettingsWindow Show(SettingsScopes scopes)
        {
            var settingsWindow = FindWindowByScope(scopes) ?? Create(scopes);
            settingsWindow.Show();
            return settingsWindow;
        }

        private static SettingsWindow FindWindowByScope(SettingsScopes scopes)
        {
            var settingsWindows = Resources.FindObjectsOfTypeAll(typeof(SettingsWindow)).Cast<SettingsWindow>();
            return settingsWindows.FirstOrDefault(settingsWindow => (settingsWindow.m_Scopes & scopes) != 0);
        }
    }
}
