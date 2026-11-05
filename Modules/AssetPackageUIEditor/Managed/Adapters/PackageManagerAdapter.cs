// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.AssetPackage;

internal interface IPackageManagerAdapter
{
    public bool IsTrustDisabled { get; }
}

internal class PackageManagerAdapter : IPackageManagerAdapter
{
    // If a user loses bypass trust entitlement, we turn back on trust even if Project Settings says otherwise
    public bool IsTrustDisabled => PackageManager.TrustPolicySettings.trustDisabled && PackageManager.PackageTrustLevel.HasBypassPackageTrustEntitlement();
}
