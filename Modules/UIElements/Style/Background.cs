// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public struct Background : IEquatable<Background>
    {
        public Texture2D texture { get; set; }

        public Background(Texture2D t)
        {
            texture = t;
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
            hashCode = hashCode * -1521134295 + EqualityComparer<Texture2D>.Default.GetHashCode(texture);
            return hashCode;
        }

        public override string ToString()
        {
            return $"{texture.ToString()}";
        }
    }
}
