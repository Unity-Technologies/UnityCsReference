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

        static readonly string menuOpenModifierUssClassName = ussClassName.WithUssModifier("menu-open");
        static readonly string polymorphicModifierUssClassName = ussClassName.WithUssModifier("polymorphic");

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

        VisualElement m_Arrow;
        Button m_Button;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            base.BuildUI(container);

            m_Icon.RemoveFromHierarchy();
            m_Button = new Button { name = "PolymorphicDropDown" };
            m_Button.RegisterCallback<ClickEvent>(ShowPolymorphicMenu);
            m_Button.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            m_Button.Add(m_Icon);
            m_Arrow = new VisualElement { name = "arrow" };
            m_Button.Add(m_Arrow);
            Root.Add(m_Button);
            m_Button.PlaceBehind(m_ConnectorLabel);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            Root.AddToClassList(ussClassName);
            Root.AddPackageStylesheet("PortConnectorPolymorphicPart.uss");
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            // The dropdown arrow only makes sense on a polymorphic port. When the port is redefined as non-polymorphic,
            // the connector part stays the same but the arrow must disappear.
            if (m_Model is PortModel portModel)
            {
                var isPolymorphic = portModel.IsPolymorphic;
                Root.EnableInClassList(polymorphicModifierUssClassName, isPolymorphic);
                if (m_Arrow != null)
                    m_Arrow.style.display = isPolymorphic ? DisplayStyle.Flex : DisplayStyle.None;
                if (m_Button != null)
                {
                    m_Button.pickingMode = isPolymorphic ? PickingMode.Position : PickingMode.Ignore;
                    m_Button.focusable = isPolymorphic;
                }
            }
        }

        void OnPortTypeChanged(uint index)
        {
            if (m_Model is PortModel portModel)
                m_OwnerElement.RootView.Dispatch(new ChangePortTypeCommand { PortModel = portModel, NewTypeIndex = index });
        }

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            OpenPolymorphicMenu();
            evt.StopPropagation();
        }

        void ShowPolymorphicMenu(ClickEvent evt)
        {
            OpenPolymorphicMenu();
        }

        void OpenPolymorphicMenu()
        {
            if (m_Model is PortModel portModel && portModel.IsPolymorphic)
            {
                var rootMenu = new GenericDropdownMenu();
                var rootMenuRoot = rootMenu.contentContainer.parent.parent.parent.parent;
                rootMenuRoot.AddPackageStylesheet($"View_{(EditorGUIUtility.isProSkin ? "dark" : "light")}.uss");
                rootMenuRoot.AddPackageStylesheet("PortConnectorPolymorphicPart.uss");
                rootMenuRoot.AddPackageStylesheet("TypeIcons.uss");
                var types = portModel.AllowedTypes;
                uint selectedIndex = 0;
                for (var i = 0; i < types.Count; i++)
                {
                    if (types[i] == portModel.DataTypeHandle)
                    {
                        selectedIndex = (uint)i;
                        break;
                    }
                }
                uint currentIndex = 0;
                foreach (var type in types)
                {
                    var icon = new Image { name = "menuIcon" };
                    var label = type.FriendlyName;
                    icon.pickingMode = PickingMode.Ignore;
                    icon.AddToClassList(menuIconUssName);
                    // Use the framework's canonical USS name for the type (kebab-case) so it matches the rules in TypeIcons.uss.
                    m_OwnerElement.RootView.TypeHandleInfos.AddUssClasses(GraphElementHelper.iconDataTypeClassPrefix, icon, type);
                    var index = currentIndex++;
                    rootMenu.AddItem(label, index == selectedIndex, () => OnPortTypeChanged(index));
                    var menuItem = rootMenu.contentContainer.Query<VisualElement>(null, GenericDropdownMenu.itemContentUssClassName).Last();
                    menuItem.Insert(1, icon);
                    menuItem.pickingMode = PickingMode.Ignore;
                }

                FixMenu(rootMenu, types.Count);
                Root.AddToClassList(menuOpenModifierUssClassName);
                rootMenuRoot.RegisterCallbackOnce<DetachFromPanelEvent>(_ => Root.RemoveFromClassList(menuOpenModifierUssClassName));
                var arrowBound = m_Arrow.worldBound;
                rootMenu.DropDown(new Rect(arrowBound.center.x, arrowBound.yMin, 200, arrowBound.height), m_Button, DropdownMenuSizeMode.Fixed);
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
