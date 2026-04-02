// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

using Unity.UI.Builder;
using UnityEditor.Search;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;
using UnityEditor.Search.Providers;

namespace UnityEditor.Experimental.GraphView
{
    internal interface ITemplateSorter : IComparer<GraphViewTemplateDescriptor>
    {
        string Label { get; }
    }

    internal class ByNameTemplateSorter : ITemplateSorter
    {
        public string Label => "Name";
        public int Compare(GraphViewTemplateDescriptor x, GraphViewTemplateDescriptor y)
        {
            return x.header.CompareTo(y.header);
        }
    }

    internal class ByDateTemplateSorter : ITemplateSorter
    {
        public string Label => "Moditication Date";

        public int Compare(GraphViewTemplateDescriptor x, GraphViewTemplateDescriptor y)
        {
            // Reverse order: most recent first
            var result = y.GetModificationDate().CompareTo(x.GetModificationDate());
            if (result != 0)
            {
                return result;
            }

            // Treat the equality appart to keep sorting stability when an items have the same modification date (e.g. built-in templates)
            return x.order.CompareTo(y.order);
        }
    }

    internal class ByOrderTemplateSorter : ITemplateSorter
    {
        public string Label => "Order";

        public int Compare(GraphViewTemplateDescriptor x, GraphViewTemplateDescriptor y)
        {
            return x.order.CompareTo(y.order);
        }
    }

    internal class ByLastUsedTemplateSorter : ITemplateSorter
    {
        private GraphViewTemplateWindowPrefs m_GraphiViewTemplateWindowPrefs;

        public ByLastUsedTemplateSorter(GraphViewTemplateWindowPrefs graphiViewTemplateWindowPrefs)
        {
            this.m_GraphiViewTemplateWindowPrefs= graphiViewTemplateWindowPrefs;
        }

        public string Label => "Last Used";

        public int Compare(GraphViewTemplateDescriptor x, GraphViewTemplateDescriptor y)
        {
            var aLastUsed = this.m_GraphiViewTemplateWindowPrefs.FindHistoryItem(x.assetGuid);
            var bLastUsed = this.m_GraphiViewTemplateWindowPrefs.FindHistoryItem(y.assetGuid);

            var result = bLastUsed.CompareTo(aLastUsed);
            if (result != 0)
            {
                return result;
            }

            // Treat the equality appart to keep sorting stability when an item has no usage history
            return x.order.CompareTo(y.order);
        }
    }

    internal class ByFavoriteTemplateSorter : ITemplateSorter
    {
        public string Label => "Favorite";

        public int Compare(GraphViewTemplateDescriptor a, GraphViewTemplateDescriptor b)
        {
            var isFavoriteA = GraphViewTemplateWindow.IsFavorite(a);
            var isFavoriteB = GraphViewTemplateWindow.IsFavorite(b);

            if (isFavoriteA == isFavoriteB)
            {
                return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            }

            return isFavoriteA ? -1 : 1;
        }
    }

    class TemplateCategorySorter : IComparer<List<GraphViewTemplateDescriptor>>
    {
        public int Compare(List<GraphViewTemplateDescriptor> x, List<GraphViewTemplateDescriptor> y)
        {
            var internalSort = x[0].internalOrder.CompareTo(y[0].internalOrder);

            return internalSort != 0
                ? internalSort
                : x[0].category.CompareTo(y[0].category);
        }
    }

    [Serializable]
    internal struct TemplateUseHistoryItem
    {
        public string toolKey;
        public string assetGuid;
        public long lastUsedTicks;

