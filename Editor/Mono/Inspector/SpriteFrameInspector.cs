// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;
using Unity.Collections;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

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
            public static readonly GUIContent spriteAlignmentText = EditorGUIUtility.TrTextContent("X:{0:0.##}, Y:{1:0.##}");
            public static readonly GUIContent borderLabel = EditorGUIUtility.TrTextContent("Border", "Border values for the Sprite set in Sprite Editor window. May be useful for 9-Slicing Sprites. The values are in pixels units.");
            public static readonly GUIContent borderText = EditorGUIUtility.TrTextContent("L:{0:0.##} B:{1:0.##} R:{2:0.##} T:{3:0.##}");
            public static readonly GUIContent multiValueText = EditorGUIUtility.TrTextContent("-");
            public static readonly GUIContent elemenText = EditorGUIUtility.TrTextContent("Element {0}");
        }

        SerializedProperty m_Name;
        SerializedProperty m_Pivot;
        SerializedProperty m_ScriptableObjects;
        Vector4 m_BorderValue;
        bool m_BorderHasSameValue = true;
        GUIContent m_SpriteNameContent;
        GUIContent m_SpriteAlignmentContent;
        GUIContent m_SpriteBorderContent;
        StringBuilder m_ElementStringBuilder;
        void OnEnable()
        {
            m_Name = serializedObject.FindProperty("m_Name");
            m_Pivot = serializedObject.FindProperty("m_Pivot");
            m_ScriptableObjects = serializedObject.FindProperty("m_ScriptableObjects");
            m_BorderValue = sprite.border;

            CheckBorderHasSameValue();
            m_SpriteNameContent = new GUIContent(sprite.name);
            m_SpriteAlignmentContent = new GUIContent(UnityString.Format(Styles.spriteAlignmentText.text, m_Pivot.vector2Value.x, m_Pivot.vector2Value.y));
            m_SpriteBorderContent = new GUIContent(UnityString.Format(Styles.borderText.text, m_BorderValue.x, m_BorderValue.y, m_BorderValue.z, m_BorderValue.w));
            m_ElementStringBuilder = new StringBuilder();
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
                    var borderValue = spr.border;
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

        public override VisualElement CreateInspectorGUI()
        {
            var rootVisualElement = new VisualElement();
            rootVisualElement.Add(new IMGUIContainer(OnSpriteFramePropertiesGUI));
            var listElement = new ListView()
            {
                name = "SpriteScriptableObjectListView",
                style = { height = 300}
            };
            listElement.headerTitle = "ScriptableObjects";
            listElement.showBorder = true;
            listElement.showAddRemoveFooter = false;
            listElement.showBoundCollectionSize = false;
            listElement.showFoldoutHeader = true;
            listElement.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listElement.userData = (object) m_ScriptableObjects;
            listElement.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            listElement.bindingPath = m_ScriptableObjects.propertyPath;
            listElement.viewDataKey = m_ScriptableObjects.propertyPath;
            listElement.name = "unity-list-" + m_ScriptableObjects.propertyPath;
            listElement.BindProperty(m_ScriptableObjects.serializedObject);
            listElement.makeItem = () =>
            {
                var propertyField = new ObjectField();
                propertyField.SetEnabled(false);
                var image = new Image()
                {
                    image = EditorGUIUtility.GetHelpIcon(MessageType.Warning),
                    tooltip = EditorGUIUtility.TrTextContent("ScriptableObject that was previously assigned is no longer available.").text,
                    style =
                    {
                        maxHeight = EditorGUI.kSingleLineHeight,
                        flexShrink = 0,
                        alignSelf = Align.FlexEnd,
                    },
                    name = "WarningIcon"
                };
                propertyField.Add(image);

                return propertyField;
            };
            listElement.bindItem = (element, i) =>
            {
                var pf = (ObjectField)element;
                m_ElementStringBuilder.Clear();
                m_ElementStringBuilder.AppendFormat(Styles.elemenText.text, i);
                pf.label = m_ElementStringBuilder.ToString();
                var objReference = m_ScriptableObjects.GetArrayElementAtIndex(i).objectReferenceValue;
                pf.SetValueWithoutNotify(objReference);
                var image = pf.Q<Image>("WarningIcon");
                image.style.visibility = objReference != null ? Visibility.Hidden : Visibility.Visible;
                image.style.position = objReference != null ? Position.Absolute : Position.Relative;
                element.MarkDirtyRepaint();
            };
            listElement.unbindItem = (element, i) =>
            {
                element.Unbind();
            };
            listElement.destroyItem = element => element.Unbind();
            rootVisualElement.Add(listElement);
            return rootVisualElement;
        }

        void OnSpriteFramePropertiesGUI()
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


        public static Texture2D BuildPreviewTexture(Sprite sprite, Material spriteRendererMaterial, bool isPolygon, int width, int height)
        {
            return BuildPreviewTexture(sprite, spriteRendererMaterial, isPolygon, width, height, Color.white, Matrix4x4.identity);
        }

        public static Texture2D BuildPreviewTexture(Sprite sprite, Material spriteRendererMaterial, bool isPolygon, int width, int height, Color color, Matrix4x4 transform)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || sprite == null)
            {
                return null;
            }

            var spriteWidth = sprite.rect.width;
            var spriteHeight = sprite.rect.height;

            Texture2D texture = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(sprite, false);

            // only adjust the preview texture size if the sprite is not in polygon mode.
            // In polygon mode, we are looking at a 4x4 texture will detailed mesh. It's better to maximize the display of it.
            if (!isPolygon)
            {
                // Try to have a minimum of 64 pixels for width and height, unless requested width and height is smaller
                var minWidth = Mathf.Min(64, width);
                var minHeight = Mathf.Min(64, height);

                PreviewHelpers.AdjustWidthAndHeightForStaticPreview((int) spriteWidth, (int) spriteHeight, ref width, ref height);

                // Set minimum size for width and height to prevent small previews for small sprites
                width = Mathf.Max(minWidth, width);
                height = Mathf.Max(minHeight, height);
            }

            SavedRenderTargetState savedRTState = new SavedRenderTargetState();

            RenderTexture tmp = RenderTexture.GetTemporary(
                width,
                height,
                0,
                SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));

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
                if (hasColors || texture == null)
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
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < triangles.Length; ++i)
            {
                ushort index = triangles[i];
                Vector3 vertex = vertices[index];
                vertex = transform.MultiplyPoint(vertex);
                Vector2 uv = uvs[index];
                GL.Color(colors != null ? colors.Value[index] * color : color);
                GL.TexCoord(new Vector3(uv.x, uv.y, 0));
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

            var tmp2 = RenderTexture.GetTemporary(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm);
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

            return BuildPreviewTexture(sprite, null, isPolygonSpriteAsset, width, height);
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

            Texture2D previewTexture = BuildPreviewTexture(frame, spriteRendererMaterial, isPolygon, (int) wantedRect.width, (int) wantedRect.height);
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

            return UnityString.Format("({0}x{1})",
                (int)sprite.rect.width,
                (int)sprite.rect.height
            );
        }
    }
}
