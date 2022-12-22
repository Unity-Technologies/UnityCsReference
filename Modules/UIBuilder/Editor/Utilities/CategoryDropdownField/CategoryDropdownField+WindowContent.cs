// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace Unity.UI.Builder
{
    using Item = CategoryDropdownContent.Item;
    using ItemType = CategoryDropdownContent.ItemType;

    partial class CategoryDropdownField
    {
        class WindowContent : PopupWindowContent
        {
            const string k_UssPath = BuilderConstants.UtilitiesPath + "/CategoryDropdownField/CategoryDropdownContent.uss";
            const string k_SelectionContextKey = "CategoryDropdownField.SelectionContext";
            const string k_BaseClass = "unity-category-dropdown-field";
            const string k_Category = k_BaseClass + "__category";
            const string k_Item = k_BaseClass + "__item";
            const string k_ItemInCategory = k_BaseClass + "__category-item";
            const string k_Separator = k_BaseClass + "__separator";
            const string k_SearchField = k_BaseClass + "__search-field";

            class SelectionContext
            {
                public int index;
                public Item item;
                public WindowContent content;
            }

            static readonly UnityEngine.Pool.ObjectPool<TextElement> s_CategoryPool = new UnityEngine.Pool.ObjectPool<TextElement>(() =>
            {
                var category = new TextElement();
                category.AddToClassList(k_Category);
                return category;
            }, null, te =>
            {
                te.style.display = DisplayStyle.Flex;
            });

            static readonly UnityEngine.Pool.ObjectPool<TextElement> s_ItemPool = new UnityEngine.Pool.ObjectPool<TextElement>(() =>
            {
                var value = new TextElement();
                value.style.display = DisplayStyle.Flex;
                value.AddToClassList(k_Item);
                return value;
            }, null, te =>
            {
                te.pseudoStates &= ~PseudoStates.Checked;
                te.style.display = DisplayStyle.Flex;
                te.RemoveFromClassList(k_ItemInCategory);
            });

            static readonly UnityEngine.Pool.ObjectPool<VisualElement> s_SeparatorPool = new UnityEngine.Pool.ObjectPool<VisualElement>(() =>
            {
                var separator = new VisualElement();
                separator.AddToClassList(k_Separator);
                return separator;
            }, null, ve =>
            {
                ve.style.display = DisplayStyle.Flex;
            });

            readonly List<Item> m_Items = new List<Item>();

            Vector2 m_Size;
            string m_CurrentActiveValue;
            int m_SelectedIndex = -1;
            ScrollView m_ScrollView;
            KeyboardNavigationManipulator m_NavigationManipulator;

            public event Action<string> onSelectionChanged;

            public void Show(Rect rect, string currentValue, IEnumerable<Item> items)
            {
                m_CurrentActiveValue = currentValue;

                m_Items.Clear();
                m_Items.AddRange(items);

                m_Size = new Vector2(rect.width, 225);
                PopupWindow.Show(rect, this);
            }

            public override void OnOpen()
            {
                var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath);
                editorWindow.rootVisualElement.styleSheets.Add(styleSheet);
                editorWindow.rootVisualElement.focusable = true;

                editorWindow.rootVisualElement.AddToClassList(k_BaseClass);
                editorWindow.rootVisualElement.AddManipulator(m_NavigationManipulator = new KeyboardNavigationManipulator(Apply));

                var searchField = new ToolbarSearchField();
                searchField.AddToClassList(k_SearchField);
                searchField.RegisterCallback<AttachToPanelEvent>(evt =>
                {
                    evt.elementTarget?.Focus();
                });

                searchField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    switch (evt.keyCode)
                    {
                        case KeyCode.UpArrow:
                        case KeyCode.DownArrow:
                        case KeyCode.PageDown:
                        case KeyCode.PageUp:
                            evt.StopPropagation();
                            m_ScrollView.Focus();
                            break;
                        case KeyCode.Return:
                        case KeyCode.KeypadEnter:
                            evt.StopPropagation();
                            if (string.IsNullOrWhiteSpace(searchField.value) || m_SelectedIndex < 0)
                            {
                                m_ScrollView.Focus();
                                return;
                            }

                            onSelectionChanged?.Invoke(m_Items[m_SelectedIndex].value);
                            editorWindow.Close();
                            break;
                    }
                });

                editorWindow.rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
                {
                    searchField.Focus();
                });

                searchField.RegisterValueChangedCallback(OnSearchChanged);
                editorWindow.rootVisualElement.Add(searchField);

                m_ScrollView = new ScrollView();
                m_ScrollView.RegisterCallback<GeometryChangedEvent, ScrollView>((evt, sv) =>
                {
                    if (m_SelectedIndex >= 0)
                        sv.ScrollTo(sv[m_SelectedIndex]);
                }, m_ScrollView);

                var selectionWasSet = false;
                for (var i = 0; i < m_Items.Count; ++i)
                {
                    var property = m_Items[i];
                    var element = GetPooledItem(property, i);
                    m_ScrollView.Add(element);

                    if (selectionWasSet)
                        continue;

                    if (property.itemType != ItemType.Item || property.value != m_CurrentActiveValue)
                        continue;

                    m_SelectedIndex = i;
                    element.pseudoStates |= PseudoStates.Checked;
                    selectionWasSet = true;
                }
                editorWindow.rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.F && evt.actionKey)
                    {
                        searchField.Focus();
                    }
                }, TrickleDown.TrickleDown);

                editorWindow.rootVisualElement.Add(m_ScrollView);
            }

            public override void OnClose()
            {
                editorWindow.rootVisualElement.RemoveManipulator(m_NavigationManipulator);

                // Return to pool
                for (var i = 0; i < m_Items.Count; ++i)
                {
                    switch(m_Items[i].itemType)
                    {
                        case ItemType.Category:
                            s_CategoryPool.Release((TextElement)m_ScrollView[i]);
                            break;
                        case ItemType.Separator:
                            s_SeparatorPool.Release(m_ScrollView[i]);
                            break;
                        case ItemType.Item:
                            s_ItemPool.Release((TextElement)m_ScrollView[i]);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                m_ScrollView.Clear();
            }

            bool SetSelection(int index)
            {
                if (index < 0 || index >= m_ScrollView.childCount)
                {
                    if (m_SelectedIndex >= 0)
                    {
                        var previous = m_ScrollView[m_SelectedIndex];
                        previous.pseudoStates &= ~PseudoStates.Checked;
                    }

                    m_SelectedIndex = -1;
                    return false;
                }

                if (m_SelectedIndex >= 0)
                {
                    var previous = m_ScrollView[m_SelectedIndex];
                    previous.pseudoStates &= ~PseudoStates.Checked;
                }

                m_SelectedIndex = index;
                var next = m_ScrollView[m_SelectedIndex];
                next.pseudoStates |= PseudoStates.Checked;
                m_ScrollView.ScrollTo(next);
                return true;
            }

            void ResetSearch()
            {
                for (var i = 0; i < m_ScrollView.childCount; ++i)
                {
                    var element = m_ScrollView[i];
                    element.style.display = DisplayStyle.Flex;
                }
            }

            void OnSearchChanged(ChangeEvent<string> evt)
            {
                var searchString = evt.newValue;
                if (string.IsNullOrEmpty(searchString))
                {
                    ResetSearch();
                    return;
                }

                for (var i = 0; i < m_Items.Count; ++i)
                {
                    var item = m_Items[i];
                    var element = m_ScrollView[i];

                    switch (item.itemType)
                    {
                        case ItemType.Category:
                        {
                            var categoryIndex = i;
                            var shouldDisplayCategory = false;
                            // Manually iterate through the item of the current category
                            for (; i + 1 < m_Items.Count; ++i)
                            {
                                var sub = i + 1;
                                var categoryItem = m_Items[sub];
                                var categoryElement = m_ScrollView[sub];
                                if (categoryItem.itemType == ItemType.Item &&
                                    categoryItem.categoryName == item.displayName)
                                {
                                    if (categoryItem.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        categoryItem.value.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        shouldDisplayCategory = true;
                                        categoryElement.style.display = DisplayStyle.Flex;
                                    }
                                    else
                                    {
                                        categoryElement.style.display = DisplayStyle.None;
                                    }
                                }
                                else
                                    break;
                            }

                            m_ScrollView[categoryIndex].style.display = shouldDisplayCategory ? DisplayStyle.Flex : DisplayStyle.None;
                            break;
                        }
                        case ItemType.Item:
                            if (item.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                item.value.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                element.style.display = DisplayStyle.Flex;
                            }
                            else
                            {
                                element.style.display = DisplayStyle.None;
                            }

                            break;
                        case ItemType.Separator:
                            m_ScrollView[i].style.display = DisplayStyle.None;
                            break;
                    }
                }

                // Check if previous selection is still visible, otherwise select the first shown item
                if (m_SelectedIndex >= 0 && m_ScrollView[m_SelectedIndex].style.display == DisplayStyle.Flex)
                    return;

                if (!SelectFirstDisplayedItem())
                    SetSelection(-1);
            }

            VisualElement GetPooledItem(Item item, int index)
            {
                switch (item.itemType)
                {
                    case ItemType.Category:
                        var category = s_CategoryPool.Get();
                        category.text = item.displayName;
                        return category;
                    case ItemType.Separator:
                        return s_SeparatorPool.Get();
                        case ItemType.Item:
                        var element = s_ItemPool.Get();
                        element.text = item.displayName;
                        element.tooltip = item.value;

                        var context = (SelectionContext)element.GetProperty(k_SelectionContextKey);
                        if (null == context)
                        {
                            context = new SelectionContext();
                            element.SetProperty(k_SelectionContextKey, context);
                            element.RegisterCallback<PointerUpEvent>(OnItemSelected);
                        }

                        context.index = index;
                        context.item = item;
                        context.content = this;

                        if (!string.IsNullOrWhiteSpace(item.categoryName))
                            element.AddToClassList(k_ItemInCategory);

                        return element;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void OnItemSelected(PointerUpEvent evt)
            {
                var e = evt.elementTarget;
                var ctx = (SelectionContext) e.GetProperty(k_SelectionContextKey);
                // We must go through the context here, because the elements are pooled and the closure would bind on
                // the previous time the element was used.
                ctx.content.SetSelection(ctx.index);
                ctx.content.onSelectionChanged?.Invoke(ctx.item.value);
                ctx.content.editorWindow.Close();
            }

            bool SelectFirstDisplayedItem()
            {
                for (var i = 0; i < m_Items.Count; ++i)
                {
                    if (m_Items[i].itemType == ItemType.Item && m_ScrollView[i].style.display == DisplayStyle.Flex)
                        return SetSelection(i);
                }

                return false;
            }

            bool SelectLastDisplayedItem()
            {
                for (var i = m_Items.Count - 1; i >= 0; --i)
                {
                    if (m_Items[i].itemType == ItemType.Item && m_ScrollView[i].style.display == DisplayStyle.Flex)
                        return SetSelection(i);
                }

                return false;
            }

            bool SelectNextDisplayedItem(int offset = 1)
            {
                var current = m_SelectedIndex;
                var initialIndex = Mathf.Clamp(m_SelectedIndex + offset, 0, m_Items.Count - 1);
                for (var i = initialIndex; i < m_Items.Count; ++i)
                {
                    if (m_Items[i].itemType == ItemType.Item &&
                        m_ScrollView[i].style.display == DisplayStyle.Flex
                        && i != current)
                        return SetSelection(i);
                }

                return false;
            }

            bool SelectPreviousDisplayedItem(int offset = 1)
            {
                var current = m_SelectedIndex;
                var initialIndex = Mathf.Clamp(m_SelectedIndex - offset, 0, m_Items.Count - 1);
                for (var i = initialIndex; i >= 0; --i)
                {
                    if (m_Items[i].itemType == ItemType.Item &&
                        m_ScrollView[i].style.display == DisplayStyle.Flex &&
                        i != current)
                    {
                        return SetSelection(i);
                    }
                }

                return false;
            }

            void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
            {
                if (!Apply(op))
                    return;

                sourceEvent.StopImmediatePropagation();
                sourceEvent.PreventDefault();
            }

            bool Apply(KeyboardNavigationOperation op)
            {
                switch (op)
                {
                    case KeyboardNavigationOperation.None:
                    case KeyboardNavigationOperation.SelectAll:
                        break;

                    case KeyboardNavigationOperation.Cancel:
                        editorWindow.Close();
                        break;
                    case KeyboardNavigationOperation.Submit:
                        if (m_SelectedIndex < 0)
                            return false;

                        onSelectionChanged?.Invoke(m_Items[m_SelectedIndex].value);
                        editorWindow.Close();
                        break;
                    case KeyboardNavigationOperation.Previous:
                    {
                        return SelectPreviousDisplayedItem() ||
                               SelectLastDisplayedItem();
                    }
                    case KeyboardNavigationOperation.Next:
                    {
                        return SelectNextDisplayedItem() ||
                               SelectFirstDisplayedItem();
                    }
                    case KeyboardNavigationOperation.PageUp:
                    {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        return SelectPreviousDisplayedItem(10) ||
                               SelectFirstDisplayedItem() ||
                               true;
                    }
                    case KeyboardNavigationOperation.PageDown:
                    {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        return SelectNextDisplayedItem(10) ||
                               SelectLastDisplayedItem() ||
                               true;
                    }
                    case KeyboardNavigationOperation.Begin:
                    {
                        SelectFirstDisplayedItem();
                        return true;
                    }
                    case KeyboardNavigationOperation.End:
                    {
                        SelectLastDisplayedItem();
                        return true;
                    }
                }

                return false;
            }

            public override void OnGUI(Rect rect)
            {
                // Intentionally left empty.
            }

            public override Vector2 GetWindowSize()
            {
                return m_Size;
            }
        }
    }
}
