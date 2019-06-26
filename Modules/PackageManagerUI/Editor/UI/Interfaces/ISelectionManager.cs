// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface ISelectionManager
    {
        event Action<IEnumerable<IPackageVersion>> onSelectionChanged;

        IEnumerable<IPackageVersion> GetSelections();

        void ClearSelection();

        void SetSelected(IPackage package, IPackageVersion version = null);

        bool IsSelected(IPackage package, IPackageVersion version = null);

        void SetSeeAllVersions(IPackage package, bool value);

        bool IsSeeAllVersions(IPackage package);

        void SetExpanded(IPackage package, bool value);

        bool IsExpanded(IPackage package);

        void Setup();
    }
}
