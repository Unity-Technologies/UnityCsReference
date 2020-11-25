// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.Search
{
    internal static class Icons
    {
        public static string iconFolder = $"{Utils.packageFolderName}/Editor/Icons";
        public static Texture2D shortcut = LoadIcon($"{iconFolder}/shortcut.png");
        public static Texture2D staticAPI = EditorGUIUtility.FindTexture("cs Script Icon");
        public static Texture2D quicksearch = LoadIcon($"{iconFolder}/quicksearch.png");
        public static Texture2D searchQuery = LoadIcon($"{iconFolder}/search_query.png");
        public static Texture2D filter = LoadIcon($"{iconFolder}/filter.png");
        public static Texture2D settings = LoadIcon($"{iconFolder}/settings.png");
        public static Texture2D favorite = EditorGUIUtility.FindTexture("Favorite");
        public static Texture2D folder = EditorGUIUtility.FindTexture("FolderOpened Icon");
        public static Texture2D help = EditorGUIUtility.FindTexture($"Icons/_Help.png");
        public static Texture2D search = LoadIcon($"{iconFolder}/search.png");
        public static Texture2D clear = LoadIcon($"{iconFolder}/clear.png");
        public static Texture2D more = LoadIcon($"{iconFolder}/more.png");
        public static Texture2D store = LoadIcon($"{iconFolder}/store.png");
        public static Texture2D logInfo = LoadIcon($"{iconFolder}/log_info.png");
        public static Texture2D logWarning = LoadIcon($"{iconFolder}/log_warning.png");
        public static Texture2D logError = LoadIcon($"{iconFolder}/log_error.png");
        public static Texture2D packageInstalled = LoadIcon($"{iconFolder}/package_installed.png");
        public static Texture2D packageUpdate = LoadIcon($"{iconFolder}/package_update.png");

        static Icons()
        {
            if (EditorGUIUtility.isProSkin)
            {
                shortcut = LightenTexture(shortcut);
                quicksearch = LightenTexture(quicksearch);
                filter = LightenTexture(filter);
                settings = LightenTexture(settings);
                search = LightenTexture(search);
                clear = LightenTexture(clear);
                more = LightenTexture(more);
                store = LightenTexture(store);
                packageInstalled = LightenTexture(packageInstalled);
                packageUpdate = LightenTexture(packageUpdate);
            }
        }

        private static Texture2D LoadIcon(string resourcePath, bool autoScale = false)
        {
            if (String.IsNullOrEmpty(resourcePath))
                return null;

            float systemScale = EditorGUIUtility.pixelsPerPoint;
            if (autoScale && systemScale > 1f)
            {
                int scale = Mathf.RoundToInt(systemScale);
                string dirName = Path.GetDirectoryName(resourcePath).Replace('\\', '/');
                string fileName = Path.GetFileNameWithoutExtension(resourcePath);
                string fileExt = Path.GetExtension(resourcePath);
                for (int s = scale; scale > 1; --scale)
                {
                    string scaledResourcePath = $"{dirName}/{fileName}@{s}x{fileExt}";
                    var scaledResource = EditorResources.Load<Texture2D>(scaledResourcePath, false);
                    if (scaledResource)
                        return scaledResource;
                }
            }

            return EditorResources.Load<Texture2D>(resourcePath, false);
        }

        private static Texture2D LightenTexture(Texture2D texture)
        {
            if (!texture)
                return texture;
            Texture2D outTexture = new Texture2D(texture.width, texture.height);
            var outColorArray = outTexture.GetPixels();

            var colorArray = texture.GetPixels();
            for (var i = 0; i < colorArray.Length; ++i)
                outColorArray[i] = LightenColor(colorArray[i]);

            outTexture.hideFlags = HideFlags.HideAndDontSave;
            outTexture.SetPixels(outColorArray);
            outTexture.Apply();

            return outTexture;
        }

        private static Color LightenColor(Color color)
        {
            Color.RGBToHSV(color, out var h, out _, out _);
            var outColor = Color.HSVToRGB((h + 0.5f) % 1, 0f, 0.8f);
            outColor.a = color.a;
            return outColor;
        }

    }
}
