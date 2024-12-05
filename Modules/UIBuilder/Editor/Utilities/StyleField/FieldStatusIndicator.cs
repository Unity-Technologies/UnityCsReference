// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Control that displays, using the appropriate icons, the status of a field based on the type, the value binding,
    /// the value source of the underlying property. It also provides access to the field's contextual upon left click.
    /// </summary>
    class FieldStatusIndicator : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(targetFieldName), "field-name")
                });
            }

            #pragma warning disable 649
            [SerializeField, UxmlAttribute("field-name")] string targetFieldName;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags targetFieldName_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new FieldStatusIndicator();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(targetFieldName_UxmlAttributeFlags))
                {
                    var e = (FieldStatusIndicator)obj;
                    e.targetFieldName = targetFieldName;
                }
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
