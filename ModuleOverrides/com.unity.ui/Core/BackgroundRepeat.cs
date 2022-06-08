// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Script interface for <see cref="VisualElement"/> background-repeat style property <see cref="IStyle.backgroundRepeat"/>.
    /// </summary>
    public struct BackgroundRepeat : IEquatable<BackgroundRepeat>
    {
        /// <summary>
        /// Background repeat in the x direction.
        /// </summary>
        public Repeat x;

        /// <summary>
        /// Background repeat in the y direction.
        /// </summary>
        public Repeat y;

        /// <summary>
        /// Create a BackgroundRepeat with x and y repeat
        /// </summary>
        public BackgroundRepeat(Repeat repeatX, Repeat repeatY)
        {
            x = repeatX;
            y = repeatY;
        }

        internal static BackgroundRepeat Initial()
        {
            return BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat();
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is BackgroundRepeat && Equals((BackgroundRepeat)obj);
        }

        /// <undoc/>
        public bool Equals(BackgroundRepeat other)
        {
            return other.x == x && other.y == y;
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            var hashCode = 1500536833;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        /// <undoc/>
        public static bool operator==(BackgroundRepeat style1, BackgroundRepeat style2)
        {
            return style1.Equals(style2);
        }

        /// <undoc/>
        public static bool operator!=(BackgroundRepeat style1, BackgroundRepeat style2)
        {
            return !(style1 == style2);
        }

        /// <undoc/>
        public override string ToString()
        {
            return $"(x:{x}, y:{y})";
        }
    }
}
