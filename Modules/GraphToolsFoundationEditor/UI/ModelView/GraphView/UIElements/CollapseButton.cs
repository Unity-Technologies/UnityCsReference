// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A toggle button with an arrow image that rotates with the state of the button.
    /// </summary>
    class CollapseButton : ToggleIconButton
    {
        public static readonly string collapseButtonUssClassName = "ge-collapse-button";
        public static readonly string collapsedUssClassName = collapseButtonUssClassName.WithUssModifier("collapsed");
        public static readonly string collapsedButtonIconElementUssClassName = collapseButtonUssClassName.WithUssElement(iconElementName);

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseButton"/> class.
        /// </summary>
        public CollapseButton()
        {
            AddToClassList(collapseButtonUssClassName);
            m_Icon.AddToClassList(collapsedButtonIconElementUssClassName);
        }

        /// <inheritdoc />
        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            EnableInClassList(collapsedUssClassName, m_Value);
        }
    }
}
