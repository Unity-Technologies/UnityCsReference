// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// An Editor-only control that lets users one or more options from a list of options. 
    /// </summary>
    /// <remarks> The difference between Mask64Field and [[UIElements.MarkField]] is that Mask64Field supports a 64-bit bitmask while [[UIElements.MarkField]] supports a 32-bit bitmask. For more information, refer to [[wiki:UIE-uxml-element-Mask64Field|UXML element Mask64Field]].
    /// </remarks>
    public abstract class BaseMask64Field : BasePopupField<UInt64, string>
    {
        internal static readonly BindingId choicesMasksProperty = nameof(choicesMasks);

        internal abstract UInt64 MaskToValue(UInt64 newMask);
        internal abstract UInt64 ValueToMask(UInt64 value);

        static readonly int s_NothingIndex = 0;
        static readonly int s_EverythingIndex = 1;
        static readonly int s_TotalIndex = 2;

        static readonly string s_MixedLabel = L10n.Tr("Mixed...");
        static readonly string s_EverythingLabel = L10n.Tr("Everything");
        static readonly string s_NothingLabel = L10n.Tr("Nothing");

        // This is the list of string representing all the user choices
        List<string> m_UserChoices;

        // This is the list of masks for every specific choice... if null, the mask will be computed with the default values
        List<UInt64> m_UserChoicesMasks;

        // This is containing a mask to cover all the choices from the list. Computed with the help of m_UserChoicesMasks
        //     or based on the power of 2 mask values.
        UInt64 m_FullChoiceMask;
        internal UInt64 fullChoiceMask => m_FullChoiceMask;

        internal BaseMask64Field(string label) : base(label)
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
                case ~(UInt64)0:
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
        public virtual List<UInt64> choicesMasks
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
                        m_UserChoicesMasks = new List<UInt64>();
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
                    if (m_UserChoices.Count >= (sizeof(UInt64) * 8))
                    {
                        m_FullChoiceMask = ~(UInt64)0;
                    }
                    else
                    {
                        m_FullChoiceMask = 0;
                        foreach (UInt64 itemMask in m_UserChoicesMasks)
                        {
                            if (itemMask == ~(UInt64)0)
                            {
                                continue;
                            }

                            m_FullChoiceMask |= itemMask;
                        }
                    }
                }
                else
                {
                    if (m_UserChoices.Count >= (sizeof(UInt64) * 8))
                    {
                        m_FullChoiceMask = ~(UInt64)0;
                    }
                    else
                    {
                        m_FullChoiceMask = ((UInt64)1 << m_UserChoices.Count) - 1;
                    }
                }
            }
        }

        // Trick to get the number of selected values...
        // A power of 2 number means only 1 selected...
        internal bool IsPowerOf2(UInt64 itemIndex)
        {
            return ((itemIndex & (itemIndex - 1)) == 0);
        }

        internal override string GetValueToDisplay()
        {
            return GetDisplayedValue(ValueToMask(value));
        }

        internal override string GetListItemToDisplay(UInt64 item)
        {
            return GetDisplayedValue(ValueToMask(item));
        }

        internal string GetDisplayedValue(UInt64 itemIndex)
        {
            var newValueToShowUser = "";

            switch (itemIndex)
            {
                case 0:
                    newValueToShowUser = m_Choices[s_NothingIndex];
                    break;

                case ~(UInt64)0:
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
                            foreach (UInt64 itemMask in m_UserChoicesMasks)
                            {
                                if (itemMask != ~(UInt64)0 && ((itemMask & itemIndex) == itemIndex))
                                {
                                    indexOfValue = m_UserChoicesMasks.IndexOf(itemMask);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            while (((UInt64)1 << indexOfValue) != itemIndex)
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

        public override UInt64 value
        {
            get => base.value;
            set
            {
                // We need to convert the value to an accepted mask value so that the comparision with the old value works (UUM-56605)
                // For example, if the value is null, we need to convert it to the mask value of the Nothing choice or it will be considered as different.
                base.value = MaskToValue(UpdateMaskIfEverything(ValueToMask(value)));
            }
        }

        public override void SetValueWithoutNotify(UInt64 newValue)
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
                var isSelected = IsItemSelected(maskOfItem) && !showMixedValue;

                menu.AddItem(GetListItemToDisplay(MaskToValue(maskOfItem)), isSelected, () => ChangeValueFromMenu(item));
            }
        }

        private bool IsItemSelected(UInt64 maskOfItem)
        {
            var valueMask = ValueToMask(value);

            if(maskOfItem == 0)
                return valueMask == 0;

            return (maskOfItem & valueMask) == maskOfItem;
        }

        private void UpdateMenuItems()
        {
            var menu = m_GenericMenu as GenericDropdownMenu;

            if (menu == null)
                return;

            foreach (var item in m_Choices)
            {
                var maskOfItem = GetMaskValueOfItem(item);
                var isSelected = IsItemSelected(maskOfItem);

                menu.UpdateItem(GetListItemToDisplay(MaskToValue(maskOfItem)), isSelected);
            }
        }

        // Based on the current mask, this is updating the value of the actual mask to use vs the full mask.
        // This is returning ~0 if all the values are selected...
        private protected virtual UInt64 UpdateMaskIfEverything(UInt64 currentMask)
        {
            var newMask = currentMask;
            // If the mask is full, put back the Everything flag.
            if (m_FullChoiceMask != 0)
            {
                if ((currentMask & m_FullChoiceMask) == m_FullChoiceMask)
                {
                    newMask = ~(UInt64)0;
                }
                else
                {
                    newMask &= m_FullChoiceMask;
                }
            }

            return newMask;
        }

        internal void ChangeValueFromMenu(string menuItem)
        {
            var newMask = showMixedValue ? 0 : ValueToMask(value);
            var maskFromItem = GetMaskValueOfItem(menuItem);
            switch (maskFromItem)
            {
                // Nothing
                case 0:
                    newMask = 0;
                    break;

                // Everything
                case ~(UInt64)0:
                    newMask = ~(UInt64)0;
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
        UInt64 GetMaskValueOfItem(string item)
        {
            UInt64 maskValue;
            var indexOfItem = m_Choices.IndexOf(item);
            switch (indexOfItem)
            {
                case 0: // Nothing
                    maskValue = 0;
                    break;
                case 1: // Everything
                    maskValue = ~(UInt64)0;
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
                            maskValue = (UInt64)1 << (indexOfItem - s_TotalIndex);
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
    /// Make a field for 64-bit masks.
    /// </summary>
    public class Mask64Field : BaseMask64Field
    {
        internal override UInt64 MaskToValue(UInt64 newMask) => newMask;
        internal override UInt64 ValueToMask(UInt64 value) => value;

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseMask64Field.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseMask64Field.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(choices), "choices"),
                });
            }

            #pragma warning disable 649
            [SerializeField] List<string> choices;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags choices_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Mask64Field();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                // Assigning null value throws.
                if (ShouldWriteAttributeValue(choices_UxmlAttributeFlags) && choices != null)
                {
                    var e = (Mask64Field)obj;
                    e.choices = choices;
                }
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

        internal override string GetListItemToDisplay(UInt64 itemIndex)
        {
            string displayedValue = GetDisplayedValue(itemIndex);
            if (ShouldFormatListItem(itemIndex))
                displayedValue = m_FormatListItemCallback(displayedValue);

            return displayedValue;
        }

        internal override string GetValueToDisplay()
        {
            string displayedValue = showMixedValue ? mixedValueString : GetDisplayedValue(rawValue);
            if (ShouldFormatSelectedValue())
                displayedValue = m_FormatSelectedValueCallback(displayedValue);
            return displayedValue;
        }

        internal bool ShouldFormatListItem(UInt64 itemIndex)
        {
            return itemIndex != 0 && itemIndex != ~(UInt64)0 && m_FormatListItemCallback != null;
        }

        internal bool ShouldFormatSelectedValue()
        {
            return rawValue != 0 && rawValue != ~(UInt64)0 && m_FormatSelectedValueCallback != null && IsPowerOf2(rawValue);
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-mask64-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";


        /// <summary>
        /// Initializes and returns an instance of Mask64Field.
        /// </summary>
        /// <param name="choices">A list of choices to populate the field.</param>
        /// <param name="defaultValue">The initial mask value for this field.</param>
        /// <param name="formatSelectedValueCallback">A callback to format the selected value. Unity calls this method automatically when a new value is selected in the field.</param>
        /// <param name="formatListItemCallback">The initial mask value this field should use. Unity calls this method automatically when displaying choices for the field.</param>
        public Mask64Field(List<string> choices, UInt64 defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
            : this(null, choices, defaultMask, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        /// <summary>
        /// Initializes and returns an instance of Mask64Field.
        /// </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        /// <param name="choices">A list of choices to populate the field.</param>
        /// <param name="defaultValue">The initial mask value for this field.</param>
        /// <param name="formatSelectedValueCallback">A callback to format the selected value. Unity calls this method automatically when a new value is selected in the field.</param>
        /// <param name="formatListItemCallback">The initial mask value this field should use. Unity calls this method automatically when displaying choices for the field.</param>
        public Mask64Field(string label, List<string> choices, UInt64 defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
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
        /// Initializes and returns an instance of Mask64Field.
        /// </summary>
        public Mask64Field()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Mask64Field.
        /// </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        public Mask64Field(string label)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }
}
