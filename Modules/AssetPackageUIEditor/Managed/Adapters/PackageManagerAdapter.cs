// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading.Tasks;
using UnityEditor.PackageManager.UI.Internal;

namespace UnityEditor.AssetPackage;

internal interface IPackageManagerAdapter
{
    public bool HasBypassPackageTrustEntitlement { get; }
}

internal class PackageManagerAdapter : IPackageManagerAdapter
{
    public bool HasBypassPackageTrustEntitlement => PackageManager.PackageTrustLevel.HasBypassPackageTrustEntitlement();
}
