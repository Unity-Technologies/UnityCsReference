// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public struct CursorStyle : IEquatable<CursorStyle>
    {
        public Texture2D texture { get; set; }
        public Vector2 hotspot { get; set; }
        // Used to support default cursor in the editor (map to MouseCursor enum)
        internal int defaultCursorId { get; set; }


        public bool Equals(CursorStyle other)
        {
            return Equals(texture, other.texture) && hotspot.Equals(other.hotspot) && defaultCursorId == other.defaultCursorId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CursorStyle && Equals((CursorStyle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (texture != null ? texture.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ hotspot.GetHashCode();
                hashCode = (hashCode * 397) ^ defaultCursorId;
                return hashCode;
            }
        }
    }

    internal interface ICursorManager
    {
        void SetCursor(CursorStyle cursor);
        void ResetCursor();
    }

    // In game implementation (not implemented yet)
    internal class CursorManager : ICursorManager
    {
        public void SetCursor(CursorStyle cursor)
        {
        }

        public void ResetCursor()
        {
        }
    }
}
