using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Script interface for <see cref="VisualElement"/> cursor style property <see cref="IStyle.cursor"/>.
    /// </summary>
    public struct Cursor : IEquatable<Cursor>
    {
        /// <summary>
        /// The texture to use for the cursor style. To use a texture as a cursor, import the texture with "Read/Write enabled" in the texture importer (or using the "Cursor" defaults).
        /// </summary>
        public Texture2D texture { get; set; }
        /// <summary>
        /// The offset from the top left of the texture to use as the target point (must be within the bounds of the cursor).
        /// </summary>
        public Vector2 hotspot { get; set; }
        // Used to support default cursor in the editor (map to MouseCursor enum)
        internal int defaultCursorId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Cursor && Equals((Cursor)obj);
        }

        public bool Equals(Cursor other)
        {
            return EqualityComparer<Texture2D>.Default.Equals(texture, other.texture) &&
                hotspot.Equals(other.hotspot) &&
                defaultCursorId == other.defaultCursorId;
        }

        public override int GetHashCode()
        {
            var hashCode = 1500536833;
            hashCode = hashCode * -1521134295 + EqualityComparer<Texture2D>.Default.GetHashCode(texture);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(hotspot);
            hashCode = hashCode * -1521134295 + defaultCursorId.GetHashCode();
            return hashCode;
        }

        public static bool operator==(Cursor style1, Cursor style2)
        {
            return style1.Equals(style2);
        }

        public static bool operator!=(Cursor style1, Cursor style2)
        {
            return !(style1 == style2);
        }

        public override string ToString()
        {
            return $"texture={texture}, hotspot={hotspot}";
        }
    }

    internal interface ICursorManager
    {
        void SetCursor(Cursor cursor);
        void ResetCursor();
    }

    internal class CursorManager : ICursorManager
    {
        public void SetCursor(Cursor cursor)
        {
            if (cursor.texture != null)
            {
                UnityEngine.Cursor.SetCursor(cursor.texture, cursor.hotspot, CursorMode.Auto);
            }
            else
            {
                if (cursor.defaultCursorId != 0)
                {
                    Debug.LogWarning(
                        "Runtime cursors other than the default cursor need to be defined using a texture.");
                }

                ResetCursor();
            }
        }

        public void ResetCursor()
        {
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
