// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace UnityEditor.Modules;

[Serializable]
internal struct CachedSDKPlatformDefines
{
    public string platformGuid;
    public string[] defines;
}

internal static partial class SDKPlatformCompilationUtil
{
    public static ICompilationExtension CreateActiveCompilationExtension(IPlatformSupportModule platformSupportModule)
    {
        var baseExtension = platformSupportModule?.CreateCompilationExtension();
        var activeProfile = GetActiveSDKPlatformProfile();
        if (activeProfile == null || platformSupportModule == null)
            return baseExtension;

        // Resolved through the build target because an SDK derived platform GUID is not registered with a module until its package is loaded.
        if (platformSupportModule != ModuleManager.FindPlatformSupportModule(ModuleManager.GetTargetStringFromBuildTarget(activeProfile.buildTarget)))
            return baseExtension;

        var sdkPlatformExtension = GetActivePlatformExtension();
        if (sdkPlatformExtension == null)
        {
            var cachedDefines = GetCachedDefines(activeProfile.platformGuid);
            return cachedDefines.Length > 0 ? CompositeCompilationExtension.Combine(baseExtension, new ConfigurableCompilationExtension(cachedDefines)) : baseExtension;
        }

        // An SDK derived platform's define comes through its platform support module.
        return activeProfile.isMultiTarget ? sdkPlatformExtension.CreateCompilationExtension(baseExtension) : baseExtension;
    }

    public static string[] GetActivePlatformEditorDefines()
    {
        var activeProfile = GetActiveSDKPlatformProfile();
        if (activeProfile == null)
            return Array.Empty<string>();

        var sdkPlatformExtension = GetActivePlatformExtension();
        if (sdkPlatformExtension == null)
            return GetCachedDefines(activeProfile.platformGuid);

        var defines = new List<string>();

        // A multi-target platform's selected platform can be an SDK derived platform, which adds its own define.
        if (activeProfile.isMultiTarget && BuildTargetDiscovery.TryGetSDKPlatformExtension(activeProfile.selectedPlatformGuid, out var selectedPlatformExtension))
            defines.AddRange(selectedPlatformExtension.additionalCompilationExtension.GetAdditionalEditorDefines());

        defines.AddRange(sdkPlatformExtension.additionalCompilationExtension.GetAdditionalEditorDefines());
        return defines.ToArray();
    }

    public static CachedSDKPlatformDefines GetActivePlatformDefinesToCache()
    {
        var platformGuid = GetActiveSDKPlatformProfile()?.platformGuid.ToString() ?? string.Empty;
        return new CachedSDKPlatformDefines { platformGuid = platformGuid, defines = GetActivePlatformEditorDefines() };
    }

    static ISDKPlatformExtension GetActivePlatformExtension()
    {
        var activeProfile = GetActiveSDKPlatformProfile();
        if (activeProfile == null)
            return null;

        return BuildTargetDiscovery.TryGetSDKPlatformExtension(activeProfile.platformGuid, out var sdkPlatformExtension) ? sdkPlatformExtension : null;
    }

    static BuildProfile GetActiveSDKPlatformProfile()
    {
        var activeProfile = BuildProfileContext.activeProfile;
        if (activeProfile != null && BuildTargetDiscovery.BuildPlatformIsSDKPlatform(activeProfile.platformGuid))
            return activeProfile;

        return null;
    }

    static string[] GetCachedDefines(GUID sdkPlatformGuid)
    {
        if (!EditorUserBuildSettings.isBuildProfileAvailable)
            return Array.Empty<string>();

        var cached = BuildProfileContext.instance.cachedSDKPlatformDefines;
        return cached.platformGuid == sdkPlatformGuid.ToString() ? cached.defines : Array.Empty<string>();
    }

    // The first compilation of an editor session runs before SDK platforms are registered from their packages, so it can
    // only use the cached defines. Recompiles if the cache defines don't match the registered platform's defines.
    [OnCodeLoaded]
    static void RequestCompilationIfActivePlatformDefinesChanged()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorUserBuildSettings.isBuildProfileAvailable)
                return;

            var activeDefines = GetActivePlatformExtension() != null ? GetActivePlatformEditorDefines() : Array.Empty<string>();
            if (ArrayUtility.ArrayEquals(activeDefines, BuildProfileContext.instance.cachedSDKPlatformDefines.defines))
                return;

            BuildProfileContext.instance.cachedSDKPlatformDefines = new CachedSDKPlatformDefines { platformGuid = string.Empty, defines = Array.Empty<string>() };
            BuildProfileModuleUtil.RequestScriptCompilation(BuildProfileContext.activeProfile);
        };
    }
}
