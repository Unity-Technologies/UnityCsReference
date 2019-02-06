// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEngine.TextCore
{
    static class ShaderUtilities
    {
        // Shader Property IDs
        public static int ID_MainTex;

        public static int ID_FaceTex;
        public static int ID_FaceColor;
        public static int ID_FaceDilate;
        public static int ID_Shininess;

        public static int ID_UnderlayColor;
        public static int ID_UnderlayOffsetX;
        public static int ID_UnderlayOffsetY;
        public static int ID_UnderlayDilate;
        public static int ID_UnderlaySoftness;

        public static int ID_WeightNormal;
        public static int ID_WeightBold;

        public static int ID_OutlineTex;
        public static int ID_OutlineWidth;
        public static int ID_OutlineSoftness;
        public static int ID_OutlineColor;

        public static int ID_GradientScale;
        public static int ID_ScaleX;
        public static int ID_ScaleY;
        public static int ID_PerspectiveFilter;

        public static int ID_TextureWidth;
        public static int ID_TextureHeight;

        public static int ID_BevelAmount;

        public static int ID_GlowColor;
        public static int ID_GlowOffset;
        public static int ID_GlowPower;
        public static int ID_GlowOuter;

        public static int ID_LightAngle;

        public static int ID_EnvMap;
        public static int ID_EnvMatrix;
        public static int ID_EnvMatrixRotation;

        public static int ID_MaskCoord;
        public static int ID_ClipRect;
        public static int ID_MaskSoftnessX;
        public static int ID_MaskSoftnessY;
        public static int ID_VertexOffsetX;
        public static int ID_VertexOffsetY;
        public static int ID_UseClipRect;

        public static int ID_StencilID;
        public static int ID_StencilOp;
        public static int ID_StencilComp;
        public static int ID_StencilReadMask;
        public static int ID_StencilWriteMask;

        public static int ID_ShaderFlags;
        public static int ID_ScaleRatio_A;
        public static int ID_ScaleRatio_B;
        public static int ID_ScaleRatio_C;

        public static string Keyword_Bevel = "BEVEL_ON";
        public static string Keyword_Glow = "GLOW_ON";
        public static string Keyword_Underlay = "UNDERLAY_ON";
        public static string Keyword_Ratios = "RATIOS_OFF";
        public static string Keyword_MASK_SOFT = "MASK_SOFT";
        public static string Keyword_MASK_HARD = "MASK_HARD";
        public static string Keyword_MASK_TEX = "MASK_TEX";
        public static string Keyword_Outline = "OUTLINE_ON";

        public static string ShaderTag_ZTestMode = "unity_GUIZTestMode";
        public static string ShaderTag_CullMode = "_CullMode";

        static float m_clamp = 1.0f;
        public static bool isInitialized;

        /// <summary>
        /// Returns a reference to the mobile distance field shader.
        /// </summary>
        internal static Shader ShaderRef_MobileSDF
        {
            get
            {
                if (k_ShaderRef_MobileSDF == null)
                    k_ShaderRef_MobileSDF = Shader.Find("Hidden/TextCore/Distance Field SSD");

                return k_ShaderRef_MobileSDF;
            }
        }
        static Shader k_ShaderRef_MobileSDF;

        /// <summary>
        /// Returns a reference to the mobile bitmap shader.
        /// </summary>
        internal static Shader ShaderRef_MobileBitmap
        {
            get
            {
                if (k_ShaderRef_MobileBitmap == null)
                    k_ShaderRef_MobileBitmap = Shader.Find("Hidden/Internal-GUITextureClipText");

                return k_ShaderRef_MobileBitmap;
            }
        }
        static Shader k_ShaderRef_MobileBitmap;


        /// <summary>
        ///
        /// </summary>
        static ShaderUtilities()
        {
            GetShaderPropertyIDs();
        }

        /// <summary>
        ///
        /// </summary>
        public static void GetShaderPropertyIDs()
        {
            if (isInitialized == false)
            {
                //Debug.Log("Getting Shader property IDs");
                isInitialized = true;

                ID_MainTex = Shader.PropertyToID("_MainTex");

                ID_FaceTex = Shader.PropertyToID("_FaceTex");
                ID_FaceColor = Shader.PropertyToID("_FaceColor");
                ID_FaceDilate = Shader.PropertyToID("_FaceDilate");
                ID_Shininess = Shader.PropertyToID("_FaceShininess");

                ID_UnderlayColor = Shader.PropertyToID("_UnderlayColor");
                ID_UnderlayOffsetX = Shader.PropertyToID("_UnderlayOffsetX");
                ID_UnderlayOffsetY = Shader.PropertyToID("_UnderlayOffsetY");
                ID_UnderlayDilate = Shader.PropertyToID("_UnderlayDilate");
                ID_UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");

                ID_WeightNormal = Shader.PropertyToID("_WeightNormal");
                ID_WeightBold = Shader.PropertyToID("_WeightBold");

                ID_OutlineTex = Shader.PropertyToID("_OutlineTex");
                ID_OutlineWidth = Shader.PropertyToID("_OutlineWidth");
                ID_OutlineSoftness = Shader.PropertyToID("_OutlineSoftness");
                ID_OutlineColor = Shader.PropertyToID("_OutlineColor");

                ID_GradientScale = Shader.PropertyToID("_GradientScale");
                ID_ScaleX = Shader.PropertyToID("_ScaleX");
                ID_ScaleY = Shader.PropertyToID("_ScaleY");
                ID_PerspectiveFilter = Shader.PropertyToID("_PerspectiveFilter");

                ID_TextureWidth = Shader.PropertyToID("_TextureWidth");
                ID_TextureHeight = Shader.PropertyToID("_TextureHeight");

                ID_BevelAmount = Shader.PropertyToID("_Bevel");

                ID_LightAngle = Shader.PropertyToID("_LightAngle");

                ID_EnvMap = Shader.PropertyToID("_Cube");
                ID_EnvMatrix = Shader.PropertyToID("_EnvMatrix");
                ID_EnvMatrixRotation = Shader.PropertyToID("_EnvMatrixRotation");


                ID_GlowColor = Shader.PropertyToID("_GlowColor");
                ID_GlowOffset = Shader.PropertyToID("_GlowOffset");
                ID_GlowPower = Shader.PropertyToID("_GlowPower");
                ID_GlowOuter = Shader.PropertyToID("_GlowOuter");

                ID_MaskCoord = Shader.PropertyToID("_MaskCoord");
                ID_ClipRect = Shader.PropertyToID("_ClipRect");
                ID_UseClipRect = Shader.PropertyToID("_UseClipRect");
                ID_MaskSoftnessX = Shader.PropertyToID("_MaskSoftnessX");
                ID_MaskSoftnessY = Shader.PropertyToID("_MaskSoftnessY");
                ID_VertexOffsetX = Shader.PropertyToID("_VertexOffsetX");
                ID_VertexOffsetY = Shader.PropertyToID("_VertexOffsetY");

                ID_StencilID = Shader.PropertyToID("_Stencil");
                ID_StencilOp = Shader.PropertyToID("_StencilOp");
                ID_StencilComp = Shader.PropertyToID("_StencilComp");
                ID_StencilReadMask = Shader.PropertyToID("_StencilReadMask");
                ID_StencilWriteMask = Shader.PropertyToID("_StencilWriteMask");

                ID_ShaderFlags = Shader.PropertyToID("_ShaderFlags");
                ID_ScaleRatio_A = Shader.PropertyToID("_ScaleRatioA");
                ID_ScaleRatio_B = Shader.PropertyToID("_ScaleRatioB");
                ID_ScaleRatio_C = Shader.PropertyToID("_ScaleRatioC");
            }
        }

        // Scale Ratios to ensure property ranges are optimum in Material Editor
        public static void UpdateShaderRatios(Material mat)
        {
            bool isRatioEnabled = !mat.shaderKeywords.Contains(Keyword_Ratios);

            // Compute Ratio A
            float scale = mat.GetFloat(ID_GradientScale);
            float faceDilate = mat.GetFloat(ID_FaceDilate);
            float outlineThickness = mat.GetFloat(ID_OutlineWidth);
            float outlineSoftness = mat.GetFloat(ID_OutlineSoftness);

            float weight = Mathf.Max(mat.GetFloat(ID_WeightNormal), mat.GetFloat(ID_WeightBold)) / 4.0f;

            float t = Mathf.Max(1, weight + faceDilate + outlineThickness + outlineSoftness);

            float ratioA = isRatioEnabled ? (scale - m_clamp) / (scale * t) : 1;

            mat.SetFloat(ID_ScaleRatio_A, ratioA);

            // Compute Ratio B
            if (mat.HasProperty(ID_GlowOffset))
            {
                float glowOffset = mat.GetFloat(ID_GlowOffset);
                float glowOuter = mat.GetFloat(ID_GlowOuter);

                float range = (weight + faceDilate) * (scale - m_clamp);

                t = Mathf.Max(1, glowOffset + glowOuter);

                float ratioB = isRatioEnabled ? Mathf.Max(0, scale - m_clamp - range) / (scale * t) : 1;

                mat.SetFloat(ID_ScaleRatio_B, ratioB);
            }

            // Compute Ratio C
            if (mat.HasProperty(ID_UnderlayOffsetX))
            {
                float underlayOffsetX = mat.GetFloat(ID_UnderlayOffsetX);
                float underlayOffsetY = mat.GetFloat(ID_UnderlayOffsetY);
                float underlayDilate = mat.GetFloat(ID_UnderlayDilate);
                float underlaySoftness = mat.GetFloat(ID_UnderlaySoftness);

                float range = (weight + faceDilate) * (scale - m_clamp);

                t = Mathf.Max(1, Mathf.Max(Mathf.Abs(underlayOffsetX), Mathf.Abs(underlayOffsetY)) + underlayDilate + underlaySoftness);

                float ratioC = isRatioEnabled ? Mathf.Max(0, scale - m_clamp - range) / (scale * t) : 1;

                mat.SetFloat(ID_ScaleRatio_C, ratioC);
            }
        }

        // Function to check if Masking is enabled
        public static bool IsMaskingEnabled(Material material)
        {
            if (material == null || !material.HasProperty(ID_ClipRect))
                return false;

            if (material.shaderKeywords.Contains(Keyword_MASK_SOFT) || material.shaderKeywords.Contains(Keyword_MASK_HARD) || material.shaderKeywords.Contains(Keyword_MASK_TEX))
                return true;

            return false;
        }

        // Function to determine how much extra padding is required as a result of material properties like dilate, outline thickness, softness, glow, etc...
        public static float GetPadding(Material material, bool enableExtraPadding, bool isBold)
        {
            if (isInitialized == false)
                GetShaderPropertyIDs();

            // Return if Material is null
            if (material == null) return 0;

            int extraPadding = enableExtraPadding ? 4 : 0;

            if (!material.HasProperty(ID_GradientScale))
                return extraPadding;   // We are using an non SDF Shader.

            Vector4 padding = Vector4.zero;
            Vector4 maxPadding = Vector4.zero;

            //float weight = 0;
            float faceDilate = 0;
            float faceSoftness = 0;
            float outlineThickness = 0;
            float scaleRatioA = 0;
            float scaleRatioB = 0;
            float scaleRatioC = 0;

            float glowOffset = 0;
            float glowOuter = 0;

            // Iterate through each of the assigned materials to find the max values to set the padding.

            // Update Shader Ratios prior to computing padding
            UpdateShaderRatios(material);

            string[] shaderKeywords = material.shaderKeywords;

            if (material.HasProperty(ID_ScaleRatio_A))
                scaleRatioA = material.GetFloat(ID_ScaleRatio_A);

            if (material.HasProperty(ID_FaceDilate))
                faceDilate = material.GetFloat(ID_FaceDilate) * scaleRatioA;

            if (material.HasProperty(ID_OutlineSoftness))
                faceSoftness = material.GetFloat(ID_OutlineSoftness) * scaleRatioA;

            if (material.HasProperty(ID_OutlineWidth))
                outlineThickness = material.GetFloat(ID_OutlineWidth) * scaleRatioA;

            float uniformPadding = outlineThickness + faceSoftness + faceDilate;

            // Glow padding contribution
            if (material.HasProperty(ID_GlowOffset) && shaderKeywords.Contains(Keyword_Glow)) // Generates GC
            {
                if (material.HasProperty(ID_ScaleRatio_B))
                    scaleRatioB = material.GetFloat(ID_ScaleRatio_B);

                glowOffset = material.GetFloat(ID_GlowOffset) * scaleRatioB;
                glowOuter = material.GetFloat(ID_GlowOuter) * scaleRatioB;
            }

            uniformPadding = Mathf.Max(uniformPadding, faceDilate + glowOffset + glowOuter);

            // Underlay padding contribution
            if (material.HasProperty(ID_UnderlaySoftness) && shaderKeywords.Contains(Keyword_Underlay)) // Generates GC
            {
                if (material.HasProperty(ID_ScaleRatio_C))
                    scaleRatioC = material.GetFloat(ID_ScaleRatio_C);

                float offsetX = material.GetFloat(ID_UnderlayOffsetX) * scaleRatioC;
                float offsetY = material.GetFloat(ID_UnderlayOffsetY) * scaleRatioC;
                float dilate = material.GetFloat(ID_UnderlayDilate) * scaleRatioC;
                float softness = material.GetFloat(ID_UnderlaySoftness) * scaleRatioC;

                padding.x = Mathf.Max(padding.x, faceDilate + dilate + softness - offsetX);
                padding.y = Mathf.Max(padding.y, faceDilate + dilate + softness - offsetY);
                padding.z = Mathf.Max(padding.z, faceDilate + dilate + softness + offsetX);
                padding.w = Mathf.Max(padding.w, faceDilate + dilate + softness + offsetY);
            }

            padding.x = Mathf.Max(padding.x, uniformPadding);
            padding.y = Mathf.Max(padding.y, uniformPadding);
            padding.z = Mathf.Max(padding.z, uniformPadding);
            padding.w = Mathf.Max(padding.w, uniformPadding);

            padding.x += extraPadding;
            padding.y += extraPadding;
            padding.z += extraPadding;
            padding.w += extraPadding;

            padding.x = Mathf.Min(padding.x, 1);
            padding.y = Mathf.Min(padding.y, 1);
            padding.z = Mathf.Min(padding.z, 1);
            padding.w = Mathf.Min(padding.w, 1);

            maxPadding.x = maxPadding.x < padding.x ? padding.x : maxPadding.x;
            maxPadding.y = maxPadding.y < padding.y ? padding.y : maxPadding.y;
            maxPadding.z = maxPadding.z < padding.z ? padding.z : maxPadding.z;
            maxPadding.w = maxPadding.w < padding.w ? padding.w : maxPadding.w;

            float gradientScale = material.GetFloat(ID_GradientScale);
            padding *= gradientScale;

            // Set UniformPadding to the maximum value of any of its components.
            uniformPadding = Mathf.Max(padding.x, padding.y);
            uniformPadding = Mathf.Max(padding.z, uniformPadding);
            uniformPadding = Mathf.Max(padding.w, uniformPadding);

            return uniformPadding + 0.5f;
        }
    }
}
