// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Shape of a radial gradient.
    /// </summary>
    public enum BackgroundGradientShape
    {
        /// <summary>An axis-aligned ellipse stretched to the gradient size.</summary>
        Ellipse,
        /// <summary>A circle whose radius is taken from the gradient size.</summary>
        Circle
    }

    /// <summary>
    /// Sizing rule of a radial gradient, mirroring the CSS radial-gradient extent keywords.
    /// </summary>
    public enum BackgroundGradientSize
    {
        /// <summary>Reach the corner of the element farthest from the gradient center (CSS default).</summary>
        FarthestCorner,
        /// <summary>Reach the side of the element farthest from the gradient center.</summary>
        FarthestSide,
        /// <summary>Reach the corner of the element closest to the gradient center.</summary>
        ClosestCorner,
        /// <summary>Reach the side of the element closest to the gradient center.</summary>
        ClosestSide,
    }

    /// <summary>
    /// A single color stop in a <see cref="BackgroundGradient"/>.
    /// </summary>
    [Serializable]
    public struct BackgroundGradientStop : IEquatable<BackgroundGradientStop>
    {
        /// <summary>Color of the stop.</summary>
        public Color color;

        /// <summary>
        /// Position of the stop along the gradient axis. Interpretation depends on <see cref="positionIsPercent"/>:
        /// when true, expressed as a fraction in [0,1]; when false, expressed as a length in element-local pixels.
        /// </summary>
        public float position;

        /// <summary>True when <see cref="position"/> is a percentage (fraction of the gradient extent).</summary>
        public bool positionIsPercent;

        /// <summary>Construct a stop expressed as a percentage of the gradient extent.</summary>
        public static BackgroundGradientStop Percent(Color color, float fraction)
        {
            return new BackgroundGradientStop { color = color, position = fraction, positionIsPercent = true };
        }

        /// <summary>Construct a stop expressed as a pixel length along the gradient axis.</summary>
        public static BackgroundGradientStop Pixels(Color color, float pixels)
        {
            return new BackgroundGradientStop { color = color, position = pixels, positionIsPercent = false };
        }

        /// <undoc/>
        public bool Equals(BackgroundGradientStop other)
        {
            return color == other.color
                && position.Equals(other.position)
                && positionIsPercent == other.positionIsPercent;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is BackgroundGradientStop other && Equals(other);
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            var h = color.GetHashCode();
            h = (h * -1521134295) + position.GetHashCode();
            h = (h * -1521134295) + positionIsPercent.GetHashCode();
            return h;
        }

        /// <undoc/>
        public static bool operator ==(BackgroundGradientStop a, BackgroundGradientStop b) => a.Equals(b);

        /// <undoc/>
        public static bool operator !=(BackgroundGradientStop a, BackgroundGradientStop b) => !a.Equals(b);

        /// <undoc/>
        public override string ToString()
        {
            int r = Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f);
            int g = Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f);
            int b = Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f);
            string colorStr = Mathf.Approximately(color.a, 1f)
                ? $"rgb({r},{g},{b})"
                : $"rgba({r},{g},{b},{color.a:0.###})";
            return positionIsPercent
                ? $"{colorStr} {position * 100f:0.##}%"
                : $"{colorStr} {position:0.##}px";
        }
    }

    /// <summary>
    /// Color gradient that can be used as a <see cref="Background"/>, mirroring the CSS
    /// linear-gradient() / radial-gradient() functions.
    /// </summary>
    /// <remarks>
    /// A default-constructed <see cref="BackgroundGradient"/> has no color stops and is treated
    /// as "no gradient" — see <see cref="IsEmpty"/>.
    /// </remarks>
    [Serializable]
    public struct BackgroundGradient : IEquatable<BackgroundGradient>
    {
        [SerializeField]
        GradientType m_Type;
        /// <summary>Whether the gradient transitions linearly or radiates from a center point.</summary>
        public GradientType type { get => m_Type; set => m_Type = value; }

        [SerializeField]
        float m_Angle;
        /// <summary>
        /// Angle of a linear gradient, in radians, measured clockwise from "to top" (CSS convention,
        /// matching `linear-gradient(0deg, ...)`). Ignored for radial gradients.
        /// </summary>
        public float angle { get => m_Angle; set => m_Angle = value; }

        [SerializeField]
        BackgroundGradientStop[] m_Stops;
        /// <summary>Ordered list of color stops along the gradient axis.</summary>
        public BackgroundGradientStop[] stops { get => m_Stops; set => m_Stops = value; }

        [SerializeField]
        BackgroundGradientShape m_Shape;
        /// <summary>Shape used to interpret <see cref="size"/> for radial gradients. Ignored for linear gradients.</summary>
        public BackgroundGradientShape shape { get => m_Shape; set => m_Shape = value; }

        [SerializeField]
        BackgroundGradientSize m_Size;
        /// <summary>Extent of a radial gradient. Ignored for linear gradients.</summary>
        public BackgroundGradientSize size { get => m_Size; set => m_Size = value; }

        [SerializeField]
        Vector2 m_Position;
        /// <summary>
        /// Center of a radial gradient, expressed as a fraction in [0,1] of the element box
        /// (0 = top-left, 1 = bottom-right). Default (0.5, 0.5) places the gradient at the
        /// element center. Ignored for linear gradients.
        /// </summary>
        public Vector2 position { get => m_Position; set => m_Position = value; }

        /// <summary>True when this gradient has no color stops, meaning it represents "no gradient".</summary>
        public bool IsEmpty()
        {
            return m_Stops == null || m_Stops.Length == 0;
        }

        /// <summary>Construct a linear gradient from an angle (radians) and a stop list.</summary>
        public static BackgroundGradient Linear(float angle, params BackgroundGradientStop[] stops)
        {
            return new BackgroundGradient
            {
                m_Type = GradientType.Linear,
                m_Angle = angle,
                m_Stops = stops,
                m_Position = new Vector2(0.5f, 0.5f),
                m_Shape = BackgroundGradientShape.Ellipse,
                m_Size = BackgroundGradientSize.FarthestCorner,
            };
        }

        /// <summary>Construct a radial gradient at the element center with default extent / shape.</summary>
        public static BackgroundGradient Radial(params BackgroundGradientStop[] stops)
        {
            return new BackgroundGradient
            {
                m_Type = GradientType.Radial,
                m_Angle = 0f,
                m_Stops = stops,
                m_Position = new Vector2(0.5f, 0.5f),
                m_Shape = BackgroundGradientShape.Ellipse,
                m_Size = BackgroundGradientSize.FarthestCorner,
            };
        }

        /// <undoc/>
        public bool Equals(BackgroundGradient other)
        {
            if (m_Type != other.m_Type) return false;
            if (!m_Angle.Equals(other.m_Angle)) return false;
            if (m_Shape != other.m_Shape) return false;
            if (m_Size != other.m_Size) return false;
            if (!m_Position.Equals(other.m_Position)) return false;
            if (m_Stops == null) return other.m_Stops == null || other.m_Stops.Length == 0;
            if (other.m_Stops == null) return m_Stops.Length == 0;
            if (m_Stops.Length != other.m_Stops.Length) return false;
            for (int i = 0; i < m_Stops.Length; ++i)
            {
                if (!m_Stops[i].Equals(other.m_Stops[i])) return false;
            }
            return true;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is BackgroundGradient other && Equals(other);
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            var h = m_Type.GetHashCode();
            h = (h * -1521134295) + m_Angle.GetHashCode();
            h = (h * -1521134295) + m_Shape.GetHashCode();
            h = (h * -1521134295) + m_Size.GetHashCode();
            h = (h * -1521134295) + m_Position.GetHashCode();
            if (m_Stops != null)
            {
                for (int i = 0; i < m_Stops.Length; ++i)
                    h = (h * -1521134295) + m_Stops[i].GetHashCode();
            }
            return h;
        }

        /// <undoc/>
        public static bool operator ==(BackgroundGradient a, BackgroundGradient b) => a.Equals(b);

        /// <undoc/>
        public static bool operator !=(BackgroundGradient a, BackgroundGradient b) => !a.Equals(b);

        /// <undoc/>
        public override string ToString()
        {
            if (IsEmpty())
                return "";

            if (m_Type == GradientType.Linear)
                return $"linear-gradient({m_Angle * Mathf.Rad2Deg:0.##}deg, {StopsToString()})";

            // Match the USS writer's elision rules so a round-trip is stable.
            string sizeKw = m_Size switch
            {
                BackgroundGradientSize.ClosestSide => "closest-side",
                BackgroundGradientSize.ClosestCorner => "closest-corner",
                BackgroundGradientSize.FarthestSide => "farthest-side",
                _ => "farthest-corner",
            };
            bool sizeIsDefault = m_Size == BackgroundGradientSize.FarthestCorner;
            bool positionIsDefault = Mathf.Approximately(m_Position.x, 0.5f)
                                  && Mathf.Approximately(m_Position.y, 0.5f);

            bool isCircle = m_Shape == BackgroundGradientShape.Circle;

            var prefix = new System.Text.StringBuilder();
            if (isCircle)
                prefix.Append("circle");
            if (!sizeIsDefault)
                prefix.Append(isCircle ? " " : "ellipse ").Append(sizeKw);
            if (!positionIsDefault)
            {
                if (prefix.Length > 0) prefix.Append(' ');
                prefix.Append($"at {m_Position.x * 100f:0.##}% {m_Position.y * 100f:0.##}%");
            }

            return prefix.Length > 0
                ? $"radial-gradient({prefix}, {StopsToString()})"
                : $"radial-gradient({StopsToString()})";
        }

        string StopsToString()
        {
            if (m_Stops == null || m_Stops.Length == 0)
                return "";
            var s = new System.Text.StringBuilder();
            for (int i = 0; i < m_Stops.Length; ++i)
            {
                if (i > 0) s.Append(", ");
                s.Append(m_Stops[i]);
            }
            return s.ToString();
        }
    }
}
