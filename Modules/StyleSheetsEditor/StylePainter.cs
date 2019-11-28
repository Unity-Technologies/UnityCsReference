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
        private static readonly int k_BorderTopLeftRadiusKey = "border-top-left-radius".GetHashCode();
        private static readonly int k_BorderTopRightRadiusKey = "border-top-right-radius".GetHashCode();
        private static readonly int k_BorderBottomLeftRadiusKey = "border-bottom-left-radius".GetHashCode();
        private static readonly int k_BorderBottomRightRadiusKey = "border-bottom-right-radius".GetHashCode();

        static Dictionary<StyleState, StyleState[]> s_StatesCache = new Dictionary<StyleState, StyleState[]>();

        internal static bool DrawStyle(GUIStyle gs, Rect position, GUIContent content, DrawStates states)
        {
            if (gs == GUIStyle.none || gs.normal.background != null)
                return false;

            if (!GUIClip.visibleRect.Overlaps(position))
                return true;

            if (gs.blockId == 0)
            {
                var blockName = GUIStyleExtensions.StyleNameToBlockName(gs.name, false);
                gs.blockId = blockName.GetHashCode();
            }

            var block = FindBlock(gs.blockId, states);
            if (!block.IsValid())
                return false;

            DrawBlock(gs, block, position, content, states);

            return true;
        }

        internal static StyleBlock FindBlock(int name, DrawStates drawStates)
        {
            StyleState stateFlags = 0;
            if (GUI.enabled)
            {
                if (drawStates.hasKeyboardFocus && GUIView.current.hasFocus) stateFlags |= StyleState.focus;
                if (drawStates.isActive || GUI.HasMouseControl(drawStates.controlId)) stateFlags |= StyleState.active;
                if (drawStates.isHover && GUIUtility.hotControl == 0) stateFlags |= StyleState.hover;
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
        private static Texture2D GenerateGradient(int key, Rect rect, IList<StyleSheetResolver.Value[]> args)
        {
            if (args.Count < 2)
            {
                Debug.LogError("Not enough linear gradient arguments.");
                return null;
            }

            key += (int)rect.width * 30 + (int)rect.height * 8;

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

            if (args[0][0].IsFloat())
            {
                angle = args[0][0].AsFloat();
                valueStartIndex = 1;
            }

            var valueCount = args.Count - valueStartIndex;

            var colorKeys = new GradientColorKey[valueCount];
            var alphaKeys = new GradientAlphaKey[valueCount];

            float increment = valueCount <= 1 ? 1f : 1f / (valueCount - 1);
            float autoStep = 0;
            for (int i = 0; i < valueCount; ++i)
            {
                var values = args[i + valueStartIndex];
                if (values.Length == 1)
                {
                    colorKeys[i].color = values[0].AsColor();
                    colorKeys[i].time = autoStep;
                }
                else if (values.Length == 2)
                {
                    colorKeys[i].color = values[0].AsColor();
                    colorKeys[i].time = values[1].AsFloat();
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

        internal struct GradientParams
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
            Color colorTint = GUI.color;
            if (!GUI.enabled)
                colorTint.a *= block.GetFloat(StyleCatalogKeyword.opacity, 0.5f);

            StyleRect offset = new StyleRect
            {
                top = block.GetFloat(StyleCatalogKeyword.top),
                left = block.GetFloat(StyleCatalogKeyword.left),
                bottom = block.GetFloat(StyleCatalogKeyword.bottom),
                right = block.GetFloat(StyleCatalogKeyword.right)
            };

            var userRect = drawRect;

            drawRect.xMin += offset.left;
            drawRect.yMin += offset.top;
            drawRect.yMax += offset.bottom;
            drawRect.xMax += offset.right;

            // Adjust width and height if enforced by style block
            drawRect.width = basis.fixedWidth == 0f ? drawRect.width : basis.fixedWidth;
            drawRect.height = basis.fixedHeight == 0f ? drawRect.height : basis.fixedHeight;

            var border = new StyleBorder(block);

            var guiBgColor = GUI.backgroundColor;
            var guiContentColor = GUI.contentColor;
            var bgColorTint = guiBgColor * colorTint;

            if (!block.Execute(StyleCatalogKeyword.background, DrawGradient, new GradientParams(drawRect, border.radius, bgColorTint)))
            {
                var smoothCorners = !border.all;

                // Draw background color
                var backgroundColor = block.GetColor(StyleCatalogKeyword.backgroundColor);
                if (backgroundColor.a > 0f)
                {
                    GUI.DrawTexture(drawRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0f, backgroundColor * bgColorTint, Vector4.zero, border.radius, smoothCorners);
                }
            }

            // Draw background image
            if (block.HasValue(StyleCatalogKeyword.backgroundImage))
                DrawBackgroundImage(block, drawRect, bgColorTint);

            if (content != null)
            {
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
        private static readonly Func<StyleBlock, string, List<StyleSheetResolver.Value[]>, GradientParams, bool> DrawGradient = (block, funcName, args, gp) =>
        {
            if (funcName != "linear-gradient")
                return false;

            var gradientTexture = GenerateGradient(block.name, gp.rect, args);
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

        private struct StyleBorder
        {
            // borderWidths The width of the borders(left, top, right and bottom). If Vector4.zero, the full texture is drawn.
            // borderRadiuses The radiuses for rounded corners (top-left, top-right, bottom-right and bottom-left). If Vector4.zero, corners will not be rounded.
            public StyleBorder(StyleBlock block)
            {
                var defaultColor = block.GetColor(StyleCatalogKeyword.borderColor);
                var defaultRadius = block.GetFloat(StyleCatalogKeyword.borderRadius);
                var borderWidth = block.GetFloat(StyleCatalogKeyword.borderWidth);
                widths = new Vector4(
                    block.GetFloat(StyleCatalogKeyword.borderLeftWidth, borderWidth),
                    block.GetFloat(StyleCatalogKeyword.borderTopWidth, borderWidth),
                    block.GetFloat(StyleCatalogKeyword.borderRightWidth, borderWidth),
                    block.GetFloat(StyleCatalogKeyword.borderBottomWidth, borderWidth));
                radius = new Vector4(
                    block.GetFloat(k_BorderTopLeftRadiusKey, defaultRadius),
                    block.GetFloat(k_BorderTopRightRadiusKey, defaultRadius),
                    block.GetFloat(k_BorderBottomRightRadiusKey, defaultRadius),
                    block.GetFloat(k_BorderBottomLeftRadiusKey, defaultRadius));

                borderLeftColor = block.GetColor(StyleCatalogKeyword.borderLeftColor, defaultColor);
                borderTopColor = block.GetColor(StyleCatalogKeyword.borderTopColor, defaultColor);
                borderRightColor = block.GetColor(StyleCatalogKeyword.borderRightColor, defaultColor);
                borderBottomColor = block.GetColor(StyleCatalogKeyword.borderBottomColor, defaultColor);
            }

            public bool any
            {
                get
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        if (widths[i] >= 1f)
                            return true;
                    }

                    return false;
                }
            }

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
