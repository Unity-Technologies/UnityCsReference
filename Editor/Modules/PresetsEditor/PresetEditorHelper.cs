// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.Presets
{
    internal static partial class PresetEditorHelper
    {
        [AutoStaticsCleanupOnCodeReload]
        internal static Object[] InspectedObjects { get; set; }

        /// <summary>
        /// Internal flag set to true when the preset picker is opened.
        /// When an item is selected or cancelled, the flag is reset.
        /// </summary>
        [NoAutoStaticsCleanup] // transient bool flag reset on picker select/cancel; safe to persist across reload
        internal static bool presetEditorOpen { get; set; }
    }
}
