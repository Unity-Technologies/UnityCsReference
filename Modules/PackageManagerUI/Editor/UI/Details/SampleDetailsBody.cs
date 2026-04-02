// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SampleDetailsBody: VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new SampleDetailsBody(ServicesContainer.instance.Resolve<IUpmCache>(), ServicesContainer.instance.Resolve<IIOProxy>());
        }

        private readonly VisualElement m_CardsContainer;
        private readonly VisualElement m_ImagesContainer;
        private readonly VisualElement m_ControlsContainer;
        private readonly SelectableLabel m_DescriptionLabel;
        private readonly Label m_CounterLabel;
        private readonly VisualElement m_PrevButtonHotspot;
        private readonly VisualElement m_NextButtonHotspot;

        private VisualElement m_AspectRatioContainer;
        private Image m_ImageElement;

        private Texture2D m_CurrentTexture;
        private string[] m_CurrentImages;
        private string m_CurrentResolvedPath;
        private int m_CurrentImageIndex;

        private readonly IIOProxy m_IOProxy;

        public SampleDetailsBody(IUpmCache upmCache, IIOProxy ioProxy)
        {
            m_IOProxy = ioProxy;

            m_CardsContainer = new VisualElement { name = "detailInformationCardsContainer" };
            m_CardsContainer.Add(new SampleParentPackageDisplayNameCard());
            m_CardsContainer.Add(new SourceInfoCard(upmCache));
            m_CardsContainer.Add(new SignatureInfoCard());
            m_CardsContainer.Add(new SampleSizeInfoCard());

            m_ImagesContainer = new VisualElement { name = "imagesContainer" };
            m_ImagesContainer.style.alignItems = Align.FlexStart;
            m_ImagesContainer.style.justifyContent = Justify.Center;

            m_ControlsContainer = new VisualElement { name = "controlsContainer" };
            var prevButton = new Button(OnPreviousImage) { name = "imageControlsPreviousButton", text = "<" };
            var nextButton = new Button(OnNextImage) { name = "imageControlsNextButton", text = ">" };
            prevButton.pickingMode = PickingMode.Ignore;
            nextButton.pickingMode = PickingMode.Ignore;
            m_CounterLabel = new Label() { name = "imagesCounterLabel" };

            m_PrevButtonHotspot = CreateButtonHotspot("prevButtonHotspot", prevButton, OnPreviousImage);
            m_NextButtonHotspot = CreateButtonHotspot("nextButtonHotspot", nextButton, OnNextImage);

            m_ControlsContainer.Add(m_CounterLabel);
            m_DescriptionLabel = new SelectableLabel { name = "descriptionLabel", };

            CreateImageStructure();
            Add(m_CardsContainer);
            Add(m_ImagesContainer);
            Add(m_DescriptionLabel);

            RegisterCallback<DetachFromPanelEvent>(evt => CleanupTexture());
        }

        private void CreateImageStructure()
        {
            m_AspectRatioContainer = new VisualElement { name = "aspectRatioContainer" };
            m_ImageElement = new Image
            {
                name = "sampleImage",
                scaleMode = ScaleMode.ScaleToFit
            };

            m_ImageElement.style.width = Length.Percent(100);
            m_ImageElement.style.height = Length.Percent(100);

            m_AspectRatioContainer.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var currentWidth = evt.newRect.width;
                if (currentWidth <= 0.001f) return;
                var targetRatio = 1920f / 1080f;
                var targetHeight = currentWidth / targetRatio;
                var maxAllowedHeight = 300f;
                if (targetHeight > maxAllowedHeight)
                {
                    targetHeight = maxAllowedHeight;
                    m_AspectRatioContainer.style.maxWidth = targetHeight * targetRatio;
                }

                m_AspectRatioContainer.style.height = targetHeight;
            });

            m_AspectRatioContainer.Add(m_ImageElement);
            m_ImageElement.Add(m_ControlsContainer);
            m_ImageElement.Add(m_PrevButtonHotspot);
            m_ImageElement.Add(m_NextButtonHotspot);
        }

        private VisualElement CreateButtonHotspot(string name, Button button, Action callback)
        {
            var hotspot = new VisualElement { name = name };

            hotspot.Add(button);
            hotspot.RegisterCallback<ClickEvent>(evt =>
            {
                callback?.Invoke();
                evt.StopPropagation();
            });

            return hotspot;
        }

        public void Refresh(Sample sample)
        {
            if (sample.isDefault)
                return;

            m_DescriptionLabel.text = string.IsNullOrEmpty(sample.description)
                ? L10n.Tr("There is no description for this sample.")
                : sample.description;

            CleanupTexture();

            m_CurrentResolvedPath = sample.package.versions.primary.localPath;
            m_CurrentImages = FilterValidImages(sample.images, m_CurrentResolvedPath);
            m_CurrentImageIndex = 0;

            var showControls = false;
            var hasImages = false;
            if (!sample.isDefault && m_CurrentImages != null && m_CurrentImages.Length > 0)
            {
                UpdateImage();
                hasImages = true;
                showControls = m_CurrentImages.Length > 1;
            }

            if (hasImages && m_ImagesContainer.childCount == 0)
                m_ImagesContainer.Add(m_AspectRatioContainer);
            else if (!hasImages && m_ImagesContainer.childCount > 0)
                m_ImagesContainer.Clear();

            UIUtils.SetElementDisplay(m_ControlsContainer, showControls);
            UIUtils.SetElementDisplay(m_PrevButtonHotspot, showControls);
            UIUtils.SetElementDisplay(m_NextButtonHotspot, showControls);

            var parentPackageVersion = sample.package?.versions.primary;
            var showCards = !sample.isDefault && parentPackageVersion != null;
            UIUtils.SetElementDisplay(m_CardsContainer, showCards);
            if (!showCards)
                return;

            foreach (var child in m_CardsContainer.Children())
            {
                if (child is PackageInformationCard packageCard)
                    packageCard.Refresh(parentPackageVersion);
                else if (child is SampleInformationCard sampleCard)
                    sampleCard.Refresh(sample);
            }
        }

        private string[] FilterValidImages(string[] images, string resolvedPath)
        {
            if (images == null || images.Length == 0)
                return Array.Empty<string>();

            var validImages = new List<string>();
            foreach (var image in images)
            {
                var imagePath = IOUtils.PathsCombine(resolvedPath, image);
                if (!m_IOProxy.FileExists(imagePath))
                    continue;

                try
                {
                    var bytes = m_IOProxy.FileReadAllBytes(imagePath);
                    if (m_IOProxy.TryLoadTexture(bytes, out var tempTex))
                        validImages.Add(image);

                    if (tempTex != null)
                        UnityEngine.Object.DestroyImmediate(tempTex);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return validImages.ToArray();
        }

        private void OnPreviousImage()
        {
            if (m_CurrentImages == null || m_CurrentImages.Length <= 1)
                return;
            m_CurrentImageIndex--;
            if (m_CurrentImageIndex < 0)
                m_CurrentImageIndex = m_CurrentImages.Length - 1;
            UpdateImage();
        }

        private void OnNextImage()
        {
            if (m_CurrentImages == null || m_CurrentImages.Length <= 1)
                return;
            m_CurrentImageIndex++;
            if (m_CurrentImageIndex >= m_CurrentImages.Length)
                m_CurrentImageIndex = 0;
            UpdateImage();
        }

        private void UpdateImage()
        {
            CleanupTexture();
            m_CounterLabel.text = $"{m_CurrentImageIndex + 1} / {m_CurrentImages.Length}";

            var imagePath = IOUtils.PathsCombine(m_CurrentResolvedPath, m_CurrentImages[m_CurrentImageIndex]);
            if (m_IOProxy.FileExists(imagePath))
            {
                var fileData = m_IOProxy.FileReadAllBytes(imagePath);
                if (m_IOProxy.TryLoadTexture(fileData, out m_CurrentTexture))
                    m_ImageElement.image = m_CurrentTexture;
            }
        }

        private void CleanupTexture()
        {
            if (m_CurrentTexture == null)
                return;

            UnityEngine.Object.DestroyImmediate(m_CurrentTexture);
            m_CurrentTexture = null;
        }
    }
}
