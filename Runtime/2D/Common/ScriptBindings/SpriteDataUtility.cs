// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Sprites
{
    public sealed partial class DataUtility
    {
        // UV coordinates for the inner part of a sliced Sprite or the whole Sprite if borders=0
        public static Vector4 GetInnerUV(Sprite sprite)
        {
            return sprite.GetInnerUVs();
        }

        // UV coordinates for the outer part of a sliced Sprite (the whole Sprite)
        public static Vector4 GetOuterUV(Sprite sprite)
        {
            return sprite.GetOuterUVs();
        }

        // Pixel padding from the edges of the sprite to the drawn rectangle (left, bottom, right, top). Valid when the RenderData rect does not match the definition rect (i.e. alpha trimming).
        public static Vector4 GetPadding(Sprite sprite)
        {
            return sprite.GetPadding();
        }

        // Gets the minimum size of a sliced Sprite
        public static Vector2 GetMinSize(Sprite sprite)
        {
            Vector2 v;
            v.x = sprite.border.x + sprite.border.z;
            v.y = sprite.border.y + sprite.border.w;
            return v;
        }
    }
}
