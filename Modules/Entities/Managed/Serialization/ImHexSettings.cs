// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;

namespace Unity.Entities
{
    /// <summary>
    /// Convenience accessor for ImHex pattern generation setting.
    /// </summary>
    internal static partial class ImHexSettings
    {
        // The package's EntitiesDebuggingSettings writes its GenerateImHexPatterns value here as it loads and changes.
        [AutoStaticsCleanupOnCodeReload]
        static bool s_Enabled;

        /// <summary>
        /// When true, ImHex pattern files (.hexpat) are generated during Entities serialization.
        /// </summary>
        public static bool Enabled
        {
            get => s_Enabled;
            set => s_Enabled = value;
        }
    }
}
