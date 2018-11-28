// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor
{
    [AssetFileNameExtension("brush")]
    internal class Brush : ScriptableObject
    {
        [MenuItem("Assets/Create/Brush")]
        public static void CreateNewDefaultBrush()
        {
            Brush b = CreateInstance(DefaultMask(), AnimationCurve.Linear(0, 0, 1, 1), 1.0f, false);
            ProjectWindowUtil.CreateAsset(b, "New Brush.brush");
        }

        // Don't instantiate directly, use Brush.CreateInstance()
        protected Brush() {}

        internal const float sqrt2 = 1.414214f;   // diagonal of a unit square
        internal const float kMaxRadiusScale = sqrt2 + (sqrt2 * 0.05f);   // take into account the smoothness from CreateBrush.shader ( circleFalloff = 1.0f - smoothstep(_BrushRadiusScale-0.05f, _BrushRadiusScale, dist) )

        public Texture2D m_Mask;
        public AnimationCurve m_Falloff;
        [Range(1.0f, kMaxRadiusScale)]
        public float m_RadiusScale = kMaxRadiusScale;

        Texture2D m_Texture = null;
        Texture2D m_Thumbnail = null;

        bool m_UpdateTexture = true;
        bool m_UpdateThumbnail = true;
        internal bool readOnly { get; private set; } = false;

        static Texture2D s_WhiteTexture = null;
        static Material s_CreateBrushMaterial = null;

        internal static Brush CreateInstance(Texture2D t, AnimationCurve f, float radiusScale, bool isReadOnly)
        {
            var b = ScriptableObject.CreateInstance<Brush>();
            b.m_Mask = t;
            b.m_Falloff = f;
            b.m_RadiusScale = radiusScale;
            b.readOnly = isReadOnly;
            return b;
        }

        void UpdateTexture()
        {
            if (m_UpdateTexture || m_Texture == null)
            {
                if (m_Mask == null)
                    m_Mask = DefaultMask();

                m_Texture = GenerateBrushTexture(m_Mask, m_Falloff, m_RadiusScale, m_Mask.width, m_Mask.height);
                m_UpdateTexture = false;
            }
        }

        void UpdateThumbnail()
        {
            if (m_UpdateThumbnail || m_Thumbnail == null)
            {
                if (m_Mask == null)
                    m_Mask = DefaultMask();

                m_Thumbnail = GenerateBrushTexture(m_Mask, m_Falloff, m_RadiusScale, 64, 64, true);
                m_UpdateThumbnail = false;
            }
        }

        public Texture2D texture { get { UpdateTexture(); return m_Texture; } }
        public Texture2D thumbnail { get { UpdateThumbnail(); return m_Thumbnail; } }

        public void SetDirty(bool isDirty)
        {
            m_UpdateTexture |= isDirty;
            m_UpdateThumbnail |= isDirty;
        }

        internal static Texture2D DefaultMask()
        {
            if (s_WhiteTexture == null)
            {
                s_WhiteTexture = new Texture2D(64, 64, TextureFormat.Alpha8, false);
                Color[] colors = new Color[64 * 64];
                for (int i = 0; i < 64 * 64; i++)
                    colors[i] = new Color(1, 1, 1, 1);
                s_WhiteTexture.SetPixels(colors);
                s_WhiteTexture.filterMode = FilterMode.Bilinear;
                s_WhiteTexture.Apply();
            }
            return s_WhiteTexture;
        }

        internal static Texture2D GenerateBrushTexture(Texture2D mask, AnimationCurve falloff, float radiusScale, int width, int height, bool isThumbnail = false)
        {
            if (s_CreateBrushMaterial == null)
                s_CreateBrushMaterial = new Material(EditorGUIUtility.LoadRequired("Brushes/CreateBrush.shader") as Shader);

            int sampleCount = Mathf.Max(width, 1024);
            Texture2D falloffTex = new Texture2D(sampleCount, 1, TextureFormat.R16, false);
            Color[] falloffPix = new Color[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / (float)(sampleCount - 1);
                float val = falloff.Evaluate(time);
                falloffPix[sampleCount - i - 1].r = falloffPix[sampleCount - i - 1].g = falloffPix[sampleCount - i - 1].b = falloffPix[sampleCount - i - 1].a = val;
            }
            falloffTex.SetPixels(falloffPix);
            falloffTex.wrapMode = TextureWrapMode.Clamp;
            falloffTex.filterMode = FilterMode.Bilinear;
            falloffTex.Apply();

            RenderTexture oldRT = RenderTexture.active;
            RenderTextureFormat outputRenderFormat = isThumbnail ? RenderTextureFormat.ARGB32 : Terrain.heightmapRenderTextureFormat;
            TextureFormat outputTexFormat = isThumbnail ? TextureFormat.ARGB32 : Terrain.heightmapTextureFormat;

            // build brush texture
            Vector4 brushParams = new Vector4(radiusScale * 0.5f, mask.format != TextureFormat.Alpha8 ? 1.0f : 0.0f, 0.0f, 0.0f);
            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, outputRenderFormat);
            s_CreateBrushMaterial.SetTexture("_BrushFalloff", falloffTex);
            s_CreateBrushMaterial.SetVector("_BrushParams", brushParams);
            Graphics.Blit(mask, tempRT, s_CreateBrushMaterial);

            Texture2D previewTexture = new Texture2D(width, height, outputTexFormat, false);

            RenderTexture.active = tempRT;
            previewTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            previewTexture.Apply();

            RenderTexture.ReleaseTemporary(tempRT);
            tempRT = null;

            RenderTexture.active = oldRT;
            return previewTexture;
        }
    }
}
