using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderLibraryPlainView : BuilderLibraryView
    {
        public class LibraryPlainViewItem : VisualElement
        {
            readonly BuilderLibraryTreeItem m_TreeItem;
            readonly VisualElement m_Icon;

            public int Id => m_TreeItem.id;
            public VisualElement content { get; }

            public LibraryPlainViewItem(BuilderLibraryTreeItem libraryTreeItem)
            {
                m_TreeItem = libraryTreeItem;
                var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.LibraryUIPath + "/BuilderLibraryPlainViewItem.uxml");
                template.CloneTree(this);

                var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.LibraryUIPath + "/BuilderLibraryPlainViewItem.uss");
                styleSheets.Add(styleSheet);

                content = ElementAt(0);
                if (m_TreeItem == null)
                {
                    content.AddToClassList(k_PlainViewNoHoverVariantUssClassName);
                    content.Clear();
                    return;
                }

                this.Q<Label>().text = m_TreeItem.data;
                m_Icon = this.Q<VisualElement>("icon");
                SetIcon(m_TreeItem.largeIcon);
            }

            public void SwitchToDarkSkinIcon() => SetIcon(m_TreeItem.darkSkinLargeIcon);
            public void SwitchToLightSkinIcon() => SetIcon(m_TreeItem.lightSkinLargeIcon);

            void SetIcon(Texture2D icon)
            {
                var styleBackgroundImage = m_Icon.style.backgroundImage;
                styleBackgroundImage.value = new Background { texture = icon };
                m_Icon.style.backgroundImage = styleBackgroundImage;
            }
        }

        const string k_PlainViewFoldoutStyle = "unity-builder-library__controls-category-foldout";
        const string k_PlainViewNoHoverVariantUssClassName = "plain-view__item--no-hover";
        const string k_PlainViewSelectedVariantUssClassName = "plain-view__item--selected";
        const string k_ViewSelectedWithNoFocusVariantUssClassName = "plain-view__item--selected-no-focus";

        const string k_ContentContainerName = "content";

        readonly VisualElement m_ContentContainer;
        readonly List<LibraryPlainViewItem> m_DummyItems = new List<LibraryPlainViewItem>();
        readonly List<LibraryPlainViewItem> m_PlainViewItems = new List<LibraryPlainViewItem>();
        readonly IEnumerable<ITreeViewItem> m_Items;

        LibraryPlainViewItem m_SelectedItem;

        [SerializeField]
        int m_SelectedItemId;

        public override VisualElement primaryFocusable => m_ContentContainer;

        public BuilderLibraryPlainView(IEnumerable<ITreeViewItem> items)
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.LibraryUIPath + "/BuilderLibraryPlainView.uxml");
            builderTemplate.CloneTree(this);

            m_Items = items;
            m_ContentContainer = this.Q<VisualElement>(k_ContentContainerName);
            m_ContentContainer.focusable = true;
            m_ContentContainer.RegisterCallback<FocusEvent>(OnFocusEvent);
            m_ContentContainer.RegisterCallback<BlurEvent>(OnBlurEvent);
        }

        void OnBlurEvent(BlurEvent evt)
        {
            if (m_SelectedItem != null)
            {
                m_SelectedItem.content.RemoveFromClassList(k_PlainViewSelectedVariantUssClassName);
                if (!EditorGUIUtility.isProSkin)
                    m_SelectedItem.SwitchToLightSkinIcon();

                m_SelectedItem.content.AddToClassList(k_ViewSelectedWithNoFocusVariantUssClassName);
            }
        }

        void OnFocusEvent(FocusEvent evt)
        {
            if (m_SelectedItem != null)
            {
                m_SelectedItem.content.RemoveFromClassList(k_ViewSelectedWithNoFocusVariantUssClassName);
                if (!EditorGUIUtility.isProSkin)
                    m_SelectedItem.SwitchToDarkSkinIcon();

                m_SelectedItem.content.AddToClassList(k_PlainViewSelectedVariantUssClassName);
            }
        }

        public override void SetupView(BuilderLibraryDragger dragger, BuilderTooltipPreview tooltipPreview,
            BuilderPaneContent builderPaneContent, BuilderPaneWindow builderPaneWindow,
            VisualElement documentElement, BuilderSelection selection)
        {
            base.SetupView(dragger, tooltipPreview, builderPaneContent, builderPaneWindow, documentElement, selection);
            FillView(m_Items);
        }

        void AdjustSpace(PersistedFoldout foldout)
        {
            if (foldout.contentContainer.style.display == DisplayStyle.None)
                return;

            foreach (var dummy in m_DummyItems)
            {
                if (dummy.parent == foldout)
                    dummy.RemoveFromHierarchy();
            }

            var plainViewItem = foldout.contentContainer.Q<LibraryPlainViewItem>();
            var plainViewItemSize = new Vector2(plainViewItem.resolvedStyle.width, plainViewItem.resolvedStyle.height);
            var itemsInRow = (int) Mathf.Floor(foldout.contentContainer.resolvedStyle.width / plainViewItemSize.x);

            var foldoutItemsCount = foldout.contentContainer.childCount;
            if (foldoutItemsCount <= itemsInRow)
            {
                foldout.contentContainer.style.justifyContent = Justify.FlexStart;
                return;
            }

            foldout.contentContainer.style.justifyContent = Justify.SpaceAround;
            var rem = foldoutItemsCount % itemsInRow;
            if (rem == 0)
                return;

            var numberOfRequiredDummies = itemsInRow - rem;

            for (var i = 0; i < numberOfRequiredDummies; i++)
                foldout.Add(GetDummyItemView());
        }

        LibraryPlainViewItem GetDummyItemView()
        {
            foreach (var item in m_DummyItems.Where(item => item.parent == null))
            {
                return item;
            }

            var newItem = new LibraryPlainViewItem(null);
            m_DummyItems.Add(newItem);
            return newItem;
        }

        public override VisualElement contentContainer => m_ContentContainer == null ? this : m_ContentContainer;

        void FillView(IEnumerable<ITreeViewItem> items, VisualElement itemsParent = null)
        {
            foreach (var item in items)
            {
                if (item is BuilderLibraryTreeItem libraryTreeItem)
                {
                    if (libraryTreeItem.isHeader)
                    {
                        var categoryFoldout = new LibraryFoldout {text = libraryTreeItem.data};
                        if (libraryTreeItem.isEditorOnly)
                        {
                            categoryFoldout.tag = BuilderConstants.EditorOnlyTag;
                            categoryFoldout.Q(LibraryFoldout.TagLabelName).AddToClassList(BuilderConstants.TagPillClassName);
                        }
                        categoryFoldout.contentContainer.RegisterCallback<GeometryChangedEvent>(e => AdjustSpace(categoryFoldout));
                        categoryFoldout.AddToClassList(k_PlainViewFoldoutStyle);
                        Add(categoryFoldout);
                        FillView(libraryTreeItem.children, categoryFoldout);
                        continue;
                    }

                    var plainViewItem = new LibraryPlainViewItem(libraryTreeItem);
                    plainViewItem.AddManipulator(new ContextualMenuManipulator(OnContextualMenuPopulateEvent));
                    plainViewItem.RegisterCallback<MouseDownEvent, LibraryPlainViewItem>(OnPlainViewItemMouseDown, plainViewItem);

                    LinkToTreeViewItem(plainViewItem, libraryTreeItem);

                    // The element set up is not yet completed at this point.
                    // SetupView has to be called as well.
                    plainViewItem.RegisterCallback<AttachToPanelEvent>(e =>
                    {
                        RegisterControlContainer(plainViewItem);
                    });

                    plainViewItem.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (e.clickCount == 2)
                            AddItemToTheDocument(libraryTreeItem);
                    });
                    itemsParent?.Add(plainViewItem);
                    m_PlainViewItems.Add(plainViewItem);
                }
            }
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            OverwriteFromViewData(this, viewDataKey);
            DoSelected(m_SelectedItemId);

            if (!m_ContentContainer.IsFocused())
                OnBlurEvent(null);
        }

        void OnPlainViewItemMouseDown(MouseDownEvent evt, LibraryPlainViewItem plainViewItem)
        {
             if (evt.button != (int)MouseButton.LeftMouse)
                 return;

             DoSelected(plainViewItem.Id);
        }

        void DoSelected(int itemId)
        {
            m_SelectedItemId = itemId;
            if (m_SelectedItem != null)
            {
                m_SelectedItem.content.RemoveFromClassList(k_PlainViewSelectedVariantUssClassName);
                m_SelectedItem.content.RemoveFromClassList(k_ViewSelectedWithNoFocusVariantUssClassName);
                if (!EditorGUIUtility.isProSkin)
                    m_SelectedItem.SwitchToLightSkinIcon();
            }

            foreach (var plainViewItem in m_PlainViewItems)
            {
                if (plainViewItem.Id.Equals(itemId))
                {
                    m_SelectedItem = plainViewItem;
                    m_SelectedItem.content.AddToClassList(k_PlainViewSelectedVariantUssClassName);
                    if (!EditorGUIUtility.isProSkin)
                        m_SelectedItem.SwitchToDarkSkinIcon();

                    SaveViewData();
                }
            }
        }

        void OnContextualMenuPopulateEvent(ContextualMenuPopulateEvent evt)
        {
            var libraryItem = GetLibraryTreeItem((VisualElement) evt.target);

            evt.menu.AppendAction(
                "Add",
                action => { AddItemToTheDocument(libraryItem); },
                action => DropdownMenuAction.Status.Normal);
        }

        public override void Refresh() { }
    }
}
