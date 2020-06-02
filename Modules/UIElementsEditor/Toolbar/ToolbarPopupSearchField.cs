// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ToolbarPopupSearchField : ToolbarSearchField, IToolbarMenuElement
    {
        public new class UxmlFactory : UxmlFactory<ToolbarPopupSearchField> {}

        public DropdownMenu menu { get; }

        public ToolbarPopupSearchField()
        {
            AddToClassList(popupVariantUssClassName);

            menu = new DropdownMenu();
            searchButton.clickable.clicked += this.ShowMenu;
        }
    }
}
