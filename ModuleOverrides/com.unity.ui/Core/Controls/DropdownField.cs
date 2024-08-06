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
    public class DropdownField : PopupField<string>
    {
        /// <summary>
        /// Instantiates a <see cref="DropdownField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<DropdownField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="DropdownField"/>.
        /// </summary>
        public new class UxmlTraits : BaseField<string>.UxmlTraits
        {
            UxmlIntAttributeDescription m_Index = new UxmlIntAttributeDescription { name = "index", defaultValue = kPopupFieldDefaultIndex };
            UxmlStringAttributeDescription m_Choices = new UxmlStringAttributeDescription() { name = "choices" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (DropdownField)ve;
                var choices = ParseChoiceList(m_Choices.GetValueFromBag(bag, cc));
                if (choices != null)
                    f.choices = choices;
                f.index = m_Index.GetValueFromBag(bag, cc);
            }
        }

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
