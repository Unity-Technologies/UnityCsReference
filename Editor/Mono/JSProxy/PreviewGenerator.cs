// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace UnityEditor.Web
{
    internal class PreviewGenerator
    {
        const string kPreviewBuildFolder = "builds";

        static protected PreviewGenerator s_Instance = null;

        public static PreviewGenerator GetInstance()
        {
            if (s_Instance == null)
            {
                return new PreviewGenerator();
            }
            return s_Instance;
        }

        public byte[] GeneratePreview(string assetPath, int width, int height)
        {
            UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj == null)
                return null;

            Editor editor = Editor.CreateEditor(obj);
            if (editor == null)
                return null;

            Texture2D tex = editor.RenderStaticPreview(assetPath, null, width, height);
            if (tex == null)
            {
                UnityEngine.Object.DestroyImmediate(editor);
                return null;
            }

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(editor);
            return bytes;
        }
    }
}
