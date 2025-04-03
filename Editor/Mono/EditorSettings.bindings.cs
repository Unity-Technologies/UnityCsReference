// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
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
        [Obsolete("Sprite Packing Tags are deprecated. Please use Sprite Atlas asset.")]
        BuildTimeOnly = 1,
        [Obsolete("Sprite Packing Tags are deprecated. Please use Sprite Atlas asset.")]
        AlwaysOn = 2,
        BuildTimeOnlyAtlas = 3,
        AlwaysOnAtlas = 4,
        SpriteAtlasV2 = 5,
        SpriteAtlasV2Build = 6,
    }

    public enum LineEndingsMode
    {
        OSNative = 0,
        Unix = 1,
        Windows = 2
    }

    public enum AssetPipelineMode
    {
        Version1 = 0,
        Version2 = 1
    }

    public enum CacheServerMode
    {
        AsPreferences = 0,
        Enabled = 1,
        Disabled = 2
    }

    public enum CacheServerValidationMode
    {
        Disabled = 0,
        UploadOnly = 1,
        Enabled = 2,
        Required = 3
    }

    [Flags]
    public enum EnterPlayModeOptions
    {
        None = 0,
        DisableDomainReload = 1 << 0,
        DisableSceneReload = 1 << 1,
        [Obsolete("Option has no effect and is deprecated.")]
        DisableSceneBackupUnlessDirty = 1 << 2
    }

    [NativeHeader("Editor/Src/EditorSettings.h")]
    [NativeHeader("Editor/Src/VersionControlSettings.h")]
    [NativeHeader("Editor/Src/EditorUserSettings.h")]
    public sealed class EditorSettings : Object
    {
        internal enum Bc7TextureCompressor
        {
            Default = 0,
            Ispc = 1,
            Bc7e = 2,
        }

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

        [System.Obsolete(@"Use VersionControlSettings.mode instead.")]
        [StaticAccessor("GetVersionControlSettings()", StaticAccessorType.Dot)]
        public static extern string externalVersionControl
        {
            [NativeMethod("GetMode")]
            get;
            [NativeMethod("SetMode")]
            set;
        }

        [FreeFunction("GetEditorSettings")]
        internal static extern EditorSettings GetEditorSettings();

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
        public static extern bool prefabModeAllowAutoSave { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern SpritePackerMode spritePackerMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int spritePackerPaddingPower { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        internal static extern Bc7TextureCompressor bc7TextureCompressor { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int etcTextureCompressorBehavior { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int etcTextureFastCompressor {[NativeMethod("GetEtcTextureFastCompressorNoOffset")] get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int etcTextureNormalCompressor {[NativeMethod("GetEtcTextureNormalCompressorNoOffset")] get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int etcTextureBestCompressor {[NativeMethod("GetEtcTextureBestCompressorNoOffset")] get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool enableTextureStreamingInEditMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool enableTextureStreamingInPlayMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool asyncShaderCompilation { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static bool cachingShaderPreprocessor { get { return true; } set {} }

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

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static string[] projectGenerationBuiltinExtensions
        {
            get { return new[] { "cs", "uxml", "uss", "shader", "compute", "cginc", "hlsl", "glslinc", "template", "raytrace" }; }
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

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool useLegacyProbeSampleCount { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool shadowmaskStitching { get; set; }

        [Obsolete("The disableCookiesInLightmapper setting is no longer supported. Cookies are always enabled in the Progressive Lightmapper.", true)]
        public static bool disableCookiesInLightmapper
        {
            get { return false; }
            set {}
        }

        [Obsolete("The enableCookiesInLightmapper setting is no longer supported. Cookies are always enabled in the Progressive Lightmapper.")]
        public static bool enableCookiesInLightmapper
        {
            get { return true; }
            set {}
        }

        [Obsolete("Bake with the Progressive Lightmapper.The backend that uses Enlighten to bake is obsolete.", true)]
        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool enableEnlightenBakedGI { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool enterPlayModeOptionsEnabled { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern EnterPlayModeOptions enterPlayModeOptions { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool serializeInlineMappingsOnOneLine { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern AssetPipelineMode assetPipelineMode { get; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern CacheServerMode cacheServerMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern AssetDatabase.RefreshImportMode refreshImportMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern string cacheServerEndpoint { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern string cacheServerNamespacePrefix { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool cacheServerEnableDownload { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool cacheServerEnableUpload { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool cacheServerEnableAuth { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool cacheServerEnableTls { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern CacheServerValidationMode cacheServerValidationMode { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern Int32 cacheServerDownloadBatchSize { get; set; }

        public enum NamingScheme
        {
            SpaceParenthesis = 0,
            Dot,
            Underscore
            // note: C++ code has more, but we don't want to expose them for Hierarchy naming
        }
        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern int gameObjectNamingDigits { get; set; }
        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern NamingScheme gameObjectNamingScheme { get; set; }
        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool assetNamingUsesSpace { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        internal static extern bool inspectorUseIMGUIDefaultInspector { get; set; }

        [StaticAccessor("GetEditorSettings()", StaticAccessorType.Dot)]
        public static extern bool referencedClipsExactNaming { get; set; }
    }
}
