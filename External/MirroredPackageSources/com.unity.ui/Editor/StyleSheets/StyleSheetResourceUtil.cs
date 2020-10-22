using UnityEngine;
using System.IO;

namespace UnityEditor.UIElements.StyleSheets
{
    internal class StyleSheetResourceUtil
    {
        public static Object LoadResource(string pathName, System.Type type)
        {
            return LoadResource(pathName, type, GUIUtility.pixelsPerPoint);
        }

        public static Object LoadResource(string pathName, System.Type type, float displayDpiScaling)
        {
            Object resource = null;
            string hiResPath = string.Empty;

            bool lookForRetinaAssets = (displayDpiScaling > 1.0f) && (type == typeof(Texture2D));
            bool assetIsRetinaTexture = false;
            if (lookForRetinaAssets)
            {
                string ext = Path.GetExtension(pathName);
                string fileRenamed = Path.GetFileNameWithoutExtension(pathName) + "@2x" + ext;
                hiResPath = Path.Combine(Path.GetDirectoryName(pathName), fileRenamed);
                lookForRetinaAssets = !string.IsNullOrEmpty(hiResPath);
            }

            if (lookForRetinaAssets)
            {
                resource = EditorGUIUtility.Load(hiResPath);
                assetIsRetinaTexture = (resource as Texture2D != null);
            }

            if (resource == null)
            {
                resource = EditorGUIUtility.Load(pathName);
            }

            if (resource == null && lookForRetinaAssets)
            {
                resource = Resources.Load(hiResPath, type);
                assetIsRetinaTexture = (resource as Texture2D != null);
            }

            if (type == typeof(Sprite))
            {
                // Special case for sprites, which are stored as Texture2D sub-assets
                var spriteResource = Resources.Load(Path.GetFileNameWithoutExtension(pathName), type);
                if (spriteResource != null)
                    resource = spriteResource;
            }

            if (resource == null)
            {
                resource = Resources.Load(pathName, type);
            }

            // This should be deprecated and removed.
            // Project asset paths should be resolved at import time.
            if (resource == null && lookForRetinaAssets)
            {
                resource = AssetDatabase.LoadMainAssetAtPath(hiResPath);
                assetIsRetinaTexture = (resource as Texture2D != null);
            }

            // This should be deprecated and removed.
            // Project asset paths should be resolved at import time.
            if (resource == null)
            {
                resource = AssetDatabase.LoadMainAssetAtPath(pathName);
            }

            Texture2D tex = resource as Texture2D;

            if (tex != null)
            {
                if (assetIsRetinaTexture)
                {
                    tex.pixelsPerPoint = 2.0f;
                }

                if (!Mathf.Approximately(displayDpiScaling % 1.0f, 0))
                {
                    tex.filterMode = FilterMode.Bilinear;
                }
            }

            return resource;
        }
    }
}
