// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ImageCarousel : VisualElement
    {
        private const string k_ClassName = "image-carousel";
        internal const int k_MaxLoadedImages = 5;

        // Twice the largest size of the image carousel, so images stay sharp on 200% displays.
        // Keep in sync with max-width and max-height of #carouselAspectRatioContainer in ImageCarousel.uss.
        internal const int k_MaxImageWidth = 1067;
        internal const int k_MaxImageHeight = 600;

        private class LoadedImage
        {
            public int index;
            public Texture2D texture;
            public bool isLoading;
        }

        private readonly VisualElement m_AspectRatioContainer;
        private readonly Image m_ImageElement;
        private readonly LoadingSpinner m_LoadingSpinner;
        private readonly Label m_LoadFailedMessage;
        private readonly VisualElement m_ControlsContainer;
        private readonly Label m_CounterLabel;
        private readonly VisualElement m_PreviousButtonHotspot;
        private readonly VisualElement m_NextButtonHotspot;

        private readonly IImageLoader m_ImageLoader;

        private IReadOnlyList<ImageSource> m_Images = Array.Empty<ImageSource>();
        private int m_CurrentImageIndex;
        // Least recently viewed first. A load that finishes after its entry was evicted destroys its texture.
        // We use a plain list because it never holds more than k_MaxLoadedImages entries, so a linked list and dictionary would add complexity without a noticeable speedup.
        private readonly List<LoadedImage> m_LoadedImages = new();

        public int imageCount => m_Images.Count;
        public int currentImageIndex => m_CurrentImageIndex;

        public ImageCarousel(IImageLoader imageLoader)
        {
            m_ImageLoader = imageLoader;

            AddToClassList(k_ClassName);
            tabIndex = 0;

            m_AspectRatioContainer = new VisualElement { name = "carouselAspectRatioContainer" };
            m_ImageElement = new Image
            {
                name = "carouselImage",
                scaleMode = ScaleMode.ScaleToFit
            };
            m_ImageElement.style.width = Length.Percent(100);
            m_ImageElement.style.height = Length.Percent(100);

            m_LoadingSpinner = new LoadingSpinner { name = "carouselLoadingSpinner" };
            m_LoadFailedMessage = new Label(L10n.Tr("Unable to load image", null))
            {
                name = "carouselLoadFailedMessage",
                pickingMode = PickingMode.Ignore
            };
            UIUtils.SetElementDisplay(m_LoadFailedMessage, false);

            m_ControlsContainer = new VisualElement { name = "carouselControlsContainer", pickingMode = PickingMode.Ignore };
            m_CounterLabel = new Label { name = "carouselCounterLabel" };
            m_ControlsContainer.Add(m_CounterLabel);

            var previousButton = new Button(ShowPreviousImage)
            {
                name = "carouselPreviousButton",
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            var nextButton = new Button(ShowNextImage)
            {
                name = "carouselNextButton",
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            m_PreviousButtonHotspot = CreateButtonHotspot("carouselPreviousHotspot", L10n.Tr("Previous image", null), previousButton, ShowPreviousImage);
            m_NextButtonHotspot = CreateButtonHotspot("carouselNextHotspot", L10n.Tr("Next image", null), nextButton, ShowNextImage);

            m_ImageElement.Add(m_LoadingSpinner);
            m_ImageElement.Add(m_LoadFailedMessage);
            m_ImageElement.Add(m_ControlsContainer);
            m_ImageElement.Add(m_PreviousButtonHotspot);
            m_ImageElement.Add(m_NextButtonHotspot);
            m_AspectRatioContainer.Add(m_ImageElement);
            Add(m_AspectRatioContainer);

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

            SetImages(null);
        }

        private static VisualElement CreateButtonHotspot(string name, string tooltip, Button button, Action callback)
        {
            var hotspot = new VisualElement { name = name, tooltip = tooltip };
            hotspot.Add(button);
            hotspot.AddManipulator(new Clickable(callback));
            // Anchor the tooltip to the button, otherwise it appears below the full-height hotspot
            hotspot.RegisterCallback<TooltipEvent>(evt => evt.rect = button.worldBound, TrickleDown.TrickleDown);
            return hotspot;
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (evt.direction == NavigationMoveEvent.Direction.Left)
                ShowPreviousImage();
            else if (evt.direction == NavigationMoveEvent.Direction.Right)
                ShowNextImage();
            else
                return;
            evt.StopPropagation();
        }

        // UI Toolkit treats a click on a child that cannot take focus as a click on the background and clears focus.
        // Taking focus here keeps the arrow keys working after the user clicks the carousel.
        // With a single image there is nothing to navigate, so the carousel is not focusable and the click is left alone.
        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!focusable)
                return;

            focusController?.IgnoreEvent(evt);
            Focus();
        }

        // We drop the images along with their textures because the views that host the carousel always call SetImages after attaching it.
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            SetImages(null);
        }

        public void SetImages(IReadOnlyList<ImageSource> images)
        {
            m_Images = images ?? Array.Empty<ImageSource>();
            m_CurrentImageIndex = 0;

            var hasImages = m_Images.Count > 0;
            UIUtils.SetElementDisplay(this, hasImages);

            var showControls = m_Images.Count > 1;
            focusable = showControls;
            UIUtils.SetElementDisplay(m_ControlsContainer, showControls);
            UIUtils.SetElementDisplay(m_PreviousButtonHotspot, showControls);
            UIUtils.SetElementDisplay(m_NextButtonHotspot, showControls);

            ClearLoadedImages();
            if (hasImages)
                RefreshImageDisplay();
        }

        public void ShowPreviousImage()
        {
            if (m_Images.Count <= 1)
                return;
            m_CurrentImageIndex = (m_CurrentImageIndex - 1 + m_Images.Count) % m_Images.Count;
            RefreshImageDisplay();
        }

        public void ShowNextImage()
        {
            if (m_Images.Count <= 1)
                return;
            m_CurrentImageIndex = (m_CurrentImageIndex + 1) % m_Images.Count;
            RefreshImageDisplay();
        }

        private void RefreshImageDisplay()
        {
            m_CounterLabel.text = $"{m_CurrentImageIndex + 1} / {m_Images.Count}";

            var entry = m_LoadedImages.Find(e => e.index == m_CurrentImageIndex);
            if (entry != null)
            {
                // We move this image to the last position so that it counts as the most recently viewed and is evicted last
                m_LoadedImages.Remove(entry);
                m_LoadedImages.Add(entry);
            }
            else
            {
                entry = new LoadedImage { index = m_CurrentImageIndex, isLoading = true };
                m_LoadedImages.Add(entry);
                if (m_LoadedImages.Count > k_MaxLoadedImages)
                {
                    DestroyTexture(m_LoadedImages[0]);
                    m_LoadedImages.RemoveAt(0);
                }
                LoadImage(entry);
            }

            DisplayEntry(entry);
        }

        private void LoadImage(LoadedImage entry)
        {
            try
            {
                m_ImageLoader.LoadImageAsync(m_Images[entry.index], k_MaxImageWidth, k_MaxImageHeight, texture => OnImageLoaded(entry, texture));
            }
            catch (Exception e)
            {
                // We treat a load that cannot start as a failed load so that the entry does not stay loading forever
                Debug.LogError($"[Package Manager Window] Unable to load image: {e}");
                OnImageLoaded(entry, null);
            }
        }

        private void OnImageLoaded(LoadedImage entry, Texture2D texture)
        {
            if (!m_LoadedImages.Contains(entry))
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                return;
            }

            entry.isLoading = false;
            entry.texture = texture;
            var isCurrent = entry.index == m_CurrentImageIndex;

            // We drop failed loads so that they are retried when the image is viewed again
            if (texture == null)
                m_LoadedImages.Remove(entry);

            if (isCurrent)
                DisplayEntry(entry);
        }

        private void DisplayEntry(LoadedImage entry)
        {
            if (entry.isLoading)
                m_LoadingSpinner.Start();
            else
                m_LoadingSpinner.Stop();

            UIUtils.SetElementDisplay(m_LoadFailedMessage, !entry.isLoading && entry.texture == null);
            m_ImageElement.image = entry.texture;
        }

        private void ClearLoadedImages()
        {
            foreach (var entry in m_LoadedImages)
                DestroyTexture(entry);
            m_LoadedImages.Clear();

            m_LoadingSpinner.Stop();
            UIUtils.SetElementDisplay(m_LoadFailedMessage, false);
            m_ImageElement.image = null;
        }

        private static void DestroyTexture(LoadedImage entry)
        {
            if (entry.texture == null)
                return;

            UnityEngine.Object.DestroyImmediate(entry.texture);
            entry.texture = null;
        }
    }
}
