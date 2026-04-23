// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Modules
{
    using DiscoveredTargetInfo = BuildTargetDiscovery.DiscoveredTargetInfo;

    internal class DerivedBuildTargetExtensionsProvider
    {
        Dictionary<GUID, IDerivedBuildTargetExtensions> m_DerivedBuildTargetExtensions = new();
        DiscoveredTargetInfo m_DiscoveredTargetInfo;
        BuildTarget m_BuildTarget;

        internal DerivedBuildTargetExtensionsProvider(BuildTarget buildTarget)
        {
            m_BuildTarget = buildTarget;
            LoadDiscoveredTargetInfo();
        }

        internal IBuildTarget GetIBuildTarget(IBuildTarget baseBuildTarget)
        {
            var activePlatformGuid = EditorUserBuildSettings.activePlatformGuid;
            if (m_DerivedBuildTargetExtensions.TryGetValue(activePlatformGuid, out var derivedBuildTargetExtensions))
            {
                return derivedBuildTargetExtensions.DerivedBuildTarget;
            }
            return baseBuildTarget;
        }

        internal ICompilationExtension CreateCompilationExtension(ICompilationExtension baseCompilationExtension)
        {
            var activePlatformGuid = EditorUserBuildSettings.activePlatformGuid;
            if (m_DerivedBuildTargetExtensions.TryGetValue(activePlatformGuid, out var derivedBuildTargetExtensions))
            {
                return derivedBuildTargetExtensions.CompilationExtension ?? baseCompilationExtension;
            }
            return baseCompilationExtension;
        }

        internal IBuildPostprocessor CreateBuildPostprocessor(IBuildPostprocessor baseBuildPostprocessor)
        {
            var activePlatformGuid = EditorUserBuildSettings.activePlatformGuid;
            if (m_DerivedBuildTargetExtensions.TryGetValue(activePlatformGuid, out var derivedBuildTargetExtensions))
            {
                return derivedBuildTargetExtensions.BuildPostprocessor ?? baseBuildPostprocessor;
            }
            return baseBuildPostprocessor;
        }

        internal ISettingEditorExtension CreateSettingsEditorExtension(GUID buildTarget, ISettingEditorExtension baseSettingEditorExtension)
        {
            if (m_DerivedBuildTargetExtensions.TryGetValue(buildTarget, out var derivedBuildTargetExtensions))
            {
                return derivedBuildTargetExtensions.CreateSettingEditorExtension() ?? baseSettingEditorExtension;
            }
            return baseSettingEditorExtension;
        }

        internal IBuildProfileExtension CreateBuildProfileExtension(GUID buildTarget, IBuildProfileExtension baseBuildProfileExtension)
        {
            if (m_DerivedBuildTargetExtensions.TryGetValue(buildTarget, out var derivedBuildTargetExtensions))
            {
                return derivedBuildTargetExtensions.CreateBuildProfileExtension() ?? baseBuildProfileExtension;
            }
            return baseBuildProfileExtension;
        }

        internal IEnumerable<IDerivedBuildTarget> GetDerivedBuildTargets()
        {
            List<IDerivedBuildTarget> derivedBuildTargets = new();
            foreach (var extension in m_DerivedBuildTargetExtensions.Values)
                derivedBuildTargets.Add(extension.DerivedBuildTarget);
            return derivedBuildTargets;
        }

        internal bool TryGetDiscoveredTargetInfo(GUID buildTarget, out DiscoveredTargetInfo discoveredTargetInfo)
        {
            if (m_DerivedBuildTargetExtensions.TryGetValue(buildTarget, out var derivedBuildTargetExtensions))
            {
                discoveredTargetInfo = derivedBuildTargetExtensions.DerivedBuildTarget.GetDerivedBuildTargetInfo(m_DiscoveredTargetInfo);
                return true;
            }

            discoveredTargetInfo = default;
            return false;
        }

        internal void LoadDerivedPlatformPlugin<T>() where T : IDerivedBuildTargetExtensions, new()
        {
            var derivedBuildTargetExtensions = new T();
            var guid = derivedBuildTargetExtensions.DerivedBuildTarget.Guid;
            var guidStr = guid.ToString();
            var platformEnabled = EditorPrefs.HasKey(guidStr) && (EditorPrefs.GetInt(guidStr) == 1);
            if (platformEnabled || ForceEnablePlatform())
            {
                m_DerivedBuildTargetExtensions.Add(guid, derivedBuildTargetExtensions);
            }
        }

        internal void RemoveDerivedPlatformPlugin(GUID guid)
        {
            m_DerivedBuildTargetExtensions.Remove(guid);
        }

        bool ForceEnablePlatform()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg == "-enableAllDerivedPlatforms")
                {
                    return true;
                }
            }
            return false;
        }

        void LoadDiscoveredTargetInfo()
        {
            var list = BuildTargetDiscovery.GetBuildTargetInfoList();
            foreach (var info in list)
            {
                if (info.buildTargetPlatformVal == m_BuildTarget)
                {
                    m_DiscoveredTargetInfo = info;
                }
            }
        }

        internal delegate IBuildProfileExtension CreateBuildProfileExtensionFunction();

        internal void LoadSDKDerivedPlatforms(IBuildTarget baseIBuildTarget, CreateBuildProfileExtensionFunction createBaseBuildProfileExtensionFunction)
        {
            var types = TypeCache.GetTypesDerivedFrom<IPlatformProvider>();
            foreach (var type in types)
            {
                if (!BuildTargetDiscovery.TryCreateIPlatformProvider(type, out var provider))
                    continue;

                var sdkPlatformProvider = SDKPlatformProvider.TryCreateDerivedPlatformProvider(provider);
                if (sdkPlatformProvider == null)
                    continue;

                LoadSDKDerivedPlatformExtension(sdkPlatformProvider, baseIBuildTarget, createBaseBuildProfileExtensionFunction);
            }
        }

        void LoadSDKDerivedPlatformExtension(SDKPlatformProvider sdkPlatformProvider, IBuildTarget baseIBuildTarget, CreateBuildProfileExtensionFunction createBaseBuildProfileExtensionFunction)
        {
            if (!BuildTargetDiscovery.TryGetPlatformInfo(sdkPlatformProvider.guid, out var platformInfo))
            {
                Debug.LogError(string.Format(BuildTargetDiscovery.k_SDKProviderMissingPlatformInfoError, sdkPlatformProvider.providerType.FullName));
                return;
            }

            if (!BuildTargetDiscovery.BuildPlatformIsDerivedPlatform(sdkPlatformProvider.guid))
            {
                Debug.LogError(string.Format(BuildTargetDiscovery.k_SDKProviderNotDerivedTargetError, sdkPlatformProvider.providerType.FullName, sdkPlatformProvider.guid));
                return;
            }

            if (platformInfo.buildTarget != m_BuildTarget)
                return;

            var basePlatformGuid = BuildTargetDiscovery.GetBasePlatformGUID(sdkPlatformProvider.guid);
            if (basePlatformGuid != baseIBuildTarget.Guid)
                return;

            if (!BuildTargetDiscovery.BuildPlatformModuleIsInstalled(sdkPlatformProvider.guid))
                return;

            var derivedBuildTarget = new ConfigurableDerivedBuildTarget(sdkPlatformProvider, platformInfo, baseIBuildTarget);
            var derivedBuildTargetExtensions = new ConfigurableDerivedBuildTargetExtensions(sdkPlatformProvider, derivedBuildTarget, createBaseBuildProfileExtensionFunction);
            m_DerivedBuildTargetExtensions.Add(derivedBuildTarget.Guid, derivedBuildTargetExtensions);

            var sdkPlatformExtension = new ConfigurableSDKPlatformExtension(sdkPlatformProvider, derivedBuildTarget);
            BuildTargetDiscovery.RegisterSDKPlatformExtension(derivedBuildTarget.Guid, sdkPlatformExtension);

            BuildTargetDiscovery.SetSDKPlatformInstalledStatus(derivedBuildTarget.Guid, true);
        }
    }
}
