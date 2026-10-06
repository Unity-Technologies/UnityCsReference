// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;


namespace UnityEditor.TextCore.LowLevel
{
    [NativeHeader("Modules/TextCoreFontEngineEditor/Native/FontEngineEditorUtilities.h")]
    internal sealed class FontEngineEditorUtilities
    {
        [NativeMethod(Name = "TextCore::FontEngineEditorUtilities::SetAtlasTextureIsReadable", IsFreeFunction = true)]
        internal extern static void SetAtlasTextureIsReadable(Texture2D texture, bool isReadable);

        [NativeMethod(Name = "TextCore::FontEngineEditorUtilities::SetAtlasTextureSessionOnly", IsFreeFunction = true)]
        internal extern static void SetAtlasTextureSessionOnly(Texture2D texture, bool sessionOnly);

        [NativeMethod(Name = "TextCore::FontEngineEditorUtilities::IsAtlasTextureSessionOnly", IsFreeFunction = true)]
        internal extern static bool IsAtlasTextureSessionOnly(Texture2D texture);
    }
}
