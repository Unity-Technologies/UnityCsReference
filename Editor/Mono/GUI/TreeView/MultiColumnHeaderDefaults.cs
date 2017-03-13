// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor.IMGUI.Controls
{
    public partial class MultiColumnHeader
    {
        public static class DefaultGUI
        {
            public static float defaultHeight { get { return 27f; } }
            public static float minimumHeight { get { return 20f; } }
            public static float columnContentMargin { get { return 3f; } }
            internal static float labelSpaceFromBottom { get { return 3; } }
        }

        public static class DefaultStyles
        {
            public static GUIStyle columnHeader;
            public static GUIStyle columnHeaderRightAligned;
            public static GUIStyle columnHeaderCenterAligned;
            public static GUIStyle background;
            internal static GUIStyle arrowStyle;

            static DefaultStyles()
            {
                // Use bar background with 1 pixel line at the bottom to let items have someting to clip against when scrolled
                background = new GUIStyle("ProjectBrowserTopBarBg");
                background.fixedHeight = 0;
                background.border = new RectOffset(3, 3, 3, 3);

                // Column header uses same colors as label, but slightly dimmed to let list items stand out more (similar to win 10)
                // Use middle alignment to ensure labels are aligned regardless of icon size
                columnHeader = new GUIStyle(EditorStyles.label);
                columnHeader.alignment = TextAnchor.MiddleLeft;
                columnHeader.padding = new RectOffset(4, 4, 0, 0);
                Color col = columnHeader.normal.textColor;
                col.a = 0.8f;
                columnHeader.normal.textColor = col;

                columnHeaderRightAligned = new GUIStyle(columnHeader);
                columnHeaderRightAligned.alignment = TextAnchor.MiddleRight;

                columnHeaderCenterAligned = new GUIStyle(columnHeader);
                columnHeaderCenterAligned.alignment = TextAnchor.MiddleCenter;

                // Arrow uses same colors as label (derived)
                arrowStyle = new GUIStyle(EditorStyles.label);
                arrowStyle.padding = new RectOffset();
                arrowStyle.fixedWidth = 13f;
                arrowStyle.alignment = TextAnchor.UpperCenter; // arrow is alligned at top
            }
        }
    }
}
