// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Creates a tab to organize content on different screens.
    /// </summary>
    /// <remarks>
    /// A Tab has a header that can respond to pointer and mouse events. The header can hold an icon, a label, or both.
    /// The Tab content container can hold anything inside of it giving more flexibility to customize its layout.
    ///
    /// For more information, refer to [[wiki:UIE-uxml-element-tab|UXML element Tab]].
    /// </remarks>
    [UxmlElement(libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/Tab.png")]
    public partial class Tab : VisualElement
    {
        internal static readonly BindingId labelProperty = nameof(label);
        internal static readonly BindingId iconImageProperty = nameof(iconImage);
        internal static readonly BindingId closeableProperty = nameof(closeable);

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-tab";
        internal static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name for the header of this type.
        /// </summary>
        public static readonly string tabHeaderUssClassName = ussClassName + "__header";
        internal static readonly UniqueStyleString tabHeaderUssClassNameUnique = new(tabHeaderUssClassName);

        /// <summary>
        /// USS class name for the icon inside the header.
        /// </summary>
        public static readonly string tabHeaderImageUssClassName = tabHeaderUssClassName + "-image";
        internal static readonly UniqueStyleString tabHeaderImageUssClassNameUnique = new(tabHeaderImageUssClassName);

        /// <summary>
        /// USS class name for the icon inside the header when the value is null.
        /// </summary>
        public static readonly string tabHeaderEmptyImageUssClassName = tabHeaderImageUssClassName + "--empty";
        internal static readonly UniqueStyleString tabHeaderEmptyImageUssClassNameUnique = new(tabHeaderEmptyImageUssClassName);

        /// <summary>
        /// USS class name for the icon inside the header when the label is empty or null.
        /// </summary>
        public static readonly string tabHeaderStandaloneImageUssClassName = tabHeaderImageUssClassName + "--standalone";
        internal static readonly UniqueStyleString tabHeaderStandaloneImageUssClassNameUnique = new(tabHeaderStandaloneImageUssClassName);

        /// <summary>
        /// USS class name for the label of the header.
        /// </summary>
        public static readonly string tabHeaderLabelUssClassName = tabHeaderUssClassName + "-label";
        internal static readonly UniqueStyleString tabHeaderLabelUssClassNameUnique = new(tabHeaderLabelUssClassName);

        /// <summary>
        /// USS class name for the label of the header when the value is empty or null.
        /// </summary>
        public static readonly string tabHeaderEmptyLabeUssClassName = tabHeaderLabelUssClassName + "--empty";
        internal static readonly UniqueStyleString tabHeaderEmptyLabeUssClassNameUnique = new(tabHeaderEmptyLabeUssClassName);

        /// <summary>
        /// USS class name for the active state underline of the header.
        /// </summary>
        public static readonly string tabHeaderUnderlineUssClassName = tabHeaderUssClassName + "-underline";
        internal static readonly UniqueStyleString tabHeaderUnderlineUssClassNameUnique = new(tabHeaderUnderlineUssClassName);

        /// <summary>
        /// USS class name of container element of this type.
        /// </summary>
        public static readonly string contentUssClassName = ussClassName + "__content-container";
        internal static readonly UniqueStyleString contentUssClassNameUnique = new(contentUssClassName);

        /// <summary>
        /// USS class name for the dragging state of this type.
        /// </summary>
        public static readonly string draggingUssClassName = ussClassName + "--dragging";
        internal static readonly UniqueStyleString draggingUssClassNameUnique = new(draggingUssClassName);

        /// <summary>
        /// USS class name for reorderable tab elements.
        /// </summary>
        public static readonly string reorderableUssClassName = ussClassName + "__reorderable";
        internal static readonly UniqueStyleString reorderableUssClassNameUnique = new(reorderableUssClassName);

        /// <summary>
        /// USS class name for drag handle in reorderable tabs.
        /// </summary>
        public static readonly string reorderableItemHandleUssClassName = reorderableUssClassName + "-handle";
        internal static readonly UniqueStyleString reorderableItemHandleUssClassNameUnique = new(reorderableItemHandleUssClassName);

        /// <summary>
        /// USS class name for drag handlebar in reorderable tabs.
        /// </summary>
        public static readonly string reorderableItemHandleBarUssClassName = reorderableItemHandleUssClassName + "-bar";
        internal static readonly UniqueStyleString reorderableItemHandleBarUssClassNameUnique = new(reorderableItemHandleBarUssClassName);
        internal static readonly UniqueStyleString reorderableItemHandleBarLeftUssClassNameUnique = new(reorderableItemHandleBarUssClassName + "--left");

        /// <summary>
        /// The USS class name for a closeable tab.
        /// </summary>
        public static readonly string closeableUssClassName = tabHeaderUssClassName + "__closeable";
        internal static readonly UniqueStyleString closeableUssClassNameUnique = new(closeableUssClassName);

        /// <summary>
        /// The USS class name for close button in closable tabs.
        /// </summary>
        public static readonly string closeButtonUssClassName = ussClassName + "__close-button";
        internal static readonly UniqueStyleString closeButtonUssClassNameUnique = new(closeButtonUssClassName);

        /// <summary>
        /// This event is called when a tab has been selected.
        /// </summary>
        public event Action<Tab> selected;

        /// <summary>
        /// This event is called before a tab is closed. Return true if the tab can be closed.
        /// </summary>
        /// <remarks>
        /// In the case where more than one event are assigned, all of them will be executed synchronously but only the
        /// last value will be taken into account.
        /// </remarks>
        public event Func<bool> closing;

        /// <summary>
        /// This event is called when a tab has been closed.
        /// </summary>
        public event Action<Tab> closed;

        string m_Label;
        Background m_IconImage;
        bool m_Closeable;

        VisualElement m_ContentContainer;
        VisualElement m_DragHandle;
        VisualElement m_CloseButton;
        VisualElement m_TabHeader;
        Image m_TabHeaderImage;
        Label m_TabHeaderLabel;

        // Needed by the UIBuilder for authoring in the viewport
        internal Label headerLabel
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_TabHeaderLabel;
        }

        /// <summary>
        /// Returns the Tab's header.
        /// </summary>
        public VisualElement tabHeader => m_TabHeader;

        // Returns the reorder manipulator.
        internal TabDragger dragger { get; }

        /// <summary>
        /// The current index in the <see cref="TabView"/>.
        /// </summary>
        internal int index { get; set; }

        /// <summary>
        /// Sets the label of the Tab's header.
        /// </summary>
        [MultilineTextField]
        [UxmlAttribute]
        [CreateProperty]
        public string label
        {
            get => m_Label;
            set
            {
                if (string.CompareOrdinal(value, m_Label) == 0)
                    return;

                m_TabHeaderLabel.text = value;
                m_TabHeaderLabel.EnableInClassList(tabHeaderEmptyLabeUssClassNameUnique, string.IsNullOrEmpty(value));

                // Removes the margin using this class. This is until we have better support for targeting siblings in
                // USS or additional pseudo support like :not().
                m_TabHeaderImage.EnableInClassList(tabHeaderStandaloneImageUssClassNameUnique, string.IsNullOrEmpty(value));

                m_Label = value;
                NotifyPropertyChanged(labelProperty);
            }
        }

        [UxmlAttribute("icon-image"), UxmlAttributeBindingPath(nameof(iconImage))]
        [ImageFieldValueDecorator(displayName = "Icon Image")]
        // Used privately to help the serializer convert the Unity Object to the appropriate asset type.
        Object iconImageReference
        {
            get => iconImage.GetSelectedImage();
            set => iconImage = Background.FromObject(value);
        }

        /// <summary>
        /// Sets the icon for the Tab's header.
        /// </summary>
        [CreateProperty]
        public Background iconImage
        {
            get => m_IconImage;
            set
            {
                if (value == m_IconImage)
                    return;

                if (value.IsEmpty())
                {
                    m_TabHeaderImage.image = null;
                    m_TabHeaderImage.sprite = null;
                    m_TabHeaderImage.vectorImage = null;

                    m_TabHeaderImage.AddToClassList(tabHeaderEmptyImageUssClassNameUnique);
                    m_TabHeaderImage.RemoveFromClassList(tabHeaderStandaloneImageUssClassNameUnique);

                    m_IconImage = value;
                    NotifyPropertyChanged(iconImageProperty);
                    return;
                }

                // The image control will reset the other values to null
                if (value.texture)
                    m_TabHeaderImage.image = value.texture;
                else if (value.sprite)
                    m_TabHeaderImage.sprite = value.sprite;
                else if (value.renderTexture)
                    m_TabHeaderImage.image = value.renderTexture;
                else
                    m_TabHeaderImage.vectorImage = value.vectorImage;

                m_TabHeaderImage.RemoveFromClassList(tabHeaderEmptyImageUssClassNameUnique);
                m_TabHeaderImage.EnableInClassList(tabHeaderStandaloneImageUssClassNameUnique, string.IsNullOrEmpty(m_Label));
                m_IconImage = value;
                NotifyPropertyChanged(iconImageProperty);
            }
        }

        /// <summary>
        /// A property that adds the ability to close tabs.
        /// </summary>
        /// <remarks>
        /// The default value is <c>false</c>.
        /// Set this value to <c>true</c> to allow the user to close tabs in the tab view.
        /// </remarks>
        [UxmlAttribute]
        [CreateProperty]
        public bool closeable
        {
            get => m_Closeable;
            set
            {
                if (m_Closeable == value)
                    return;
                m_Closeable = value;
                m_TabHeader.EnableInClassList(closeableUssClassNameUnique, value);
                EnableTabCloseButton(value);
                NotifyPropertyChanged(closeableProperty);
            }
        }

        /// <summary>
        /// The container for the content of the <see cref="Tab"/>.
        /// </summary>
        public override VisualElement contentContainer => m_ContentContainer;

        /// <summary>
        /// Constructs a Tab.
        /// </summary>
        public Tab() : this(null, null) {}

        /// <summary>
        /// Constructs a Tab.
        /// </summary>
        /// <param name="label">The text to use as the header label.</param>
        public Tab(string label) : this(label, null) {}

        /// <summary>
        /// Constructs a Tab.
        /// </summary>
        /// <param name="iconImage">The icon used as the header icon.</param>
        public Tab(Background iconImage) : this(null, iconImage) {}

        /// <summary>
        /// Constructs a Tab.
        /// </summary>
        /// <param name="label">The text to use as the header label.</param>
        /// <param name="iconImage">The icon used as the header icon.</param>
        public Tab(string label, Background iconImage)
        {
            AddToClassList(ussClassNameUnique);

            m_TabHeader = new VisualElement()
            {
                name = tabHeaderUssClassName
            }.WithClassList(tabHeaderUssClassNameUnique);

            // Create Draggable handles for reorderable mode
            m_DragHandle = new VisualElement
            {
                name = reorderableItemHandleUssClassName
            }.WithClassList(reorderableItemHandleUssClassNameUnique);
            m_DragHandle.AddToClassList(reorderableItemHandleUssClassNameUnique);
            m_DragHandle.Add(new VisualElement
            {
                name = reorderableItemHandleBarUssClassName,
            }.WithClassList(reorderableItemHandleBarUssClassNameUnique, reorderableItemHandleBarLeftUssClassNameUnique));
            m_DragHandle.Add(new VisualElement
            {
                name = reorderableItemHandleBarUssClassName
            }.WithClassList(reorderableItemHandleBarUssClassNameUnique));

            // Creates the Image control that will hold the Tab's icon if applicable.
            m_TabHeaderImage = new Image()
            {
                name = tabHeaderImageUssClassName
            }.WithClassList(tabHeaderImageUssClassNameUnique, tabHeaderEmptyImageUssClassNameUnique);
            m_TabHeader.Add(m_TabHeaderImage);

            m_TabHeaderLabel = new Label()
            {
                name = tabHeaderLabelUssClassName
            }.WithClassList(tabHeaderLabelUssClassNameUnique);
            m_TabHeader.Add(m_TabHeaderLabel);

            m_TabHeader.RegisterCallback<PointerDownEvent>(OnTabClicked, CallbackOptions.IncludeDisabled);

            // Add the Tab's underline for active tab
            m_TabHeader.Add(new VisualElement()
            {
                name = tabHeaderUnderlineUssClassName
            }.WithClassList(tabHeaderUnderlineUssClassNameUnique));

            // Create close button for closable mode
            m_CloseButton = new VisualElement
            {
                name = closeButtonUssClassName
            }.WithClassList(closeButtonUssClassNameUnique);
            m_CloseButton.RegisterCallback<PointerDownEvent>(OnCloseButtonClicked);

            hierarchy.Add(m_TabHeader);

            m_ContentContainer = new VisualElement()
            {
                name = contentUssClassName,
                userData = m_TabHeader
            }.WithClassList(contentUssClassNameUnique);
            hierarchy.Add(m_ContentContainer);

            this.label = label;
            this.iconImage = iconImage;

            m_DragHandle.AddManipulator(dragger = new TabDragger());
            // Take over and repurpose the default VisualElement tooltip behaviour for the TabHeader.
            m_TabHeader.RegisterCallback<TooltipEvent>(UpdateTooltip);
            // Stop the default tooltip behaviour for Tab otherwise the content will take the tooltip's value over the
            // content's tooltip value
            RegisterCallback<TooltipEvent>((evt) => evt.StopImmediatePropagation());
        }

        // We want the tooltip to be displayed on the header level and not in the content-container.
        void UpdateTooltip(TooltipEvent evt)
        {
            if (evt.currentTarget is VisualElement element && !string.IsNullOrEmpty(tooltip))
            {
                evt.rect = element.worldBound;
                evt.tooltip = tooltip;
                evt.StopImmediatePropagation();
            }
        }

        void AddDragHandles()
        {
            m_TabHeader.Insert(0, m_DragHandle);
        }

        void RemoveDragHandles()
        {
            if (m_TabHeader.Contains(m_DragHandle))
            {
                m_TabHeader.Remove(m_DragHandle);
            }
        }

        internal void EnableTabDragHandles(bool enable)
        {
            if (enable)
                AddDragHandles();
            else
                RemoveDragHandles();
        }


        void AddCloseButton()
        {
            m_TabHeader.Add(m_CloseButton);
        }

        void RemoveCloseButton()
        {
            if (m_TabHeader.Contains(m_CloseButton))
            {
                m_TabHeader.Remove(m_CloseButton);
            }
        }

        internal void EnableTabCloseButton(bool enable)
        {
            if (enable)
                AddCloseButton();
            else
                RemoveCloseButton();
        }

        /// <summary>
        /// Sets the tab header's pseudo state to checked.
        /// </summary>
        internal void SetActive()
        {
            m_TabHeader.SetCheckedPseudoState(true);
            SetCheckedPseudoState(true);
        }

        /// <summary>
        /// Resets the tab header's pseudo state to its original state.
        /// </summary>
        internal void SetInactive()
        {
            m_TabHeader.SetCheckedPseudoState(false);
            SetCheckedPseudoState(false);
        }

        void OnTabClicked(PointerDownEvent _)
        {
            selected?.Invoke(this);
        }

        void OnCloseButtonClicked(PointerDownEvent evt)
        {
            var canClose = closing?.Invoke() ?? true;
            if (canClose)
            {
                RemoveFromHierarchy();
                closed?.Invoke(this);
            }
            evt.StopPropagation();
        }
    }
}
