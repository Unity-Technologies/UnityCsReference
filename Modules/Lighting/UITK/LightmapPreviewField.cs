// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Lighting not yet converted
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting
{
    class LightmapPreviewField : VisualElement
    {
        readonly Image m_Image;

        const string k_OpenPreviewText = "Open Preview";
        const string k_ClassName = "lighting-search-lightmap-field";
        readonly string k_ThumbnailClassName = k_ClassName.WithUssElement("thumbnail");
        readonly string k_PreviewButtonClassName = k_ClassName.WithUssElement("preview-button");

        const string k_LightingSearchUSSPath = "StyleSheets/LightingSearch.uss";

        public LightmapPreviewField()
        {
            VisualElement internalContainer = new VisualElement();

            var uss = EditorResources.Load<Object>(k_LightingSearchUSSPath) as StyleSheet;
            internalContainer.styleSheets.Add(uss);
            internalContainer.AddToClassList(k_ClassName);

            m_Image = new Image();
            m_Image.AddToClassList(k_ThumbnailClassName);
            internalContainer.Add(m_Image);

            var button = new Button(() =>
            {
                #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
                LightmapPreviewWindow.CreateLightmapPreviewWindowIndexedWithExposure(lightmapIndex, false, false, exposure);
                #pragma warning restore UAL0015
            })
            { text = k_OpenPreviewText };
            button.AddToClassList(k_PreviewButtonClassName);
            internalContainer.Add(button);

            Add(internalContainer);
        }

        internal int lightmapIndex { get; set; }
        internal float exposure { get; set; }

        internal Texture2D lightmapTexture
        {
            get => m_Image.image as Texture2D;
            set => m_Image.image = value;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
