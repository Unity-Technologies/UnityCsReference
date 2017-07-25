// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(IHVImageFormatImporter))]
    [CanEditMultipleObjects]
    internal class IHVImageFormatImporterInspector : AssetImporterEditor
    {
        public override bool showImportedObject { get { return false; } }

        SerializedProperty  m_IsReadable;
        SerializedProperty  m_sRGBTexture;
        SerializedProperty  m_FilterMode;
        SerializedProperty  m_WrapU;
        SerializedProperty  m_WrapV;
        SerializedProperty  m_WrapW;

        internal class Styles
        {
            // copy pasted from TextureImporterInspector.TextureSettingsGUI()
            public static readonly GUIContent readWrite     = EditorGUIUtility.TextContent("Read/Write Enabled|Enable to be able to access the raw pixel data from code.");
            public static readonly GUIContent sRGBTexture   = EditorGUIUtility.TextContent("sRGB (Color Texture)|Texture content is stored in gamma space. Non-HDR color textures should enable this flag (except if used for IMGUI).");
            public static readonly GUIContent wrapMode      = EditorGUIUtility.TextContent("Wrap Mode");
            public static readonly GUIContent filterMode    = EditorGUIUtility.TextContent("Filter Mode");

            public static readonly int[] filterModeValues           =
            { (int)FilterMode.Point, (int)FilterMode.Bilinear, (int)FilterMode.Trilinear };
            public static readonly GUIContent[] filterModeOptions   =
            { EditorGUIUtility.TextContent("Point (no filter)"), EditorGUIUtility.TextContent("Bilinear"), EditorGUIUtility.TextContent("Trilinear") };
        }


        public override void OnEnable()
        {
            m_IsReadable    = serializedObject.FindProperty("m_IsReadable");
            m_sRGBTexture   = serializedObject.FindProperty("m_sRGBTexture");
            m_FilterMode    = serializedObject.FindProperty("m_TextureSettings.m_FilterMode");
            m_WrapU         = serializedObject.FindProperty("m_TextureSettings.m_WrapU");
            m_WrapV         = serializedObject.FindProperty("m_TextureSettings.m_WrapV");
            m_WrapW         = serializedObject.FindProperty("m_TextureSettings.m_WrapW");
        }

        // alas it looks impossible to share code so we copy paste from TextureImporterInspector.TextureSettingsGUI()

        bool m_ShowPerAxisWrapModes = false;
        public void TextureSettingsGUI()
        {
            // NOTE: once we get ability to have 3D/Volume texture shapes, should pass true for isVolume based on m_TextureShape
            bool isVolume = false;
            TextureInspector.WrapModePopup(m_WrapU, m_WrapV, m_WrapW, isVolume, ref m_ShowPerAxisWrapModes);

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.filterMode, m_FilterMode);
            EditorGUI.BeginChangeCheck();
            FilterMode filter = m_FilterMode.intValue == -1 ? FilterMode.Bilinear : (FilterMode)m_FilterMode.intValue;
            filter = (FilterMode)EditorGUI.IntPopup(rect, Styles.filterMode, (int)filter, Styles.filterModeOptions, Styles.filterModeValues);
            if (EditorGUI.EndChangeCheck())
                m_FilterMode.intValue = (int)filter;
            EditorGUI.EndProperty();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_IsReadable, Styles.readWrite);
            EditorGUILayout.PropertyField(m_sRGBTexture, Styles.sRGBTexture);

            EditorGUI.BeginChangeCheck();
            TextureSettingsGUI();
            if (EditorGUI.EndChangeCheck())
            {
                // copy pasted from TextureImporterInspector.TextureSettingsGUI()
                foreach (AssetImporter importer in targets)
                {
                    Texture tex = AssetDatabase.LoadMainAssetAtPath(importer.assetPath) as Texture;
                    if (m_FilterMode.intValue != -1)
                        TextureUtil.SetFilterModeNoDirty(tex, (FilterMode)m_FilterMode.intValue);
                    if ((m_WrapU.intValue != -1 || m_WrapV.intValue != -1 || m_WrapW.intValue != -1) &&
                        !m_WrapU.hasMultipleDifferentValues && !m_WrapV.hasMultipleDifferentValues && !m_WrapW.hasMultipleDifferentValues)
                    {
                        TextureUtil.SetWrapModeNoDirty(tex, (TextureWrapMode)m_WrapU.intValue, (TextureWrapMode)m_WrapV.intValue, (TextureWrapMode)m_WrapW.intValue);
                    }
                }
                SceneView.RepaintAll();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            ApplyRevertGUI();
            GUILayout.EndHorizontal();
        }
    }
}
