// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.U2D;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;
using Object = UnityEngine.Object;
using UnityTexture2D = UnityEngine.Texture2D;

namespace UnityEditor
{
    internal static class SpriteUtility
    {
        static class SpriteUtilityStrings
        {
            public static readonly GUIContent saveAnimDialogMessage = EditorGUIUtility.TextContent("Create a new animation for the game object '{0}':");
            public static readonly GUIContent saveAnimDialogTitle = EditorGUIUtility.TextContent("Create New Animation");
            public static readonly GUIContent saveAnimDialogName = EditorGUIUtility.TextContent("New Animation");
            public static readonly GUIContent unableToFindSpriteRendererWarning = EditorGUIUtility.TextContent("There should be a SpriteRenderer in dragged object");
            public static readonly GUIContent unableToAddSpriteRendererWarning = EditorGUIUtility.TextContent("Unable to add SpriteRenderer into Gameobject.");
            public static readonly GUIContent failedToCreateAnimationError = EditorGUIUtility.TextContent("Failed to create animation for dragged object");
        }

        private static Material s_PreviewSpriteDefaultMaterial;

        internal static Material previewSpriteDefaultMaterial
        {
            get
            {
                if (s_PreviewSpriteDefaultMaterial == null)
                {
                    Shader shader = Shader.Find("Sprites/Default");
                    s_PreviewSpriteDefaultMaterial = new Material(shader);
                }
                return s_PreviewSpriteDefaultMaterial;
            }
        }

        static List<Object> s_SceneDragObjects;
        static DragType s_DragType;
        enum DragType { NotInitialized, SpriteAnimation, CreateMultiple }

        public delegate string ShowFileDialogDelegate(string title, string defaultName, string extension, string message, string defaultPath);
        public static void OnSceneDrag(SceneView sceneView)
        {
            HandleSpriteSceneDrag(sceneView, new UnityEngine.U2D.Interface.Event(), DragAndDrop.objectReferences, DragAndDrop.paths, EditorUtility.SaveFilePanelInProject);
        }

        public static void HandleSpriteSceneDrag(SceneView sceneView, IEvent evt, Object[] objectReferences, string[] paths, ShowFileDialogDelegate saveFileDialog)
        {
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform && evt.type != EventType.DragExited)
                return;

            // Return if any of the dragged objects are null, e.g. a MonoBehaviour without a managed instance
            if (objectReferences.Any(obj => obj == null))
                return;

            // Regardless of EditorBehaviorMode or SceneView mode we don't handle if texture is dragged over a GO with renderer
            if (objectReferences.Length == 1 && objectReferences[0] as UnityTexture2D != null)
            {
                GameObject go = HandleUtility.PickGameObject(evt.mousePosition, true);
                if (go != null)
                {
                    var renderer = go.GetComponent<Renderer>();
                    if (renderer != null && !(renderer is SpriteRenderer))
                    {
                        // There is an object where the cursor is
                        // and we are dragging a texture. Most likely user wants to
                        // assign texture to the GO
                        // Case 730444: Proceed only if the go has a renderer
                        CleanUp(true);
                        return;
                    }
                }
            }

            switch (evt.type)
            {
                case (EventType.DragUpdated):
                    DragType newDragType = evt.alt ? DragType.CreateMultiple : DragType.SpriteAnimation;

                    if (s_DragType != newDragType || s_SceneDragObjects == null) // Either this is first time we are here OR evt.alt changed during drag
                    {
                        if (!ExistingAssets(objectReferences) && PathsAreValidTextures(paths)) // External drag with images that are not in the project
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            s_SceneDragObjects = new List<Object>();
                            s_DragType = newDragType;
                        }
                        else // Internal drag with assets from project
                        {
                            List<Sprite> assets = GetSpriteFromPathsOrObjects(objectReferences, paths, evt.type);

                            if (assets.Count == 0)
                                return;

                            if (s_DragType != DragType.NotInitialized) // evt.alt changed during drag, so we need to cleanup and start over
                                CleanUp(true);

                            s_DragType = newDragType;
                            CreateSceneDragObjects(assets);
                            IgnoreForRaycasts(s_SceneDragObjects);
                        }
                    }

