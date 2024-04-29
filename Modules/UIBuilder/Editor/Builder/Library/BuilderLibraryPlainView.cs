// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using BuilderLibraryItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;
using TreeViewItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;

namespace Unity.UI.Builder
{
    class BuilderLibraryPlainView : BuilderLibraryView
    {
        public class LibraryPlainViewItem : VisualElement
        {
            readonly BuilderLibraryItem m_TreeItem;
            readonly VisualElement m_Icon;

            public int Id => m_TreeItem.id;
            public VisualElement content { get; }

            public LibraryPlainViewItem(BuilderLibraryItem libraryTreeItem)
            {
                m_TreeItem = libraryTreeItem;
                var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.LibraryUIPath + "/BuilderLibraryPlainViewItem.uxml");
                template.CloneTree(this);

                var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.LibraryUIPath + "/BuilderLibraryPlainViewItem.uss");
                styleSheets.Add(styleSheet);

                content = ElementAt(0);
                if (m_TreeItem.data == null)
                {
                    content.AddToClassList(k_PlainViewNoHoverVariantUssClassName);
                    content.Clear();
                    return;
                }

                m_Icon = this.Q<VisualElement>("icon");
                this.Q<Label>().text = m_TreeItem.data.name;
                SetIcon(m_TreeItem.data.largeIcon);
            }

            public void SwitchToDarkSkinIcon() => SetIcon(m_TreeItem.data.darkSkinLargeIcon);
            public void SwitchToLightSkinIcon() => SetIcon(m_TreeItem.data.lightSkinLargeIcon);

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

        LibraryPlainViewItem m_SelectedItem;

        [SerializeField]
        int m_SelectedItemId;

        public override VisualElement primaryFocusable => m_ContentContainer;

        public BuilderLibraryPlainView(IList<TreeViewItem> items)
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

        public override VisualElement contentContainer => m_ContentContainer == null ? this : m_ContentContainer;

        void FillView(IEnumerable<TreeViewItem> items, VisualElement itemsParent = null)
        {
            foreach (var item in items)
            {
                var libraryTreeItem = item.data;
                if (libraryTreeItem.isHeader)
                {
                    var categoryFoldout = new LibraryFoldout {text = libraryTreeItem.name};
                    if (libraryTreeItem.isEditorOnly)
                    {
                        categoryFoldout.tag = BuilderConstants.EditorOnlyTag;
                        categoryFoldout.Q(LibraryFoldout.TagLabelName).AddToClassList(BuilderConstants.TagPillClassName);
                    }
                    categoryFoldout.AddToClassList(k_PlainViewFoldoutStyle);
                    Add(categoryFoldout);
                    FillView(item.children, categoryFoldout);
                    continue;
                }

                var plainViewItem = new LibraryPlainViewItem(item);
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
                    if (e.clickCount == 2 && e.button == (int)MouseButton.LeftMouse)
                    {
                        AddItemToTheDocument(libraryTreeItem);
                    }
                });
                itemsParent?.Add(plainViewItem);
                m_PlainViewItems.Add(plainViewItem);
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
            if (m_Dragger.active)
            {
                evt.StopImmediatePropagation();
                return;
            }

            var libraryItem = GetLibraryTreeItem(evt.elementTarget);

            evt.menu.AppendAction(
                "Add",
                action => { AddItemToTheDocument(libraryItem); },
                action => DropdownMenuAction.Status.Normal);
        }

        public override void Refresh() {}

        public override void FilterView(string value)
        {
            m_ContentContainer.Clear();
            m_PlainViewItems.Clear();
            m_VisibleItems = string.IsNullOrEmpty(value) ? m_Items : FilterTreeViewItems(m_Items, value);
            FillView(m_VisibleItems);
        }
    }
}
