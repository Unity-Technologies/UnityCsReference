// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    internal static class SnapshotUtils
    {
        public delegate void OnTextureReady(Texture2D texture);

        public static Texture2D TakeCameraSnapshot([NotNull] Camera camera)
        {
            var rect = new Rect(0, 0, camera.pixelWidth, camera.pixelHeight);
            var renderTexture = new RenderTexture((int)rect.width, (int)rect.height, 24);
            var snapshotTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            var oldCameraRenderTexture = camera.targetTexture;
            camera.targetTexture = renderTexture;
            camera.Render();

            var old = RenderTexture.active;
            RenderTexture.active = renderTexture;
            snapshotTexture.ReadPixels(rect, 0, 0);
            RenderTexture.active = old;
            camera.targetTexture = oldCameraRenderTexture;

            // Don't forget to apply so that all operations are done.
            snapshotTexture.Apply();

            snapshotTexture.Compress(false);

            return snapshotTexture;
        }

        public static void TakeSceneViewSnapshot([NotNull] SceneView sceneView, OnTextureReady onTextureReadyCallback)
        {
            // Focus the sceneView and wait until it has fully focused
            sceneView.Focus();

            void WaitForFocus()
            {
                if (!sceneView.hasFocus)
                {
                    EditorApplication.delayCall += WaitForFocus;
                    return;
                }

                // Prepare the sceneView region the
                const int tabHeight = 19; // Taken from DockArea, which is internal, and the value is also internal.
                var cameraRect = sceneView.cameraRect;
                var offsetPosition = sceneView.position.position + cameraRect.position + new Vector2(0, tabHeight);
                var region = new Rect(offsetPosition, cameraRect.size);

                // Take the snapshot
                var texture = TakeScreenSnapshot(region);

                // Execute callback
                onTextureReadyCallback(texture);
            }

            EditorApplication.delayCall += WaitForFocus;
        }

        public static Texture2D TakeScreenSnapshot(Rect region)
        {
            var colors = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(region.position, (int)region.width, (int)region.height);
            var snapshotTexture = new Texture2D((int)region.width, (int)region.height, TextureFormat.RGB24, false);
            snapshotTexture.SetPixels(colors);
            snapshotTexture.Apply();

            snapshotTexture.Compress(false);

            return snapshotTexture;
        }

        public static void TakeGameViewSnapshot([NotNull] EditorWindow gameView, OnTextureReady onTextureReadyCallback)
        {
            // Focus the game view (there is no need to wait for focus here
            // as the snapshot won't happen until there is a render
            gameView.Focus();

            var textureAssetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/game-view-texture.png");
            ScreenCapture.CaptureScreenshot(textureAssetPath);

            void WaitForSnapshotReady()
            {
                // Wait if the file is not ready
                if (!File.Exists(textureAssetPath))
                {
                    EditorApplication.delayCall += WaitForSnapshotReady;
                    return;
                }

                // Import the texture a first time
                AssetDatabase.ImportAsset(textureAssetPath, ImportAssetOptions.ForceSynchronousImport);

                // Then get the importer for the texture
                var textureImporter = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;
                if (textureImporter == null)
                    return;

                // Set it readable
                var oldIsReadable = textureImporter.isReadable;
                textureImporter.isReadable = true;

                // Re-import it again, then load it
                AssetDatabase.ImportAsset(textureAssetPath, ImportAssetOptions.ForceSynchronousImport);
                var textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
                textureImporter.isReadable = oldIsReadable;
                if (!textureAsset)
                {
                    Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "Texture asset unavailable.");
                    return;
                }

                // Copy the texture since we are going to delete the asset
                var textureCopy = new Texture2D(textureAsset.width, textureAsset.height);
                EditorUtility.CopySerialized(textureAsset, textureCopy);

                // Delete the original texture asset
                AssetDatabase.DeleteAsset(textureAssetPath);

                onTextureReadyCallback(textureCopy);
            }

            EditorApplication.delayCall += WaitForSnapshotReady;
        }
    }
}
