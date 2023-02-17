// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.SceneTemplate
{
    class SceneTemplatePreviewArea
    {
        string m_Name;
        string m_NoPreviewText;
        Texture2D m_PreviewTexture;
        Texture2D m_BadgeTexture;

        public VisualElement Element { get; private set; }

        public SceneTemplatePreviewArea(string name, Texture2D preview, Texture2D badge, string noPreviewText)
        {
            m_Name = name;
            m_NoPreviewText = noPreviewText;
            MakeElement(preview, badge);
        }

        void MakeElement(Texture2D preview, Texture2D badge)
        {
            var previewAreaElement = new VisualElement();
            previewAreaElement.name = m_Name;
            previewAreaElement.AddToClassList(Styles.classPreviewArea);
            Element = previewAreaElement;

            UpdatePreview(preview, badge);
        }

        public void UpdatePreview(Texture2D preview, Texture2D badge)
        {
            Element.Clear();
            m_PreviewTexture = preview;
            m_BadgeTexture = badge;
            if (preview != null)
            {
                Element.style.backgroundImage = new StyleBackground(preview);

                if (badge != null)
                {
                    var badgeElement = new VisualElement();
                    badgeElement.style.backgroundImage = new StyleBackground(badge);
                    badgeElement.AddToClassList(Styles.classPreviewAreaBadge);
                    Element.Add(badgeElement);
                }
            }
            else
            {
                Element.style.backgroundImage = null;
                var noThumbnailLabel = new Label(m_NoPreviewText);
                noThumbnailLabel.AddToClassList("preview-area-no-img-label");
                noThumbnailLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                Element.Add(noThumbnailLabel);
            }
        }

        public void UpdatePreviewAreaSize()
        {
            // Because I can't figure out how to get the resolved style values coming from uss on the first pass.
            const float badgeMaxHeight = 64;

            if (m_PreviewTexture == null)
            {
                Element.style.height = Length.Percent(50);
                return;
            }

            float newHeight;
            if (m_PreviewTexture.height > m_PreviewTexture.width)
            {
                newHeight = Element.parent.worldBound.height * 0.5f;
                Element.style.height = newHeight;
            }
            else
            {
                var aspectRatio = (float)m_PreviewTexture.height / m_PreviewTexture.width;
                var width = Element.worldBound.width;
                newHeight = width * aspectRatio;
                // Debug.Log($"Preview size: {info.name} width: {info.thumbnail.width} height:{info.thumbnail.height} ratio: {aspectRatio} AreaW: {width} NewHeigth: {newHeight}");
                Element.style.height = newHeight;
            }

            var badgeElement = Element.Q(null, Styles.classPreviewAreaBadge);
            if (m_BadgeTexture == null || badgeElement == null)
                return;

            var badgeAspectRatio = m_BadgeTexture.width / m_BadgeTexture.height;
            var newBadgeHeight = Mathf.Min(newHeight * 0.25f, badgeMaxHeight);
            var newBadgeWidth = newBadgeHeight * badgeAspectRatio;
            badgeElement.style.height = newBadgeHeight;
            badgeElement.style.width = newBadgeWidth;
        }
    }
}