        public TemplateUseHistoryItem(string key, string guid)
        {
            this.toolKey = key;
            this.assetGuid = guid;
            this.lastUsedTicks = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    internal class GraphViewTemplateWindowPrefs
    {
        [SerializeField] private string m_LastUsedTemplateGuid;
        [SerializeField] private List<TemplateUseHistoryItem> m_UseHistoryItems = new ();
        [SerializeField] private string m_LastUsedSorter;
        [SerializeField] private List<string> m_CollapsedCategories = new ();

        public string LastUsedTemplateGuid
        {
            get => this.m_LastUsedTemplateGuid;
            set => this.m_LastUsedTemplateGuid = value;
        }

        public string LastUsedSorter
        {
            get => this.m_LastUsedSorter;
            set => this.m_LastUsedSorter = value;
        }


        public void AddHistoryItem(string toolKey, string guid)
        {
            if (m_UseHistoryItems.Find(x => x.assetGuid == guid) is {} historyItem && !string.IsNullOrEmpty(historyItem.assetGuid))
            {
                m_UseHistoryItems.Remove(historyItem);
            }
            m_UseHistoryItems.Add(new TemplateUseHistoryItem(toolKey, guid));
        }

        public long FindHistoryItem(string guid)
        {
            return this.m_UseHistoryItems.Find(x => x.assetGuid == guid).lastUsedTicks;
        }

        public void ClearPrefs(string toolKey)
        {
            EditorPrefs.DeleteKey(GetPrefsKey(toolKey));
        }

        public void SavePrefs(string toolKey)
        {
            var prefs = EditorJsonUtility.ToJson(this);
            EditorPrefs.SetString(GetPrefsKey(toolKey), prefs);
        }

        public void LoadPrefs(string toolKey)
        {
            var prefs = EditorPrefs.GetString(GetPrefsKey(toolKey));

            if (!string.IsNullOrEmpty(prefs))
            {
                EditorJsonUtility.FromJsonOverwrite(prefs, this);
            }
        }

        public void SetCategoryCollapsedState(string toolkey, string category, bool isCollapsed)
        {
            var key =  $"{toolkey}.{category}";
            if (isCollapsed && !m_CollapsedCategories.Contains(key))
            {
                m_CollapsedCategories.Add(key);
            }
            else if (!isCollapsed)
            {
                m_CollapsedCategories.Remove(key);
            }
        }

        // Returns true if expanded, false if collapsed
        public bool GetCategoryCollapsedState(string toolkey, string category) => m_CollapsedCategories.Contains($"{toolkey}.{category}");

        private string GetPrefsKey(string toolKey) => $"gvtw_{toolKey}";
    }

    internal class GraphViewTemplateWindow : EditorWindow
    {
        private const string k_FavoriteUssClass = "favorite";
        private const string k_TemplateItemUssClass = "template-item";
        private const string k_TemplateSectionUssClass = "template-section";

        internal interface ISaveFileDialogHelper
        {
            string OpenSaveFileDialog();
        }

        private class TemplateSection : ITemplateDescriptor
        {
            public TemplateSection(string text)
            {
                header = string.IsNullOrEmpty(text.Trim()) ? TemplateSearchProvider.kUncategorized : text;
            }
            public string header { get; }
        }

        private const float PackageManagerTimeout = 5f; // 5s

        private static readonly List<string> s_HideInstallSampleButtonByTool = new ();
        private readonly List<TreeViewItemData<ITemplateDescriptor>> m_TemplatesTree = new ();

        private TreeView m_ListOfTemplates;
        private Texture2D m_CustomTemplateIcon;
        private VisualElement m_DetailsScreenshot;
        private Label m_DetailsTitle;
        private Label m_DetailsDescription;
        private VisualElement m_TitleAndDoc;
        private Button m_CreateButton;
        private VisualTreeAsset m_ItemTemplate;
        private Action<string> m_AssetCreationCallback;
        private string m_LastSelectedTemplatePath;
        private CreateMode m_CurrentMode;
        private Action<string, string> m_UserCallback;
        private GraphViewTemplateDescriptor m_SelectedTemplate;
        private Button m_InstallButton;
        private SearchFieldElement m_SearchField;
        private TemplateSearchViewModel m_ViewModel;
        private TemplateSearchProvider m_SearchProvider;
        private ITemplateHelper m_TemplateHelper;
        private ITemplateSorter m_TemplateSorter;
        private ITemplateSorter[] m_DefaultTemplateSorter;
        private GraphViewTemplateWindowPrefs m_templateWindowPrefs;

        private enum CreateMode
        {
            CreateNew,
            Insert,
            None,
        }

        /// <summary>
        /// Opens a template window to create a new asset from a template
        /// </summary>
        /// <param name="templateHelper">This object provides all relevant information to customize the template window</param>
        /// <param name="callback">This callback will be called when the user validates the asset creation</param>
        /// <param name="showSaveDialog">If true, a save file dialog will be prompt to let the user pick a path to save the new asset</param>
        /// <param name="filters">A collection of filters to apply to the found templates</param>
        public static void ShowCreateFromTemplate(
            ITemplateHelper templateHelper,
            Action<string, string> callback,
            bool showSaveDialog = true,
            string hiddenSearchQuery = null,
            string initialSearchQuery = null) => ShowInternal(showSaveDialog ? CreateMode.CreateNew : CreateMode.None, templateHelper, callback, hiddenSearchQuery, initialSearchQuery, false);

        /// <summary>
        /// Opens a template window to insert a template into an existing asset
        /// </summary>
        /// <param name="templateHelper">This object provides all relevant information to customize the template window</param>
        /// <param name="callback">This callback will be called when the user validates the asset creation</param>
        /// <param name="filters">A collection of filters to apply to the found templates</param>
        public static void ShowInsertTemplate(
            ITemplateHelper templateHelper,
            Action<string, string> callback,
            string hiddenSearchQuery = null,
            string intialSearchQuery = null) => ShowInternal(CreateMode.Insert, templateHelper, callback, hiddenSearchQuery, intialSearchQuery, false);


        // For testing purpose only
        internal static void ShowCreateFromTemplateAdbOnly(
            ITemplateHelper templateHelper,
            Action<string, string> callback,
            bool showSaveDialog = true,
            string hiddenSearchQuery = null,
            string initialSearchQuery = null) => ShowInternal(showSaveDialog ? CreateMode.CreateNew : CreateMode.None, templateHelper, callback, hiddenSearchQuery, initialSearchQuery, true);

        // For testing purpose only
        internal static void ShowInsertTemplateAdbOnly(
            ITemplateHelper templateHelper,
            Action<string, string> callback,
            string hiddenSearchQuery = null,
            string intialSearchQuery = null) => ShowInternal(CreateMode.Insert, templateHelper, callback, hiddenSearchQuery, intialSearchQuery, true);

        private static void ShowInternal(CreateMode mode, ITemplateHelper templateHelper, Action<string, string> callback, string hiddenSearchQuery, string initialSearchQuery, bool adbOnly)
        {
            if (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                Debug.LogWarning("A template window is already open, close it before opening a new one.");
                return;
            }

            var templateWindow = EditorWindow.GetWindow<GraphViewTemplateWindow>(true, string.Empty, false);
            templateWindow.titleContent = new GUIContent(mode == CreateMode.Insert ? templateHelper.insertTemplateTitle : templateHelper.createNewAssetTitle);
            templateWindow.Setup(mode, templateHelper, callback, hiddenSearchQuery, initialSearchQuery, adbOnly);
        }

        private void Setup(CreateMode mode, ITemplateHelper templateHelper, Action<string, string> callback, string hiddenSearchQuery, string initialSearchQuery, bool adbOnly)
        {
            minSize = new Vector2(800, 300);
            m_UserCallback = callback;
            m_CurrentMode = mode;
            m_TemplateHelper = templateHelper;
            m_CustomTemplateIcon = EditorGUIUtility.LoadIcon(m_TemplateHelper.customTemplateIcon);
            m_SearchProvider = new TemplateSearchProvider(m_TemplateHelper, hiddenSearchQuery, adbOnly);
            m_templateWindowPrefs = new GraphViewTemplateWindowPrefs();
            m_templateWindowPrefs.LoadPrefs(m_TemplateHelper.toolKey);
            m_DefaultTemplateSorter = new ITemplateSorter[] {
                new ByNameTemplateSorter(),
                new ByOrderTemplateSorter(),
                new ByDateTemplateSorter(),
                new ByLastUsedTemplateSorter(m_templateWindowPrefs),
                new ByFavoriteTemplateSorter()
            };
            SetCallBack();
            SetupSearchAndFilter(hiddenSearchQuery, initialSearchQuery);

            // Handle the install button here because we need the template helper
            m_InstallButton = rootVisualElement.Q<Button>("InstallButton");
            if (!string.IsNullOrEmpty(m_TemplateHelper.learningSampleName)
                && !s_HideInstallSampleButtonByTool.Contains(m_TemplateHelper.toolKey)
                && TryFindSample(m_TemplateHelper.learningSampleName, out var packageInfo, out var sample) && !sample.isImported)
            {
                m_InstallButton.clicked += OnInstall;
                m_InstallButton.parent.style.display = DisplayStyle.Flex;
                var hideInstallButton = rootVisualElement.Q<Button>("HideInstallButton");
                hideInstallButton.clicked += HideInstallButton;
            }
            else
            {
                m_InstallButton.parent.style.display = DisplayStyle.None;
            }

            // Handle the package indexing banner here because it needs the template helper
            if (SearchDatabase.GetDefaultSearchDatabase().settings.IsPackagesIndexingEnabled() || !m_TemplateHelper.showPackageIndexingBanner)
            {
                HidePackageIndexingBanner();
            }
            else
            {
                var packageIndexingButton = rootVisualElement.Q<Button>("PackageIndexingButton");
                packageIndexingButton.clicked += OnEnablePackageIndexing;

                var closeIndexingBannerButton = rootVisualElement.Q<Button>("CloseBannerButton");
                closeIndexingBannerButton.clicked += OnClosePackageIndexingBanner;
            }

            var actionButton = rootVisualElement.Q<Button>("CreateButton");
            if (m_CurrentMode == CreateMode.Insert)
            {
                actionButton.text = "Insert";
            }
        }


        private void CreateGUI()
        {
            m_ItemTemplate = (VisualTreeAsset)EditorGUIUtility.Load("UXML/GraphView/TemplateItem.uxml");
            var tpl = (VisualTreeAsset)EditorGUIUtility.Load("UXML/GraphView/TemplateWindow.uxml");
            tpl.CloneTree(rootVisualElement);

            rootVisualElement.AddStyleSheetPath("StyleSheets/GraphView/TemplateWindow.uss");
            rootVisualElement.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");
            SearchElement.AppendStyleSheets(rootVisualElement);

            rootVisualElement.name = "TemplateWindowRoot";

            m_CreateButton = rootVisualElement.Q<Button>("CreateButton");
            m_CreateButton.clicked += OnCreate;
            rootVisualElement.Q<Button>("CancelButton").clicked += OnCancel;

            m_DetailsScreenshot = rootVisualElement.Q<VisualElement>("Screenshot");
            m_DetailsTitle = rootVisualElement.Q<Label>("Title");
            m_DetailsDescription = rootVisualElement.Q<Label>("Description");
            m_TitleAndDoc = rootVisualElement.Q<VisualElement>("TitleAndDoc");

            var helpButton = rootVisualElement.Q<Button>("HelpButton");
            helpButton.clicked += OnOpenHelp;
            var helpImage = helpButton.Q<Image>("HelpImage");
            helpImage.image = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "_Help.png");

            m_ListOfTemplates = rootVisualElement.Q<TreeView>("ListOfTemplates");
            m_ListOfTemplates.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            m_ListOfTemplates.makeItem = CreateTemplateItem;
            m_ListOfTemplates.bindItem = BindTemplateItem;
            m_ListOfTemplates.unbindItem = UnbindTemplateItem;
            m_ListOfTemplates.selectionChanged += OnSelectionChanged;

            Dispatcher.On(SearchEvent.ItemFavoriteStateChanged, OnFavoriteStateChanged, SearchEventManager.GetSearchEventHandlerHashCode(OnFavoriteStateChanged));
        }

        private void SetCallBack()
        {
            switch (m_CurrentMode)
            {
                case CreateMode.CreateNew:
                    m_AssetCreationCallback = CreateNewAsset;
                    break;
                case CreateMode.Insert:
                    m_AssetCreationCallback = InsertTemplateInVisualEffect;
                    break;
                case CreateMode.None:
                    m_AssetCreationCallback = x => m_UserCallback.Invoke(x, null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(m_CurrentMode), m_CurrentMode, null);
            }
        }

        private void OnOpenHelp() => Help.BrowseURL(m_TemplateHelper.templateWindowDocUrl);

        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }

