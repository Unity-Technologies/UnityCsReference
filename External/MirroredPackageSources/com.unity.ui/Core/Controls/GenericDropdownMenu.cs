using System;
using System.Collections.Generic;

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
    public class GenericDropdownMenu : IGenericMenu
    {
        class MenuItem
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

        List<MenuItem> m_Items = new List<MenuItem>();
        VisualElement m_MenuContainer;
        VisualElement m_OuterContainer;
        ScrollView m_ScrollView;
        VisualElement m_PanelRootVisualContainer;
        VisualElement m_TargetElement;
        Rect m_DesiredRect;
        KeyboardNavigationManipulator m_NavigationManipulator;

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
            m_OuterContainer.hierarchy.Add(m_ScrollView);

            m_MenuContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_MenuContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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
            m_ScrollView.RegisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);
            m_ScrollView.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            m_MenuContainer.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_MenuContainer.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            contentContainer.RemoveManipulator(m_NavigationManipulator);
            m_MenuContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_MenuContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            m_MenuContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);

            evt.originPanel.visualTree.UnregisterCallback<GeometryChangedEvent>(OnParentResized);
            m_ScrollView.UnregisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);
            m_ScrollView.UnregisterCallback<FocusOutEvent>(OnFocusOut);
        }

        void Hide()
        {
            m_MenuContainer.RemoveFromHierarchy();

            if (m_TargetElement != null)
                m_TargetElement.pseudoStates ^= PseudoStates.Active;
            m_TargetElement = null;
        }

        void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            if (Apply(op))
            {
                sourceEvent.StopPropagation();
                sourceEvent.PreventDefault();
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
                    Hide();
                    return true;
                case KeyboardNavigationOperation.Submit:
                    var item = m_Items[selectedIndex];
                    if (selectedIndex >= 0 && item.element.enabledSelf)
                    {
                        item.action?.Invoke();
                        item.actionUserData?.Invoke(item.element.userData);
                    }

                    Hide();
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
            UpdateSelection(evt.target as VisualElement);

            if (evt.pointerId != PointerId.mousePointerId)
            {
                m_MenuContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }

            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            m_MousePosition = m_ScrollView.WorldToLocal(evt.position);
            UpdateSelection(evt.target as VisualElement);

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

                Hide();
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
            Hide();
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

            if (isChecked)
            {
                var checkElement = new VisualElement();
                checkElement.AddToClassList(checkmarkUssClassName);
                checkElement.pickingMode = PickingMode.Ignore;
                rowElement.Add(checkElement);

                rowElement.pseudoStates |= PseudoStates.Checked;
            }

            var label = new Label(itemName);
            label.AddToClassList(labelUssClassName);
            label.pickingMode = PickingMode.Ignore;
            rowElement.Add(label);

            m_ScrollView.Add(rowElement);

            MenuItem menuItem = new MenuItem
            {
                name = itemName,
                element = rowElement,
            };
            m_Items.Add(menuItem);

            return menuItem;
        }

        /// <summary>
        /// Displays the menu at the specified position.
        /// </summary>
        /// <remarks>
        /// This method automatically finds the parent VisualElement that displays the menu.
        /// For editor UI, <see cref="EditorWindow.rootVisualElement"/> is used as the parent.
        /// For runtime UI,<see cref="UIDocument.rootVisualElement"/> is used as the parent.
        /// </remarks>
        /// <param name="position">The position in the coordinate space of the panel.</param>
        /// <param name="targetElement">The element used to determine in which root to parent the menu.</param>
        /// <param name="anchored">Whether the menu should use the width of the position argument instead of its normal width.</param>
        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false)
        {
            // TODO the argument should not optional. This is because IGenericMenu requires it, but this is not great.
            if (targetElement == null)
            {
                Debug.LogError("VisualElement Generic Menu needs a target to find a root to attach to.");
                return;
            }

            m_TargetElement = targetElement;
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

            var local = m_PanelRootVisualContainer.WorldToLocal(position);
            m_OuterContainer.style.left = local.x - m_PanelRootVisualContainer.layout.x;
            m_OuterContainer.style.top = local.y + position.height - m_PanelRootVisualContainer.layout.y;

            m_DesiredRect = anchored ? position : Rect.zero;

            m_MenuContainer.schedule.Execute(contentContainer.Focus);
            EnsureVisibilityInParent();

            if (targetElement != null)
                targetElement.pseudoStates |= PseudoStates.Active;
        }

        void OnContainerGeometryChanged(GeometryChangedEvent evt)
        {
            EnsureVisibilityInParent();
        }

        void EnsureVisibilityInParent()
        {
            if (m_PanelRootVisualContainer != null && !float.IsNaN(m_OuterContainer.layout.width) && !float.IsNaN(m_OuterContainer.layout.height))
            {
                if (m_DesiredRect == Rect.zero)
                {
                    var posX = Mathf.Min(m_OuterContainer.layout.x, m_PanelRootVisualContainer.layout.width - m_OuterContainer.layout.width);
                    var posY = Mathf.Min(m_OuterContainer.layout.y, Mathf.Max(0, m_PanelRootVisualContainer.layout.height - m_OuterContainer.layout.height));

                    m_OuterContainer.style.left = posX;
                    m_OuterContainer.style.top = posY;
                }

                m_OuterContainer.style.height = Mathf.Min(
                    m_MenuContainer.layout.height - m_MenuContainer.layout.y - m_OuterContainer.layout.y,
                    m_ScrollView.layout.height + m_OuterContainer.resolvedStyle.borderBottomWidth + m_OuterContainer.resolvedStyle.borderTopWidth);

                if (m_DesiredRect != Rect.zero)
                {
                    m_OuterContainer.style.width = m_DesiredRect.width;
                }
            }
        }
    }
}
