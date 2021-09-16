// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    internal class MultiselectDataSource : AdvancedDropdownDataSource
    {
        private Enum m_EnumFlag;
        private int m_Mask;
        private int[] m_OptionMaskValues;
        private string[] m_OptionNames;
        private int[] m_SelectedOptions;

        string[] m_DisplayNames;
        int[] m_FlagValues;

        public Enum enumFlags => m_EnumFlag;
        public int mask => m_Mask;

        public MultiselectDataSource(Enum enumValue)
        {
            m_EnumFlag = enumValue;
            var enumType = enumFlags.GetType();

            var enumData = EnumDataUtility.GetCachedEnumData(enumType);
            if (!enumData.serializable)
                // this is the same message used in ScriptPopupMenus.cpp
                throw new NotSupportedException(string.Format("Unsupported enum base type for {0}", enumType.Name));

            m_Mask = EnumDataUtility.EnumFlagsToInt(enumData, enumFlags);
            m_DisplayNames = enumData.displayNames;
            m_FlagValues = enumData.flagValues;

            MaskFieldGUI.GetMenuOptions(m_Mask, m_DisplayNames, m_FlagValues, out var buttonText, out var buttonTextMixed, out m_OptionNames, out m_OptionMaskValues, out m_SelectedOptions);
        }

        public MultiselectDataSource(int mask, string[] displayedOptions, int[] flagValues)
        {
            m_DisplayNames = displayedOptions;
            m_FlagValues = flagValues;
            MaskFieldGUI.GetMenuOptions(mask, displayedOptions, flagValues, out var buttonText, out var buttonTextMixed, out m_OptionNames, out m_OptionMaskValues, out m_SelectedOptions);
        }

        protected override AdvancedDropdownItem FetchData()
        {
            var rootGroup = new AdvancedDropdownItem(string.Empty);
            for (var i = 0; i < m_OptionNames.Length; i++)
            {
                if (i == 2)
                    rootGroup.AddSeparator();
                var element = new AdvancedDropdownItem(m_OptionNames[i]);
                element.elementIndex = i;
                rootGroup.AddChild(element);
            }

            RebuildSelection(rootGroup);
            return rootGroup;
        }

        void RebuildSelection(AdvancedDropdownItem rootItem)
        {
            selectedIDs.Clear();
            foreach (var selectionOption in m_SelectedOptions)
            {
                var name = m_OptionNames[selectionOption];
                foreach (var child in rootItem.children)
                {
                    if (child.name == name)
                        selectedIDs.Add(child.id);
                }
            }
        }

        public void UpdateSelectedId(AdvancedDropdownItem item)
        {
            m_Mask = m_OptionMaskValues[item.elementIndex];
            MaskFieldGUI.GetMenuOptions(m_Mask, m_DisplayNames, m_FlagValues, out var buttonText, out var buttonTextMixed, out m_OptionNames, out m_OptionMaskValues, out m_SelectedOptions);
            if (enumFlags != null)
                m_EnumFlag = EnumDataUtility.IntToEnumFlags(enumFlags.GetType(), m_Mask);
            RebuildSelection(root);
        }
    }
}
