// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Profiling;

namespace UnityEditor.Experimental.UIElements
{
    public class MaskField : BasePopupField<int, string>
    {
        public new class UxmlFactory : UxmlFactory<MaskField, UxmlTraits> {}

        public new class UxmlTraits : BasePopupField<int, string>.UxmlTraits
        {
            UxmlStringAttributeDescription m_MaskChoices = new UxmlStringAttributeDescription { name = "choices" };
            UxmlIntAttributeDescription m_MaskValue = new UxmlIntAttributeDescription { name = "value" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (MaskField)ve;
                string choicesFromBag = m_MaskChoices.GetValueFromBag(bag, cc);
                // Here the choices is comma separated in the string...
                string[] choices = choicesFromBag.Split(',');

                if (choices.Length != 0)
                {
                    List<string> listOfChoices = new List<string>();
                    foreach (var choice in choices)
                    {
                        listOfChoices.Add(choice.Trim());
                    }
                    f.choices = listOfChoices;
                }
                // The mask is simply an int
                f.SetValueWithoutNotify(m_MaskValue.GetValueFromBag(bag, cc));
            }
        }

        static int s_NothingIndex = 0;
        static int s_EverythingIndex = 1;
        static int s_TotalIndex = 2;

        // This is the list of string representing all the user choices
        List<string> m_UserChoices;

        // This is the list of masks for every specific choices... if null, the mask will be computed with the default values
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

        internal override List<string> choices
        {
            get { return m_UserChoices; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("choices", "choices can't be null");
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
                m_Choices.Add(L10n.Tr("Nothing"));
                m_Choices.Add(L10n.Tr("Everything"));
                m_Choices.AddRange(m_UserChoices);

                ComputeFullChoiceMask();

                // Make sure to update the text displayed
                SetValueWithoutNotify(m_Value);
            }
        }
        internal virtual List<int> choicesMasks
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
                    SetValueWithoutNotify(m_Value);
                }
            }
        }

        public virtual Func<string, string> formatSelectedValueCallback
        {
            get { return m_FormatSelectedValueCallback; }
            set
            {
                m_FormatSelectedValueCallback = value;
                m_TextElement.text = GetValueToDisplay();
            }
        }

        public virtual Func<string, string> formatListItemCallback
        {
            get { return m_FormatListItemCallback; }
            set { m_FormatListItemCallback = value; }
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            base.SetValueWithoutNotify(UpdateMaskIfEverything(newValue));
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
            string displayedValue = GetDisplayedValue(m_Value);
            if (ShouldFormatSelectedValue())
                displayedValue = m_FormatSelectedValueCallback(displayedValue);
            return displayedValue;
        }

        // Trick to get the number of selected values...
        // A power of 2 number means only 1 selected...
        internal bool IsPowerOf2(int itemIndex)
        {
            return ((itemIndex & (itemIndex - 1)) == 0);
        }

        internal bool ShouldFormatListItem(int itemIndex)
        {
            return itemIndex != 0 && itemIndex != -1 && m_FormatListItemCallback != null;
        }

        internal bool ShouldFormatSelectedValue()
        {
            return m_Value != 0 && m_Value != -1 && m_FormatSelectedValueCallback != null && IsPowerOf2(m_Value);
        }

        internal string GetDisplayedValue(int itemIndex)
        {
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
                        newValueToShowUser = L10n.Tr("Mixed ...");
                    }
                    break;
            }
            return newValueToShowUser;
        }

        internal override void AddMenuItems(GenericMenu menu)
        {
            foreach (var item in m_Choices)
            {
                var maskOfItem = GetMaskValueOfItem(item);
                var isSelected = false;
                switch (maskOfItem)
                {
                    case 0:
                        if (value == 0)
                        {
                            isSelected = true;
                        }
                        break;

                    case ~0:
                        if (value == ~0)
                        {
                            isSelected = true;
                        }
                        break;

                    default:
                        if ((maskOfItem & value) == maskOfItem)
                        {
                            isSelected = true;
                        }
                        break;
                }

                menu.AddItem(new GUIContent(GetListItemToDisplay(maskOfItem)), isSelected, () => ChangeValueFromMenu(item));
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

        private MaskField(List<string> choices, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null)
        {
            this.choices = choices;
            m_FormatListItemCallback = formatListItemCallback;
            m_FormatSelectedValueCallback = formatSelectedValueCallback;
        }

        public MaskField(List<string> choices, int defaultMask, Func<string, string> formatSelectedValueCallback = null, Func<string, string> formatListItemCallback = null) :
            this(choices, formatSelectedValueCallback, formatListItemCallback)
        {
            SetValueWithoutNotify(defaultMask);
        }

        public MaskField()
        {
        }

        // Returns the mask to be used for the item...
        int GetMaskValueOfItem(string item)
        {
            var maskValue = 0;
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
            var newMask = value;
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
                    newMask &= m_FullChoiceMask;

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
            value = newMask;
        }
    }
}
