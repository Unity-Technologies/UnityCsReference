// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Profile;

[NativeHeader("Runtime/Misc/PlayerSettings.h")]
[NativeClass("BuildProfilePlayerSettings", PersistentTypeId = 0x43E334C1)]
[UsedByNativeCode]
[VisibleToOtherModules]
internal class BuildProfilePlayerSettings : UnityEngine.Object
{
    private extern static void Internal_Create([Writable] BuildProfilePlayerSettings self);

    internal SerializedObject GetSerializedObject() => new SerializedObject(this);

    [RequiredByNativeCode]
    public BuildProfilePlayerSettings()
    {
        Internal_Create(this);
    }

    [NativeMethod("GetPlatformGraphicsAPIsWithUGKVariants")]
    internal extern GraphicsDeviceType[] GetGraphicsAPIsWithUGKVariants(BuildTarget platform);

    [NativeMethod("GetPlatformGraphicsAPIUGKFlags")]
    internal extern int[] GetGraphicsAPIUGKFlags(BuildTarget platform);

    [NativeMethod("SetPlatformGraphicsAPIs")]
    private extern void SetGraphicsAPIsImpl(BuildTarget platform, GraphicsDeviceType[] apis, bool skipValidation, int[] ugkFlags);

    internal void SetGraphicsAPIs(BuildTarget platform, GraphicsDeviceType[] apis, int[] ugkFlags, bool shouldSync)
    {
        SetGraphicsAPIsImpl(platform, apis, false, ugkFlags);
        // we do cache api list in player settings editor, so if we update from script we should forcibly update cache
        if (shouldSync)
            PlayerSettingsEditor.SyncEditors(platform);
    }

    [NativeMethod("GetPlatformGraphicsAPIs")]
    internal extern GraphicsDeviceType[] GetGraphicsAPIs(BuildTarget platform);

    internal void SetGraphicsAPIs(BuildTarget platform, GraphicsDeviceType[] apis, bool shouldSync)
    {
        SetGraphicsAPIs(platform, apis, null, shouldSync);
    }

    [NativeMethod("GetPlatformAutomaticGraphicsAPIs")]
    internal extern bool GetUseDefaultGraphicsAPIs(BuildTarget platform);

    [NativeMethod("SetPlatformAutomaticGraphicsAPIs")]
    private extern void SetUseDefaultGraphicsAPIsImpl(BuildTarget platform, bool automatic);

    internal void SetUseDefaultGraphicsAPIs(BuildTarget platform, bool automatic)
    {
        SetUseDefaultGraphicsAPIsImpl(platform, automatic);
        // we do cache api list in player settings editor, so if we update from script we should forcibly update cache
        PlayerSettingsEditor.SyncEditors(platform);
    }

    internal extern ColorGamut[] GetColorGamuts();

    [NativeMethod("SetColorGamuts")]
    private extern void SetColorGamutsImpl(ColorGamut[] colorSpaces);

    internal void SetColorGamuts(ColorGamut[] colorSpaces)
    {
        SetColorGamutsImpl(colorSpaces);
        // Color space data is cached in player settings editor
        PlayerSettingsEditor.SyncEditors(BuildTarget.NoTarget);
    }

