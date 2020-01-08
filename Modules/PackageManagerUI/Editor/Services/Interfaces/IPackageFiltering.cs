// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageFiltering
    {
        PackageFilterTab currentFilterTab { get; set; }

        PackageFilterTab defaultFilterTab { get; }

        string currentSearchText { get; set; }

        event Action<PackageFilterTab> onFilterTabChanged;
        event Action<string> onSearchTextChanged;

        bool FilterByCurrentSearchText(IPackage package);
        bool FilterByCurrentTab(IPackage package);

        void SetCurrentFilterTabWithoutNotify(PackageFilterTab tab);
    }
}
