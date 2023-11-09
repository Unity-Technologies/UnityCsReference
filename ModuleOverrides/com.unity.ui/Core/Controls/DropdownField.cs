// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A control that allows the user to pick a choice from a list of options. For more information, refer to [[wiki:UIE-uxml-element-dropdown|UXML element Dropdown]].
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
            UxmlIntAttributeDescription m_Index = new UxmlIntAttributeDescription { name = "index" };
            UxmlStringAttributeDescription m_Choices = new UxmlStringAttributeDescription() { name = "choices" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (DropdownField)ve;
                var choices = UxmlUtility.ParseStringListAttribute(m_Choices.GetValueFromBag(bag, cc));
                if (choices != null)
                    f.choices = choices;
                f.index = m_Index.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField()
            : this(null) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(string label)
            : base(label) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(List<string> choices, string defaultValue, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(string label, List<string> choices, string defaultValue, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : base(label, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(List<string> choices, int defaultIndex, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback) {}

        /// <summary>
        /// Construct a DropdownField.
        /// </summary>
        public DropdownField(string label, List<string> choices, int defaultIndex, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : base(label, choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback) {}
    }
}
