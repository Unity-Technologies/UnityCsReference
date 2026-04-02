// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    sealed class GenericToDropdownMenuConverter : AbstractGenericMenu
    {
        public DropdownMenu DropdownMenu { get; set; }

        public override void AddItem(string itemName, bool isChecked, Action action)
        {
            DropdownMenu.AppendAction(itemName, _ => { action(); }, isChecked ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        public override void AddItem(string itemName, bool isChecked, Action<object> action, object data)
        {
            DropdownMenu.AppendAction(itemName, _ => { action(data); }, isChecked ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        public override void AddDisabledItem(string itemName, bool isChecked)
        {
            var checkedStatus = isChecked ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.None;
            DropdownMenu.AppendAction(itemName, _ => { }, DropdownMenuAction.Status.Disabled | checkedStatus);
        }

        public override void AddSeparator(string path)
        {
            DropdownMenu.AppendSeparator(path);
        }

        public override void DropDown(Rect position, VisualElement targetElement,
            DropdownMenuSizeMode dropdownMenuSizeMode = DropdownMenuSizeMode.Auto)
        {
            throw new NotImplementedException();
        }
    }
}
