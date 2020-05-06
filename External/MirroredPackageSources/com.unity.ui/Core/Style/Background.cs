using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes a <see cref="VisualElement"/> background.
    /// </summary>
    public struct Background : IEquatable<Background>
    {
        private Texture2D m_Texture;
        /// <summary>
        /// The texture to display as a background. You cannot set this and <see cref="Background.vectorImage"/> at the same time.
        /// </summary>
        public Texture2D texture
        {
            get { return m_Texture; }
            set
            {
                if (value != null && vectorImage != null)
                    throw new InvalidOperationException("Cannot set both texture and vectorImage on Background object");
                m_Texture = value;
            }
        }

        private VectorImage m_VectorImage;
        /// <summary>
        /// The <see cref="VectorImage"/> to display as a background.  You cannot set this and <see cref="Background.texture"/> at the same time.
        /// </summary>
        public VectorImage vectorImage
        {
            get { return m_VectorImage; }
            set
            {
                if (value != null && texture != null)
                    throw new InvalidOperationException("Cannot set both texture and vectorImage on Background object");
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
            m_VectorImage = null;
        }

        /// <summary>
        /// Creates a background from a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="t">The texture to use as a background.</param>
        /// <returns>A new background object.</returns>
        public static Background FromTexture2D(Texture2D t)
        {
            return new Background() { texture = t };
        }

        /// <summary>
        /// Creates a background from a <see cref="VectorImage"/>.
        /// </summary>
        /// <param name="vi">The vector image to use as a background.</param>
        /// <returns>A new background object.</returns>
        public static Background FromVectorImage(VectorImage vi)
        {
            return new Background() { vectorImage = vi };
        }

        internal static Background FromObject(object obj)
        {
            var texture = obj as Texture2D;
            if (texture != null)
                return Background.FromTexture2D(texture);

            var vectorImage = obj as VectorImage;
            if (vectorImage != null)
                return Background.FromVectorImage(vectorImage);

            return default(Background);
        }

        public static bool operator==(Background lhs, Background rhs)
        {
            return EqualityComparer<Texture2D>.Default.Equals(lhs.texture, rhs.texture);
        }

        public static bool operator!=(Background lhs, Background rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Background other)
        {
            return other == this;
        }

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
            if (texture != null)
                hashCode = hashCode * -1521134295 + EqualityComparer<Texture2D>.Default.GetHashCode(texture);
            if (vectorImage != null)
                hashCode = hashCode * -1521134295 + EqualityComparer<VectorImage>.Default.GetHashCode(vectorImage);
            return hashCode;
        }

        public override string ToString()
        {
            return $"{texture}";
        }
    }
}
