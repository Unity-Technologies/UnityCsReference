// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal static partial class StatelessAdvancedDropdown
    {
        public static int SearchablePopup(Rect rect, int selectedIndex, string[] displayedOptions)
        {
            return DoSearchablePopup(rect, selectedIndex, displayedOptions, EditorStyles.popup);
        }

        public static Enum EnumFlagsField(Rect rect, Enum options)
        {
            return DoEnumMaskPopup(rect, options, EditorStyles.popup);
        }

        public static int MaskField(Rect rect, int mask, string[] displayedOptions)
        {
            return DoMaskField(rect, mask, displayedOptions, EditorStyles.popup);
        }

        public static Enum EnumPopup(Rect rect, Enum selected, params GUILayoutOption[] options)
        {
            return DoEnumPopup(rect, selected, EditorStyles.popup, options);
        }

        public static int Popup(Rect rect, int selectedIndex, string[] displayedOptions)
        {
            return Popup(rect, selectedIndex, EditorGUIUtility.TempContent(displayedOptions));
        }

        public static int Popup(Rect rect, int selectedIndex, GUIContent[] displayedOptions)
        {
            return DoPopup(rect, selectedIndex, displayedOptions);
        }

        public static int IntPopup(Rect rect, int selectedValue, string[] displayedOptions, int[] optionValues)
        {
            return DoIntPopup(rect, selectedValue, displayedOptions, optionValues);
        }
    }
}
