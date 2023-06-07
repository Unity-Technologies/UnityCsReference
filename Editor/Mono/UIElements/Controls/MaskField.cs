// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Base class implementing the shared functionality for editing bit mask values.
    /// </summary>
    public abstract class BaseMaskField<TChoice> : BasePopupField<TChoice, string>
    {
        internal static readonly BindingId choicesMasksProperty = nameof(choicesMasks);

        internal abstract TChoice MaskToValue(int newMask);
        internal abstract int ValueToMask(TChoice value);

        static readonly int s_NothingIndex = 0;
        static readonly int s_EverythingIndex = 1;
        static readonly int s_TotalIndex = 2;

        static readonly string s_MixedLabel = L10n.Tr("Mixed...");
        static readonly string s_EverythingLabel = L10n.Tr("Everything");
        static readonly string s_NothingLabel = L10n.Tr("Nothing");

        // This is the list of string representing all the user choices
        List<string> m_UserChoices;

        // This is the list of masks for every specific choice... if null, the mask will be computed with the default values
        // More details about this :
        //   In IMGUI, the MaskField is only allowing the creation of the field with an array of choices that will have a mask
        //    based on power of 2 value starting from 1.
        //   However, the LayerMaskField is created based on the Layers and do not necessarily has power of 2 consecutive masks.
        //     Therefore, this specific internal field (in IMGUI...) is requiring a specific array to contain the mask value of the
        //     actual list of choices.
        List<int> m_UserChoicesMasks;

        // This is containing a mask to cover all the choices from the list. Computed with the help of m_UserChoicesMasks
        //     or based on the power of 2 mask values.
        int m_FullChoiceMask;
        internal int fullChoiceMask => m_FullChoiceMask;

        internal BaseMaskField(string label) : base(label)
        {
            textElement.RegisterCallback<GeometryChangedEvent>(OnTextElementGeometryChanged);
            m_AutoCloseMenu = false;
        }

        private void OnTextElementGeometryChanged(GeometryChangedEvent evt)
        {
            var mask = ValueToMask(value);

            switch (mask)
            {
                case 0:
                case ~0:
                    // Don't do anything for Nothing or Everything
                    break;
                default:
                    // Mixed values
                    if (!IsPowerOf2(mask))
                    {
                        // If the current text is "Mixed..." and we now have more space, we might need to check if the
                        // actual values would fit.
                        // If the current label contains the actual values and we now have less space, we might need to
                        // change it to "Mixed..."
                        if (textElement.text == s_MixedLabel && evt.oldRect.width < evt.newRect.width
                            || textElement.text != s_MixedLabel && evt.oldRect.width > evt.newRect.width)
                        {
                            textElement.text = GetMixedString();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// The list of choices to display in the popup menu.
        /// </summary>
        public override List<string> choices
        {
            get { return m_UserChoices; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Keep the original list in a separate user list ...
                if (m_UserChoices == null)
                {
                    m_UserChoices = new List<string>();
                }
                else
                {
                    m_UserChoices.Clear();
                }
                m_UserChoices.AddRange(value);

                // Now, add the nothing and everything choices...
                if (m_Choices == null)
                {
                    m_Choices = new List<string>();
                }
                else
                {
                    m_Choices.Clear();
                }

                ComputeFullChoiceMask();

                m_Choices.Add(GetNothingName());
                m_Choices.Add(GetEverythingName());
                m_Choices.AddRange(m_UserChoices);

                // Make sure to update the text displayed
                SetValueWithoutNotify(rawValue);
                NotifyPropertyChanged(choicesProperty);
            }
        }

        internal virtual string GetNothingName()
        {
            return s_NothingLabel;
        }

        internal virtual string GetEverythingName()
        {
            return s_EverythingLabel;
        }

        /// <summary>
        /// The list of list of masks for every specific choice to display in the popup menu.
        /// </summary>
        [CreateProperty]
        public virtual List<int> choicesMasks
        {
            get { return m_UserChoicesMasks; }
            set
            {
                if (value == null)
                {
                    m_UserChoicesMasks = null;
                }
                else
                {
                    // Keep the original list in a separate user list ...
                    if (m_UserChoicesMasks == null)
                    {
                        m_UserChoicesMasks = new List<int>();
                    }
                    else
                    {
                        m_UserChoicesMasks.Clear();
                    }
                    m_UserChoicesMasks.AddRange(value);
                    ComputeFullChoiceMask();
                    // Make sure to update the text displayed
                    SetValueWithoutNotify(rawValue);
                }
                NotifyPropertyChanged(choicesMasksProperty);
            }
        }

        void ComputeFullChoiceMask()
        {
            // Compute the full mask for all the items... it is not necessarily ~0 (which is all bits set to 1)
            if (m_UserChoices.Count == 0)
            {
                m_FullChoiceMask = 0;
            }
            else
            {
                if ((m_UserChoicesMasks != null) && (m_UserChoicesMasks.Count == m_UserChoices.Count))
                {
                    if (m_UserChoices.Count >= (sizeof(int) * 8))
                    {
                        m_FullChoiceMask = ~0;
                    }
                    else
                    {
                        m_FullChoiceMask = 0;
                        foreach (int itemMask in m_UserChoicesMasks)
                        {
                            if (itemMask == ~0)
                            {
                                continue;
                            }

                            m_FullChoiceMask |= itemMask;
                        }
                    }
                }
                else
                {
                    if (m_UserChoices.Count >= (sizeof(int) * 8))
                    {
                        m_FullChoiceMask = ~0;
                    }
                    else
                    {
                        m_FullChoiceMask = (1 << m_UserChoices.Count) - 1;
                    }
                }
            }
        }

        // Trick to get the number of selected values...
        // A power of 2 number means only 1 selected...
        internal bool IsPowerOf2(int itemIndex)
        {
            return ((itemIndex & (itemIndex - 1)) == 0);
        }

        internal override string GetValueToDisplay()
        {
            return GetDisplayedValue(ValueToMask(value));
        }

        internal override string GetListItemToDisplay(TChoice item)
        {
            return GetDisplayedValue(ValueToMask(item));
        }

        internal string GetDisplayedValue(int itemIndex)
        {
            if (showMixedValue)
                return mixedValueString;

            var newValueToShowUser = "";

            switch (itemIndex)
            {
                case 0:
                    newValueToShowUser = m_Choices[s_NothingIndex];
                    break;

                case ~0:
                    newValueToShowUser = m_Choices[s_EverythingIndex];
                    break;

                default:
                    // Show up the right selected value
                    if (IsPowerOf2(itemIndex))
                    {
                        var indexOfValue = 0;
                        if (m_UserChoicesMasks != null)
                        {
                            // Find the actual index of the selected choice...
                            foreach (int itemMask in m_UserChoicesMasks)
                            {
                                if ((itemMask & itemIndex) == itemIndex)
                                {
                                    indexOfValue = m_UserChoicesMasks.IndexOf(itemMask);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            while ((1 << indexOfValue) != itemIndex)
                            {
                                indexOfValue++;
                            }
                        }

                        // To get past the Nothing + Everything choices...
                        indexOfValue += s_TotalIndex;
                        if (indexOfValue < m_Choices.Count)
                        {
                            newValueToShowUser = m_Choices[indexOfValue];
                        }
                    }
                    else
                    {
                        if (m_UserChoicesMasks != null)
                        {
                            // Check if there's a name defined for this value
                            for (int i = 0; i < m_UserChoicesMasks.Count; i++)
                            {
                                var itemMask = m_UserChoicesMasks[i];
                                if (itemMask == itemIndex)
                                {
                                    var index = i + s_TotalIndex;
                                    newValueToShowUser = m_Choices[index];
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(newValueToShowUser))
                        {
                            newValueToShowUser = GetMixedString();
                        }
                    }
                    break;
            }
            return newValueToShowUser;
        }

        private string GetMixedString()
        {
            var sb = GenericPool<StringBuilder>.Get();

            foreach (var item in m_Choices)
            {
                var maskOfItem = GetMaskValueOfItem(item);

                if (!IsItemSelected(maskOfItem))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(item);
            }

            var mixedString = sb.ToString();
            var minSize = textElement.MeasureTextSize(mixedString, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);

            // If text doesn't fit, we use "Mixed..."
            if (float.IsNaN(textElement.resolvedStyle.width) || minSize.x > textElement.resolvedStyle.width)
            {
                mixedString = s_MixedLabel;
            }

            sb.Clear();
            GenericPool<StringBuilder>.Release(sb);

            return mixedString;
        }

        public override void SetValueWithoutNotify(TChoice newValue)
        {
            base.SetValueWithoutNotify(MaskToValue(UpdateMaskIfEverything(ValueToMask(newValue))));
        }

        internal override void AddMenuItems(IGenericMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            foreach (var item in m_Choices)
            {
                var maskOfItem = GetMaskValueOfItem(item);
                var isSelected = IsItemSelected(maskOfItem);

                menu.AddItem(GetListItemToDisplay(MaskToValue(maskOfItem)), isSelected, () => ChangeValueFromMenu(item));
            }
        }

        private bool IsItemSelected(int maskOfItem)
        {
            int valueMask = ValueToMask(value);

            if (maskOfItem == 0)
                return valueMask == 0;

            return (maskOfItem & valueMask) == maskOfItem;
        }

        private void UpdateMenuItems()
        {
            var menu = m_GenericMenu as GenericDropdownMenu;

            if (menu == null)
            {
                return;
            }

            foreach (var item in m_Choices)
            {
                var maskOfItem = GetMaskValueOfItem(item);
                var isSelected = IsItemSelected(maskOfItem);

                menu.UpdateItem(GetListItemToDisplay(MaskToValue(maskOfItem)), isSelected);
            }
        }

        // Based on the current mask, this is updating the value of the actual mask to use vs the full mask.
        // This is returning ~0 if all the values are selected...
        int UpdateMaskIfEverything(int currentMask)
        {
            int newMask = currentMask;
            // If the mask is full, put back the Everything flag.
            if (m_FullChoiceMask != 0)
            {
                if ((currentMask & m_FullChoiceMask) == m_FullChoiceMask)
                {
                    newMask = ~0;
                }
                else
                {
                    newMask &= m_FullChoiceMask;
                }
            }

            return newMask;
        }

        private void ChangeValueFromMenu(string menuItem)
        {
            var newMask = ValueToMask(value);
            var maskFromItem = GetMaskValueOfItem(menuItem);
            switch (maskFromItem)
            {
                // Nothing
                case 0:
                    newMask = 0;
                    break;

                // Everything
                case ~0:
                    newMask = ~0;
                    break;

                default:
                    // Make sure to have only the real selected one...
                    //newMask &= m_FullChoiceMask;

                    // Add or remove the newly selected...
                    if ((newMask & maskFromItem) == maskFromItem)
                    {
                        newMask &= ~maskFromItem;
                    }
                    else
                    {
                        newMask |= maskFromItem;
                    }

                    // If the mask is full, put back the Everything flag.
                    newMask = UpdateMaskIfEverything(newMask);
                    break;
            }
            // Finally, make sure to update the value of the mask...
            value = MaskToValue(newMask);
            UpdateMenuItems();
        }

        // Returns the mask to be used for the item...
        int GetMaskValueOfItem(string item)
        {
            int maskValue;
            var indexOfItem = m_Choices.IndexOf(item);
            switch (indexOfItem)
            {
                case 0: // Nothing
                    maskValue = 0;
                    break;
                case 1: // Everything
                    maskValue = ~0;
                    break;
                default: // All others
                    if (indexOfItem > 0)
                    {
                        if ((m_UserChoicesMasks != null) && (m_UserChoicesMasks.Count == m_UserChoices.Count))
                        {
                            maskValue = m_UserChoicesMasks[(indexOfItem - s_TotalIndex)];
                        }
                        else
                        {
                            maskValue = 1 << (indexOfItem - s_TotalIndex);
                        }
                    }
                    else
                    {
                        // If less than 0, it means the item was not found...
                        maskValue = 0;
                    }

                    break;
            }
            return maskValue;
        }
    }

    /// <summary>
    /// Make a field for masks.
    /// </summary>
    public class MaskField : BaseMaskField<int>
    {
        internal override int MaskToValue(int newMask) => newMask;
        internal override int ValueToMask(int value) => value;

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseMaskField<int>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] private List<string> choices;
            #pragma warning restore 649

            public override object CreateInstance() => new MaskField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (MaskField)obj;

                // Assigning null value throws.
                if (choices != null)
                    e.choices = choices;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="MaskField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<MaskField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MaskField"/>.
        /// </summary>
        public new class UxmlTraits : BasePopupField<int, UxmlIntAttributeDescription>.UxmlTraits
        {
            UxmlStringAttributeDescription m_MaskChoices = new UxmlStringAttributeDescription { name = "choices" };
            UxmlIntAttributeDescription m_MaskValue = new UxmlIntAttributeDescription { name = "value" };

            /// <summary>
            /// Initializes the <see cref="UxmlTraits"/> for <see cref="MaskField"/>.
            /// </summary>
            /// <param name="ve">The VisualElement that will be populated.</param>
            /// <param name="bag">The bag from where the attributes are taken.</param>
            /// <param name="cc">The creation context, unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var f = (MaskField)ve;

                string choicesFromBag = m_MaskChoices.GetValueFromBag(bag, cc);


                var listOfChoices = UxmlUtility.ParseStringListAttribute(choicesFromBag);

                if (listOfChoices != null && listOfChoices.Count > 0)
                {
                    f.choices = listOfChoices;
                }
                // The mask is simply an int
                f.SetValueWithoutNotify(m_MaskValue.GetValueFromBag(bag, cc));

                base.Init(ve, bag, cc);
            }
        }

        /// <summary>
        /// Callback that provides a string representation used to display the selected value.
        /// </summary>
        public virtual Func<string, string> formatSelectedValueCallback
        {
            get { return m_FormatSelectedValueCallback; }
            set
            {
                m_FormatSelectedValueCallback = value;
                textElement.text = GetValueToDisplay();
            }
        }

        /// <summary>
        /// Callback that provides a string representation used to populate the popup menu.
        /// </summary>
        public virtual Func<string, string> formatListItemCallback
        {
            get { return m_FormatListItemCallback; }
            set { m_FormatListItemCallback = value; }
        }

        internal override string GetListItemToDisplay(int itemIndex)
        {
            string displayedValue = GetDisplayedValue(itemIndex);
            if (ShouldFormatListItem(itemIndex))
                displayedValue = m_FormatListItemCallback(displayedValue);

            return displayedValue;
        }

        internal override string GetValueToDisplay()
        {
            string displayedValue = GetDisplayedValue(rawValue);
            if (ShouldFormatSelectedValue())
                displayedValue = m_FormatSelectedValueCallback(displayedValue);
            return displayedValue;
        }

        internal bool ShouldFormatListItem(int itemIndex)
        {
            return itemIndex != 0 && itemIndex != -1 && m_FormatListItemCallback != null;
        }

        internal bool ShouldFormatSelectedValue()
        {
            return rawValue != 0 && rawValue != -1 && m_FormatSelectedValueCallback != null && IsPowerOf2(rawValue);
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-mask-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";


        /// <summary>
        /// Initializes and returns an instance of MaskField.
        /// </summary>
        /// <param name="choices">A list of choices to populate the field.</param>
        /// <param name="defaultValue">The initial mask value for this field.</param>
        /// <param name="formatSelectedValueCallback">A callback to format the selected value. Unity calls this method automatically when a new value is selected in the field..</param>
        /// <param name="formatListItemCallback">The initial mask value this field should use. Unity calls this method automatically when displaying choices for the field.</param>
        public MaskField(List<string> choices, int defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultMask, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>
        /// Initializes and returns an instance of MaskField.
        /// </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        /// <param name="choices">A list of choices to populate the field.</param>
        /// <param name="defaultValue">The initial mask value for this field.</param>
        /// <param name="formatSelectedValueCallback">A callback to format the selected value. Unity calls this method automatically when a new value is selected in the field..</param>
        /// <param name="formatListItemCallback">The initial mask value this field should use. Unity calls this method automatically when displaying choices for the field.</param>
        public MaskField(string label, List<string> choices, int defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(label)
        {
            this.choices = choices;
            m_FormatListItemCallback = formatListItemCallback;
            m_FormatSelectedValueCallback = formatSelectedValueCallback;

            SetValueWithoutNotify(defaultMask);

            this.formatListItemCallback = formatListItemCallback;
            this.formatSelectedValueCallback = formatSelectedValueCallback;
        }

        /// <summary>
        /// Initializes and returns an instance of MaskField.
        /// </summary>
        public MaskField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of MaskField.
        /// </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        public MaskField(string label)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }
}
