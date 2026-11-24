// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class VersionsGroupHeader : VisualElement
    {
        public VersionsGroupHeader(IPackageVersion version, IUpmCache upmCache)
        {
            string text;
            var icon = Icon.None;

            if (version.HasTag(PackageTag.Git))
                text = L10n.Tr("Git", null);
            else if (version.HasTag(PackageTag.InstalledFromPath))
                text = L10n.Tr("Local", null);
            else
            {
                switch (version.availableRegistry)
                {
                    case RegistryType.UnityRegistry:
                        text = L10n.Tr("Unity Registry", null);
                        icon = Icon.UnityRegistryPage;
                        break;
                    case RegistryType.AssetStore:
                        text = L10n.Tr("Asset Store", null);
                        icon = Icon.MyAssetsPage;
                        break;
                    case RegistryType.MyRegistries:
                        var packageInfo = upmCache.GetBestMatchPackageInfo(version.name, version.isInstalled);
                        text = packageInfo?.registry?.name ?? L10n.Tr("My Registries", null);
                        icon = Icon.MyRegistriesPage;
                        break;
                    default:
                        text = L10n.Tr("Unknown", null);
                        break;
                }
            }

            if (icon != Icon.None)
            {
                var iconElement = new VisualElement { name = "versionsGroupHeaderIcon" };
                iconElement.AddToClassList(icon.ClassName());
                Add(iconElement);
            }

            Add(new Label(text) { name = "versionsGroupHeaderLabel" });
        }
    }
}
