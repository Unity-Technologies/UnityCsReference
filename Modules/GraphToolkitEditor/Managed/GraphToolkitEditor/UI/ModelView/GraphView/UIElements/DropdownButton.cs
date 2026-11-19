// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// This control is a button with a dropdown menu. The main button part functions as a normal button, allowing users to click on it to trigger an action.
    /// The dropdown menu contains a list of options for the user to choose from to trigger an action.
    /// </summary>
    [UnityRestricted]
    internal class DropdownButton : BaseField<string>
    {
        /// <summary>
        /// Holds information on an item for a dropdown menu.
        /// </summary>
        [UnityRestricted]
        internal class MenuItem
        {
            public string Name { get; set; }
            public Action Action { get; set; }
            public bool IsDisabled { get; set; }
        }

        /// <summary>
        /// The USS class name of a <see cref="DropdownButton"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-dropdown-button";

        /// <summary>
        /// The USS class name of the arrow input of a <see cref="DropdownButton"/>.
        /// </summary>
        public static readonly string dropdownInputElementUssClassName = ussClassName.WithUssElement("arrow-input");

        /// <summary>
        /// The USS class name of the arrow icon of a <see cref="DropdownButton"/>.
        /// </summary>
        public static readonly string dropdownArrowIconUssClassName = ussClassName.WithUssElement("arrow-icon");

        MenuItem[] m_Items;

        /// <summary>
        /// Initializes a new instance of the <see cref="DropdownButton"/> class.
        /// </summary>
        /// <param name="items">The items of the dropdown menu. The first item is used for the main button.</param>
        public DropdownButton(MenuItem[] items)
            : base("", null)
        {
            if (items.Length == 0)
            {
                Debug.LogWarning("A DropdownButton needs at least 1 item.");
                return;
            }

            // Make the element look like a button
            this.AddPackageStylesheet("DropdownButton.uss");

            m_Items = items;

            // The main button uses the first item
            label = MainItem.Name;

            // Remove classes that we do not need
            labelElement.RemoveFromClassList(labelUssClassName);
            var inputElement = this.SafeQ(null, inputUssClassName);
            Remove(inputElement);

            labelElement.displayTooltipWhenElided = false;
            labelElement.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
            {
                var maxWidth = labelElement.resolvedStyle.maxWidth.value;
                var width = labelElement.resolvedStyle.width;
                tooltip = width > maxWidth ? MainItem.Name : "";
            });

            // Add classes we need
            AddToClassList(ussClassName);
            AddToClassList(Button.ussClassName);

            // Adds the dropdown arrow input
            var dropdownInputElement = new VisualElement { pickingMode = PickingMode.Position };
            dropdownInputElement.AddToClassList(dropdownInputElementUssClassName);
            Add(dropdownInputElement);

            var arrowElement = new Image
            {
                image = EditorGUIUtility.FindTexture("icon dropdown"),
                pickingMode = PickingMode.Ignore
            };
            arrowElement.AddToClassList(dropdownArrowIconUssClassName);
            dropdownInputElement.Add(arrowElement);

            // Add the click manipulators
            dropdownInputElement.AddManipulator(new Clickable(ShowMenu));
            this.AddManipulator(new Clickable(OnClickMainButton));
        }

        /// <inheritdoc />
        public override void SetValueWithoutNotify(string selectedItemName)
        {
            foreach (var item in m_Items)
            {
                if (selectedItemName == item.Name)
                {
                    item.Action.Invoke();
                    return;
                }
            }
        }

        /// <summary>
        /// Enable an item in the dropdown menu.
        /// </summary>
        /// <param name="index">The index of the item to enable in the item list.</param>
        /// <param name="isEnabled">Whether the item should be enabled or not.</param>
        public void EnableItem(int index, bool isEnabled)
        {
            if (m_Items.Length <= index)
                return;

            m_Items[index].IsDisabled = !isEnabled;
        }

        void OnClickMainButton()
        {
            MainItem.Action?.Invoke();
        }

        void ShowMenu()
        {
            var menu = new GenericDropdownMenu();

            PopulateMenu(menu);

            FixMenu(menu);
            menu.DropDown(worldBound, this, DropdownMenuSizeMode.Fixed);
        }

        void PopulateMenu(GenericDropdownMenu menu)
        {
            foreach (var item in m_Items)
            {
                if (MainItem.Name == item.Name)
                {
                    // The first item is the main item, it is invoked by clicking on the main button and is not part of the menu
                    continue;
                }
                if (item.IsDisabled)
                {
                    menu.AddDisabledItem(item.Name, false);
                }
                else
                {
                    menu.AddItem(item.Name, false, item.Action);
                }
            }
        }

        void FixMenu(GenericDropdownMenu menu)
        {
            // Set a min height to the menu, else the menu is too small for the items to show
            var menuContainerOuter = menu.contentContainer.GetFirstAncestorWhere(ve =>
                ve.ClassListContains(GenericDropdownMenu.containerOuterUssClassName));

            const int minHeight = 50;
            menuContainerOuter.style.minHeight = minHeight;

            var firstItem = menu.contentContainer.SafeQ(className: GenericDropdownMenu.itemUssClassName);
            firstItem?.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
            {
                // The desired min height is the min height of an item X the number of items
                const int maxHeight = 200;
                var height = Mathf.Min(firstItem.resolvedStyle.minHeight.value * m_Items.Length, maxHeight);
                menuContainerOuter.style.minHeight = height;
            });

            foreach (var item in menu.contentContainer.Children())
            {
                var itemLabel = item.SafeQ<Label>();
                if (itemLabel != null)
                {
                    // Set the picking mode to position to be able to show tooltips on items
                    itemLabel.pickingMode = PickingMode.Position;

                    // Set the text overflow position to Start to be be consistent with VS
                    itemLabel.style.unityTextOverflowPosition = TextOverflowPosition.Start;
                }
            }
        }

        MenuItem MainItem => m_Items.Length > 0 ? m_Items[0] : new MenuItem();
    }
}
