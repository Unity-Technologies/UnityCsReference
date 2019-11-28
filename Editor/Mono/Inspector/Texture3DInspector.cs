// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        static class Props
        {
            public static readonly int invResolution = Shader.PropertyToID("_InvResolution");
            public static readonly int quality = Shader.PropertyToID("_Quality");
            public static readonly int invChannels = Shader.PropertyToID("_InvChannels");
            public static readonly int invScale = Shader.PropertyToID("_InvScale");
            public static readonly int positions = Shader.PropertyToID("_Positions");
            public static readonly int alpha = Shader.PropertyToID("_Alpha");
            public static readonly int ramp = Shader.PropertyToID("_Ramp");
            public static readonly int scale = Shader.PropertyToID("_Scale");
            public static readonly int offset = Shader.PropertyToID("_Offset");
        }

        static class Styles
        {
            public static readonly GUIContent ramp = EditorGUIUtility.TrTextContent("Ramp", "Use gradient color ramp visualization");
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

        Mesh s_MeshCube;

        Material s_MaterialVolume;
        Material s_MaterialSliced;
        Material s_MaterialSDF;

        const float s_SliderWidth = 120;
        const float s_FloatWidth = 62;
        const float s_MinViewDistance = 0f;
        const float s_MaxViewDistance = 5.0f;

        PreviewRenderUtility m_PreviewUtility;
        Preview3DMode m_Preview3DMode;
        Texture3D m_Texture;
        Vector2 m_PreviewDir;
        float m_ViewDistance;

        Vector3 m_Scale;
        Vector3 m_InvScale;
        float m_InvResolution;

        bool m_Ramp;
        float m_Channels;
        float m_MaxAlpha = 1;

        Vector3 m_Slice;

        float m_HDRScale = 1;
        float m_HDROffset = 0;

        public override string GetInfoString()
        {
            Texture3D tex = target as Texture3D;

            string info = UnityString.Format("{0}x{1}x{2} {3} {4}",
                tex.width, tex.height, tex.depth,
                TextureUtil.GetTextureFormatString(tex.format),
                EditorUtility.FormatBytes(TextureUtil.GetRuntimeMemorySizeLong(tex)));

            return info;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            InitPreviewUtility();
            LoadPreviewMeshes();
            LoadPreviewMaterials();
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
                m_Channels = Mathf.Clamp(GraphicsFormatUtility.GetColorComponentCount(m_Texture.graphicsFormat), 1, 3);

                CalculateResolutionAndScale(m_Texture);
                m_ViewDistance = Mathf.Lerp(1, 3, Mathf.Clamp01(m_Scale.magnitude - 0.414f));
                m_PreviewUtility.camera.farClipPlane = s_MaxViewDistance + 1;
                m_PreviewUtility.camera.nearClipPlane = 0.01f;
            }
        }

        float PreviewFloatField(GUIContent content, float value, float width)
        {
            float labelWidth = EditorStyles.label.CalcSize(content).x + 2;
            Rect controlRect = EditorGUILayout.GetControlRect(GUILayout.Width(labelWidth + width));
            int controlId = content.text.GetHashCode() ^ controlRect.GetHashCode();
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
                    GUILayout.Label(Styles.alpha);
                    m_MaxAlpha = EditorGUILayout.Slider(m_MaxAlpha, 0.01f, 1, GUILayout.Width(s_SliderWidth));
                    break;

                case Preview3DMode.Slice:
                    m_Slice.x = Mathf.Clamp(PreviewFloatField(Styles.x, m_Slice.x, s_FloatWidth), 0, m_Texture.width);
                    m_Slice.y = Mathf.Clamp(PreviewFloatField(Styles.y, m_Slice.y, s_FloatWidth), 0, m_Texture.height);
                    m_Slice.z = Mathf.Clamp(PreviewFloatField(Styles.z, m_Slice.z, s_FloatWidth), 0, m_Texture.depth);
                    break;

                case Preview3DMode.SDF:
                    m_HDRScale = PreviewFloatField(Styles.scale, m_HDRScale, s_FloatWidth);
                    m_HDROffset = PreviewFloatField(Styles.offset, m_HDROffset, s_FloatWidth);
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

        void DrawPreview()
        {
            Texture3D texture = target as Texture3D;
            CalculateResolutionAndScale(texture);

            Quaternion rotation = Quaternion.Euler(-m_PreviewDir.y, 0, 0) * Quaternion.Euler(0, -m_PreviewDir.x, 0) * Quaternion.Euler(15, 30, 0);

            m_PreviewUtility.camera.transform.position = rotation * Vector3.back * m_ViewDistance;
            m_PreviewUtility.camera.transform.rotation = rotation;

            switch (m_Preview3DMode)
            {
                case Preview3DMode.Volume:
                    s_MaterialVolume.mainTexture = texture;
                    s_MaterialVolume.SetFloat(Props.invResolution, m_InvResolution);
                    s_MaterialVolume.SetFloat(Props.quality, Mathf.Clamp(9 - Mathf.Log(1.0f / m_InvResolution, 2), 2, 8));
                    s_MaterialVolume.SetVector(Props.invChannels, new Vector3(1 / m_Channels, 1 / m_Channels, 1 / m_Channels));
                    s_MaterialVolume.SetFloat(Props.alpha, Mathf.Pow(m_MaxAlpha, 3));
                    s_MaterialVolume.SetVector(Props.invScale, m_InvScale);
                    s_MaterialVolume.SetFloat(Props.ramp, Convert.ToSingle(m_Ramp));

                    m_PreviewUtility.DrawMesh(s_MeshCube, Matrix4x4.Scale(m_Scale), s_MaterialVolume, 0);
                    break;
                case Preview3DMode.Slice:
                    s_MaterialSliced.mainTexture = texture;
                    Vector3 positions = new Vector3(m_Slice.x / m_Texture.width, m_Slice.y / m_Texture.height, m_Slice.z / m_Texture.depth) - new Vector3(0.5f, 0.5f, 0.5f);
                    positions.Scale(m_Scale);
                    s_MaterialSliced.SetVector(Props.positions, positions);
                    s_MaterialSliced.SetVector(Props.invScale, m_InvScale);

                    m_PreviewUtility.DrawMesh(s_MeshCube, Matrix4x4.Scale(m_Scale + new Vector3(0.0001f, 0.0001f, 0.0001f)), s_MaterialSliced, 0);
                    break;
                case Preview3DMode.SDF:
                    s_MaterialSDF.mainTexture = texture;
                    s_MaterialSDF.SetFloat(Props.invResolution, m_InvResolution);
                    s_MaterialSDF.SetFloat(Props.scale, m_HDRScale);
                    s_MaterialSDF.SetFloat(Props.offset, m_HDROffset);
                    s_MaterialSDF.SetVector(Props.invScale, m_InvScale);

                    m_PreviewUtility.DrawMesh(s_MeshCube, Matrix4x4.Scale(m_Scale), s_MaterialSDF, 0);
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
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;

            OnEnable();

            Rect r = new Rect(0, 0, width, height);
            m_PreviewUtility.BeginStaticPreview(r);
            DrawPreview();
            return m_PreviewUtility.EndStaticPreview();
        }

        void LoadPreviewMeshes()
        {
            s_MeshCube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        }

        void LoadPreviewMaterials()
        {
            s_MaterialVolume = EditorGUIUtility.LoadRequired("Previews/Preview3DVolume.mat") as Material;
            s_MaterialSliced = EditorGUIUtility.LoadRequired("Previews/Preview3DSliced.mat") as Material;
            s_MaterialSDF = EditorGUIUtility.LoadRequired("Previews/Preview3DSDF.mat") as Material;
        }

        void CalculateResolutionAndScale(Texture3D texture)
        {
            m_InvResolution = 1.0f / Mathf.Max(texture.width, texture.height, texture.depth);
            m_Scale.x = texture.width * m_InvResolution;
            m_Scale.y = texture.height * m_InvResolution;
            m_Scale.z = texture.depth * m_InvResolution;

            m_InvScale = new Vector3(1.0f / m_Scale.x, 1.0f / m_Scale.y, 1.0f / m_Scale.z);
        }
    }
}
