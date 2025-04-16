// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;

using Unity.UI.Builder;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    internal interface ITemplateHelper
    {
        string packageInfoName { get; }
        string learningSampleName { get; }
        string templateWindowDocUrl { get; }
        string builtInTemplatePath { get; }
        string builtInCategory { get; }
        string assetType { get; }
        string emptyTemplateName { get; }
        string emptyTemplateDescription { get; }
        string lastSelectedGuidKey { get; }
        string createNewAssetTitle { get; }
        string insertTemplateTitle { get; }
        string emptyTemplateIconPath { get; }
        string emptyTemplateScreenshotPath { get; }
        string customTemplateIcon { get; }
        GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper { get; set; }

        void RaiseTemplateUsed(GraphViewTemplateDescriptor usedTemplate);
        bool TryGetTemplate(string assetPath, out GraphViewTemplateDescriptor graphViewTemplate);
        bool TrySetTemplate(string assetPath, GraphViewTemplateDescriptor graphViewTemplate);
    }

    internal interface ITemplateDescriptor
    {
        string header { get; }
    }

    internal class GraphViewTemplateWindow : EditorWindow
    {
        internal interface ISaveFileDialogHelper
        {
            string OpenSaveFileDialog();
        }

        private class TemplateSection : ITemplateDescriptor
        {
            public TemplateSection(string text)
            {
                header = text;
            }
            public string header { get; }
        }

        private const float PackageManagerTimeout = 5f; // 5s

        private readonly List<TreeViewItemData<ITemplateDescriptor>> m_TemplatesTree = new ();

        private TreeView m_ListOfTemplates;
        private Texture2D m_CustomTemplateIcon;
        private Image m_DetailsScreenshot;
        private Label m_DetailsTitle;
        private Label m_DetailsDescription;
        private VisualTreeAsset m_ItemTemplate;
        private Action<string> m_AssetCreationCallback;
        private string m_LastSelectedTemplatePath;
        private int m_LastSelectedIndex;
        private CreateMode m_CurrentMode;
        private Action<string, string> m_UserCallback;
        private string m_LastSelectedTemplateGuid;
        private GraphViewTemplateDescriptor m_SelectedTemplate;
        private Button m_InstallButton;
        private ITemplateHelper m_TemplateHelper;

        private enum CreateMode
        {
            CreateNew,
            Insert,
            None,
        }

        public static void ShowCreateFromTemplate(ITemplateHelper templateHelper, Action<string, string> callback, bool showSaveDialog = true) => ShowInternal(showSaveDialog ? CreateMode.CreateNew : CreateMode.None, templateHelper, callback);
        public static void ShowInsertTemplate(ITemplateHelper templateHelper, Action<string, string> callback) => ShowInternal(CreateMode.Insert, templateHelper, callback);

        private static void ShowInternal(CreateMode mode, ITemplateHelper templateHelper, Action<string, string> callback)
        {
            var windowTitle = mode == CreateMode.Insert ? templateHelper.insertTemplateTitle : templateHelper.createNewAssetTitle;
            var templateWindow = EditorWindow.GetWindow<GraphViewTemplateWindow>(true, windowTitle, false);
            templateWindow.Setup(mode, templateHelper, callback);
        }

        private void Setup(CreateMode mode, ITemplateHelper templateHelper, Action<string, string> callback)
        {
            minSize = new Vector2(800, 300);
            m_UserCallback = callback;
            m_CurrentMode = mode;
            m_TemplateHelper = templateHelper;
            SetCallBack();
            LoadTemplates();

            // Handle the install button here because we need the template helper
            m_InstallButton = rootVisualElement.Q<Button>("InstallButton");
            if (!string.IsNullOrEmpty(m_TemplateHelper.learningSampleName))
            {
                m_InstallButton.clicked += OnInstall;
                m_InstallButton.enabledSelf = TryFindSample(m_TemplateHelper.learningSampleName, out var sample) && !sample.isImported;
            }
            else
            {
                m_InstallButton.style.display = DisplayStyle.None;
            }

            m_CustomTemplateIcon = EditorGUIUtility.LoadIcon(m_TemplateHelper.customTemplateIcon);
        }

        private void CreateGUI()
        {
            m_ItemTemplate = (VisualTreeAsset)EditorGUIUtility.Load("UXML/GraphView/TemplateItem.uxml");
            var tpl = (VisualTreeAsset)EditorGUIUtility.Load("UXML/GraphView/TemplateWindow.uxml");
            tpl.CloneTree(rootVisualElement);
            rootVisualElement.AddStyleSheetPath("StyleSheets/GraphView/TemplateWindow.uss");

            rootVisualElement.name = "VFXTemplateWindowRoot";
            rootVisualElement.Q<Button>("CreateButton").clicked += OnCreate;
            rootVisualElement.Q<Button>("CancelButton").clicked += OnCancel;

            m_DetailsScreenshot = rootVisualElement.Q<Image>("Screenshot");
            m_DetailsScreenshot.scaleMode = ScaleMode.ScaleAndCrop;
            m_DetailsTitle = rootVisualElement.Q<Label>("Title");
            m_DetailsDescription = rootVisualElement.Q<Label>("Description");

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

        private void LoadTemplates()
        {
            m_LastSelectedTemplateGuid = EditorPrefs.GetString(m_TemplateHelper.lastSelectedGuidKey);
            CollectTemplates();
            m_ListOfTemplates.ExpandAll();
        }

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
            EditorPrefs.SetString(m_TemplateHelper.lastSelectedGuidKey, m_LastSelectedTemplateGuid);
        }

        private void OnCancel()
        {
            m_LastSelectedTemplatePath = null;
            m_AssetCreationCallback?.Invoke(m_LastSelectedTemplatePath);
            Close();
        }

        private void OnInstall()
        {
            if (TryFindSample(m_TemplateHelper.learningSampleName, out var samplePackage))
            {
                m_InstallButton.enabledSelf = !samplePackage.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
            }
        }

        private void OnCreate()
        {
            var template = m_ListOfTemplates.selectedIndex != -1 ? (GraphViewTemplateDescriptor)m_ListOfTemplates.selectedItem : m_SelectedTemplate;
            m_LastSelectedTemplatePath = AssetDatabase.GUIDToAssetPath(template.assetGuid);
            m_AssetCreationCallback?.Invoke(m_LastSelectedTemplatePath);
            Close();
            m_TemplateHelper.RaiseTemplateUsed(template);
            m_AssetCreationCallback = null;
        }

        private bool TryFindSample(string sampleName, out Sample sample)
        {
            try
            {
                var startTime = Time.time;
                var searchRequest = Client.Search(m_TemplateHelper.packageInfoName, true);
                while (!searchRequest.IsCompleted && Time.time - startTime < PackageManagerTimeout)
                {
                    Thread.Sleep(20);
                }

                if (searchRequest is { Result: { Length: 1 }, IsCompleted: true } && searchRequest.Result[0] is { } packageInfo)
                {
                    // Workaround for UUM-63664
                    foreach (var extension in PackageManagerExtensions.Extensions)
                    {
                        try
                        {
                            extension.OnPackageSelectionChange(packageInfo);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"An error occured while trying to select a package extension.\n{e.Message}");
                        }
                    }

                    foreach (var samplePackage in Sample.FindByPackage(m_TemplateHelper.packageInfoName, null))
                    {
                        if (string.Compare(samplePackage.displayName, sampleName, StringComparison.OrdinalIgnoreCase) ==
                            0)
                        {
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
                    m_DetailsTitle.text = template.name;
                    m_DetailsDescription.text = template.description;
                    m_LastSelectedTemplateGuid = template.assetGuid;
                    m_LastSelectedIndex = m_ListOfTemplates.selectedIndex;
                    // Maybe set a placeholder screenshot when null
                    m_DetailsScreenshot.image = template.thumbnail;
                }

                // We expect only one item to be selected
                return;
            }

            // Reach here when the selection is empty
            m_ListOfTemplates.selectedIndex = m_LastSelectedIndex;
        }

        private void BindTemplateItem(VisualElement item, int index)
        {
            var data = m_ListOfTemplates.GetItemDataForIndex<ITemplateDescriptor>(index);
            var label = item.Q<Label>("TemplateName");
            label.text = data.header;

            string ussClass;
            if (data is GraphViewTemplateDescriptor template)
            {
                item.Q<Image>("TemplateIcon").image = template.icon != null ? template.icon : m_CustomTemplateIcon;
                if (template.assetGuid == m_LastSelectedTemplateGuid)
                    m_ListOfTemplates.SetSelection(index);
                ussClass = "vfxtemplate-item";

                item.RegisterCallback<ClickEvent>(OnClickItem);
            }
            else
            {
                // This is a hack to put the expand/collapse button above the item so that we can interact with it
                var toggle = item.parent.parent.Q<Toggle>();
                toggle.BringToFront();
                ussClass = "vfxtemplate-section";
            }

            if (item.GetFirstAncestorWithClass("unity-tree-view__item") is { } parent)
            {
                parent.AddToClassList(ussClass);
            }
        }

        private void UnbindTemplateItem(VisualElement item, int index)
        {
            if (item.GetFirstAncestorWithClass("unity-tree-view__item") is { } parent)
            {
                parent.RemoveFromClassList("vfxtemplate-item");
                parent.RemoveFromClassList("vfxtemplate-section");
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

        private void CollectTemplates()
        {
            m_TemplatesTree.Clear();

            var assetsGuid = AssetDatabase.FindAssets($"t:{m_TemplateHelper.assetType}");
            var allTemplates = new List<GraphViewTemplateDescriptor>(assetsGuid.Length);

            foreach (var guid in assetsGuid)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (m_TemplateHelper.TryGetTemplate(assetPath, out var template))
                {
                    var isBuiltIn = assetPath.StartsWith(m_TemplateHelper.builtInTemplatePath);
                    template.category = isBuiltIn ? m_TemplateHelper.builtInCategory : template.category;
                    template.order =  isBuiltIn ? 0 : 1;
                    template.assetGuid = guid;
                    if (isBuiltIn)
                    {
                        template.icon = GetSkinIcon(template.icon);
                    }
                    allTemplates.Add(template);
                }
            }

            if (m_CurrentMode != CreateMode.Insert)
            {
                allTemplates.Add(MakeEmptyTemplate());
            }

            var templatesGroupedByCategory = new Dictionary<string, List<GraphViewTemplateDescriptor>>();
            foreach (var template in allTemplates)
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
            templates.Sort((listA, listB) => listA[0].order.CompareTo(listB[0].order));

            var id = 0;
            var lastSelectedTemplateFound = false;
            var fallBackTemplateAssetGuid = string.Empty;
            foreach (var group in templates)
            {
                var groupId = id++;
                var children = new List<TreeViewItemData<ITemplateDescriptor>>(group.Count);
                foreach (var child in group)
                {
                    if (id == 2)
                        fallBackTemplateAssetGuid = child.assetGuid;
                    if (child.assetGuid == m_LastSelectedTemplateGuid)
                        lastSelectedTemplateFound = true;
                    children.Add(new TreeViewItemData<ITemplateDescriptor>(id++, child));
                }
                var section = new TreeViewItemData<ITemplateDescriptor>(groupId, new TemplateSection(group[0].category), children);
                m_TemplatesTree.Add(section);
            }
            m_ListOfTemplates.SetRootItems(m_TemplatesTree);
            if (!lastSelectedTemplateFound)
            {
                m_LastSelectedTemplateGuid = fallBackTemplateAssetGuid;
            }
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

        private GraphViewTemplateDescriptor MakeEmptyTemplate()
        {
            return new GraphViewTemplateDescriptor
            {
                name = m_TemplateHelper.emptyTemplateName,
                icon = EditorGUIUtility.LoadIcon(m_TemplateHelper.emptyTemplateIconPath),
                thumbnail = EditorGUIUtility.LoadIcon(m_TemplateHelper.emptyTemplateScreenshotPath),
                category = m_TemplateHelper.builtInCategory,
                description = m_TemplateHelper.emptyTemplateDescription,
                assetGuid = "empty",
            };
        }
    }
}
