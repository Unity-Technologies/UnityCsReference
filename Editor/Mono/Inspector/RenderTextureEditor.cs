// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(RenderTexture))]
    [CanEditMultipleObjects]
    internal class RenderTextureEditor : TextureInspector
    {
        private class Styles
        {
            public readonly GUIContent size = EditorGUIUtility.TrTextContent("Size", "Size of the render texture in pixels.");
            public readonly GUIContent cross = EditorGUIUtility.TextContent("x");
            public readonly GUIContent antiAliasing = EditorGUIUtility.TrTextContent("Anti-aliasing", "Number of anti-aliasing samples.");
            public readonly GUIContent colorFormat = EditorGUIUtility.TrTextContent("Color Format", "Format of the color buffer.");
            public readonly GUIContent depthBuffer = EditorGUIUtility.TrTextContent("Depth Buffer", "Format of the depth buffer.");
            public readonly GUIContent enableCompatibleFormat = EditorGUIUtility.TrTextContent("Enable Compatible Color Format", "Lets the color format be changed to compatible and supported formats for the target platform automatically, if the target platform doesn't support the input format.");
            public readonly GUIContent dimension = EditorGUIUtility.TrTextContent("Dimension", "Is the texture 2D, Cube or 3D?");
            public readonly GUIContent enableMipmaps = EditorGUIUtility.TrTextContent("Enable Mip Maps", "This render texture will have Mip Maps.");
            public readonly GUIContent bindMS = EditorGUIUtility.TrTextContent("Bind multisampled", "If enabled, the texture will not go through an AA resolve if bound to a shader.");
            public readonly GUIContent autoGeneratesMipmaps = EditorGUIUtility.TrTextContent("Auto generate Mip Maps", "This render texture automatically generates its Mip Maps.");
            public readonly GUIContent sRGBTexture = EditorGUIUtility.TrTextContent("sRGB (Color RenderTexture)", "RenderTexture content is stored in gamma space. Non-HDR color textures should enable this flag.");
            public readonly GUIContent useDynamicScale = EditorGUIUtility.TrTextContent("Dynamic Scaling", "Allow the texture to be automatically resized by ScalableBufferManager, to support dynamic resolution.");

            public readonly GUIContent[] renderTextureAntiAliasing =
            {
                EditorGUIUtility.TrTextContent("None"),
                EditorGUIUtility.TrTextContent("2 samples"),
                EditorGUIUtility.TrTextContent("4 samples"),
                EditorGUIUtility.TrTextContent("8 samples")
            };
            public readonly int[] renderTextureAntiAliasingValues = { 1, 2, 4, 8 };

            public readonly GUIContent[] dimensionStrings = { EditorGUIUtility.TextContent("2D"), EditorGUIUtility.TrTextContent("Cube"), EditorGUIUtility.TrTextContent("3D") };
            public readonly int[] dimensionValues = { (int)UnityEngine.Rendering.TextureDimension.Tex2D, (int)UnityEngine.Rendering.TextureDimension.Cube, (int)UnityEngine.Rendering.TextureDimension.Tex3D };
        }

        static Styles s_Styles = null;
        private static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }


        [Flags]
        protected enum GUIElements
        {
            RenderTargetNoneGUI = 0,
            RenderTargetDepthGUI = 1 << 1,
            RenderTargetAAGUI = 1 << 2
        }

        const GUIElements s_AllGUIElements = GUIElements.RenderTargetDepthGUI | GUIElements.RenderTargetAAGUI;

        SerializedProperty m_Width;
        SerializedProperty m_Height;
        SerializedProperty m_Depth;
        SerializedProperty m_ColorFormat;
        SerializedProperty m_DepthFormat;
        SerializedProperty m_EnableCompatibleFormat;
        SerializedProperty m_AntiAliasing;
        SerializedProperty m_EnableMipmaps;
        SerializedProperty m_AutoGeneratesMipmaps;
        SerializedProperty m_Dimension;
        SerializedProperty m_sRGB;
        SerializedProperty m_UseDynamicScale;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Width = serializedObject.FindProperty("m_Width");
            m_Height = serializedObject.FindProperty("m_Height");
            m_Depth = serializedObject.FindProperty("m_VolumeDepth");
            m_AntiAliasing = serializedObject.FindProperty("m_AntiAliasing");
            m_ColorFormat = serializedObject.FindProperty("m_ColorFormat");
            m_DepthFormat = serializedObject.FindProperty("m_DepthFormat");
            m_EnableCompatibleFormat = serializedObject.FindProperty("m_EnableCompatibleFormat");
            m_EnableMipmaps = serializedObject.FindProperty("m_MipMap");
            m_AutoGeneratesMipmaps = serializedObject.FindProperty("m_GenerateMips");
            m_Dimension = serializedObject.FindProperty("m_Dimension");
            m_sRGB = serializedObject.FindProperty("m_SRGB");
            m_UseDynamicScale = serializedObject.FindProperty("m_UseDynamicScale");
        }

        protected void OnRenderTextureGUI(GUIElements guiElements)
        {
            GUI.changed = false;

            bool isTexture3D = (m_Dimension.intValue == (int)UnityEngine.Rendering.TextureDimension.Tex3D);

            EditorGUILayout.IntPopup(m_Dimension, styles.dimensionStrings, styles.dimensionValues, styles.dimension);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(styles.size, EditorStyles.popup);
            EditorGUILayout.DelayedIntField(m_Width, GUIContent.none, GUILayout.MinWidth(40));
            GUILayout.Label(styles.cross);
            EditorGUILayout.DelayedIntField(m_Height, GUIContent.none, GUILayout.MinWidth(40));
            if (isTexture3D)
            {
                GUILayout.Label(styles.cross);
                EditorGUILayout.DelayedIntField(m_Depth, GUIContent.none, GUILayout.MinWidth(40));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if ((guiElements & GUIElements.RenderTargetAAGUI) != 0)
                EditorGUILayout.IntPopup(m_AntiAliasing, styles.renderTextureAntiAliasing, styles.renderTextureAntiAliasingValues, styles.antiAliasing);

            GraphicsFormat format = (GraphicsFormat)m_ColorFormat.intValue;
            GraphicsFormat compatibleFormat = SystemInfo.GetCompatibleFormat(format, FormatUsage.Render);

            using (new EditorGUI.DisabledScope(compatibleFormat == GraphicsFormat.None))
                EditorGUILayout.PropertyField(m_EnableCompatibleFormat, styles.enableCompatibleFormat);

            EditorGUILayout.PropertyField(m_ColorFormat, styles.colorFormat);
            m_sRGB.boolValue = GraphicsFormatUtility.IsSRGBFormat((GraphicsFormat)m_ColorFormat.intValue);

            if (compatibleFormat != format)
            {
                string text = string.Format("Format {0} is not supported on this platform. ", format.ToString());
                if (compatibleFormat != GraphicsFormat.None)
                {
                    if (m_EnableCompatibleFormat.boolValue)
                        text += string.Format("Using {0} as a compatible format.", compatibleFormat.ToString());
                    else
                        text += string.Format("You may enable Compatible Color Format to fallback automatically to a platform specific comptible format, {0} on this device.", compatibleFormat.ToString());
                }
                EditorGUILayout.HelpBox(text, m_EnableCompatibleFormat.boolValue && compatibleFormat != GraphicsFormat.None ? MessageType.Warning : MessageType.Error);
            }

            if ((guiElements & GUIElements.RenderTargetDepthGUI) != 0)
                EditorGUILayout.PropertyField(m_DepthFormat, styles.depthBuffer);

            using (new EditorGUI.DisabledScope(isTexture3D))
            {
                EditorGUILayout.PropertyField(m_EnableMipmaps, styles.enableMipmaps);
                using (new EditorGUI.DisabledScope(!m_EnableMipmaps.boolValue))
                    EditorGUILayout.PropertyField(m_AutoGeneratesMipmaps, styles.autoGeneratesMipmaps);
            }

            if (isTexture3D)
            {
                // Mip map generation is not supported yet for 3D textures.
                EditorGUILayout.HelpBox("3D RenderTextures do not support Mip Maps.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(m_UseDynamicScale, styles.useDynamicScale);

            var rt = target as RenderTexture;
            if (GUI.changed && rt != null)
                rt.Release();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DoWrapModePopup();
            DoFilterModePopup();

            using (new EditorGUI.DisabledScope(RenderTextureHasDepth())) // Render Textures with depth are forced to 0 Aniso Level
            {
                DoAnisoLevelSlider();
            }
            if (RenderTextureHasDepth())
            {
                // RenderTextures don't enforce this nicely. RenderTexture only forces Aniso to 0 if the gfx card
                // supports depth, rather than forcing Aniso to zero depending on what the user asks of the RT. If the
                // user requests any kind of depth then we will force aniso to zero here.
                m_Aniso.intValue = 0;
                EditorGUILayout.HelpBox("RenderTextures with depth must have an Aniso Level of 0.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
                ApplySettingsToTextures();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnRenderTextureGUI(s_AllGUIElements);

            serializedObject.ApplyModifiedProperties();
        }

        private bool RenderTextureHasDepth()
        {
            if (GraphicsFormatUtility.IsDepthFormat((GraphicsFormat)m_ColorFormat.enumValueIndex))
                return true;

            return m_DepthFormat.enumValueIndex != 0;
        }

        override public string GetInfoString()
        {
            RenderTexture t = target as RenderTexture;

            string info = t.width + "x" + t.height;
            if (t.dimension == UnityEngine.Rendering.TextureDimension.Tex3D)
                info += "x" + t.volumeDepth;

            if (!t.isPowerOfTwo)
                info += "(NPOT)";

            if (QualitySettings.desiredColorSpace == ColorSpace.Linear)
            {
                bool formatIsHDR = GraphicsFormatUtility.IsIEEE754Format(t.graphicsFormat);
                bool sRGB = t.sRGB && !formatIsHDR;
                info += " " + (sRGB ? "sRGB" : "Linear");
            }

            info += "  " + t.graphicsFormat;
            info += "  " + EditorUtility.FormatBytes(TextureUtil.GetRuntimeMemorySizeLong(t));

            return info;
        }
    }
}
