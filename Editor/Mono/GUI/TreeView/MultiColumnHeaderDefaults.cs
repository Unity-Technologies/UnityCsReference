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
            // Column header uses same colors as label, but slightly dimmed to let list items stand out more (similar to win 10)
            // Use middle alignment to ensure labels are aligned regardless of icon size
            public static GUIStyle columnHeader = "MultiColumnHeader";
            public static GUIStyle columnHeaderRightAligned = "MultiColumnHeaderRight";
            public static GUIStyle columnHeaderCenterAligned = "MultiColumnHeaderCenter";
            public static GUIStyle background = "MultiColumnTopBar"; // Use bar background with 1 pixel line at the bottom to let items have someting to clip against when scrolled
            // Arrow uses same colors as label (derived) aligned at top
            internal static GUIStyle arrowStyle = "MultiColumnArrow";
        }
    }
}
