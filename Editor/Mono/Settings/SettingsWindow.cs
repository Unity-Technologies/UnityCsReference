// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.StyleSheets;

namespace UnityEditor
{
    internal class SettingsWindow : EditorWindow, IHasCustomMenu
    {
        [SerializeField] private Vector2 m_PosLeft;
        [SerializeField] private Vector2 m_PosRight;

        [SerializeField] private SettingsScope m_Scope;
        [SerializeField] public float m_SplitterFlex = 0.2f;

        private SettingsProvider[] m_Providers;
        private SettingsTreeView m_TreeView;
        private VisualSplitter m_Splitter;
        private VisualElement m_SettingsPanel;
        private string m_SearchText;
        private bool m_SearchFieldGiveFocus;


        private static class ImguiStyles
        {
            public static readonly GUIStyle header = "SettingsHeader";
            public const float searchFieldHeight = 20;
        }

        private static class Styles
        {
            public static StyleBlock window => EditorResources.GetStyle("settings-window");
            public static StyleBlock settingsPanel => EditorResources.GetStyle("settings-panel-client-area");
            public static StyleBlock header => EditorResources.GetStyle("settings-header");
        }

        public static float s_DefaultLabelWidth => Styles.window.GetFloat("-unity-label-width");
        public static float s_DefaultLayoutMaxWidth => Styles.window.GetFloat("-unity-max-layout-width");

        public SettingsWindow()
        {
            m_SearchFieldGiveFocus = true;
            m_Scope = SettingsScope.Project;
        }

        internal SettingsProvider[] GetProviders()
        {
            return m_Providers;
        }

        internal SettingsProvider GetCurrentProvider()
        {
            return m_TreeView.currentProvider;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            if (Unsupported.IsDeveloperMode())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Print Provider Keywords"), false, PrintProviderKeywords);
                menu.AddItem(EditorGUIUtility.TrTextContent("Refresh providers"), false, OnSettingsProviderChanged);
            }
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

            SettingsService.settingsProviderChanged -= OnSettingsProviderChanged;
            SettingsService.settingsProviderChanged += OnSettingsProviderChanged;

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        internal void OnDisable()
        {
            if (m_Splitter != null && m_Splitter.childCount >= 1)
            {
                var splitLeft = m_Splitter.Children().First();
                float flexGrow = splitLeft.style.flex.value.grow;
                EditorPrefs.SetFloat(GetPrefKeyName(nameof(m_Splitter)), flexGrow);
            }

            if (m_TreeView != null && m_TreeView.currentProvider != null)
                EditorPrefs.SetString(GetPrefKeyName(titleContent.text + "_current_provider"), m_TreeView.currentProvider.settingsPath);

            SettingsService.settingsProviderChanged -= OnSettingsProviderChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            Repaint();
        }

        private void PrintProviderKeywords()
        {
            var sb = new StringBuilder();
            foreach (var settingsProvider in m_Providers)
            {
                sb.AppendLine(settingsProvider.label);
                foreach (var keyword in settingsProvider.keywords)
                {
                    sb.Append("    ");
                    sb.AppendLine(keyword);
                }
            }
            File.WriteAllText("settings_keywords.txt", sb.ToString());
        }

        private void OnSettingsProviderChanged()
        {
            Init();
            Repaint();
        }

        private void RestoreSelection()
        {
            var lastSelectedProvider = EditorPrefs.GetString(GetPrefKeyName(titleContent.text + "_current_provider"), "");
            if (!string.IsNullOrEmpty(lastSelectedProvider) && Array.Find(m_Providers, provider => provider.settingsPath == lastSelectedProvider) != null)
            {
                SelectProviderByName(lastSelectedProvider);
            }
            else if (m_Providers.Length > 0)
            {
                SelectProviderByName(m_Providers[0].settingsPath);
            }
        }

        private void Init()
        {
            m_Providers = SettingsService.FetchSettingsProviders().Where(p => p.scope == m_Scope).ToArray();

            WarnAgainstDuplicates();

            m_SplitterFlex = EditorPrefs.GetFloat(GetPrefKeyName(nameof(m_Splitter)), m_SplitterFlex);

            foreach (var provider in m_Providers)
            {
                provider.settingsWindow = this;
                if (!provider.icon)
                {
                    provider.icon = EditorGUIUtility.FindTexture("UnityEditor/EditorSettings Icon");
                }
            }

            m_TreeView = new SettingsTreeView(m_Providers);
            m_TreeView.currentProviderChanged += ProviderChanged;
            m_SearchText = String.Empty;

            RestoreSelection();
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

            if (newlySelectedProvider != null)
            {
                newlySelectedProvider?.OnActivate(m_SearchText, m_SettingsPanel);
                EditorPrefs.SetString(GetPrefKeyName(titleContent.text + "_current_provider"), newlySelectedProvider.settingsPath);
            }

            if (m_SettingsPanel.childCount == 0)
            {
                var imguiContainer = new IMGUIContainer(DrawSettingsPanel);
                imguiContainer.AddToClassList("settings-panel-imgui-container");
                m_SettingsPanel.Add(imguiContainer);
            }
        }