    internal extern int GetDefaultShaderChunkSizeInMB();
    internal extern void SetDefaultShaderChunkSizeInMB(int sizeInMegabytes);
    internal extern int GetDefaultShaderChunkCount();
    internal extern void SetDefaultShaderChunkCount(int chunkCount);
    internal extern bool GetOverrideShaderChunkSettingsForPlatform(BuildTarget buildTarget);
    internal extern void SetOverrideShaderChunkSettingsForPlatform(BuildTarget buildTarget, bool value);
    [NativeMethod("GetPlatformShaderChunkSizeInMB")]
    internal extern int GetShaderChunkSizeInMBForPlatform(BuildTarget buildTarget);
    [NativeMethod("SetPlatformShaderChunkSizeInMB")]
    internal extern void SetShaderChunkSizeInMBForPlatform(BuildTarget buildTarget, int sizeInMegabytes);
    [NativeMethod("GetPlatformShaderChunkCount")]
    internal extern int GetShaderChunkCountForPlatform(BuildTarget buildTarget);
    [NativeMethod("SetPlatformShaderChunkCount")]
    internal extern void SetShaderChunkCountForPlatform(BuildTarget buildTarget, int chunkCount);
    [NativeMethod("GetPlatformBatching")]
    internal extern void GetBatchingForPlatform(BuildTarget platform, out int staticBatching, out int dynamicBatching);
    [NativeMethod("SetPlatformBatching")]
    internal extern void SetBatchingForPlatform(BuildTarget platform, int staticBatching, int dynamicBatching);
    [NativeMethod("GetPlatformGraphicsJobs")]
    internal extern bool GetGraphicsJobsForPlatform(BuildTarget platform);
    [NativeMethod("SetPlatformGraphicsJobs")]
    internal extern void SetGraphicsJobsForPlatform(BuildTarget platform, bool graphicsJobs);
    [NativeMethod("GetPlatformGraphicsJobMode")]
    internal extern GraphicsJobMode GetGraphicsJobModeForPlatform(BuildTarget platform);
    [NativeMethod("SetPlatformGraphicsJobMode")]
    internal extern void SetGraphicsJobModeForPlatform(BuildTarget platform, GraphicsJobMode gfxJobMode);
    [NativeMethod("SetPlatformGraphicsThreadingMode")]
    internal extern void SetGraphicsThreadingModeForPlatform(BuildTarget platform, GfxThreadingMode gfxJobMode);
    internal extern NormalMapEncoding GetNormalMapEncoding(string platform);
    internal extern void SetNormalMapEncoding(string platform, NormalMapEncoding encoding);
    [NativeMethod("GetLightmapStreamingEnabled")]
    internal extern bool GetLightmapStreamingEnabledForPlatformGroup(BuildTargetGroup platformGroup);
    [NativeMethod("SetLightmapStreamingEnabled")]
    internal extern void SetLightmapStreamingEnabledForPlatformGroup(BuildTargetGroup platformGroup, bool lightmapStreamingEnabled);
    [NativeMethod("GetLightmapStreamingPriority")]
    internal extern int GetLightmapStreamingPriorityForPlatformGroup(BuildTargetGroup platformGroup);
    [NativeMethod("SetLightmapStreamingPriority")]
    internal extern void SetLightmapStreamingPriorityForPlatformGroup(BuildTargetGroup platformGroup, int lightmapStreamingPriority);
    [NativeMethod("GetLightmapEncodingQuality")]
    internal extern LightmapEncodingQuality GetLightmapEncodingQualityForPlatform(BuildTarget platform);
    [NativeMethod("SetLightmapEncodingQuality")]
    internal extern void SetLightmapEncodingQualityForPlatform(BuildTarget platform, LightmapEncodingQuality encodingQuality);
    [NativeMethod("GetHDRCubemapEncodingQuality")]
    internal extern HDRCubemapEncodingQuality GetHDRCubemapEncodingQualityForPlatform(BuildTarget platform);
    [NativeMethod("SetHDRCubemapEncodingQuality")]
    internal extern void SetHDRCubemapEncodingQualityForPlatform(BuildTarget platform, HDRCubemapEncodingQuality encodingQuality);
    [NativeMethod("GetLoadStoreDebugModeEnabled")]
    internal extern bool GetLoadStoreDebugModeEnabledForPlatformGroup(BuildTargetGroup platformGroup);
    [NativeMethod("SetLoadStoreDebugModeEnabled")]
    internal extern void SetLoadStoreDebugModeEnabledForPlatformGroup(BuildTargetGroup platformGroup, bool loadStoreDebugModeEnabled);
    [NativeMethod("GetLoadStoreDebugModeEditorOnly")]
    internal extern bool GetLoadStoreDebugModeEditorOnlyForPlatformGroup(BuildTargetGroup platformGroup);
    [NativeMethod("SetLoadStoreDebugModeEditorOnly")]
    internal extern void SetLoadStoreDebugModeEditorOnlyForPlatformGroup(BuildTargetGroup platformGroup, bool loadStoreDebugModeEnabled);
    internal extern bool HasAnyNetFXCompatibilityLevel();
    internal extern ScriptingImplementation GetScriptingBackend(string buildTargetGroupName);
    internal extern string GetTemplateCustomValue(string name);
    internal extern void SetTemplateCustomValue(string name, string value);
    internal extern void SetTemplateCustomKeys(string[] templateCustomKeys);
    [NativeMethod("GetMobileMTRendering", ThrowsException = true)]
    internal extern bool GetMobileMTRendering(string buildTargetName);
    [NativeMethod("SetMobileMTRendering", ThrowsException = true)]
    internal extern void SetMobileMTRendering(string buildTargetName, bool enable);
    internal extern GraphicsDeviceType[] GetPlatformAutomaticGraphicsAPIsList(BuildTarget platform);
}