        private void OnBeforeAssemblyReload()
        {
            Close();
        }

        private void OnDestroy()
        {
            Dispatcher.Off(SearchEvent.ItemFavoriteStateChanged, SearchEventManager.GetSearchEventHandlerHashCode(OnFavoriteStateChanged));
            this.m_templateWindowPrefs?.SavePrefs(this.m_TemplateHelper.toolKey);
        }

        private void HidePackageIndexingBanner()
        {
            rootVisualElement.Q<VisualElement>("PackageIndexingBanner").style.display = DisplayStyle.None;
        }

        private void OnClosePackageIndexingBanner()
        {
            this.HidePackageIndexingBanner();
            m_TemplateHelper.showPackageIndexingBanner = false;
        }

        private void OnEnablePackageIndexing()
        {
            SearchDatabase.GetDefaultSearchDatabase().settings.EnablePackagesIndexing(true);
            SearchDatabase.GetDefaultSearchDatabase().SaveSettingsOptions(startIndexing: true);
            HidePackageIndexingBanner();
        }

        private void OnCancel()
        {
            m_LastSelectedTemplatePath = null;
            m_AssetCreationCallback?.Invoke(m_LastSelectedTemplatePath);
            Close();
        }

        private void OnInstall()
        {
            if (TryFindSample(m_TemplateHelper.learningSampleName, out var packageInfo, out var samplePackage))
            {
                // Workaround for UUM-63664
                m_TemplateHelper.RaiseImportSampleDependencies(packageInfo, samplePackage);
                m_InstallButton.enabledSelf = !samplePackage.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
            }
        }

