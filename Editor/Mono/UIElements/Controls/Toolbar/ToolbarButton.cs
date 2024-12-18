// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Editor-only control that serves as a button in a <see cref="Toolbar"/>.
    /// </summary>
    /// <remarks>
    /// For more information, refer to [[wiki:UIE-uxml-element-ToolbarButton|UXML element ToolbarButton]].
    /// </remarks>
    /// <example>
    /// The following C# example creates a ToolbarButton with a label and click callback:
    /// <code source="../../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/Toolbar_button.cs"/>
    /// </example>
    /// <remarks>
    /// SA: [[Button]], [[Toolbar]]
    /// </remarks>
    public class ToolbarButton : Button
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : Button.UxmlSerializedData
        {
            public override object CreateInstance() => new ToolbarButton();
        }

        /// <summary>
        /// Instantiates a <see cref="ToolbarButton"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<ToolbarButton, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarButton"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
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
