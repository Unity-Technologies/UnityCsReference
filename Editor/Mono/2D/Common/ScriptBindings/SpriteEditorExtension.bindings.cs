// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.U2D;

namespace UnityEditor.Experimental.U2D
{
    [NativeHeader("Editor/Src/2D/SpriteEditorExtension.h")]
    public static class SpriteEditorExtension
    {
        public static GUID GetSpriteID(this Sprite sprite)
        {
            return new GUID(GetSpriteIDScripting(sprite));
        }

        public static void SetSpriteID(this Sprite sprite, GUID guid)
        {
            SetSpriteIDScripting(sprite, guid.ToString());
        }

        private static extern string GetSpriteIDScripting([NotNull] Sprite sprite);
        private static extern void SetSpriteIDScripting([NotNull] Sprite sprite, string spriteID);
        internal static extern SpriteAtlas GetActiveAtlas([NotNull] this Sprite sprite);
        internal static extern string GetActiveAtlasName([NotNull] this Sprite sprite);
        internal static extern Texture2D GetActiveAtlasTexture([NotNull] this Sprite sprite);
        internal static extern Rect GetActiveAtlasTextureRect([NotNull] this Sprite sprite);
        internal static extern Vector2 GetActiveAtlasTextureRectOffset([NotNull] this Sprite sprite);
        internal static extern Texture2D GetActiveAtlasAlphaTexture([NotNull] this Sprite sprite);
    }
}
