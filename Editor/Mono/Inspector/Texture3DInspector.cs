// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(Texture3D))]
    [CanEditMultipleObjects]
    internal class Texture3DInspector : TextureInspector
    {
        enum Preview3DMode
        {
            Volume,
            Slice,
            SDF,
        }

        internal static class Materials
        {
            static Material s_SDF;
            public static Material SDF
            {
                get
                {
                    if (s_SDF == null)
                        s_SDF = Instantiate(EditorGUIUtility.LoadRequired("Previews/Preview3DSDF.mat")) as Material;
                    return s_SDF;
                }
            }

            static Material s_Slice;
            public static Material Slice
            {
                get
                {
                    if (s_Slice == null)
                        s_Slice = Instantiate(EditorGUIUtility.LoadRequired("Previews/Preview3DSliced.mat")) as Material;
                    return s_Slice;
                }
            }

            static Material s_Volume;
            public static Material Volume
            {
                get
                {
                    if (s_Volume == null)
                        s_Volume = Instantiate(EditorGUIUtility.LoadRequired("Previews/Preview3DVolume.mat")) as Material;
                    return s_Volume;
                }
            }
        }

        static class MaterialProps
        {
            public static readonly int colorRamp = Shader.PropertyToID("_ColorRamp");
            public static readonly int voxelSize = Shader.PropertyToID("_VoxelSize");
            public static readonly int invChannels = Shader.PropertyToID("_InvChannels");
            public static readonly int invScale = Shader.PropertyToID("_InvScale");
            public static readonly int globalScale = Shader.PropertyToID("_GlobalScale");
            public static readonly int invResolution = Shader.PropertyToID("_InvResolution");
            public static readonly int quality = Shader.PropertyToID("_Quality");
            public static readonly int alpha = Shader.PropertyToID("_Alpha");
            public static readonly int positions = Shader.PropertyToID("_Positions");
            public static readonly int scale = Shader.PropertyToID("_Scale");
            public static readonly int offset = Shader.PropertyToID("_Offset");
            public static readonly int filterMode = Shader.PropertyToID("_FilterMode");
            public static readonly int ramp = Shader.PropertyToID("_Ramp");
            public static readonly int CamToW = Shader.PropertyToID("_CamToW");
            public static readonly int WToCam = Shader.PropertyToID("_WToCam");
            public static readonly int ObjToW = Shader.PropertyToID("_ObjToW");
            public static readonly int WToObj = Shader.PropertyToID("_WToObj");
            public static readonly int isNormalMap = Shader.PropertyToID("_IsNormalMap");
        }

        static class Styles
        {
            public static readonly GUIContent ramp = EditorGUIUtility.TrTextContent("Ramp", "Use gradient color ramp visualization");
            public static readonly GUIContent quality = EditorGUIUtility.TrTextContent("Quality", "Sample per texture pixel modifier");
            public static readonly GUIContent alpha = EditorGUIUtility.TrTextContent("Alpha", "Opacity of the texture visualization");
            public static readonly GUIContent x = EditorGUIUtility.TrTextContent("X");
            public static readonly GUIContent y = EditorGUIUtility.TrTextContent("Y");
            public static readonly GUIContent z = EditorGUIUtility.TrTextContent("Z");
            public static readonly GUIContent scale = EditorGUIUtility.TrTextContent("Scale", "SDF value scale (how many texels SDF value of 1 represents)");
            public static readonly GUIContent offset = EditorGUIUtility.TrTextContent("Offset", "SDF surface is at this value");
            public static readonly GUIContent volume = EditorGUIUtility.TrTextContent("Volume", "Volumetric rendering display");
            public static readonly GUIContent slice = EditorGUIUtility.TrTextContent("Slice", "Display three planar slices of the texture");
            public static readonly GUIContent sdf = EditorGUIUtility.TrTextContent("SDF", "Display texture as a Signed Distance Field surface");
        }

        const float s_SliderWidth = 40;
        const float s_FloatWidth = 40;
        const float s_MinViewDistance = 0f;
        const float s_MaxViewDistance = 5.0f;
        static readonly Vector2 s_InitialRotation = new Vector2(15, 30);

        PreviewRenderUtility m_PreviewUtility;
        Preview3DMode m_Preview3DMode;
        Texture3D m_Texture;
        Vector2 m_PreviewDir;
        float m_ViewDistance;

        static Color TurboColorEvaluation(float t)
        {
            Vector4 kRedVec4 = new Vector4(0.13572138f, 4.61539260f, -42.66032258f, 132.13108234f);
            Vector4 kGreenVec4 = new Vector4(0.09140261f, 2.19418839f, 4.84296658f, -14.18503333f);
            Vector4 kBlueVec4 = new Vector4(0.10667330f, 12.64194608f, -60.58204836f, 110.36276771f);
            Vector2 kRedVec2 = new Vector2(-152.94239396f, 59.28637943f);
            Vector2 kGreenVec2 = new Vector2(4.27729857f, 2.82956604f);
            Vector2 kBlueVec2 = new Vector2(-89.90310912f, 27.34824973f);

            t = Mathf.Clamp01(t);
            Vector4 v4 = new Vector4(1.0f, t, t * t, t * t * t);
            Vector2 v2 = new Vector2(v4.z, v4.w) * v4.z;

            return new Color(Vector4.Dot(v4, kRedVec4) + Vector2.Dot(v2, kRedVec2),
                Vector4.Dot(v4, kGreenVec4) + Vector2.Dot(v2, kGreenVec2),
                Vector4.Dot(v4, kBlueVec4) + Vector2.Dot(v2, kBlueVec2));
        }

        static Texture2D s_TurboColorRamp;
        static Texture2D TurboColorRamp
        {
            get
            {
                if (s_TurboColorRamp == null)
                {
                    s_TurboColorRamp = new Texture2D(256, 1);
                    s_TurboColorRamp.filterMode = FilterMode.Bilinear;
                    s_TurboColorRamp.wrapMode = TextureWrapMode.Clamp;
                    Color[] pixels = new Color[256];

                    float inversePixelCount = 1.0f / (pixels.Length - 1);
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = TurboColorEvaluation(i * inversePixelCount);
                    }

                    s_TurboColorRamp.SetPixels(pixels);
                    s_TurboColorRamp.Apply();
                }

                return s_TurboColorRamp;
            }
        }

        bool m_Ramp;
        float m_QualityModifier = 1;
        float m_MaxAlpha = 1;

        Vector3 m_Slice;

        float m_StepScale = 1;
        float m_SurfaceOffset = 0;

        public override string GetInfoString()
        {
            Texture3D tex = target as Texture3D;
            var format = TextureUtil.GetTextureFormatString(tex.format);
            var size = EditorUtility.FormatBytes(TextureUtil.GetRuntimeMemorySizeLong(tex));
            string info = $"{tex.width}x{tex.height}x{tex.depth} {format} {size}";
            return info;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            InitPreviewUtility();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
        }

        void InitPreviewUtility()
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility();
                m_PreviewUtility.camera.fieldOfView = 30.0f;
                m_Preview3DMode = Preview3DMode.Volume;
                m_Texture = target as Texture3D;
                m_PreviewDir = new Vector2(0, 0);

                float inverseResolution;
                Vector3 scale = VoxelSize(GetTextureResolution(m_Texture), out inverseResolution);
                m_ViewDistance = Mathf.Lerp(1, 3, Mathf.Clamp01(scale.magnitude - 0.414f));
                m_PreviewUtility.camera.farClipPlane = s_MaxViewDistance * 2 + 1;
                m_PreviewUtility.camera.nearClipPlane = 0.1f;

                m_QualityModifier = Mathf.Clamp(8 - Mathf.Log(1.0f / inverseResolution, 2), 1, 8);
                m_Slice = Vector3.Scale(Vector3.one / 2, new Vector3(m_Texture.width, m_Texture.height, m_Texture.depth));
            }
        }

        float PreviewFloatField(GUIContent content, float value, float width)
        {
            float labelWidth = EditorStyles.label.CalcSize(content).x + 2;
            Rect controlRect = EditorGUILayout.GetControlRect(GUILayout.Width(labelWidth + width));
            int controlId = GUIUtility.GetControlID(FocusType.Keyboard);
            Rect labelRect = new Rect(controlRect.position, new Vector2(labelWidth, controlRect.height));
            controlRect.x += labelRect.width;
            controlRect.width -= labelRect.width + 2;
            EditorGUI.PrefixLabel(labelRect, controlId, content);
            return EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, controlRect, labelRect, controlId, value, EditorGUI.kFloatFieldFormatString, EditorStyles.numberField, true);
        }

        public override void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supports3DTextures)
                return;
            GUI.enabled = true;

            switch (m_Preview3DMode)
            {
                case Preview3DMode.Volume:
                    m_Ramp = GUILayout.Toggle(m_Ramp, Styles.ramp, EditorStyles.toolbarButton);
                    m_QualityModifier = PreviewSettingsSlider(Styles.quality, m_QualityModifier, 0.5f, 8, s_SliderWidth, s_FloatWidth, isInteger: false);
                    m_MaxAlpha = PreviewSettingsSlider(Styles.alpha, m_MaxAlpha, 0.01f, 1, s_SliderWidth, s_FloatWidth, isInteger: false);
                    break;

                case Preview3DMode.Slice:
                    m_Ramp = GUILayout.Toggle(m_Ramp, Styles.ramp, EditorStyles.toolbarButton);
                    m_Slice.x = Mathf.Clamp(PreviewFloatField(Styles.x, m_Slice.x, s_FloatWidth), 0, m_Texture.width);
                    m_Slice.y = Mathf.Clamp(PreviewFloatField(Styles.y, m_Slice.y, s_FloatWidth), 0, m_Texture.height);
                    m_Slice.z = Mathf.Clamp(PreviewFloatField(Styles.z, m_Slice.z, s_FloatWidth), 0, m_Texture.depth);
                    break;

                case Preview3DMode.SDF:
                    m_StepScale = PreviewFloatField(Styles.scale, m_StepScale, s_FloatWidth);
                    m_SurfaceOffset = PreviewFloatField(Styles.offset, m_SurfaceOffset, s_FloatWidth);
                    break;

                default:
                    throw new ArgumentException($"Unexpected \"Preview3DMode\" value: \"{m_Preview3DMode}\"");
            }

            if (GUILayout.Toggle(m_Preview3DMode == Preview3DMode.Volume, Styles.volume, EditorStyles.toolbarButton))
                m_Preview3DMode = Preview3DMode.Volume;
            if (GUILayout.Toggle(m_Preview3DMode == Preview3DMode.Slice, Styles.slice, EditorStyles.toolbarButton))
                m_Preview3DMode = Preview3DMode.Slice;
            if (GUILayout.Toggle(m_Preview3DMode == Preview3DMode.SDF, Styles.sdf, EditorStyles.toolbarButton))
                m_Preview3DMode = Preview3DMode.SDF;
        }

        static Vector3 GetTextureResolution(Texture texture)
        {
            if (texture.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
            {
                throw new ArgumentException($"Texture dimension must be {UnityEngine.Rendering.TextureDimension.Tex3D}, was {texture.dimension}");
            }

            Texture3D texture3D = texture as Texture3D;
            RenderTexture renderTexture = texture as RenderTexture;

            Vector3 textureResolution;
            if (texture3D != null)
            {
                textureResolution = new Vector3(texture3D.width, texture3D.height, texture3D.depth);
            }
            else if (renderTexture != null)
            {
                textureResolution = new Vector3(renderTexture.width, renderTexture.height, renderTexture.volumeDepth);
            }
            else
            {
                throw new ArgumentException($"Could not cast texture to {typeof(Texture3D)} or {typeof(RenderTexture)}");
            }

            return textureResolution;
        }

        static Vector3 VoxelSize(Vector3 textureResolution, out float inverseResolution)
        {
            inverseResolution = 1.0f / Mathf.Max(textureResolution.x, textureResolution.y, textureResolution.z);
            return new Vector3(textureResolution.x * inverseResolution, textureResolution.y * inverseResolution, textureResolution.z * inverseResolution);
        }

        static Vector3 PrepareGeneralPreview(Material material, Texture texture, out Vector3 inverseScale,
            out float inverseResolution, Gradient customColorRamp = null)
        {
            Vector3 voxelSize = VoxelSize(GetTextureResolution(texture), out inverseResolution);
            inverseScale = new Vector3(1.0f / voxelSize.x, 1.0f / voxelSize.y, 1.0f / voxelSize.z);

            material.mainTexture = texture;
            material.SetVector(MaterialProps.voxelSize, voxelSize);
            material.SetVector(MaterialProps.invScale, inverseScale);
            material.SetInt(MaterialProps.isNormalMap, IsNormalMap(texture) ? 1 : 0);

            if (customColorRamp != null)
            {
                material.SetTexture(MaterialProps.colorRamp, GradientPreviewCache.GetGradientPreview(customColorRamp));
            }
            else
            {
                material.SetTexture(MaterialProps.colorRamp, TurboColorRamp);
            }

            return voxelSize;
        }

        internal static void PrepareSDFPreview(Material material, Texture texture, Vector3 scale, float stepScale = 1,
            float surfaceOffset = 0, Gradient customColorRamp = null)
        {
            float inverseResolution;
            Vector3 inverseScale;
            PrepareGeneralPreview(material, texture, out inverseScale, out inverseResolution, customColorRamp);

            float boundSize = Mathf.Max(scale.x, scale.y, scale.z) / 2;

            material.SetVector(MaterialProps.globalScale, scale);
            material.SetFloat(MaterialProps.invResolution, inverseResolution);
            material.SetFloat(MaterialProps.scale, stepScale * boundSize);
            material.SetFloat(MaterialProps.offset, surfaceOffset);
        }

        internal static void PrepareSlicePreview(Material material, Texture texture, Vector3 slice,
            FilterMode filterMode, bool colorRamp = false, Gradient customColorRamp = null)
        {
            float inverseResolution;
            Vector3 inverseScale;
            PrepareGeneralPreview(material, texture, out inverseScale, out inverseResolution, customColorRamp);
            uint colorChannelCount = GraphicsFormatUtility.GetColorComponentCount(texture.graphicsFormat);

            Vector3 voxelSize = material.GetVector(MaterialProps.voxelSize);
            Vector3 textureResolution = GetTextureResolution(texture);
            Vector3 positions = new Vector3(slice.x / textureResolution.x, slice.y / textureResolution.y, slice.z / textureResolution.z);
            positions.x = Mathf.Clamp01(positions.x);
            positions.y = Mathf.Clamp01(positions.y);
            positions.z = Mathf.Clamp01(positions.z);
            positions -= new Vector3(0.5f, 0.5f, 0.5f);
            positions.Scale(voxelSize);
            material.SetVector(MaterialProps.positions, positions);
            material.SetVector(MaterialProps.invChannels, new Vector3(1.0f / colorChannelCount, 1.0f / colorChannelCount, 1.0f / colorChannelCount));
            material.SetFloat(MaterialProps.filterMode, Convert.ToSingle(filterMode));
            material.SetFloat(MaterialProps.ramp, Convert.ToSingle(colorRamp));
        }

        internal static int PrepareVolumePreview(Material material, Texture texture, Vector3 scale, float opacity,
            FilterMode filterMode, bool colorRamp, Gradient customColorRamp, Camera camera, Matrix4x4 trs, float qualityModifier = 2)
        {
            float inverseResolution;
            Vector3 inverseScale;
            PrepareGeneralPreview(material, texture, out inverseScale, out inverseResolution, customColorRamp);
            uint colorChannelCount = GraphicsFormatUtility.GetColorComponentCount(texture.graphicsFormat);

            material.SetVector(MaterialProps.globalScale, scale);
            material.SetFloat(MaterialProps.ramp, Convert.ToSingle(colorRamp));
            material.SetFloat(MaterialProps.invResolution, inverseResolution);
            material.SetVector(MaterialProps.invChannels, new Vector3(1.0f / colorChannelCount, 1.0f / colorChannelCount, 1.0f / colorChannelCount));
            material.SetFloat(MaterialProps.alpha, Mathf.Pow(Mathf.Clamp01(opacity), 3));
            material.SetFloat(MaterialProps.filterMode, Convert.ToSingle(filterMode));

            float quality = inverseResolution / qualityModifier / 2;
            material.SetFloat(MaterialProps.quality, quality);

            material.SetMatrix(MaterialProps.CamToW, camera.cameraToWorldMatrix);
            material.SetMatrix(MaterialProps.WToCam, camera.worldToCameraMatrix);
            material.SetMatrix(MaterialProps.ObjToW, trs);
            material.SetMatrix(MaterialProps.WToObj, trs.inverse);

            return Convert.ToInt32(1 / inverseResolution * qualityModifier * 2);
        }

        void DrawPreview()
        {
            if (!SystemInfo.supports3DTextures)
                return;
            Texture3D texture = target as Texture3D;
            if (texture == null)
                return;
            if (!SystemInfo.supportsCompressed3DTextures && GraphicsFormatUtility.IsCompressedTextureFormat(texture.format))
                return;

            Quaternion rotation = Quaternion.Euler(-m_PreviewDir.y + s_InitialRotation.x, -m_PreviewDir.x + s_InitialRotation.y, 0);

            m_PreviewUtility.camera.transform.position = rotation * Vector3.back * m_ViewDistance;
            m_PreviewUtility.camera.transform.rotation = rotation;

            Vector3 scale = Vector3.one;
            switch (m_Preview3DMode)
            {
                case Preview3DMode.Volume:
                    int sampleCount = PrepareVolumePreview(Materials.Volume, texture, scale, m_MaxAlpha, texture.filterMode,
                        m_Ramp, null, m_PreviewUtility.camera, Matrix4x4.identity, m_QualityModifier);
                    GL.PushMatrix();
                    GL.LoadProjectionMatrix(m_PreviewUtility.camera.projectionMatrix);
                    Materials.Volume.SetPass(0);
                    Graphics.DrawProceduralNow(MeshTopology.Quads, 4, sampleCount);
                    GL.PopMatrix();
                    return;
                case Preview3DMode.Slice:
                    PrepareSlicePreview(Materials.Slice, texture, m_Slice, texture.filterMode, m_Ramp);
                    m_PreviewUtility.DrawMesh(Handles.cubeMesh, Matrix4x4.Scale(scale + new Vector3(0.0001f, 0.0001f, 0.0001f)), Materials.Slice, 0);
                    break;
                case Preview3DMode.SDF:
                    PrepareSDFPreview(Materials.SDF, texture, scale, m_StepScale, m_SurfaceOffset);
                    m_PreviewUtility.DrawMesh(Handles.cubeMesh, Matrix4x4.Scale(scale), Materials.SDF, 0);
                    break;
            }

            m_PreviewUtility.Render();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supports3DTextures)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "3D texture preview not supported");
                return;
            }
            Texture3D texture = target as Texture3D;
            if (texture == null)
                return;
            if (!SystemInfo.supportsCompressed3DTextures && GraphicsFormatUtility.IsCompressedTextureFormat(texture.format))
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Compressed 3D texture preview is not supported");
                return;
            }

            InitPreviewUtility();
            Event e = Event.current;
            m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, r);

            if (e.type == EventType.ScrollWheel)
            {
                m_ViewDistance = Mathf.Clamp(m_ViewDistance + e.delta.y * (0.01f + Mathf.Sqrt(m_ViewDistance) / 20), s_MinViewDistance, s_MaxViewDistance);
                e.Use();
                Repaint();
            }

            if (e.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(r, background);
            DrawPreview();
            m_PreviewUtility.EndAndDrawPreview(r);
        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supports3DTextures)
                return null;
            if (target == null)
                return null;

            OnEnable();
            m_QualityModifier *= 2;

            Rect r = new Rect(0, 0, width, height);
            m_PreviewUtility.BeginStaticPreview(r);
            DrawPreview();
            return m_PreviewUtility.EndStaticPreview();
        }
    }
}
