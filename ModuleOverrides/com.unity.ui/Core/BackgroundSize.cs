// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Script interface for <see cref="VisualElement"/> background-size style property <see cref="IStyle.BackgroundSize"/>.
    /// </summary>
    public partial struct BackgroundSize : IEquatable<BackgroundSize>
    {
        /// <summary>
        /// Background size type
        /// </summary>
        public BackgroundSizeType sizeType
        {
            get
            {
                return m_SizeType;
            }
            set
            {
                m_SizeType = value;
                m_X = new Length(0);
                m_Y = new Length(0);
            }
        }

        /// <summary>
        /// Background size x
        /// </summary>
        public Length x
        {
            get
            {
                return m_X;
            }
            set
            {
                m_X = value;
                m_SizeType = BackgroundSizeType.Length;
            }
        }

        /// <summary>
        /// Background size y
        /// </summary>
        public Length y
        {
            get
            {
                return m_Y;
            }
            set
            {
                m_Y = value;
                m_SizeType = BackgroundSizeType.Length;
            }
        }

        private BackgroundSizeType m_SizeType;
        private Length m_X;
        private Length m_Y;

        /// <summary>
        /// Create a BackgroundSize with x and y repeat
        /// </summary>
        public BackgroundSize(Length sizeX, Length sizeY)
        {
            m_SizeType = BackgroundSizeType.Length;
            m_X = sizeX;
            m_Y = sizeY;
        }

        /// <summary>
        /// Create a BackgroundSize using Enum
        /// </summary>
        public BackgroundSize(BackgroundSizeType sizeType)
        {
            m_SizeType = sizeType;
            m_X = new Length(0);
            m_Y = new Length(0);
        }

        internal static BackgroundSize Initial()
        {
            return BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize();
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is BackgroundSize && Equals((BackgroundSize)obj);
        }

        /// <undoc/>
        public bool Equals(BackgroundSize other)
        {
            return other.x == x && other.y == y && other.sizeType == sizeType;
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            var hashCode = 1500536833;
            hashCode = hashCode * -1521134295 + m_SizeType.GetHashCode();
            hashCode = hashCode * -1521134295 + m_X.GetHashCode();
            hashCode = hashCode * -1521134295 + m_Y.GetHashCode();
            return hashCode;
        }

        /// <undoc/>
        public static bool operator==(BackgroundSize style1, BackgroundSize style2)
        {
            return style1.Equals(style2);
        }

        /// <undoc/>
        public static bool operator!=(BackgroundSize style1, BackgroundSize style2)
        {
            return !(style1 == style2);
        }

        /// <undoc/>
        public override string ToString()
        {
            return $"(sizeType:{sizeType} x:{x}, y:{y})";
        }
    }
}