                    PositionSceneDragObjects(s_SceneDragObjects, sceneView, evt.mousePosition);

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;
                case (EventType.DragPerform):
                    List<Sprite> sprites = GetSpriteFromPathsOrObjects(objectReferences, paths, evt.type);

                    if (sprites.Count > 0 && s_SceneDragObjects != null)
                    {
                        // Store current undoIndex to undo all operations done if any part of sprite creation fails
                        int undoIndex = Undo.GetCurrentGroup();

                        // For external drags, we have delayed all creation to DragPerform because only now we have the imported sprite assets
                        if (s_SceneDragObjects.Count == 0)
                        {
                            CreateSceneDragObjects(sprites);
                            PositionSceneDragObjects(s_SceneDragObjects, sceneView, evt.mousePosition);
                        }

                        foreach (GameObject dragGO in s_SceneDragObjects)
                        {
                            dragGO.hideFlags = HideFlags.None;
                            Undo.RegisterCreatedObjectUndo(dragGO, "Create Sprite");
                            EditorUtility.SetDirty(dragGO);
                        }

                        bool createGameObject = true;
                        if (s_DragType == DragType.SpriteAnimation && sprites.Count > 1)
                        {
                            UsabilityAnalytics.Event("Sprite Drag and Drop", "Drop multiple sprites to scene", "null", 1);
                            createGameObject = AddAnimationToGO((GameObject)s_SceneDragObjects[0], sprites.ToArray(), saveFileDialog);
                        }
                        else
                            UsabilityAnalytics.Event("Sprite Drag and Drop", "Drop single sprite to scene", "null", 1);

                        if (createGameObject)
                        {
                            Selection.objects = s_SceneDragObjects.ToArray();
                        }
                        else
                        {
                            // Revert all Create Sprite actions if animation failed to be created or was cancelled
                            Undo.RevertAllDownToGroup(undoIndex);
                        }
                        CleanUp(!createGameObject);
                        evt.Use();
                    }
                    break;
                case EventType.DragExited:
                    if (s_SceneDragObjects != null)
                    {
                        CleanUp(true);
                        evt.Use();
                    }
                    break;
            }
        }

        private static void IgnoreForRaycasts(List<Object> objects)
        {
            List<Transform> ignoredTransforms = new List<Transform>();

            foreach (GameObject gameObject in objects)
                ignoredTransforms.AddRange(gameObject.GetComponentsInChildren<Transform>());

            HandleUtility.ignoreRaySnapObjects = ignoredTransforms.ToArray();
        }

        private static void PositionSceneDragObjects(List<Object> objects, SceneView sceneView, Vector2 mousePosition)
        {
            Vector3 position = Vector3.zero;
            position = HandleUtility.GUIPointToWorldRay(mousePosition).GetPoint(10);
            if (sceneView.in2DMode)
            {
                position.z = 0f;
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(mousePosition));
                if (hit != null)
                {
                    RaycastHit rh = (RaycastHit)hit;
                    position = rh.point;
                }
            }

            if (Selection.activeGameObject != null)
            {
                Grid grid = Selection.activeGameObject.GetComponentInParent<Grid>();
                if (grid != null)
                {
                    Vector3Int cell = grid.WorldToCell(position);
                    position = grid.GetCellCenterWorld(cell);
                }
            }

            foreach (GameObject gameObject in objects)
            {
                gameObject.transform.position = position;
            }
        }

        private static void CreateSceneDragObjects(List<Sprite> sprites)
        {
            if (s_SceneDragObjects == null)
                s_SceneDragObjects = new List<Object>();

            if (s_DragType == DragType.CreateMultiple)
            {
                foreach (Sprite sprite in sprites)
                    s_SceneDragObjects.Add(CreateDragGO(sprite, Vector3.zero));
            }
            else
            {
                s_SceneDragObjects.Add(CreateDragGO(sprites[0], Vector3.zero));
            }
        }

        private static void CleanUp(bool deleteTempSceneObject)
        {
            if (s_SceneDragObjects != null)
            {
                if (deleteTempSceneObject)
                {
                    foreach (GameObject gameObject in s_SceneDragObjects)
                        Object.DestroyImmediate(gameObject, false);
                }

                s_SceneDragObjects.Clear();
                s_SceneDragObjects = null;
            }
            HandleUtility.ignoreRaySnapObjects = null;
            s_DragType = DragType.NotInitialized;
        }

        static bool CreateAnimation(GameObject gameObject, Object[] frames, ShowFileDialogDelegate saveFileDialog)
        {
            saveFileDialog = saveFileDialog ?? EditorUtility.SaveFilePanelInProject;

            // Use same name compare as when we sort in the backend: See AssetDatabase.cpp: SortChildren
            System.Array.Sort(frames, (a, b) => EditorUtility.NaturalCompare(a.name, b.name));

            Animator animator = AnimationWindowUtility.EnsureActiveAnimationPlayer(gameObject)
                ? AnimationWindowUtility.GetClosestAnimatorInParents(gameObject.transform)
                : null;

            bool createSuccess = animator != null;

            if (animator != null)
            {
                // Go forward with presenting user a save clip dialog
                string message = string.Format(SpriteUtilityStrings.saveAnimDialogMessage.text, gameObject.name);
                string newClipDirectory = ProjectWindowUtil.GetActiveFolderPath();
                string newClipPath = saveFileDialog(SpriteUtilityStrings.saveAnimDialogTitle.text, SpriteUtilityStrings.saveAnimDialogName.text, "anim", message, newClipDirectory);

                if (string.IsNullOrEmpty(newClipPath))
                {
                    Object.DestroyImmediate(animator);
                    return false;
                }
                else
                {
                    AnimationClip newClip = AnimationWindowUtility.CreateNewClipAtPath(newClipPath);
                    if (newClip != null)
                    {
                        AddSpriteAnimationToClip(newClip, frames);
                        createSuccess = AnimationWindowUtility.AddClipToAnimatorComponent(animator, newClip);
                    }
                }
            }

            if (createSuccess == false)
                Debug.LogError(SpriteUtilityStrings.failedToCreateAnimationError.text);

            return createSuccess;
        }

        static void AddSpriteAnimationToClip(AnimationClip newClip, Object[] frames)
        {
            // TODO Default framerate be exposed to user?
            newClip.frameRate = 12;

            // Add keyframes
            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length];

            for (int i = 0; i < keyframes.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe();
                keyframes[i].value = RemapObjectToSprite(frames[i]);
                keyframes[i].time = i / newClip.frameRate;
            }

            // Create binding
            EditorCurveBinding curveBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

            // Save curve to clip
            AnimationUtility.SetObjectReferenceCurve(newClip, (EditorCurveBinding)curveBinding, keyframes);
        }

        public static List<Sprite> GetSpriteFromPathsOrObjects(Object[] objects, string[] paths, EventType currentEventType)
        {
            List<Sprite> result = new List<Sprite>();

            foreach (Object obj in objects)
            {
                if (AssetDatabase.Contains(obj))
                {
                    if (obj is Sprite)
                        result.Add(obj as Sprite);
                    else if (obj is UnityTexture2D)
                        result.AddRange(TextureToSprites(obj as UnityTexture2D));
                }
            }

            // Fix case 742896. If any of the drag objects is already in the AssetDatabase, means we don't have to handle external drags.
            // Fix case 857231. We only handle external drag if default behaviour mode is 2D
            if (!ExistingAssets(objects) && currentEventType == EventType.DragPerform && EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
            {
                HandleExternalDrag(paths, true, ref result);
            }
            return result;
        }

        public static bool ExistingAssets(Object[] objects)
        {
            foreach (Object obj in objects)
            {
                if (AssetDatabase.Contains(obj))
                    return true;
            }
            return false;
        }

        private static void HandleExternalDrag(string[] paths, bool perform, ref List<Sprite> result)
        {
            foreach (var path in paths)
            {
                if (!ValidPathForTextureAsset(path))
                    continue;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (!perform)
                    continue;

                var newPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine("Assets", FileUtil.GetLastPathNameComponent(path)));
                if (newPath.Length <= 0)
                    continue;

                FileUtil.CopyFileOrDirectory(path, newPath);
                ForcedImportFor(newPath);

                Sprite defaultSprite = GenerateDefaultSprite(AssetDatabase.LoadMainAssetAtPath(newPath) as UnityTexture2D);
                if (defaultSprite != null)
                    result.Add(defaultSprite);
            }
        }

        private static bool PathsAreValidTextures(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return false;

            foreach (var path in paths)
            {
                if (!ValidPathForTextureAsset(path))
                    return false;
            }

            return true;
        }

        private static void ForcedImportFor(string newPath)
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                AssetDatabase.ImportAsset(newPath);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static Sprite GenerateDefaultSprite(UnityTexture2D texture)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null) // could be DDS importer or other non-TextureImporter type
                return null;

            if (textureImporter.textureType != TextureImporterType.Sprite)
                return null;

            if (textureImporter.spriteImportMode == SpriteImportMode.None)
            {
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                ForcedImportFor(assetPath);
            }

            Object firstSprite = null;
            firstSprite = AssetDatabase.LoadAllAssetsAtPath(assetPath).FirstOrDefault(t => t is Sprite);

            return firstSprite as Sprite;
        }

        public static GameObject CreateDragGO(Sprite frame, Vector3 position)
        {
            string name = string.IsNullOrEmpty(frame.name) ? "Sprite" : frame.name;
            name = GameObjectUtility.GetUniqueNameForSibling(null, name);
            GameObject go = new GameObject(name);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = frame;
            go.transform.position = position;
            go.hideFlags = HideFlags.HideAndDontSave;
            return go;
        }

        public static bool AddAnimationToGO(GameObject go, Sprite[] frames, ShowFileDialogDelegate saveFileDialog)
        {
            SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning(SpriteUtilityStrings.unableToFindSpriteRendererWarning.text);
                spriteRenderer = go.AddComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    Debug.LogWarning(SpriteUtilityStrings.unableToAddSpriteRendererWarning.text);
                    return false;
                }
            }
            spriteRenderer.sprite = frames[0];
            return CreateAnimation(go, frames, saveFileDialog);
        }

        public static GameObject DropSpriteToSceneToCreateGO(Sprite sprite, Vector3 position)
        {
            GameObject go = new GameObject(string.IsNullOrEmpty(sprite.name) ? "Sprite" : sprite.name);
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            go.transform.position = position;
            Selection.activeObject = go;

            return go;
        }

        public static Sprite RemapObjectToSprite(Object obj)
        {
            if (obj is Sprite)
                return (Sprite)obj;

            if (obj is UnityTexture2D)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj));
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i].GetType() == typeof(Sprite))
                        return assets[i] as Sprite;
                }
            }
            return null;
        }

        public static List<Sprite> TextureToSprites(UnityTexture2D tex)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(tex));
            List<Sprite> result = new List<Sprite>();

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i].GetType() == typeof(Sprite))
                    result.Add(assets[i] as Sprite);
            }

            if (result.Count > 0)
                return result;

            Sprite defaultSprite = GenerateDefaultSprite(tex);
            if (defaultSprite != null)
                result.Add(defaultSprite);

            return result;
        }

        public static Sprite TextureToSprite(UnityTexture2D tex)
        {
            List<Sprite> sprites = TextureToSprites(tex);
            if (sprites.Count > 0)
                return sprites[0];
            return null;
        }

        private static bool ValidPathForTextureAsset(string path)
        {
            string ext = FileUtil.GetPathExtension(path).ToLower();
            return
                ext == "jpg" ||
                ext == "jpeg" ||
                ext == "tif" ||
                ext == "tiff" ||
                ext == "tga" ||
                ext == "gif" ||
                ext == "png" ||
                ext == "psd" ||
                ext == "bmp" ||
                ext == "iff" ||
                ext == "pict" ||
                ext == "pic" ||
                ext == "pct" ||
                ext == "exr" ||
                ext == "hdr";
        }

        public static UnityTexture2D RenderStaticPreview(Sprite sprite, Color color, int width, int height)
        {
            return RenderStaticPreview(sprite, color, width, height, Matrix4x4.identity);
        }

        public static UnityTexture2D RenderStaticPreview(Sprite sprite, Color color, int width, int height, Matrix4x4 transform)
        {
            if (sprite == null)
                return null;

            PreviewHelpers.AdjustWidthAndHeightForStaticPreview((int)sprite.rect.width, (int)sprite.rect.height, ref width, ref height);

            SavedRenderTargetState savedRTState = new SavedRenderTargetState();

            RenderTexture tmp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            RenderTexture.active = tmp;
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GL.Clear(true, true, new Color(0f, 0f, 0f, 0.1f));

            previewSpriteDefaultMaterial.mainTexture = sprite.texture;
            previewSpriteDefaultMaterial.SetPass(0);

            RenderSpriteImmediate(sprite, color, transform);

            GL.sRGBWrite = false;
            UnityTexture2D copy = new UnityTexture2D(width, height, TextureFormat.ARGB32, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);

            savedRTState.Restore();
            return copy;
        }

        internal static void RenderSpriteImmediate(Sprite sprite, Color color, Matrix4x4 transform)
        {
            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;

            float pixelsToUnits = sprite.rect.width / sprite.bounds.size.x;
            Vector2[] vertices = sprite.vertices;
            Vector2[] uvs = sprite.uv;
            ushort[] triangles = sprite.triangles;
            Vector2 pivot = sprite.pivot;

            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < sprite.triangles.Length; ++i)
            {
                ushort index = triangles[i];
                Vector2 spriteVertex = vertices[index];
                Vector2 uv = uvs[index];
                Vector3 vertex = new Vector3(spriteVertex.x, spriteVertex.y, 0);
                vertex = transform.MultiplyPoint(vertex);
                vertex.x = (vertex.x * pixelsToUnits + pivot.x) / spriteWidth;
                vertex.y = (vertex.y * pixelsToUnits + pivot.y) / spriteHeight;
                GL.Color(color);
                GL.TexCoord(new Vector3(uv.x, uv.y, 0));
                GL.Vertex3(vertex.x, vertex.y, vertex.z);
            }
            GL.End();
            GL.PopMatrix();
            GL.sRGBWrite = false;
        }

        public static UnityTexture2D CreateTemporaryDuplicate(UnityTexture2D original, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !original)
                return null;

            RenderTexture save = RenderTexture.active;
            var savedViewport = ShaderUtil.rawViewportRect;

            bool sRGB = !TextureUtil.GetLinearSampled(original);
            RenderTexture tmp = RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.Default,
                    sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);

            GL.sRGBWrite = (sRGB && QualitySettings.activeColorSpace == ColorSpace.Linear);
            Graphics.Blit(original, tmp);
            GL.sRGBWrite = false;

            RenderTexture.active = tmp;

            // If the user system doesn't support this texture size, force it to use mipmap
            bool forceUseMipMap = width >= SystemInfo.maxTextureSize || height >= SystemInfo.maxTextureSize;

            UnityTexture2D copy = new UnityTexture2D(width, height, TextureFormat.RGBA32, original.mipmapCount > 1 || forceUseMipMap);
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);

            EditorGUIUtility.SetRenderTextureNoViewport(save);
            ShaderUtil.rawViewportRect = savedViewport;

            copy.alphaIsTransparency = original.alphaIsTransparency;
            return copy;
        }

        public static SpriteImportMode GetSpriteImportMode(ISpriteEditorDataProvider dataProvider)
        {
            return dataProvider == null ? SpriteImportMode.None : dataProvider.spriteImportMode;
        }
    }
}
