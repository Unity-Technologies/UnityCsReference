// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPage
    {
        PackageFilterTab tab { get; }
        bool isLoading { get; }
        bool morePackagesToFetch { get; }
        List<VisualState> packageVisualStates { get; }
        VisualState GetVisualState(string packageUniqueId);
        void LoadMore();
        void Load(IPackage package, IPackageVersion version = null);
    }
}
