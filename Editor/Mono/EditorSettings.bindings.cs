// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.VisualStudioIntegration;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public enum SerializationMode
    {
        Mixed = 0,
        ForceBinary = 1,
        ForceText = 2
    }

    public enum EditorBehaviorMode
    {
        Mode3D = 0,
        Mode2D = 1
    }

    public enum SpritePackerMode
    {
        Disabled = 0,
        BuildTimeOnly = 1,
        AlwaysOn = 2,
        BuildTimeOnlyAtlas = 3,
        AlwaysOnAtlas = 4
    }

    public enum LineEndingsMode
    {
        OSNative = 0,
        Unix = 1,
        Windows = 2
    }

    // Must be a struct in order to have correct comparison behaviour
    [StructLayout(LayoutKind.Sequential)]
    public struct ExternalVersionControl
    {
        private readonly string m_Value;

        public static readonly string Disabled = "Hidden Meta Files";
        public static readonly string AutoDetect = "Auto detect";
        public static readonly string Generic = "Visible Meta Files";


        [Obsolete("Asset Server VCS support has been removed.")]
        public static readonly string AssetServer = "Asset Server";

        public ExternalVersionControl(string value)
        {
            m_Value = value;
        }

        // User-defined conversion
        public static implicit operator string(ExternalVersionControl d)
        {
            return d.ToString();
        }

        // User-defined conversion
        public static implicit operator ExternalVersionControl(string d)
        {
            return new ExternalVersionControl(d);
        }

        public override string ToString()
        {
            return m_Value;
        }
    }

    [NativeHeader("Editor/Src/EditorSettings.h")]
    [NativeHeader("Editor/Src/EditorUserSettings.h")]
    public sealed class EditorSettings : Object
    {
        private EditorSettings()
        {
        }

        // Device that editor should use for Unity Remote
        public static string unityRemoteDevice
        {
            get { return GetConfigValue("UnityRemoteDevice"); }
            set { SetConfigValue("UnityRemoteDevice", value); }
        }

        // Compression method for Unity Remote to use
        public static string unityRemoteCompression
        {
            get { return GetConfigValue("UnityRemoteCompression"); }
            set { SetConfigValue("UnityRemoteCompression", value); }
        }

        // Screen size for Unity Remote to use
        public static string unityRemoteResolution
        {
            get { return GetConfigValue("UnityRemoteResolution"); }
            set { SetConfigValue("UnityRemoteResolution", value); }
        }

        // Remote joystick input behavior for Unity Remote (use local / override with remote)
        public static string unityRemoteJoystickSource
        {
            get { return GetConfigValue("UnityRemoteJoystickSource"); }
            set { SetConfigValue("UnityRemoteJoystickSource", value); }
        }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern string externalVersionControl
        {
            [NativeMethod("GetExternalVersionControlSupport")]
            get;
            [NativeMethod("SetExternalVersionControlSupport")]
            set;
        }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern SerializationMode serializationMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern LineEndingsMode lineEndingsForNewScripts { get; set; }

        [Obsolete("EditorSettings.webSecurityEmulationEnabled is no longer supported, " +
            "since the Unity Web Player is no longer supported by Unity.")]
        public static bool webSecurityEmulationEnabled
        {
            get { return false; }
            set {}
        }

        [Obsolete("EditorSettings.webSecurityEmulationHostUrl is no longer supported, " +
            "since the Unity Web Player is no longer supported by Unity.")]
        public static string webSecurityEmulationHostUrl
        {
            get { return ""; }
            set {}
        }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern EditorBehaviorMode defaultBehaviorMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern SceneAsset prefabRegularEnvironment { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern SceneAsset prefabUIEnvironment { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern SpritePackerMode spritePackerMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int spritePackerPaddingPower { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int etcTextureCompressorBehavior { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int etcTextureFastCompressor {[NativeMethod("GetEtcTextureFastCompressorNoOffset")] get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int etcTextureNormalCompressor {[NativeMethod("GetEtcTextureNormalCompressorNoOffset")] get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int etcTextureBestCompressor {[NativeMethod("GetEtcTextureBestCompressorNoOffset")] get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool enableTextureStreamingInPlayMode { get; set; }

        public static string[] projectGenerationUserExtensions
        {
            get
            {
                return Internal_ProjectGenerationUserExtensions
                    .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimStart('.', '*'))
                    .ToArray();
            }
            set { Internal_ProjectGenerationUserExtensions = string.Join(";", value); }
        }

        public static string[] projectGenerationBuiltinExtensions
        {
            get { return SolutionSynchronizer.BuiltinSupportedExtensions.Keys.ToArray(); }
        }

        internal static extern string Internal_ProjectGenerationUserExtensions
        {
            [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
            [NativeMethod("GetProjectGenerationIncludedExtensions")]
            get;
            [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
            [NativeMethod("SetProjectGenerationIncludedExtensions")]
            set;
        }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern string projectGenerationRootNamespace { get; set; }

        [StaticAccessor("GetEditorUserSettings()", StaticAccessorType.Dot)]
        private static extern string GetConfigValue(string name);

        [StaticAccessor("GetEditorUserSettings()", StaticAccessorType.Dot)]
        private static extern void SetConfigValue(string name, string value);

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        internal static extern void SetEtcTextureCompressorLegacyBehavior();

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        internal static extern void SetEtcTextureCompressorDefaultBehavior();
    }
}
