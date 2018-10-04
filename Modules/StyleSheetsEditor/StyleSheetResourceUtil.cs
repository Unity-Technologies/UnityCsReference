// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.IO;

namespace UnityEditor.StyleSheets
{
    internal class StyleSheetResourceUtil
    {
        public static Object LoadResource(string pathName, System.Type type)
        {
            return LoadResource(pathName, type, GUIUtility.pixelsPerPoint > 1.0f);
        }

        public static Object LoadResource(string pathName, System.Type type, bool lookForRetinaAssets)
        {
            Object resource = null;
            string hiResPath = string.Empty;

            lookForRetinaAssets &= (type == typeof(Texture2D));
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

            if (resource != null)
            {
                Debug.Assert(type.IsAssignableFrom(resource.GetType()), "Resource type mismatch");
            }

            if (assetIsRetinaTexture)
            {
                Texture2D tex = (Texture2D)resource;
                tex.pixelsPerPoint = 2.0f;
            }

            return resource;
        }
    }
}
