// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEditor.Build.Profile;

[CustomEditor(typeof(BuildProfilePlayerSettings))]
[VisibleToOtherModules("UnityEditor.BuildProfileModule")]
internal partial class BuildProfilePlayerSettingsEditor : PlayerSettingsEditor
{
    private class BuildProfilePlayerSettingsAccessor : IPlayerSettingsAccessor, IPlayerSettingsiOSAccessor
    {
        BuildProfilePlayerSettings m_PlayerSettings;

        public BuildProfilePlayerSettingsAccessor(BuildProfilePlayerSettings playerSettings)
        {
            m_PlayerSettings = playerSettings;
        }

        public GraphicsDeviceType[] GetGraphicsAPIsWithUGKVariants_Internal(BuildTarget platform)
        {
            return m_PlayerSettings.GetGraphicsAPIsWithUGKVariants(platform);
        }
        public int[] GetGraphicsAPIUGKFlags_Internal(BuildTarget platform)
        {
            return m_PlayerSettings.GetGraphicsAPIUGKFlags(platform);
        }
        public void SetGraphicsAPIs_Internal(BuildTarget platform, GraphicsDeviceType[] apis, int[] ugkFlags, bool shouldSync)
        {
            m_PlayerSettings.SetGraphicsAPIs(platform, apis, ugkFlags, shouldSync);
        }

        public GraphicsDeviceType[] GetGraphicsAPIs_Internal(BuildTarget platform)
        {
            return m_PlayerSettings.GetGraphicsAPIs(platform);
        }
        public void SetGraphicsAPIs_Internal(BuildTarget platform, GraphicsDeviceType[] apis, bool shouldSync)
        {
            m_PlayerSettings.SetGraphicsAPIs(platform, apis, shouldSync);
        }

        public bool GetUseDefaultGraphicsAPIs_Internal(BuildTarget platform)
        {
            return m_PlayerSettings.GetUseDefaultGraphicsAPIs(platform);
        }

        public void SetUseDefaultGraphicsAPIs_Internal(BuildTarget platform, bool automatic)
        {
            m_PlayerSettings.SetUseDefaultGraphicsAPIs(platform, automatic);
        }

        public ColorGamut[] GetColorGamuts_Internal()
        {
            return m_PlayerSettings.GetColorGamuts();
        }

        public void SetColorGamuts_Internal(ColorGamut[] colorSpaces)
        {
            m_PlayerSettings.SetColorGamuts(colorSpaces);
        }

        public int GetDefaultShaderChunkSizeInMB_Internal()
        {
            return m_PlayerSettings.GetDefaultShaderChunkSizeInMB();
        }

        public void SetDefaultShaderChunkSizeInMB_Internal(int sizeInMegabytes)
        {
            m_PlayerSettings.SetDefaultShaderChunkSizeInMB(sizeInMegabytes);
        }

        public int GetDefaultShaderChunkCount_Internal()
        {
            return m_PlayerSettings.GetDefaultShaderChunkCount();
        }

        public void SetDefaultShaderChunkCount_Internal(int chunkCount)
        {
            m_PlayerSettings.SetDefaultShaderChunkCount(chunkCount);
        }

        public bool GetOverrideShaderChunkSettingsForPlatform_Internal(BuildTarget buildTarget)
        {
            return m_PlayerSettings.GetOverrideShaderChunkSettingsForPlatform(buildTarget);
        }

        public void SetOverrideShaderChunkSettingsForPlatform_Internal(BuildTarget buildTarget, bool value)
        {
            m_PlayerSettings.SetOverrideShaderChunkSettingsForPlatform(buildTarget, value);
        }

        public int GetShaderChunkSizeInMBForPlatform_Internal(BuildTarget buildTarget)
        {
            return m_PlayerSettings.GetShaderChunkSizeInMBForPlatform(buildTarget);
        }

        public void SetShaderChunkSizeInMBForPlatform_Internal(BuildTarget buildTarget, int sizeInMegabytes)
        {
            m_PlayerSettings.SetShaderChunkSizeInMBForPlatform(buildTarget, sizeInMegabytes);
        }

