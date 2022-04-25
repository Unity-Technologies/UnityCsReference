using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Control that displays, using the appropriate icons, the status of a field based on the type, the value binding,
    /// the value source of the underlying property. It also provides access to the field's contextual upon left click.
    /// </summary>
    class FieldStatusIndicator : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<FieldStatusIndicator, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription m_TargetFieldName = new UxmlStringAttributeDescription { name = "field-name" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                (ve as FieldStatusIndicator).targetFieldName = m_TargetFieldName.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string s_UssClassName = "unity-builder-field-status-indicator";
        /// <summary>
        /// Name of the content element.
        /// </summary>
        public static readonly string s_ContentElementName = "content-element";
        /// <summary>
        /// USS class name of content elements in elements of this type.
        /// </summary>
        public static readonly string s_ContentElementUSSClassName = s_UssClassName + "__content";
        /// <summary>
        /// Name of the icon element.
        /// </summary>
        public static readonly string s_IconElementName = "icon-element";
        /// <summary>
        /// USS class name of icon elements in elements of this type.
        /// </summary>
        public static readonly string s_IconElementUSSClassName = s_UssClassName + "__icon";
        /// <summary>
        /// Name of the property used to directly look up for the FieldStatusIndicator object associated to a given field.
        /// </summary>
        public static readonly string s_FieldStatusIndicatorVEPropertyName = "__unity-ui-builder-field-status-indicator";

        VisualElement m_TargetField;

        /// <summary>
        /// Callback used to add menu items to the contextual menu of the associated field before it opens.
        /// </summary>
        public Action<DropdownMenu> populateMenuItems;

        /// <summary>
        /// The field associated with this FieldStatusIndicator.
        /// </summary>
        public VisualElement targetField
        {
            get => m_TargetField;
            set
            {
                if (m_TargetField == value)
                    return;

                if (m_TargetField != null)
                    m_TargetField.SetProperty(s_FieldStatusIndicatorVEPropertyName, null);

                m_TargetField = value;

                if (m_TargetField != null)
                    m_TargetField.SetProperty(s_FieldStatusIndicatorVEPropertyName, this);
            }
        }

        /// <summary>
        /// The name of the field to be associated to when the indicator is added to a StyleRow in UXML.
        /// </summary>
        public string targetFieldName { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public FieldStatusIndicator()
            : base()
        {
            AddToClassList(s_UssClassName);
            var contextMenuManipulator = new ContextualMenuManipulator((evt) =>
            {
                populateMenuItems(evt.menu);
                // stop immediately to not propagate the event to the row.
                evt.StopImmediatePropagation();
            });
            // show menu also on left-click
            contextMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            var contentElement = new VisualElement { name = s_ContentElementName };

            contentElement.AddToClassList(s_ContentElementUSSClassName);
            contentElement.AddManipulator(contextMenuManipulator);

            var iconElement = new VisualElement()
            {
                name = s_IconElementName,
                pickingMode = PickingMode.Ignore
            };

            iconElement.AddToClassList(s_IconElementUSSClassName);

            contentElement.Add(iconElement);
            Add(contentElement);
        }
    }
}
