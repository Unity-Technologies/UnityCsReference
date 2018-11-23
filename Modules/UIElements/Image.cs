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

        private int m_ScaleMode;
        private Texture m_Image;
        private Rect m_UV;

        private bool m_ImageIsInline;
        private bool m_ScaleModeIsInline;

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

        public ScaleMode scaleMode
        {
            get { return (ScaleMode)m_ScaleMode; }
            set
            {
                m_ScaleModeIsInline = true;
                if ((ScaleMode)m_ScaleMode != value)
                {
                    m_ScaleMode = (int)value;
                    IncrementVersion(VersionChangeType.Layout);
                }
            }
        }

        public static readonly string ussClassName = "unity-image";

        public Image()
        {
            AddToClassList(ussClassName);

            this.scaleMode = ScaleMode.ScaleAndCrop;
            m_UV = new Rect(0, 0, 1, 1);

            requireMeasureFunction = true;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
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

        internal override void DoRepaint(IStylePainter painter)
        {
            Texture current = image;
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

        static CustomStyleProperty<Texture2D> s_ImageProperty = new CustomStyleProperty<Texture2D>("--unity-image");
        static CustomStyleProperty<int> s_ScaleModeProperty = new CustomStyleProperty<int>("--unity-image-size");
        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            // We should consider not exposing image as a style at all, since it's intimately tied to uv/sourceRect
            Texture2D textureValue = null;
            int scaleModeValue = 0;

            ICustomStyle customStyle = e.customStyle;
            if (!m_ImageIsInline && customStyle.TryGetValue(s_ImageProperty, out textureValue))
                m_Image = textureValue;

            if (!m_ScaleModeIsInline && customStyle.TryGetValue(s_ScaleModeProperty, out scaleModeValue))
                scaleMode = (ScaleMode)scaleModeValue;
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

                m_UV.x =  srcRect.x / width;
                m_UV.width = srcRect.width / width;
                m_UV.height =  srcRect.height / height;
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
