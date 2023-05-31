// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements.Debugger;

namespace UnityEditor.UIElements.Samples
{
    internal class UIElementsSamples : EditorWindow
    {
        private static readonly string s_StyleSheetPath = "UIPackageResources/StyleSheets/UIElementsSamples/UIElementsSamples.uss";
        private static readonly string s_DarkStyleSheetPath = "UIPackageResources/StyleSheets/UIElementsSamples/UIElementsSamplesDark.uss";
        private static readonly string s_LightStyleSheetPath = "UIPackageResources/StyleSheets/UIElementsSamples/UIElementsSamplesLight.uss";

        private readonly string k_SplitterClassName = "unity-samples-explorer";
        private readonly int k_SplitterLeftPaneStartingWidth = 200;

        private readonly string k_TreeViewName = "tree-view";
        private readonly string k_TreeViewClassName = "unity-samples-explorer__tree-view";
        private readonly string k_ContentPanelName = "content-container";
        private readonly string k_ContentPanelClassName = "unity-samples-explorer__content-container";

        private readonly string k_TreeItemClassName = "unity-samples-explorer__tree-item";
        private readonly string k_TreeItemLabelClassName = "unity-samples-explorer__tree-item-label";

        private readonly int k_TreeViewSelectionRestoreDelay = 400;
        private readonly int k_TreeViewInitialSelectionDelay = 500;

        private readonly string k_CategoryPanelClassName = "unity-samples-explorer__category_panel";
        private readonly string k_CategoryTitleClassName = "unity-samples-explorer__category_title";

        private VisualElement m_ContentPanel;

        public const string k_WindowPath = "Window/UI Toolkit/Samples";
        public static readonly string OpenWindowCommand = nameof(OpenUIElementsSamplesCommand);

        [MenuItem(k_WindowPath, false, 3010, false, secondaryPriority = 1)]
        private static void OpenUIElementsSamplesCommand()
        {
            if (CommandService.Exists(OpenWindowCommand))
                CommandService.Execute(OpenWindowCommand, CommandHint.Menu);
            else
            {
                OpenUIElementsSamples();
            }
        }

        public static void OpenUIElementsSamples()
        {
            var wnd = GetWindow<UIElementsSamples>();
            wnd.minSize = new Vector2(640, 100);
            wnd.titleContent = new GUIContent("UI Toolkit Samples");
        }

        internal readonly struct SampleTreeItem
        {
            public string name { get; }
            public Func<SampleTreeItem, VisualElement> makeItem { get; }

            public SampleTreeItem(string name, Func<SampleTreeItem, VisualElement> makeItem)
            {
                this.name = name;
                this.makeItem = makeItem;
            }
        }

        public void OnEnable()
        {
            var root = rootVisualElement;

            var styleSheet = EditorGUIUtility.Load(s_StyleSheetPath) as StyleSheet;
            root.styleSheets.Add(styleSheet);

            var themedStyleSheet = EditorGUIUtility.isProSkin
                ? EditorGUIUtility.Load(s_DarkStyleSheetPath) as StyleSheet
                : EditorGUIUtility.Load(s_LightStyleSheetPath) as StyleSheet;
            root.styleSheets.Add(themedStyleSheet);

            var nextId = 0;
            var items = new List<TreeViewItemData<SampleTreeItem>>()
            {
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Styles", StylesExplorer.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Button", ButtonSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Scroller", ScrollerSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Toggle", ToggleSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("ToggleButtonGroup", ToggleButtonGroupSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("RadioButton", RadioButtonSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Label", LabelSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Text Field", TextFieldSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("HelpBox", HelpBoxSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Object Field", ObjectFieldSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("List View", ListViewSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Tree View", TreeViewSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Tab View", TabViewSnippet.Create)),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Numeric Fields", MakeNumericFieldsPanel), new List<TreeViewItemData<SampleTreeItem>>()
                {
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Integer", IntegerFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Float", FloatFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Long", LongFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("MinMaxSlider", MinMaxSliderSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Slider", SliderSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Vector2", Vector2FieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Vector3", Vector3FieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Vector4", Vector4FieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Rect", RectFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Bounds", BoundsFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("UnsignedInteger", UnsignedIntegerFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("UnsignedLong", UnsignedLongFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("SliderInt", SliderIntSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Vector2Int", Vector2IntFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Vector3Int", Vector3IntFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("RectInt", RectIntFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("BoundsInt", BoundsIntFieldSnippet.Create))
                }),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Value Fields", MakeValueFieldsPanel), new List<TreeViewItemData<SampleTreeItem>>()
                {
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Color", ColorFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Curve", CurveFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Gradient", GradientFieldSnippet.Create))
                }),
                new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Choice Fields", MakeChoiceFieldsPanel), new List<TreeViewItemData<SampleTreeItem>>()
                {
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Enum", EnumFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("EnumFlags", EnumFlagsFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Popup", PopupFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Tag", TagFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Mask", MaskFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("Layer", LayerFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("LayerMask", LayerMaskFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("DropdownField", DropdownFieldSnippet.Create)),
                    new TreeViewItemData<SampleTreeItem>(nextId++, new SampleTreeItem("RadioButtonGroup", RadioButtonGroupSnippet.Create)),
                }),
            };

            var treeView = new TreeView() { name = k_TreeViewName };
            treeView.AddToClassList(k_TreeViewClassName);
            m_ContentPanel = new VisualElement() { name = k_ContentPanelName };
            m_ContentPanel.AddToClassList(k_ContentPanelClassName);

            Func<VisualElement> makeItem = () =>
            {
                var box = new VisualElement();
                box.AddToClassList(k_TreeItemClassName);

                var label = new Label();
                label.AddToClassList(k_TreeItemLabelClassName);

                box.Add(label);
                return box;
            };

            Action<VisualElement, int> bindItem = (element, index) =>
            {
                var item = treeView.GetItemDataForIndex<SampleTreeItem>(index);
                (element.ElementAt(0) as Label).text = item.name;
            };

            Action<IEnumerable<int>> onSelectionChanged = selectedIndices =>
            {
                if (!selectedIndices.Any())
                    return;

                var sampleItem = treeView.GetItemDataForIndex<SampleTreeItem>(selectedIndices.First());
                m_ContentPanel.Clear();
                m_ContentPanel.Add(sampleItem.makeItem(sampleItem));
            };

            var splitter = new DebuggerSplitter();
            splitter.AddToClassList(k_SplitterClassName);
            splitter.leftPane.style.width = k_SplitterLeftPaneStartingWidth;
            root.Add(splitter);

            splitter.leftPane.Add(treeView);
            splitter.rightPane.Add(m_ContentPanel);

            treeView.viewDataKey = "samples-tree";
            treeView.fixedItemHeight = 20;
            treeView.SetRootItems(items);
            treeView.makeItem = makeItem;
            treeView.bindItem = bindItem;
            treeView.selectedIndicesChanged += onSelectionChanged;
            treeView.Rebuild();

            // Force TreeView to call onSelectionChanged when it restores its own selection from view data.
            treeView.schedule.Execute(() =>
            {
                onSelectionChanged(treeView.selectedIndices);
            }).StartingIn(k_TreeViewSelectionRestoreDelay);

            // Force TreeView to select something if nothing is selected.
            treeView.schedule.Execute(() =>
            {
                if (treeView.selectedItems.Count() > 0)
                    return;

                treeView.SetSelection(0);

                // Auto-expand all items on load.
                treeView.ExpandAll();
            }).StartingIn(k_TreeViewInitialSelectionDelay);
        }

