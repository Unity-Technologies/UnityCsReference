// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Bindings;

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

        [FreeFunction("SpriteEditorExtension::GetSpriteIDScripting")]
        private static extern string GetSpriteIDScripting([NotNull] Sprite sprite);
        [FreeFunction("SpriteEditorExtension::SetSpriteIDScripting")]
        private static extern void SetSpriteIDScripting([NotNull] Sprite sprite, string spriteID);
    }
}
