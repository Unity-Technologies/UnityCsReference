// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(Cubemap))]
    internal class CubemapInspector : TextureInspector
    {
        internal static class Styles
        {
            public const int faceThumbnailSize = 64;

            public static readonly string nativeTextureInfo = L10n.Tr("External texture: Unity cannot make changes to this Cubemap.");
            public static readonly string compressedTextureInfo = L10n.Tr("Compressed texture: Unity can only make limited changes to this Cubemap.");

            public static readonly string[] faceSelectionLabels = { "Right\n(+X)", "Left\n(-X)", "Top\n(+Y)", "Bottom\n(-Y)", "Front\n(+Z)", "Back\n(-Z)" };

            public static readonly string faceSizeLabel = L10n.Tr("Face size");
            public static readonly string faceSizeWarning = L10n.Tr("Lowering face size is a destructive operation, you might need to re-assign the textures later to fix resolution issues. It's preferable to use Cubemap texture import type instead of Legacy Cubemap assets.");
            public static readonly string[] faceSizeOptionLabels = { "16", "32", "64", "128", "256", "512", "1024", "2048" };
            public static readonly int[] faceSizeOptionValues = { 16, 32, 64, 128, 256, 512, 1024, 2048 };

            public static readonly string generateMipmapLabel = L10n.Tr("Generate Mipmap");
            public static readonly GUIContent streamingMipmapLevelsContent = EditorGUIUtility.TrTextContent("Stream Mipmap Levels", "Don't load image data immediately but wait till image data is requested from script.");

            public static readonly string linearLabel = L10n.Tr("Linear");

            public static readonly string readableLabel = L10n.Tr("Readable");
        }

        private Texture2D[] m_Images;

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_Images != null)
            {
                for (int i = 0; i < m_Images.Length; ++i)
                {
                    if (m_Images[i] && !EditorUtility.IsPersistent(m_Images[i]))
                        DestroyImmediate(m_Images[i]);
                }
            }
            m_Images = null;
        }

        private void InitFaceThumbnailsFromCubemap()
        {
            var c = target as Cubemap;
            if (c is null || c.isNativeTexture || GraphicsFormatUtility.IsCompressedFormat(c.format))
            {
                return;
            }

            if (m_Images == null)
                m_Images = new Texture2D[6];
            for (int i = 0; i < m_Images.Length; ++i)
            {
                if (m_Images[i] && !EditorUtility.IsPersistent(m_Images[i]))
                    DestroyImmediate(m_Images[i]);

                if (TextureUtil.GetSourceTexture(c, (CubemapFace)i))
                {
                    m_Images[i] = TextureUtil.GetSourceTexture(c, (CubemapFace)i);
                }
                else
                {
                    m_Images[i] = new Texture2D(Styles.faceThumbnailSize, Styles.faceThumbnailSize, TextureFormat.RGBA32, false);
                    m_Images[i].hideFlags = HideFlags.HideAndDontSave;
                    TextureUtil.CopyCubemapFaceIntoTexture(c, (CubemapFace)i, m_Images[i]);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var c = target as Cubemap;
            if (c == null)
                return;

            if (c.isNativeTexture)
            {
                EditorGUILayout.HelpBox(Styles.nativeTextureInfo, MessageType.Info);
                return;
            }

            // A number of option we present in the "full" inspector rely on reformatting or writing to the Cubemap to achieve the desired effect.
            // These operations are not possible on compressed Cubemaps, so we display a limited version of the inspector for them.
            bool isCompressedTex = GraphicsFormatUtility.IsCompressedFormat(c.format);
            if (!isCompressedTex)
            {
                DisplayFullInspector(c);
            }
            else
            {
                DisplayInspectorForCompressedCubemap(c);
            }
        }

        private void DisplayFullInspector(Cubemap c)
        {
            HandleFaceSelectionGUI();

            EditorGUILayout.Space();

            HandleFaceSizeGUI(c);
            bool useMipMap = HandleGenerateMipmapGUI(c);

            if (useMipMap)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    HandleStreamingMipmapGUI(c);
                }
            }

            HandleLinearSamplingGUI(c);
            HandleReadableGUI(c);
        }

        private void DisplayInspectorForCompressedCubemap(Cubemap c)
        {
            EditorGUILayout.HelpBox(Styles.compressedTextureInfo, MessageType.Info);

            bool usesMipMap = TextureUtil.GetMipmapCount(c) > 1;
            if (usesMipMap)
            {
                HandleStreamingMipmapGUI(c);
            }
            HandleReadableGUI(c);
        }

        private void HandleFaceSelectionGUI()
        {
            if (m_Images == null)
                InitFaceThumbnailsFromCubemap();

            EditorGUIUtility.labelWidth = 50;

            using (new GUILayout.VerticalScope())
            {
                for (int face = 0; face < 6; face += 2)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        ShowFace(Styles.faceSelectionLabels[face], (CubemapFace)face);
                        ShowFace(Styles.faceSelectionLabels[face + 1], (CubemapFace)face + 1);
                    }
                }
            }

            EditorGUIUtility.labelWidth = 0;
        }

        private int HandleFaceSizeGUI(Cubemap c)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.HelpBox(Styles.faceSizeWarning, MessageType.Warning);
                int faceSize = TextureUtil.GetGPUWidth(c);
                faceSize = EditorGUILayout.IntPopup(Styles.faceSizeLabel, faceSize, Styles.faceSizeOptionLabels, Styles.faceSizeOptionValues);

                if (changed.changed)
                {
                    HandleCubemapReformatting(c, faceSize: faceSize);
                }

                return faceSize;
            }
        }

        private bool HandleGenerateMipmapGUI(Cubemap c)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                int mipMaps = TextureUtil.GetMipmapCount(c);
                bool useMipMap = EditorGUILayout.Toggle(Styles.generateMipmapLabel, mipMaps > 1);

                if (changed.changed)
                {
                    HandleCubemapReformatting(c, useMipMap: useMipMap);
                }

                return useMipMap;
            }
        }

        private bool HandleStreamingMipmapGUI(Cubemap c)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                bool streamingMipmaps = TextureUtil.GetCubemapStreamingMipmaps(c);
                streamingMipmaps = EditorGUILayout.Toggle(Styles.streamingMipmapLevelsContent, streamingMipmaps);

                if (changed.changed)
                {
                    TextureUtil.SetCubemapStreamingMipmaps(c, streamingMipmaps);
                }

                return streamingMipmaps;
            }
        }

        private bool HandleLinearSamplingGUI(Cubemap c)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                bool linear = TextureUtil.GetLinearSampled(c);
                linear = EditorGUILayout.Toggle(Styles.linearLabel, linear);

                if (changed.changed)
                {
                    HandleCubemapReformatting(c, linear: linear);
                }

                return linear;
            }
        }

        private bool HandleReadableGUI(Cubemap c)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                bool readable = TextureUtil.IsCubemapReadable(c);
                readable = EditorGUILayout.Toggle(Styles.readableLabel, readable);

                if (changed.changed)
                {
                    TextureUtil.MarkCubemapReadable(c, readable);
                }

                return readable;
            }
        }

        private void HandleCubemapReformatting(Cubemap c, int? faceSize = null, bool? useMipMap = null, bool? linear = null)
        {
            // If a value has not been provided, assume that it has not changed and needs to be fetched.
            if (faceSize == null)
            {
                faceSize = TextureUtil.GetGPUWidth(c);
            }
            if (useMipMap == null)
            {
                useMipMap = TextureUtil.GetMipmapCount(c) > 1;
            }
            if (linear == null)
            {
                linear = TextureUtil.GetLinearSampled(c);
            }

            if (TextureUtil.ReformatCubemap(c, faceSize.Value, faceSize.Value, c.format, useMipMap.Value, linear.Value))
            {
                InitFaceThumbnailsFromCubemap();
            }
        }

        // A minimal list of settings to be shown in the Asset Store preview inspector
        internal override void OnAssetStoreInspectorGUI()
        {
            OnInspectorGUI();
        }

        private void ShowFace(string label, CubemapFace face)
        {
            var c = target as Cubemap;
            var iface = (int)face;
            GUI.changed = false;

            var tex = (Texture2D)ObjectField(label, m_Images[iface], typeof(Texture2D), false);
            if (GUI.changed)
            {
                if (tex != null)
                {
                    TextureUtil.CopyTextureIntoCubemapFace(tex, c, face);
                }
                // enable this line in order to retain connections from cube faces to their corresponding
                // texture2D assets, this allows auto-update functionality when editing the source texture
                // images
                //TextureUtil.SetSourceTexture(c, face, tex);
                m_Images[iface] = tex;
            }
        }

        // Variation of ObjectField where label is not restricted to one line
        public static Object ObjectField(string label, Object obj, System.Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            using (new GUILayout.HorizontalScope())
            {
                Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUI.kSingleLineHeight * 2, EditorStyles.label, GUILayout.ExpandWidth(false));
                GUI.Label(r, label, EditorStyles.label);
                r = GUILayoutUtility.GetAspectRect(1, EditorStyles.objectField, GUILayout.Width(64));
                Object retval = EditorGUI.ObjectField(r, obj, objType, allowSceneObjects);
                return retval;
            }
        }
    }
}
