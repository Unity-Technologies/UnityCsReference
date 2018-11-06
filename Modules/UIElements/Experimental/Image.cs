// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
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

        private StyleValue<int> m_ScaleMode;
        private StyleValue<Texture> m_Image;
        private Rect m_UV;

        public StyleValue<Texture> image
        {
            get { return m_Image; }
            set
            {
                if (StyleValueUtils.ApplyAndCompareObject(ref m_Image, value))
                {
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                    if (m_Image.value == null)
                    {
                        m_UV = new Rect(0, 0, 1 , 1);
                    }
                }
            }
        }

        public Rect sourceRect
        {
            get { return GetSourceRect();  }
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

        public StyleValue<ScaleMode> scaleMode
        {
            get { return new StyleValue<ScaleMode>((ScaleMode)m_ScaleMode.value, m_ScaleMode.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref m_ScaleMode,
                    new StyleValue<int>((int)value.value, value.specificity)))
                {
                    IncrementVersion(VersionChangeType.Layout);
                }
            }
        }

        public Image()
        {
            this.scaleMode = ScaleMode.ScaleAndCrop;
            m_UV = new Rect(0, 0, 1, 1);

            requireMeasureFunction = true;
        }

        protected internal override Vector2 DoMeasure(float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            Texture current = image.GetSpecifiedValueOrDefault(null);
            if (current == null)
                return new Vector2(measuredWidth, measuredHeight);

            // covers the MeasureMode.Exactly case
            Rect rect = sourceRect;
            bool hasImagePosition = rect != Rect.zero;
            measuredWidth = hasImagePosition ? rect.width : current.width;
            measuredHeight = hasImagePosition ? rect.height : current.height;

            if (widthMode == MeasureMode.AtMost)
            {
                measuredWidth = Mathf.Min(measuredWidth, width);
            }

            if (heightMode == MeasureMode.AtMost)
            {
                measuredHeight = Mathf.Min(measuredHeight, height);
            }

            return new Vector2(measuredWidth, measuredHeight);
        }

        protected override void DoRepaint(IStylePainter painter)
        {
            Texture current = image.GetSpecifiedValueOrDefault(null);
            if (current == null)
            {
                return;
            }

            var painterParams = new TextureStylePainterParameters
            {
                rect = contentRect,
                uv = uv,
                texture = current,
                color = GUI.color,
                scaleMode = scaleMode
            };
            var stylePainter = (IStylePainterInternal)painter;
            stylePainter.DrawTexture(painterParams);
        }

        protected override void OnStyleResolved(ICustomStyle elementStyle)
        {
            base.OnStyleResolved(elementStyle);

            // We should consider not exposing image as a style at all, since it's intimately tied to uv/sourceRect
            StyleValue<Texture2D> targetAsDefinition = new StyleValue<Texture2D>(m_Image.value as Texture2D, m_Image.specificity);
            elementStyle.ApplyCustomProperty("image", ref targetAsDefinition);
            m_Image = new StyleValue<Texture>(targetAsDefinition.value, targetAsDefinition.specificity);

            elementStyle.ApplyCustomProperty("image-size", ref m_ScaleMode);
        }

        private void CalculateUV(Rect srcRect)
        {
            m_UV = new Rect(0, 0, 1, 1);
            Texture texture = image.GetSpecifiedValueOrDefault(null);
            if (texture != null)
            {
                // Convert texture coordinates to UV
                int width = texture.width;
                int height = texture.height;

                m_UV.x =  srcRect.x / width;
                m_UV.width = srcRect.width / width;
                m_UV.height =  srcRect.height / height;
                m_UV.y = 1.0f - m_UV.height - (srcRect.y / height);
            }
        }

        private Rect GetSourceRect()
        {
            Rect rect = Rect.zero;
            Texture texture = image.GetSpecifiedValueOrDefault(null);
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
