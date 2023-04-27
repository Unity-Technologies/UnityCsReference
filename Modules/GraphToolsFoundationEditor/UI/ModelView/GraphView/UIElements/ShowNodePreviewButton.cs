// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A toggle button to show or hide the node preview.
    /// </summary>
    class ShowNodePreviewButton : ToggleIconButton
    {
        public static readonly string previewButtonUssClassName = "ge-show-node-preview-button";
        public static readonly string showPreviewUssClassName = previewButtonUssClassName.WithUssModifier("show-preview");
        public static readonly string previewButtonIconElementUssClassName = previewButtonUssClassName.WithUssElement(iconElementName);

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowNodePreviewButton"/> class.
        /// </summary>
        public ShowNodePreviewButton()
        {
            AddToClassList(previewButtonUssClassName);
            m_Icon.AddToClassList(previewButtonIconElementUssClassName);
            SetToolTip();
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
            tooltip = m_Value ? "Hide Node preview" : "Show Node preview";
        }
    }
}
