// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// <see cref="PlatformDiscoveryWindow"/> required and recommended package list item.
    /// </summary>
    internal class PlatformPackageItem : VisualElement
    {
        const string k_Uxml = "BuildProfile/UXML/PlatformPackageItemElement.uxml";

        readonly Toggle m_ShouldInstallToggle;
        readonly Label m_Text;
        readonly Label m_RequiredIndicator;
        readonly Label m_InstalledIndicator;
        PlatformPackageEntry m_Entry;

        internal PlatformPackageItem()
        {
            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(stylesheet);
            uxml.CloneTree(this);

            m_ShouldInstallToggle = this.Q<Toggle>("package-list-toggle-active");
            m_ShouldInstallToggle.RegisterValueChangedCallback(evt =>
            {
                if (m_Entry != null)
                    m_Entry.shouldInstalled = evt.newValue;
            });
            m_Text = this.Q<Label>("package-list-label-name");
            m_RequiredIndicator = this.Q<Label>("package-list-label-required");
            m_RequiredIndicator.text = TrText.required;
            m_InstalledIndicator = this.Q<Label>("package-list-label-installed");
            m_InstalledIndicator.tooltip = TrText.packageInstalled;
        }

        internal void Set(PlatformPackageEntry entry)
        {
            m_Entry = entry;
            m_Text.text = entry.packageName;
            SetInstalledIndicator(entry.isInstalled);
            SetRequiredIndicator(entry.required);
            m_ShouldInstallToggle.SetValueWithoutNotify(entry.shouldInstalled || entry.isInstalled);
            m_ShouldInstallToggle.SetEnabled(!entry.required && !entry.isInstalled);
        }

        internal void SetShouldInstallToggle(bool selection)
        {
            // Required entries are always selected in package list view.
            if (m_Entry.required || m_Entry.isInstalled)
                return;

            m_ShouldInstallToggle.value = selection;
        }

        void SetInstalledIndicator(bool active)
        {
            if (active)
                m_InstalledIndicator.Show();
            else
                m_InstalledIndicator.Hide();
        }

        void SetRequiredIndicator(bool active)
        {
            if (active)
                m_RequiredIndicator.Show();
            else
                m_RequiredIndicator.Hide();
        }

        public static PlatformPackageEntry[] CreateItemSource(string[] requiredPackages, string[] recommendedPackages)
        {
            var result = new PlatformPackageEntry[requiredPackages.Length + recommendedPackages.Length];

            for (int index = 0; index < requiredPackages.Length; index++)
            {
                var packageName = requiredPackages[index];
                result[index] = new PlatformPackageEntry(packageName, true, true,
                    PackageManager.PackageInfo.IsPackageRegistered(packageName));
            }

            for (int index = 0; index < recommendedPackages.Length; index++)
            {
                var packageName = recommendedPackages[index];
                bool isPackageInstalled = PackageManager.PackageInfo.IsPackageRegistered(packageName);
                result[index + requiredPackages.Length] = new PlatformPackageEntry(packageName, isPackageInstalled, false, isPackageInstalled);
            }

            return result;
        }

        public static PlatformPackageItem Make()
        {
            var item = new PlatformPackageItem();
            return item;
        }
    }
}
