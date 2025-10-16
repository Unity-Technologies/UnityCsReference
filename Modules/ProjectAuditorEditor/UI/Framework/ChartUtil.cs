// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal static class ChartUtil
    {
        public struct Element
        {
            public string Label;
            public string Tooltip;
            public float Value;
            public Color Color;
            public GUIContent IconContent;

            public Element(string label, string tooltip, float value, Color color, GUIContent iconConent = null)
            {
                Label = label;
                Tooltip = tooltip;
                Value = value;
                Color = color;
                IconContent = iconConent;
            }
        }

        const int k_NumberLabelWidth = 60;
        const int k_RowSize = 22;
        const int k_LabelWidth = 80;

        static readonly GUIStyle s_Row = new GUIStyle(GUIStyle.none)
        {
            normal = {background = Utility.MakeColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.0f))},
            fixedHeight = k_RowSize
        };

        public static void DrawHorizontalStackedBar(Draw2D draw2D, float barHeight, string title, Element[] inValues,
            string labelFormat = "{0}", string numberFormat = "D", bool oneColumnLayout = false,
            bool skipZeroValues = false, bool showTotal = false)
        {
            EditorGUILayout.BeginVertical();

            if (!string.IsNullOrEmpty(title))
            {
                EditorGUILayout.LabelField(title, SharedStyles.WhiteLargeLabel);
                GUILayout.Space(10);
            }

            float totalValue = 0;
            for (int i = 0; i < inValues.Length; ++i)
                totalValue += inValues[i].Value;

            var rect = EditorGUILayout.GetControlRect(false, barHeight, GUILayout.ExpandWidth(true));
            int x = 0;

            for (int i = 0; i < inValues.Length; ++i)
            {
                var value = inValues[i].Value;

                if (value > 0 && draw2D.DrawStart(rect))
                {
                    var barWidth = (int)Math.Max(1f, rect.width * value / totalValue);
                    draw2D.DrawFilledBox(x, 1,  barWidth - 2, rect.height - 1, inValues[i].Color);
                    draw2D.DrawEnd();

                    x += barWidth;
                }
            }

            if (oneColumnLayout)
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(200)))
                    {
                        for (int i = 0; i < inValues.Length; ++i)
                        {
                            if (skipZeroValues && inValues[i].Value == 0)
                                continue;

                            DrawLegendItem(draw2D, inValues[i].Label, inValues[i].Tooltip, inValues[i].Value,
                                inValues[i].Color, labelFormat, numberFormat, inValues[i].IconContent, false);
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (showTotal)
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            var totalInfo = "Total: <b>" + totalValue.ToString(numberFormat) + "</b>";
                            GUILayout.Label(totalInfo, SharedStyles.LabelRichText);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                return;
            }

            if (skipZeroValues)
            {
                inValues = Array.FindAll(inValues, e => e.Value > 0);
            }

            int firstColumnNum = (inValues.Length + 1) / 2;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 0; i < firstColumnNum; ++i)
                    {
                        DrawLegendItem(draw2D, inValues[i].Label, inValues[i].Tooltip, inValues[i].Value, inValues[i].Color, labelFormat,
                            numberFormat, inValues[i].IconContent);
                    }

                    DrawLine(draw2D);
                }

                GUILayout.Space(10);

                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = firstColumnNum; i < inValues.Length; ++i)
                    {
                        DrawLegendItem(draw2D, inValues[i].Label, inValues[i].Tooltip, inValues[i].Value, inValues[i].Color, labelFormat,
                            numberFormat, inValues[i].IconContent);
                    }

                    DrawLine(draw2D);
                }

                if (showTotal)
                {
                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(4);

                        var totalInfo = "Total: <b>" + totalValue.ToString(numberFormat) + "</b>";
                        GUILayout.Label(totalInfo, SharedStyles.LabelRichText);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        static void DrawLegendItem(Draw2D draw2D, string text, string tooltip, float count, Color color,
            string labelFormat = "{0}", string numberFormat = "#", GUIContent iconContent = null, bool drawLines = true)
        {
            if (drawLines)
                DrawLine(draw2D);

            EditorGUILayout.BeginHorizontal(s_Row, GUILayout.Height(10), GUILayout.Width(120));

            if (iconContent != null)
            {
                EditorGUILayout.LabelField(iconContent,
                    GUILayout.Width(28), GUILayout.Height(20));
            }
            else
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(10), GUILayout.Height(20));
                if (draw2D.DrawStart(rect))
                {
                    var alphaColor = new Color(color.r, color.g, color.b, color.a * 0.6f);
                    draw2D.DrawFilledCircle(4, 9, 4.5f, alphaColor, 6);
                    draw2D.DrawFilledCircle(4, 9, 4, color, 6);
                    draw2D.DrawEnd();
                }
            }

            EditorGUILayout.LabelField(new GUIContent(text, tooltip), SharedStyles.Label, GUILayout.Width(k_LabelWidth));
            EditorGUILayout.LabelField(new GUIContent(String.Format(labelFormat, count.ToString(numberFormat)), tooltip), SharedStyles.BoldLabel, GUILayout.Width(k_NumberLabelWidth));

            EditorGUILayout.EndHorizontal();
        }

        public static void DrawLine(Draw2D draw2D)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            var color = new Color(0.3f, 0.3f, 0.3f);

            if (draw2D.DrawStart(rect))
            {
                draw2D.DrawLine(0, 0, rect.width, 0, color);
                draw2D.DrawEnd();
            }
        }
    }
}
