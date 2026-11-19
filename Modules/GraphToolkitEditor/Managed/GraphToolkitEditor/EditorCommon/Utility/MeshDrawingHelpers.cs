// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    static class MeshDrawingHelpers
    {
        public static void SolidRectangle(MeshGenerationContext mgc, Rect rectParams, Color color, float radius = 0f)
        {
            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            DrawRect(mgc.painter2D, rectParams, color , radius, 0f,  true,  playModeTintColor);
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color color, float radius, float width)
        {
            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            DrawRect(mgc.painter2D, rectParams, color , radius, width, false, playModeTintColor);
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color color, float borderWidth, Vector2[] radii)
        {
            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            DrawBorder(mgc.painter2D, rectParams, color, radii, borderWidth, playModeTintColor);
        }

        static void DrawRect(Painter2D painter2D, Rect rectParams, Color color, float radius, float width, bool fill, Color playModeTint)
        {
            DrawRect(painter2D,
                new Vector2(rectParams.x, rectParams.y),
                new Vector2(rectParams.x + rectParams.width, rectParams.y),
                new Vector2(rectParams.x + rectParams.width, rectParams.y + rectParams.height),
                new Vector2(rectParams.x, rectParams.y + rectParams.height),
                color, radius, width, fill, playModeTint);
        }

        static void DrawRect(Painter2D painter2D, Vector2 A, Vector2 B, Vector2 C, Vector2 D, Color color, float radius, float width, bool fill, Color playModeTint)
        {
            painter2D.BeginPath();

            if (fill)
                painter2D.fillColor = color * playModeTint;
            else
            {
                painter2D.strokeColor = color * playModeTint;
                painter2D.lineWidth = width;
            }

            // If radius is greater than zero, draw rounded corners.
            if (radius > 0f)
            {
                painter2D.MoveTo((A + B) / 2);
                painter2D.ArcTo(B, (B + C) / 2, radius);
                painter2D.ArcTo(C, (C + D) / 2, radius);
                painter2D.ArcTo(D, (D + A) / 2, radius);
                painter2D.ArcTo(A, (A + B) / 2, radius);
            }
            else
            {
                // Draw a rectangle, no rounded corners.
                painter2D.MoveTo(A);
                painter2D.LineTo(B);
                painter2D.LineTo(C);
                painter2D.LineTo(D);
                painter2D.LineTo(A);
            }

            painter2D.ClosePath();
            
            if (fill)
                painter2D.Fill();
            else
                painter2D.Stroke();
        }

        static void DrawBorder(Painter2D painter2D, Rect rectParams, Color color, Vector2[] radii, float width, Color playModeTint)
        {
            DrawBorder(painter2D,
                new Vector2(rectParams.x, rectParams.y),
                new Vector2(rectParams.x + rectParams.width, rectParams.y),
                new Vector2(rectParams.x + rectParams.width, rectParams.y + rectParams.height),
                new Vector2(rectParams.x, rectParams.y + rectParams.height),
                color, radii, width, playModeTint);
        }

        static void DrawBorder(Painter2D painter2D, Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl, Color color, Vector2[] radii, float width, Color playModeTint)
        {
            painter2D.BeginPath();
            painter2D.MoveTo((tl + tr) / 2);
            painter2D.lineWidth = width;
            painter2D.strokeColor = color * playModeTint;

            // If all radii are zero, draw a rectangle, no rounded corners.
            if (radii[0].x > 0 && radii[1].x > 0 && radii[2].x > 0 && radii[3].x > 0)
            {
                painter2D.ArcTo(tr, (tr + br) / 2, radii[1].x);
                painter2D.ArcTo(br, (br + bl) / 2, radii[2].x);
                painter2D.ArcTo(bl, (bl + tl) / 2, radii[3].x);
                painter2D.ArcTo(tl, (tl + tr) / 2, radii[0].x);
            }
            else
            {
                painter2D.LineTo(tr);
                painter2D.LineTo(br);
                painter2D.LineTo(bl);
                painter2D.LineTo(tl);
            }

            painter2D.ClosePath();
            painter2D.Stroke();
        }
    }
}
