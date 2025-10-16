// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A toggle button with an arrow image that rotates with the state of the button.
    /// </summary>
    [UnityRestricted]
    internal class CollapseButton : NodeToolbarButton
    {
        /// <summary>
        /// The name of <see cref="CollapseButton"/>.
        /// </summary>
        public static readonly string collapseButtonName = "collapse-button";

        /// <summary>
        /// The USS class name of a <see cref="CollapseButton"/>.
        /// </summary>
        public static readonly string collapseButtonUssClassName = "ge-collapse-button";

        /// <summary>
        /// The USS class name added to a <see cref="CollapseButton"/> when it is collapsed.
        /// </summary>
        public static readonly string collapsedUssClassName = collapseButtonUssClassName.WithUssModifier(GraphElementHelper.collapsedUssModifier);

        /// <summary>
        /// The USS class name added to the icon of a <see cref="CollapseButton"/>.
        /// </summary>
        public static readonly string collapsedButtonIconElementUssClassName = collapseButtonUssClassName.WithUssElement(GraphElementHelper.iconName);

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseButton"/> class.
        /// </summary>
        /// <param name="buttonName">The name of the button, used to identify it.</param>
        /// <param name="changeEventCallback">A callback after the button's value is changed.</param>
        /// <param name="showCallback">A callback after the button is shown on the node. Buttons are shown on hover, else they aren't.</param>
        public CollapseButton(string buttonName, EventCallback<ChangeEvent<bool>> changeEventCallback = null, Action<bool> showCallback = null)
            : base(buttonName, null, changeEventCallback, showCallback)
        {
            AddToClassList(collapseButtonUssClassName);
            Icon.AddToClassList(collapsedButtonIconElementUssClassName);
        }

        /// <inheritdoc />
        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            EnableInClassList(collapsedUssClassName, m_Value);
        }
    }
}
