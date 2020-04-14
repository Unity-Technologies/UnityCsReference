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

        private PageManager m_PageManager;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_PageManager = container.Resolve<PageManager>();
        }

        public PackageVersionItem(IPackage package, IPackageVersion version)
        {
            ResolveDependencies();

            this.package = package;
            this.version = version;
            RefreshLabel();
            this.OnLeftClick(() => m_PageManager.SetSelected(package, version, true));
        }

        public IPackageVersion targetVersion { get { return version; } }
        public VisualElement element { get { return this; } }

        private void RefreshLabel()
        {
            text = PackageItem.GetVersionText(version);
        }
    }
}
