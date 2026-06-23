// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using static UnityEditor.BuildTargetDiscovery;

namespace UnityEditor.Modules;

internal enum SDKPlatformType
{
    Derived = 0,
    MultiTarget = 1
}

[Serializable]
internal class SDKPlatformFlags
{
    public SDKPlatformType platformType;
}

[Serializable]
internal class SDKPlatformInfo
{
    public int version;
    public string guid;
    public string[] supportedPlatformGuids;
    public string basePlatformGuid;
    public string platformGroupName;
    public string displayName;
    public string description;
    public string instructions;
    public string keyFeatures;
    public string resources;
    public string iconName;
    public string bannerBackgroundColorHex;
    public PlatformPackageList internalPackages;
    public PlatformPackageList partnerPackages;
    public SDKPlatformFlags flags;
    public bool isDeprecated;
    public string deprecationMessage;
}
