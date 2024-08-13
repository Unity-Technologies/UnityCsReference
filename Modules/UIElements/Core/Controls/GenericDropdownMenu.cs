// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    internal interface IGenericMenu
    {
        void AddItem(string itemName, bool isChecked, Action action);
        void AddItem(string itemName, bool isChecked, Action<object> action, object data);
        void AddDisabledItem(string itemName, bool isChecked);
        void AddSeparator(string path);
        void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false);
    }

    /// <summary>
    /// GenericDropdownMenu allows you to display contextual menus with default textual options or any <see cref="VisualElement"/>.
    /// </summary>
    /// <remarks>
    /// The GenericDropdownMenu is a generic implementation of a dropdown menu that you can use in both Editor UI and runtime UI.
    /// </remarks>
    /// <example>
    /// The following example creates a dropdown menu with three items. It displays the menu when the user clicks the button. The example also demonstrates how to set 
    /// the width of the dropdown menu with the @@DropDown@@ method.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/MenuExample.cs"/>
    /// </example>
    public class GenericDropdownMenu : IGenericMenu
    {
        internal class MenuItem
        {
            public string name;
            public VisualElement element;
            public Action action;
            public Action<object> actionUserData;
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-base-dropdown";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public static readonly string itemUssClassName = ussClassName + "__item";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public static readonly string itemContentUssClassName = ussClassName + "__item-content";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of inner containers in elements of this type.
        /// </summary>
        public static readonly string containerInnerUssClassName = ussClassName + "__container-inner";
        /// <summary>
        /// USS class name of outer containers in elements of this type.
        /// </summary>
        public static readonly string containerOuterUssClassName = ussClassName + "__container-outer";
        /// <summary>
        /// USS class name of separators in elements of this type.
        /// </summary>
        public static readonly string checkmarkUssClassName = ussClassName + "__checkmark";
        /// <summary>
        /// USS class name of separators in elements of this type.
        /// </summary>
        public static readonly string separatorUssClassName = ussClassName + "__separator";
        /// <summary>
        /// USS class name that's added when the GenericDropdownMenu fits the width of its content.
        /// </summary>
        public static readonly string contentWidthUssClassName = ussClassName + "--content-width-menu";

        const float k_MenuItemPadding = 20f;
        const float k_MenuPadding = 2f;

        List<MenuItem> m_Items = new List<MenuItem>();
        // Used in tests
        internal List<MenuItem> items => m_Items;

        VisualElement m_MenuContainer;
        VisualElement m_OuterContainer;
        ScrollView m_ScrollView;
        VisualElement m_PanelRootVisualContainer;
        VisualElement m_TargetElement;
        Rect m_DesiredRect;
        KeyboardNavigationManipulator m_NavigationManipulator;
        float m_PositionTop;
        float m_PositionLeft;
        float m_ContentWidth;
        bool m_FitContentWidth;
        bool m_ShownAboveTarget;

        internal VisualElement menuContainer => m_MenuContainer;
        internal VisualElement outerContainer => m_OuterContainer;
        internal ScrollView scrollView => m_ScrollView;

        internal bool isSingleSelectionDropdown { get; set; }
        internal bool closeOnParentResize { get; set; }

        /// <summary>
        /// Returns the content container for the <see cref="GenericDropdownMenu"/>. Allows users to create their own
        /// dropdown menu if they don't want to use the default implementation.
        /// </summary>
        public VisualElement contentContainer => m_ScrollView.contentContainer;

        /// <summary>
        ///  Initializes and returns an instance of GenericDropdownMenu.
        /// </summary>
        public GenericDropdownMenu()
        {
            m_MenuContainer = new VisualElement();
            m_MenuContainer.AddToClassList(ussClassName);

            m_OuterContainer = new VisualElement();
            m_OuterContainer.AddToClassList(containerOuterUssClassName);
            m_MenuContainer.Add(m_OuterContainer);

            m_ScrollView = new ScrollView();
            m_ScrollView.AddToClassList(containerInnerUssClassName);
            m_ScrollView.pickingMode = PickingMode.Position;
            m_ScrollView.contentContainer.focusable = true;
            m_ScrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            m_ScrollView.mode = ScrollViewMode.VerticalAndHorizontal;
            m_OuterContainer.hierarchy.Add(m_ScrollView);

            m_MenuContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_MenuContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            isSingleSelectionDropdown = true;
            closeOnParentResize = true;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            contentContainer.AddManipulator(m_NavigationManipulator = new KeyboardNavigationManipulator(Apply));
            m_MenuContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            m_MenuContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            m_MenuContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);

            evt.destinationPanel.visualTree.RegisterCallback<GeometryChangedEvent>(OnParentResized);
            m_ScrollView.RegisterCallback<GeometryChangedEvent>(OnInitialDisplay, InvokePolicy.Once);
            m_ScrollView.RegisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);
            m_ScrollView.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            contentContainer.RemoveManipulator(m_NavigationManipulator);
            m_MenuContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_MenuContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            m_MenuContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);

            evt.originPanel.visualTree.UnregisterCallback<GeometryChangedEvent>(OnParentResized);
            m_ScrollView.UnregisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);
            m_ScrollView.UnregisterCallback<FocusOutEvent>(OnFocusOut);
        }

        void Hide(bool giveFocusBack = false)
        {
            m_MenuContainer.RemoveFromHierarchy();

            if (m_TargetElement != null)
            {
                m_TargetElement.UnregisterCallback<DetachFromPanelEvent>(OnTargetElementDetachFromPanel);
                m_TargetElement.pseudoStates ^= PseudoStates.Active;
                if (giveFocusBack && m_TargetElement.canGrabFocus)
                    m_TargetElement.Focus();
            }

            m_TargetElement = null;
        }

        void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            if (Apply(op))
            {
                sourceEvent.StopPropagation();
            }
        }

        bool Apply(KeyboardNavigationOperation op)
        {
            var selectedIndex = GetSelectedIndex();

            void UpdateSelectionDown(int newIndex)
            {
                while (newIndex < m_Items.Count)
                {
                    if (m_Items[newIndex].element.enabledSelf)
                    {
                        ChangeSelectedIndex(newIndex, selectedIndex);
                        break;
                    }

                    ++newIndex;
                }
            }

            void UpdateSelectionUp(int newIndex)
            {
                while (newIndex >= 0)
                {
                    if (m_Items[newIndex].element.enabledSelf)
                    {
                        ChangeSelectedIndex(newIndex, selectedIndex);
                        break;
                    }

                    --newIndex;
                }
            }

            switch (op)
            {
                case KeyboardNavigationOperation.Cancel:
                    Hide(true);
                    return true;
                case KeyboardNavigationOperation.Submit:
                    var item = selectedIndex != -1 ? m_Items[selectedIndex] : null;
                    if (selectedIndex >= 0 && item.element.enabledSelf)
                    {
                        item.action?.Invoke();
                        item.actionUserData?.Invoke(item.element.userData);
                    }

                    Hide(true);
                    return true;
                case KeyboardNavigationOperation.Previous:
                    UpdateSelectionUp(selectedIndex < 0 ? m_Items.Count - 1 : selectedIndex - 1);
                    return true;
                case KeyboardNavigationOperation.Next:
                    UpdateSelectionDown(selectedIndex + 1);
                    return true;
                case KeyboardNavigationOperation.PageUp:
                case KeyboardNavigationOperation.Begin:
                    UpdateSelectionDown(0);
                    return true;
                case KeyboardNavigationOperation.PageDown:
                case KeyboardNavigationOperation.End:
                    UpdateSelectionUp(m_Items.Count - 1);
                    return true;
            }

            return false;
        }

        Vector2 m_MousePosition;

        void OnPointerDown(PointerDownEvent evt)
        {
            m_MousePosition = m_ScrollView.WorldToLocal(evt.position);
            UpdateSelection(evt.elementTarget);

            if (evt.pointerId != PointerId.mousePointerId)
            {
                m_MenuContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }

            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            m_MousePosition = m_ScrollView.WorldToLocal(evt.position);
            UpdateSelection(evt.elementTarget);

            if (evt.pointerId != PointerId.mousePointerId)
            {
                m_MenuContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }

            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            var selectedIndex = GetSelectedIndex();
            if (selectedIndex != -1)
            {
                var item = m_Items[selectedIndex];
                item.action?.Invoke();
                item.actionUserData?.Invoke(item.element.userData);

                if (isSingleSelectionDropdown)
                {
                    Hide(true);
                }
            }

            if (evt.pointerId != PointerId.mousePointerId)
            {
                m_MenuContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }

            evt.StopPropagation();
        }

        void OnFocusOut(FocusOutEvent evt)
        {
            if (!m_ScrollView.ContainsPoint(m_MousePosition))
            {
                Hide();
            }
            else
            {
                // Keep the focus in.
                m_MenuContainer.schedule.Execute(contentContainer.Focus);
            }
        }

        void OnParentResized(GeometryChangedEvent evt)
        {
            if (closeOnParentResize)
            {
                Hide(true);
            }
        }

        void UpdateSelection(VisualElement target)
        {
            if (!m_ScrollView.ContainsPoint(m_MousePosition))
            {
                var selectedIndex = GetSelectedIndex();
                if (selectedIndex >= 0)
                    m_Items[selectedIndex].element.pseudoStates &= ~PseudoStates.Hover;

                return;
            }

            if (target == null)
                return;

            if ((target.pseudoStates & PseudoStates.Hover) != PseudoStates.Hover)
            {
                var selectedIndex = GetSelectedIndex();
                if (selectedIndex >= 0)
                {
                    m_Items[selectedIndex].element.pseudoStates &= ~PseudoStates.Hover;
                }

                target.pseudoStates |= PseudoStates.Hover;
            }
        }

        void ChangeSelectedIndex(int newIndex, int previousIndex)
        {
            if (previousIndex >= 0 && previousIndex < m_Items.Count)
            {
                m_Items[previousIndex].element.pseudoStates &= ~PseudoStates.Hover;
            }

            if (newIndex >= 0 && newIndex < m_Items.Count)
            {
                m_Items[newIndex].element.pseudoStates |= PseudoStates.Hover;
                m_ScrollView.ScrollTo(m_Items[newIndex].element);
            }
        }

        int GetSelectedIndex()
        {
            for (var i = 0; i < m_Items.Count; ++i)
            {
                if ((m_Items[i].element.pseudoStates & PseudoStates.Hover) == PseudoStates.Hover)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Adds an item to this menu using a default VisualElement.
        /// </summary>
        /// <param name="itemName">The text to display to the user.</param>
        /// <param name="isChecked">Indicates whether a checkmark next to the item is displayed.</param>
        /// <param name="action">The callback to invoke when the item is selected by the user.</param>
        public void AddItem(string itemName, bool isChecked, Action action)
        {
            var menuItem = AddItem(itemName, isChecked, true);

            if (menuItem != null)
            {
                menuItem.action = action;
            }
        }

        /// <summary>
        /// Adds an item to this menu using a default VisualElement.
        /// </summary>
        /// <remarks>
        /// This overload of the method accepts an arbitrary object that's passed as a parameter to your callback.
        /// </remarks>
        /// <param name="itemName">The text to display to the user.</param>
        /// <param name="isChecked">Indicates whether a checkmark next to the item is displayed.</param>
        /// <param name="action">The callback to invoke when the item is selected by the user.</param>
        /// <param name="data">The object to pass to the callback as a parameter.</param>
        public void AddItem(string itemName, bool isChecked, Action<object> action, object data)
        {
            var menuItem = AddItem(itemName, isChecked, true, data);

            if (menuItem != null)
            {
                menuItem.actionUserData = action;
            }
        }

        /// <summary>
        /// Adds a disabled item to this menu using a default VisualElement.
        /// </summary>
        /// <remarks>
        /// Items added with this method cannot be selected by the user.
        /// </remarks>
        /// <param name="itemName">The text to display to the user.</param>
        /// <param name="isChecked">Indicates whether a checkmark next to the item is displayed.</param>
        public void AddDisabledItem(string itemName, bool isChecked)
        {
            AddItem(itemName, isChecked, false);
        }

        /// <summary>
        /// Adds a visual separator after the previously added items in this menu.
        /// </summary>
        /// <param name="path">Not used.</param>
        public void AddSeparator(string path)
        {
            // TODO path is not used. This is because IGenericMenu requires it, but this is not great.
            var separator = new VisualElement();
            separator.AddToClassList(separatorUssClassName);
            separator.pickingMode = PickingMode.Ignore;
            m_ScrollView.Add(separator);
        }

        MenuItem AddItem(string itemName, bool isChecked, bool isEnabled, object data = null)
        {
            // Empty item name must count as a separator.
            if (string.IsNullOrEmpty(itemName) || itemName.EndsWith("/"))
            {
                AddSeparator(itemName);
                return null;
            }

            // Ignore if item already exists.
            for (var i = 0; i < m_Items.Count; ++i)
                if (itemName == m_Items[i].name)
                    return null;

            var rowElement = new VisualElement();
            rowElement.AddToClassList(itemUssClassName);
            rowElement.SetEnabled(isEnabled);
            rowElement.userData = data;

            var itemContent = new VisualElement();
            itemContent.AddToClassList(itemContentUssClassName);

            var checkElement = new VisualElement();
            checkElement.AddToClassList(checkmarkUssClassName);
            checkElement.pickingMode = PickingMode.Ignore;
            itemContent.Add(checkElement);

            if (isChecked)
            {
                rowElement.pseudoStates |= PseudoStates.Checked;
            }

            var label = new Label(itemName);
            label.AddToClassList(labelUssClassName);
            label.pickingMode = PickingMode.Ignore;
            itemContent.Add(label);

            rowElement.Add(itemContent);
            m_ScrollView.Add(rowElement);

            MenuItem menuItem = new MenuItem
            {
                name = itemName,
                element = rowElement,
            };
            m_Items.Add(menuItem);

            return menuItem;
        }

        internal void UpdateItem(string itemName, bool isChecked)
        {
            var item = m_Items.Find(x => x.name == itemName);

            if (item == null)
            {
                return;
            }

            if (isChecked)
            {
                item.element.pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                item.element.pseudoStates &= ~PseudoStates.Checked;
            }
        }

        /// <summary>
        /// Displays the menu at the specified position.
        /// </summary>
        /// <remarks>
        /// The parent element that displays the menu:
        /// 
        ///- For Editor UI, the parent element is <see cref="EditorWindow.rootVisualElement"/>.
        ///- For runtime UI, the parent element is <see cref="UIDocument.rootVisualElement"/>.
        /// 
        /// The @@anchored@@ parameter determines the width of the menu. Refer to <see cref="GenericDropdownMenu"/> for example usages.
        /// </remarks>
        /// <param name="position">The position in the coordinate space of the panel.</param>
        /// <param name="targetElement">The element determines which root to use as the menu's parent.</param>
        /// <param name="anchored">If true, the menu's width matches the width of the @@position@@; otherwise, the menu expands to the container's full width.</param>
        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false)
        {
            // TODO the argument should not optional. This is because IGenericMenu requires it, but this is not great.
            if (targetElement == null)
            {
                Debug.LogError("VisualElement Generic Menu needs a target to find a root to attach to.");
                return;
            }

            m_TargetElement = targetElement;
            m_TargetElement.RegisterCallback<DetachFromPanelEvent>(OnTargetElementDetachFromPanel);

            m_PanelRootVisualContainer = m_TargetElement.GetRootVisualContainer();

            if (m_PanelRootVisualContainer == null)
            {
                Debug.LogError("Could not find rootVisualContainer...");
                return;
            }

            m_PanelRootVisualContainer.Add(m_MenuContainer);

            m_MenuContainer.style.left = m_PanelRootVisualContainer.layout.x;
            m_MenuContainer.style.top = m_PanelRootVisualContainer.layout.y;
            m_MenuContainer.style.width = m_PanelRootVisualContainer.layout.width;
            m_MenuContainer.style.height = m_PanelRootVisualContainer.layout.height;
            m_MenuContainer.style.fontSize = m_TargetElement.computedStyle.fontSize;
            m_MenuContainer.style.unityFont = m_TargetElement.computedStyle.unityFont;
            m_MenuContainer.style.unityFontDefinition = m_TargetElement.computedStyle.unityFontDefinition;

            var local = m_PanelRootVisualContainer.WorldToLocal(position);
            m_PositionTop = local.y + position.height - m_PanelRootVisualContainer.layout.y;
            m_PositionLeft = local.x - m_PanelRootVisualContainer.layout.x;

            m_OuterContainer.style.left = m_PositionLeft;
            m_OuterContainer.style.top = m_PositionTop;
            m_OuterContainer.style.maxHeight = Length.None();
            m_OuterContainer.style.maxWidth = Length.None();

            m_DesiredRect = anchored ? position : Rect.zero;

            m_MenuContainer.schedule.Execute(contentContainer.Focus);
            m_ShownAboveTarget = false;

            EnsureVisibilityInParent();

            if (targetElement != null)
                targetElement.pseudoStates |= PseudoStates.Active;
        }

        /// <summary>
        /// Displays the menu at the specified position.
        /// </summary>
        /// <remarks>
        /// The parent element that displays the menu:
        /// 
        ///- For Editor UI, the parent element is <see cref="EditorWindow.rootVisualElement"/>.
        ///- For runtime UI, the parent element is <see cref="UIDocument.rootVisualElement"/>.
        /// 
        /// The @@anchored@@ and @@fitContentWidthIfAnchored@@ parameters determine the width of the menu. Refer to <see cref="GenericDropdownMenu"/> for example usages.
        /// 
        /// </remarks>
        /// <param name="position">The position in the coordinate space of the panel.</param>
        /// <param name="targetElement">The element determines which root to use as the menu's parent.</param>
        /// <param name="anchored">If true, the menu's width matches the width of the @@position@@; otherwise, the menu expands 
        /// to the container's full width.</param>
        /// <param name="fitContentWidthIfAnchored">If true and the menu is anchored, the menu's width matches its content's width; 
        /// otherwise, the menu's width matches the width of the @@position@@. If the menu is unanchored, this parameter is ignored.</param>
        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false, bool fitContentWidthIfAnchored = false)
        {
            m_FitContentWidth = anchored && fitContentWidthIfAnchored;
            m_OuterContainer.EnableInClassList(contentWidthUssClassName, m_FitContentWidth);
            DropDown(position, targetElement, anchored);
        }

        private void OnTargetElementDetachFromPanel(DetachFromPanelEvent evt)
        {
            Hide();
        }

        void OnContainerGeometryChanged(GeometryChangedEvent evt)
        {
            EnsureVisibilityInParent();
        }

        void OnInitialDisplay(GeometryChangedEvent evt)
        {
            m_ContentWidth = GetLargestItemWidth() + k_MenuItemPadding;
        }

        void EnsureVisibilityInParent()
        {
            if (m_PanelRootVisualContainer != null && !float.IsNaN(m_OuterContainer.layout.width) && !float.IsNaN(m_OuterContainer.layout.height))
            {
                if (m_DesiredRect == Rect.zero)
                {
                    var posX = Math.Max(0, Mathf.Min(m_PositionLeft, m_PanelRootVisualContainer.layout.width - m_OuterContainer.layout.width));
                    var posY = Mathf.Min(m_PositionTop, Mathf.Max(0, m_PanelRootVisualContainer.layout.height - m_OuterContainer.layout.height));

                    m_OuterContainer.style.left = posX;
                    m_OuterContainer.style.top = posY;
                }
                else
                {
                    var dropdownWidth = m_ContentWidth;
                    if (m_ScrollView.isVerticalScrollDisplayed)
                    {
                        dropdownWidth += Mathf.Ceil(m_ScrollView.verticalScroller.computedStyle.width.value);
                    }

                    dropdownWidth = m_FitContentWidth ? dropdownWidth : m_DesiredRect.width;

                    m_OuterContainer.style.width = dropdownWidth;

                    // Ensure width is visible
                    var spaceToTheRight = m_PanelRootVisualContainer.layout.width - m_PositionLeft;

                    if (spaceToTheRight <= dropdownWidth)
                    {
                        m_PositionLeft -= dropdownWidth - spaceToTheRight + k_MenuPadding;
                    }

                    m_PositionLeft = Math.Max(m_PositionLeft, 0);
                    if (m_PositionLeft == 0)
                    {
                        m_OuterContainer.style.maxWidth = Math.Min(m_PanelRootVisualContainer.layout.width, dropdownWidth);
                    }
                    m_OuterContainer.style.left = m_PositionLeft;
                }

                // Ensure height is visible
                var targetElement = m_MenuContainer.WorldToLocal(m_TargetElement.worldBound);
                var itemHeight = m_Items[0].element.layout.height + k_MenuItemPadding;

                var dropdownHeight = m_OuterContainer.layout.height;
                var targetElementTop = targetElement.y;
                var actualTop = m_OuterContainer.worldBound.y;
                var spaceBelow = m_ShownAboveTarget ? targetElementTop - actualTop : m_PanelRootVisualContainer.worldBound.height - actualTop;
                var spaceAbove = m_ShownAboveTarget ? m_PanelRootVisualContainer.worldBound.height - actualTop : targetElementTop;

                var adjustTop = spaceBelow < dropdownHeight;

                if (adjustTop && spaceAbove > spaceBelow)
                {
                    m_PositionTop = targetElementTop - dropdownHeight;
                    m_PositionTop = Math.Max(m_PositionTop, 0);
                    m_OuterContainer.style.maxHeight = m_PositionTop == 0 ? Math.Max(targetElementTop, itemHeight) : Length.None();
                    m_OuterContainer.style.top = m_PositionTop;
                    m_ShownAboveTarget = true;
                }
                else if (adjustTop)
                {
                    // space below is greater, we'll just set a height and let the dropdown expand to the top
                    if (spaceBelow < itemHeight)
                    {
                        m_OuterContainer.style.maxHeight = itemHeight;
                        m_PositionTop = m_PanelRootVisualContainer.worldBound.height - itemHeight;
                    }
                    else
                    {
                        m_OuterContainer.style.maxHeight = spaceBelow;
                    }
                    m_OuterContainer.style.top = m_PositionTop;
                }
            }
        }

        float GetLargestItemWidth()
        {
            var largestWidth = 0.0f;
            if (m_Items.Count == 0 && m_ScrollView.contentContainer.childCount > 0)
            {
                // If the items are not added directly to the menu, we need to find the largest width in the children of the content container.
                var menuItems = ListPool<MenuItem>.Get();
                foreach (var element in m_ScrollView.contentContainer.Children())
                {
                    menuItems.Add(new MenuItem()
                    {
                       element = element
                    });
                }
                m_Items.AddRange(menuItems);
                ListPool<MenuItem>.Release(menuItems);
            }
            foreach (var item in m_Items)
            {
                largestWidth = Math.Max(largestWidth, item.element.layout.width);
            }

            return largestWidth;
        }
    }
}
