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
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : PopupField<string>.UxmlSerializedData
        {
            #pragma warning disable 649
            // The index field is responsible for applying validation to the value entered by users.
            // In order to ensure that users are able to enter the complete value without interruption,
            // we need to introduce a delay before the validation is performed. 
            [Delayed, SerializeField] int index;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags index_UxmlAttributeFlags;
            [SerializeField] List<string> choices;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags choices_UxmlAttributeFlags;

            // This field serves the purpose of overriding the value field so we can conceal it from the UI Builder.
            // Displaying it could result in conflicts when trying to control the dropdown value using both the value and index fields.
            [UxmlAttribute("value")]
            [HideInInspector, SerializeField] int valueOverride;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags valueOverride_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new DropdownField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (DropdownField)obj;

                // Assigning null value throws.
                if (ShouldWriteAttributeValue(choices_UxmlAttributeFlags) && choices != null)
                {
                    // We must copy
                    e.choices = new List<string>(choices);
                }

                // Index needs to be set after choices to initialize the field value
                // Dont set the index if its default or it will revert the change that may have come from `value`.
                if (ShouldWriteAttributeValue(index_UxmlAttributeFlags) && index != DropdownField.kPopupFieldDefaultIndex)
                    e.index = index;

                if (ShouldWriteAttributeValue(valueOverride_UxmlAttributeFlags))
                    e.valueOverride = valueOverride;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="DropdownField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<DropdownField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="DropdownField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
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

        // Placeholder required to prevent issues syncing UxmlSerializedData.
        internal int valueOverride { get; set; }

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
