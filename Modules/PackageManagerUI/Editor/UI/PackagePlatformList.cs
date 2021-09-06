// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackagePlatformList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackagePlatformList> {}

        private IPackageVersion m_Version;
        private ResourceLoader m_ResourceLoader;
        private UpmCache m_UpmCache;
        private bool m_MorePlatformView;
        public PackagePlatformList()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackagePlatformList.uxml");
            Add(root);

            cache = new VisualElementCache(root);

            buttonPlatformMore.clickable.clicked += PlatformMoreClick;
            buttonPlatformLess.clickable.clicked += PlatformLessClick;
        }

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_UpmCache = container.Resolve<UpmCache>();
        }

        public void SetPackageVersion(IPackageVersion version)
        {
            m_Version = version;
            CheckForSupportedPlatforms();
        }

        private void CheckForSupportedPlatforms()
        {
            UIUtils.SetElementDisplay(this, false);

            if (m_Version?.packageInfo == null)
                return;

            var platformNames = GetPlatforms(m_Version);
            if (!platformNames.Any())
                return;

            UIUtils.SetElementDisplay(this, true);
            UIUtils.SetElementDisplay(buttonPlatformMore, false);
            UIUtils.SetElementDisplay(buttonPlatformLess, false);

            platformList.Clear();
            platformList.Add(supportedPlatformsLabel);

            AdaptSupportedPlatformLabel(platformNames.Count() > 1);
            DisplaySupportedPlatforms(platformNames);
        }

        private IEnumerable<string> GetPlatforms(IPackageVersion version)
        {
            var upmReserved = m_UpmCache.ParseUpmReserved(version?.packageInfo);
            return upmReserved?.GetList<string>("supportedPlatforms") ?? Enumerable.Empty<string>();
        }

        private void AdaptSupportedPlatformLabel(bool moreThanOnePlatform)
        {
            supportedPlatformsLabel.text = moreThanOnePlatform ? L10n.Tr("Supported Platforms:") : L10n.Tr("Supported Platform:");
        }

        private void DisplaySupportedPlatforms(IEnumerable<string> platformNames)
        {
            foreach (var platformName in platformNames)
            {
                var packageTagPlatform = new PackageTagLabel {classList = {"platform"}};
                packageTagPlatform.text = platformName;
                platformList.Add(packageTagPlatform);
            }

            if (platformNames.Count() < 5)
                return;

            platformList.Add(buttonPlatformMore);
            platformList.Add(buttonPlatformLess);
            PlatformLessClick();
        }

        internal void PlatformMoreClick()
        {
            UIUtils.SetElementDisplay(buttonPlatformMore, false);
            UIUtils.SetElementDisplay(buttonPlatformLess, true);
            m_MorePlatformView = true;
            HideShowPlatforms();
        }

        private void PlatformLessClick()
        {
            UIUtils.SetElementDisplay(buttonPlatformMore, true);
            UIUtils.SetElementDisplay(buttonPlatformLess, false);
            m_MorePlatformView = false;
            HideShowPlatforms();
        }

        private void HideShowPlatforms()
        {
            var platformElements = platformList.Children().OfType<PackageTagLabel>();
            // We start at 3 to keep the first three platforms visible
            for (var i = 3; i < platformElements.Count(); i++)
            {
                var platform = platformElements.ElementAt(i);
                UIUtils.SetElementDisplay(platform, m_MorePlatformView);
            }
        }

        private VisualElementCache cache { get; set; }
        private VisualElement platformList => cache.Get<VisualElement>("platformsList");
        private Label supportedPlatformsLabel => cache.Get<Label>("supportedPlatformsLabel");
        private Button buttonPlatformMore => cache.Get<Button>("buttonPlatformMore");
        private Button buttonPlatformLess => cache.Get<Button>("buttonPlatformLess");
    }
}
