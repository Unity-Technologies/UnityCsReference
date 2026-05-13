// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// The pop-up search field for the toolbar. The search field includes a menu button. For more information, refer to [[wiki:UIE-uxml-element-ToolbarPopupSearchField|UXML element ToolbarPopupSearchField]].
    /// </summary>
    [UxmlElement]
    [Icon("UIToolkit/Icons/ToolbarPopupSearchField.png")]
    public partial class ToolbarPopupSearchField : ToolbarSearchField, IToolbarMenuElement
    {
        internal static readonly BindingId menuProperty = nameof(menu);

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
