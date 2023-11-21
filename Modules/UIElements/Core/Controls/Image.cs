// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A <see cref="VisualElement"/> representing a source texture.
    ///
    /// **Note**: This is not related to the `UnityEngine.UI.Image` uGUI control. This is the Image control for the UI Toolkit framework.
    /// </summary>
    public class Image : VisualElement
    {
        internal static readonly BindingId imageProperty = nameof(image);
        internal static readonly BindingId spriteProperty = nameof(sprite);
        internal static readonly BindingId vectorImageProperty = nameof(vectorImage);
        internal static readonly BindingId sourceRectProperty = nameof(sourceRect);
        internal static readonly BindingId uvProperty = nameof(uv);
        internal static readonly BindingId scaleModeProperty = nameof(scaleMode);
        internal static readonly BindingId tintColorProperty = nameof(tintColor);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new Image();
        }

        /// <summary>
        /// Instantiates an <see cref="Image"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<Image, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Image"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
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

        // Internal for tests
        internal bool m_ImageIsInline;
        private bool m_ScaleModeIsInline;
        private bool m_TintColorIsInline;

        /// <summary>
        /// The texture to display in this image. If you assign a `Texture` or `Texture2D`, the Image element will resize and show the assigned texture.
        /// </summary>
        [CreateProperty]
        public Texture image
        {
            get => m_Image;
            set
            {
                if (m_Image == value && m_ImageIsInline)
                    return;

                m_ImageIsInline = value != null;
                SetProperty(value, ref m_Image, ref m_Sprite, ref m_VectorImage, imageProperty);
            }
        }

        /// <summary>
        /// The sprite to display in this image.
        /// </summary>
        [CreateProperty]
        public Sprite sprite
        {
            get => m_Sprite;
            set
            {
                if (m_Sprite == value && m_ImageIsInline)
                    return;

                m_ImageIsInline = value != null;
                SetProperty(value, ref m_Sprite, ref m_Image, ref m_VectorImage, spriteProperty);
            }
        }

        /// <summary>
        /// The <see cref="VectorImage"/> to display in this image.
        /// </summary>
        [CreateProperty]
        public VectorImage vectorImage
        {
            get => m_VectorImage;
            set
            {
                if (m_VectorImage == value && m_ImageIsInline)
                    return;

                m_ImageIsInline = value != null;
                SetProperty(value, ref m_VectorImage, ref m_Image, ref m_Sprite, vectorImageProperty);
            }
        }

        /// <summary>
        /// The source rectangle inside the texture relative to the top left corner.
        /// </summary>
        [CreateProperty]
        public Rect sourceRect
        {
            get => GetSourceRect();
            set
            {
                if (GetSourceRect() == value)
                    return;

                if (sprite != null)
                {
                    Debug.LogError("Cannot set sourceRect on a sprite image");
                    return;
                }
                CalculateUV(value);
                NotifyPropertyChanged(sourceRectProperty);
            }
        }

        /// <summary>
        /// The base texture coordinates of the Image relative to the bottom left corner.
        /// </summary>
        [CreateProperty]
        public Rect uv
        {
            get => m_UV;
            set
            {
                if (m_UV == value)
                    return;
                m_UV = value;
                NotifyPropertyChanged(uvProperty);
            }
        }

        /// <summary>
        /// ScaleMode used to display the Image.
        /// </summary>
        [CreateProperty]
        public ScaleMode scaleMode
        {
            get => m_ScaleMode;
            set
            {
                if (m_ScaleMode == value && m_ScaleModeIsInline)
                    return;
                m_ScaleModeIsInline = true;
                SetScaleMode(value);
            }
        }

        /// <summary>
        /// Tinting color for this Image.
        /// </summary>
        [CreateProperty]
        public Color tintColor
        {
            get => m_TintColor;
            set
            {
                if (m_TintColor == value && m_TintColorIsInline)
                    return;
                m_TintColorIsInline = true;
                SetTintColor(value);
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

            m_ScaleMode = ScaleMode.ScaleToFit;
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
            {
                float scale = UIElementsUtility.PixelsPerUnitScaleForElement(this, sprite);
                result = (Vector2)(sprite.bounds.size * sprite.pixelsPerUnit) * scale;
            }
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
            bool hasRect = rect != Rect.zero;
            // UUM-17229: rect width/height can be negative (e.g. when the UVs are flipped)
            measuredWidth = hasRect ? Mathf.Abs(rect.width) : sourceSize.x;
            measuredHeight = hasRect ? Mathf.Abs(rect.height) : sourceSize.y;

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

            var alignedRect = GUIUtility.AlignRectToDevice(contentRect);
            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;

            var rectParams = new UIR.MeshGenerator.RectangleParams();
            if (image != null)
                rectParams = UIR.MeshGenerator.RectangleParams.MakeTextured(alignedRect, uv, image, scaleMode, playModeTintColor);
            else if (sprite != null)
            {
                var slices = Vector4.zero;
                rectParams = UIR.MeshGenerator.RectangleParams.MakeSprite(alignedRect, uv, sprite, scaleMode, playModeTintColor, false, ref slices);
            }
            else if (vectorImage != null)
                rectParams = UIR.MeshGenerator.RectangleParams.MakeVectorTextured(alignedRect, uv, vectorImage, scaleMode, playModeTintColor);
            rectParams.color = tintColor;
            mgc.meshGenerator.DrawRectangle(rectParams);
        }

        static CustomStyleProperty<Texture2D> s_ImageProperty = new CustomStyleProperty<Texture2D>("--unity-image");
        static CustomStyleProperty<Sprite> s_SpriteProperty = new CustomStyleProperty<Sprite>("--unity-image");
        static CustomStyleProperty<VectorImage> s_VectorImageProperty = new CustomStyleProperty<VectorImage>("--unity-image");
        static CustomStyleProperty<string> s_ScaleModeProperty = new CustomStyleProperty<string>("--unity-image-size");
        static CustomStyleProperty<Color> s_TintColorProperty = new CustomStyleProperty<Color>("--unity-image-tint-color");

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            // We should consider not exposing image as a style at all, since it's intimately tied to uv/sourceRect
            ReadCustomProperties(e.customStyle);
        }

        private void ReadCustomProperties(ICustomStyle customStyleProvider)
        {
            if (!m_ImageIsInline)
            {
                if (customStyleProvider.TryGetValue(s_ImageProperty, out var textureValue))
                {
                    SetProperty(textureValue, ref m_Image, ref m_Sprite, ref m_VectorImage, imageProperty);
                }
                else if (customStyleProvider.TryGetValue(s_SpriteProperty, out var spriteValue))
                {
                    SetProperty(spriteValue, ref m_Sprite, ref m_Image, ref m_VectorImage, spriteProperty);
                }
                else if (customStyleProvider.TryGetValue(s_VectorImageProperty, out var vectorImageValue))
                {
                    SetProperty(vectorImageValue, ref m_VectorImage, ref m_Image, ref m_Sprite, vectorImageProperty);
                }
                // If the value is not inline and none of the custom style properties are resolved, unset the value.
                else
                {
                    ClearProperty();
                }
            }

            if (!m_ScaleModeIsInline && customStyleProvider.TryGetValue(s_ScaleModeProperty, out var scaleModeValue))
            {
                StylePropertyUtil.TryGetEnumIntValue(StyleEnumType.ScaleMode, scaleModeValue, out var intValue);
                SetScaleMode((ScaleMode)intValue);
            }

            if (!m_TintColorIsInline && customStyleProvider.TryGetValue(s_TintColorProperty, out var tintValue))
            {
                SetTintColor(tintValue);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetProperty<T0, T1, T2>(T0 src, ref T0 dst, ref T1 alt0, ref T2 alt1, BindingId binding)
            where T0 : Object where T1 : Object where T2 : Object
        {
            if (src == dst)
                return;

            dst = src;

            if (dst != null)
            {
                alt0 = null;
                alt1 = null;
            }

            if (dst == null)
            {
                uv = new Rect(0, 0, 1, 1);
                ReadCustomProperties(customStyle);
            }

            IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
            NotifyPropertyChanged(binding);
        }

        private void ClearProperty()
        {
            if (m_ImageIsInline)
                return;
            image = null;
            sprite = null;
            vectorImage = null;
        }

        private void SetScaleMode(ScaleMode mode)
        {
            if (m_ScaleMode != mode)
            {
                m_ScaleMode = mode;
                IncrementVersion(VersionChangeType.Repaint);
                NotifyPropertyChanged(scaleModeProperty);
            }
        }

        private void SetTintColor(Color color)
        {
            if (m_TintColor != color)
            {
                m_TintColor = color;
                IncrementVersion(VersionChangeType.Repaint);
                NotifyPropertyChanged(tintColorProperty);
            }
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
