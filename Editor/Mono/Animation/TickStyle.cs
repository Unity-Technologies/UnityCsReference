// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [System.Serializable]
    internal class TickStyle
    {
        public EditorGUIUtility.SkinnedColor tickColor = new EditorGUIUtility.SkinnedColor(new Color(0.0f, 0.0f, 0.0f, 0.2f), new Color(.45f, .45f, .45f, 0.2f)); // color and opacity of ticks
        public EditorGUIUtility.SkinnedColor labelColor = new EditorGUIUtility.SkinnedColor(new Color(0.0f, 0.0f, 0.0f, 0.32f), new Color(0.8f, 0.8f, 0.8f, 0.32f)); // color and opacity of tick labels
        public int distMin = 10; // min distance between ticks before they disappear completely
        public int distFull = 80; // distance between ticks where they gain full strength
        public int distLabel = 50; // min distance between tick labels
        public bool stubs = false; // draw ticks as stubs or as full lines?
        public bool centerLabel = false; // center label on tick lines
        public string unit = ""; // unit to write after the number
    }
}
