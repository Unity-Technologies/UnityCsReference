// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class ExternalDynamicBorder : DynamicBorder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalDynamicBorder"/> class.
        /// </summary>
        /// <param name="view">The <see cref="ModelView"/> on which the border will appear.</param>
        public ExternalDynamicBorder(ModelView view)
            : base(view)
        {
            float maxMargin = (SmallSelectionWidth + HoverWidth) / k_MinZoom;
            style.left = -maxMargin;
            style.right = -maxMargin;
            style.bottom = -maxMargin;
            style.top = -maxMargin;
        }

        static readonly CustomStyleProperty<float> k_InsetProperty = new("--inset");

        /// <summary>
        /// The inset for the element bounds.
        /// </summary>
        public float Inset { get; private set; }

        /// <inheritdoc />
        protected override float GetCornerOffset(float width) => width;

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            base.OnCustomStyleResolved(e);

            if (e.customStyle.TryGetValue(k_InsetProperty, out var value))
                Inset = value;
            float maxMargin = (SmallSelectionWidth + HoverWidth) / k_MinZoom;
            style.left = -maxMargin + resolvedStyle.paddingLeft;
            style.right = -maxMargin + resolvedStyle.paddingRight;
            style.bottom = -maxMargin + resolvedStyle.paddingBottom;
            style.top = -maxMargin + resolvedStyle.paddingTop;
        }

        /// <inheritdoc />
        protected override void AlterBounds(ref Rect bound, float width)
        {
            float maxMargin = (SmallSelectionWidth + HoverWidth) / k_MinZoom;
            bound.position += Vector2.one * (maxMargin - width + Inset);
            bound.size -= Vector2.one * (maxMargin - width + Inset) * 2;
        }
    }
}
