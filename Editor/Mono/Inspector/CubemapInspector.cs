// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine;


namespace UnityEditor
{
    [CustomEditor(typeof(Cubemap))]
    internal class CubemapInspector : TextureInspector
    {
        static private readonly string[] kSizes = { "16", "32", "64", "128" , "256" , "512" , "1024" , "2048" };
        static private readonly int[] kSizesValues = { 16, 32, 64, 128, 256, 512, 1024, 2048 };
        const int kTextureSize = 64;

        private Texture2D[] m_Images;

        protected override void OnEnable()
        {
            base.OnEnable();
            InitTexturesFromCubemap();
        }

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

        private void InitTexturesFromCubemap()
        {
            var c = target as Cubemap;
            if (c != null)
            {
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
                        m_Images[i] = new Texture2D(kTextureSize, kTextureSize, TextureFormat.RGBA32, false);
                        m_Images[i].hideFlags = HideFlags.HideAndDontSave;
                        TextureUtil.CopyCubemapFaceIntoTexture(c, (CubemapFace)i, m_Images[i]);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (m_Images == null)
                InitTexturesFromCubemap();

            EditorGUIUtility.labelWidth = 50;
            var c = target as Cubemap;
            if (c == null)
                return;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            ShowFace("Right\n(+X)", CubemapFace.PositiveX);
            ShowFace("Left\n(-X)", CubemapFace.NegativeX);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            ShowFace("Top\n(+Y)", CubemapFace.PositiveY);
            ShowFace("Bottom\n(-Y)", CubemapFace.NegativeY);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            ShowFace("Front\n(+Z)", CubemapFace.PositiveZ);
            ShowFace("Back\n(-Z)", CubemapFace.NegativeZ);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.HelpBox("Lowering face size is a destructive operation, you might need to re-assign the textures later to fix resolution issues. It's preferable to use Cubemap texture import type instead of Legacy Cubemap assets.", MessageType.Warning);
            int faceSize = TextureUtil.GetGPUWidth(c);
            faceSize = EditorGUILayout.IntPopup("Face size", faceSize, kSizes, kSizesValues);

            int mipMaps = TextureUtil.GetMipmapCount(c);
            bool useMipMap = EditorGUILayout.Toggle("MipMaps", mipMaps > 1);


            bool linear = TextureUtil.GetLinearSampled(c);
            linear = EditorGUILayout.Toggle("Linear", linear);

            bool readable = TextureUtil.IsCubemapReadable(c);
            readable = EditorGUILayout.Toggle("Readable", readable);

            if (EditorGUI.EndChangeCheck())
            {
                // reformat the cubemap
                if (TextureUtil.ReformatCubemap(ref c, faceSize, faceSize, c.format, useMipMap, linear))
                    InitTexturesFromCubemap();

                TextureUtil.MarkCubemapReadable(c, readable);
                c.Apply();
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
                TextureUtil.CopyTextureIntoCubemapFace(tex, c, face);
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
            GUILayout.BeginHorizontal();
            Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUI.kSingleLineHeight * 2, EditorStyles.label, GUILayout.ExpandWidth(false));
            GUI.Label(r, label, EditorStyles.label);
            r = GUILayoutUtility.GetAspectRect(1, EditorStyles.objectField, GUILayout.Width(64));
            Object retval = EditorGUI.ObjectField(r, obj, objType, allowSceneObjects);
            GUILayout.EndHorizontal();
            return retval;
        }
    }
}
