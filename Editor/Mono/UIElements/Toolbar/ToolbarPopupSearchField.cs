// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class ToolbarPopupSearchField : ToolbarSearchField, IToolbarMenuElement
    {
        public new class UxmlFactory : UxmlFactory<ToolbarPopupSearchField> {}

        const string k_SearchButtonClassName = "toolbarSearchFieldPopup";

        public DropdownMenu menu { get; }

        public ToolbarPopupSearchField() :
            base(k_SearchButtonClassName)
        {
            menu = new DropdownMenu();
            m_SearchButton.clickable.clicked += this.ShowMenu;
        }
    }
}
