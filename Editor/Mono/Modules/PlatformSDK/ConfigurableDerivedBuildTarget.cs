// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using static UnityEditor.BuildTargetDiscovery;

namespace UnityEditor.Modules;

class ConfigurableDerivedBuildTarget : ConfigurableBuildTarget, IDerivedBuildTarget
{
    public GUID BaseGuid => m_BuildTarget.Guid;

    public DiscoveredTargetInfo GetDerivedBuildTargetInfo(DiscoveredTargetInfo baseBuildTargetInfo)
    {
        var derivedBuildTargetInfo = baseBuildTargetInfo;
        derivedBuildTargetInfo.iconName = m_IconName;
        derivedBuildTargetInfo.niceName = DisplayName;
        return derivedBuildTargetInfo;
    }

    public ConfigurableDerivedBuildTarget(SDKPlatformProvider platformProvider, PlatformInfo platformInfo, IBuildTarget baseBuildTarget)
    {
        Guid = platformProvider.guid;
        DisplayName = platformInfo.displayName;
        TargetName = platformProvider.targetName;
        m_IconName = platformInfo.iconName;
        m_BuildTarget = baseBuildTarget;
    }
}
