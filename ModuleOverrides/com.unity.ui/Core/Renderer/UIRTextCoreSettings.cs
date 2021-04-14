// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEngine.UIElements.UIR
{
    internal struct TextCoreSettings : IEquatable<TextCoreSettings>
    {
        public Color outlineColor;
        public float outlineWidth;

        public Color underlayColor;
        public Vector2 underlayOffset;
        public float underlaySoftness;

        public override bool Equals(object obj)
        {
            return obj is TextCoreSettings && Equals((TextCoreSettings)obj);
        }

        public bool Equals(TextCoreSettings other)
        {
            return
                other.outlineColor == outlineColor &&
                other.outlineWidth == outlineWidth &&
                other.underlayColor == underlayColor &&
                other.underlayOffset == underlayOffset &&
                other.underlaySoftness == underlaySoftness;
        }

        public override int GetHashCode()
        {
            var hashCode = 75905159;
            hashCode = hashCode * -1521134295 + outlineColor.GetHashCode();
            hashCode = hashCode * -1521134295 + outlineWidth.GetHashCode();
            hashCode = hashCode * -1521134295 + underlayColor.GetHashCode();
            hashCode = hashCode * -1521134295 + underlayOffset.x.GetHashCode();
            hashCode = hashCode * -1521134295 + underlayOffset.y.GetHashCode();
            hashCode = hashCode * -1521134295 + underlaySoftness.GetHashCode();
            return hashCode;
        }
    }
}
