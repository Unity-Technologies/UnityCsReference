using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Toolbar = UnityEditor.UIElements.Toolbar;
using BuilderLibraryItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;
using TreeViewItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;

namespace Unity.UI.Builder
{
    static class BuilderLibraryContent
    {
        class AssetModificationProcessor : IBuilderAssetModificationProcessor
        {
            readonly Action m_OnAssetChange;

            public AssetModificationProcessor(Action onAssetChange)
            {
                m_OnAssetChange = onAssetChange;
            }

            public void OnAssetChange()
            {
                // AssetDatabase.FindAllAssets(filter) will return outdated assets if
                // we refresh immediately.

                // Note: This used to rely on Builder.ActiveWindow.rootVisualElement.schedule
                // when not in batch mode to do the delay call but this caused problems with
                // tests that waited on the LibraryContent to refresh based on asset changes
                // before the any UI Builder window was open.
                EditorApplication.delayCall += m_OnAssetChange.Invoke;
            }

            public AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
                => AssetMoveResult.DidNotMove;

            public AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
                => AssetDeleteResult.DidNotDelete;
        }

        internal class AssetPostprocessor : IBuilderOneTimeAssetPostprocessor
        {
            readonly Action m_OnPostprocessAllAssets;

            public AssetPostprocessor(Action onPostprocessAllAssets)
            {
                m_OnPostprocessAllAssets = onPostprocessAllAssets;
            }

            public void OnPostProcessAsset()
            {
                // AssetDatabase.FindAllAssets(filter) will return outdated assets if
                // we refresh immediately.

                // Note: This used to rely on Builder.ActiveWindow.rootVisualElement.schedule
                // when not in batch mode to do the delay call but this caused problems with
                // tests that waited on the LibraryContent to refresh based on asset changes
                // before the any UI Builder window was open.
                EditorApplication.delayCall += m_OnPostprocessAllAssets.Invoke;
            }
        }

        static readonly Dictionary<Type, BuilderLibraryTreeItem> s_ControlsTypeCache = new Dictionary<Type, BuilderLibraryTreeItem>();
        static readonly BuilderLibraryProjectScanner s_ProjectAssetsScanner = new BuilderLibraryProjectScanner();
        static int s_ProjectUxmlPathsHash;

        public static event Action OnLibraryContentUpdated;
        public static List<TreeViewItem> standardControlsTree { get; private set; }
        public static List<TreeViewItem> standardControlsTreeNoEditor { get; private set; }
        public static List<TreeViewItem> projectContentTree { get; private set; }
        public static List<TreeViewItem> projectContentTreeNoPackages { get; private set; }

        static readonly int k_DefaultVisualElementFlexGrow = 1;
        static readonly Color k_DefaultVisualElementBackgroundColor = new (0, 0, 0, 0);

        static BuilderLibraryContent()
        {
            RegenerateLibraryContent();
            BuilderAssetModificationProcessor.Register(new AssetModificationProcessor(() =>
            {
                if (s_ProjectUxmlPathsHash != s_ProjectAssetsScanner.GetAllProjectUxmlFilePathsHash())
                    RegenerateLibraryContent();
            }));

            BuilderAssetPostprocessor.Register(new AssetPostprocessor(() =>
            {
                RegenerateLibraryContent();
            }));
        }

        public static void RegenerateLibraryContent()
        {
            standardControlsTree = GenerateControlsItemsTree();
            standardControlsTreeNoEditor = new List<TreeViewItem>();
            foreach (var item in standardControlsTree)
            {
                var builderLibraryTreeItem = item.data;
                if (builderLibraryTreeItem.isEditorOnly)
                    continue;

                standardControlsTreeNoEditor.Add(item);
                RemoveEditorOnlyControls(item);
            }

            GenerateProjectContentTrees();
            UpdateControlsTypeCache(projectContentTree);
            UpdateControlsTypeCache(standardControlsTree);
            s_ProjectUxmlPathsHash = s_ProjectAssetsScanner.GetAllProjectUxmlFilePathsHash();

            OnLibraryContentUpdated?.Invoke();
        }

        static void RemoveEditorOnlyControls(TreeViewItem item)
        {
            var children = new List<TreeViewItem>(item.children);
            (item.children as IList)?.Clear();
            foreach (var child in children)
            {
                var builderLibraryTreeItem = child.data;
                if (!builderLibraryTreeItem.isEditorOnly)
                {
                    item.AddChild(child);
                    if (child.hasChildren)
                        RemoveEditorOnlyControls(child);
                }
            }
        }

        internal static void ResetProjectUxmlPathsHash()
        {
            s_ProjectUxmlPathsHash = default;
        }

