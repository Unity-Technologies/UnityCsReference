// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class Images
    {
        internal enum ImageName
        {
            Loading,
            MainEditorIcon,
            CloneEditorIcon,
            Settings,
        }

        static readonly Dictionary<string, Texture2D> CachedImagesByPath = new Dictionary<string, Texture2D>();

        internal static Texture2D GetImage(ImageName imageName)
        {
            var imageFileName2X = imageName + "@2x.png";
            var darkImageFileName2X = $"dark_{imageFileName2X}";
            var imageFileRelativePath2X = Path.Combine(UXMLPaths.IconsRoot, imageFileName2X);
            var darkImageFileRelativePath2X = Path.Combine(UXMLPaths.IconsRoot, darkImageFileName2X);

            Texture2D result = null;

            if (EditorGUIUtility.isProSkin)
                result = EditorGUIUtility.LoadIcon(darkImageFileRelativePath2X);

            if (result != null)
                return result;

            result = EditorGUIUtility.LoadIcon(imageFileRelativePath2X);

            if (result != null)
                return result;

            MppmLog.Error($"Image not found: '{imageFileRelativePath2X};");
            return null;    // If we can't find the image it is a developer issue or the package got altered. Please address it!
        }
    }
}
