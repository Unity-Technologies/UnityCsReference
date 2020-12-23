// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UIToolkitListView = UnityEngine.UIElements.ListView;

namespace UnityEditor.Search
{
    class IndexManager : EditorWindow
    {
        [MenuItem("Window/Search/Index Manager")]
        public static void OpenWindow()
        {
            OpenWindow(-1);
        }

        public static void OpenWindow(int instanceID)
        {
            s_SelectedAssetOnOpen = instanceID;
            var window = CreateWindow<IndexManager>();
            window.m_WindowId = GUID.Generate().ToString();
            window.position = Utils.GetMainWindowCenteredPosition(new Vector2(760f, 500f));
            window.minSize = new Vector2(510f, 200f);
            SearchAnalytics.SendEvent(window.m_WindowId, SearchAnalytics.GenericEventType.IndexManagerOpen);
            window.Show();
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (Path.GetExtension(AssetDatabase.GetAssetPath(instanceID)) == "." + k_IndexExtension)
            {
                OpenWindow(instanceID);
                return true;
            }
            return false;
        }

        private const string k_IndexExtension = "index";
        private const string k_BuildText = "Build";
        private List<IndexManagerViewModel> m_IndexSettings;
        private List<SearchDatabase> m_IndexSettingsAssets;
        private List<string> m_IndexSettingsFilePaths;
        private List<bool> m_IndexSettingsExists;
        private ListViewIndexSettings m_ListViewIndexSettings;
        private ListViewIndexSettings m_ListViewRoots;
        private ListViewIndexSettings m_ListViewIncludes;
        private ListViewIndexSettings m_ListViewExcludes;
        private Foldout m_RootsFoldout;
        private Foldout m_IncludesFoldout;
        private Foldout m_ExcludesFoldout;
        private Foldout m_OptionsFoldout;
        private VisualElement m_IndexDetailsElement;
        private TextField m_IndexFilePathTextField;
        private IntegerField m_IndexScore;
        private Button m_CreateButton;
        private Button m_SaveButton;
        private Label m_SavedIndexDataNotLoadedYet;
        private VisualElement m_SavedIndexData;
        private UIToolkitListView m_DependenciesListView;
        private Button m_DependenciesButton;
        private UIToolkitListView m_DocumentsListView;
        private Button m_DocumentsButton;
        private UIToolkitListView m_KeywordsListView;
        private Button m_KeywordsButton;
        private List<SearchDatabase.Settings> m_IndexSettingsTemplates;
        private TextField m_IndexNameTextField;
        private ScrollView m_IndexDetailsElementScrollView;
        [SerializeField] private string m_WindowId;

        private static string k_ProjectPath { get { return Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length); } }

        private static int s_SelectedAssetOnOpen;
        private int m_PreviousSelectedIndex = -1;
        private int m_IndexToInsertPackagesOnToggle = -1;

        int selectedIndex => m_ListViewIndexSettings != null ? m_ListViewIndexSettings.selectedIndex : -1;
        IndexManagerViewModel selectedItem => m_IndexSettings != null && selectedIndex >= 0 && selectedIndex < m_IndexSettings.Count ? m_IndexSettings[selectedIndex] : null;
        SearchDatabase selectedItemAsset => m_IndexSettingsAssets != null && selectedIndex >= 0 && selectedIndex < m_IndexSettingsAssets.Count ? m_IndexSettingsAssets[selectedIndex] : null;
        string selectedItemPath => m_IndexSettingsFilePaths != null && selectedIndex >= 0 && selectedIndex < m_IndexSettingsFilePaths.Count ? m_IndexSettingsFilePaths[selectedIndex] : null;
        bool selectedItemExists => m_IndexSettingsExists != null && selectedIndex >= 0 && selectedIndex < m_IndexSettingsExists.Count ? m_IndexSettingsExists[selectedIndex] : false;

        internal void OnEnable()
        {
            titleContent.image = Icons.quicksearch;
            titleContent.text = "Search Index Manager";

            Utils.AddStyleSheet(rootVisualElement, "IndexManager.uss");
            Utils.AddStyleSheet(rootVisualElement, EditorGUIUtility.isProSkin ? "IndexManager_Dark.uss" : "IndexManager_Light.uss");

            rootVisualElement.AddToClassList("index-manager-variables");
            rootVisualElement.style.flexDirection = FlexDirection.Row;

            m_IndexSettings = new List<IndexManagerViewModel>();
            m_IndexSettingsAssets = new List<SearchDatabase>();
            m_IndexSettingsFilePaths = new List<string>();
            m_IndexSettingsExists = new List<bool>();

            int indexToSelect = AddSearchDatabases(SearchSettings.showPackageIndexes);

            m_ListViewIndexSettings = new ListViewIndexSettings(m_IndexSettings, MakeIndexItem, BindIndexItem, CreateNewIndexSettingMenu, DeleteIndexSetting, this, false, 40) { name = "IndexListView" };
            m_ListViewIndexSettings.ListView.onSelectionChange += OnSelectedIndexChanged;

            m_IndexDetailsElement = new VisualElement() { name = "Details" };
            SearchDatabase.indexLoaded += OnIndexLoaded;
            if (m_IndexSettings.Any())
                CreateIndexDetailsElement();

            var showPackagesToggle = new Toggle("Show package indexes") { name = "show-package-indexes-toggle", value = SearchSettings.showPackageIndexes };
            showPackagesToggle.Q<Label>().style.marginRight = 10;
            showPackagesToggle.style.marginBottom = 6;
            showPackagesToggle.style.marginLeft = 8;
            showPackagesToggle.RegisterValueChangedCallback(evt => OnPackageToggle(evt));
            m_ListViewIndexSettings.Add(showPackagesToggle);

            var splitter = new VisualSplitter(m_ListViewIndexSettings, m_IndexDetailsElement, FlexDirection.Row);
            rootVisualElement.Add(splitter);

            m_IndexSettingsTemplates = new List<SearchDatabase.Settings>();
            foreach (var templateName in SearchDatabaseTemplates.all.Keys.Where(k => k[0] != '_'))
            {
                m_IndexSettingsTemplates.Add(ExtractIndexFromTemplate(templateName));
            }

            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnSizeChange);

