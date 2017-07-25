// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.IO;

namespace UnityEditor.StyleSheets
{
    internal class StyleSheetResourceUtil
    {
        internal static Object LoadResource(string pathName, System.Type type)
        {
            return LoadResource(pathName, type, GUIUtility.pixelsPerPoint > 1.0f);
        }

        internal static Object LoadResource(string pathName, System.Type type, bool lookForRetinaAssets)
        {
            Object resource = null;
            string hiResPath = string.Empty;

            lookForRetinaAssets &= (type == typeof(Texture2D));

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
            }

            if (resource == null)
            {
                resource = EditorGUIUtility.Load(pathName);
            }

            if (resource == null && lookForRetinaAssets)
            {
                resource = Resources.Load(hiResPath, type);
            }

            if (resource == null)
            {
                resource = Resources.Load(pathName, type);
            }

            if (resource == null && lookForRetinaAssets)
            {
                resource = AssetDatabase.LoadMainAssetAtPath(hiResPath);
            }

            if (resource == null)
            {
                resource = AssetDatabase.LoadMainAssetAtPath(pathName);
            }

            if (resource != null)
            {
                Debug.Assert(type.IsAssignableFrom(resource.GetType()), "Resource type mismatch");

                // Add to post processor for tracking asset moves/deletes.
                var absoluteAssetPath = AssetDatabase.GetAssetPath(resource);
                if (type != typeof(UnityEngine.StyleSheets.StyleSheet) && !absoluteAssetPath.StartsWith("Library"))
                {
                    StyleSheetAssetPostprocessor.AddReferencedAssetPath(absoluteAssetPath);
                }
            }

            return resource;
        }
    }
}
