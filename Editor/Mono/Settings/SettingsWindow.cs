// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.StyleSheets;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class SettingsWindow : EditorWindow, IHasCustomMenu
    {
        [SerializeField] private Vector2 m_PosLeft;
        [SerializeField] private Vector2 m_PosRight;

        [SerializeField] private SettingsScope m_Scope;
        [SerializeField] public float m_SplitterPos;
        [SerializeField] private string m_SearchText;
        [SerializeField] private TreeViewState m_TreeViewState;

        private SettingsProvider[] m_Providers;
        private SettingsTreeView m_TreeView;
        private TwoPaneSplitView m_Splitter;
        private VisualElement m_SettingsPanel;
        private VisualElement m_TreeViewContainer;
        private VisualElement m_Toolbar;
        private bool m_ProviderChanging;

        private bool m_SearchFieldGiveFocus;
        const string k_SearchField = "SearchField";
        private const string k_MainSplitterViewDataKey =  "settings-main-splitter__view-data-key";

        internal bool GuiCreated => m_SettingsPanel != null;

        struct ProviderChangingScope : IDisposable
        {
            SettingsWindow m_Window;

            public ProviderChangingScope(SettingsWindow window)
            {
                m_Window = window;
                window.m_ProviderChanging = true;
            }

            public void Dispose()
            {
                m_Window.m_ProviderChanging = false;
            }
        }

        private static class ImguiStyles
        {
            public static readonly GUIStyle header = "SettingsHeader";
            public const float searchFieldHeight = 20;
            public const float searchFieldWidth = 300;
        }

        internal static class Styles
        {
            public static StyleBlock window => EditorResources.GetStyle("sb-settings-window");
            public static StyleBlock settingsPanel => EditorResources.GetStyle("sb-settings-panel-client-area");
            public static StyleBlock header => EditorResources.GetStyle("sb-settings-header");

            // FIXME: the highlight mesh is drawn *over* the text, hiding it. We have to reduce the alpha value of the highlight color
            // in order to make the text readable. Blocked by jira https://jira.unity3d.com/browse/UUM-9296
            private static float s_HighlightColorAlpha = 0.67f;

            private static string s_SelectionColorTag = null;
            public static string SelectionColorTag => s_SelectionColorTag
                ??= $"<mark=#{ColorUtility.ToHtmlStringRGBA(new Color(HighlightColor.r, HighlightColor.g, HighlightColor.b, s_HighlightColorAlpha))}>";
            public static readonly string SelectionColorEndTag = "</mark>";
            private static string s_TextColorTag = null;
            public static string TextColorTag => s_TextColorTag
                ??= $"<color=#{ColorUtility.ToHtmlStringRGBA(settingsPanel.GetColor("-unity-search-highlight-color"))}>";
            public static readonly string TextColorEndTag = "</color>";
            public static readonly Regex TagRegex = new(@"<[^>]*>", RegexOptions.Compiled);

            private static Color HighlightColor => Styles.settingsPanel.GetColor("-unity-search-highlight-selection-color");
        }

        public static float s_DefaultLabelWidth => Styles.window.GetFloat("-unity-label-width");
        public static float s_DefaultLayoutMaxWidth => Styles.window.GetFloat("-unity-max-layout-width");

        public SettingsWindow()
            : this(SettingsScope.Project)
        {
        }

        public SettingsWindow(SettingsScope scope)
        {
            m_Scope = scope;
            titleContent.text = scope == SettingsScope.Project ? "Project Settings" : "Preferences";
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SelectProviderByName(string name, bool ignoreLastSelected = true)
        {
            if (m_SettingsPanel == null)
            {
                SaveCurrentProvider(name);
                return;
            }

            var currentSelection = m_TreeView.GetSelection();
            var selectionID = name.GetHashCode();
            // Check if the section is already selected to avoid the scroll bar to reset at the top of the window.
            if (ignoreLastSelected || currentSelection.Count != 1 || currentSelection[0] != selectionID)
                m_TreeView.FocusSelection(selectionID);
        }

        internal void OnLostFocus()
        {
            m_TreeView.currentProvider?.OnFocusLost();
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
            titleContent.image = EditorGUIUtility.IconContent("Settings").image;

            SettingsService.settingsProviderChanged -= OnSettingsProviderChanged;
            SettingsService.settingsProviderChanged += OnSettingsProviderChanged;
            SettingsService.repaintAllSettingsWindow -= OnRepaintAllWindows;
            SettingsService.repaintAllSettingsWindow += OnRepaintAllWindows;
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
            Undo.undoRedoEvent += OnUndoRedoPerformed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            InitProviders();

            // TODO : testing purposes to remove
            // EditorApplication.delayCall += SetupGUI;
        }

        internal void OnDisable()
        {
            if (m_Splitter != null && m_Splitter.childCount >= 1)
            {
                EditorPrefs.SetFloat(GetPrefKeyName(nameof(m_SplitterPos)), m_Splitter.fixedPaneDimension);
            }

            DeactivateAndSaveCurrentProvider();

            SettingsService.settingsProviderChanged -= OnSettingsProviderChanged;
            SettingsService.repaintAllSettingsWindow -= OnRepaintAllWindows;
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void CreateGUI()
        {
            // TODO : testing purposes to reenable
            SetupGUI();
        }

        void SetupGUI()
        {
            var root = rootVisualElement;
            root.AddStyleSheetPath("StyleSheets/SettingsWindowCommon.uss");
            root.AddStyleSheetPath($"StyleSheets/SettingsWindow{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}.uss");

            root.style.flexDirection = FlexDirection.Column;

            m_Toolbar = new IMGUIContainer(DrawToolbar);
            root.Add(m_Toolbar);

            m_SplitterPos = EditorPrefs.GetFloat(GetPrefKeyName(nameof(m_SplitterPos)), 150f);
            m_Splitter = new TwoPaneSplitView(0, m_SplitterPos, TwoPaneSplitViewOrientation.Horizontal)
            {
                name = "SettingsSplitter",
                viewDataKey = k_MainSplitterViewDataKey
            };
            m_Splitter.AddToClassList("settings-splitter");
            root.Add(m_Splitter);

            m_TreeViewContainer = new IMGUIContainer(DrawTreeView)
            {
                focusOnlyIfHasFocusableControls = false,
            };
            m_TreeViewContainer.AddToClassList("settings-tree-imgui-container");
            m_Splitter.Add(m_TreeViewContainer);

            m_SettingsPanel = new VisualElement();
            m_SettingsPanel.AddToClassList("settings-panel");
            m_Splitter.Add(m_SettingsPanel);


            // Restore selection after setting the ProviderChanged callback so we can activate the initial selected provider
            RestoreSelection();
        }

        internal void InitProviders()
        {
            m_Providers = SettingsService.FetchSettingsProviders(m_Scope);
            foreach (var provider in m_Providers)
            {
                provider.settingsWindow = this;
                if (!provider.icon)
                {
                    provider.icon = EditorGUIUtility.FindTexture("UnityEditor/EditorSettings Icon");
                }
            }

            if (m_TreeView != null)
            {
                m_TreeView.currentProviderChanged -= ProviderChanged;
            }
            m_TreeViewState = m_TreeViewState ?? new TreeViewState();
            m_TreeView = new SettingsTreeView(m_TreeViewState, m_Providers);
            m_TreeView.searchString = m_SearchText = m_SearchText ?? string.Empty;
            m_TreeView.currentProviderChanged += ProviderChanged;
        }

        internal void OnInspectorUpdate()
        {
            m_TreeView.currentProvider?.OnInspectorUpdate();
        }

        private void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            Repaint();
        }

        private void OnRepaintAllWindows()
        {
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (m_TreeView.currentProvider != null)
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    ProviderChanged(m_TreeView.currentProvider, null);
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    RestoreSelection();
                }
            }
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
            if (m_ProviderChanging)
                return;
            DeactivateAndSaveCurrentProvider();
            InitProviders();
            RestoreSelection();
            Repaint();
        }

        private void RestoreSelection()
        {
            var lastSelectedProvider = GetSavedCurrentProvider();
            if (!string.IsNullOrEmpty(lastSelectedProvider) && Array.Find(m_Providers, provider => provider.settingsPath == lastSelectedProvider) != null)
            {
                SelectProviderByName(lastSelectedProvider);
            }
            else if (m_Providers.Length > 0)
            {
                SelectProviderByName(m_Providers[0].settingsPath);
            }
        }

        private bool ProviderChanged(SettingsProvider lastSelectedProvider, SettingsProvider newlySelectedProvider)
        {
            if (m_SettingsPanel == null)
                return false;

            using var pcd = new ProviderChangingScope(this);
            // If we fail to deactivate the last provider, still continue to select the new one.
            try
            {
                lastSelectedProvider?.Deactivate();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            m_SettingsPanel.Clear();

            if (newlySelectedProvider != null)
            {
                // If activating the new provider fails, restore the last selected provider.
                try
                {
                    newlySelectedProvider?.Activate(m_SearchText, m_SettingsPanel);
                    EditorPrefs.SetString(GetPrefKeyName(titleContent.text + "_current_provider"), newlySelectedProvider.settingsPath);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    RestoreSelection();
                    return false;
                }
            }

            SetupIMGUIForCurrentProviderIfNeeded();
            return true;
        }

        internal void SetupIMGUIForCurrentProviderIfNeeded()
        {
            if (m_SettingsPanel.childCount == 0)
            {
                var imguiContainer = new IMGUIContainer(DrawSettingsPanel);
                imguiContainer.AddToClassList("settings-panel-imgui-container");
                m_SettingsPanel.Add(imguiContainer);
            }
        }

        private void SetupWindowPosition()
        {
            var minWidth = Styles.window.GetFloat("min-width");
            // To accomodate some large labels in the settings window, min-width is being increase (case 1282739)
            minWidth += 10;
            var minHeight = Styles.window.GetFloat("min-height");
            minSize = new Vector2(minWidth, minHeight);

            // Center the window if it has never been opened by the user.
            if (EditorPrefs.HasKey($"{this.GetType().FullName}h"))
                return; // Do nothing if the window was opened previously.

            var initialWidth = Styles.window.GetFloat("-unity-initial-width");
            var initialHeight = Styles.window.GetFloat("-unity-initial-height");
            var containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));

            Vector2 initialSize = new Vector2(Mathf.Min(initialWidth, Screen.width), Mathf.Min(initialHeight, Screen.height));
            foreach (ContainerWindow window in containers)
            {
                if (window.showMode == ShowMode.MainWindow)
                {
                    position = new Rect(window.position.center - (initialSize / 2), initialSize);
                    break;
                }
            }
        }

        private static void UpdateSearchHighlight(VisualElement container, string searchText)
        {
            container.Query(null, "settings-panel").Descendents<Label>().ForEach((label) =>
            {
                var text = label.text;
                var hasHighlight = Styles.TagRegex.IsMatch(text);
                text = Styles.TagRegex.Replace(text, String.Empty);
                if (!SearchUtils.MatchSearchGroups(searchText, text, out var startHighlight, out var endHighlight))
                {
                    if (hasHighlight)
                        label.text = text;
                    return;
                }

                text = text.Insert(startHighlight, Styles.SelectionColorTag);
                text = text.Insert(endHighlight + Styles.SelectionColorTag.Length + 1, Styles.SelectionColorEndTag);
                text = $"{Styles.TextColorTag}{text}{Styles.TextColorEndTag}";
                label.text = text;
            });
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            var e = Event.current;
            if (e.commandName == EventCommandNames.Find)
            {
                if (e.type == EventType.ExecuteCommand)
                {
                    EditorGUI.FocusTextInControl(k_SearchField);
                }

                if (e.type != EventType.Layout)
                    e.Use();
            }

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape || ((e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow) &&
                                                    GUI.GetNameOfFocusedControl() == k_SearchField))
                {
                    m_SearchText = string.Empty;
                    HandleSearchFiltering();
                    GUIUtility.keyboardControl = m_TreeView.treeViewControlID;

                    EditorApplication.delayCall += () =>
                    {
                        m_TreeViewContainer.Focus();
                    };

                    Repaint();
                }
            }

            GUI.SetNextControlName(k_SearchField);
            var searchTextRect = GUILayoutUtility.GetRect(0, ImguiStyles.searchFieldWidth, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField);
            var searchText = EditorGUI.ToolbarSearchField(searchTextRect, m_SearchText, false);
            if (e.type == EventType.Repaint && m_SearchFieldGiveFocus)
            {
                m_SearchFieldGiveFocus = false;
                EditorGUI.FocusTextInControl(k_SearchField);
            }

            if (searchText != m_SearchText)
            {
                m_SearchText = searchText;
                HandleSearchFiltering();
            }

            GUILayout.EndHorizontal();
        }

        internal string GetSearchText()
        {
            return m_SearchText;
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

            var e = Event.current;
            if (GUI.GetNameOfFocusedControl() == k_SearchField && e.type == EventType.MouseDown && e.button == 0)
            {
                m_SettingsPanel.Focus();
                GUIUtility.keyboardControl = 0;
            }
        }

        private void DrawControls()
        {
            using (new EditorGUI.LabelHighlightScope(m_SearchText, Styles.settingsPanel.GetColor("-unity-search-highlight-selection-color"), Styles.settingsPanel.GetColor("-unity-search-highlight-color")))
            {
                var currentWideMode = EditorGUIUtility.wideMode;
                var inspectorWidth = m_SettingsPanel.layout.width;
                // the inspector's width can be NaN if this is our first layout check.
                // If that's the case we'll set wideMode to true to avoid computing too tall an inspector on the first layout calculation
                if (!float.IsNaN(inspectorWidth))
                {
                    EditorGUIUtility.wideMode = inspectorWidth > Editor.k_WideModeMinWidth;
                }
                else
                {
                    EditorGUIUtility.wideMode = true;
                }
                m_TreeView.currentProvider.OnGUI(m_SearchText);
                EditorGUIUtility.wideMode = currentWideMode;
            }
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(Styles.settingsPanel.GetFloat(StyleCatalogKeyword.marginLeft));
            var headerContent = new GUIContent(m_TreeView.currentProvider.label, Styles.header.GetBool("-unity-show-icon") ? m_TreeView.currentProvider.icon : null);
            GUILayout.Label(headerContent, ImguiStyles.header, GUILayout.MaxHeight(Styles.header.GetFloat("max-height")), GUILayout.MinWidth(160));
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
            // Splitter's fixedPane might only be available in the next `GeometryChangedEvent`.
            var splitterRect = m_Splitter.fixedPane?.layout ?? Rect.zero;
            var splitterPos = splitterRect.xMax;
            var treeWidth = splitterPos;
            using (var scrollViewScope = new GUILayout.ScrollViewScope(m_PosLeft, GUILayout.Width(splitterPos), GUILayout.MaxWidth(splitterPos), GUILayout.MinWidth(splitterPos)))
            {
                m_PosLeft = scrollViewScope.scrollPosition;
                m_TreeView.OnGUI(new Rect(0, Styles.window.GetFloat("margin-top"), treeWidth, position.height - ImguiStyles.searchFieldHeight - Styles.window.GetFloat("margin-top")));
            }
        }

        private void HandleSearchFiltering()
        {
            m_TreeView.searchString = m_SearchText;
            UpdateSearchHighlight(m_SettingsPanel, m_SearchText);
        }

        void DeactivateAndSaveCurrentProvider()
        {
            if (m_TreeView.currentProvider != null)
            {
                using var _ = new ProviderChangingScope(this);
                try
                {
                    m_TreeView.currentProvider.Deactivate();
                    SaveCurrentProvider(m_TreeView.currentProvider.settingsPath);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        string GetSavedCurrentProvider()
        {
            return EditorPrefs.GetString(GetPrefKeyName(titleContent.text + "_current_provider"), "");
        }

        void SaveCurrentProvider(string settingsPath)
        {
            EditorPrefs.SetString(GetPrefKeyName(titleContent.text + "_current_provider"), settingsPath);
        }

        [MenuItem("Edit/Project Settings...", false, 20000, false)]
        internal static void OpenProjectSettings()
        {
            SendTopMenuProjectSettingsEvent();
            Show(SettingsScope.Project);
        }

        static void SendTopMenuProjectSettingsEvent()
        {
            EditorAnalytics.SendEventEditorGameService(new EditorGameServiceEvent
            {
                action = "Project Settings",
                assembly_info = "",
                component = "Top Menu",
                package = "Unity Editor",
                package_ver = ""
            });
        }

        [Serializable]
        internal struct EditorGameServiceEvent
        {
            public string action;
            public string assembly_info;
            public string component;
            public string package;
            public string package_ver;
        }

        internal static SettingsWindow OpenUserPreferences()
        {
            return Show(SettingsScope.User);
        }

        private static SettingsWindow Create(SettingsScope scope)
        {
            if (scope == SettingsScope.Project)
                return CreateInstance<ProjectSettingsWindow>();

            return CreateInstance<PreferenceSettingsWindow>();
        }

        internal static SettingsWindow Show(SettingsScope scopes, string settingsPath = null)
        {
            var settingsWindow = FindWindowByScope(scopes);
            if (settingsWindow != null)
                EditorGUI.FocusTextInControl(k_SearchField);
            else
                settingsWindow = Create(scopes);

            bool ignoreLastSelection = false;
            if (!settingsWindow.hasFocus)
            {
                settingsWindow.Show();
                settingsWindow.SetupWindowPosition();
                settingsWindow.Focus();
                ignoreLastSelection = true;
            }

            if (settingsPath != null)
            {
                settingsWindow.SelectProviderByName(settingsPath, ignoreLastSelection);
            }

            EditorApplication.delayCall += () =>
            {
                settingsWindow.Focus();
                settingsWindow.m_SearchFieldGiveFocus = true;
            };

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
                GUILayout.BeginHorizontal();
                GUILayout.Space(Styles.settingsPanel.GetFloat(StyleCatalogKeyword.marginLeft));
                GUILayout.BeginVertical();
                GUILayout.Space(Styles.settingsPanel.GetFloat(StyleCatalogKeyword.marginTop));
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

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class ProjectSettingsWindow : SettingsWindow
    {
        public ProjectSettingsWindow()
            : base(SettingsScope.Project)
        {
        }
    }

    internal class PreferenceSettingsWindow : SettingsWindow
    {
        public PreferenceSettingsWindow()
            : base(SettingsScope.User)
        {
        }
    }
}
