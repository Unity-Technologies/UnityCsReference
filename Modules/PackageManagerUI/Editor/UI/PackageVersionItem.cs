// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageVersionItem : Label, ISelectableItem
    {
        public IPackage package { get; set; }
        public IPackageVersion version { get; set; }

        public PackageVersionItem(IPackage package, IPackageVersion version)
        {
            this.package = package;
            this.version = version;
            RefreshLabel();
            this.OnLeftClick(() => PageManager.instance.SetSelected(package, version));
        }

        public IPackageVersion targetVersion { get { return version; } }
        public VisualElement element { get { return this; } }

        private void RefreshLabel()
        {
            text = PackageItem.GetVersionText(version);
        }
    }
}
