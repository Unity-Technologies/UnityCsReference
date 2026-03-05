// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SampleDetailsBody: VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new SampleDetailsBody(ServicesContainer.instance.Resolve<IUpmCache>());
        }

        private readonly VisualElement m_CardsContainer;
        private readonly SelectableLabel m_DescriptionLabel;

        public SampleDetailsBody(IUpmCache upmCache)
        {
            m_CardsContainer = new VisualElement { name = "detailInformationCardsContainer" };
            m_CardsContainer.Add(new SampleParentPackageDisplayNameCard());
            m_CardsContainer.Add(new SourceInfoCard(upmCache));
            m_CardsContainer.Add(new SignatureInfoCard());
            m_CardsContainer.Add(new SampleSizeInfoCard());
            m_DescriptionLabel = new SelectableLabel() { name = "descriptionLabel", };

            Add(m_CardsContainer);
            Add(m_DescriptionLabel);
        }

        public void Refresh(Sample sample)
        {
            m_DescriptionLabel.text = string.IsNullOrEmpty(sample.description) ? L10n.Tr("There is no description for this sample.") : sample.description;

            var showCards = !sample.isDefault;
            UIUtils.SetElementDisplay(m_CardsContainer, showCards);
            if (!showCards)
                return;

            foreach (var child in m_CardsContainer.Children())
            {
                if (child is PackageInformationCard packageCard)
                    packageCard.Refresh(sample.package?.versions.primary);
                else if (child is SampleInformationCard sampleCard)
                    sampleCard.Refresh(sample);
            }
        }
    }
}
