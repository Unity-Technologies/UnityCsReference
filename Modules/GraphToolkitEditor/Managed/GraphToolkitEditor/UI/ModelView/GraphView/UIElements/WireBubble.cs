// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A rounded label used to display text on a wire. Position of the label follows another VisualElement.
    /// </summary>
    [UnityRestricted]
    internal class WireBubble : Label
    {
        /// <summary>
        /// The USS class name added to a <see cref="WireBubble"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-wire-bubble";

        protected Attacher m_Attacher;

        /// <summary>
        /// Initializes a new instance of the <see cref="WireBubble"/> class.
        /// </summary>
        public WireBubble()
        {
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Attaches the bubble to a target VisualElement.
        /// </summary>
        /// <param name="wireControlTarget">The VisualElement to attach the bubble to.</param>
        /// <param name="align">The alignment of the bubble relative to the target VisualElement.</param>
        public void AttachTo(VisualElement wireControlTarget, SpriteAlignment align)
        {
            if (m_Attacher?.Target == wireControlTarget && m_Attacher?.Alignment == align)
                return;

            Detach();

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_Attacher = new Attacher(this, wireControlTarget, align);
        }

        /// <summary>
        /// Detaches the bubble from the target VisualElement.
        /// </summary>
        public void Detach()
        {
            if (m_Attacher == null)
                return;

            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_Attacher.Detach();
            m_Attacher = null;
        }

        protected void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ResizeToFitText();
        }

        /// <summary>
        /// Sets the offset of the bubble relative to the target VisualElement.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public void SetAttacherOffset(Vector2 offset)
        {
            if (m_Attacher != null)
                m_Attacher.Offset = offset;
        }

        void ResizeToFitText()
        {
            if (style.fontSize == 0)
                return;

            var newSize = DoMeasure(resolvedStyle.maxWidth.value, MeasureMode.AtMost, 0, MeasureMode.Undefined);

            style.width = newSize.x +
                resolvedStyle.marginLeft +
                resolvedStyle.marginRight +
                resolvedStyle.borderLeftWidth +
                resolvedStyle.borderRightWidth +
                resolvedStyle.paddingLeft +
                resolvedStyle.paddingRight;

            style.height = newSize.y +
                resolvedStyle.marginTop +
                resolvedStyle.marginBottom +
                resolvedStyle.borderTopWidth +
                resolvedStyle.borderBottomWidth +
                resolvedStyle.paddingTop +
                resolvedStyle.paddingBottom;
        }
    }
}
