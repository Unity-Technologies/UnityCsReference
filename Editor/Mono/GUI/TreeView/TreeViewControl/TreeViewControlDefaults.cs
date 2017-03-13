// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public partial class TreeView
    {
        public static class DefaultGUI
        {
            public static void FoldoutLabel(Rect rect, string label, bool selected, bool focused)
            {
                if (Event.current.type == EventType.Repaint)
                    DefaultStyles.foldoutLabel.Draw(rect, GUIContent.Temp(label), false, false, selected, focused);
            }

            public static void Label(Rect rect, string label, bool selected, bool focused)
            {
                if (Event.current.type == EventType.Repaint)
                    DefaultStyles.label.Draw(rect, GUIContent.Temp(label), false, false, selected, focused);
            }

            public static void LabelRightAligned(Rect rect, string label, bool selected, bool focused)
            {
                if (Event.current.type == EventType.Repaint)
                    DefaultStyles.labelRightAligned.Draw(rect, GUIContent.Temp(label), false, false, selected, focused);
            }

            public static void BoldLabel(Rect rect, string label, bool selected, bool focused)
            {
                if (Event.current.type == EventType.Repaint)
                    DefaultStyles.boldLabel.Draw(rect, GUIContent.Temp(label), false, false, selected, focused);
            }

            public static void BoldLabelRightAligned(Rect rect, string label, bool selected, bool focused)
            {
                if (Event.current.type == EventType.Repaint)
                    DefaultStyles.boldLabelRightAligned.Draw(rect, GUIContent.Temp(label), false, false, selected, focused);
            }

            internal static float contentLeftMargin
            {
                get { return DefaultStyles.foldoutLabel.margin.left; }
            }
        }

        public static class DefaultStyles
        {
            public static GUIStyle foldoutLabel;
            public static GUIStyle label;
            public static GUIStyle labelRightAligned;

            public static GUIStyle boldLabel;
            public static GUIStyle boldLabelRightAligned;

            public static GUIStyle backgroundEven = "OL EntryBackEven";
            public static GUIStyle backgroundOdd = "OL EntryBackOdd";

            static DefaultStyles()
            {
                // Make a copy of lineStyle since left padding is being dynamically changed on that
                // Note the left padding of 0 for exact placement of content after foldout or icon
                foldoutLabel = new GUIStyle(TreeViewGUI.Styles.lineStyle);
                foldoutLabel.padding.left = 0;

                // For generic labels use same padding values as the standard EditorStyles.label for consistency
                label = new GUIStyle(foldoutLabel);
                label.padding.left = 2;
                label.padding.right = 2;

                labelRightAligned = new GUIStyle(label);
                labelRightAligned.alignment = TextAnchor.UpperRight;

                boldLabel = new GUIStyle(label);
                boldLabel.font = EditorStyles.boldLabel.font;
                boldLabel.fontStyle = EditorStyles.boldLabel.fontStyle;

                boldLabelRightAligned = new GUIStyle(boldLabel);
                boldLabelRightAligned.alignment = TextAnchor.UpperRight;
            }
        }
    }
}
