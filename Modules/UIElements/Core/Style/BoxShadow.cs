// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Script interface for <see cref="VisualElement"/> box-shadow style property <see cref="IStyle.boxShadow"/>.
    /// </summary>
    /// <remarks>
    /// The default value draws no shadow and is equivalent to the USS keyword <c>none</c>.
    /// </remarks>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public partial struct BoxShadow : IEquatable<BoxShadow>
    {
        /// <summary>
        /// The color of the shadow.
        /// </summary>
        public Color color;

        /// <summary>
        /// The offset of the shadow, in pixels. Positive values move the shadow right and down.
        /// </summary>
        public Vector2 offset;

        /// <summary>
        /// The blur radius of the shadow, in pixels. Negative values are not allowed.
        /// </summary>
        public float blurRadius;

        /// <summary>
        /// The spread radius of the shadow, in pixels. Positive values grow the shadow, negative values shrink it.
        /// </summary>
        public float spreadRadius;

        /// <summary>
        /// When true, the shadow is drawn inside the element's border instead of outside of it.
        /// </summary>
        public bool inset;

        // The initial value of box-shadow is `none`, which is represented by the default value.
        internal static BoxShadow None() => default;

        // Exact, unlike == which compares the color and offset approximately
        internal bool IsNone()
        {
            return color.r == 0 && color.g == 0 && color.b == 0 && color.a == 0 && offset.x == 0 && offset.y == 0 &&
                blurRadius == 0 && spreadRadius == 0 && !inset;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is BoxShadow other && Equals(other);
        }

        /// <undoc/>
        public bool Equals(BoxShadow other)
        {
            return other.color == color && other.offset == offset && other.blurRadius == blurRadius &&
                other.spreadRadius == spreadRadius && other.inset == inset;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(color, offset, blurRadius, spreadRadius, inset);
        }

        /// <undoc/>
        public static bool operator==(BoxShadow style1, BoxShadow style2)
        {
            return style1.Equals(style2);
        }

        /// <undoc/>
        public static bool operator!=(BoxShadow style1, BoxShadow style2)
        {
            return !(style1 == style2);
        }

        public override string ToString()
        {
            return $"offset={offset}, blurRadius={blurRadius}, spreadRadius={spreadRadius}, color={color}, inset={inset}";
        }

        // CSS backgrounds-3: `none` interpolates as a transparent shadow with zero lengths and the other side's
        // inset value. Its color is the other side's, made transparent, so that a fade doesn't darken.
        // Shadows with different inset values switch halfway instead of interpolating.
        internal static BoxShadow LerpUnclamped(BoxShadow a, BoxShadow b, float t)
        {
            if (a.IsNone())
                a = new BoxShadow { color = new Color(b.color.r, b.color.g, b.color.b, 0), inset = b.inset };
            else if (b.IsNone())
                b = new BoxShadow { color = new Color(a.color.r, a.color.g, a.color.b, 0), inset = a.inset };

            if (a.inset != b.inset)
                return t < 0.5f ? a : b;

            return new BoxShadow
            {
                color = Color.LerpUnclamped(a.color, b.color, t),
                offset = Vector2.LerpUnclamped(a.offset, b.offset, t),
                blurRadius = Mathf.Max(0, Mathf.LerpUnclamped(a.blurRadius, b.blurRadius, t)),
                spreadRadius = Mathf.LerpUnclamped(a.spreadRadius, b.spreadRadius, t),
                inset = a.inset
            };
        }
    }
}