        public static Texture2D GetTypeLibraryIcon(Type type)
        {
            if (s_ControlsTypeCache.TryGetValue(type, out var builderLibraryTreeItem))
                return builderLibraryTreeItem.icon;

            // Just in case to avoid infinity loop.
            if (type != typeof(VisualElement))
                return GetTypeLibraryIcon(typeof(VisualElement));

            return null;
        }

        internal static BuilderLibraryTreeItem GetLibraryItemForType(Type type)
        {
            return s_ControlsTypeCache.TryGetValue(type, out var builderLibraryTreeItem)
                ? builderLibraryTreeItem
                : null;
        }

        public static Texture2D GetTypeDarkSkinLibraryIcon(Type type)
        {
            if (s_ControlsTypeCache.TryGetValue(type, out var builderLibraryTreeItem))
                return builderLibraryTreeItem.darkSkinIcon;

            return null;
        }

        public static Texture2D GetUXMLAssetIcon(string uxmlAssetPath)
        {
            return GetUXMLAssetIcon(projectContentTree, uxmlAssetPath);
        }

        static Texture2D GetUXMLAssetIcon(IEnumerable<TreeViewItem> items, string uxmlAssetPath)
        {
            foreach (var item in items)
            {
                var builderLibraryTreeItem = item.data;
                if (!string.IsNullOrEmpty(builderLibraryTreeItem.sourceAssetPath) && builderLibraryTreeItem.sourceAssetPath.Equals(uxmlAssetPath))
                {
                    return builderLibraryTreeItem.icon;
                }

                if (item.hasChildren)
                {
                    var icon = GetUXMLAssetIcon(item.children, uxmlAssetPath);
                    if (icon != null)
                        return icon;
                }
            }

            return (Texture2D)EditorGUIUtility.IconContent("VisualTreeAsset Icon").image;
        }

        static void GenerateProjectContentTrees()
        {
            projectContentTree = new List<TreeViewItem>();
            projectContentTreeNoPackages = new List<TreeViewItem>();

            var fromProjectCategory = CreateItem(BuilderConstants.LibraryAssetsSectionHeaderName, null, null, null, isHeader: true);
            s_ProjectAssetsScanner.ImportUxmlFromProject(fromProjectCategory, true);
            projectContentTree.Add(fromProjectCategory);

            var fromProjectCategoryNoPackages = CreateItem(BuilderConstants.LibraryAssetsSectionHeaderName, null, null, null, isHeader: true);
            s_ProjectAssetsScanner.ImportUxmlFromProject(fromProjectCategoryNoPackages, false);
            projectContentTreeNoPackages.Add(fromProjectCategoryNoPackages);

            var customControlsCategory = CreateItem(BuilderConstants.LibraryCustomControlsSectionHeaderName, null, null, null, isHeader: true);
            s_ProjectAssetsScanner.ImportFactoriesFromSource(customControlsCategory);
            if (customControlsCategory.hasChildren)
            {
                projectContentTree.Add(customControlsCategory);
                projectContentTreeNoPackages.Add(customControlsCategory);
            }
        }

        internal static TreeViewItemData<BuilderLibraryTreeItem> CreateItem(string name, string iconName, Type type, Func<VisualElement> makeVisualElementCallback,
            Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeElementAssetCallback = null, List<TreeViewItemData<BuilderLibraryTreeItem>> children = null, VisualTreeAsset asset = null,
            int id = default, bool isHeader = false, bool isEditorOnly = false)
        {
            var itemId = BuilderLibraryTreeItem.GetItemId(name, type, asset, id);
            var data = new BuilderLibraryTreeItem(name, iconName, type, makeVisualElementCallback, makeElementAssetCallback, asset) { isHeader = isHeader, isEditorOnly = isEditorOnly };
            return new TreeViewItemData<BuilderLibraryTreeItem>(itemId, data, children);
        }

