using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A button for the toolbar.
    /// </summary>
    public class ToolbarButton : Button
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarButton"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ToolbarButton, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarButton"/>.
        /// </summary>
        public new class UxmlTraits : Button.UxmlTraits {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-toolbar-button";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clickEvent">The action to be called when the button is pressed.</param>
        public ToolbarButton(Action clickEvent) :
            base(clickEvent)
        {
            Toolbar.SetToolbarStyleSheet(this);
            RemoveFromClassList(Button.ussClassName);
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ToolbarButton() : this(() => {})
        {
        }
    }
}
