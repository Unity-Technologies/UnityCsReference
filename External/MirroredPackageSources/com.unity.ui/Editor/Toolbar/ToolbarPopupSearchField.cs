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
