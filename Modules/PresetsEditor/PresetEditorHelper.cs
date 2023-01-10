// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;

namespace UnityEditor.Presets
{
    internal static class PresetEditorHelper
    {
        internal static Object[] InspectedObjects { get; set; }

        /// <summary>
        /// Internal flag set to true when the preset picker is opened.
        /// When an item is selected or cancelled, the flag is reset.
        /// </summary>
        internal static bool presetEditorOpen { get; set; }
    }
}
