// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Audio;

namespace UnityEditor
{
    internal static class AudioMixerColorCodes
    {
        // Must match 'colorNames' array
        static Color[] darkSkinColors = new[] {
            new Color(0.5f, 0.5f, 0.5f, 0.2f),
            new Color(255 / 255f, 208 / 255f,   0 / 255f),
            new Color(245 / 255f, 156 / 255f,   4 / 255f),
            new Color(255 / 255f,  75 / 255f,  58 / 255f),
            new Color(255 / 255f,  97 / 255f, 156 / 255f),
            new Color(168 / 255f, 114 / 255f, 183 / 255f),
            new Color(13 / 255f, 156 / 255f, 210 / 255f),
            new Color(0 / 255f,  190 / 255f, 200 / 255f),
            new Color(138 / 255f, 192 / 255f,   1 / 255f)
        };

        // Must match 'colorNames' array
        static Color[] lightSkinColors = new[] {
            new Color(0.5f, 0.5f, 0.5f, 0.2f),
            new Color(255 / 255f, 214 / 255f,  22 / 255f),
            new Color(247 / 255f, 147 / 255f,   0 / 255f),
            new Color(255 / 255f,  75 / 255f,  58 / 255f),
            new Color(255 / 255f,  97 / 255f, 156 / 255f),
            new Color(168 / 255f, 114 / 255f, 183 / 255f),
            new Color(13 / 255f, 156 / 255f, 210 / 255f),
            new Color(0 / 255f, 181 / 255f, 185 / 255f),
            new Color(114 / 255f, 169 / 255f,  24 / 255f)
        };

        static string[] colorNames = new[] {    "No Color",
                                                "Yellow",
                                                "Orange",
                                                "Red",
                                                "Magenta",
                                                "Violet",
                                                "Blue",
                                                "Cyan",
                                                "Green" };

        static string[] GetColorNames()
        {
            return colorNames;
        }

        static Color[] GetColors()
        {
            if (EditorGUIUtility.isProSkin)
                return darkSkinColors;
            else
                return lightSkinColors;
        }

        public static void AddColorItemsToGenericMenu(GenericMenu menu, AudioMixerGroupController[] groups)
        {
            var colors = GetColors();
            var names = GetColorNames();

            for (int i = 0; i < colors.Length; i++)
            {
                bool selected = (groups.Length == 1) && (i == groups[0].userColorIndex);
                menu.AddItem(new GUIContent(names[i]), selected, ItemCallback, new ItemData { groups = groups, index = i });
            }
        }

        struct ItemData
        {
            public AudioMixerGroupController[] groups;
            public int index;
        }
        static void ItemCallback(object data)
        {
            ItemData d = (ItemData)data;

            Undo.RecordObjects(d.groups, "Change Group(s) Color");

            foreach (AudioMixerGroupController group in d.groups)
                group.userColorIndex = d.index;
        }

        public static Color GetColor(int index)
        {
            var colors = GetColors();
            if (index >= 0 && index < colors.Length)
                return colors[index];

            Debug.LogError("Invalid color code index: " + index);
            return Color.white;
        }
    }
}
