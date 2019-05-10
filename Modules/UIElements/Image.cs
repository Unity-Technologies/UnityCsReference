// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public class Image : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Image, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        private ScaleMode m_ScaleMode;
        private Texture m_Image;
        private Rect m_UV;
        private Color m_TintColor;

        private bool m_ImageIsInline;
        private bool m_ScaleModeIsInline;
        private bool m_TintColorIsInline;


        public Texture image
        {
            get { return m_Image; }
            set
            {
                m_ImageIsInline = value != null;
                if (m_Image != value)
                {
                    m_Image = value;
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                    if (m_Image == null)
                    {
                        m_UV = new Rect(0, 0, 1, 1);
                    }
                }
            }
        }

        public Rect sourceRect
        {
            get { return GetSourceRect(); }
            set
            {
                CalculateUV(value);
            }
        }

        public Rect uv
        {
            get { return m_UV; }
            set { m_UV = value; }
        }

        public ScaleMode scaleMode
        {
            get { return m_ScaleMode; }
            set
            {
                m_ScaleModeIsInline = true;
                if (m_ScaleMode != value)
                {
                    m_ScaleMode = value;
                    IncrementVersion(VersionChangeType.Layout);
                }
            }
        }

        public Color tintColor
        {
            get
            {
                return m_TintColor;
            }
            set
            {
                m_TintColorIsInline = true;
                if (m_TintColor != value)
                {
                    m_TintColor = value;
                    IncrementVersion(VersionChangeType.Repaint);
                }
            }
        }

        public static readonly string ussClassName = "unity-image";

        public Image()
        {
            AddToClassList(ussClassName);

            m_ScaleMode = ScaleMode.ScaleAndCrop;
            m_TintColor = Color.white;

            m_UV = new Rect(0, 0, 1, 1);

            requireMeasureFunction = true;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            generateVisualContent += OnGenerateVisualContent;
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            Texture current = image;
            if (current == null)
                return new Vector2(measuredWidth, measuredHeight);

            // covers the MeasureMode.Exactly case
            Rect rect = sourceRect;
            bool hasImagePosition = rect != Rect.zero;
            measuredWidth = hasImagePosition ? rect.width : current.width;
            measuredHeight = hasImagePosition ? rect.height : current.height;

            if (widthMode == MeasureMode.AtMost)
            {
                measuredWidth = Mathf.Min(measuredWidth, desiredWidth);
            }

            if (heightMode == MeasureMode.AtMost)
            {
                measuredHeight = Mathf.Min(measuredHeight, desiredHeight);
            }

            return new Vector2(measuredWidth, measuredHeight);
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Texture current = image;
            if (current == null)
                return;

            var rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(contentRect, uv, current, scaleMode);
            rectParams.color = tintColor;
            mgc.Rectangle(rectParams);
        }

        static CustomStyleProperty<Texture2D> s_ImageProperty = new CustomStyleProperty<Texture2D>("--unity-image");
        static CustomStyleProperty<string> s_ScaleModeProperty = new CustomStyleProperty<string>("--unity-image-size");
        static CustomStyleProperty<Color> s_TintColorProperty = new CustomStyleProperty<Color>("--unity-image-tint-color");

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            // We should consider not exposing image as a style at all, since it's intimately tied to uv/sourceRect
            Texture2D textureValue = null;
            string scaleModeValue;
            Color tintValue = Color.white;
            ICustomStyle customStyle = e.customStyle;
            if (!m_ImageIsInline && customStyle.TryGetValue(s_ImageProperty, out textureValue))
                m_Image = textureValue;

            if (!m_ScaleModeIsInline && customStyle.TryGetValue(s_ScaleModeProperty, out scaleModeValue))
            {
                int scaleModeIntValue;

                if (StyleSheetCache.TryParseEnum<ScaleMode>(scaleModeValue, out scaleModeIntValue))
                {
                    m_ScaleMode = (ScaleMode)scaleModeIntValue;
                }
            }

            if (!m_TintColorIsInline && customStyle.TryGetValue(s_TintColorProperty, out tintValue))
                m_TintColor = tintValue;
        }

        private void CalculateUV(Rect srcRect)
        {
            m_UV = new Rect(0, 0, 1, 1);
            Texture texture = image;
            if (texture != null)
            {
                // Convert texture coordinates to UV
                int width = texture.width;
                int height = texture.height;

                m_UV.x = srcRect.x / width;
                m_UV.width = srcRect.width / width;
                m_UV.height = srcRect.height / height;
                m_UV.y = 1.0f - m_UV.height - (srcRect.y / height);
            }
        }

        private Rect GetSourceRect()
        {
            Rect rect = Rect.zero;
            Texture texture = image;
            if (texture != null)
            {
                // Convert UV to texture coordinates
                int width = texture.width;
                int height = texture.height;

                rect.x = uv.x * width;
                rect.width = uv.width * width;
                rect.y = (1.0f - uv.y - uv.height) * height;
                rect.height = uv.height * height;
            }

            return rect;
        }
    }
}
