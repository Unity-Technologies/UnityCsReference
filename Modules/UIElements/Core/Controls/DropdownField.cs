// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A control that allows the user to pick a choice from a list of options. For more information, refer to [[wiki:UIE-uxml-element-DropdownField|UXML element DropdownField]].
    /// </summary>
    [UxmlElement(libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/DropdownField.png")]
    public partial class DropdownField : PopupField<string>
    {
        // The index field is responsible for applying validation to the value entered by users.
        // In order to ensure that users are able to enter the complete value without interruption,
        // we need to introduce a delay before the validation is performed.
        [Delayed]
        [UxmlAttribute("index"), UxmlAttributeBindingPath(nameof(index))]
        internal int indexUXML
        {
            get => index;
            set => index = value;
        }

        [UxmlAttribute("choices"), UxmlAttributeBindingPath(nameof(choices))]
        internal List<string> choicesUXML
        {
            get => choices;
            set
            {
                choices = value;

                // Index needs to be set after choices to initialize the field value
                // Dont set the index if its default or it will revert the change that may have come from `value`.
                if (index != DropdownField.kPopupFieldDefaultIndex)
                {
                    SetIndexWithoutNotify(index);
                }
            }
        }

        // This field serves the purpose of overriding the value field so we can conceal it from the UI Builder.
        // Displaying it could result in conflicts when trying to control the dropdown value using both the value and index fields.
        [UxmlAttribute("value"), HideInInspector]
        internal int valueOverride { get; set; }

        /// <summary>
        /// Construct an empty DropdownField.
        /// </summary>
        public DropdownField()
            : this(null) {}

        /// <summary>
        /// Construct a DropdownField with a Label in front.
        /// </summary>
        /// <param name="label">The label to display in front of the field.</param>
        public DropdownField(string label)
            : base(label) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        /// <param name="choices">The list of choices to display in the dropdown.</param>
        /// <param name="defaultValue">The default value selected from the dropdown.</param>
        /// <param name="formatSelectedValueCallback">Callback to format the selected value.</param>
        /// <param name="formatListItemCallback">Callback to format the list items displayed in the dropdown.</param>
        public DropdownField(List<string> choices, string defaultValue, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        /// <param name="label">The label to display in front of the field.</param>
        /// <param name="choices">The list of choices to display in the dropdown.</param>
        /// <param name="defaultValue">The default value selected from the dropdown.</param>
        /// <param name="formatSelectedValueCallback">Callback to format the selected value.</param>
        /// <param name="formatListItemCallback">Callback to format the list items displayed in the dropdown.</param>
        public DropdownField(string label, List<string> choices, string defaultValue, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : base(label, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        /// <param name="choices">The list of choices to display in the dropdown.</param>
        /// <param name="defaultIndex">The index of the choice selected by default.</param>
        /// <param name="formatSelectedValueCallback">Callback to format the selected value.</param>
        /// <param name="formatListItemCallback">Callback to format the list items displayed in the dropdown.</param>
        public DropdownField(List<string> choices, int defaultIndex, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        /// <param name="label">The label to display in front of the field.</param>
        /// <param name="choices">The list of choices to display in the dropdown.</param>
        /// <param name="defaultIndex">The index of the choice selected by default.</param>
        /// <param name="formatSelectedValueCallback">Callback to format the selected value.</param>
        /// <param name="formatListItemCallback">Callback to format the list items displayed in the dropdown.</param>
        public DropdownField(string label, List<string> choices, int defaultIndex, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : base(label, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback) {}
    }
}
