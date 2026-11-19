// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The dynamic border of <see cref="Editor.ErrorMarker"/>s.
    /// </summary>
    class DynamicErrorMarkerBorder : DynamicBorder
    {
        /// <summary>
        /// The <see cref="Editor.ErrorMarker"/> this border is for.
        /// </summary>
        ErrorMarker ErrorMarker { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="DynamicErrorMarkerBorder"/> class.
        /// </summary>
        /// <param name="errorMarker">The error marker.</param>
        public DynamicErrorMarkerBorder(ErrorMarker errorMarker)
            : base(errorMarker)
        {
            ErrorMarker = errorMarker;
        }

        /// <inheritdoc />
        protected override void DrawBorder(MeshGenerationContext mgc, Rect r, float wantedWidth, Color[] colors, Vector2[] corners)
        {
            DrawBorder(mgc.painter2D, wantedWidth, colors[0]);
        }

        void DrawBorder(Painter2D p2d, float wantedWidth, Color color)
        {
            ErrorMarker.DrawErrorMarker(p2d, true);
            p2d.strokeColor = color;
            p2d.lineWidth = wantedWidth;
            p2d.Stroke();
        }
    }
}
