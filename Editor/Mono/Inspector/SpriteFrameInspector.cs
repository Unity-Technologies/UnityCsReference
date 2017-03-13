// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    internal class SpriteInspector : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent[] spriteAlignmentOptions =
            {
                EditorGUIUtility.TextContent("Center"),
                EditorGUIUtility.TextContent("Top Left"),
                EditorGUIUtility.TextContent("Top"),
                EditorGUIUtility.TextContent("Top Right"),
                EditorGUIUtility.TextContent("Left"),
                EditorGUIUtility.TextContent("Right"),
                EditorGUIUtility.TextContent("Bottom Left"),
                EditorGUIUtility.TextContent("Bottom"),
                EditorGUIUtility.TextContent("Bottom Right"),
                EditorGUIUtility.TextContent("Custom"),
            };

            public static readonly GUIContent spriteAlignment = EditorGUIUtility.TextContent("Pivot|Sprite pivot point in its localspace. May be used for syncing animation frames of different sizes.");
        }

        private Sprite sprite
        {
            get { return target as Sprite; }
        }

        private SpriteMetaData GetMetaData(string name)
        {
            string path = AssetDatabase.GetAssetPath(sprite);
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter != null)
            {
                if (textureImporter.spriteImportMode == SpriteImportMode.Single)
                {
                    return GetMetaDataInSingleMode(name, textureImporter);
                }
                else
                {
                    return GetMetaDataInMultipleMode(name, textureImporter);
                }
            }

            return new SpriteMetaData();
        }

        private static SpriteMetaData GetMetaDataInMultipleMode(string name, TextureImporter textureImporter)
        {
            SpriteMetaData[] spritesheet = textureImporter.spritesheet;
            for (int i = 0; i < spritesheet.Length; i++)
            {
                if (spritesheet[i].name.Equals(name))
                {
                    return spritesheet[i];
                }
            }
            return new SpriteMetaData();
        }

        private static SpriteMetaData GetMetaDataInSingleMode(string name, TextureImporter textureImporter)
        {
            SpriteMetaData metaData = new SpriteMetaData();
            metaData.border = textureImporter.spriteBorder;
            metaData.name = name;
            metaData.pivot = textureImporter.spritePivot;
            metaData.rect = new Rect(0, 0, 1, 1);
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            metaData.alignment = textureImporterSettings.spriteAlignment;
            return metaData;
        }

        public override void OnInspectorGUI()
        {
            bool nameM;
            bool alignM;
            bool borderM;
            UnifiedValues(out nameM, out alignM, out borderM);

            if (nameM)
                EditorGUILayout.LabelField("Name", sprite.name);
            else
                EditorGUILayout.LabelField("Name", "-");

            if (alignM)
            {
                int align = GetMetaData(sprite.name).alignment;
                EditorGUILayout.LabelField(Styles.spriteAlignment, Styles.spriteAlignmentOptions[align]);
            }
            else
                EditorGUILayout.LabelField(Styles.spriteAlignment.text, "-");

            if (borderM)
            {
                Vector4 border = GetMetaData(sprite.name).border;
                EditorGUILayout.LabelField("Border", string.Format("L:{0} B:{1} R:{2} T:{3}", border.x, border.y, border.z, border.w));
            }
            else
                EditorGUILayout.LabelField("Border", "-");
        }

        private void UnifiedValues(out bool name, out bool alignment, out bool border)
        {
            name = true;
            alignment = true;
            border = true;
            if (targets.Length < 2)
                return;

            string path = AssetDatabase.GetAssetPath(sprite);
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            SpriteMetaData[] spritesheet = textureImporter.spritesheet;

            string previousName = null;
            int previousAligment = -1;
            Vector4? previousBorder = null;

            for (int targetsIndex = 0; targetsIndex < targets.Length; targetsIndex++)
            {
                Sprite curSprite = targets[targetsIndex] as Sprite;
                for (int spritesIndex = 0; spritesIndex < spritesheet.Length; spritesIndex++)
                {
                    if (spritesheet[spritesIndex].name.Equals(curSprite.name))
                    {
                        if (spritesheet[spritesIndex].alignment != previousAligment && previousAligment > 0)
                            alignment = false;
                        else
                            previousAligment = spritesheet[spritesIndex].alignment;

                        if (spritesheet[spritesIndex].name != previousName && previousName != null)
                            name = false;
                        else
                            previousName = spritesheet[spritesIndex].name;

                        if (spritesheet[spritesIndex].border != previousBorder && previousBorder != null)
                            border = false;
                        else
                            previousBorder = spritesheet[spritesIndex].border;
                    }
                }
            }
        }

        public static Texture2D BuildPreviewTexture(int width, int height, Sprite sprite, Material spriteRendererMaterial, bool isPolygon)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                return null;
            }

            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;

            Texture2D texture = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(sprite, false);

            // only adjust the preview texture size if the sprite is not in polygon mode.
            // In polygon mode, we are looking at a 4x4 texture will detailed mesh. It's better to maximize the display of it.
            if (!isPolygon)
            {
                PreviewHelpers.AdjustWidthAndHeightForStaticPreview((int)spriteWidth, (int)spriteHeight, ref width, ref height);
            }

            SavedRenderTargetState savedRTState = new SavedRenderTargetState();

            RenderTexture tmp = RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Default);

            RenderTexture.active = tmp;
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);

            GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));

            Texture _oldTexture = null;
            Vector4 _oldTexelSize = new Vector4(0, 0, 0, 0);
            bool _matHasTexture = false;
            bool _matHasTexelSize = false;
            if (spriteRendererMaterial != null)
            {
                _matHasTexture = spriteRendererMaterial.HasProperty("_MainTex");
                _matHasTexelSize = spriteRendererMaterial.HasProperty("_MainTex_TexelSize");
            }

            Material copyMaterial = null;
            if (spriteRendererMaterial != null)
            {
                if (_matHasTexture)
                {
                    _oldTexture = spriteRendererMaterial.GetTexture("_MainTex");
                    spriteRendererMaterial.SetTexture("_MainTex", texture);
                }

                if (_matHasTexelSize)
                {
                    _oldTexelSize = spriteRendererMaterial.GetVector("_MainTex_TexelSize");
                    spriteRendererMaterial.SetVector("_MainTex_TexelSize", TextureUtil.GetTexelSizeVector(texture));
                }

                spriteRendererMaterial.SetPass(0);
            }
            else
            {
                copyMaterial = new Material(Shader.Find("Hidden/BlitCopy"));

                copyMaterial.mainTexture = texture;
                copyMaterial.mainTextureScale = Vector2.one;
                copyMaterial.mainTextureOffset = Vector2.zero;
                copyMaterial.SetPass(0);
            }


            float pixelsToUnits = sprite.rect.width / sprite.bounds.size.x;
            Vector2[] vertices = sprite.vertices;
            Vector2[] uvs = sprite.uv;
            ushort[] triangles = sprite.triangles;
            Vector2 pivot = sprite.pivot;

            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Color(new Color(1, 1, 1, 1));
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < triangles.Length; ++i)
            {
                ushort index = triangles[i];
                Vector2 vertex = vertices[index];
                Vector2 uv = uvs[index];
                GL.TexCoord(new Vector3(uv.x, uv.y, 0));
                GL.Vertex3((vertex.x * pixelsToUnits + pivot.x) / spriteWidth, (vertex.y * pixelsToUnits + pivot.y) / spriteHeight, 0);
            }
            GL.End();
            GL.PopMatrix();
            GL.sRGBWrite = false;


            if (spriteRendererMaterial != null)
            {
                if (_matHasTexture)
                    spriteRendererMaterial.SetTexture("_MainTex", _oldTexture);
                if (_matHasTexelSize)
                    spriteRendererMaterial.SetVector("_MainTex_TexelSize", _oldTexelSize);
            }

            Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.filterMode = texture.filterMode;
            copy.anisoLevel = texture.anisoLevel;
            copy.wrapMode = texture.wrapMode;
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);

            savedRTState.Restore();

            if (copyMaterial != null)
                DestroyImmediate(copyMaterial);

            return copy;
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            // Determine is sprite in assetpath a polygon sprite
            bool isPolygonSpriteAsset = false;
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter != null)
            {
                isPolygonSpriteAsset = textureImporter.spriteImportMode == SpriteImportMode.Polygon;
            }

            return BuildPreviewTexture(width, height, sprite, null, isPolygonSpriteAsset);
        }

        public override bool HasPreviewGUI()
        {
            return (target != null);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (target == null)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            bool isPolygon = false;
            string path = AssetDatabase.GetAssetPath(sprite);
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter != null)
            {
                isPolygon = textureImporter.spriteImportMode == SpriteImportMode.Polygon;
            }

            DrawPreview(r, sprite, null, isPolygon);
        }

        public static void DrawPreview(Rect r, Sprite frame, Material spriteRendererMaterial, bool isPolygon)
        {
            if (frame == null)
                return;

            float zoomLevel = Mathf.Min(r.width / frame.rect.width, r.height / frame.rect.height);
            Rect wantedRect = new Rect(r.x, r.y, frame.rect.width * zoomLevel, frame.rect.height * zoomLevel);
            wantedRect.center = r.center;

            Texture2D previewTexture = BuildPreviewTexture((int)wantedRect.width, (int)wantedRect.height, frame, spriteRendererMaterial, isPolygon);
            EditorGUI.DrawTextureTransparent(wantedRect, previewTexture, ScaleMode.ScaleToFit);

            var border = frame.border;
            border *= zoomLevel;
            if (!Mathf.Approximately(border.sqrMagnitude, 0))
            {
                SpriteEditorUtility.BeginLines(new Color(0f, 1f, 0f, 0.7f));
                //TODO: this
                //SpriteEditorUtility.DrawBorder (wantedRect, border);
                SpriteEditorUtility.EndLines();
            }

            DestroyImmediate(previewTexture);
        }

        public override string GetInfoString()
        {
            if (target == null)
                return "";

            Sprite sprite = target as Sprite;

            return string.Format("({0}x{1})",
                (int)sprite.rect.width,
                (int)sprite.rect.height
                );
        }
    }
}
