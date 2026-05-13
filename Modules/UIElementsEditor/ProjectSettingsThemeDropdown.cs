// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [UxmlElement]
    internal partial class ProjectSettingsThemeDropdown : DropdownField
    {
        public const string k_Separator = "|";

        internal override void AddMenuItems(AbstractGenericMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            if (choices == null)
                return;

            foreach (string item in choices)
            {
                bool isSelected = EqualityComparer<string>.Default.Equals(item, value) && !showMixedValue;
                if (item == k_Separator)
                {
                    menu.AddSeparator("");
                    continue;
                }
                menu.AddItem(GetListItemToDisplay(item), isSelected,
                    () => ChangeValueFromMenu(item));
            }
        }

        private void ChangeValueFromMenu(string menuItem)
        {
            value = menuItem;
        }
    }
}

