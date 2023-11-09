// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// The pop-up search field for the toolbar. The search field includes a menu button. For more information, refer to [[wiki:UIE-uxml-element-ToolbarPopupSearchField|UXML element ToolbarPopupSearchField]].
    /// </summary>
    public class ToolbarPopupSearchField : ToolbarSearchField, IToolbarMenuElement
    {
        internal static readonly DataBindingProperty menuProperty = nameof(menu);

        /// <summary>
        /// Instantiates a <see cref="ToolbarPopupSearchField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ToolbarPopupSearchField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarPopupSearchField"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a ToolbarPopupSearchField element that you can
        /// use in a UXML asset.
        /// </remarks>
        public new class UxmlTraits : ToolbarSearchField.UxmlTraits {}

        /// <summary>
        /// The menu used by the pop-up search field element.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
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