        private void SetupUI()
        {
            minSize = new Vector2(Styles.window.GetFloat("min-width"), Styles.window.GetFloat("min-height"));

            var root = this.GetRootVisualContainer();

            root.style.flexDirection = FlexDirection.Column;

            var toolbar = new IMGUIContainer(DrawToolbar);
            root.Add(toolbar);

            m_Splitter = new VisualSplitter { splitSize = Styles.window.GetInt("-unity-splitter-size") };
            m_Splitter.AddToClassList("settings-splitter");
            root.Add(m_Splitter);
            var settingsTree = new IMGUIContainer(DrawTreeView)
            {
                style =
                {
                    flex = new Flex(m_SplitterFlex)
                }
            };
            settingsTree.AddToClassList("settings-tree-imgui-container");
            m_Splitter.Add(settingsTree);

            m_SettingsPanel = new VisualElement()
            {
                style =
                {
                    flex = new Flex(1.0f - m_SplitterFlex)
                }
            };
            m_SettingsPanel.AddToClassList("settings-panel");
            m_Splitter.Add(m_SettingsPanel);
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            var searchText = EditorGUILayout.ToolbarSearchField(m_SearchText);
            if (searchText != m_SearchText)
            {
                m_SearchText = searchText;
                HandleSearchFiltering();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawSettingsPanel()
        {
            if (m_TreeView.currentProvider == null)
                return;

            DrawTitleBar();

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(m_PosRight, GUILayout.ExpandWidth(true)))
            {
                m_PosRight = scrollViewScope.scrollPosition;
                DrawControls();
            }

            DrawFooterBar();
        }

        private void DrawControls()
        {
            using (new EditorGUI.LabelHighlightScope(m_SearchText, Styles.settingsPanel.GetColor("-unity-search-highlight-selection-color"), Styles.settingsPanel.GetColor("-unity-search-highlight-color")))
                m_TreeView.currentProvider.OnGUI(m_SearchText);
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(s_DefaultLayoutMaxWidth));
            GUILayout.Space(Styles.settingsPanel.GetFloat(StyleKeyword.marginLeft));
            var headerContent = new GUIContent(m_TreeView.currentProvider.label, Styles.header.GetBool("-unity-show-icon") ? m_TreeView.currentProvider.icon : null);
            GUILayout.Label(headerContent, ImguiStyles.header, GUILayout.MaxHeight(Styles.header.GetFloat("max-height")));
            GUILayout.FlexibleSpace();
            m_TreeView.currentProvider.OnTitleBarGUI();
            GUILayout.EndHorizontal();
        }

        private void DrawFooterBar()
        {
            m_TreeView.currentProvider.OnFooterBarGUI();
        }

        private void DrawTreeView()
        {
            var splitterRect = m_Splitter.GetSplitterRect(m_Splitter.Children().First());
            var splitterPos = splitterRect.xMax;
            var treeWidth = splitterPos - Styles.window.GetFloat("margin-left") - Styles.window.GetFloat("margin-right");
            using (var scrollViewScope = new GUILayout.ScrollViewScope(m_PosLeft, GUILayout.Width(splitterPos), GUILayout.MaxWidth(splitterPos), GUILayout.MinWidth(splitterPos)))
            {
                m_PosLeft = scrollViewScope.scrollPosition;
                m_TreeView.OnGUI(new Rect(0, Styles.window.GetFloat("margin-top"), treeWidth, position.height - ImguiStyles.searchFieldHeight - Styles.window.GetFloat("margin-top")));
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

        [MenuItem("Edit/Project Settings...", false, 259, false)]
        internal static void OpenProjectSettings()
        {
            Show(SettingsScope.Project);
        }

        internal static SettingsWindow OpenUserPreferences()
        {
            return Show(SettingsScope.User);
        }

        private static SettingsWindow Create(SettingsScope scope)
        {
            var settingsWindow = CreateInstance<SettingsWindow>();
            settingsWindow.m_Scope = scope;
            settingsWindow.titleContent.text = scope == SettingsScope.Project ? "Project Settings" : "Preferences";
            settingsWindow.Init();
            return settingsWindow;
        }

        internal static SettingsWindow Show(SettingsScope scopes, string settingsPath = null)
        {
            var settingsWindow = FindWindowByScope(scopes) ?? Create(scopes);
            settingsWindow.Show();

            if (settingsPath != null)
            {
                settingsWindow.SelectProviderByName(settingsPath);
            }

            return settingsWindow;
        }

        internal static SettingsWindow FindWindowByScope(SettingsScope scopes)
        {
            var settingsWindows = Resources.FindObjectsOfTypeAll(typeof(SettingsWindow)).Cast<SettingsWindow>();
            return settingsWindows.FirstOrDefault(settingsWindow => settingsWindow.m_Scope == scopes);
        }

        internal class GUIScope : GUI.Scope
        {
            float m_LabelWidth;
            public GUIScope(float layoutMaxWidth)
            {
                m_LabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = s_DefaultLabelWidth;
                GUILayout.BeginHorizontal(GUILayout.MaxWidth(layoutMaxWidth));
                GUILayout.Space(Styles.settingsPanel.GetFloat(StyleKeyword.marginLeft));
                GUILayout.BeginVertical();
                GUILayout.Space(Styles.settingsPanel.GetFloat(StyleKeyword.marginTop));
            }

            public GUIScope() : this(s_DefaultLayoutMaxWidth)
            {
            }

            protected override void CloseScope()
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = m_LabelWidth;
            }
        }
    }
}
