using System;
using UnityEngine;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Script interface for <see cref="VisualElement"/> text-shadow style property <see cref="IStyle.textShadow"/>.
    /// </summary>
    public struct TextShadow : IEquatable<TextShadow>
    {
        /// <summary>
        /// The offset of the shadow.
        /// </summary>
        public Vector2 offset;

        /// <summary>
        /// The blur radius of the shadow.
        /// </summary>
        public float blurRadius;

        /// <summary>
        /// The color of the shadow.
        /// </summary>
        public Color color;

        public override bool Equals(object obj)
        {
            return obj is TextShadow && Equals((TextShadow)obj);
        }

        public bool Equals(TextShadow other)
        {
            return other.offset == offset && other.blurRadius == blurRadius && other.color == color;
        }

        public override int GetHashCode()
        {
            var hashCode = 1500536833;
            hashCode = hashCode * -1521134295 + offset.GetHashCode();
            hashCode = hashCode * -1521134295 + blurRadius.GetHashCode();
            hashCode = hashCode * -1521134295 + color.GetHashCode();
            return hashCode;
        }

        public static bool operator==(TextShadow style1, TextShadow style2)
        {
            return style1.Equals(style2);
        }

        public static bool operator!=(TextShadow style1, TextShadow style2)
        {
            return !(style1 == style2);
        }

        public override string ToString()
        {
            return $"offset={offset}, blurRadius={blurRadius}, color={color}";
        }
    }
}
