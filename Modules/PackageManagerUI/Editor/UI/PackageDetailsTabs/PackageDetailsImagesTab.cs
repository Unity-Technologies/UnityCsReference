// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsImagesTab : PackageDetailsTabElement
    {
        public const string k_Id = "images";

        private static Texture2D s_LoadingTexture;

        private IPackageVersion m_Version;

        private VisualElement m_MainImageInnerContainer;
        private Image m_MainImage;

        private VisualElement m_ThumbnailsContainer;

        private AssetStoreCache m_AssetStoreCache;

        private List<Texture2D> m_ImageTextures;

        public PackageDetailsImagesTab(AssetStoreCache assetStoreCache)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Images");
            m_AssetStoreCache = assetStoreCache;

            m_ImageTextures = new List<Texture2D>();

            m_MainImage = new Image() { name = "mainImage", classList = { "image" } };
            m_MainImage.scaleMode = ScaleMode.ScaleToFit;

            // Wrap main image in two layers of containers to lock the aspect ratio
            var mainImageOuterContainer = new VisualElement() { name = "mainImageOuterContainer" };
            m_MainImageInnerContainer = new VisualElement() { name = "mainImageInnerContainer" };
            m_MainImageInnerContainer.Add(m_MainImage);
            mainImageOuterContainer.Add(m_MainImageInnerContainer);
            Add(mainImageOuterContainer);

            m_ThumbnailsContainer = new VisualElement() { name = "thumbnailsContainer" };
            Add(m_ThumbnailsContainer);

            m_Version = null;
        }

        public override bool IsValid(IPackageVersion version)
        {
            return version?.package?.product?.images?.Any() ?? false;
        }

        public override void Refresh(IPackageVersion version)
        {
            m_Version = version;
            var package = version.package;

            ClearImages();

            if (s_LoadingTexture == null)
                s_LoadingTexture = (Texture2D)EditorGUIUtility.LoadRequired("Icons/UnityLogo.png");

            var product = package.product;
            if (product == null)
                return;

            m_MainImage.image = s_LoadingTexture;
            foreach (var packageImage in package.product.images)
            {
                var thumbnail = new Image() { classList = { "thumbnail", "image" } };
                thumbnail.image = s_LoadingTexture;
                thumbnail.scaleMode = ScaleMode.ScaleAndCrop;
                m_ThumbnailsContainer.Add(thumbnail);
                m_AssetStoreCache.DownloadImageAsync(product.id, packageImage.thumbnailUrl, (retId, texture) =>
                {
                    if (texture != null)
                    {
                        m_ImageTextures.Add(texture);
                        texture.hideFlags = HideFlags.HideAndDontSave;
                    }

                    if (retId.ToString() == m_Version.package?.uniqueId)
                    {
                        thumbnail.image = texture ?? s_LoadingTexture;
                        thumbnail.OnLeftClick(() => OnImageClicked(thumbnail, packageImage));
                    }
                });

                // We run `OnImageClicked` at the beginning to make sure the selection is set properly
                if (packageImage.type == PackageImage.ImageType.Main)
                    OnImageClicked(thumbnail, packageImage);
            }
        }

        private void ClearImages()
        {
            m_MainImage.image = null;
            foreach (var thumbnail in m_ThumbnailsContainer.Children().OfType<Image>())
                thumbnail.image = null;
            m_ThumbnailsContainer.Clear();
            foreach (var texture in m_ImageTextures)
                Object.DestroyImmediate(texture);
            m_ImageTextures.Clear();
        }

        private void OnImageClicked(Image image, PackageImage changeToImage)
        {
            var product = m_Version.package.product;
            if (product == null)
                return;

            var url = changeToImage.type == PackageImage.ImageType.Main ? changeToImage.thumbnailUrl : changeToImage.url;
            m_AssetStoreCache.DownloadImageAsync(product.id, url, (retId, texture2d) =>
            {
                if (texture2d != null)
                {
                    m_ImageTextures.Add(texture2d);
                    texture2d.hideFlags = HideFlags.HideAndDontSave;
                }

                // If for whatever reason the main image is not available, we fall back to use thumbnail so that the user don't just see
                // the loading texture indefinitely.
                var texture = texture2d ?? image.image;
                if (retId.ToString() == m_Version.package?.uniqueId)
                {
                    m_MainImage.image = texture;
                    if (changeToImage.type == PackageImage.ImageType.Main && texture.width > 0)
                        m_MainImageInnerContainer.style.paddingTop = new Length(100.0f * texture.height / texture.width, LengthUnit.Percent);
                }
            });
            foreach (var thumbnail in m_ThumbnailsContainer.Children().OfType<Image>())
                thumbnail.RemoveFromClassList("selected");
            image.AddToClassList("selected");
        }
    }
}
