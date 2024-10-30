// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    using DiscoveredTargetInfo = BuildTargetDiscovery.DiscoveredTargetInfo;

    internal interface IDerivedBuildTarget : IBuildTarget
    {
        GUID BaseGuid { get; }
        DiscoveredTargetInfo GetDerivedBuildTargetInfo(DiscoveredTargetInfo baseBuildTargetInfo);
    }
}
