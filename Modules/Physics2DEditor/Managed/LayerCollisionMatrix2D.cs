// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class LayerCollisionMatrixGUI2D
    {
        static class Styles
        {
            public static readonly GUIStyle rightLabel = new GUIStyle("RightLabel");
            public static readonly GUIStyle hoverStyle = GetHoverStyle();
        }

        private static Color transparentColor = new Color(1, 1, 1, 0);
        private static Color highlightColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.2f) : new Color(0,0,0, 0.2f);

        public delegate bool GetValueFunc(int layerA, int layerB);
        public delegate void SetValueFunc(int layerA, int layerB, bool val);

        const int kMaxLayers = 32;

        // Get the styled used when hovering over the rows/columns.
        public static GUIStyle GetHoverStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);

            var texNormal = new Texture2D(1,1) { alphaIsTransparency = true };
            texNormal.SetPixel(1, 1, transparentColor);
            texNormal.Apply();

            var texHover = new Texture2D(1, 1) { alphaIsTransparency = true };
            texHover.SetPixel(1, 1, highlightColor);
            texHover.Apply();

            style.normal.background = texNormal;
            style.hover.background = texHover;

            return style;
        }

        // Draw the whole collision matrix view.
        public static void Draw(GUIContent label, GetValueFunc getValue, SetValueFunc setValue)
        {
            const int checkboxSize = 16;
            var labelSize = 110;
            const int indent = 30;

            // Count the number of active layers.
            var activeLayerCount = 0;
            for (var i = 0; i < kMaxLayers; ++i)
            {
                if (LayerMask.LayerToName(i) != string.Empty)
                    activeLayerCount++;
            }

            // Find the longest label
            for (var i = 0; i < kMaxLayers; ++i)
            {
                var textDimensions = GUI.skin.label.CalcSize(new GUIContent(LayerMask.LayerToName(i)));
                if (labelSize < textDimensions.x)
                    labelSize = (int)textDimensions.x;
            }

            {
                GUILayout.BeginScrollView(new Vector2(0, 0), GUILayout.Height(labelSize + 15));
                var topLabelRect = GUILayoutUtility.GetRect(checkboxSize * activeLayerCount + labelSize, labelSize);
                var scrollArea = GUIClip.topmostRect;
                var topLeft = GUIClip.Unclip(new Vector2(topLabelRect.x - 10, topLabelRect.y));
                var y = 0;

                for (var i = 0; i < kMaxLayers; ++i)
                {
                    if (LayerMask.LayerToName(i) != "")
                    {
                        var hidelabel = false;
                        var hidelabelonscrollbar = false;
                        var defautlabelrectwidth = 311;
                        var defaultlablecount  = 10;
                        var clipOffset = labelSize + (checkboxSize * activeLayerCount) + checkboxSize;

                        // Hide vertical labels when they overlap with the rest of the UI.
                        if ((topLeft.x + (clipOffset - checkboxSize * y)) <= 0)
                            hidelabel = true;

                        // Hide label when it touches the horizontal scroll area.
                        if (topLabelRect.height > scrollArea.height)
                        {
                            hidelabelonscrollbar = true;
                        }
                        else if (topLabelRect.width != scrollArea.width || topLabelRect.width != scrollArea.width - topLeft.x)
                        {
                            // Hide label when it touch vertical scroll area.
                            if (topLabelRect.width > defautlabelrectwidth)
                            {
                                var tmp = topLabelRect.width - scrollArea.width;
                                if (tmp > 1)
                                {
                                    if (topLeft.x < 0)
                                        tmp += topLeft.x;

                                    if (tmp / checkboxSize > y)
                                        hidelabelonscrollbar = true;
                                }
                            }
                            else
                            {
                                var tmp = defautlabelrectwidth;
                                if (activeLayerCount < defaultlablecount)
                                {
                                    tmp -= checkboxSize * (defaultlablecount - activeLayerCount);
                                }

                                if ((scrollArea.width + y * checkboxSize) + checkboxSize <= tmp)
                                    hidelabelonscrollbar = true;

                                // Reenable the label when we move the scroll bar.
                                if (topLeft.x < 0)
                                {
                                    if (topLabelRect.width == scrollArea.width - topLeft.x)
                                        hidelabelonscrollbar = false;

                                    if (activeLayerCount <= defaultlablecount / 2)
                                    {
                                        if ((tmp - (scrollArea.width - ((topLeft.x - 10) * (y + 1)))) < 0)
                                            hidelabelonscrollbar = false;
                                    }
                                    else
                                    {
                                        float hiddenlables = (int)(tmp - scrollArea.width) / checkboxSize;
                                        int res = (int)((topLeft.x * -1) + 12) / checkboxSize;
                                        if (hiddenlables - res < y)
                                            hidelabelonscrollbar = false;
                                    }
                                }
                            }
                        }

                        var translate = new Vector3(labelSize + indent + checkboxSize * (activeLayerCount - y) + topLeft.y + topLeft.x + 10, topLeft.y, 0);
                        GUI.matrix = Matrix4x4.TRS(translate, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 90), Vector3.one);

                        var labelRect = new Rect(2 - topLeft.x, 0, labelSize, checkboxSize + 5);
                        if (hidelabel || hidelabelonscrollbar)
                        {
                            GUI.Label(labelRect, GUIContent.none, Styles.rightLabel);
                        }
                        else
                        {
                            GUI.Label(labelRect, LayerMask.LayerToName(i), Styles.rightLabel);

                            // Empty Transparent label used to indicate highlighted row
                            var checkRect = new Rect(2 - topLeft.x, 1  /*This centers the highlight*/ , labelSize + 4 + (y + 1) * checkboxSize, checkboxSize);
                            GUI.Label(checkRect, GUIContent.none, Styles.hoverStyle);

                            checkRect = new Rect(
                                GUI.matrix.MultiplyPoint(new Vector3(checkRect.position.x, checkRect.position.y + 200, 0)),
                                GUI.matrix.MultiplyPoint(new Vector3(checkRect.size.x, checkRect.size.y, 0)));
                                GUIView.current.MarkHotRegion(labelRect);
                                GUIView.current.MarkHotRegion(checkRect);
                        }

                        ++y;
                    }
                }
                GUILayout.EndScrollView();
            }

            {
                GUI.matrix = Matrix4x4.identity;
                var y = 0;

                for (var i = 0; i < kMaxLayers; ++i)
                {
                    // Is the layer valid?
                    if (LayerMask.LayerToName(i) != string.Empty)
                    {
                        // Yes so draw toggles.
                        var x = 0;
                        var r = GUILayoutUtility.GetRect(indent + checkboxSize * activeLayerCount + labelSize, checkboxSize);
                        var labelRect = new Rect(r.x + indent, r.y, labelSize, checkboxSize + 5);
                        GUI.Label(labelRect, LayerMask.LayerToName(i), Styles.rightLabel);

                        // Empty Transparent label used to indicate highlighted row.
                        var checkRect = new Rect(r.x + indent, r.y, labelSize + (activeLayerCount - y) * checkboxSize, checkboxSize);
                        GUI.Label(checkRect, GUIContent.none, Styles.hoverStyle);
                        GUIView.current.MarkHotRegion(labelRect);
                        GUIView.current.MarkHotRegion(checkRect);

                        // Iterate all the layers.
                        for (var j = kMaxLayers - 1; j >= 0; --j)
                        {
                            // Is the layer valid?
                            if (LayerMask.LayerToName(j) != string.Empty)
                            {
                                // Yes, so draw layer toggles.
                                if (x < activeLayerCount - y)
                                {
                                    var tooltip = new GUIContent("", LayerMask.LayerToName(i) + "/" + LayerMask.LayerToName(j));
                                    var val = getValue(i, j);
                                    var toggle = GUI.Toggle(new Rect(labelSize + indent + r.x + x * checkboxSize, r.y, checkboxSize, checkboxSize), val, tooltip);

                                    if (toggle != val)
                                        setValue(i, j, toggle);
                                }
                                ++x;
                            }
                        }
                        ++y;
                    }
                }
            }

            // Buttons.
            {
                EditorGUILayout.Space(8);
                GUILayout.BeginHorizontal();

                // Made the buttons span the entire matrix of layers
                if (GUILayout.Button("Disable All", GUILayout.MinWidth((checkboxSize * activeLayerCount) / 2), GUILayout.ExpandWidth(false)))
                    SetAllLayerCollisions(false, setValue);

                if (GUILayout.Button("Enable All", GUILayout.MinWidth((checkboxSize * activeLayerCount) / 2), GUILayout.ExpandWidth(false)))
                    SetAllLayerCollisions(true, setValue);

                GUILayout.EndHorizontal();
            }
        }

        static void SetAllLayerCollisions(bool flag, SetValueFunc setValue)
        {
            for (int i = 0; i < kMaxLayers; ++i)
                for (int j = i; j < kMaxLayers; ++j)
                    setValue(i, j, flag);
        }
    }
}
