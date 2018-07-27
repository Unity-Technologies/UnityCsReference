// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    internal class SpriteInspector : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent nameLabel = EditorGUIUtility.TrTextContent("Name", "The name for the Sprite.");
            public static readonly GUIContent spriteAlignmentLabel = EditorGUIUtility.TrTextContent("Pivot", "The value is normalized to the Sprite's size where (0, 0) is the lower left and (1, 1) is the upper right. May be used for syncing animation frames of different sizes.");
            public static readonly GUIContent spriteAlignmentText = EditorGUIUtility.TrTextContent("X:{0}, Y:{1}");
            public static readonly GUIContent borderLabel = EditorGUIUtility.TrTextContent("Border", "Border values for the Sprite set in Sprite Editor window. May be useful for 9-Slicing Sprites.");
            public static readonly GUIContent borderText = EditorGUIUtility.TrTextContent("L:{0} B:{1} R:{2} T:{3}");
            public static readonly GUIContent multiValueText = EditorGUIUtility.TrTextContent("-");
        }

        SerializedProperty m_Name;
        SerializedProperty m_Pivot;
        Vector4 m_BorderValue;
        bool m_BorderHasSameValue = true;
        GUIContent m_SpriteNameContent;
        GUIContent m_SpriteAlignmentContent;
        GUIContent m_SpriteBorderContent;

        void OnEnable()
        {
            m_Name = serializedObject.FindProperty("m_Name");
            m_Pivot = serializedObject.FindProperty("m_Pivot");
            m_BorderValue = GetSpriteBorderValue(sprite);

            CheckBorderHasSameValue();
            m_SpriteNameContent = new GUIContent(sprite.name);
            m_SpriteAlignmentContent = new GUIContent(string.Format(Styles.spriteAlignmentText.text, m_Pivot.vector2Value.x, m_Pivot.vector2Value.y));
            m_SpriteBorderContent = new GUIContent(string.Format(Styles.borderText.text, m_BorderValue.x, m_BorderValue.y, m_BorderValue.z, m_BorderValue.w));
        }

        Vector4 GetSpriteBorderValue(Sprite sprite)
        {
            var returnValue = Vector4.one;
            var path = AssetDatabase.GetAssetPath(sprite);
            var spriteDataProvider = AssetImporter.GetAtPath(path) as ISpriteEditorDataProvider;
            if (spriteDataProvider != null)
            {
                var textureDataProvider = spriteDataProvider.GetDataProvider<ITextureDataProvider>();
                if (textureDataProvider?.texture != null)
                {
                    int width, height;
                    textureDataProvider.GetTextureActualWidthAndHeight(out width, out height);
                    var texture = textureDataProvider.texture;
                    var textureScale = new Vector2(width / (float)texture.width, height / (float)texture.height);
                    returnValue.x = Mathf.RoundToInt(sprite.border.x * textureScale.x);
                    returnValue.z = Mathf.RoundToInt(sprite.border.z * textureScale.x);
                    returnValue.y = Mathf.RoundToInt(sprite.border.y * textureScale.y);
                    returnValue.w = Mathf.RoundToInt(sprite.border.w * textureScale.y);
                }
            }

            return returnValue;
        }

        void CheckBorderHasSameValue()
        {
            foreach (var obj in serializedObject.targetObjects)
            {
                if (obj == target)
                    continue;
                if (obj is Sprite)
                {
                    var spr = (Sprite)obj;
                    var borderValue = GetSpriteBorderValue(spr);
                    if (borderValue != m_BorderValue)
                    {
                        m_BorderHasSameValue = false;
                        break;
                    }
                }
                else
                {
                    m_BorderHasSameValue = false;
                    break;
                }
            }
        }

        private Sprite sprite
        {
            get { return target as Sprite; }
        }

        public override void OnInspectorGUI()
        {
            if (!m_Name.hasMultipleDifferentValues)
                EditorGUILayout.LabelField(Styles.nameLabel, m_SpriteNameContent);
            else
                EditorGUILayout.LabelField(Styles.nameLabel, Styles.multiValueText);

            if (!m_Pivot.hasMultipleDifferentValues)
                EditorGUILayout.LabelField(Styles.spriteAlignmentLabel, m_SpriteAlignmentContent);
            else
                EditorGUILayout.LabelField(Styles.spriteAlignmentLabel, Styles.multiValueText);

            if (m_BorderHasSameValue)
                EditorGUILayout.LabelField(Styles.borderLabel, m_SpriteBorderContent);
            else
                EditorGUILayout.LabelField(Styles.borderLabel, Styles.multiValueText);
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
                RenderTextureReadWrite.Linear);

            RenderTexture.active = tmp;

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

            bool hasColors = sprite.HasVertexAttribute(VertexAttribute.Color);

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
                if (hasColors)
                {
                    SpriteUtility.previewSpriteDefaultMaterial.SetPass(0);
                }
                else if (texture != null)
                {
                    copyMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
                    copyMaterial.mainTexture = texture;
                    copyMaterial.mainTextureScale = Vector2.one;
                    copyMaterial.mainTextureOffset = Vector2.zero;
                    copyMaterial.SetPass(0);
                }
            }

            float pixelsToUnits = sprite.rect.width / sprite.bounds.size.x;
            Vector2[] vertices = sprite.vertices;
            Vector2[] uvs = Sprites.SpriteUtility.GetSpriteUVs(sprite, false);
            ushort[] triangles = sprite.triangles;
            Vector2 pivot = sprite.pivot;

            NativeSlice<Color32>? colors = null;
            if (hasColors)
                colors = sprite.GetVertexAttribute<Color32>(VertexAttribute.Color);

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
                if (colors != null)
                    GL.Color(colors.Value[index]);
                GL.Vertex3((vertex.x * pixelsToUnits + pivot.x) / spriteWidth, (vertex.y * pixelsToUnits + pivot.y) / spriteHeight, 0);
            }
            GL.End();
            GL.PopMatrix();


            if (spriteRendererMaterial != null)
            {
                if (_matHasTexture)
                    spriteRendererMaterial.SetTexture("_MainTex", _oldTexture);
                if (_matHasTexelSize)
                    spriteRendererMaterial.SetVector("_MainTex_TexelSize", _oldTexelSize);
            }

            var tmp2 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(tmp, tmp2, EditorGUIUtility.GUITextureBlit2SRGBMaterial);

            RenderTexture.active = tmp2;

            Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.filterMode = texture != null ? texture.filterMode : FilterMode.Point;
            copy.anisoLevel = texture != null ? texture.anisoLevel : 0;
            copy.wrapMode = texture != null ? texture.wrapMode : TextureWrapMode.Clamp;
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);
            RenderTexture.ReleaseTemporary(tmp2);

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
            var sprite = target as Sprite;
            return (sprite != null) && UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(sprite, false) != null;
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
