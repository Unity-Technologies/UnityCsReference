using System;
using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A <see cref="VisualElement"/> representing a source texture.
    /// </summary>
    public class Image : VisualElement
    {
        /// <summary>
        /// Instantiates an <see cref="Image"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Image, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Image"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            /// <summary>
            /// Returns an empty enumerable, as images generally do not have children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        private ScaleMode m_ScaleMode;
        private Texture m_Image;
        private Sprite m_Sprite;
        private VectorImage m_VectorImage;
        private Rect m_UV;
        private Color m_TintColor;

        private bool m_ImageIsInline;
        private bool m_ScaleModeIsInline;
        private bool m_TintColorIsInline;


        /// <summary>
        /// The texture to display in this image.
        /// </summary>
        public Texture image
        {
            get { return m_Image; }
            set
            {
                if (value != null && (m_Sprite != null || m_VectorImage != null))
                {
                    var unsetProp = m_Sprite != null ? "sprite" : "vector image";
                    Debug.LogWarning($"Image object already has a background, removing {unsetProp}");
                    m_Sprite = null;
                    m_VectorImage = null;
                }
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

        /// <summary>
        /// The sprite to display in this image.
        /// </summary>
        public Sprite sprite
        {
            get { return m_Sprite; }
            set
            {
                if (value != null && (m_Image != null || m_VectorImage != null))
                {
                    var unsetProp = m_Image != null ? "texture" : "vector image";
                    Debug.LogWarning($"Image object already has a background, removing {unsetProp}");
                    m_Image = null;
                    m_VectorImage = null;
                }
                m_ImageIsInline = value != null;
                if (m_Sprite != value)
                {
                    m_Sprite = value;
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }


        /// <summary>
        /// The <see cref="VectorImage"/> to display in this image.
        /// </summary>
        public VectorImage vectorImage
        {
            get { return m_VectorImage; }
            set
            {
                if (value != null && (m_Image != null || m_Sprite != null))
                {
                    var unsetProp = m_Image != null ? "texture" : "sprite";
                    Debug.LogWarning($"Image object already has a background, removing {unsetProp}");
                    m_Image = null;
                    m_Sprite = null;
                }
                m_ImageIsInline = value != null;
                if (m_VectorImage != value)
                {
                    m_VectorImage = value;
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                    if (m_VectorImage == null)
                    {
                        m_UV = new Rect(0, 0, 1, 1);
                    }
                }
            }
        }

        /// <summary>
        /// The source rectangle inside the texture relative to the top left corner.
        /// </summary>
        public Rect sourceRect
        {
            get { return GetSourceRect(); }
            set
            {
                if (sprite != null)
                {
                    Debug.LogError("Cannot set sourceRect on a sprite image");
                    return;
                }
                CalculateUV(value);
            }
        }

        /// <summary>
        /// The base texture coordinates of the Image relative to the bottom left corner.
        /// </summary>
        public Rect uv
        {
            get { return m_UV; }
            set { m_UV = value; }
        }

        /// <summary>
        /// ScaleMode used to display the Image.
        /// </summary>
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

        /// <summary>
        /// Tinting color for this Image.
        /// </summary>
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

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-image";

        /// <summary>
        /// Constructor.
        /// </summary>
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

        private Vector2 GetTextureDisplaySize(Texture texture)
        {
            var result = Vector2.zero;
            if (texture != null)
            {
                result = new Vector2(texture.width, texture.height);
                var t2d = texture as Texture2D;
                if (t2d != null)
                    result = result / t2d.pixelsPerPoint;
            }

            return result;
        }

        private Vector2 GetTextureDisplaySize(Sprite sprite)
        {
            var result = Vector2.zero;
            if (sprite != null)
                result = (Vector2)(sprite.bounds.size * sprite.pixelsPerUnit);
            return result;
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            if (image == null && sprite == null && vectorImage == null)
                return new Vector2(measuredWidth, measuredHeight);

            var sourceSize = Vector2.zero;

            if (image != null)
                sourceSize = GetTextureDisplaySize(image);
            else if (sprite != null)
                sourceSize = GetTextureDisplaySize(sprite);
            else
                sourceSize = vectorImage.size;

            // covers the MeasureMode.Exactly case
            Rect rect = sourceRect;
            bool hasImagePosition = rect != Rect.zero;
            measuredWidth = hasImagePosition ? rect.width : sourceSize.x;
            measuredHeight = hasImagePosition ? rect.height : sourceSize.y;

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
            if (image == null && sprite == null && vectorImage == null)
                return;

            var rectParams = new MeshGenerationContextUtils.RectangleParams();
            if (image != null)
                rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(contentRect, uv, image, scaleMode, panel.contextType);
            else if (sprite != null)
            {
                var slices = Vector4.zero;
                rectParams = MeshGenerationContextUtils.RectangleParams.MakeSprite(contentRect, sprite, scaleMode, panel.contextType, false, ref slices);
            }
            else if (vectorImage != null)
                rectParams = MeshGenerationContextUtils.RectangleParams.MakeVectorTextured(contentRect, uv, vectorImage, scaleMode, panel.contextType);
            rectParams.color = tintColor;
            mgc.Rectangle(rectParams);
        }

        static CustomStyleProperty<Texture2D> s_ImageProperty = new CustomStyleProperty<Texture2D>("--unity-image");
        static CustomStyleProperty<Sprite> s_SpriteProperty = new CustomStyleProperty<Sprite>("--unity-image");
        static CustomStyleProperty<VectorImage> s_VectorImageProperty = new CustomStyleProperty<VectorImage>("--unity-image");
        static CustomStyleProperty<string> s_ScaleModeProperty = new CustomStyleProperty<string>("--unity-image-size");
        static CustomStyleProperty<Color> s_TintColorProperty = new CustomStyleProperty<Color>("--unity-image-tint-color");

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            // We should consider not exposing image as a style at all, since it's intimately tied to uv/sourceRect
            Texture2D textureValue = null;
            Sprite spriteValue = null;
            VectorImage vectorImageValue = null;
            string scaleModeValue;
            Color tintValue = Color.white;
            ICustomStyle customStyle = e.customStyle;
            if (!m_ImageIsInline && customStyle.TryGetValue(s_ImageProperty, out textureValue))
            {
                m_Image = textureValue;
                m_Sprite = null;
                m_VectorImage = null;
            }

            if (!m_ImageIsInline && customStyle.TryGetValue(s_SpriteProperty, out spriteValue))
            {
                m_Image = null;
                m_Sprite = spriteValue;
                m_VectorImage = null;
            }

            if (!m_ImageIsInline && customStyle.TryGetValue(s_VectorImageProperty, out vectorImageValue))
            {
                m_Image = null;
                m_Sprite = null;
                m_VectorImage = vectorImageValue;
            }

            if (!m_ScaleModeIsInline && customStyle.TryGetValue(s_ScaleModeProperty, out scaleModeValue))
            {
                m_ScaleMode = (ScaleMode)StylePropertyUtil.GetEnumIntValue(StyleEnumType.ScaleMode, scaleModeValue);
            }

            if (!m_TintColorIsInline && customStyle.TryGetValue(s_TintColorProperty, out tintValue))
                m_TintColor = tintValue;
        }

        private void CalculateUV(Rect srcRect)
        {
            m_UV = new Rect(0, 0, 1, 1);

            var size = Vector2.zero;

            Texture texture = image;
            if (texture != null)
                size = GetTextureDisplaySize(texture);

            var vi = vectorImage;
            if (vi != null)
                size = vi.size;

            if (size != Vector2.zero)
            {
                // Convert texture coordinates to UV
                m_UV.x = srcRect.x / size.x;
                m_UV.width = srcRect.width / size.x;
                m_UV.height = srcRect.height / size.y;
                m_UV.y = 1.0f - m_UV.height - (srcRect.y / size.y);
            }
        }

        private Rect GetSourceRect()
        {
            Rect rect = Rect.zero;
            var size = Vector2.zero;

            var texture = image;
            if (texture != null)
                size = GetTextureDisplaySize(texture);

            var vi = vectorImage;
            if (vi != null)
                size = vi.size;

            if (size != Vector2.zero)
            {
                // Convert UV to texture coordinates
                rect.x = uv.x * size.x;
                rect.width = uv.width * size.x;
                rect.y = (1.0f - uv.y - uv.height) * size.y;
                rect.height = uv.height * size.y;
            }

            return rect;
        }
    }
}
