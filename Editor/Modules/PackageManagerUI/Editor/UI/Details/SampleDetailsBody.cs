// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class SampleDetailsBody: VisualElement
    {
        private readonly VisualElement m_CardsContainer;
        private readonly ImageCarousel m_ImageCarousel;
        private readonly SelectableLabel m_DescriptionLabel;

        private readonly IIOProxy m_IOProxy;

        public SampleDetailsBody() : this(ServicesContainer.instance.Resolve<IUpmCache>(), ServicesContainer.instance.Resolve<IIOProxy>(), ServicesContainer.instance.Resolve<IImageLoader>())
        {
        }

        public SampleDetailsBody(IUpmCache upmCache, IIOProxy ioProxy, IImageLoader imageLoader)
        {
            m_IOProxy = ioProxy;

            m_CardsContainer = new VisualElement { name = "detailInformationCardsContainer" };
            m_CardsContainer.Add(new SampleParentPackageDisplayNameCard());
            m_CardsContainer.Add(new SourceInfoCard(upmCache));
            m_CardsContainer.Add(new SignatureInfoCard());
            m_CardsContainer.Add(new SampleSizeInfoCard());

            m_ImageCarousel = new ImageCarousel(imageLoader) { name = "sampleImageCarousel" };
            m_DescriptionLabel = new SelectableLabel { name = "descriptionLabel", };

            Add(m_CardsContainer);
            Add(m_ImageCarousel);
            Add(m_DescriptionLabel);
        }

        public void Refresh(Sample sample)
        {
            var parentPackageVersion = sample.package?.versions.primary;
            if (sample.isDefault || parentPackageVersion == null)
                return;

            m_DescriptionLabel.text = string.IsNullOrEmpty(sample.description)
                ? L10n.Tr("There is no description for this sample.", null)
                : sample.description;

            m_ImageCarousel.SetImages(GetExistingImages(sample.images, parentPackageVersion.localPath));

            foreach (var child in m_CardsContainer.Children())
            {
                if (child is PackageInformationCard packageCard)
                    packageCard.Refresh(parentPackageVersion);
                else if (child is SampleInformationCard sampleCard)
                    sampleCard.Refresh(sample);
            }
        }

        private IReadOnlyList<ImageSource> GetExistingImages(string[] images, string resolvedPath)
        {
            if (images == null || images.Length == 0 || string.IsNullOrEmpty(resolvedPath))
                return Array.Empty<ImageSource>();

            var existingImages = new List<ImageSource>(images.Length);
            foreach (var image in images)
            {
                var imagePath = IOUtils.PathsCombine(resolvedPath, image);
                if (m_IOProxy.FileExists(imagePath))
                    existingImages.Add(ImageSource.FromFilePath(imagePath));
            }
            return existingImages;
        }
    }
}
