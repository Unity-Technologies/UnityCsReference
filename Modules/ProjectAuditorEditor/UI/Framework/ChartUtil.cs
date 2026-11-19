// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
            public string InfoText;
            public GUIContent ButtonContent;
            public Action ButtonAction;
            public string ValueText;

            public Element(string label, string tooltip, float value, Color color, GUIContent iconConent = null,
                string infoText = null, GUIContent buttonContent = null, Action buttonAction = null,
                string valueText = null)
            {
                ValueText = valueText;
                Label = label;
                Tooltip = tooltip;
                Value = value;
                Color = color;
                IconContent = iconConent;
                InfoText = infoText;
                ButtonContent = buttonContent;
                ButtonAction = buttonAction;
            }
        }

        // Neutral, translucent track drawn behind an all-zero bar.
        static readonly Color k_EmptyBarColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        const int k_NumberLabelWidth = 90;
        const int k_RowSize = 26;
        const int k_BarLegendSpacing = 4;
        const int k_LabelWidth = 150;
        const int k_ValueTextWidth = 165;
        const int k_InfoWidth = 195;
        const int k_ButtonWidth = 60;

        static readonly GUIStyle s_Row = new GUIStyle(GUIStyle.none)
        {
            normal = {background = Utility.MakeColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.0f))},
            fixedHeight = k_RowSize
        };

        public static void DrawHorizontalStackedBar(Draw2D draw2D, float barHeight, string title, List<Element> inValues,
            string labelFormat = "{0}", string numberFormat = "D", bool oneColumnLayout = false,
            bool skipZeroValues = false, bool showTotal = false, string extraTotalInfo = null)
        {
            EditorGUILayout.BeginVertical();

            if (!string.IsNullOrEmpty(title))
            {
                EditorGUILayout.LabelField(title, SharedStyles.WhiteLargeLabel);
                GUILayout.Space(10);
            }

            float totalValue = 0;
            for (int i = 0; i < inValues.Count; ++i)
                totalValue += inValues[i].Value;

            // Optional columns are reserved for every row so the items line up
            bool hasInfo = false;
            bool hasButton = false;
            bool hasValueText = false;
            for (int i = 0; i < inValues.Count; ++i)
            {
                hasInfo |= !string.IsNullOrEmpty(inValues[i].InfoText);
                hasButton |= inValues[i].ButtonContent != null;
                hasValueText |= !string.IsNullOrEmpty(inValues[i].ValueText);
            }

            int extraWidth = (hasInfo ? k_InfoWidth : 0) + (hasButton ? k_ButtonWidth : 0)
                + (hasValueText ? k_ValueTextWidth - k_NumberLabelWidth : 0);

            var rect = EditorGUILayout.GetControlRect(false, barHeight, GUILayout.ExpandWidth(true));
            int x = 0;

            for (int i = 0; i < inValues.Count; ++i)
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

            // Nothing to draw: still show the empty bar instead of a blank gap.
            if (totalValue <= 0 && draw2D.DrawStart(rect))
            {
                draw2D.DrawFilledBox(0, 1, rect.width - 2, rect.height - 1, k_EmptyBarColor);
                draw2D.DrawEnd();
            }

            GUILayout.Space(k_BarLegendSpacing);

            if (oneColumnLayout)
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(280 + extraWidth)))
                    {
                        for (int i = 0; i < inValues.Count; ++i)
                        {
                            if (skipZeroValues && inValues[i].Value == 0)
                                continue;

                            DrawLegendItem(draw2D, inValues[i], labelFormat, numberFormat, hasInfo, hasButton, hasValueText, false);
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (showTotal)
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                var totalInfo = $"Total: <b>{totalValue.ToString(numberFormat)}</b>";
                                GUILayout.FlexibleSpace();
                                GUILayout.Label(totalInfo, SharedStyles.LabelRichText);
                            }
                            if (extraTotalInfo != null)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    var extraInfo = $"<i>{extraTotalInfo}</i>";
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Label(extraInfo, SharedStyles.LabelRichTextGrey);
                                }
                            }
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                return;
            }

            if (skipZeroValues)
            {
                inValues = inValues.FindAll(e => e.Value > 0);
            }

            int firstColumnNum = (inValues.Count + 1) / 2;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 0; i < firstColumnNum; ++i)
                        DrawLegendItem(draw2D, inValues[i], labelFormat, numberFormat, hasInfo, hasButton, hasValueText);

                    DrawLine(draw2D);
                }

                GUILayout.Space(10);

                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = firstColumnNum; i < inValues.Count; ++i)
                        DrawLegendItem(draw2D, inValues[i], labelFormat, numberFormat, hasInfo, hasButton, hasValueText);

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

        static void DrawLegendItem(Draw2D draw2D, in Element element, string labelFormat, string numberFormat,
            bool hasInfo, bool hasButton, bool hasValueText, bool drawLines = true)
        {
            if (drawLines)
                DrawLine(draw2D);

            EditorGUILayout.BeginHorizontal(s_Row, GUILayout.Height(10), GUILayout.Width(120));

            if (element.IconContent != null)
            {
                EditorGUILayout.LabelField(element.IconContent, GUILayout.Width(28), GUILayout.Height(20));
            }
            else
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(12), GUILayout.Height(20));
                if (draw2D.DrawStart(rect))
                {
                    draw2D.DrawFilledBox(1, 5, 10, 10, element.Color);
                    draw2D.DrawEnd();
                }
            }

            EditorGUILayout.LabelField(new GUIContent(element.Label, element.Tooltip), SharedStyles.Label, GUILayout.Width(k_LabelWidth));
            var valueText = element.ValueText ?? string.Format(labelFormat, element.Value.ToString(numberFormat));
            EditorGUILayout.LabelField(new GUIContent(valueText, element.Tooltip), SharedStyles.BoldLabel,
                GUILayout.Width(hasValueText ? k_ValueTextWidth : k_NumberLabelWidth));

            if (hasInfo)
                EditorGUILayout.LabelField(element.InfoText, SharedStyles.LabelRichTextGrey, GUILayout.Width(k_InfoWidth));

            if (hasButton)
            {
                if (element.ButtonContent == null)
                    GUILayout.Space(k_ButtonWidth);
                else if (GUILayout.Button(element.ButtonContent, EditorStyles.miniButton, GUILayout.Width(k_ButtonWidth)))
                    element.ButtonAction?.Invoke();
            }

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
