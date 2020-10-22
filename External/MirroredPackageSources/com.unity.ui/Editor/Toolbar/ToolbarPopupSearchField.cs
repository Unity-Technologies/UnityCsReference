using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// The pop-up search field for the toolbar. The search field includes a menu button.
    /// </summary>
    public class ToolbarPopupSearchField : ToolbarSearchField, IToolbarMenuElement
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarPopupSearchField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ToolbarPopupSearchField> {}

        /// <summary>
        /// The menu used by the pop-up search field element.
        /// </summary>
        public DropdownMenu menu { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ToolbarPopupSearchField()
        {
            AddToClassList(popupVariantUssClassName);

            menu = new DropdownMenu();
            searchButton.clickable.clicked += this.ShowMenu;
        }
    }
}
