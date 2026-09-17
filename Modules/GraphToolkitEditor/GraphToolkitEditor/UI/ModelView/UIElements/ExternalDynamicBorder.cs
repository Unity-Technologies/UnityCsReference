// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
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
            UpdateMargins();
        }

        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> k_InsetProperty = new("--inset");

        /// <summary>
        /// The inset for the element bounds.
        /// </summary>
        public float Inset { get; private set; }

        /// <summary>
        /// How much further than <see cref="BaseMargin"/> the element extends past the bounds of the
        /// <see cref="ModelView"/> it decorates, in graph units.
        /// </summary>
        /// <remarks>
        /// Only affects how far the element reaches, not where the border is drawn: <see cref="AlterBounds"/>
        /// compensates for the whole margin, so the outline always lands on the edge of the decorated element.
        /// This is read while the base class constructor runs, so overrides must not rely on derived state
        /// being initialized.
        /// </remarks>
        protected virtual float ExtraMargin => 0f;

        /// <summary>
        /// The margin needed to draw the widest border this element can display, in graph units.
        /// </summary>
        protected float BaseMargin => (SmallSelectionWidth + HoverWidth) / k_MinZoom;

        float TotalMargin => BaseMargin + ExtraMargin;

        /// <summary>
        /// Positions the element so that it extends <see cref="TotalMargin"/> past the bounds of the
        /// <see cref="ModelView"/> it decorates, minus the element's own padding.
        /// </summary>
        protected void UpdateMargins()
        {
            var margin = TotalMargin;
            style.left = -margin + resolvedStyle.paddingLeft;
            style.right = -margin + resolvedStyle.paddingRight;
            style.bottom = -margin + resolvedStyle.paddingBottom;
            style.top = -margin + resolvedStyle.paddingTop;
        }

        /// <inheritdoc />
        protected override float GetCornerOffset(float width) => width;

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            base.OnCustomStyleResolved(e);

            if (e.customStyle.TryGetValue(k_InsetProperty, out var value))
                Inset = value;

            UpdateMargins();
        }

        /// <inheritdoc />
        protected override void AlterBounds(ref Rect bound, float width)
        {
            var margin = TotalMargin;
            bound.position += Vector2.one * (margin - width + Inset);
            bound.size -= Vector2.one * (margin - width + Inset) * 2;
        }
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
