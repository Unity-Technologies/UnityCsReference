// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    public enum SemanticMergeMode
    {
        Off = 0,
        Premerge = 1,
        Ask = 2,
    }

    [NativeHeader("Editor/Src/EditorUserSettings.h")]
    [StaticAccessor("GetEditorUserSettings()", StaticAccessorType.Dot)]
    public sealed class EditorUserSettings : UnityObject
    {
        EditorUserSettings()
        {
        }

        static extern bool HasConfigValue(string name);

        [NativeMethod("GetConfigValue")]
        static extern string Internal_GetConfigValue(string name);

        public static string GetConfigValue(string name)
        {
            return HasConfigValue(name) ? Internal_GetConfigValue(name) : null;
        }

        public static extern void SetConfigValue(string name, string value);

        internal static extern void SetPrivateConfigValue(string name, string value);

        [NativeProperty("VCAutomaticAdd")]
        public static extern bool AutomaticAdd { get; set; }

        [NativeProperty("VCWorkOffline")]
        public static extern bool WorkOffline { get; set; }

        [NativeProperty("VCShowFailedCheckout")]
        public static extern bool showFailedCheckout { get; set; }

        [NativeProperty("VCOverwriteFailedCheckoutAssets")]
        public static extern bool overwriteFailedCheckoutAssets { get; set; }

        [NativeProperty("VCProjectOverlayIcons")]
        public static extern bool overlayIcons { get; set; }

        [NativeProperty("VCHierarchyOverlayIcons")]
        internal static extern bool hierarchyOverlayIcons { get; set; }

        [NativeProperty("VCOtherOverlayIcons")]
        internal static extern bool otherOverlayIcons { get; set; }

        [NativeProperty("VCAllowAsyncUpdate")]
        public static extern bool allowAsyncStatusUpdate { get; set; }

        [NativeProperty("VCDebugCmd")]
        internal static extern bool DebugCmd { get; set; }

        [NativeProperty("VCDebugOut")]
        internal static extern bool DebugOut { get; set; }

        [NativeProperty("VCDebugCom")]
        internal static extern bool DebugCom { get; set; }

        public static extern SemanticMergeMode semanticMergeMode { get; set; }

        internal static extern void SoftReset();
    }
}
