// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public struct CursorStyle
    {
        public Texture2D texture { get; set; }
        public Vector2 hotspot { get; set; }
        // Used to support default cursor in the editor (map to MouseCursor enum)
        internal int defaultCursorId { get; set; }

        public override int GetHashCode()
        {
            return texture.GetHashCode() ^ hotspot.GetHashCode() ^ defaultCursorId.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (!(other is CursorStyle))
            {
                return false;
            }

            CursorStyle rhs = (CursorStyle)other;
            return texture.Equals(rhs.texture) && hotspot.Equals(rhs.hotspot) && defaultCursorId == rhs.defaultCursorId;
        }

        public static bool operator==(CursorStyle lhs, CursorStyle rhs)
        {
            return lhs.texture == rhs.texture && lhs.hotspot == rhs.hotspot;
        }

        public static bool operator!=(CursorStyle lhs, CursorStyle rhs)
        {
            return !(lhs == rhs);
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
            Debug.LogError("UIElements cursors are not yet supported outside of the editor.");
        }

        public void ResetCursor()
        {
            Debug.LogError("UIElements cursors are not yet supported outside of the editor.");
        }
    }
}
