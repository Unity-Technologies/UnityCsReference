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

    [NativeHeader("Editor/Src/EditorUserSettings.bindings.h")]
    [StaticAccessor("GetEditorUserSettings()", StaticAccessorType.Dot)]
    public sealed class EditorUserSettings : UnityObject
    {
        EditorUserSettings()
        {
        }

        [FreeFunction("Internal_GetConfigValue")]
        public static extern string GetConfigValue(string name);

        public static extern void SetConfigValue(string name, string value);

        internal static extern void SetPrivateConfigValue(string name, string value);

        [NativeProperty("VCAutomaticAdd")]
        public static extern bool AutomaticAdd { get; set; }

        [NativeProperty("VCWorkOffline")]
        public static extern bool WorkOffline { get; set; }

        [NativeProperty("VCShowFailedCheckout")]
        public static extern bool showFailedCheckout { get; set; }

        [NativeProperty("VCAllowAsyncUpdate")]
        public static extern bool allowAsyncStatusUpdate { get; set; }

        [NativeProperty("VCDebugCmd")]
        internal static extern bool DebugCmd { get; set; }

        [NativeProperty("VCDebugOut")]
        internal static extern bool DebugOut { get; set; }

        [NativeProperty("VCDebugCom")]
        internal static extern bool DebugCom { get; set; }

        public static extern SemanticMergeMode semanticMergeMode { get; set; }
    }
}
