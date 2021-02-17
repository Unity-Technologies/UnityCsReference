// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsImages : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetailsImages> {}

        private static Texture2D s_LoadingTexture;

        // Keep track of the width breakpoints at which images were hidden so we know
        //  when to add them back in
        private Stack<float> m_WidthsWhenImagesRemoved;

        private IPackage m_Package;

        private VisualElement m_ImagesContainer;
        private Button m_MoreLink;

        private ApplicationProxy m_Application;
        private AssetStoreCache m_AssetStoreCache;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<ApplicationProxy>();
            m_AssetStoreCache = container.Resolve<AssetStoreCache>();
        }

        public PackageDetailsImages()
        {
            ResolveDependencies();

            m_WidthsWhenImagesRemoved = new Stack<float>();

            var titleLabel = new Label(L10n.Tr("Images & Videos")) { classList = { "containerTitle" } };
            Add(titleLabel);

            m_ImagesContainer = new VisualElement() { name = "detailImages" };
            Add(m_ImagesContainer);
            m_ImagesContainer.RegisterCallback<GeometryChangedEvent>(ImagesGeometryChangeEvent);

            m_MoreLink = new Button() { name = "detailImagesMoreLink", classList = { "link", "font-big", "moreless" } };
            m_MoreLink.text = L10n.Tr("View images & videos on Asset Store");
            Add(m_MoreLink);
        }

        public void OnDisable()
        {
            ClearSupportingImages();
        }

        public void Refresh(IPackage package)
        {
            m_Package = package;

            var visible = m_Package?.images.Any() ?? false;

            UIUtils.SetElementDisplay(this, visible);
            ClearSupportingImages();

            if (!visible)
                return;

            if (s_LoadingTexture == null)
                s_LoadingTexture = (Texture2D)EditorGUIUtility.LoadRequired("Icons/UnityLogo.png");

            if (long.TryParse(m_Package.uniqueId, out long id))
            {
                foreach (var packageImage in m_Package.images)
                {
                    var image = new Label { classList = { "image" } };
                    image.OnLeftClick(() => { m_Application.OpenURL(packageImage.url); });
                    image.style.backgroundImage = s_LoadingTexture;
                    m_ImagesContainer.Add(image);

                    m_AssetStoreCache.DownloadImageAsync(id, packageImage.thumbnailUrl, (retId, texture) =>
                    {
                        if (retId.ToString() == m_Package?.uniqueId)
                        {
                            texture.hideFlags = HideFlags.HideAndDontSave;
                            image.style.backgroundImage = texture;
                        }
                    });
                }
            }

            m_MoreLink.clicked += OnMoreImagesClicked;
        }

        private void ClearSupportingImages()
        {
            foreach (var elt in m_ImagesContainer.Children())
            {
                if (elt is Label &&
                    elt.style.backgroundImage.value.texture != null &&
                    elt.style.backgroundImage.value.texture != s_LoadingTexture)
                {
                    Object.DestroyImmediate(elt.style.backgroundImage.value.texture);
                }
            }
            m_ImagesContainer.Clear();
            m_MoreLink.clicked -= OnMoreImagesClicked;
        }

        private void OnMoreImagesClicked()
        {
            m_Application.OpenURL((m_Package as AssetStorePackage).assetStoreLink);
        }

        private void ImagesGeometryChangeEvent(GeometryChangedEvent evt)
        {
            // hide or show the last image depending on whether it fits on the screen
            var images = m_ImagesContainer.Children();
            var visibleImages = images.Where(elem => UIUtils.IsElementVisible(elem));

            var firstInvisibleImage = images.FirstOrDefault(elem => !UIUtils.IsElementVisible(elem));
            var visibleImagesWidth = visibleImages.Sum(elem => elem.rect.width);
            var lastVisibleImage = visibleImages.LastOrDefault();

            var widthWhenLastImageRemoved = m_WidthsWhenImagesRemoved.Any() ? m_WidthsWhenImagesRemoved.Peek() : float.MaxValue;

            // if the container approximately doubles in height, that indicates the last image was wrapped around to another row
            if (lastVisibleImage != null && (evt.newRect.height >= 2 * lastVisibleImage.rect.height || visibleImagesWidth >= evt.newRect.width))
            {
                UIUtils.SetElementDisplay(lastVisibleImage, false);
                m_WidthsWhenImagesRemoved.Push(evt.newRect.width);
            }
            else if (firstInvisibleImage != null && evt.newRect.width > widthWhenLastImageRemoved)
            {
                UIUtils.SetElementDisplay(firstInvisibleImage, true);
                m_WidthsWhenImagesRemoved.Pop();
            }
        }
    }
}
