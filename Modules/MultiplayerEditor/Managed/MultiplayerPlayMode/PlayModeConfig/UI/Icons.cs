// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Helper class to load Icons that will get assigned via code
    ///
    /// Code parts taken from MPPM Images.cs
    /// </summary>

    static class Icons
    {
        internal enum ImageName
        {
            Loading,
            Error,
            CompletedTask,
            PlayModeScenario,
            Warning,
            UnityLogo,
            Idle,
            Help,
            Drift,
        }

        /// <summary>
        /// Used to have a central place to use in editor shipped icons, so if a
        /// icon that exists in the editor is used in many places add it here, so
        /// if we have to change it in the future we only have one spot to change.
        /// </summary>
        static readonly Dictionary<ImageName, string> k_InternalIcons = new()
        {
            { ImageName.Warning, "console.warnicon" },
            { ImageName.UnityLogo, "UnityLogo" },
            { ImageName.Help, "_Help" },
            { ImageName.Drift, "console.warnicon.inactive.sml" }
        };


        static readonly Dictionary<string, Texture2D> CachedImagesByPath = new();

        internal static Texture2D GetImage(ImageName imageName)
        {
            // Try to return a internal icon first.
            if (k_InternalIcons.TryGetValue(imageName, out var iconName))
            {
                return EditorGUIUtility.FindTexture(iconName);
            }

            var imageFileName2X = imageName + "@2x.png";
            var darkImageFileName2X = $"d_{imageFileName2X}";
            var imageFileRelativePath2X = Path.Combine("Multiplayer", "Icons", imageFileName2X);
            var darkImageFileRelativePath2X = Path.Combine("Multiplayer", "Icons", darkImageFileName2X);

            Texture2D result = null;

            if (EditorGUIUtility.isProSkin)
                result = EditorGUIUtility.LoadIcon(darkImageFileRelativePath2X);

            if (result != null)
                return result;

            result = EditorGUIUtility.LoadIcon(imageFileRelativePath2X);

            if (result != null)
                return result;

            return null; // If we can't find the image it is a developer issue or the package got altered. Please address it!
        }
    }
}