            m_ListViewIndexSettings.SetSelection(indexToSelect);
        }

        private void OnPackageToggle(ChangeEvent<bool> evt)
        {
            int indexToSelectToggle = selectedIndex;
            if (evt.newValue)
            {
                if (m_IndexToInsertPackagesOnToggle < 0) // weird case that could happen: no package indexes, adding package index, toggle on: the field would be = -1 in that case so we want to initialize it so that it doesn't try to insert at -1
                {
                    m_IndexToInsertPackagesOnToggle = 0;
                    for (; m_IndexToInsertPackagesOnToggle < m_IndexSettingsFilePaths.Count; m_IndexToInsertPackagesOnToggle++)
                    {
                        if (m_IndexSettingsFilePaths[m_IndexToInsertPackagesOnToggle].CompareTo("Packages/") > 0)
                        {
                            break;
                        }
                    }
                }
                int addedItems = 0;
                foreach (var searchDatabase in SearchDatabase.Enumerate(SearchDatabase.IndexLocation.packages))
                {
                    if (m_IndexSettingsFilePaths.Contains(searchDatabase.path))
                        continue;
                    m_IndexSettingsAssets.Insert(m_IndexToInsertPackagesOnToggle, searchDatabase);
                    m_IndexSettings.Insert(m_IndexToInsertPackagesOnToggle, new IndexManagerViewModel(searchDatabase.settings, false));
                    m_IndexSettingsFilePaths.Insert(m_IndexToInsertPackagesOnToggle, searchDatabase.path);
                    m_IndexSettingsExists.Insert(m_IndexToInsertPackagesOnToggle, true);
                    addedItems++;
                }
                if (indexToSelectToggle >= m_IndexToInsertPackagesOnToggle) indexToSelectToggle += addedItems;
            }
            else
            {
                m_IndexToInsertPackagesOnToggle = -1;
                for (int i = 0; i < m_IndexSettingsFilePaths.Count;)
                {
                    if (m_IndexSettingsFilePaths[i].StartsWith("Packages/"))
                    {
                        if (m_IndexToInsertPackagesOnToggle < 0)
                            m_IndexToInsertPackagesOnToggle = i;
                        m_IndexSettingsAssets.RemoveAt(i);
                        m_IndexSettings.RemoveAt(i);
                        m_IndexSettingsFilePaths.RemoveAt(i);
                        m_IndexSettingsExists.RemoveAt(i);
                        if (indexToSelectToggle >= i) indexToSelectToggle--;
                    }
                    else
                        i++;
                }
            }
            m_ListViewIndexSettings.ListView.Refresh();
            m_ListViewIndexSettings.SetSelection(indexToSelectToggle);
            m_ListViewIndexSettings.UpdateListView();

            SearchSettings.showPackageIndexes = evt.newValue;
        }

        private int AddSearchDatabases(bool includePackages)
        {
            int indexToSelect = -1;

            foreach (var searchDatabase in SearchDatabase.Enumerate(includePackages ? SearchDatabase.IndexLocation.all : SearchDatabase.IndexLocation.assets))
            {
                m_IndexSettingsAssets.Add(searchDatabase);
                m_IndexSettings.Add(new IndexManagerViewModel(searchDatabase.settings, false));
                m_IndexSettingsFilePaths.Add(searchDatabase.path);
                m_IndexSettingsExists.Add(true);

                if (searchDatabase.GetInstanceID() == s_SelectedAssetOnOpen)
                    indexToSelect = m_IndexSettings.Count - 1;
            }
            if (indexToSelect == -1 && m_IndexSettings.Any())
                indexToSelect = 0;
            return indexToSelect;
        }

        internal void OnDisable()
        {
            m_ListViewIndexSettings.ListView.onSelectionChange -= OnSelectedIndexChanged;

            SearchDatabase.indexLoaded -= OnIndexLoaded;

            if (m_DocumentsListView != null)
                m_DocumentsListView.onSelectionChange -= PingAsset;
            if (m_DependenciesListView != null)
                m_DependenciesListView.onSelectionChange -= PingAsset;
        }

        private void OnSizeChange(GeometryChangedEvent evt)
        {
            UpdateIndexStatsListViewHeight(evt);
        }

        internal static SearchDatabase.Settings ExtractIndexFromTemplate(string name)
        {
            return ExtractIndexFromText(name, SearchDatabaseTemplates.all[name]);
        }

        private static SearchDatabase.Settings ExtractIndexFromFile(string filePath)
        {
            return ExtractIndexFromText(Path.GetFileName(filePath), File.ReadAllText(filePath));
        }

        private static SearchDatabase.Settings ExtractIndexFromText(string name, string jsonText)
        {
            var settings = JsonUtility.FromJson<SearchDatabase.Settings>(jsonText);
            settings.name = name;
            return settings;
        }

        internal void UpdateUnsavedChanges(bool hasUnsavedChanges)
        {
            if (selectedItem.hasUnsavedChanges != hasUnsavedChanges)
            {
                selectedItem.hasUnsavedChanges = hasUnsavedChanges;
                if (hasUnsavedChanges)
                    this.hasUnsavedChanges = true;
                else
                {
                    bool anySettingHasChanges = false;
                    foreach (var item in m_IndexSettings)
                    {
                        if (item.hasUnsavedChanges)
                        {
                            anySettingHasChanges = true;
                            break;
                        }
                    }

                    this.hasUnsavedChanges = anySettingHasChanges;
                }
                UpdateSaveButtonDisplay();
            }
        }

        private void CreateIndexDetailsElement()
        {
            m_IndexDetailsElement.Add(m_IndexDetailsElementScrollView = new ScrollView());
            m_IndexDetailsElementScrollView.style.flexGrow = 1;

            var buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.style.flexGrow = 0;
            buttonsContainer.style.flexShrink = 0;
            m_IndexDetailsElement.Add(buttonsContainer);
            var leftSpaceButtons = new VisualElement();
            leftSpaceButtons.style.flexGrow = 1;
            buttonsContainer.Add(leftSpaceButtons);
            buttonsContainer.Add(m_SaveButton = new Button(UpdateIndexSettings) { name = "SaveButton" });
            UpdateSaveButtonDisplay();
            buttonsContainer.Add(m_CreateButton = new Button(CreateIndexSettings) { text = "Create", name = "CreateButton" });

            m_IndexDetailsElementScrollView.Add(m_IndexFilePathTextField = new TextField("File Path") {
                name = "FilePathTextField",
                tooltip = "The path to this index in the Project", value = selectedItemPath
            });
            m_IndexFilePathTextField.SetEnabled(false);

            m_IndexDetailsElementScrollView.Add(m_IndexNameTextField = new TextField("Name") {
                name = "NameTextField",
                tooltip = "The name of this index (can be different than the file)", value = selectedItem.name
            });
            m_IndexNameTextField.RegisterValueChangedCallback(evt => { selectedItem.name = evt.newValue; UpdateUnsavedChanges(true); });

            m_IndexScore = new IntegerField("Score")
            {
                name = "IndexScore",
                tooltip = "When the Project has multiple indexes, those with higher scores take priority over those with lower scores",
                value = selectedItem.score
            };
            m_IndexScore.RegisterValueChangedCallback(evt => { selectedItem.score = evt.newValue; UpdateUnsavedChanges(true); });
            m_IndexDetailsElementScrollView.Add(m_IndexScore);

            m_RootsFoldout = CreateFoldout("Roots");
            m_RootsFoldout.tooltip = "List of root folders to start indexing from (can index single files too)";
            m_ListViewRoots = new ListViewIndexSettings(selectedItem.roots, MakeRootPathItem, BindRootPathItem, AddRootElement, RemoveRootElement, this, false);
            m_RootsFoldout.Add(m_ListViewRoots);
            m_IndexDetailsElementScrollView.Add(m_RootsFoldout);

            m_IncludesFoldout = CreateFoldout("Includes");
            m_IncludesFoldout.tooltip = "A list of files, folders, and/or file types (by extension) that this index must include";
            m_ListViewIncludes = new ListViewIndexSettings(selectedItem.includes, MakeIncludeItem, BindIncludeItem, AddIncludeElement, RemoveIncludeElement, this);
            m_IncludesFoldout.Add(m_ListViewIncludes);
            m_IndexDetailsElementScrollView.Add(m_IncludesFoldout);

            m_ExcludesFoldout = CreateFoldout("Excludes");
            m_ExcludesFoldout.tooltip = "A list of files, folders, and/or file types (by extension) that this index must exclude";
            m_ListViewExcludes = new ListViewIndexSettings(selectedItem.excludes, MakeExcludeItem, BindExcludeItem, AddExcludeElement, RemoveExcludeElement, this);
            m_ExcludesFoldout.Add(m_ListViewExcludes);
            m_IndexDetailsElementScrollView.Add(m_ExcludesFoldout);

            m_OptionsFoldout = CreateFoldout(Utils.isDeveloperBuild ? $"Options (0x{selectedItem.options.GetHashCode():X})" : "Options");
            m_IndexDetailsElementScrollView.Add(m_OptionsFoldout);

            CreateOptionsVisualElements();

            m_SavedIndexDataNotLoadedYet = new Label("Loading the index...") { name = "SavedIndexDataNotLoadedYet" };
            m_IndexDetailsElementScrollView.Add(m_SavedIndexDataNotLoadedYet);
            m_SavedIndexDataNotLoadedYet.style.display = DisplayStyle.None;
            m_SavedIndexData = new VisualElement() { name = "SavedIndexData" };
            m_SavedIndexData.style.flexGrow = 1;
            m_SavedIndexData.style.flexShrink = 0;
            m_SavedIndexData.style.paddingTop = 23; //tab size
            m_IndexDetailsElementScrollView.Add(m_SavedIndexData);

            m_DependenciesListView = new UIToolkitListView() { itemHeight = 20, makeItem = () => { return new Label(); }, bindItem = (e, i) => { e.Q<Label>().text = (string)(m_DependenciesListView.itemsSource[i]); } };
            m_DependenciesListView.AddToClassList("PreviewListView");
            m_DependenciesListView.onSelectionChange += PingAsset;
            m_SavedIndexData.Add(m_DependenciesListView);

            m_DocumentsListView = new UIToolkitListView() { itemHeight = 20, makeItem = () => { return new Label(); }, bindItem = (e, i) => { e.Q<Label>().text = (string)(m_DocumentsListView.itemsSource[i]); } };
            m_DocumentsListView.AddToClassList("PreviewListView");
            m_DocumentsListView.onSelectionChange += PingAsset;
            m_SavedIndexData.Add(m_DocumentsListView);

            m_KeywordsListView = new UIToolkitListView() { itemHeight = 20, makeItem = () => { return new Label(); }, bindItem = (e, i) => { e.Q<Label>().text = (string)(m_KeywordsListView.itemsSource[i]); } };
            m_KeywordsListView.AddToClassList("PreviewListView");
            m_SavedIndexData.Add(m_KeywordsListView);

            var tabContainer = new VisualElement() { name = "IndexPreviewTabs" };
            m_SavedIndexData.Add(tabContainer);

            m_DependenciesButton = new Button(() => SelectTab(0));
            m_DocumentsButton = new Button(() => SelectTab(1));
            m_KeywordsButton = new Button(() => SelectTab(2));
            tabContainer.Add(m_DependenciesButton);
            tabContainer.Add(m_DocumentsButton);
            tabContainer.Add(m_KeywordsButton);

            m_IndexDetailsElementScrollView.Q<VisualElement>("unity-content-container").RegisterCallback<GeometryChangedEvent>(UpdateIndexStatsListViewHeight);

            SelectTab(1);

            UpdateDetailsForNewOrExistingSettings();
        }

        private void OnIndexLoaded(SearchDatabase sb)
        {
            if (selectedItemAsset != null && sb.GetInstanceID() == selectedItemAsset.GetInstanceID())
                UpdatePreviewCheckIfNeedDelay();
            m_ListViewIndexSettings.ListView.Refresh();
        }

        private void CreateOptionsVisualElements()
        {
            foreach (var field in typeof(SearchDatabase.Options).GetFields())
            {
                if (!(field.GetValue(selectedItem.options) is bool optionValue))
                    continue;
                string name = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
                var toggle = new Toggle(name) { value = optionValue, name = "OptionsToggle" + field.Name };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(selectedItem.options, evt.newValue);
                    UpdateUnsavedChanges(true);
                });

                m_OptionsFoldout.Add(toggle);
                switch (field.Name)
                {
                    case "disabled":
                        toggle.tooltip = "Toggles this index off so search does not use it";
                        toggle.RegisterValueChangedCallback(evt => m_ListViewIndexSettings.ListView.Refresh());
                        break;
                    case "files":
                        toggle.tooltip = "Include file paths in this index";
                        break;
                    case "types":
                        toggle.tooltip = "Include object type information in this index";
                        break;
                    case "properties":
                        toggle.tooltip = "Include objects' serialized properties in this index";
                        break;
                    case "extended":
                        toggle.tooltip = "Include as many properties as possible";
                        break;
                    case "dependencies":
                        toggle.tooltip = "Include information about objects' direct dependencies in this index";
                        break;
                }
            }
        }

        private void UpdateOptionsVisualElements()
        {
            foreach (var field in typeof(SearchDatabase.Options).GetFields())
            {
                m_OptionsFoldout.Q<Toggle>("OptionsToggle" + field.Name).value = (bool)field.GetValue(selectedItem.options);
            }
        }

        private void PingAsset(IEnumerable<object> obj)
        {
            if (obj.Any())
            {
                string path = (string)obj.First();
                if (Path.HasExtension(path)) // In case of Scene and Prefab index, it can give only objects ids so in that case we can't ping
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            }
        }

        private void UpdatePreviewCheckIfNeedDelay()
        {
            if (selectedItemAsset && selectedItemAsset.index != null) // index is initialized during OnEnable too so when this method is called during OnEnable we must do a delayCall
                UpdatePreview();
            else
                EditorApplication.delayCall += UpdatePreview;
        }

        private void UpdatePreview()
        {
            if (selectedItemExists && selectedItemAsset)
            {
                if (selectedItemAsset.index == null || !selectedItemAsset.index.IsReady())
                {
                    m_SavedIndexDataNotLoadedYet.style.display = DisplayStyle.Flex;
                    m_SavedIndexData.style.display = DisplayStyle.None;
                }
                else
                {
                    m_SavedIndexDataNotLoadedYet.style.display = DisplayStyle.None;
                    m_SavedIndexData.style.display = DisplayStyle.Flex;
                    var documentTitle = "";
                    if (selectedItemAsset.index is SceneIndexer objectIndexer)
                    {
                        m_DependenciesButton.style.display = DisplayStyle.Flex;

                        var dependencies = objectIndexer.GetDependencies();
                        UpdateIndexPreviewListView(dependencies, m_DependenciesListView);
                        m_DependenciesButton.text = $"{dependencies.Count} Documents";

                        documentTitle = "Objects";
                    }
                    else
                    {
                        if (m_DependenciesButton.style.display != DisplayStyle.None)
                        {
                            m_DependenciesButton.style.display = DisplayStyle.None;
                            SelectTab(1);
                        }
                        documentTitle = "Documents";
                    }

                    UpdateIndexPreviewListView(selectedItemAsset.index.GetDocuments(true).OrderBy(p => p.id).Select(d => d.id).ToList(), m_DocumentsListView);
                    m_DocumentsButton.text = $"{selectedItemAsset.index.documentCount} {documentTitle}";

                    UpdateIndexPreviewListView(selectedItemAsset.index.GetKeywords().OrderBy(p => p).ToList(), m_KeywordsListView);
                    m_KeywordsButton.text = $"{selectedItemAsset.index.keywordCount} Keywords";
                }
            }
        }

        private void UpdateIndexStatsListViewHeight(GeometryChangedEvent evt)
        {
            if (selectedItemExists)
            {
                var container = m_IndexDetailsElement.Q<VisualElement>("SavedIndexData");
                var totalContainerHeight = m_IndexDetailsElementScrollView.resolvedStyle.height;
                var usedSpace = m_IndexDetailsElementScrollView.Q<VisualElement>("unity-content-container").resolvedStyle.height;

                // The available space is the total container height (the entire scrollview) - the needed space (the content of the scrollview) + the current list view height
                // if the space is not big enough, there is still a fixed height for the list view (in that case there will be a scrollbar)
                var availableSpace = Math.Max((totalContainerHeight - usedSpace) + container.resolvedStyle.height, 150);
                container.style.height = availableSpace;// - 30;
            }
        }

        private void UpdateIndexPreviewListView(IList list, UIToolkitListView listView)
        {
            listView.itemsSource = list;
        }

        private void SelectTab(int tab)
        {
            switch (tab)
            {
                case 0:
                    m_DependenciesButton.RemoveFromClassList("IndexPreviewNotSelectedTab");
                    m_DependenciesButton.AddToClassList("IndexPreviewSelectedTab");
                    m_DocumentsButton.RemoveFromClassList("IndexPreviewSelectedTab");
                    m_DocumentsButton.AddToClassList("IndexPreviewNotSelectedTab");
                    m_KeywordsButton.RemoveFromClassList("IndexPreviewSelectedTab");
                    m_KeywordsButton.AddToClassList("IndexPreviewNotSelectedTab");
                    m_DependenciesListView.style.display = DisplayStyle.Flex;
                    m_DocumentsListView.style.display = DisplayStyle.None;
                    m_KeywordsListView.style.display = DisplayStyle.None;
                    break;
                case 1:
                    m_DependenciesButton.RemoveFromClassList("IndexPreviewSelectedTab");
                    m_DependenciesButton.AddToClassList("IndexPreviewNotSelectedTab");
                    m_DocumentsButton.RemoveFromClassList("IndexPreviewNotSelectedTab");
                    m_DocumentsButton.AddToClassList("IndexPreviewSelectedTab");
                    m_KeywordsButton.RemoveFromClassList("IndexPreviewSelectedTab");
                    m_KeywordsButton.AddToClassList("IndexPreviewNotSelectedTab");
                    m_DependenciesListView.style.display = DisplayStyle.None;
                    m_DocumentsListView.style.display = DisplayStyle.Flex;
                    m_KeywordsListView.style.display = DisplayStyle.None;
                    break;
                case 2:
                    m_DependenciesButton.RemoveFromClassList("IndexPreviewSelectedTab");
                    m_DependenciesButton.AddToClassList("IndexPreviewNotSelectedTab");
                    m_DocumentsButton.RemoveFromClassList("IndexPreviewSelectedTab");
                    m_DocumentsButton.AddToClassList("IndexPreviewNotSelectedTab");
                    m_KeywordsButton.RemoveFromClassList("IndexPreviewNotSelectedTab");
                    m_KeywordsButton.AddToClassList("IndexPreviewSelectedTab");
                    m_DependenciesListView.style.display = DisplayStyle.None;
                    m_DocumentsListView.style.display = DisplayStyle.None;
                    m_KeywordsListView.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        private Foldout CreateFoldout(string title)
        {
            Foldout roots = new Foldout();
            roots.style.flexShrink = 0;
            var toggle = roots.Q<Toggle>();
            toggle.text = title;
            return roots;
        }

        private void UpdateDetailsForNewOrExistingSettings()
        {
            if (selectedIndex >= 0)
            {
                if (selectedItemExists)
                {
                    m_IndexFilePathTextField.style.display = DisplayStyle.Flex;
                    m_IndexNameTextField.style.display = DisplayStyle.Flex;
                    m_RootsFoldout.value = false;
                    m_IncludesFoldout.value = false;
                    m_ExcludesFoldout.value = false;
                    m_OptionsFoldout.value = false;
                    m_SavedIndexData.style.display = DisplayStyle.Flex;
                    m_CreateButton.style.display = DisplayStyle.None;
                    m_SaveButton.style.display = DisplayStyle.Flex;

                    UpdatePreviewCheckIfNeedDelay();
                }
                else
                {
                    m_IndexFilePathTextField.style.display = DisplayStyle.None;
                    m_IndexNameTextField.style.display = DisplayStyle.None;
                    m_RootsFoldout.value = true;
                    m_IncludesFoldout.value = true;
                    m_ExcludesFoldout.value = true;
                    m_OptionsFoldout.value = true;
                    m_SavedIndexData.style.display = DisplayStyle.None;
                    m_CreateButton.style.display = DisplayStyle.Flex;
                    m_SaveButton.style.display = DisplayStyle.None;
                }
            }
        }

        private void CreateIndexSettings()
        {
            var path = EditorUtility.SaveFilePanel("Save Index Settings", Application.dataPath, selectedItem.name, k_IndexExtension);
            if (!string.IsNullOrEmpty(path) && Utils.IsPathUnderProject(path))
            {
                path = path.Substring(k_ProjectPath.Length + 1); // Only the project part
                CreateIndexSettings(path);
            }
        }

        private void CreateIndexSettings(string path)
        {
            SetupNewAsset(path);
            if (CreateOrUpdateAsset(path))
            {
                m_IndexSettingsExists[selectedIndex] = true;
                m_IndexSettingsFilePaths[selectedIndex] = path;
                m_IndexFilePathTextField.value = selectedItemPath;
                SearchDatabase.ImportAsset(selectedItemPath);
                m_IndexSettingsAssets[selectedIndex] = (SearchDatabase)AssetDatabase.LoadAssetAtPath(selectedItemPath, typeof(SearchDatabase));

                m_IndexNameTextField.SetValueWithoutNotify(selectedItem.name); // Update the textfield with the file name
                m_ListViewIndexSettings.ListView.Refresh();
                UpdateDetailsForNewOrExistingSettings();
                UpdateUnsavedChanges(false);
            }
        }

        private void SetupNewAsset(string path)
        {
            if (m_IndexSettingsAssets[selectedIndex] == null) // 'else' should not appear but just in case
            {
                var newItem = ScriptableObject.CreateInstance<SearchDatabase>();
                newItem.settings = new SearchDatabase.Settings()
                {
                    options = new SearchDatabase.Options()
                };
                m_IndexSettingsAssets[selectedIndex] = newItem;
            }
            selectedItem.name = Path.GetFileNameWithoutExtension(path); // Initialize with the file name
        }

        private bool CreateOrUpdateAsset(string path)
        {
            selectedItem.UpdateAsset(m_IndexSettingsAssets[selectedIndex], path);
            try
            {
                var json = JsonUtility.ToJson(m_IndexSettingsAssets[selectedIndex].settings, true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }
            UpdateSaveButtonDisplay();
            return true;
        }

        private void UpdateIndexSettings()
        {
            if (m_SaveButton.text == k_BuildText)
                SendIndexEvent(SearchAnalytics.GenericEventType.IndexManagerBuildIndex, m_IndexSettings[selectedIndex]);
            if (CreateOrUpdateAsset(selectedItemPath))
            {
                UpdateUnsavedChanges(false);
                SearchDatabase.ImportAsset(selectedItemPath);
                m_ListViewIndexSettings.ListView.Refresh();
                UpdatePreviewCheckIfNeedDelay();
            }
        }

        public override void SaveChanges()
        {
            if (hasUnsavedChanges)
            {
                for (int i = 0; i < m_IndexSettings.Count; ++i)
                {
                    if (m_IndexSettings[i].hasUnsavedChanges)
                    {
                        m_ListViewIndexSettings.SetSelectionWithoutNotify(i);
                        SendSaveIndexEvent(m_IndexSettings[i]);
                        if (!m_IndexSettingsExists[i])
                        {
                            var path = EditorUtility.SaveFilePanel("Save Index Settings", Application.dataPath, selectedItem.name, k_IndexExtension);
                            if (!string.IsNullOrEmpty(path) && Utils.IsPathUnderProject(path))
                            {
                                path = path.Substring(k_ProjectPath.Length + 1); // Only the project part
                                SetupNewAsset(path);
                                CreateOrUpdateAsset(path);
                                SearchDatabase.ImportAsset(path);
                            }
                        }
                        else
                        {
                            CreateOrUpdateAsset(m_IndexSettingsFilePaths[i]);
                            SearchDatabase.ImportAsset(m_IndexSettingsFilePaths[i]);
                        }
                    }
                }
            }
        }

        private void SendIndexEvent(SearchAnalytics.GenericEventType type, IndexManagerViewModel model)
        {
            SearchAnalytics.SendEvent(m_WindowId, SearchAnalytics.GenericEventType.IndexManagerRemoveIndex, model.type.ToString(),  $"0x{model.options.GetHashCode():X}");
        }

        private void SendSaveIndexEvent(IndexManagerViewModel model)
        {
            var evt = SearchAnalytics.GenericEvent.Create(m_WindowId, SearchAnalytics.GenericEventType.IndexManagerSaveModifiedIndex, model.type.ToString());
            evt.message = $"0x{model.options.GetHashCode():X}";
            if (model.roots.Count > 0)
                evt.description = "roots:" + string.Join(",", model.roots);
            if (model.includes.Count > 0)
                evt.stringPayload1 = "includes:" + string.Join(",", model.includes);
            if (model.excludes.Count > 0)
                evt.stringPayload2 = "excludes:" + string.Join(",", model.excludes);
            SearchAnalytics.SendEvent(evt);
        }

        private void UpdateSaveButtonDisplay()
        {
            if (selectedItem.hasUnsavedChanges)
                m_SaveButton.text = "Save";
            else
                m_SaveButton.text = k_BuildText;
        }

        private VisualElement AddRemoveButtons(Action add, Action remove)
        {
            var container = new VisualElement() { name = "Footer" };
            container.style.flexDirection = FlexDirection.Row;
            var leftSpace = new VisualElement();
            leftSpace.style.flexDirection = FlexDirection.Row;
            leftSpace.style.flexGrow = 1;
            container.Add(leftSpace);
            var buttonsContainer = new VisualElement() { name = "AddRemoveButtons" };
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.Add(new Button(add) { name = "AddButton" });
            buttonsContainer.Add(new Button(remove) { name = "RemoveButton" });
            foreach (var item in buttonsContainer.Children())
            {
                item.RemoveFromClassList("unity-button");
                item.RemoveFromClassList("unity-text-element");
            }
            container.Add(buttonsContainer);
            return container;
        }

        private void RemoveExcludeElement()
        {
            RemoveListElement(selectedItem.excludes, m_ListViewExcludes);
        }

        private void AddExcludeElement()
        {
            AddListElement(selectedItem.excludes, m_ListViewExcludes);
        }

        private void RemoveIncludeElement()
        {
            RemoveListElement(selectedItem.includes, m_ListViewIncludes);
        }

        private void AddIncludeElement()
        {
            AddListElement(selectedItem.includes, m_ListViewIncludes);
        }

        private void RemoveRootElement()
        {
            RemoveListElement(selectedItem.roots, m_ListViewRoots);
        }

        private void AddRootElement()
        {
            selectedItem.roots.Add("");
            m_ListViewRoots.UpdateListViewOnAdd();
        }

        private void AddListElement(List<string> list, ListViewIndexSettings listView)
        {
            list.Add("");

            listView.UpdateListViewOnAdd();
        }

        private void RemoveListElement(List<string> list, ListViewIndexSettings listView)
        {
            if (listView.selectedIndex >= 0)
            {
                list.RemoveAt(listView.selectedIndex);

                listView.UpdateListViewOnRemove();
            }
            UpdateUnsavedChanges(true);
        }

        private void DeleteIndexSetting()
        {
            if (selectedIndex >= 0 && EditorUtility.DisplayDialog("Delete selected index?", "You are about to delete this index, are you sure?", "Yes", "No"))
            {
                SendIndexEvent(SearchAnalytics.GenericEventType.IndexManagerRemoveIndex, m_IndexSettings[selectedIndex]);
                var deleteIndex = selectedIndex;
                string path = selectedItemPath;
                m_IndexSettings.RemoveAt(deleteIndex);
                m_IndexSettingsAssets.RemoveAt(deleteIndex);
                if (m_IndexSettingsExists[deleteIndex])
                {
                    if (File.Exists(path + ".meta"))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                    else
                    {
                        File.Delete(path);
                        AssetPostprocessorIndexer.RaiseContentRefreshed(new string[0], new string[] { path }, new string[0]);
                        AssetDatabase.Refresh();
                    }
                }
                m_IndexSettingsFilePaths.RemoveAt(deleteIndex);
                m_IndexSettingsExists.RemoveAt(deleteIndex);

                m_ListViewIndexSettings.UpdateListViewOnRemove();
            }
        }

        private void CreateNewIndexSettingMenu()
        {
            var menu = new GenericMenu();
            foreach (var template in m_IndexSettingsTemplates)
            {
                menu.AddItem(new GUIContent(template.name), false, () => { CreateNewIndexSettingFromTemplate(template); });
            }
            if (SearchDatabase.EnumeratePaths(SearchDatabase.IndexLocation.assets).Count() == 0 && !File.Exists(SearchDatabase.defaultSearchDatabaseIndexPath))
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Create Default"), false, () =>
                {
                    var defaultDB = SearchDatabase.CreateDefaultIndex();
                    var newItem = new IndexManagerViewModel(defaultDB.settings, false);
                    m_IndexSettings.Add(newItem);
                    m_IndexSettingsFilePaths.Add(defaultDB.path);
                    m_IndexSettingsExists.Add(true);
                    m_IndexSettingsAssets.Add(defaultDB);
                    SendIndexEvent(SearchAnalytics.GenericEventType.IndexManagerCreateIndex, newItem);
                    m_ListViewIndexSettings.UpdateListViewOnAdd();
                });
            }
            menu.ShowAsContext();
        }

        private void CreateNewIndexSettingFromTemplate(SearchDatabase.Settings template)
        {
            if (template == null)
            {
                Debug.LogError("The chosen template was null");
                return;
            }

            var newItem = new IndexManagerViewModel(template, true);
            if (newItem.type == SearchDatabase.IndexType.asset || EditorUtility.DisplayDialog("Create non asset index?", $"You are about to create a {template.type} index, this type of index will do a deep indexing that will be longer and take more space than a standard asset index, are you sure?", "Yes", "No"))
            {
                m_IndexSettings.Add(newItem);
                m_IndexSettingsFilePaths.Add("");
                m_IndexSettingsExists.Add(false);
                m_IndexSettingsAssets.Add(null);
                SendIndexEvent(SearchAnalytics.GenericEventType.IndexManagerCreateIndex, newItem);
                m_ListViewIndexSettings.UpdateListViewOnAdd();
            }
        }

        private VisualElement MakeIndexItem()
        {
            var container = new VisualElement();
            container.AddToClassList("index-list-element");
            container.style.flexDirection = FlexDirection.Row;

            var icon = new VisualElement() { name = "IndexTypeIcon" };
            container.Add(icon);

            var container2ndColumn = new VisualElement();
            container2ndColumn.style.flexGrow = 1;
            container.Add(container2ndColumn);

            var container1stRow = new VisualElement();
            container1stRow.style.flexGrow = 1.0f;
            container1stRow.style.height = new Length(50, LengthUnit.Percent);
            container1stRow.style.flexDirection = FlexDirection.Row;
            container2ndColumn.Add(container1stRow);

            var name = new Label() { name = "IndexName" };
            container1stRow.Add(name);

            container2ndColumn.Add(AddIndexBasicData());

            return container;
        }

        private static VisualElement AddIndexBasicData()
        {
            var container = new VisualElement() { name = "BasicDataContainer" };
            container.style.flexShrink = 0;
            container.style.flexGrow = 1.0f;
            container.style.flexDirection = FlexDirection.Row;

            container.Add(new Label() { name = "IndexSize" });
            container.Add(new Label() { name = "IndexNumber" });
            return container;
        }

        private void BindIndexItem(VisualElement element, int index)
        {
            var icon = element.Q<VisualElement>("IndexTypeIcon");
            if (m_IndexSettingsExists[index])
            {
                icon.tooltip = "Index of type " + Enum.GetName(typeof(SearchDatabase.IndexType), m_IndexSettings[index].type);
                switch (m_IndexSettings[index].type)
                {
                    case SearchDatabase.IndexType.asset:
                        icon.style.backgroundImage = new StyleBackground(Icons.quicksearch);
                        break;
                    case SearchDatabase.IndexType.prefab:
                        icon.style.backgroundImage = new StyleBackground(EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D);
                        break;
                    case SearchDatabase.IndexType.scene:
                        icon.style.backgroundImage = new StyleBackground(EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D);
                        break;
                }
                element.Q<Label>("IndexName").text = m_IndexSettings[index].name;
                element.Q<VisualElement>("BasicDataContainer").style.display = DisplayStyle.Flex;

                UpdateBasicPreview(element, index);

                element.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 1)
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Open JSon"), false, () => EditorUtility.OpenWithDefaultApp(m_IndexSettingsFilePaths[index]));
                        menu.AddItem(new GUIContent("Force rebuild"), false, () =>
                        {
                            var settings = m_IndexSettingsAssets[index].settings;
                            var indexImporterType = SearchIndexEntryImporter.GetIndexImporterType(settings.type, settings.options.GetHashCode());
                            AssetDatabaseAPI.RegisterCustomDependency(indexImporterType.GUID.ToString("N"), Hash128.Parse(Guid.NewGuid().ToString("N")));
                            SearchDatabase.ImportAsset(m_IndexSettingsFilePaths[index]);
                        });
                        menu.ShowAsContext();
                    }
                });
                element.SetEnabled(!m_IndexSettings[index].options.disabled);
            }
            else
            {
                var text = "Index settings not saved yet";
                icon.style.backgroundImage = new StyleBackground(EditorGUIUtility.IconContent("console.warnicon").image as Texture2D);
                icon.tooltip = text;
                element.Q<Label>("IndexName").text = text;
                element.Q<VisualElement>("BasicDataContainer").style.display = DisplayStyle.None;
            }
        }

        private void UpdateBasicPreview(VisualElement element, int index)
        {
            if (m_IndexSettingsExists[index] && m_IndexSettingsAssets[index])
            {
                element.Q<Label>("IndexSize").text = "Size: " + EditorUtility.FormatBytes(m_IndexSettingsAssets[index].bytes?.Length ?? 0);
                element.Q<Label>("IndexNumber").text = m_IndexSettingsAssets[index].index.indexCount.ToString() + " elements";
            }
        }

        private void OnSelectedIndexChanged(IEnumerable<object> obj)
        {
            if (selectedIndex != m_PreviousSelectedIndex)
            {
                if (m_DependenciesListView != null)
                    m_DependenciesListView.onSelectionChange -= PingAsset;
                if (m_DocumentsListView != null)
                    m_DocumentsListView.onSelectionChange -= PingAsset;
                m_IndexDetailsElement.Clear();

                if (obj.Any())
                {
                    CreateIndexDetailsElement();
                }

                if (selectedItemExists)
                    EditorGUIUtility.PingObject(selectedItemAsset);
            }

            m_PreviousSelectedIndex = selectedIndex;
        }

        private VisualElement MakeRootPathItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            var objectField = new ObjectField() { name = "RootObjectField" };
            objectField.allowSceneObjects = false;
            objectField.objectType = typeof(DefaultAsset);
            container.Add(objectField);
            Button button;
            container.Add(button = new Button(() =>
            {
                var path = EditorUtility.OpenFolderPanel("Select Root Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(k_ProjectPath))
                {
                    path = path.Substring(k_ProjectPath.Length + 1); // Only the project part

                    objectField.value = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                }
            })
                { name = "RootButton" });
            objectField.RegisterValueChangedCallback(e =>
            {
                var assetPath = AssetDatabase.GetAssetPath(e.newValue);
                if (!Directory.Exists(assetPath))
                    assetPath = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                objectField.SetValueWithoutNotify(AssetDatabase.LoadMainAssetAtPath(assetPath));
                selectedItem.roots[(int)container.userData] = assetPath;
                UpdateUnsavedChanges(true);
            });
            return container;
        }

        private void BindRootPathItem(VisualElement element, int index)
        {
            element.userData = index;
            element.Q<ObjectField>().value = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedItem.roots[index]);
            element.Q<ObjectField>().tooltip = selectedItem.roots[index];
        }

        private VisualElement MakeIncludeItem()
        {
            return new IncludeExcludePathElement(m_ListViewIncludes, this);
        }

        private VisualElement MakeExcludeItem()
        {
            return new IncludeExcludePathElement(m_ListViewExcludes, this);
        }

        private void BindIncludeItem(VisualElement element, int index)
        {
            ((IncludeExcludePathElement)element).UpdateValues(selectedItem.includes[index], index);
        }

        private void BindExcludeItem(VisualElement element, int index)
        {
            ((IncludeExcludePathElement)element).UpdateValues(selectedItem.excludes[index], index);
        }

        private class IncludeExcludePathElement : VisualElement
        {
            ListViewIndexSettings m_PathsListView;
            IndexManager m_Window;

            string m_Path;

            FilePattern m_Pattern;
            EnumField m_EnumField;
            TextField m_PrefixTextField;
            TextField m_PathTextField;
            TextField m_SuffixTextField;
            Button m_ExplorerButton;

            public IncludeExcludePathElement(ListViewIndexSettings pathsListView, IndexManager window)
            {
                m_PathsListView = pathsListView;
                m_Window = window;

                style.flexGrow = 1.0f;
                style.flexDirection = FlexDirection.Row;

                var grip = new VisualElement() { name = "ReorderableListViewGrip" };
                Add(grip);

                Add(m_EnumField = new EnumField(FilePattern.File));
                m_EnumField.RegisterValueChangedCallback(FilePatternChanged);

                Add(m_PrefixTextField = new TextField() { name = "Prefix", value = "." });
                Add(m_PathTextField = new DraggableTextField(this) { name = "PathTextField" });
                m_PathTextField.RegisterValueChangedCallback(evt =>
                {
                    PathTextFieldValueChanged();
                });
                m_PathTextField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    if (!string.IsNullOrEmpty(m_PathTextField.value) && m_PathTextField.value.Substring(m_PathTextField.value.Length - 1, 1) == "/")
                        m_PathTextField.value = m_PathTextField.value.Substring(0, m_PathTextField.value.Length - 1);
                    PathTextFieldValueChanged();
                });
                Add(m_SuffixTextField = new TextField() { name = "Suffix", value = "/" });
                Add(m_ExplorerButton = new Button(ChooseFolder));
            }

            private class DraggableTextField : TextField
            {
                private IncludeExcludePathElement m_Parent;
                public DraggableTextField(IncludeExcludePathElement parent)
                {
                    m_Parent = parent;
                }

                protected override void ExecuteDefaultActionAtTarget(EventBase evt)
                {
                    base.ExecuteDefaultActionAtTarget(evt);

                    if (evt == null)
                    {
                        return;
                    }
                    if (evt.eventTypeId == DragUpdatedEvent.TypeId())
                        OnDragUpdated(evt);
                    else if (evt.eventTypeId == DragPerformEvent.TypeId())
                        OnDragPerform(evt);
                }

                private void OnDragUpdated(EventBase evt)
                {
                    UnityEngine.Object draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
                    if (draggedObject != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                        evt.StopPropagation();
                    }
                }

                private void OnDragPerform(EventBase evt)
                {
                    UnityEngine.Object draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
                    if (draggedObject != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        value = AssetDatabase.GetAssetPath(draggedObject);

                        if (ProjectWindowUtil.IsFolder(draggedObject.GetInstanceID()))
                            m_Parent.m_EnumField.value = FilePattern.Folder;
                        else
                            m_Parent.m_EnumField.value = FilePattern.File;

                        DragAndDrop.AcceptDrag();

                        evt.StopPropagation();
                    }
                }
            }

            private void PathTextFieldValueChanged()
            {
                if ((int)userData < m_PathsListView.itemsSource.Count)
                {
                    m_PathsListView.itemsSource[(int)userData] = UpdateAndGetPath();
                    m_Window.UpdateUnsavedChanges(true);
                }
            }

            private void FilePatternChanged(ChangeEvent<Enum> evt)
            {
                m_Pattern = (FilePattern)evt.newValue;
                FilePatternChanged();
                m_PathsListView.itemsSource[(int)userData] = m_Path;
                m_Window.UpdateUnsavedChanges(true);
            }

            private void FilePatternChanged()
            {
                m_PrefixTextField.style.display = DisplayStyle.None;
                m_SuffixTextField.style.display = DisplayStyle.None;
                m_ExplorerButton.style.display = DisplayStyle.None;
                switch (m_Pattern)
                {
                    case FilePattern.Extension:
                        m_PrefixTextField.style.display = DisplayStyle.Flex;
                        break;
                    case FilePattern.Folder:
                        m_SuffixTextField.style.display = DisplayStyle.Flex;
                        m_ExplorerButton.style.display = DisplayStyle.Flex;
                        m_ExplorerButton.text = "Choose folder";
                        break;
                    case FilePattern.File:
                        m_ExplorerButton.style.display = DisplayStyle.Flex;
                        m_ExplorerButton.text = "Choose file";
                        break;
                }
                UpdateAndGetPath();
            }

            private string UpdateAndGetPath()
            {
                switch (m_Pattern)
                {
                    case FilePattern.Extension:
                        m_Path = "." + m_PathTextField.value;
                        break;
                    case FilePattern.Folder:
                        m_Path = m_PathTextField.value + "/";
                        break;
                    default:
                        m_Path = m_PathTextField.value;
                        break;
                }
                return m_Path;
            }

            internal void UpdateValues(string path, int index)
            {
                userData = index;
                UpdateValues(path);
            }

            internal void UpdateValues(string path)
            {
                m_Pattern = ObjectIndexer.GetFilePattern(path);
                m_EnumField.value = m_Pattern;
                FilePatternChanged();
                m_Path = path;
                if (m_Pattern == FilePattern.Extension)
                    m_PathTextField.value = m_Path.Remove(0, 1);
                else if (m_Pattern == FilePattern.Folder)
                    m_PathTextField.value = m_Path.Remove(m_Path.Length - 1, 1);
                else m_PathTextField.value = m_Path;
            }

            private void ChooseFolder()
            {
                if (m_Pattern == FilePattern.Folder)
                {
                    string folderName = IndexManager.ChooseFolder(m_Path, m_ExplorerButton.text) + "/";
                    UpdateValues(folderName);
                }
                else if (m_Pattern == FilePattern.File)
                {
                    var fileName = "";

                    var result = EditorUtility.OpenFilePanel(m_ExplorerButton.text, Application.dataPath, "");
                    if (!string.IsNullOrEmpty(result))
                    {
                        result = Utils.CleanPath(result);
                        if (File.Exists(result) && Utils.IsPathUnderProject(result))
                        {
                            fileName = result.Substring(k_ProjectPath.Length + 1); // Only the project part
                        }
                    }
                    UpdateValues(fileName);
                }
            }
        }
        internal static string ChooseFolder(string path, string title)
        {
            var folderName = "";

            var result = EditorUtility.OpenFolderPanel(title, Application.dataPath, folderName);
            if (!string.IsNullOrEmpty(result))
            {
                result = Utils.CleanPath(result);
                if (Directory.Exists(result) && Utils.IsPathUnderProject(result))
                {
                    folderName = result.Substring(k_ProjectPath.Length + 1); // Only the project part
                }
            }

            return folderName;
        }

        class IndexManagerViewModel
        {
            public string name;
            public SearchDatabase.IndexType type;
            public int score;
            public List<string> roots;
            public List<string> includes;
            public List<string> excludes;
            public SearchDatabase.Options options;
            public bool hasUnsavedChanges;

            public IndexManagerViewModel()
            {
                name = "Index New";
                this.roots = new List<string>();
                this.includes = new List<string>();
                this.excludes = new List<string>();
                this.options = new SearchDatabase.Options();
            }

            public IndexManagerViewModel(SearchDatabase.Settings searchDatabaseSettings, bool newItem) : this()
            {
                this.name = !newItem ? searchDatabaseSettings.name : null;
                type = (SearchDatabase.IndexType)Enum.Parse(typeof(SearchDatabase.IndexType), searchDatabaseSettings.type);
                this.score = searchDatabaseSettings.baseScore;
                this.roots = new List<string>();
                if (searchDatabaseSettings.roots != null)
                    this.roots.AddRange(searchDatabaseSettings.roots);
                this.includes = new List<string>();
                if (searchDatabaseSettings.includes != null)
                    this.includes.AddRange(searchDatabaseSettings.includes);
                this.excludes = new List<string>();
                if (searchDatabaseSettings.excludes != null)
                    this.excludes.AddRange(searchDatabaseSettings.excludes);
                SetOptions(this.options, searchDatabaseSettings.options);
            }

            internal void UpdateAsset(SearchDatabase searchDatabase, string path)
            {
                if (name == Path.GetFileNameWithoutExtension(path))
                    // we need to force to null in that case (on creation it's not needed because searchDatabase.settings.name is empty but on an existing index, it has the filename by default)
                    searchDatabase.settings.name = null;
                else
                    searchDatabase.settings.name = name;

                // we don't change the path, the user can't change it from the UI, only modifying the Json directly
                // it is null at creation, in that case, it is automatically set when importing
                //searchDatabase.settings.path = path;

                searchDatabase.settings.type = Enum.GetName(typeof(SearchDatabase.IndexType), type);
                searchDatabase.settings.baseScore = score;
                searchDatabase.settings.roots = roots.ToArray();
                searchDatabase.settings.includes = includes.ToArray();
                searchDatabase.settings.excludes = excludes.ToArray();
                SetOptions(searchDatabase.settings.options, this.options);
            }

            private static void SetOptions(SearchDatabase.Options optionsToModify, SearchDatabase.Options input)
            {
                foreach (var field in typeof(SearchDatabase.Options).GetFields())
                {
                    var value = field.GetValue(input);
                    field.SetValue(optionsToModify, value);
                }
            }
        }
    }


    internal class ListViewIndexSettings : VisualElement
    {
        const int k_DefaultItemHeight = 20;
        private int m_ItemHeight;
        private Label m_EmptyListViewLabel;
        internal UIToolkitListView ListView { get; private set; }
        internal int selectedIndex { get { return ListView.selectedIndex; } }
        internal IList itemsSource { get { return ListView.itemsSource; } set { ListView.itemsSource = value; } }
        private IndexManager m_Window;

        public ListViewIndexSettings(IList itemsSource, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem, Action addButtonAction, Action removeButtonAction, IndexManager window, bool isReorderable = true, int itemHeight = k_DefaultItemHeight)
        {
            m_Window = window;
            m_ItemHeight = itemHeight;
            ListView = new UIToolkitListView(itemsSource, m_ItemHeight, makeItem, bindItem);
            var container = new VisualElement() { name = "ListViewIndexSettingsContent" };
            Add(container);
            container.Add(ListView);
            container.Add(m_EmptyListViewLabel = new Label("List is empty"));
            Add(AddRemoveButtons(addButtonAction, removeButtonAction));

            ListView.selectionType = SelectionType.Single;
            ListView.reorderable = isReorderable;
            if (itemsSource.Count > 0)
            {
                ListView.selectedIndex = 0;
                m_EmptyListViewLabel.style.display = DisplayStyle.None;
            }
            else
                ListView.style.display = DisplayStyle.None;
            UpdateHeight();
        }

        internal void UpdateHeight()
        {
            ListView.style.height = ListView.itemsSource.Count * m_ItemHeight + 2 + 4; //+2 for the borders +4 for the margins
        }

        private VisualElement AddRemoveButtons(Action add, Action remove)
        {
            var container = new VisualElement() { name = "Footer" };
            container.style.flexDirection = FlexDirection.Row;
            var leftSpace = new VisualElement();
            leftSpace.style.flexDirection = FlexDirection.Row;
            leftSpace.style.flexGrow = 1;
            container.Add(leftSpace);
            var buttonsContainer = new VisualElement() { name = "AddRemoveButtons" };
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.Add(new Button(add) { name = "AddButton" });
            buttonsContainer.Add(new Button(remove) { name = "RemoveButton" });
            foreach (var item in buttonsContainer.Children())
            {
                item.RemoveFromClassList("unity-button");
                item.RemoveFromClassList("unity-text-element");
            }
            container.Add(buttonsContainer);
            return container;
        }

        internal void UpdateListView()
        {
            UpdateHeight();
            if (ListView.itemsSource.Count == 0)
            {
                m_EmptyListViewLabel.style.display = DisplayStyle.Flex;
                ListView.style.display = DisplayStyle.None;
            }
            else
            {
                m_EmptyListViewLabel.style.display = DisplayStyle.None;
                ListView.style.display = DisplayStyle.Flex;
            }
        }

        internal void UpdateListViewOnAdd()
        {
            ListView.Refresh();
            SetSelection(itemsSource.Count - 1);
            UpdateListView();
            m_Window.UpdateUnsavedChanges(true);
        }

        internal void UpdateListViewOnRemove()
        {
            if (selectedIndex > 0 || itemsSource.Count == 0) // if == 0 and 0 then we need to go to -1
                SetSelection(selectedIndex - 1);
            ListView.Refresh();
            UpdateListView();
        }

        internal void SetSelection(int index)
        {
            ListView.SetSelection(index);
        }

        internal void SetSelectionWithoutNotify(int index)
        {
            ListView.SetSelectionWithoutNotify(new int[] { index });
        }
    }
}
