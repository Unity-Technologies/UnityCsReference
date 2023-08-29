// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal static class AssetPreviewUpdater
    {
        static readonly Type[] s_InspectorsRequireFullPipelineInitialization = new[] { typeof(MaterialEditor), typeof(ModelInspector), typeof(GameObjectInspector) };

        [RequiredByNativeCode]
        public static Texture2D CreatePreviewForAsset(Object obj, Object[] subAssets, string assetPath)
        {
            return CreatePreview(obj, subAssets, assetPath, 128, 128);
        }

        // Generate a preview texture for an asset
        public static Texture2D CreatePreview(Object obj, Object[] subAssets, string assetPath, int width, int height)
        {
            var type = CustomEditorAttributes.FindCustomEditorType(obj, false);
            if (type == null)
                return null;

            var info = type.GetMethod("RenderStaticPreview");
            if (info == null)
            {
                Debug.LogError("Fail to find RenderStaticPreview base method");
                return null;
            }

            if (info.DeclaringType == typeof(Editor))
                return null;


            var editor = Editor.CreateEditor(obj);

            if (editor == null)
                return null;

            //Check that Render Pipeline is ready
            var pipelineWasNotInitialized = !RenderPipelineManager.pipelineSwitchCompleted;

            //We keep this call to initialize Render Pipeline when Render Pipeline is not ready
            var tex = editor.RenderStaticPreview(assetPath, subAssets, width, height);

            //If Render Pipeline was not created before we will ignore the results and mark it dirty.
            if (pipelineWasNotInitialized)
            {
                for (int i = 0; i < s_InspectorsRequireFullPipelineInitialization.Length; i++)
                {
                    if (type != s_InspectorsRequireFullPipelineInitialization[i])
                        continue;
                    EditorUtility.SetDirty(obj);
                    Object.DestroyImmediate(editor);
                    return null;
                }
            }

            // For debugging we write the preview to a file (keep)
            //{
            //  var bytes = tex.EncodeToPNG();
            //  string previewFilePath = string.Format ("{0}/../SavedPreview{1}.png", Application.dataPath, (int)(EditorApplication.timeSinceStartup*1000));
            //  System.IO.File.WriteAllBytes(previewFilePath, bytes);
            //  Debug.Log ("Wrote preview file to: " +previewFilePath);
            //}

            Object.DestroyImmediate(editor);

            return tex;
        }
    }
}
