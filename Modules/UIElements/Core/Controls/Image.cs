// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A <see cref="VisualElement"/> representing a source texture.
    /// </summary>
    /// <remarks>
    /// SA: [[wiki:UIE-uxml-element-Image|UXML element Image]].\\
    ///\\
    /// **Note**: This is the Image control for the UI Toolkit framework. This is not related to the
    /// <a href="https://docs.unity3d.com/Packages/com.unity.ugui@latest/index.html?subfolder=/api/UnityEngine.UI.Image.html">UnityEngine.UI.Image</a> uGUI control.
    /// </remarks>
    [Icon("UIToolkit/Icons/Image.png")]
    public partial class Image : VisualElement
    {
        internal static readonly BindingId sourceProperty = nameof(source);
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
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(source), "source"),
                    new (nameof(tintColor), "tint-color"),
                    new (nameof(scaleMode), "scale-mode"),
                    new (nameof(uv), "uv")
                }, false);
            }

            #pragma warning disable 649
            [SerializeField, ImageFieldValueDecorator("Source")] Object source;
            [SerializeField] Color tintColor;
            [Tooltip("The base texture coordinates of the Image relative to the bottom left corner.")]
            [SerializeField] Rect uv;
            [SerializeField] ScaleMode scaleMode;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags source_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags tintColor_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags uv_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags scaleMode_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Image();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (Image)obj;

                if (ShouldWriteAttributeValue(source_UxmlAttributeFlags))
                    e.source = source;
                if (ShouldWriteAttributeValue(tintColor_UxmlAttributeFlags))
                    e.tintColor = tintColor;
                if (ShouldWriteAttributeValue(uv_UxmlAttributeFlags))
                    e.uv = uv;
                if (ShouldWriteAttributeValue(scaleMode_UxmlAttributeFlags))
                    e.scaleMode = scaleMode;
            }
        }

        private ScaleMode m_ScaleMode;
        private Object m_Image;

        private Rect m_UV;
        private Color m_TintColor;

        // Internal for tests
        internal bool m_ImageIsInline;
        internal bool m_ScaleModeIsInline;
        internal bool m_TintColorIsInline;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [CreateProperty]
        internal Object source
        {
            get => m_Image;
            set
            {
                switch (value)
                {
                    case Texture t:
                        image = t;
                        break;
                    case Sprite s:
                        sprite = s;
                        break;
                    case VectorImage v:
                        vectorImage = v;
                        break;
                    default:
                        SetInlineProperty<Object>(null, imageProperty);
                        break;
                }

                NotifyPropertyChanged(sourceProperty);
            }
        }

        /// <summary>
        /// The texture to display in this image. If you assign a `Texture` or `Texture2D`, the Image element will resize and show the assigned texture.
        /// </summary>
        /// <example>
        /// The following example creates an `Image` element and assigns a texture to it.
        /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/AddImageExample.cs"/>
        /// </example>
        [CreateProperty]
        public Texture image
        {
            get => m_Image as Texture;
            set => SetInlineProperty<Texture>(value, imageProperty);
        }

        /// <summary>
        /// The sprite to display in this image.
        /// </summary>
        [CreateProperty]
        public Sprite sprite
        {
            get => m_Image as Sprite;
            set => SetInlineProperty<Sprite>(value, spriteProperty);
        }

        /// <summary>
        /// The <see cref="VectorImage"/> to display in this image.
        /// </summary>
        [CreateProperty]
        public VectorImage vectorImage
        {
            get => m_Image as VectorImage;
            set => SetInlineProperty<VectorImage>(value, vectorImageProperty);
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

            if (source == null)
                return new Vector2(measuredWidth, measuredHeight);

            var sourceSize = Vector2.zero;

            switch (source)
            {
                case Texture image:
                    sourceSize = GetTextureDisplaySize(image);
                    break;

                case Sprite sprite:
                    sourceSize = GetTextureDisplaySize(sprite);
                    break;

                case VectorImage vectorImage:
                    sourceSize = vectorImage.size;
                    break;
            }

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
            if (source == null)
                return;

            // As the border/padding in the resolved style are now rouded by the layout engine, we should never have a content rect that is not aligned to the pixel grid.
            // Adding assert to verify this(Jan 2025), they could be removed in the future.
            var alignedRect = contentRect;
            Debug.Assert(Mathf.Abs(alignedRect.x - this.RoundToPanelPixelSize(alignedRect.x)) < 0.01);
            Debug.Assert(Mathf.Abs(alignedRect.y - this.RoundToPanelPixelSize(alignedRect.y)) < 0.01);
            Debug.Assert(Mathf.Abs(alignedRect.width - this.RoundToPanelPixelSize(alignedRect.width)) < 0.01);
            Debug.Assert(Mathf.Abs(alignedRect.height - this.RoundToPanelPixelSize(alignedRect.height)) < 0.01);

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

        internal static CustomStyleProperty<Texture2D> s_ImageProperty = new CustomStyleProperty<Texture2D>("--unity-image");
        internal static CustomStyleProperty<Sprite> s_SpriteProperty = new CustomStyleProperty<Sprite>("--unity-image");
        internal static CustomStyleProperty<VectorImage> s_VectorImageProperty = new CustomStyleProperty<VectorImage>("--unity-image");
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
                    SetCustomProperty(textureValue, imageProperty);
                }
                else if (customStyleProvider.TryGetValue(s_SpriteProperty, out var spriteValue))
                {
                    SetCustomProperty(spriteValue, spriteProperty);
                }
                else if (customStyleProvider.TryGetValue(s_VectorImageProperty, out var vectorImageValue))
                {
                    SetCustomProperty(vectorImageValue, vectorImageProperty);
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

            if (!m_TintColorIsInline)
            {
                if (customStyleProvider.TryGetValue(s_TintColorProperty, out var tintValue))
                    SetTintColor(tintValue);
                else
                {
                    SetTintColor(Color.white);
                }
            }
        }

        void SetInlineProperty<T>(Object value, BindingId binding)
        {
            if (source == value && m_ImageIsInline)
                return;

            // Clear the old value if it came from a custom property.
            if (!m_ImageIsInline)
            {
                m_Image = null;
            }

            if (value != null)
            {
                m_Image = value;
            }
            else if (m_Image is T)
            {
                // Clear the old value as its the same type.
                m_Image = null;
            }

            m_ImageIsInline = m_Image != null;

            if (m_Image == null)
            {
                uv = new Rect(0, 0, 1, 1);
                ReadCustomProperties(customStyle);
            }

            IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
            NotifyPropertyChanged(binding);
        }

        void SetCustomProperty(Object value, BindingId binding)
        {
            Debug.Assert(!m_ImageIsInline, "Expected image to not be inline when using set custom property");
            if (value == source)
                return;

            m_Image = value;

            IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
            NotifyPropertyChanged(binding);
        }

        private void ClearProperty()
        {
            if (m_ImageIsInline)
                return;
            m_Image = null;
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
