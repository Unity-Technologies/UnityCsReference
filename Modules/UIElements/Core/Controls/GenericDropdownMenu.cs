// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    /// Provides methods to display contextual menus with default textual options, <see cref="VisualElement"/>, or a combination of both.
    /// </summary>
    public class GenericDropdownMenu : IGenericMenu
    {
        internal class MenuItem
        {
            public string name;
            public VisualElement element;
            public Action action;
            public Action<object> actionUserData;
            public bool isCustomContent;

            public MenuItem parent;
            public List<MenuItem> children = new();
            public List<MenuItem> headerActions = new();

            // Accelerates child search
            Dictionary<int, MenuItem> childrenDictionary = new();

            public bool isSubmenu => children.Count > 0;
            public bool isSeparator => string.IsNullOrEmpty(name) || name[^1] == '/';
            public bool isActionValid => action != null || actionUserData != null;

            public void PerformAction()
            {
                if (actionUserData != null)
                    actionUserData.Invoke(element.userData);
                else
                    action?.Invoke();
            }

            public void AddChild(MenuItem item)
            {
                children.Add(item);

                if (string.IsNullOrEmpty(item.name) || item.name[^1] == '/')
                    return;

                childrenDictionary.Add(item.name.GetHashCode(), item);
            }

            public bool HasChild(string name) => childrenDictionary.ContainsKey(name.GetHashCode());

            public MenuItem GetChild(string name)
            {
                childrenDictionary.TryGetValue(name.GetHashCode(), out var child);
                return child;
            }
        }

        const string hiddenClassName = "unity-hidden";

        /// <summary>
        /// The USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-base-dropdown";
        /// <summary>
        /// The USS class name of items in elements of this type.
        /// </summary>
        public static readonly string itemUssClassName = ussClassName + "__item";
        /// <summary>
        /// The USS class name of clicked items in elements of this type.
        /// </summary>
        public static readonly string clickUssClassName = ussClassName + "__click";
        /// <summary>
        /// The USS class name of labels in elements of this type.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// The USS class name of inner containers in elements of this type.
        /// </summary>
        public static readonly string containerInnerUssClassName = ussClassName + "__container-inner";
        /// <summary>
        /// The USS class name of outer containers in elements of this type.
        /// </summary>
        public static readonly string containerOuterUssClassName = ussClassName + "__container-outer";
        /// <summary>
        /// The USS class name of separators in elements of this type.
        /// </summary>
        public static readonly string appendixUssClassName = ussClassName + "__appendix";
        /// <summary>
        /// The USS class name of checkmarks in elements of this type.
        /// </summary>
        public static readonly string checkmarkUssClassName = ussClassName + "__checkmark";
        /// <summary>
        /// The USS class name of separators in elements of this type.
        /// </summary>
        public static readonly string separatorUssClassName = ussClassName + "__separator";

        internal static readonly string descendantUssClassName = ussClassName + "__descendant";
        internal static readonly string latentUssClassName = ussClassName + "__latent";
        internal static readonly string iconUssClassName = ussClassName + "__icon";
        internal static readonly string submenuUssClassName = ussClassName + "__submenu";
        internal static readonly string titleAreaUssClassName = ussClassName + "__title-area";
        internal static readonly string titleAreaWithBackUssClassName = ussClassName + "__title-area-back";
        internal static readonly string backUssClassName = ussClassName + "__back";
        internal static readonly string titleUssClassName = ussClassName + "__title";
        internal static readonly string titleButtonContainerUssClassName = ussClassName + "__title-button-container";

        // Amount of context menus that we aim to pool
        internal const int k_OptimizedMenus = 5;

        // Amount of objects that we aim to pool
        internal const int k_OptimizedElements = 100 * k_OptimizedMenus;

        MenuItem m_Root = new();
        MenuItem m_Current;

        VisualElement m_MenuContainer;
        VisualElement m_TitleBar;
        VisualElement m_Back;
        VisualElement m_HeaderButtons;
        VisualElement m_HeaderSeparator;
        Label m_Title;
        VisualElement m_OuterContainer;
        ListView m_ListView;
        VisualElement m_PanelRootVisualContainer;
        VisualElement m_TargetElement;
        KeyboardNavigationManipulator m_NavigationManipulator;
        bool m_AllowSubmenus;
        Rect m_DesiredRect;
        internal int m_PreviousIndex = -1;

        internal GenericDropdownMenu m_Parent;
        internal GenericDropdownMenu m_Child;

        internal Action<MenuItem, GenericDropdownMenu> m_SubmenuOverride;
        internal Action<bool, bool> m_OnBeforePerformAction;
        internal Action m_OnBack;

        internal event Action onRebuild;
        internal event Action<char, KeyCode, EventModifiers> onKey;
        internal event Action<KeyboardNavigationOperation> onKeyboardNavigationOperation;

        static bool s_ScheduleFocusFirstItem = false;

        static internal Func<Texture2D, string, bool, bool, VisualElement> CreateHeaderItem;
        static internal Action<VisualElement> SetupHeaderStrip;

        // Used by UI Toolkit Debugger
        internal static bool s_Picking = false;
        // UI Toolkit Debugger picker is unchecked on pointer down while we need to remember its state in pointer up event for Windows
        static bool s_wasPicking = false;

        // Used for tests or by Editor
        internal VisualElement menuContainer => m_MenuContainer;
        internal VisualElement outerContainer => m_OuterContainer;
        internal ScrollView scrollView => m_ListView.scrollView;
        internal MenuItem root => m_Root;
        internal List<MenuItem> items => root.children;
        internal MenuItem current => m_Current;
        internal bool allowBackButton { get; set; }
        internal bool autoClose { get; set; }
        internal bool customFocusHandling { get; set; }
        internal Label title => m_Title;

        static readonly ObjectPool<ListView> s_ListPool = new (() =>
        {
            var listView = new ListView()
            {
                pickingMode = PickingMode.Position,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                focusable = true,
                selectionType = SelectionType.None,
                classList = { containerInnerUssClassName },
                makeItem = () => new VisualElement() { focusable = false, pickingMode = PickingMode.Ignore },
                unbindItem = (v, i) => v.Clear(),
                style =
                {
                    flexGrow = 1,
                }
            };
            listView.bindItem = (v, i) => v.Add((listView.itemsSource as List<MenuItem>)?[i].element);
            listView.scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            listView.scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            listView.itemsSourceChanged += () =>
            {
                // We don't allow separators at the top or bottom
                if (listView.itemsSource is List<MenuItem> items)
                {
                    while (items.Count > 0 && items[0].isSeparator)
                        items.RemoveAt(0);

                    while (items.Count > 0 && items[^1].isSeparator)
                        items.Remove(items[^1]);
                }
            };
            return listView;
        }, k_OptimizedMenus);
        static readonly ObjectPool<VisualElement> s_ItemPool = new(() =>
        {
            // Since item element callback lists are cleared we cannot declare
            // any callbacks in the constructor code as it will get deleted
            // upon release and won't work upon reuse.
            var item = new VisualElement();
            item.AddToClassList(itemUssClassName);

            var leftAppendix = new VisualElement();
            leftAppendix.pickingMode = PickingMode.Ignore;
            item.Add(leftAppendix);

            var label = new Label();
            label.AddToClassList(labelUssClassName);
            label.pickingMode = PickingMode.Ignore;
            item.Add(label);

            var rightAppendix = new VisualElement();
            rightAppendix.pickingMode = PickingMode.Ignore;
            item.Add(rightAppendix);

            return item;
        }, k_OptimizedElements);

        internal void ExtendItem(Action<MenuItem> action) => ExtendItemRecursive(root, action);

        void ExtendItemRecursive(MenuItem submenu, Action<MenuItem> action)
        {
            action.Invoke(submenu);

            foreach (var child in submenu.children)
            {
                ExtendItemRecursive(child, action);
            }
        }

        internal ListView innerContainer => m_ListView;

        /// <summary>
        /// Returns the content container for the <see cref="GenericDropdownMenu"/>.
        /// </summary>
        /// <remarks>
        /// Allows users to create their own dropdown menu if they don't want to use the default implementation.
        /// </remarks>
        [Obsolete($"Use '{nameof(AddItem)}' method to add custom content.", false)]
        public VisualElement contentContainer => m_ListView.contentContainer;

        /// <summary>
        ///  Initializes and returns an instance of GenericDropdownMenu.
        /// </summary>
        public GenericDropdownMenu() : this(false) { }

        internal GenericDropdownMenu(bool allowSubmenus)
        {
            m_AllowSubmenus = allowSubmenus;

            m_MenuContainer = new VisualElement() { classList = { ussClassName } };

            m_OuterContainer = new VisualElement() { classList = { containerOuterUssClassName }};
            m_MenuContainer.Add(m_OuterContainer);

            m_TitleBar = new VisualElement() { classList = { titleAreaUssClassName, hiddenClassName } };
            m_TitleBar.RegisterCallback<PointerUpEvent>(e =>
            {
                NavigateBack();
                e.StopPropagation();
            });
            m_OuterContainer.hierarchy.Add(m_TitleBar);

            m_HeaderSeparator = CreateSeparator();
            m_OuterContainer.hierarchy.Add(m_HeaderSeparator);

            m_Back = new VisualElement() { classList = { backUssClassName, hiddenClassName } };
            m_TitleBar.hierarchy.Add(m_Back);

            m_Title = new Label() { classList = { titleUssClassName } };
            m_TitleBar.hierarchy.Add(m_Title);

            m_HeaderButtons = new VisualElement() { classList = { titleButtonContainerUssClassName } };
            m_TitleBar.Add(m_HeaderButtons);

            m_ListView = s_ListPool.Get();
            m_OuterContainer.hierarchy.Add(m_ListView);

            m_MenuContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_MenuContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            autoClose = true;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            m_ListView.Focus();
            m_ListView.AddManipulator(m_NavigationManipulator = new KeyboardNavigationManipulator(Apply));
            m_MenuContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            m_MenuContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            m_MenuContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
            m_MenuContainer.RegisterCallback<KeyDownEvent>(OnKeyDown);

            evt.destinationPanel.visualTree.RegisterCallback<GeometryChangedEvent>(OnParentResized);
            m_ListView.RegisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);
            m_ListView.RegisterCallback<FocusOutEvent>(OnFocusOut);
            m_ListView.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            m_Current = m_Root;
            Rebuild();

            if (s_ScheduleFocusFirstItem)
                m_ListView.schedule.Execute(() => Apply(KeyboardNavigationOperation.Next));

            s_ScheduleFocusFirstItem = false;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            m_ListView.RemoveManipulator(m_NavigationManipulator);
            m_MenuContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_MenuContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            m_MenuContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);

            evt.originPanel.visualTree.UnregisterCallback<GeometryChangedEvent>(OnParentResized);
            m_ListView.UnregisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);
            m_ListView.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            m_ListView.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);

            m_ListView.style.maxWidth = StyleKeyword.Null;
            m_ListView.itemsSource = Array.Empty<MenuItem>();
            m_ListView.Rebuild();
            m_ListView.EnableInClassList(latentUssClassName, false);
            m_ListView.EnableInClassList(clickUssClassName, false);
            m_ListView.RemoveFromHierarchy();
            s_ListPool.Release(m_ListView);

            if (m_Current.parent != null)
                return;

            ReleasePooledItems(m_Root);
        }

        void ReleasePooledItems(MenuItem root)
        {
            foreach (var child in root.children)
                ReleasePooledItems(child);

            if (root.element == null || !root.element.ClassListContains(itemUssClassName))
                return;

            root.element.pseudoStates = 0;

            foreach (var child in root.element.Children())
            {
                if (child.ClassListContains(appendixUssClassName))
                {
                    child.ClearClassList();
                    child.style.backgroundImage = StyleKeyword.Null;
                    child.Clear();
                }
                else if(child.ClassListContains(labelUssClassName))
                {
                    if (child is Label label)
                        label.text = string.Empty;
                }
            }

            root.element.m_CallbackRegistry.m_BubbleUpCallbacks.GetCallbackListForWriting().Clear();
            root.element.m_CallbackRegistry.m_TrickleDownCallbacks.GetCallbackListForWriting().Clear();
            s_ItemPool.Release(root.element);
        }

        internal void Hide(bool giveFocusBack = false, bool hideParent = true)
        {
            m_MenuContainer.RemoveFromHierarchy();

            if (hideParent)
                m_Parent?.Hide(giveFocusBack);

            if (m_TargetElement != null)
            {
                m_TargetElement.UnregisterCallback<DetachFromPanelEvent>(OnTargetElementDetachFromPanel);
                m_TargetElement.pseudoStates ^= PseudoStates.Active;
                if (giveFocusBack)
                    m_TargetElement.Focus();
            }

            m_TargetElement = null;
        }

        internal void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            if (Apply(op))
                sourceEvent.StopPropagation();
        }

        internal bool Apply(KeyboardNavigationOperation op)
        {
            var selectedIndex = GetSelectedIndex();
            MenuItem item;

            void UpdateSelectionDown(int newIndex)
            {
                if (newIndex >= m_Current.children.Count)
                    newIndex = 0;

                // If all menu items disabled without iteration limit we would enter an endless loop
                for (int i = 0; newIndex < m_Current.children.Count && i < m_Current.children.Count; i++)
                {
                    var item = m_Current.children[newIndex];

                    if (item.element.enabledSelf && !item.isSeparator && item.isActionValid)
                    {
                        ChangeSelectedIndex(newIndex);
                        break;
                    }

                    ++newIndex;

                    if (newIndex >= m_Current.children.Count)
                        newIndex = 0;
                }
            }

            void UpdateSelectionUp(int newIndex)
            {
                if (newIndex < 0)
                    newIndex = m_Current.children.Count - 1;

                // If all menu items disabled without iteration limit we would enter an endless loop
                for (int i = 0; newIndex >= 0 && i < m_Current.children.Count; i++)
                {
                    var item = m_Current.children[newIndex];

                    if (item.element.enabledSelf && !item.isSeparator && item.isActionValid)
                    {
                        ChangeSelectedIndex(newIndex);
                        break;
                    }

                    --newIndex;

                    if (newIndex < 0)
                        newIndex = m_Current.children.Count - 1;
                }
            }

            var result = false;

            switch (op)
            {
                case KeyboardNavigationOperation.MoveLeft:
                    if (m_Parent == null)
                        return true;

                    FirstOrDefault(m_Current.children)?.parent?.element?.Focus();

                    if (allowBackButton)
                        NavigateBack(true);
                    else
                    {
                        m_Parent.m_Child = null;
                        Hide(true, false);
                    }

                    m_OnBack?.Invoke();
                    result = true;
                    break;

                case KeyboardNavigationOperation.Cancel:
                    FirstOrDefault(m_Current.children)?.parent?.element?.Focus();

                    if (allowBackButton)
                        NavigateBack(true);
                    else
                    {
                        if(m_Parent != null)
                            m_Parent.m_Child = null;

                        Hide(true, false);
                    }

                    m_OnBack?.Invoke();
                    result = true;
                    break;

                case KeyboardNavigationOperation.Submit:
                    if (selectedIndex < 0)
                        return false;

                    item = m_Current.children[selectedIndex];
                    if (selectedIndex >= 0 && item.element.enabledSelf)
                    {
                        m_OnBeforePerformAction?.Invoke(item.isSubmenu, autoClose);
                        item.PerformAction();
                    }

                    if (!item.isSubmenu)
                        Hide(true);

                    result = true;
                    break;
                case KeyboardNavigationOperation.Previous:
                    UpdateSelectionUp(selectedIndex < 0 ? m_Current.children.Count - 1 : selectedIndex - 1);
                    result = true;
                    break;
                case KeyboardNavigationOperation.Next:
                    UpdateSelectionDown(selectedIndex + 1);
                    result = true;
                    break;
                case KeyboardNavigationOperation.PageUp:
                case KeyboardNavigationOperation.Begin:
                    UpdateSelectionDown(0);
                    result = true;
                    break;
                case KeyboardNavigationOperation.PageDown:
                case KeyboardNavigationOperation.End:
                    UpdateSelectionUp(m_Current.children.Count - 1);
                    result = true;
                    break;
                case KeyboardNavigationOperation.MoveRight:
                    if (selectedIndex < 0)
                        return false;

                    item = m_Current.children[selectedIndex];
                    s_ScheduleFocusFirstItem = true;

                    if (item.isSubmenu)
                    {
                        m_OnBeforePerformAction?.Invoke(item.isSubmenu, autoClose);
                        item.PerformAction();
                    }

                    result = true;
                    break;
            }

            if (result == true)
                onKeyboardNavigationOperation?.Invoke(op);

            return result;
        }

        void ClickItem()
        {
            var selectedIndex = GetSelectedIndex();
            if (selectedIndex != -1)
            {
                var item = m_Current.children[selectedIndex];

                if (item.element.enabledSelf && !s_wasPicking)
                {
                    m_OnBeforePerformAction?.Invoke(item.isSubmenu, autoClose);
                    item.PerformAction();

                    if(item.children.Count < 1)
                    {
                        if (autoClose)
                            Hide(true);
                        else
                            NavigateTo(current);
                    }
                }
            }

            s_wasPicking = false;
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            UpdateSelection(evt.elementTarget);

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX && Application.isEditor)
                ClickItem();

            if (evt.pointerId != PointerId.mousePointerId)
            {
                m_MenuContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }

            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            UpdateSelection(evt.elementTarget);

            if (evt.pointerId != PointerId.mousePointerId)
            {
                m_MenuContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }

            s_wasPicking |= s_Picking;
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button != 0)
                return;

            if (SystemInfo.operatingSystemFamily != OperatingSystemFamily.MacOSX || !Application.isEditor)
                ClickItem();

            if (evt.pointerId != PointerId.mousePointerId)
            {
                m_MenuContainer.panel.PreventCompatibilityMouseEvents(evt.pointerId);
            }

            evt.StopPropagation();
        }

        void OnPointerLeave(PointerLeaveEvent evt)
        {
            m_ListView.EnableInClassList(clickUssClassName, false);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            onKey?.Invoke(evt.character, evt.keyCode, evt.modifiers);
        }

        void OnFocusOut(FocusOutEvent evt)
        {
            if (customFocusHandling)
                return;

            Hide();
        }

        void OnParentResized(GeometryChangedEvent evt)
        {
            var isPlayer = menuContainer.elementPanel?.contextType == ContextType.Player;

            if (!isPlayer)
                return;

            Hide(true);
        }

        void UpdateSelection(VisualElement target)
        {
            if (target == null)
                return;

            ChangeSelectedIndex(m_Current.children.FindIndex(c => c.element == target));

            m_Parent?.ChangeSelectedIndex(m_Parent.m_Current.children.IndexOf(root.parent));
        }

        void ChangeSelectedIndex(int newIndex)
        {
            if (m_PreviousIndex >= 0 && m_PreviousIndex < m_Current.children.Count)
            {
                m_Current.children[m_PreviousIndex].element.pseudoStates &= ~PseudoStates.Hover;
            }

            m_PreviousIndex = newIndex;

            if (newIndex >= 0 && newIndex < m_Current.children.Count)
            {
                m_Current.children[newIndex].element.pseudoStates |= PseudoStates.Hover;
                m_ListView.ScrollTo(m_Current.children[newIndex].element.parent);
            }

            m_Parent?.ChangeSelectedIndex(m_Parent.m_Current.children.IndexOf(root.parent));
        }

        int GetSelectedIndex()
        {
            int index = 0;
            foreach (var item in m_Current.children)
            {
                if ((item.element.pseudoStates & PseudoStates.Hover) == PseudoStates.Hover)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Adds an item to this menu using a default <see cref="VisualElement"/>.
        /// </summary>
        /// <param name="itemName">The text to display to the user.</param>
        /// <param name="isChecked">Whether to display a checkmark next to the item.</param>
        /// <param name="action">The callback to invoke when the item is selected by the user.</param>
        public void AddItem(string itemName, bool isChecked, Action action)
        {
            AddItem(itemName, isChecked, action, null, string.Empty);
        }

        internal void AddItem(string itemName, bool isChecked, Action action, Texture2D icon, string tooltip)
        {
            AddItem(itemName, isChecked, true, action, null, null, icon, tooltip);
        }

        /// <summary>
        /// Adds an item to this menu using a default VisualElement.
        /// </summary>
        /// <remarks>
        /// This overload of the method accepts an arbitrary object that's passed as a parameter to your callback.
        /// </remarks>
        /// <param name="itemName">The text to display to the user.</param>
        /// <param name="isChecked">Whether to display a checkmark next to the item.</param>
        /// <param name="action">The callback to invoke when the item is selected by the user.</param>
        /// <param name="data">The object to pass to the callback as a parameter.</param>
        public void AddItem(string itemName, bool isChecked, Action<object> action, object data)
        {
            AddItem(itemName, isChecked, action, data, null, string.Empty);
        }

        internal void AddItem(string itemName, bool isChecked, Action<object> action, object data, Texture2D icon, string tooltip)
        {
            AddItem(itemName, isChecked, true, null, action, data, icon, tooltip);
        }

        /// <summary>
        /// Adds an item to this menu using a custom <see cref="VisualElement"/>.
        /// </summary>
        /// <param name="itemName">The text that identifies this visual element.</param>
        /// <param name="content">Custom menu item visual element.</param>
        public void AddItem(string itemName, VisualElement content)
        {
            AddItem(itemName, content, true);
        }

        /// <summary>
        /// Adds a disabled item to this menu using a default <see cref="VisualElement"/>.
        /// </summary>
        /// <remarks>
        /// Items added with this method cannot be selected by the user.
        /// </remarks>
        /// <param name="itemName">The text that identifies this visual element.</param>
        /// <param name="isChecked">Whether to display a checkmark next to the item.</param>
        public void AddDisabledItem(string itemName, bool isChecked)
        {
            AddDisabledItem(itemName, isChecked, null, string.Empty);
        }

        internal void AddDisabledItem(string itemName, bool isChecked, Texture2D icon, string tooltip)
        {
            AddItem(itemName, isChecked, false, null, null, null, icon, tooltip);
        }

        internal void AddDisabledItem(string itemName, VisualElement content)
        {
            AddItem(itemName, content, false);
        }

        internal void AddHeaderItem(Texture2D icon, string tooltip, bool isChecked, Action action)
        {
            AddHeaderItem(icon, tooltip, true, isChecked, action);
        }

        internal void AddHeaderItem(Texture2D icon, string tooltip, bool isChecked, Action<object> action, object data)
        {
            AddHeaderItem(icon, tooltip, true, isChecked, null, action, data);
        }

        internal void AddDisabledHeaderItem(Texture2D icon, string tooltip, bool isChecked)
        {
            AddHeaderItem(icon, tooltip, false, isChecked);
        }

        /// <summary>
        /// Adds a visual separator after the previously added items in this menu.
        /// </summary>
        /// <param name="path">Path to submenu where the separator is added.</param>
        public void AddSeparator(string path)
        {
            AddItem(path, false, true);
        }

        internal static VisualElement CreateSeparator() => new()
        {
            pickingMode = PickingMode.Ignore,
            classList = { separatorUssClassName }
        };

        internal MenuItem AddItem(string itemName, bool isChecked, bool isEnabled, Action action1 = null, Action<object> action2 = null, object data = null, Texture2D icon = null, string tooltip = "")
        {
            var parent = GetOrCreateParents(ref itemName, icon);
            var menuItem = new MenuItem
            {
                name = itemName ?? string.Empty,
                parent = parent,
                action = action1,
                actionUserData = action2,
            };

            // Empty item name must count as a separator. Also don't allow to put two separators next to each other
            if ((string.IsNullOrWhiteSpace(itemName) || itemName[^1] == '/')
                && parent.children.Count > 0 && !(parent.children[^1]?.isSeparator ?? true))
                menuItem.element = CreateSeparator();
            else
            {
                if (parent.HasChild(itemName))
                    return null;

                menuItem.element = BuildItem(itemName, isChecked, isEnabled, false, data, icon, tooltip);
            }

            parent.AddChild(menuItem);
            return menuItem;
        }

        internal MenuItem AddItem(string itemName, VisualElement content, bool isEnabled)
        {
            var parent = GetOrCreateParents(ref itemName);
            content.SetEnabled(isEnabled);

            var menuItem = new MenuItem
            {
                name = itemName,
                parent = parent,
                element = content,
                isCustomContent = true
            };
            parent.children.Add(menuItem);
            return menuItem;
        }

        internal void AddHeaderItem(Texture2D icon, string tooltip, bool isEnabled, bool isChecked, Action action1 = null, Action<object> action2 = null, object data = null)
        {
            var headerItem = new MenuItem
            {
                name = string.Empty,
                parent = null,
                action = action1,
                actionUserData = action2,
                element = CreateHeaderItem?.Invoke(icon, tooltip, isEnabled, isChecked)
            };

            headerItem.element.userData = data;

            headerItem.element.RegisterCallback<ClickEvent>(e =>
            {
                m_OnBeforePerformAction?.Invoke(false, autoClose);
                headerItem.PerformAction();
                Hide(true);
            });

            root.headerActions.Add(headerItem);
        }

        internal void AddItem(MenuItem item)
        {
            m_Root.AddChild(item);
        }

        internal void AddHeaderItem(MenuItem item)
        {
            m_Root.headerActions.Add(item);
        }

        MenuItem GetOrCreateParents(ref string path, Texture2D icon = null, bool canCreateParent = true)
        {
            var item = m_Root;

            if (path == null)
                return item;

            var slashIndex = path.IndexOf('/', StringComparison.Ordinal);

            Texture2D elementIcon = null;
            while (m_AllowSubmenus && slashIndex > 0)
            {
                if (slashIndex == path.Length - 1)
                    elementIcon = icon;

                var childName = path[..slashIndex];
                var childItem = item.GetChild(childName);

                if (childItem == null)
                {
                    if (!canCreateParent)
                        return null;

                    childItem = new MenuItem
                    {
                        name = childName,
                        parent = item,
                        element = BuildItem(childName, false, true, true, null, elementIcon, string.Empty),
                    };
                    childItem.action = () =>
                    {
                        if (m_SubmenuOverride != null)
                            m_SubmenuOverride.Invoke(childItem, this);
                        else
                            NavigateTo(childItem);
                    };

                    item.AddChild(childItem);
                }

                item = childItem;
                path = path[(slashIndex + 1)..];
                slashIndex = path.IndexOf('/', StringComparison.Ordinal);
            }

            return item;
        }

        VisualElement BuildItem(string name, bool isChecked, bool isEnabled, bool isSubmenu, object data, Texture2D icon, string tooltip)
        {
            var item = s_ItemPool.Get();
            item.RegisterCallback<PointerDownEvent>(e => item.parent.parent.parent.EnableInClassList(clickUssClassName, true));
            item.RegisterCallback<PointerUpEvent>(e => item.parent.parent.parent.EnableInClassList(clickUssClassName, false));
            item.SetEnabled(isEnabled);
            item.userData = data;
            item.tooltip = tooltip;

            var children = item.Children() as List<VisualElement>;

            if (isChecked)
                item.pseudoStates |= PseudoStates.Checked;
            
            var leftAppendix = children[0];
            leftAppendix.AddToClassList(appendixUssClassName);

            if (icon != null)
            {
                leftAppendix.AddToClassList(iconUssClassName);
                leftAppendix.style.backgroundImage = icon;
            }
            else
            {
                leftAppendix.AddToClassList(checkmarkUssClassName);
            }

            var label = children[1] as Label;
            label.text = name;

            var rightAppendix = children[2];
            rightAppendix.AddToClassList(appendixUssClassName);

            if (isSubmenu)
            {
                rightAppendix.AddToClassList(submenuUssClassName);
            }

            return item;
        }

        internal void NavigateTo(MenuItem menu)
        {
            m_Current = menu;
            Rebuild();
        }

        internal void NavigateBack(bool hideRoots = false)
        {
            if (m_Current.parent == null)
            {
                // Need to be able to disable hiding for search mode
                if(hideRoots)
                    Hide();

                return;
            }

            var select = m_Current.element;
            m_Current = m_Current.parent;

            Rebuild();
            select?.schedule.Execute(() => UpdateSelection(select));
        }

        void Rebuild()
        {
            if (m_Current.children.Count == 0 && m_Current.headerActions.Count == 0)
                return;
            
            m_Title.text = m_Current.name;
            var items = m_Current.children;

            m_ListView.itemsSource = items;
            m_ListView.RefreshItems();

            m_HeaderButtons.Clear();

            foreach (var item in m_Current.headerActions)
                m_HeaderButtons.Add(item.element);

            SetupHeaderStrip?.Invoke(m_HeaderButtons);

            onRebuild?.Invoke();

            m_TitleBar.EnableInClassList(hiddenClassName, string.IsNullOrWhiteSpace(m_Title.text) && m_Current.headerActions.Count < 1);

            var backButtonHidden = m_Current.parent == null || !allowBackButton;
            m_Back.EnableInClassList(hiddenClassName, backButtonHidden);
            m_TitleBar.EnableInClassList(titleAreaWithBackUssClassName, !backButtonHidden);

            var separatorIndex = outerContainer.IndexOf(m_HeaderSeparator);
            var visibleChilds = 0;
            for (int i = 0; i < separatorIndex; i++)
            {
                if (!outerContainer.ElementAt(i).ClassListContains(hiddenClassName))
                    visibleChilds++;
            }
            m_HeaderSeparator.EnableInClassList(hiddenClassName, visibleChilds < 1);
        }

        internal void UpdateItem(string itemName, bool isChecked)
        {
            // Since this is only used by MaskFieldBase we can assume we won't need to update submenus
            var item = root.children.Find(x => x.name == itemName);

            if (item == null)
                return;

            if (isChecked)
                item.element.pseudoStates |= PseudoStates.Checked;
            else
                item.element.pseudoStates &= ~PseudoStates.Checked;
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
            m_OuterContainer.style.left = local.x - m_PanelRootVisualContainer.layout.x;
            m_OuterContainer.style.top = local.y + position.height - m_PanelRootVisualContainer.layout.y;

            m_DesiredRect = anchored ? position : Rect.zero;

            m_MenuContainer.schedule.Execute(m_ListView.Focus);
            EnsureVisibilityInParent();

            if (targetElement != null)
                targetElement.pseudoStates |= PseudoStates.Active;
        }

        private void OnTargetElementDetachFromPanel(DetachFromPanelEvent evt)
        {
            Hide();
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
                else
                {
                    m_OuterContainer.style.maxHeight = m_PanelRootVisualContainer.layout.height - m_DesiredRect.y;
                    m_OuterContainer.style.width = m_DesiredRect.width;
                }
            }
        }

        // Avoid Linq
        static T FirstOrDefault<T>(List<T> source)
        {
            foreach (var item in source)
                return item;

            return default;
        }

        static T FirstOrDefault<T>(List<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
                if(predicate(item))
                    return item;

            return default;
        }
    }
}
