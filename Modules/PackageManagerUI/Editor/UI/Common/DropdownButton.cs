// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DropdownButton : BaseDropdownButton<DropdownMenu>, IToolbarMenuElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new DropdownButton();
        }

        public event Action onBeforeShowDropdown = delegate {};

        public DropdownButton() : base()
        {
        }

        public DropdownButton(Action clickEvent) : base(clickEvent)
        {
        }

        protected override int numDropdownItems => menu?.Count ?? 0;

        protected override void ShowDropdown()
        {
            onBeforeShowDropdown?.Invoke();
            if (menu != null)
                this.ShowMenu();
        }
    }
}
