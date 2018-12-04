// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets
{
    internal class InheritedStylesData : IEquatable<InheritedStylesData>
    {
        public static readonly InheritedStylesData none = new InheritedStylesData();

        public StyleColor color;
        public StyleFont font;
        public StyleLength fontSize;
        public StyleInt unityFontStyle;
        public StyleInt unityTextAlign;
        public StyleInt visibility;
        public StyleInt whiteSpace;

        public InheritedStylesData()
        {
            color = StyleSheetCache.GetInitialValue(StylePropertyID.Color).color;
        }

        public InheritedStylesData(InheritedStylesData other)
        {
            CopyFrom(other);
        }

        public void CopyFrom(InheritedStylesData other)
        {
            if (other != null)
            {
                color = other.color;
                font = other.font;
                fontSize = other.fontSize;
                visibility = other.visibility;
                whiteSpace = other.whiteSpace;
                unityFontStyle = other.unityFontStyle;
                unityTextAlign = other.unityTextAlign;
            }
        }

        public bool Equals(InheritedStylesData other)
        {
            if (other == null)
                return false;

            return color == other.color &&
                font == other.font &&
                fontSize == other.fontSize &&
                unityFontStyle == other.unityFontStyle &&
                unityTextAlign == other.unityTextAlign &&
                visibility == other.visibility &&
                whiteSpace == other.whiteSpace;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InheritedStylesData))
            {
                return false;
            }

            var v = (InheritedStylesData)obj;
            return this.Equals(v);
        }

        public override int GetHashCode()
        {
            var hashCode = -2037960190;
            hashCode = hashCode * -1521134295 + color.GetHashCode();
            hashCode = hashCode * -1521134295 + font.GetHashCode();
            hashCode = hashCode * -1521134295 + fontSize.GetHashCode();
            hashCode = hashCode * -1521134295 + unityFontStyle.GetHashCode();
            hashCode = hashCode * -1521134295 + unityTextAlign.GetHashCode();
            hashCode = hashCode * -1521134295 + visibility.GetHashCode();
            hashCode = hashCode * -1521134295 + whiteSpace.GetHashCode();
            return hashCode;
        }
    }
}
