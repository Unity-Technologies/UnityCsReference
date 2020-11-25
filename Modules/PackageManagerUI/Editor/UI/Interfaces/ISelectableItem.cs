// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ISelectableItem
    {
        IPackage package { get; }
        IPackageVersion targetVersion { get; }
        VisualElement element { get; }
    }
}
