// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

using UnityEngine.Assertions;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Script interface for <see cref="VisualElement"/> background-position style property <see cref="IStyle.BackgroundPosition"/>.
    /// </summary>
    public partial struct BackgroundPosition : IEquatable<BackgroundPosition>
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal enum Axis
        {
            Horizontal,
            Vertical
        }

        /// <summary>
        /// Background position type
        /// </summary>
        public BackgroundPositionKeyword keyword;

        /// <summary>
        /// Background offset
        /// </summary>
        public Length offset;

        /// <summary>
        /// Initialize from single position type
        /// </summary>
        public BackgroundPosition(BackgroundPositionKeyword keyword)
        {
            this.keyword = keyword;
            offset = new Length(0);
        }

        /// <summary>
        /// Initialize from x position type with x offset
        /// </summary>
        public BackgroundPosition(BackgroundPositionKeyword keyword, Length offset)
        {
            this.keyword = keyword;
            this.offset = offset;
        }

        internal static BackgroundPosition Initial()
        {
            return BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition();
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is BackgroundPosition && Equals((BackgroundPosition)obj);
        }

        /// <undoc/>
        public bool Equals(BackgroundPosition other)
        {
            return other.offset == offset && other.keyword == keyword;
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            var hashCode = 1500536833;
            hashCode = hashCode * -1521134295 + keyword.GetHashCode();
            hashCode = hashCode * -1521134295 + offset.GetHashCode();
            return hashCode;
        }

        /// <undoc/>
        public static bool operator==(BackgroundPosition style1, BackgroundPosition style2)
        {
            return style1.Equals(style2);
        }

        /// <undoc/>
        public static bool operator!=(BackgroundPosition style1, BackgroundPosition style2)
        {
            return !(style1 == style2);
        }

        /// <undoc/>
        public override string ToString()
        {
            return $"(type:{keyword} x:{offset})";
        }
    }
}
