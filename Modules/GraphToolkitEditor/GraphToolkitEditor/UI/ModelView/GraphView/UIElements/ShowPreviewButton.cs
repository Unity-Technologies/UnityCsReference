// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A toggle button to show or hide a preview.
    /// </summary>
    [UnityRestricted]
    internal class ShowPreviewButton : NodeToolbarButton
    {
        /// <summary>
        /// The name of a <see cref="ShowPreviewButton"/>.
        /// </summary>
        public static readonly string previewButtonName = "preview-button";

        /// <summary>
        /// The USS class name added to a <see cref="ShowPreviewButton"/>.
        /// </summary>
        public static readonly string previewButtonUssClassName = "ge-show-preview-button";

        /// <summary>
        /// The USS class name added to a <see cref="ShowPreviewButton"/> when it shows the preview.
        /// </summary>
        public static readonly string showPreviewUssClassName = previewButtonUssClassName.WithUssModifier("show-preview");

        /// <summary>
        /// The USS class name added to the icon of a <see cref="ShowPreviewButton"/>.
        /// </summary>
        public static readonly string previewButtonIconElementUssClassName = previewButtonUssClassName.WithUssElement(GraphElementHelper.iconName);

        string m_ShowPreviewTooltip;
        string m_HidePreviewTooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowPreviewButton"/> class.
        /// </summary>
        /// <param name="buttonName">The name of the button, used to identify it.</param>
        /// <param name="showPreviewTooltip">The tooltip for showing the preview. By default, is "Show Preview".</param>
        /// <param name="hidePreviewTooltip">The tooltip for hiding the preview. By default, is "Hide Preview".</param>
        /// <param name="changeEventCallback">A callback after the button's value is changed.</param>
        /// <param name="showCallback">A callback after the button is shown on the node. Buttons are shown on hover, else they aren't.</param>
        public ShowPreviewButton(string buttonName, string showPreviewTooltip = "", string hidePreviewTooltip = "", EventCallback<ChangeEvent<bool>> changeEventCallback = null, Action<bool> showCallback = null)
            : base(buttonName, null, changeEventCallback, showCallback)
        {
            AddToClassList(previewButtonUssClassName);
            Icon.AddToClassList(previewButtonIconElementUssClassName);
            SetToolTip();

            m_ShowPreviewTooltip = showPreviewTooltip;
            m_HidePreviewTooltip = hidePreviewTooltip;
        }

        /// <inheritdoc />
        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            EnableInClassList(showPreviewUssClassName, m_Value);
            SetToolTip();
        }

        void SetToolTip()
        {
            var showTooltip = string.IsNullOrEmpty(m_ShowPreviewTooltip) ? "Show Preview" : m_ShowPreviewTooltip;
            var hideTooltip = string.IsNullOrEmpty(m_HidePreviewTooltip) ? "Hide Preview" : m_HidePreviewTooltip;
            tooltip = m_Value ? hideTooltip : showTooltip;
        }
    }
}