        static List<TreeViewItem> GenerateControlsItemsTree()
        {
            var containersItem = CreateItem(BuilderConstants.LibraryContainersSectionHeaderName, null, null, null, id: 1, isHeader:true);
            var controlsTree = new List<TreeViewItem>();
            IList<TreeViewItem> containersItemList = new List<TreeViewItem>
            {
                CreateItem("VisualElement", "VisualElement", typeof(VisualElement),
                    () => new VisualElement(),
                    (inVta, inParent, ve) =>
                    {
                        var vea = new VisualElementAsset(typeof(VisualElement).ToString());
                        VisualTreeAssetUtilities.InitializeElement(vea);

                        BuilderStyleUtilities.SetInlineStyleValue(inVta, vea, ve, "flex-grow",
                            k_DefaultVisualElementFlexGrow);
                        BuilderStyleUtilities.SetInlineStyleValue(inVta, vea, ve, "background-color",
                            k_DefaultVisualElementBackgroundColor);
                        
                        inVta.AddElement(inParent, vea);
                        return vea;
                    }),
                CreateItem("ScrollView", "ScrollView", typeof(ScrollView), () => new ScrollView()),
                CreateItem("ListView", "ListView", typeof(ListView), () => new ListView()),
                CreateItem("TreeView", "TreeView", typeof(TreeView), () => new TreeView()),
                CreateItem("GroupBox", "VisualElement", typeof(GroupBox), () => new GroupBox()),
            };
            containersItem.AddChildren(containersItemList);
            controlsTree.Add(containersItem);

            var editorContainersItemList = CreateItem(BuilderConstants.LibraryEditorContainersSectionHeaderName, null, null, null, null, new List<TreeViewItemData<BuilderLibraryTreeItem>>
            {
                CreateItem("IMGUI Container", "VisualElement", typeof(IMGUIContainer), () => new IMGUIContainer()),
            }, id: 2, isEditorOnly: true, isHeader: true);

            var controlsItem = CreateItem(BuilderConstants.LibraryControlsSectionHeaderName, null, null, null, null, new List<TreeViewItemData<BuilderLibraryTreeItem>>
            {
                CreateItem("Label", nameof(Label), typeof(Label), () => new Label("Label")),
                CreateItem("Button", nameof(Button), typeof(Button), () => new Button { text = "Button" }),
                CreateItem("Toggle", nameof(Toggle), typeof(Toggle), () => new Toggle("Toggle")),
                CreateItem("Scroller", nameof(Scroller), typeof(Scroller), () => new Scroller(0, 100, (v) => {}, SliderDirection.Horizontal) { value = 42 }),
                CreateItem("Text Field", nameof(TextField), typeof(TextField), () => new TextField("Text Field") { value = "filler text" }),
                CreateItem("Foldout", nameof(Foldout), typeof(Foldout), () => new Foldout { text = "Foldout" }),
                CreateItem("Slider", nameof(Slider), typeof(Slider), () => new Slider("Slider", 0, 100) { value = 42 }),
                CreateItem("Slider (Int)", nameof(SliderInt), typeof(SliderInt), () => new SliderInt("SliderInt", 0, 100) { value = 42 }),
                CreateItem("Min-Max Slider", nameof(MinMaxSlider), typeof(MinMaxSlider), () => new MinMaxSlider("Min/Max Slider", 0, 20, -10, 40) { value = new Vector2(10, 12) }),
                CreateItem("Progress Bar", nameof(ProgressBar), typeof(ProgressBar), () => new ProgressBar() { title = "my-progress", value = 22 }),
                CreateItem("Dropdown", "Dropdown", typeof(DropdownField), () => new DropdownField("Dropdown")),
                CreateItem("Enum", nameof(EnumField), typeof(EnumField), () => new EnumField("Enum", TextAlignment.Center)),
                CreateItem("Radio Button", nameof(RadioButton), typeof(RadioButton), () => new RadioButton("Radio Button")),
                CreateItem("Radio Button Group", nameof(RadioButtonGroup), typeof(RadioButtonGroup), () => new RadioButtonGroup("Radio Button Group")),
            }, isHeader: true);

            var numericFields = CreateItem("Numeric Fields", null, null, null, null, new List<TreeViewItemData<BuilderLibraryTreeItem>>
            {
                CreateItem("Integer", nameof(IntegerField), typeof(IntegerField), () => new IntegerField("Int Field") { value = 42 }),
                CreateItem("Float", nameof(FloatField), typeof(FloatField), () => new FloatField("Float Field") { value = 42.2f }),
                CreateItem("Long", nameof(LongField), typeof(LongField), () => new LongField("Long Field") { value = 42 }),
                CreateItem("Double", nameof(DoubleField), typeof(DoubleField), () => new DoubleField("Double Field") { value = 42.2 }),
                CreateItem("Hash128", nameof(Hash128Field), typeof(Hash128Field), () => new Hash128Field("Hash128 Field") { value = Hash128.Compute("42") }),
                CreateItem("Vector2", nameof(Vector2Field), typeof(Vector2Field), () => new Vector2Field("Vec2 Field")),
                CreateItem("Vector3", nameof(Vector3Field), typeof(Vector3Field), () => new Vector3Field("Vec3 Field")),
                CreateItem("Vector4", nameof(Vector4Field), typeof(Vector4Field), () => new Vector4Field("Vec4 Field")),
                CreateItem("Rect", nameof(RectField), typeof(RectField), () => new RectField("Rect")),
                CreateItem("Bounds", nameof(BoundsField), typeof(BoundsField), () => new BoundsField("Bounds")),
                CreateItem("Vector2 (Int)", nameof(Vector2IntField), typeof(Vector2IntField), () => new Vector2IntField("Vector2Int")),
                CreateItem("Vector3 (Int)", nameof(Vector3IntField), typeof(Vector3IntField), () => new Vector3IntField("Vector3Int")),
                CreateItem("Rect (Int)", nameof(RectIntField), typeof(RectIntField), () => new RectIntField("RectInt")),
                CreateItem("Bounds (Int)", nameof(BoundsIntField), typeof(BoundsIntField), () => new BoundsIntField("BoundsInt")),
            }, isHeader: true);

            var valueFields = CreateItem("Value Fields", null, null, null, null, new List<TreeViewItemData<BuilderLibraryTreeItem>>
            {
                CreateItem("Color", nameof(ColorField), typeof(ColorField), () => new ColorField("Color") { value = Color.cyan }),
                CreateItem("Curve", nameof(CurveField), typeof(CurveField), () => new CurveField("Curve")
                {
                    value = new AnimationCurve(new Keyframe(0, 0), new Keyframe(5, 8), new Keyframe(10, 4))
                }),
                CreateItem("Gradient", nameof(GradientField), typeof(GradientField), () => new GradientField("Gradient")
                {
                    value = new Gradient()
                    {
                        colorKeys = new[]
                        {
                            new GradientColorKey(Color.red, 0),
                            new GradientColorKey(Color.blue, 10),
                            new GradientColorKey(Color.green, 20)
                        }
                    }
                })
            }, isEditorOnly: true, isHeader: true);

            var choiceFields = CreateItem("Choice Fields", null, null, null, null, new List<TreeViewItemData<BuilderLibraryTreeItem>>
            {
                // No UXML support for PopupField.
                //new LibraryTreeItem("Popup", () => new PopupField<string>("Normal Field", choices, 0)),

                CreateItem("Tag", nameof(TagField), typeof(TagField), () => new TagField("Tag", "Player")),
                CreateItem("Mask", nameof(MaskField), typeof(MaskField), () => new MaskField("Mask")),
                CreateItem("Layer", nameof(LayerField), typeof(LayerField), () => new LayerField("Layer")),
                CreateItem("LayerMask", nameof(LayerMaskField), typeof(LayerMaskField), () => new LayerMaskField("LayerMask")),
                CreateItem("EnumFlags", nameof(EnumFlagsField), typeof(EnumFlagsField), () => new EnumFlagsField("EnumFlags", UsageHints.DynamicTransform))
            }, isEditorOnly: true, isHeader: true);

            var toolbar = CreateItem("Toolbar", null, null, null, null, new List<TreeViewItemData<BuilderLibraryTreeItem>>
            {
                CreateItem("Toolbar", "ToolbarElement", typeof(Toolbar), () => new Toolbar()),
                CreateItem("Toolbar Menu", "ToolbarElement", typeof(ToolbarMenu), () => new ToolbarMenu()),
                CreateItem("Toolbar Button", "ToolbarElement", typeof(ToolbarButton), () => new ToolbarButton { text = "Button" }),
                CreateItem("Toolbar Spacer", "ToolbarElement", typeof(ToolbarSpacer), () => new ToolbarSpacer()),
                CreateItem("Toolbar Toggle", "ToolbarElement", typeof(ToolbarToggle), () => new ToolbarToggle { label = "Toggle" }),
                CreateItem("Toolbar Breadcrumbs", "ToolbarElement", typeof(ToolbarBreadcrumbs), () => new ToolbarBreadcrumbs()),
                CreateItem("Toolbar Search Field", "ToolbarElement", typeof(ToolbarSearchField), () => new ToolbarSearchField()),
                CreateItem("Toolbar Popup Search Field", "ToolbarElement", typeof(ToolbarPopupSearchField), () => new ToolbarPopupSearchField()),
            }, isEditorOnly: true, isHeader: true);

            var inspectors = CreateItem("Inspectors", null, null, null, null, new List<TreeViewItemData<BuilderLibraryTreeItem>>
            {
                CreateItem("Object Field", nameof(ObjectField), typeof(ObjectField), () => new ObjectField("Object Field") { value = new Texture2D(10, 10) { name = "new_texture" } }),
                CreateItem("PropertyField", nameof(PropertyField), typeof(PropertyField), () => new PropertyField())
            }, isEditorOnly: true, isHeader: true);

            controlsTree.Add(editorContainersItemList);
            controlsTree.Add(controlsItem);
            controlsTree.Add(numericFields);
            controlsTree.Add(valueFields);
            controlsTree.Add(choiceFields);
            controlsTree.Add(toolbar);
            controlsTree.Add(inspectors);

            return controlsTree;
        }

        static void UpdateControlsTypeCache(IEnumerable<TreeViewItem> items)
        {
            foreach (var item in items)
            {
                var builderLibraryTreeItem = item.data;
                if (builderLibraryTreeItem.type != null)
                    s_ControlsTypeCache[builderLibraryTreeItem.type] = builderLibraryTreeItem;

                if (item.hasChildren)
                    UpdateControlsTypeCache(item.children);
            }
        }
    }
}
