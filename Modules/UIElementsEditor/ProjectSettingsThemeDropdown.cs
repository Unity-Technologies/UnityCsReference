// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class ProjectSettingsThemeDropdown : DropdownField
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : DropdownField.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                DropdownField.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[] { }, false);
            }

            public override object CreateInstance() => new ProjectSettingsThemeDropdown();
        }

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

