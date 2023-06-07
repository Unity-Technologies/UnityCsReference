// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(RenderTexture))]
    [CanEditMultipleObjects]
    internal class RenderTextureEditor : TextureInspector
    {
        private int m_RepaintDelay = 0;

        private class Styles
        {
            public readonly GUIContent size = EditorGUIUtility.TrTextContent("Size", "Size of the render texture in pixels.");
            public readonly GUIContent cross = EditorGUIUtility.TextContent("x");
            public readonly GUIContent antiAliasing = EditorGUIUtility.TrTextContent("Anti-aliasing", "Number of anti-aliasing samples.");
            public readonly GUIContent colorFormat = EditorGUIUtility.TrTextContent("Color Format", "Format of the color buffer.");
            public readonly GUIContent depthStencilFormat = EditorGUIUtility.TrTextContent("Depth Stencil Format", "Format of the depth stencil buffer.");
            public readonly GUIContent enableCompatibleFormat = EditorGUIUtility.TrTextContent("Enable Compatible Format", "Lets the color and depth stencil formats be changed to compatible and supported formats for the target platform automatically, if the target platform doesn't support the input format.");
            public readonly GUIContent dimension = EditorGUIUtility.TrTextContent("Dimension", "Is the texture 2D, Cube or 3D?");
            public readonly GUIContent enableMipmaps = EditorGUIUtility.TrTextContent("Enable Mip Maps", "This render texture will have Mip Maps.");
            public readonly GUIContent autoGeneratesMipmaps = EditorGUIUtility.TrTextContent("Auto generate Mip Maps", "This render texture automatically generates its Mip Maps.");
            public readonly GUIContent useDynamicScale = EditorGUIUtility.TrTextContent("Dynamic Scaling", "Allow the texture to be automatically resized by ScalableBufferManager, to support dynamic resolution.");
            public readonly GUIContent enableRandomWrite = EditorGUIUtility.TrTextContent("Random Write", "Enable/disable random access write into the color buffer of this render texture.");
            public readonly GUIContent shadowSamplingMode = EditorGUIUtility.TrTextContent("Shadow Sampling Mode", "Enable/disable shadow depth-compare sampling and percentage closer filtering.");

            public readonly GUIContent[] renderTextureAntiAliasing =
            {
                EditorGUIUtility.TrTextContent("None"),
                EditorGUIUtility.TrTextContent("2 samples"),
                EditorGUIUtility.TrTextContent("4 samples"),
                EditorGUIUtility.TrTextContent("8 samples")
            };
            public readonly int[] renderTextureAntiAliasingValues = { 1, 2, 4, 8 };

            public readonly GUIContent[] dimensionStrings = { EditorGUIUtility.TextContent("2D"), EditorGUIUtility.TextContent("2D Array"), EditorGUIUtility.TrTextContent("Cube"), EditorGUIUtility.TrTextContent("3D") };
            public readonly int[] dimensionValues = { (int)UnityEngine.Rendering.TextureDimension.Tex2D, (int)UnityEngine.Rendering.TextureDimension.Tex2DArray, (int)UnityEngine.Rendering.TextureDimension.Cube, (int)UnityEngine.Rendering.TextureDimension.Tex3D };
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
        SerializedProperty m_DepthStencilFormat;
        SerializedProperty m_EnableCompatibleFormat;
        SerializedProperty m_AntiAliasing;
        SerializedProperty m_EnableMipmaps;
        SerializedProperty m_AutoGeneratesMipmaps;
        SerializedProperty m_Dimension;
        SerializedProperty m_sRGB;
        SerializedProperty m_UseDynamicScale;
        SerializedProperty m_EnableRandomWrite;
        SerializedProperty m_ShadowSamplingMode;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Width = serializedObject.FindProperty("m_Width");
            m_Height = serializedObject.FindProperty("m_Height");
            m_Depth = serializedObject.FindProperty("m_VolumeDepth");
            m_AntiAliasing = serializedObject.FindProperty("m_AntiAliasing");
            m_ColorFormat = serializedObject.FindProperty("m_ColorFormat");
            m_DepthStencilFormat = serializedObject.FindProperty("m_DepthStencilFormat");
            m_EnableCompatibleFormat = serializedObject.FindProperty("m_EnableCompatibleFormat");
            m_EnableMipmaps = serializedObject.FindProperty("m_MipMap");
            m_AutoGeneratesMipmaps = serializedObject.FindProperty("m_GenerateMips");
            m_Dimension = serializedObject.FindProperty("m_Dimension");
            m_sRGB = serializedObject.FindProperty("m_SRGB");
            m_UseDynamicScale = serializedObject.FindProperty("m_UseDynamicScale");
            m_EnableRandomWrite = serializedObject.FindProperty("m_EnableRandomWrite");
            m_ShadowSamplingMode = serializedObject.FindProperty("m_ShadowSamplingMode");

            Undo.undoRedoEvent += OnUndoRedoPerformed;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Undo.undoRedoEvent -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            var rt = target as RenderTexture;
            if (rt != null)
            {
                rt.Release();
            }
        }

        protected void OnRenderTextureGUI(GUIElements guiElements)
        {
            GUI.changed = false;

            EditorGUILayout.IntPopup(m_Dimension, styles.dimensionStrings, styles.dimensionValues, styles.dimension);

            // Note that TextureInspector.IsTexture3D/Cube/2DArray/etc. exist. Those functions will use the actual target object to determine the dimension.
            // This because they are drawing preview settings based on the selected target objects.
            // Here we are drawing the one and only Render Texture GUI so we have the dimension field as most correct value.
            bool isTexture3D = (m_Dimension.intValue == (int)UnityEngine.Rendering.TextureDimension.Tex3D);
            bool isTexture2DArray = (m_Dimension.intValue == (int)UnityEngine.Rendering.TextureDimension.Tex2DArray);

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(styles.size, EditorStyles.popup);
            EditorGUILayout.DelayedIntField(m_Width, GUIContent.none, GUILayout.MinWidth(40));
            GUILayout.Label(styles.cross);
            EditorGUILayout.DelayedIntField(m_Height, GUIContent.none, GUILayout.MinWidth(40));
            if (isTexture3D || isTexture2DArray)
            {
                GUILayout.Label(styles.cross);
                EditorGUILayout.DelayedIntField(m_Depth, GUIContent.none, GUILayout.MinWidth(40));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if ((guiElements & GUIElements.RenderTargetAAGUI) != 0)
                EditorGUILayout.IntPopup(m_AntiAliasing, styles.renderTextureAntiAliasing, styles.renderTextureAntiAliasingValues, styles.antiAliasing);

            GraphicsFormat colorFormat = (GraphicsFormat)m_ColorFormat.intValue;
            GraphicsFormat compatibleColorFormat = SystemInfo.GetCompatibleFormat(colorFormat, GraphicsFormatUsage.Render);

            GraphicsFormat depthStencilFormat = (GraphicsFormat)m_DepthStencilFormat.intValue;
            bool isDepthStencilUnused = depthStencilFormat == GraphicsFormat.None;
            bool isDepthStencilFormatIncompatible = !isDepthStencilUnused && SystemInfo.GetCompatibleFormat(depthStencilFormat, GraphicsFormatUsage.Render) == GraphicsFormat.None;
            GraphicsFormat compatibleDepthStencilFormat = (isDepthStencilUnused) ? GraphicsFormat.None :
                GraphicsFormatUtility.GetDepthStencilFormat(GraphicsFormatUtility.GetDepthBits(depthStencilFormat), (GraphicsFormatUtility.IsStencilFormat(depthStencilFormat)) ? 8 : 0);

            // If no fallbacks are found for the color AND depth stencil buffer, disable the EnableCompatibleFormat field
            // If only one of the two fails, checkbox can still be interacted with
            if (!(compatibleColorFormat == GraphicsFormat.None && compatibleDepthStencilFormat == GraphicsFormat.None))
            {
                EditorGUILayout.PropertyField(m_EnableCompatibleFormat, styles.enableCompatibleFormat);
            }

            EditorGUILayout.PropertyField(m_ColorFormat, styles.colorFormat);
            m_sRGB.boolValue = GraphicsFormatUtility.IsSRGBFormat((GraphicsFormat)m_ColorFormat.intValue);

            if (compatibleColorFormat != colorFormat)
            {
                string text = string.Format("Format {0} is not supported on this platform. ", colorFormat.ToString());
                if (compatibleColorFormat != GraphicsFormat.None)
                {
                    if (m_EnableCompatibleFormat.boolValue)
                        text += string.Format("Using {0} as a compatible format.", compatibleColorFormat.ToString());
                    else
                        text += string.Format("You may enable Compatible Format to fallback automatically to a platform specific compatible format, {0} on this device.", compatibleColorFormat.ToString());
                }
                EditorGUILayout.HelpBox(text, m_EnableCompatibleFormat.boolValue && compatibleColorFormat != GraphicsFormat.None ? MessageType.Warning : MessageType.Error);
            }

            // 3D Textures with a depth buffer aren't supported.
            if (!isTexture3D)
            {
                if ((guiElements & GUIElements.RenderTargetDepthGUI) != 0)
                {
                    EditorGUILayout.PropertyField(m_DepthStencilFormat, styles.depthStencilFormat);
                }

                if (depthStencilFormat != compatibleDepthStencilFormat)
                {
                    string text = string.Format("Format {0} is not supported on this platform. ", depthStencilFormat.ToString());
                    if (compatibleDepthStencilFormat != GraphicsFormat.None)
                    {
                        if (m_EnableCompatibleFormat.boolValue)
                            text += string.Format("Using {0} as a compatible format.", compatibleDepthStencilFormat.ToString());
                        else
                            text += string.Format("You may enable Compatible Format to fallback automatically to a platform specific compatible format, {0} on this device.", compatibleDepthStencilFormat.ToString());
                    }
                    EditorGUILayout.HelpBox(text, m_EnableCompatibleFormat.boolValue && compatibleDepthStencilFormat != GraphicsFormat.None ? MessageType.Warning : MessageType.Error);
                }

                if ((GraphicsFormat)m_DepthStencilFormat.intValue == GraphicsFormat.None && (GraphicsFormat)m_ColorFormat.intValue == GraphicsFormat.None)
                {
                    EditorGUILayout.HelpBox("You cannot set both color format and depth format to None", MessageType.Error);
                }
            }

            // Mip map generation is not supported yet for 3D textures (and for depth only textures).
            if (!(isTexture3D || RenderTextureIsDepthOnly()))
            {
                EditorGUILayout.PropertyField(m_EnableMipmaps, styles.enableMipmaps);
                if (m_EnableMipmaps.boolValue)
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_AutoGeneratesMipmaps, styles.autoGeneratesMipmaps);
                    --EditorGUI.indentLevel;
                }
            }
            else
            {
                if (isTexture3D)
                {
                    // Mip map generation is not supported yet for 3D textures.
                    EditorGUILayout.HelpBox("3D RenderTextures do not support Mip Maps.", MessageType.Info);
                }

                if (RenderTextureIsDepthOnly())
                {
                    // Mip map generation is not supported yet for depth-only textures.
                    EditorGUILayout.HelpBox("Depth-only RenderTextures do not support Mip Maps.", MessageType.Info);
                }
            }

            EditorGUILayout.PropertyField(m_UseDynamicScale, styles.useDynamicScale);
            EditorGUILayout.PropertyField(m_EnableRandomWrite, styles.enableRandomWrite);

            var rt = target as RenderTexture;
            if (GUI.changed && rt != null)
            {
                rt.Release();
                m_RepaintDelay = 5;
            }

            // Trigger delayed repaint to allow camera's to be rendered before thumbnail is generated.
            if (m_RepaintDelay > 0)
            {
                --m_RepaintDelay;
                if (m_RepaintDelay == 0)
                    EditorUtility.SetDirty(target);
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DoWrapModePopup();
            DoFilterModePopup();

            if (!RenderTextureHasDepth()) // Render Textures with depth are forced to 0 Aniso Level
            {
                DoAnisoLevelSlider();
            }
            else
            {
                EditorGUILayout.HelpBox("RenderTextures with depth are forced to have an Aniso Level of 0.", MessageType.Info);
            }

            if (RenderTextureHasDepth()) // Depth-only textures have shadow mode
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_ShadowSamplingMode, styles.shadowSamplingMode);
                // Shadow mode unlike the other filter settings requires re-creating the rt if it changed
                // as it's an actual creation flag on the texture.
                if (EditorGUI.EndChangeCheck() && rt != null)
                {
                    rt.Release();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Only render textures with depth can have shadow filtering.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
                ApplySettingsToTextures();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnRenderTextureGUI(s_AllGUIElements);
        }

        private bool RenderTextureHasDepth()
        {
            return RenderTextureIsDepthOnly() || m_DepthStencilFormat.enumValueIndex != 0;
        }

        private bool RenderTextureIsDepthOnly()
        {
            GraphicsFormat colorFormat = (GraphicsFormat)m_ColorFormat.enumValueIndex;
            return colorFormat == GraphicsFormat.None;
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
