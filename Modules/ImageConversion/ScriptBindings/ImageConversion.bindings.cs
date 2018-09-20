// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/ImageConversion/ScriptBindings/ImageConversion.bindings.h")]
    public static class ImageConversion
    {
        [NativeMethod(Name = "ImageConversionBindings::EncodeToTGA", IsFreeFunction = true, ThrowsException = true)]
        extern public static byte[] EncodeToTGA(this Texture2D tex);

        [NativeMethod(Name = "ImageConversionBindings::EncodeToPNG", IsFreeFunction = true, ThrowsException = true)]
        extern public static byte[] EncodeToPNG(this Texture2D tex);

        [NativeMethod(Name = "ImageConversionBindings::EncodeToJPG", IsFreeFunction = true, ThrowsException = true)]
        extern public static byte[] EncodeToJPG(this Texture2D tex, int quality);
        public static byte[] EncodeToJPG(this Texture2D tex)
        {
            return tex.EncodeToJPG(75);
        }

        [NativeMethod(Name = "ImageConversionBindings::EncodeToEXR", IsFreeFunction = true, ThrowsException = true)]
        extern public static byte[] EncodeToEXR(this Texture2D tex, Texture2D.EXRFlags flags);
        public static byte[] EncodeToEXR(this Texture2D tex)
        {
            return EncodeToEXR(tex, Texture2D.EXRFlags.None);
        }

        [NativeMethod(Name = "ImageConversionBindings::LoadImage", IsFreeFunction = true)]
        extern public static bool LoadImage([NotNull] this Texture2D tex, byte[] data, bool markNonReadable);
        public static bool LoadImage(this Texture2D tex, byte[] data)
        {
            return LoadImage(tex, data, false);
        }
    }
}
