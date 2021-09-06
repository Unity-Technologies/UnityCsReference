// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageUnlockButton : PackageToolBarRegularButton
    {
        private PageManager m_PageManager;
        public PackageUnlockButton(PageManager pageManager)
        {
            m_PageManager = pageManager;
        }

        protected override bool TriggerAction()
        {
            m_PageManager.SetPackagesUserUnlockedState(new string[1] { m_Package.uniqueId }, true);
            PackageManagerWindowAnalytics.SendEvent("unlock", m_Package.uniqueId);
            return true;
        }

        protected override bool isVisible => m_PageManager.GetVisualState(m_Package)?.isLocked == true;

        protected override string GetTooltip(bool isInProgress)
        {
            return L10n.Tr("Unlock to make changes");
        }

        protected override string GetText(bool isInProgress)
        {
            return L10n.Tr("Unlock");
        }

        protected override bool isInProgress => false;
    }
}
