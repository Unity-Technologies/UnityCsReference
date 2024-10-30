// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

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

        internal IBuildProfileExtension CreateBuildProfileExtension(GUID buildTarget, IBuildProfileExtension baseBuildProfileExtension)
        {
            if (m_DerivedBuildTargetExtensions.TryGetValue(buildTarget, out var derivedBuildTargetExtensions))
            {
                return derivedBuildTargetExtensions.BuildProfileExtension ?? baseBuildProfileExtension;
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
            if (EditorPrefs.HasKey(guid.ToString()) && (EditorPrefs.GetInt(guid.ToString()) == 1))
            {
                m_DerivedBuildTargetExtensions.Add(guid, derivedBuildTargetExtensions);
            }
        }

        internal void LoadAndEnableDerivedPlatformPlugin<T>() where T : IDerivedBuildTargetExtensions, new()
        {
            var derivedBuildTargetExtensions = new T();
            var guid = derivedBuildTargetExtensions.DerivedBuildTarget.Guid;
            if ((!EditorPrefs.HasKey(guid.ToString())) || (EditorPrefs.GetInt(guid.ToString()) != 1))
            {
                EditorPrefs.SetInt(guid.ToString(), 1);
            }
            LoadDerivedPlatformPlugin<T>();
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
    }
}
