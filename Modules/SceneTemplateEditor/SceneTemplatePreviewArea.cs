// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.SceneTemplate
{
    internal class SceneTemplatePreviewArea
    {
        private string m_Name;
        private string m_NoPreviewText;

        public VisualElement Element { get; private set; }

        public SceneTemplatePreviewArea(string name, Texture2D preview, string noPreviewText)
        {
            m_Name = name;
            m_NoPreviewText = noPreviewText;
            MakeElement(preview);
        }

        private void MakeElement(Texture2D preview)
        {
            var previewAreaElement = new VisualElement();
            previewAreaElement.name = m_Name;
            previewAreaElement.AddToClassList(Styles.classPreviewArea);
            Element = previewAreaElement;

            UpdatePreview(preview);
        }

        public void UpdatePreview(Texture2D preview)
        {
            Element.Clear();
            if (preview != null)
            {
                Element.style.backgroundImage = new StyleBackground(preview);
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
    }
}
