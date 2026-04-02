// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageMultiSelectFoldout(PackageAction action = null) : MultiSelectFoldoutBase<IPackageVersion, IPackage>(action)
    {
        protected override MultiSelectItemBase<IPackage> CreateMultiSelectItem(IPackage item)
        {
           return new PackageMultiSelectItem(item);
        }
    }
}
