// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    static class IconUtility
    {
        public static readonly Texture2D NoIcon = LoadBuiltInIconWithName("NoIconIcon.png", true);

        // Loads the icon at the specified iconPath. If the Pro Skin is being used, the 'd_' prefix naming convention is observed. If autoScale is true, the '@2x'/'@3x' postfix naming convention is observed.
        public static Texture2D LoadIconAtPath(string iconPath, bool autoScale = true)
        {
            return LoadIcon(iconPath, autoScale, (finalIconPath) =>
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(finalIconPath);
            });
        }

        // Loads the built-in icon with the specified iconName. If the Pro Skin is being used, the 'd_' prefix naming convention is observed. If autoScale is true, the '@2x'/'@3x' postfix naming convention is observed.
        public static Texture2D LoadBuiltInIconWithName(string iconName, bool autoScale = true)
        {
            return LoadIcon(iconName, autoScale, (finalIconPath) =>
            {
                return EditorGUIUtility.Load(finalIconPath) as Texture2D;
            });
        }

        static Texture2D LoadIcon(string iconPath, bool autoScale, Func<string, Texture2D> loadIcon)
        {
            if (string.IsNullOrEmpty(iconPath))
                return null;

            // Observe '@2x' postfix naming convention.
            float systemScale = EditorGUIUtility.pixelsPerPoint;
            if (autoScale && systemScale > 1f)
            {
                var iconScale = Mathf.RoundToInt(systemScale);
                var fileName = Path.GetFileNameWithoutExtension(iconPath);
                var fileExtension = Path.GetExtension(iconPath);
                var directoryName = Path.GetDirectoryName(iconPath);

                // Observe 'd_' prefix naming convention, and fallback to original icon if 'd_' variant is not found.
                var variantFileNames = new List<string>();
                if (EditorGUIUtility.isProSkin)
                    variantFileNames.Add("d_" + fileName);
                variantFileNames.Add(fileName);

                // Try to load the highest scale icon for the current screen resolution.
                while (iconScale > 1)
                {
                    foreach (var variantFileName in variantFileNames)
                    {
                        var scaledFileName = $"{variantFileName}@{iconScale}x{fileExtension}";
                        var scaledResourcePath = Path.Combine(directoryName, scaledFileName);
                        var icon = loadIcon.Invoke(scaledResourcePath);
                        if (icon != null)
                            return icon;
                    }

                    iconScale--;
                }
            }

            // Observe 'd_' prefix naming convention, and fallback to original icon if 'd_' variant is not found.
            if (EditorGUIUtility.isProSkin)
            {
                var proSkinFileName = "d_" + Path.GetFileName(iconPath);
                var directoryName = Path.GetDirectoryName(iconPath);
                var proSkinIconPath = Path.Combine(directoryName, proSkinFileName);
                var icon = loadIcon.Invoke(proSkinIconPath);
                if (icon != null)
                    return icon;
            }

            return loadIcon.Invoke(iconPath);
        }
    }
}