        private void HideInstallButton()
        {
            m_InstallButton.parent.style.display = DisplayStyle.None;
            s_HideInstallSampleButtonByTool.Add(m_TemplateHelper.toolKey);
        }

        private void OnCreate()
        {
            if (!string.IsNullOrEmpty(m_SelectedTemplate.assetGuid))
            {
                m_LastSelectedTemplatePath = AssetDatabase.GUIDToAssetPath(m_SelectedTemplate.assetGuid);
                m_AssetCreationCallback?.Invoke(m_LastSelectedTemplatePath);
                m_TemplateHelper.RaiseTemplateUsed(m_SelectedTemplate);
                m_AssetCreationCallback = null;
                this.m_templateWindowPrefs.AddHistoryItem(this.m_TemplateHelper.toolKey, m_SelectedTemplate.assetGuid);
                Close();
            }
        }

        private bool TryFindSample(string sampleName, out PackageManager.PackageInfo packageInfo, out Sample sample)
        {
            try
            {
                var startTime = Time.time;
                var searchRequest = Client.Search(m_TemplateHelper.packageInfoName, true);
                while (!searchRequest.IsCompleted && Time.time - startTime < PackageManagerTimeout)
                {
                    Thread.Sleep(20);
                }

                if (searchRequest is { Result: { Length: 1 }, IsCompleted: true } && searchRequest.Result[0] is { } localPackageInfo)
                {
                    foreach (var samplePackage in Sample.FindByPackage(m_TemplateHelper.packageInfoName, null))
                    {
                        if (string.Compare(samplePackage.displayName, sampleName, StringComparison.OrdinalIgnoreCase) ==
                            0)
                        {
                            packageInfo = localPackageInfo;
                            sample = samplePackage;
                            return true;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Could not determine if the {sampleName} package is installed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Something went wrong while trying to retrieve {sampleName} package info\n{ex.Message}");
            }

            packageInfo = null;
            sample = default;
            return false;
        }

        private void CreateNewAsset(string templatePath)
        {
            if (templatePath == null)
            {
                return;
            }

            var assetPath = m_TemplateHelper.saveFileDialogHelper.OpenSaveFileDialog();
            if (!string.IsNullOrEmpty(assetPath))
            {
                m_UserCallback?.Invoke(templatePath, assetPath);
            }
        }

        private void InsertTemplateInVisualEffect(string templatePath)
        {
            if (!string.IsNullOrEmpty(templatePath))
            {
                this.m_UserCallback.Invoke(templatePath, null);
            }
        }

        private void OnSelectionChanged(IEnumerable<object> newSelection)
        {
            foreach (var item in newSelection)
            {
                if (item is GraphViewTemplateDescriptor template)
                {
                    m_SelectedTemplate = template;
                    m_DetailsTitle.text = string.IsNullOrEmpty(template.name.Trim()) ? "No name" : template.name;
                    m_DetailsDescription.text = string.IsNullOrEmpty(template.description.Trim()) ? "No Description" : template.description;
                    m_templateWindowPrefs.LastUsedTemplateGuid = template.assetGuid;
                    if (template.thumbnail != null)
                    {
                        m_DetailsScreenshot.style.backgroundImage = template.thumbnail;
                        m_DetailsScreenshot.RemoveFromClassList("fallback-image");
                    }
                    else
                    {
                        m_DetailsScreenshot.style.backgroundImage = m_CustomTemplateIcon;
                        m_DetailsScreenshot.AddToClassList("fallback-image");
                    }

                    m_TitleAndDoc.style.display = DisplayStyle.Flex;
                    m_CreateButton.SetEnabled(true);

                    // We expect only one item to be selected
                    return;
                }

                // We expect only one item to be selected
                return;
            }
        }

        private void BindTemplateItem(VisualElement item, int index)
        {
            var data = m_ListOfTemplates.GetItemDataForIndex<ITemplateDescriptor>(index);
            var label = item.Q<Label>("TemplateName");
            label.text = data.header;

            string ussClass;
            string userData = null;
            var isFavorite = false;
            var parent = item.GetFirstAncestorWithClass("unity-tree-view__item");

            if (data is GraphViewTemplateDescriptor template)
            {
                userData = GetGlobalId(template);
                isFavorite = IsFavorite(userData);

                item.Q<Image>("TemplateIcon").image = template.icon != null ? template.icon : m_CustomTemplateIcon;
                if (template.assetGuid == m_templateWindowPrefs.LastUsedTemplateGuid)
                    m_ListOfTemplates.SetSelection(index);
                ussClass = k_TemplateItemUssClass;

                item.RegisterCallback<ClickEvent>(OnClickItem);

                var favoriteButton = item.Q<Button>("Favorite");
                favoriteButton.RegisterCallback<ClickEvent>(OnFavorite);
            }
            else
            {
                // This is a hack to put the expand/collapse button above the item so that we can interact with it
                var toggle = item.parent.parent.Q<Toggle>();
                toggle.BringToFront();
                toggle.RegisterCallback<ChangeEvent<bool>, ITemplateDescriptor>(OnToggleExpandCategory, data);
                ussClass = k_TemplateSectionUssClass;
            }

            if (parent != null)
            {
                parent.AddToClassList(ussClass);
                parent.userData = userData;
                ToggleFavorite(parent, isFavorite);
            }
        }

        private void OnToggleExpandCategory(ChangeEvent<bool> evt, ITemplateDescriptor item)
        {
            if (evt.target is Toggle toggle)
            {
                m_templateWindowPrefs.SetCategoryCollapsedState(m_TemplateHelper.toolKey, item.header, !toggle.value);
            }
        }

        private void OnFavorite(ClickEvent evt)
        {
            var item = ((VisualElement)evt.target).GetFirstAncestorWithClass("unity-tree-view__item");
            var globalId = (string)item.userData;
            var searchItem = new SearchItem(globalId);
            var isFavorite = IsFavorite(globalId);
            ToggleFavorite(item, isFavorite);
            if (isFavorite)
            {
                SearchSettings.RemoveItemFavorite(searchItem);
            }
            else
            {
                SearchSettings.AddItemFavorite(searchItem);
            }
            SearchSettings.Save();
            evt.StopImmediatePropagation();
        }

        private void OnFavoriteStateChanged(ISearchEvent evt)
        {
            var id = string.Empty;
            if (evt.argumentCount == 1)
                id = (string)evt.GetArgument(0);
            else
                return;

            TreeViewItemData<ITemplateDescriptor> foundItem = default;

            foreach (var item in m_TemplatesTree)
            {
                foreach (var child in item.children)
                {
                    if (child.data is GraphViewTemplateDescriptor template && GetGlobalId(template) == id)
                    {
                        foundItem = child;
                        break;
                    }
                }
                if (foundItem.data != null)
                    break;
            }

            if (m_ListOfTemplates.GetRootElementForId(foundItem.id) is { } element)
            {
                ToggleFavorite(element, IsFavorite(id));
            }
        }

        private void UnbindTemplateItem(VisualElement item, int index)
        {
            if (item.GetFirstAncestorWithClass("unity-tree-view__item") is { } parent)
            {
                if (parent.ClassListContains(k_TemplateSectionUssClass))
                {
                    var toggle = item.parent.parent.Q<Toggle>();
                    toggle.UnregisterCallback<ChangeEvent<bool>, ITemplateDescriptor>(OnToggleExpandCategory);
                }
                parent.RemoveFromClassList(k_TemplateItemUssClass);
                parent.RemoveFromClassList(k_TemplateSectionUssClass);
                parent.RemoveFromClassList(k_FavoriteUssClass);
            }
            item.UnregisterCallback<ClickEvent>(OnClickItem);
        }

        private void OnClickItem(ClickEvent evt)
        {
            if (evt.clickCount == 2 && m_ListOfTemplates.selectedItem != null)
            {
                OnCreate();
            }
        }

        private VisualElement CreateTemplateItem() => m_ItemTemplate.Instantiate();

        private void SetupSearchAndFilter(string hiddenSearchQuery, string initialSearchQuery)
        {
            var searchPanel = rootVisualElement.Q<VisualElement>("SearchPanel");
            var context = Search.SearchService.CreateContext(m_SearchProvider);
            context.useExplicitProvidersAsNormalProviders = true;
            var searchViewState = new SearchViewState(context);
            searchViewState.flags = SearchViewFlags.None;
            searchViewState.queryBuilderEnabled = true;
            m_ViewModel = new TemplateSearchViewModel(searchViewState);
            m_ViewModel.queryChanged += OnQueryChanged;
            context.searchView = m_ViewModel;
            m_SearchField = new SearchFieldElement("SearchField", m_ViewModel, SearchQueryBuilderViewFlags.Default);

            searchPanel.Add(m_SearchField);

            var allSorters = new List<ITemplateSorter>(m_DefaultTemplateSorter);
            allSorters.AddRange(this.m_TemplateHelper.GetTemplateSorter());

            var choices = new List<string>(allSorters.Count);
            allSorters.ForEach(x => choices.Add(x.Label));
            var lastUsedSorter = Math.Max(0, choices.IndexOf(this.m_templateWindowPrefs.LastUsedSorter));
            var dropDown = new DropdownField(choices, lastUsedSorter, this.FormatSortByLabel, this.FormatSortByLabel);
            dropDown.RegisterCallback<ChangeEvent<string>>(this.OnSortChanged);
            searchPanel.Add(dropDown);

            m_TemplateSorter = allSorters[lastUsedSorter];
            SetQuery(initialSearchQuery);
        }

        private string FormatSortByLabel(string label) => $"Sort By {label}";

        private void OnSortChanged(ChangeEvent<string> ev)
        {
            m_TemplateSorter = Array.Find(m_DefaultTemplateSorter, x => string.Compare(x.Label, ev.newValue, StringComparison.OrdinalIgnoreCase) == 0);
            if (m_TemplateSorter == null)
            {
                m_TemplateSorter = Array.Find(m_TemplateHelper.GetTemplateSorter(), x => string.Compare(x.Label, ev.newValue, StringComparison.OrdinalIgnoreCase) == 0);
            }

            this.m_templateWindowPrefs.LastUsedSorter = m_TemplateSorter?.Label;
            this.CollectTemplates(true);
        }

        private void CollectTemplates(IEnumerable<SearchItem> newItems)
        {
            CollectTemplates(false);
        }

        private void CollectTemplates(bool isSearchCompleted)
        {
            m_TemplatesTree.Clear();

            var allTemplates = new Dictionary<string, GraphViewTemplateDescriptor>();
            // Note: the viewModel.results list takes care of removing duplicates.
            foreach (var item in m_ViewModel.results)
            {
                var guid = ((AssetProvider.AssetMetaInfo)item.data).guid;
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (allTemplates.ContainsKey(assetPath))
                    continue;

                if (m_CurrentMode == CreateMode.Insert && guid == m_TemplateHelper.emptyTemplateGuid)
                    continue;

                if (m_TemplateHelper.TryGetTemplate(assetPath, out var template))
                {
                    var isBuiltIn = !string.IsNullOrEmpty(m_TemplateHelper.builtInTemplatePath) && assetPath.StartsWith(m_TemplateHelper.builtInTemplatePath);
                    template.category = isBuiltIn ? m_TemplateHelper.builtInCategory : template.category;
                    template.internalOrder =  isBuiltIn ? 0 : 1;
                    template.assetGuid = guid;
                    if (string.IsNullOrEmpty(template.name.Trim()))
                    {
                        template.name = Path.GetFileNameWithoutExtension(assetPath);
                    }

                    var skinIcon = GetSkinIcon(template.icon);
                    template.icon = skinIcon == null ? template.icon : skinIcon;
                    allTemplates[assetPath] = template;
                }
            }

            var templatesGroupedByCategory = new Dictionary<string, List<GraphViewTemplateDescriptor>>();
            foreach (var template in allTemplates.Values)
            {
                if (templatesGroupedByCategory.TryGetValue(template.category, out var list))
                {
                    list.Add(template);
                }
                else
                {
                    list = new List<GraphViewTemplateDescriptor> { template };
                    templatesGroupedByCategory[template.category] = list;
                }
            }

            // This is to prevent collapse/expand if there's only one category
            if (templatesGroupedByCategory.Count == 1)
            {
                m_ListOfTemplates.AddToClassList("remove-toggle");
            }
            else
            {
                m_ListOfTemplates.RemoveFromClassList("remove-toggle");
            }

            var templates = new List<List<GraphViewTemplateDescriptor>>(templatesGroupedByCategory.Values);
            templates.Sort(new TemplateCategorySorter());

            var id = 0;
            var lastSelectedTemplateFound = false;
            var fallBackTemplateAssetGuid = string.Empty;
            var indexToSelect = 2;
            foreach (var group in templates)
            {
                var groupId = id++;
                var children = new List<TreeViewItemData<ITemplateDescriptor>>(group.Count);
                group.Sort(this.m_TemplateSorter);
                foreach (var child in group)
                {
                    // Check id == 2 because it corresponds to the first item in the list
                    if (id == 2)
                        fallBackTemplateAssetGuid = child.assetGuid;
                    if (child.assetGuid == m_templateWindowPrefs.LastUsedTemplateGuid)
                    {
                        lastSelectedTemplateFound = true;
                        indexToSelect = id;
                        // Force the category containing the last used template to be expanded
                        m_templateWindowPrefs.SetCategoryCollapsedState(m_TemplateHelper.toolKey, group[0].category, false);
                    }
                    children.Add(new TreeViewItemData<ITemplateDescriptor>(id++, child));
                }
                var section = new TreeViewItemData<ITemplateDescriptor>(groupId, new TemplateSection(group[0].category), children);
                m_TemplatesTree.Add(section);
            }

            m_ListOfTemplates.SetRootItems(m_TemplatesTree);
            if (isSearchCompleted)
            {
                if (!lastSelectedTemplateFound)
                {
                    m_templateWindowPrefs.LastUsedTemplateGuid = fallBackTemplateAssetGuid;
                }
                m_ListOfTemplates.RefreshItems();

                if (allTemplates.Count > 0)
                {
                    rootVisualElement.RemoveFromClassList("no-result");
                }
                else
                {
                    rootVisualElement.AddToClassList("no-result");
                }

                m_ListOfTemplates.ExpandAll();
                // Only collapse categories when there are more than one
                if (templatesGroupedByCategory.Count > 1)
                {
                    SynchronizeExpandState();
                }

                // Let the layout pass complete before trying to scroll to freshly filled treeview
                EditorApplication.delayCall += () => m_ListOfTemplates.ScrollToItem(indexToSelect);
            }
        }

        private void SynchronizeExpandState()
        {
            bool needRefresh = false;
            foreach(var id in m_ListOfTemplates.viewController.GetAllItemIds())
            {
                var item = m_ListOfTemplates.GetItemDataForId<ITemplateDescriptor>(id);
                if (item is TemplateSection section)
                {
                    if (m_templateWindowPrefs.GetCategoryCollapsedState(m_TemplateHelper.toolKey, section.header))
                    {
                        m_ListOfTemplates.viewController.CollapseItem(id, false);
                        needRefresh = true;
                    }
                }
            }

            if (needRefresh)
            {
                m_ListOfTemplates.RefreshItems();
            }
        }

        public void OnQueryChanged(TemplateSearchViewModel viewModel, string searchText)
        {
            SetQuery(searchText);
        }

        public void SetQuery(string query)
        {
            m_SearchField.searchTextInput.SetValueWithoutNotify(query);
            m_ViewModel.context.searchText = query;
            Refresh();
        }

        public void Refresh()
        {
            m_ViewModel.RefreshItems(CollectTemplates, () => CollectTemplates(true));
        }

        private Texture2D GetSkinIcon(Texture2D templateIcon)
        {
            if (EditorGUIUtility.skinIndex == 0)
            {
                return templateIcon;
            }

            var path = AssetDatabase.GetAssetPath(templateIcon);
            return EditorGUIUtility.LoadIcon(path);
        }

        private void ToggleFavorite(VisualElement item, bool isFavorite)
        {
            if (isFavorite)
            {
                item.AddToClassList(k_FavoriteUssClass);
            }
            else
            {
                item.RemoveFromClassList(k_FavoriteUssClass);
            }
        }

        internal static string GetGlobalId(GraphViewTemplateDescriptor descriptor)
        {
            var assetObject = AssetDatabase.LoadMainAssetAtGUID(new GUID(descriptor.assetGuid));
            return GlobalObjectId.GetGlobalObjectIdSlow(assetObject).ToString();
        }

        internal static bool IsFavorite(GraphViewTemplateDescriptor descriptor)
        {
            return SearchSettings.searchItemFavorites.Contains(GetGlobalId(descriptor));
        }

        internal static bool IsFavorite(string globalId)
        {
            return SearchSettings.searchItemFavorites.Contains(globalId);
        }
    }
}
