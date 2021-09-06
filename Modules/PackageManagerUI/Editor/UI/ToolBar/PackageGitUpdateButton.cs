// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageGitUpdateButton : PackageToolBarRegularButton
    {
        private PackageDatabase m_PackageDatabase;
        public PackageGitUpdateButton(PackageDatabase packageDatabase)
        {
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction()
        {
            var installedVersion = m_Package.versions.installed;
            m_PackageDatabase.Install(installedVersion.packageInfo.packageId);
            PackageManagerWindowAnalytics.SendEvent("updateGit", installedVersion.uniqueId);
            return true;
        }

        protected override bool isVisible => m_Package?.versions.installed?.HasTag(PackageTag.Git) == true;

        protected override string GetTooltip(bool isInProgress)
        {
            if (isInProgress)
                return k_InProgressGenericTooltip;
            return L10n.Tr("Click to check for updates and update to latest version");
        }

        protected override string GetText(bool isInProgress)
        {
            return L10n.Tr("Update");
        }

        protected override bool isInProgress => m_PackageDatabase.IsInstallInProgress(m_Version);
    }
}
