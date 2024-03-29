// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class MultiSelectItem : VisualElement
    {
        private IPackage m_Package;
        private string m_RightInfoText;

        public MultiSelectItem(IPackage package, string rightInfoText = "")
        {
            m_Package = package;
            m_RightInfoText = rightInfoText;

            m_MainContainer = new VisualElement { name = "mainContainer" };
            Add(m_MainContainer);

            m_LeftContainer = new VisualElement { name = "leftContainer" };
            m_MainContainer.Add(m_LeftContainer);

            m_TypeIcon = new VisualElement { name = "typeIcon" };
            m_LeftContainer.Add(m_TypeIcon);

            m_NameLabel = new Label { name = "nameLabel" };
            m_LeftContainer.Add(m_NameLabel);

            if (m_Package?.versions.primary.HasTag(PackageTag.Feature | PackageTag.LegacyFormat | PackageTag.BuiltIn) == false)
            {
                m_VersionLabel = new Label { name = "versionLabel" };
                m_LeftContainer.Add(m_VersionLabel);
            }

            m_RightContainer = new VisualElement { name = "rightContainer" };
            m_MainContainer.Add(m_RightContainer);

            m_RightInfoLabel = new Label { name = "rightInfoLabel" };
            m_RightContainer.Add(m_RightInfoLabel);

            m_Spinner = null;

            Refresh();
        }

        public void Refresh()
        {
            var version = m_Package?.versions.primary;
            m_TypeIcon.EnableClassToggle("featureIcon", "packageIcon", version?.HasTag(PackageTag.Feature) == true);
            m_NameLabel.text = version?.displayName;
            if (m_VersionLabel != null)
                m_VersionLabel.text = version?.versionString;

            m_RightInfoLabel.text = m_RightInfoText;

            if (version?.HasTag(PackageTag.LegacyFormat) == true)
                return;

            if (m_Package != null && m_Package.progress != PackageProgress.None)
                StartSpinner();
            else
                StopSpinner();
        }

        private void StartSpinner()
        {
            if (m_Spinner == null)
            {
                m_Spinner = new LoadingSpinner { name = "packageSpinner" };
                m_RightContainer.Insert(0, m_Spinner);
            }

            m_Spinner.Start();
            m_Spinner.tooltip = "Operation in progress...";
            UIUtils.SetElementDisplay(m_RightInfoLabel, false);
        }

        private void StopSpinner()
        {
            m_Spinner?.Stop();
            UIUtils.SetElementDisplay(m_RightInfoLabel, true);
        }

        private VisualElement m_MainContainer;
        private VisualElement m_LeftContainer;
        private VisualElement m_TypeIcon;
        private Label m_NameLabel;
        private Label m_VersionLabel;
        private VisualElement m_RightContainer;
        private Label m_RightInfoLabel;
        private LoadingSpinner m_Spinner;
    }
}
