// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using static UnityEditor.BuildTargetDiscovery;

namespace UnityEditor.Modules;

internal interface IMultiTargetBuildTarget : IBuildTarget
{
    GUID selectedGuid { get; }
    void SetBuildTarget(IBuildTarget selectedBuildTarget);
}

class ConfigurableMultiTargetBuildTarget : ConfigurableBuildTarget, IMultiTargetBuildTarget
{
    public GUID selectedGuid => m_BuildTarget.Guid;
    public IBuildTarget[] availableBuildTargets => m_AvailableBuildTargets;

    IBuildTarget[] m_AvailableBuildTargets;

    public void SetBuildTarget(IBuildTarget selectedBuildTarget)
    {
        m_BuildTarget = selectedBuildTarget;
    }

    public ConfigurableMultiTargetBuildTarget(SDKPlatformProvider platformProvider, PlatformInfo platformInfo, IBuildTarget[] baseBuildTargets)
    {
        Guid = platformProvider.guid;
        DisplayName = platformInfo.displayName;
        TargetName = platformProvider.targetName;
        m_IconName = platformInfo.iconName;
        m_BuildTarget = baseBuildTargets[0];
        m_AvailableBuildTargets = baseBuildTargets;
    }
}
