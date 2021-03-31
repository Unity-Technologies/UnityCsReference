using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes a <see cref="VisualElement"/> background.
    /// </summary>
    public struct Background : IEquatable<Background>
    {
        Texture2D m_Texture;
        /// <summary>
        /// The texture to display as a background.
        /// </summary>
        public Texture2D texture
        {
            get { return m_Texture; }
            set
            {
                if (m_Texture == value)
                    return;
                m_Texture = value;
                m_Sprite = null;
                m_RenderTexture = null;
                m_VectorImage = null;
            }
        }

        private Sprite m_Sprite;
        /// <summary>
        /// The sprite to display as a background.
        /// </summary>
        public Sprite sprite
        {
            get { return m_Sprite; }
            set
            {
                if (m_Sprite == value)
                    return;
                m_Texture = null;
                m_Sprite = value;
                m_RenderTexture = null;
                m_VectorImage = null;
            }
        }

        RenderTexture m_RenderTexture;
        /// <summary>
        /// The <see cref="RenderTexture"/> to display as a background.
        /// </summary>
        public RenderTexture renderTexture
        {
            get { return m_RenderTexture; }
            set
            {
                if (m_RenderTexture == value)
                    return;
                m_Texture = null;
                m_Sprite = null;
                m_RenderTexture = value;
                m_VectorImage = null;
            }
        }

        VectorImage m_VectorImage;
        /// <summary>
        /// The <see cref="VectorImage"/> to display as a background.
        /// </summary>
        public VectorImage vectorImage
        {
            get { return m_VectorImage; }
            set
            {
                if (vectorImage == value)
                    return;
                m_Texture = null;
                m_Sprite = null;
                m_RenderTexture = null;
                m_VectorImage = value;
            }
        }

        /// <summary>
        /// Creates from a <see cref="Texture2D"/>.
        /// </summary>
        [Obsolete("Use Background.FromTexture2D instead")]
        public Background(Texture2D t)
        {
            m_Texture = t;
            m_Sprite = null;
            m_RenderTexture = null;
            m_VectorImage = null;
        }

        /// <summary>
        /// Creates a background from a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="t">The texture to use as a background.</param>
        /// <returns>A new background object.</returns>
        public static Background FromTexture2D(Texture2D t)
        {
            return new Background { texture = t };
        }

        /// <summary>
        /// Creates a background from a <see cref="RenderTexture"/>.
        /// </summary>
        /// <param name="rt">The render texture to use as a background.</param>
        /// <returns>A new background object.</returns>
        public static Background FromRenderTexture(RenderTexture rt)
        {
            return new Background { renderTexture = rt };
        }

        /// <summary>
        /// Creates a background from a <see cref="Sprite"/>.
        /// </summary>
        /// <param name="s">The sprite to use as a background.</param>
        /// <returns>A new background object.</returns>
        public static Background FromSprite(Sprite s)
        {
            return new Background() { sprite = s };
        }

        /// <summary>
        /// Creates a background from a <see cref="VectorImage"/>.
        /// </summary>
        /// <param name="vi">The vector image to use as a background.</param>
        /// <returns>A new background object.</returns>
        public static Background FromVectorImage(VectorImage vi)
        {
            return new Background { vectorImage = vi };
        }

        internal static Background FromObject(object obj)
        {
            var texture = obj as Texture2D;
            if (texture != null)
                return FromTexture2D(texture);

            var renderTexture = obj as RenderTexture;
            if (renderTexture != null)
                return FromRenderTexture(renderTexture);

            var sprite = obj as Sprite;
            if (sprite != null)
                return Background.FromSprite(sprite);

            var vectorImage = obj as VectorImage;
            if (vectorImage != null)
                return FromVectorImage(vectorImage);

            return default;
        }

        /// <undoc/>
        public static bool operator==(Background lhs, Background rhs)
        {
            return lhs.texture == rhs.texture && lhs.sprite == rhs.sprite && lhs.renderTexture == rhs.renderTexture && lhs.vectorImage == rhs.vectorImage;
        }

        /// <undoc/>
        public static bool operator!=(Background lhs, Background rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(Background other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is Background))
            {
                return false;
            }

            var v = (Background)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = 851985039;
            // The hash code must remain the same if the underlying object is destroyed and the handle becomes fake-null.
            // Otherwise it would suddenly become impossible to remove the entry from a dictionary.
            if (!ReferenceEquals(texture, null))
                hashCode = hashCode * -1521134295 + texture.GetHashCode();
            if (!ReferenceEquals(sprite, null))
                hashCode = hashCode * -1521134295 + sprite.GetHashCode();
            if (!ReferenceEquals(renderTexture, null))
                hashCode = hashCode * -1521134295 + renderTexture.GetHashCode();
            if (!ReferenceEquals(vectorImage, null))
                hashCode = hashCode * -1521134295 + vectorImage.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            if (texture != null)
                return texture.ToString();
            if (sprite != null)
                return sprite.ToString();
            if (renderTexture != null)
                return renderTexture.ToString();
            if (vectorImage != null)
                return vectorImage.ToString();
            return "";
        }
    }
}