        public int GetShaderChunkCountForPlatform_Internal(BuildTarget buildTarget)
        {
            return m_PlayerSettings.GetShaderChunkCountForPlatform(buildTarget);
        }

        public void SetShaderChunkCountForPlatform_Internal(BuildTarget buildTarget, int chunkCount)
        {
            m_PlayerSettings.SetShaderChunkCountForPlatform(buildTarget, chunkCount);
        }

        public void GetBatchingForPlatform_Internal(BuildTarget platform, out int staticBatching, out int dynamicBatching)
        {
            m_PlayerSettings.GetBatchingForPlatform(platform, out staticBatching, out dynamicBatching);
        }

        public void SetBatchingForPlatform_Internal(BuildTarget platform, int staticBatching, int dynamicBatching)
        {
            m_PlayerSettings.SetBatchingForPlatform(platform, staticBatching, dynamicBatching);
        }

        public bool GetGraphicsJobsForPlatform_Internal(BuildTarget platform)
        {
            return m_PlayerSettings.GetGraphicsJobsForPlatform(platform);
        }

        public void SetGraphicsJobsForPlatform_Internal(BuildTarget platform, bool graphicsJobs)
        {
            m_PlayerSettings.SetGraphicsJobsForPlatform(platform, graphicsJobs);
        }

        public GraphicsJobMode GetGraphicsJobModeForPlatform_Internal(BuildTarget platform)
        {
            return m_PlayerSettings.GetGraphicsJobModeForPlatform(platform);
        }

        public void SetGraphicsJobModeForPlatform_Internal(BuildTarget platform, GraphicsJobMode gfxJobMode)
        {
            m_PlayerSettings.SetGraphicsJobModeForPlatform(platform, gfxJobMode);
        }

        public void SetGraphicsThreadingModeForPlatform_Internal(BuildTarget platform, GfxThreadingMode gfxJobMode)
        {
            m_PlayerSettings.SetGraphicsThreadingModeForPlatform(platform, gfxJobMode);
        }

        public NormalMapEncoding GetNormalMapEncoding_Internal(string platform)
        {
            return m_PlayerSettings.GetNormalMapEncoding(platform);
        }

        public void SetNormalMapEncoding_Internal(string platform, NormalMapEncoding encoding)
        {
            m_PlayerSettings.SetNormalMapEncoding(platform, encoding);
        }

        public bool GetLightmapStreamingEnabledForPlatformGroup_Internal(BuildTargetGroup platformGroup)
        {
            return m_PlayerSettings.GetLightmapStreamingEnabledForPlatformGroup(platformGroup);
        }

        public void SetLightmapStreamingEnabledForPlatformGroup_Internal(BuildTargetGroup platformGroup, bool lightmapStreamingEnabled)
        {
            m_PlayerSettings.SetLightmapStreamingEnabledForPlatformGroup(platformGroup, lightmapStreamingEnabled);
        }

        public int GetLightmapStreamingPriorityForPlatformGroup_Internal(BuildTargetGroup platformGroup)
        {
            return m_PlayerSettings.GetLightmapStreamingPriorityForPlatformGroup(platformGroup);
        }

        public void SetLightmapStreamingPriorityForPlatformGroup_Internal(BuildTargetGroup platformGroup, int lightmapStreamingPriority)
        {
            m_PlayerSettings.SetLightmapStreamingPriorityForPlatformGroup(platformGroup, lightmapStreamingPriority);
        }

        public LightmapEncodingQuality GetLightmapEncodingQualityForPlatform_Internal(BuildTarget platform)
        {
            return m_PlayerSettings.GetLightmapEncodingQualityForPlatform(platform);
        }

        public void SetLightmapEncodingQualityForPlatform_Internal(BuildTarget platform, LightmapEncodingQuality encodingQuality)
        {
            m_PlayerSettings.SetLightmapEncodingQualityForPlatform(platform, encodingQuality);
        }

        public HDRCubemapEncodingQuality GetHDRCubemapEncodingQualityForPlatform_Internal(BuildTarget platform)
        {
            return m_PlayerSettings.GetHDRCubemapEncodingQualityForPlatform(platform);
        }

