// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.StyleSheets
{
    internal static class StylePainter
    {
        private static readonly int k_EnableHovering = "-unity-enable-hovering".GetHashCode();

        static Dictionary<StyleState, StyleState[]> s_StatesCache = new Dictionary<StyleState, StyleState[]>();

        internal static bool DrawStyle(GUIStyle gs, Rect position, GUIContent content, DrawStates states)
        {
            if (Highlighter.IsSearchingForIdentifier())
                Highlighter.HighlightIdentifier(position, content?.text);

            if (gs == GUIStyle.none || gs.blockId == -1 || String.IsNullOrEmpty(gs.name) || gs.normal.background != null)
                return false;

            if (!GUIClip.visibleRect.Overlaps(position))
                return true;

            if (gs.blockId == 0)
                gs.blockId = GUIStyleExtensions.StyleNameToBlockName(gs.name, false).GetHashCode();

            var block = FindBlock(gs.blockId, states);
            if (!block.IsValid())
            {
                gs.blockId = -1;
                return false;
            }

            DrawBlock(gs, block, position, content, states);

            return true;
        }

        private static bool IsMouseOverGUIView()
        {
            return GUIView.mouseOverView == GUIView.current;
        }

        internal static StyleBlock FindBlock(int name, DrawStates drawStates)
        {
            StyleState stateFlags = 0;
            if (GUI.enabled)
            {
                if (drawStates.hasKeyboardFocus && GUIView.current.hasFocus) stateFlags |= StyleState.focus;
                if (drawStates.isActive || GUI.HasMouseControl(drawStates.controlId)) stateFlags |= StyleState.active;
                if (drawStates.isHover && GUIUtility.hotControl == 0 && IsMouseOverGUIView()) stateFlags |= StyleState.hover;
            }
            else
            {
                stateFlags |= StyleState.disabled;
            }

            if (drawStates.on)
                stateFlags |= StyleState.@checked;

            StyleState[] states;
            if (!s_StatesCache.TryGetValue(stateFlags, out states))
            {
                states = new[] { stateFlags,
                                 stateFlags & StyleState.disabled,
                                 stateFlags & StyleState.active,
                                 stateFlags & StyleState.@checked,
                                 stateFlags & StyleState.hover,
                                 stateFlags & StyleState.focus,
                                 StyleState.normal}.Distinct().Where(s => s != StyleState.none).ToArray();
                s_StatesCache.Add(stateFlags, states);
            }

            return EditorResources.GetStyle(name, states);
        }

        private static readonly Dictionary<long, Texture2D> s_Gradients = new Dictionary<long, Texture2D>();
        private static Texture2D GenerateGradient(StyleFunctionCall call, Rect rect)
        {
            if (call.args.Count < 2)
            {
                Debug.LogError("Not enough linear gradient arguments.");
                return null;
            }

            int key = call.blockKey ^ call.valueKey ^ ((int)rect.width * 30 + (int)rect.height * 8);

            if (s_Gradients.ContainsKey(key) && s_Gradients[key] != null)
                return s_Gradients[key];

            if (s_Gradients.Count > 300)
            {
                while (s_Gradients.Count > 250)
                    s_Gradients.Remove(s_Gradients.Keys.ToList().First());
            }

            var width = (int)rect.width;
            var height = (int)rect.height;
            Gradient gradient = new Gradient();
            var gt = new Texture2D(width, height) {alphaIsTransparency = true};
            float angle = 0.0f;
            int valueStartIndex = 0;

            if (call.GetValueType(0, 0) == StyleValue.Type.Number)
            {
                angle = call.GetNumber(0, 0);
                valueStartIndex = 1;
            }

            var valueCount = call.args.Count - valueStartIndex;

            var colorKeys = new GradientColorKey[valueCount];
            var alphaKeys = new GradientAlphaKey[valueCount];

            float increment = valueCount <= 1 ? 1f : 1f / (valueCount - 1);
            float autoStep = 0;
            for (int i = 0; i < valueCount; ++i)
            {
                var valueIndex = valueStartIndex + i;
                var values = call.args[valueIndex];
                if (values.Length == 1 && values[0].type == StyleValue.Type.Color)
                {
                    colorKeys[i].color = call.GetColor(valueIndex, 0);
                    colorKeys[i].time = autoStep;
                }
                else if (values.Length == 2 && values[0].type == StyleValue.Type.Color && values[1].type == StyleValue.Type.Number)
                {
                    colorKeys[i].color = call.GetColor(valueIndex, 0);
                    colorKeys[i].time = call.GetNumber(valueIndex, 1);
                }
                else
                {
                    Debug.LogError("Invalid gradient value argument");
                }

                alphaKeys[i].time = colorKeys[i].time;
                alphaKeys[i].alpha = colorKeys[i].color.a;

                autoStep = Mathf.Clamp(autoStep + increment, 0f, 1f);
            }

            gradient.SetKeys(colorKeys, alphaKeys);

            var ratio = height / (float)width;
            var rad = Mathf.Deg2Rad * angle;
            float CosRad = Mathf.Cos(rad);
            float SinRad = Mathf.Sin(rad);

            var matrix = GetGradientRotMatrix(angle);

            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = width - 1; x >= 0; --x)
                {
                    var pixelPosition = new Vector2(x, y);
                    var recenteredPos = GetGradientCenterFrame(pixelPosition, width, height);
                    var scaleVec = GetGradientScaleVector(matrix, angle, width, height);
                    var scaledMatrix = GetGradientScaleMatrix(matrix, scaleVec);
                    var posInGradient = scaledMatrix.MultiplyVector(recenteredPos);
                    Color color = gradient.Evaluate(1f - (posInGradient.y / 2 + 0.5f));
                    gt.SetPixel(x, y, color);
                }
            }

            gt.Apply();
            s_Gradients[key] = gt;
            return gt;
        }

        private static Matrix4x4 GetGradientRotMatrix(float deg)
        {
            var radian = Mathf.Deg2Rad * deg;
            var matrix = new Matrix4x4();
            matrix[1, 1] = matrix[0, 0] = Mathf.Cos(radian);
            matrix[0, 1] = Mathf.Sin(radian);
            matrix[1, 0] = -matrix[0, 1];
            return matrix;
        }

        private static Matrix4x4 GetGradientScaleMatrix(Matrix4x4 matrix, Vector2 scaleVec)
        {
            var scaledMatrix = new Matrix4x4();
            for (var i = 0; i < 2; ++i)
                for (var j = 0; j < 2; ++j)
                    scaledMatrix[i, j] = matrix[i, j] * scaleVec[i];
            return scaledMatrix;
        }

        private static Vector2 GetGradientScaleVector(Matrix4x4 matrix, float deg, float width, float height)
        {
            float halfHeight = height / 2f;
            float halfWidth = width / 2f;
            Vector2 extent1;
            Vector2 extent2;
            if (deg < 90)
            {
                extent1 = new Vector2(-halfWidth, halfHeight);
                extent2 = new Vector2(halfWidth, halfHeight);
            }
            else if (deg < 180)
            {
                extent1 = new Vector2(-halfWidth, -halfHeight);
                extent2 = new Vector2(-halfWidth, halfHeight);
            }
            else if (deg < 270)
            {
                extent1 = new Vector2(halfWidth, -halfHeight);
                extent2 = new Vector2(-halfWidth, -halfHeight);
            }
            else
            {
                extent1 = new Vector2(halfWidth, halfHeight);
                extent2 = new Vector2(halfWidth, -halfHeight);
            }
            var vec1 = matrix.MultiplyVector(extent1);
            var vec2 = matrix.MultiplyVector(extent2);
            return new Vector2(1f / Math.Abs(vec2[0]), 1f / Math.Abs(vec1[1]));
        }

        private static Vector2 GetGradientCenterFrame(Vector2 pos, float width, float height)
        {
            return new Vector2(pos.x - width / 2f, height / 2f - pos.y);
        }

        internal readonly struct GradientParams
        {
            public GradientParams(Rect _r, Vector4 _b, Color _c)
            {
                rect = _r;
                radius = _b;
                colorTint = _c;
            }

            public readonly Rect rect;
            public readonly Vector4 radius;
            public readonly Color colorTint;
        }

        internal static void DrawBlock(GUIStyle basis, StyleBlock block, Rect drawRect, GUIContent content, DrawStates states)
        {
            var userRect = drawRect;

            StyleRect offset = block.GetRect(StyleCatalogKeyword.position);
            drawRect.xMin += offset.left;
            drawRect.yMin += offset.top;
            drawRect.yMax += offset.bottom;
            drawRect.xMax += offset.right;

            // Adjust width and height if enforced by style block
            drawRect.width = basis.fixedWidth == 0f ? drawRect.width : basis.fixedWidth;
            drawRect.height = basis.fixedHeight == 0f ? drawRect.height : basis.fixedHeight;

            Color colorTint = GUI.color;
            if (!GUI.enabled)
                colorTint.a *= block.GetFloat(StyleCatalogKeyword.opacity, 0.5f);
            var border = new StyleBorder(block);
            var bgColorTint = GUI.backgroundColor * colorTint;

            if (!block.Execute(StyleCatalogKeyword.background, DrawGradient, new GradientParams(drawRect, border.radius, bgColorTint)))
            {
                // Draw background color
                var backgroundColor = block.GetColor(StyleCatalogKeyword.backgroundColor);
                if (backgroundColor.a > 0f)
                {
                    var smoothCorners = !border.all;
                    GUI.DrawTexture(drawRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0f, backgroundColor * bgColorTint, Vector4.zero, border.radius, smoothCorners);
                }
            }

            // Draw background image
            if (block.HasValue(StyleCatalogKeyword.backgroundImage))
                DrawBackgroundImage(block, drawRect, bgColorTint);

            if (content != null)
            {
                var guiContentColor = GUI.contentColor;

                // Compute content rect
                Rect contentRect = drawRect;
                if (block.GetKeyword(StyleCatalogKeyword.padding) == StyleValue.Keyword.Auto)
                    GetContentCenteredRect(basis, content, ref contentRect);

                // Draw content (text & image)
                bool hasImage = content.image != null;
                float opacity = hasImage ? block.GetFloat(StyleCatalogKeyword.opacity, 1f) : 1f;
                float contentImageOffsetX = hasImage ? block.GetFloat(StyleCatalogKeyword.contentImageOffsetX) : 0;
                float contentImageOffsetY = hasImage ? block.GetFloat(StyleCatalogKeyword.contentImageOffsetY) : 0;
                basis.Internal_DrawContent(contentRect, content, states.isHover, states.isActive, states.on, states.hasKeyboardFocus,
                    states.hasTextInput, states.drawSelectionAsComposition, states.cursorFirst, states.cursorLast,
                    states.cursorColor, states.selectionColor, guiContentColor * opacity,
                    0, 0, contentImageOffsetY, contentImageOffsetX, false, false);

                // Handle tooltip and hovering region
                if (!String.IsNullOrEmpty(content.tooltip) && contentRect.Contains(Event.current.mousePosition))
                    GUIStyle.SetMouseTooltip(content.tooltip, contentRect);
            }

            // Draw border
            if (border.any)
            {
                GUI.DrawTexture(drawRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, border.borderLeftColor * colorTint,
                    border.borderTopColor * colorTint, border.borderRightColor * colorTint, border.borderBottomColor * colorTint, border.widths, border.radius);
            }

            if (block.GetKeyword(k_EnableHovering) == StyleValue.Keyword.True)
            {
                var currentView = GUIView.current;

                if (currentView != null)
                {
                    currentView.MarkHotRegion(GUIClip.UnclipToWindow(userRect));
                }
            }
        }

        // Note: Assign lambda to local variable to avoid the allocation caused by method group.
        private static readonly Func<StyleFunctionCall, GradientParams, bool> DrawGradient = (call, gp) =>
        {
            if (call.name != "linear-gradient")
                return false;

            var gradientTexture = GenerateGradient(call, gp.rect);
            if (gradientTexture == null)
                return false;
            GUI.DrawTexture(gp.rect, gradientTexture, ScaleMode.ScaleAndCrop, true, 0f, gp.colorTint, Vector4.zero, gp.radius);
            return true;
        };

        private static void DrawBackgroundImage(StyleBlock block, Rect position, Color colorTint)
        {
            var backgroundImage = block.GetTexture(StyleCatalogKeyword.backgroundImage, true);
            if (backgroundImage == null)
                return;

            var backgroundPosition = block.GetStruct<StyleBackgroundPosition>(StyleCatalogKeyword.backgroundPosition);
            var backgroundSize = block.GetRect(StyleCatalogKeyword.backgroundSize, StyleRect.Size(backgroundImage.width, backgroundImage.height));

            StyleRect anchor = StyleRect.Nil;
            if (backgroundPosition.xEdge == StyleCatalogKeyword.left) anchor.left = backgroundPosition.xOffset;
            else if (backgroundPosition.yEdge == StyleCatalogKeyword.left) anchor.left = backgroundPosition.yOffset;
            if (backgroundPosition.xEdge == StyleCatalogKeyword.right) anchor.right = backgroundPosition.xOffset;
            else if (backgroundPosition.yEdge == StyleCatalogKeyword.right) anchor.right = backgroundPosition.yOffset;
            if (backgroundPosition.xEdge == StyleCatalogKeyword.top) anchor.top = backgroundPosition.xOffset;
            else if (backgroundPosition.yEdge == StyleCatalogKeyword.top) anchor.top = backgroundPosition.yOffset;
            if (backgroundPosition.xEdge == StyleCatalogKeyword.bottom) anchor.bottom = backgroundPosition.xOffset;
            else if (backgroundPosition.yEdge == StyleCatalogKeyword.bottom) anchor.bottom = backgroundPosition.yOffset;

            var bgRect = new Rect(position.xMin, position.yMin, backgroundSize.width, backgroundSize.height);
            if (anchor.left < 1.0f)
                bgRect.xMin = position.xMin + position.width * anchor.left - backgroundSize.width / 2f;
            else if (anchor.left >= 1.0f)
                bgRect.xMin = position.xMin + anchor.left;

            if (anchor.top < 1.0f)
                bgRect.yMin = position.yMin + position.height * anchor.top - backgroundSize.height / 2f;
            else if (anchor.top >= 1.0f)
                bgRect.yMin = position.yMin + anchor.top;

            if (anchor.right == 0f || anchor.right >= 1.0f)
                bgRect.xMin = position.xMax - backgroundSize.width - anchor.right;
            else if (anchor.right < 1.0f)
                bgRect.xMin = position.xMax - position.width * anchor.right - backgroundSize.width / 2f;

            if (anchor.bottom == 0f || anchor.bottom >= 1.0f)
                bgRect.yMin = position.yMax - backgroundSize.height - anchor.bottom;
            if (anchor.bottom < 1.0f)
                bgRect.yMin = position.yMax - position.height * anchor.bottom - backgroundSize.height / 2f;

            bgRect.width = backgroundSize.width;
            bgRect.height = backgroundSize.height;

            using (new GUI.ColorScope(colorTint))
                GUI.DrawTexture(bgRect, backgroundImage);
        }

        private static void GetContentCenteredRect(GUIStyle gs, GUIContent content, ref Rect contentRect)
        {
            var initialRect = contentRect;
            var contentSize = gs.CalcSize(content);
            contentRect.xMin = Mathf.Max(initialRect.xMin, initialRect.xMin + (initialRect.width - contentSize.x) / 2f);
            contentRect.xMax = Mathf.Min(initialRect.xMax, contentRect.xMin + contentSize.x);
            contentRect.yMin = Mathf.Max(initialRect.yMin, initialRect.yMin + (initialRect.height - contentSize.y) / 2f);
            contentRect.yMax = Mathf.Min(initialRect.yMax, contentRect.yMin + contentSize.y);
        }

        private struct StyleBackgroundPosition
        {
            #pragma warning disable 0649
            public int xEdge;
            public float xOffset;
            public int yEdge;
            public float yOffset;
            #pragma warning restore 0649
        }

        private readonly struct StyleBorder
        {
            // borderWidths The width of the borders(left, top, right and bottom). If Vector4.zero, the full texture is drawn.
            // borderRadiuses The radiuses for rounded corners (top-left, top-right, bottom-right and bottom-left). If Vector4.zero, corners will not be rounded.
            public StyleBorder(StyleBlock block)
            {
                if (block.HasValue(StyleCatalogKeyword.border))
                {
                    var defaultColor = block.GetColor(StyleCatalogKeyword.borderColor);

                    var borderWidths = block.GetRect(StyleCatalogKeyword.borderWidth);
                    widths = new Vector4(borderWidths.left, borderWidths.top, borderWidths.right, borderWidths.bottom);
                    borderLeftColor = block.GetColor(StyleCatalogKeyword.borderLeftColor, defaultColor);
                    borderTopColor = block.GetColor(StyleCatalogKeyword.borderTopColor, defaultColor);
                    borderRightColor = block.GetColor(StyleCatalogKeyword.borderRightColor, defaultColor);
                    borderBottomColor = block.GetColor(StyleCatalogKeyword.borderBottomColor, defaultColor);
                }
                else
                {
                    widths = Vector4.zero;
                    borderLeftColor = borderTopColor = borderRightColor = borderBottomColor = Color.cyan;
                }

                var borderRadius = block.GetRect(StyleCatalogKeyword.borderRadius);
                radius = new Vector4(borderRadius.top, borderRadius.right, borderRadius.bottom, borderRadius.left);
            }

            public bool any => widths != Vector4.zero;

            public bool all
            {
                get
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        if (widths[i] < 1f)
                            return false;
                    }

                    return true;
                }
            }

            public readonly Color borderLeftColor;
            public readonly Color borderTopColor;
            public readonly Color borderRightColor;
            public readonly Color borderBottomColor;
            public readonly Vector4 widths;
            public readonly Vector4 radius;
        }
    }
}
