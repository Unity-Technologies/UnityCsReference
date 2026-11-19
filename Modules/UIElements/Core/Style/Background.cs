// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes a <see cref="VisualElement"/> background.
    /// </summary>
    [Serializable]
    public partial struct Background : IEquatable<Background>
    {
        // EntityId-only round-trip for legacy call sites that don't carry gradient data.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static Background From(in EntityId entityId)
        {
            var obj = Resources.EntityIdToObject(entityId);
            return FromObject(obj);
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void To(in Background background, out EntityId entityId)
        {
            // Gradient-only backgrounds yield EntityId.None; the renderer bakes on demand.
            var obj = background.GetSelectedImage();
            entityId = obj?.GetEntityId() ?? EntityId.None;
        }

        // Compound-storage round-trip: reconstructs both the asset slot and the gradient metadata.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static Background From(in UnmanagedBackground bg)
        {
            var result = FromObject(Resources.EntityIdToObject(bg.imageEntityId));
            if (bg.gradient.Count > 0)
                result.m_Gradient = bg.gradient[0].ToManaged();
            return result;
        }

        // Composes asset slot and gradient without the public setters' mutual-exclusion,
        // matching From(UnmanagedBackground) semantics for the animation binder.
        internal static Background From(in EntityId imageEntityId, in UnmanagedBackgroundGradient gradient)
        {
            var result = FromObject(Resources.EntityIdToObject(imageEntityId));
            if (gradient.stopCount > 0)
                result.m_Gradient = gradient.ToManaged();
            return result;
        }

        [SerializeField]
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
                m_Gradient = default;
            }
        }

        [SerializeField]
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
                m_Gradient = default;
            }
        }

        [SerializeField]
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
                m_Gradient = default;
            }
        }

        [SerializeField]
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
                m_Gradient = default;
            }
        }

        [SerializeField]
        BackgroundGradient m_Gradient;
        /// <summary>
        /// The color gradient to display as a background, mutually exclusive with the asset slots.
        /// Setting an empty gradient (see <see cref="BackgroundGradient.IsEmpty"/>) clears the slot
        /// without disturbing any assigned asset.
        /// </summary>
        public BackgroundGradient gradient
        {
            get { return m_Gradient; }
            set
            {
                if (m_Gradient.Equals(value))
                    return;
                m_Gradient = value;
                if (!value.IsEmpty())
                {
                    m_Texture = null;
                    m_Sprite = null;
                    m_RenderTexture = null;
                    m_VectorImage = null;
                }
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
            m_Gradient = default;
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

        /// <summary>
        /// Creates a background from a <see cref="BackgroundGradient"/>.
        /// </summary>
        /// <param name="g">The gradient to use as a background. An empty gradient yields an empty background.</param>
        /// <returns>A new background object.</returns>
        public static Background FromGradient(BackgroundGradient g)
        {
            return new Background { gradient = g };
        }

        internal Background CopyWithOwnGradientStops()
        {
            var copy = this;
            copy.m_Gradient = m_Gradient.CopyWithOwnStops();
            return copy;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
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

        internal static IReadOnlyList<Type> allowedAssetTypes => [ typeof(Texture2D), typeof(RenderTexture), typeof(Sprite), typeof(VectorImage) ];

        /// <summary>
        /// Retrieves the selected asset which can be of a type of Texture, Sprite, RenderTexture or VectorImage.
        /// </summary>
        /// <returns>An asset as an object.</returns>
        public Object GetSelectedImage()
        {
            if (texture != null)
                return texture;
            if (sprite != null)
                return sprite;
            if (renderTexture != null)
                return renderTexture;
            if (vectorImage != null)
                return vectorImage;

            return null;
        }

        /// <summary>
        /// Help verify whether an asset or gradient has been assigned or not.
        /// </summary>
        /// <returns>True if no asset and no gradient is assigned.</returns>
        public bool IsEmpty()
        {
            return texture == null && sprite == null && vectorImage == null && renderTexture == null && m_Gradient.IsEmpty();
        }

        /// <undoc/>
        public static bool operator==(Background lhs, Background rhs)
        {
            return lhs.texture == rhs.texture &&
                   lhs.sprite == rhs.sprite &&
                   lhs.renderTexture == rhs.renderTexture &&
                   lhs.vectorImage == rhs.vectorImage &&
                   lhs.m_Gradient.Equals(rhs.m_Gradient);
        }

        /// <undoc/>
        public static bool operator!=(Background lhs, Background rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator Background(Texture2D v)
        {
            return FromTexture2D(v);
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
            if (!m_Gradient.IsEmpty())
                hashCode = hashCode * -1521134295 + m_Gradient.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            // Gradient wins over the baked VectorImage: show the USS form, not the asset name.
            if (!m_Gradient.IsEmpty())
                return m_Gradient.ToString();
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