        private VisualElement MakeNumericFieldsPanel(SampleTreeItem item)
        {
            var scrollView = new ScrollView();

            var container = new VisualElement();
            scrollView.Add(container);
            container.AddToClassList(k_CategoryPanelClassName);

            container.Add(new Label("Numeric Fields") { classList = { k_CategoryTitleClassName } });

            container.Add(new IntegerField("Integer"));
            container.Add(new FloatField("Float"));
            container.Add(new LongField("Long"));
            container.Add(new MinMaxSlider("MinMaxSlider", 0, 20, -10, 40) { value = new Vector2(10, 12) });
            container.Add(new Slider("Slider"));
            container.Add(new Vector2Field("Vector2"));
            container.Add(new Vector3Field("Vector3"));
            container.Add(new Vector4Field("Vector4"));
            container.Add(new RectField("Rect"));
            container.Add(new BoundsField("Bounds"));
            container.Add(new UnsignedIntegerField("UnsignedInteger"));
            container.Add(new UnsignedLongField("UnsignedLong"));
            container.Add(new SliderInt("SliderInt"));
            container.Add(new Vector2IntField("Vector2Int"));
            container.Add(new Vector3IntField("Vector3Int"));
            container.Add(new RectIntField("RectInt"));
            container.Add(new BoundsIntField("BoundsInt"));

            return scrollView;
        }

        private VisualElement MakeValueFieldsPanel(SampleTreeItem item)
        {
            var scrollView = new ScrollView();

            var container = new VisualElement();
            scrollView.Add(container);
            container.AddToClassList(k_CategoryPanelClassName);

            container.Add(new Label("Value Fields") { classList = { k_CategoryTitleClassName } });

            var curve = new AnimationCurve(new Keyframe[]
                { new Keyframe(0, 0), new Keyframe(5, 8), new Keyframe(10, 4) });

            var gradient = new Gradient();
            gradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0),
                new GradientColorKey(Color.blue, 10),
                new GradientColorKey(Color.green, 20)
            };

            container.Add(new ColorField("Color") { value = Color.cyan });
            container.Add(new CurveField("Curve") { value = curve });
            container.Add(new GradientField("Gradient") { value = gradient });

            return scrollView;
        }

        private VisualElement MakeChoiceFieldsPanel(SampleTreeItem item)
        {
            var scrollView = new ScrollView();

            var container = new VisualElement();
            scrollView.Add(container);
            container.AddToClassList(k_CategoryPanelClassName);

            container.Add(new Label("Numeric Fields") { classList = { k_CategoryTitleClassName } });

            var choices = new List<string> { "First", "Second", "Third" };

            container.Add(new EnumField("Enum", TextAlignment.Center));
            container.Add(new PopupField<string>("Popup", choices, 0));
            container.Add(new TagField("Tag", "Player"));
            container.Add(new MaskField("Mask", choices, 1));
            container.Add(new LayerField("Layer"));
            container.Add(new LayerMaskField("LayerMask"));

            return scrollView;
        }
    }
}
