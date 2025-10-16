// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A <see cref="PortConnectorPolymorphicPart"/> with a dropdown button letting the user chose which type to use.
    /// </summary>
    class PortConnectorPolymorphicPart : PortConnectorWithIconPart
    {
        /// <summary>
        /// The USS class name added to <see cref="PortConnectorPolymorphicPart"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-port-connector-polymorphic-part";

        public static readonly string menuIconUssName = "ge-data-type-icon";

        /// <summary>
        /// Creates a new instance of the <see cref="PortConnectorPolymorphicPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortConnectorWithIconPart"/>.</returns>
        public new static PortConnectorPolymorphicPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is PortModel && ownerElement is Port)
            {
                return new PortConnectorPolymorphicPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConnectorPolymorphicPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        PortConnectorPolymorphicPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            base.BuildUI(container);

            m_Icon.RemoveFromHierarchy();
            var button = new Button { name = "PolymorphicDropDown" };
            button.RegisterCallback<ClickEvent>(ShowPolymorphicMenu);
            button.Add(m_Icon);
            button.Add(new VisualElement { name = "arrow" });
            Root.Add(button);
            button.PlaceBehind(m_ConnectorLabel);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            Root.AddToClassList(ussClassName);
            Root.AddPackageStylesheet("PortConnectorPolymorphicPart.uss");
        }

        void OnPortTypeChanged(uint index)
        {
            if (m_Model is PortModel portModel)
                m_OwnerElement.RootView.Dispatch(new ChangePortTypeCommand { PortModel = portModel, NewTypeIndex = index });
        }

        void ShowPolymorphicMenu(ClickEvent evt)
        {
            if (m_Model is PortModel portModel && portModel.PolymorphicPortHandler != null)
            {
                var rootMenu = new GenericDropdownMenu();
                var rootMenuRoot = rootMenu.contentContainer.parent.parent.parent.parent;
                rootMenuRoot.AddPackageStylesheet($"View_{(EditorGUIUtility.isProSkin ? "dark" : "light")}.uss");
                rootMenuRoot.AddPackageStylesheet("PortConnectorPolymorphicPart.uss");
                rootMenuRoot.AddPackageStylesheet("TypeIcons.uss");
                var selectedIndex = portModel.PolymorphicPortHandler.SelectedTypeIndex;
                var types = portModel.PolymorphicPortHandler.Types;
                uint currentIndex = 0;
                foreach (var type in types)
                {
                    var icon = new Image { name = "menuIcon" };
                    var label = type.FriendlyName;
                    icon.pickingMode = PickingMode.Ignore;
                    icon.AddToClassList(menuIconUssName);
                    icon.AddToClassList($"ge-icon--data-type-{label.Replace(" ", string.Empty).ToLower()}");
                    var index = currentIndex++;
                    rootMenu.AddItem(label, index == selectedIndex, () => OnPortTypeChanged(index));
                    var menuItem = rootMenu.contentContainer.Query<VisualElement>(null, GenericDropdownMenu.itemContentUssClassName).Last();
                    menuItem.Insert(1, icon);
                    menuItem.pickingMode = PickingMode.Ignore;
                }

                FixMenu(rootMenu, types.Count);
                rootMenu.DropDown(new Rect(new Vector2(evt.position.x, evt.position.y), Vector2.right * 200), evt.target as VisualElement
                    , DropdownMenuSizeMode.Fixed);
            }
        }

        void FixMenu(GenericDropdownMenu menu, int itemCount)
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
                const int maxHeight = 400;
                var height = Mathf.Min((firstItem.resolvedStyle.minHeight.value + 4) * itemCount, maxHeight); // For unknown reason yet, the height is too small if not adding an extra pixel
                menuContainerOuter.style.minHeight = height;
            });
        }
    }
}