        public void SetHDRCubemapEncodingQualityForPlatform_Internal(BuildTarget platform, HDRCubemapEncodingQuality encodingQuality)
        {
            m_PlayerSettings.SetHDRCubemapEncodingQualityForPlatform(platform, encodingQuality);
        }

        public bool GetLoadStoreDebugModeEnabledForPlatformGroup_Internal(BuildTargetGroup platformGroup)
        {
            return m_PlayerSettings.GetLoadStoreDebugModeEnabledForPlatformGroup(platformGroup);
        }

        public void SetLoadStoreDebugModeEnabledForPlatformGroup_Internal(BuildTargetGroup platformGroup, bool loadStoreDebugModeEnabled)
        {
            m_PlayerSettings.SetLoadStoreDebugModeEnabledForPlatformGroup(platformGroup, loadStoreDebugModeEnabled);
        }

        public bool GetLoadStoreDebugModeEditorOnlyForPlatformGroup_Internal(BuildTargetGroup platformGroup)
        {
            return m_PlayerSettings.GetLoadStoreDebugModeEditorOnlyForPlatformGroup(platformGroup);
        }

        public void SetLoadStoreDebugModeEditorOnlyForPlatformGroup_Internal(BuildTargetGroup platformGroup, bool loadStoreDebugModeEnabled)
        {
            m_PlayerSettings.SetLoadStoreDebugModeEditorOnlyForPlatformGroup(platformGroup, loadStoreDebugModeEnabled);
        }

        public bool HasAnyNetFXCompatibilityLevel()
        {
            return m_PlayerSettings.HasAnyNetFXCompatibilityLevel();
        }

        public ScriptingImplementation GetScriptingBackend_Internal(string buildTargetGroupName)
        {
            return m_PlayerSettings.GetScriptingBackend(buildTargetGroupName);
        }

        public string GetTemplateCustomValue_Internal(string name)
        {
            return m_PlayerSettings.GetTemplateCustomValue(name);
        }

        public void SetTemplateCustomValue_Internal(string name, string value)
        {
            m_PlayerSettings.SetTemplateCustomValue(name, value);
        }

        public void SetTemplateCustomKeys_Internal(string[] templateCustomKeys)
        {
            m_PlayerSettings.SetTemplateCustomKeys(templateCustomKeys);
        }

        public bool GetMobileMTRenderingInternal_Instance(string buildTargetName)
        {
            return m_PlayerSettings.GetMobileMTRendering(buildTargetName);
        }

        public void SetMobileMTRenderingInternal_Instance(string buildTargetName, bool enable)
        {
            m_PlayerSettings.SetMobileMTRendering(buildTargetName, enable);
        }

        public GraphicsDeviceType[] GetPlatformAutomaticGraphicsAPIsList(BuildTarget platform)
        {
            return m_PlayerSettings.GetPlatformAutomaticGraphicsAPIsList(platform);
        }

        public IPlayerSettingsiOSAccessor iOS => this;

        string[] IPlayerSettingsiOSAccessor.GetAssetBundleVariantsWithDeviceRequirements_Internal()
        {
            return PlayerSettings.iOS.GetAssetBundleVariantsWithDeviceRequirements_Internal(m_PlayerSettings);
        }

        iOSDeviceRequirementGroup IPlayerSettingsiOSAccessor.GetDeviceRequirementsForAssetBundleVariant_Internal(string name)
        {
            return PlayerSettings.iOS.GetDeviceRequirementsForAssetBundleVariant_Internal(m_PlayerSettings, name);
        }

        iOSDeviceRequirementGroup IPlayerSettingsiOSAccessor.AddDeviceRequirementsForAssetBundleVariant_Internal(string name)
        {
            return PlayerSettings.iOS.AddDeviceRequirementsForAssetBundleVariant_Internal(m_PlayerSettings, name);
        }
    }

    protected override IPlayerSettingsAccessor CreateAccessor(UnityEngine.Object target)
    {
        return new BuildProfilePlayerSettingsAccessor(target as BuildProfilePlayerSettings);
    }


}
