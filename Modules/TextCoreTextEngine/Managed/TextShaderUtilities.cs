// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.Bindings;


namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Property IDs for the various shader properties used by TextCore.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public static class TextShaderUtilities
    {
        // Shader Property IDs
        public static int ID_MainTex;

        public static int ID_FaceTex;
        public static int ID_FaceColor;
        public static int ID_FaceDilate;
        public static int ID_Shininess;

        /// <summary>
        /// Property ID for the _OutlineOffset1 shader property used by URP and HDRP shaders
        /// </summary>
        public static int ID_OutlineOffset1;

        /// <summary>
        /// Property ID for the _OutlineOffset2 shader property used by URP and HDRP shaders
        /// </summary>
        public static int ID_OutlineOffset2;

        /// <summary>
        /// Property ID for the _OutlineOffset3 shader property used by URP and HDRP shaders
        /// </summary>
        public static int ID_OutlineOffset3;

        /// <summary>
        /// Property ID for the ID_AdditiveOutlineMode shader property used by URP and HDRP shaders
        /// </summary>
        public static int ID_OutlineMode;

        /// <summary>
        /// Property ID for the _IsoPerimeter shader property used by URP and HDRP shaders
        /// </summary>
        public static int ID_IsoPerimeter;

        /// <summary>
        /// Property ID for the _Softness shader property used by URP and HDRP shaders
        /// </summary>
        public static int ID_Softness;

        public static int ID_UnderlayColor;
        public static int ID_UnderlayOffsetX;
        public static int ID_UnderlayOffsetY;
        public static int ID_UnderlayDilate;
        public static int ID_UnderlaySoftness;

        /// <summary>
        /// Property ID for the _UnderlayOffset shader property used by URP and HDRP shaders
        /// </summary>
        public static int ID_UnderlayOffset;

        /// <summary>
        /// Property ID for the _UnderlayIsoPerimeter shader property used by URP and HDRP shaders
        /// </summary>
        public static int ID_UnderlayIsoPerimeter;

        public static int ID_WeightNormal;
        public static int ID_WeightBold;

        public static int ID_OutlineTex;
        public static int ID_OutlineWidth;
        public static int ID_OutlineSoftness;
        public static int ID_OutlineColor;

        public static int ID_Outline2Color;
        public static int ID_Outline2Width;

        public static int ID_Padding;
        public static int ID_GradientScale;
        public static int ID_ScaleX;
        public static int ID_ScaleY;
        public static int ID_PerspectiveFilter;
        public static int ID_Sharpness;

        public static int ID_TextureWidth;
        public static int ID_TextureHeight;

        public static int ID_BevelAmount;

        public static int ID_GlowColor;
        public static int ID_GlowOffset;
        public static int ID_GlowPower;
        public static int ID_GlowOuter;
        public static int ID_GlowInner;

        public static int ID_LightAngle;

        public static int ID_EnvMap;
        public static int ID_EnvMatrix;
        public static int ID_EnvMatrixRotation;

        //public static int ID_MaskID;
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
        //public static string Keyword_MASK_OFF = "MASK_OFF";
        public static string Keyword_MASK_SOFT = "MASK_SOFT";
        public static string Keyword_MASK_HARD = "MASK_HARD";
        public static string Keyword_MASK_TEX = "MASK_TEX";
        public static string Keyword_Outline = "OUTLINE_ON";

        public static string ShaderTag_ZTestMode = "unity_GUIZTestMode";
        public static string ShaderTag_CullMode = "_CullMode";

        private static float m_clamp = 1.0f;
        public static bool isInitialized = false;


        /// <summary>
        /// Returns a reference to the mobile distance field shader.
        /// </summary>
        internal static Shader ShaderRef_MobileSDF
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            get
            {
                if (((System.Object)k_ShaderRef_MobileSDF) == null)
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


        internal static Shader ShaderRef_Sprite
        {
            get
            {
                if (k_ShaderRef_Sprite == null)
                {
                    k_ShaderRef_Sprite = Shader.Find("Text/Sprite");

                    if (k_ShaderRef_Sprite == null)
                        k_ShaderRef_Sprite = Shader.Find("Hidden/TextCore/Sprite");
                }
                return k_ShaderRef_Sprite;
            }
        }
        static Shader k_ShaderRef_Sprite;


        /// <summary>
        ///
        /// </summary>
        static TextShaderUtilities()
        {
            GetShaderPropertyIDs();
        }

        /// <summary>
        ///
        /// </summary>
        internal static void GetShaderPropertyIDs()
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

                ID_OutlineOffset1 = Shader.PropertyToID("_OutlineOffset1");
                ID_OutlineOffset2 = Shader.PropertyToID("_OutlineOffset2");
                ID_OutlineOffset3 = Shader.PropertyToID("_OutlineOffset3");
                ID_OutlineMode = Shader.PropertyToID("_OutlineMode");

                ID_IsoPerimeter = Shader.PropertyToID("_IsoPerimeter");
                ID_Softness = Shader.PropertyToID("_Softness");

                ID_UnderlayColor = Shader.PropertyToID("_UnderlayColor");
                ID_UnderlayOffsetX = Shader.PropertyToID("_UnderlayOffsetX");
                ID_UnderlayOffsetY = Shader.PropertyToID("_UnderlayOffsetY");
                ID_UnderlayDilate = Shader.PropertyToID("_UnderlayDilate");
                ID_UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");

                ID_UnderlayOffset = Shader.PropertyToID("_UnderlayOffset");
                ID_UnderlayIsoPerimeter = Shader.PropertyToID("_UnderlayIsoPerimeter");

                ID_WeightNormal = Shader.PropertyToID("_WeightNormal");
                ID_WeightBold = Shader.PropertyToID("_WeightBold");

                ID_OutlineTex = Shader.PropertyToID("_OutlineTex");
                ID_OutlineWidth = Shader.PropertyToID("_OutlineWidth");
                ID_OutlineSoftness = Shader.PropertyToID("_OutlineSoftness");
                ID_OutlineColor = Shader.PropertyToID("_OutlineColor");

                ID_Outline2Color = Shader.PropertyToID("_Outline2Color");
                ID_Outline2Width = Shader.PropertyToID("_Outline2Width");

                ID_Padding = Shader.PropertyToID("_Padding");
                ID_GradientScale = Shader.PropertyToID("_GradientScale");
                ID_ScaleX = Shader.PropertyToID("_ScaleX");
                ID_ScaleY = Shader.PropertyToID("_ScaleY");
                ID_PerspectiveFilter = Shader.PropertyToID("_PerspectiveFilter");
                ID_Sharpness = Shader.PropertyToID("_Sharpness");

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
                ID_GlowInner = Shader.PropertyToID("_GlowInner");

                //ID_MaskID = Shader.PropertyToID("_MaskID");
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
        static void UpdateShaderRatios(Material mat)
        {
            //Debug.Log("UpdateShaderRatios() called.");

            float ratio_A = 1;
            float ratio_B = 1;
            float ratio_C = 1;

            bool isRatioEnabled = !mat.shaderKeywords.Contains(Keyword_Ratios);

            if (!mat.HasProperty(ID_GradientScale) || !mat.HasProperty(ID_FaceDilate))
                return;

            // Compute Ratio A
            float scale = mat.GetFloat(ID_GradientScale);
            float faceDilate = mat.GetFloat(ID_FaceDilate);
            float outlineThickness = mat.GetFloat(ID_OutlineWidth);
            float outlineSoftness = mat.GetFloat(ID_OutlineSoftness);

            float weight = Mathf.Max(mat.GetFloat(ID_WeightNormal), mat.GetFloat(ID_WeightBold)) / 4.0f;

            float t = Mathf.Max(1, weight + faceDilate + outlineThickness + outlineSoftness);

            ratio_A = isRatioEnabled ? (scale - m_clamp) / (scale * t) : 1;

            //float ratio_A_old = mat.GetFloat(ID_ScaleRatio_A);

            // Only set the ratio if it has changed.
            //if (ratio_A != ratio_A_old)
            mat.SetFloat(ID_ScaleRatio_A, ratio_A);

            // Compute Ratio B
            if (mat.HasProperty(ID_GlowOffset))
            {
                float glowOffset = mat.GetFloat(ID_GlowOffset);
                float glowOuter = mat.GetFloat(ID_GlowOuter);

                float range = (weight + faceDilate) * (scale - m_clamp);

                t = Mathf.Max(1, glowOffset + glowOuter);

                ratio_B = isRatioEnabled ? Mathf.Max(0, scale - m_clamp - range) / (scale * t) : 1;
                //float ratio_B_old = mat.GetFloat(ID_ScaleRatio_B);

                // Only set the ratio if it has changed.
                //if (ratio_B != ratio_B_old)
                mat.SetFloat(ID_ScaleRatio_B, ratio_B);
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

                ratio_C = isRatioEnabled ? Mathf.Max(0, scale - m_clamp - range) / (scale * t) : 1;
                //float ratio_C_old = mat.GetFloat(ID_ScaleRatio_C);

                // Only set the ratio if it has changed.
                //if (ratio_C != ratio_C_old)
                mat.SetFloat(ID_ScaleRatio_C, ratio_C);
            }
        }

        // Function to calculate padding required for Outline Width & Dilation for proper text alignment
        internal static Vector4 GetFontExtent(Material material)
        {
            // Revised implementation where style no longer affects alignment
            return Vector4.zero;

            /*
            if (material == null || !material.HasProperty(TextShaderUtilities.ID_GradientScale))
                return Vector4.zero;   // We are using an non SDF Shader.

            float scaleRatioA = material.GetFloat(ID_ScaleRatio_A);
            float faceDilate = material.GetFloat(ID_FaceDilate) * scaleRatioA;
            float outlineThickness = material.GetFloat(ID_OutlineWidth) * scaleRatioA;

            float extent = Mathf.Min(1, faceDilate + outlineThickness);
            extent *= material.GetFloat(ID_GradientScale);

            return new Vector4(extent, extent, extent, extent);
            */
        }

        // Function to check if Masking is enabled
        internal static bool IsMaskingEnabled(Material material)
        {
            if (material == null || !material.HasProperty(ID_ClipRect))
                return false;

            if (material.shaderKeywords.Contains(Keyword_MASK_SOFT) || material.shaderKeywords.Contains(Keyword_MASK_HARD) || material.shaderKeywords.Contains(Keyword_MASK_TEX))
                return true;

            return false;
        }

        // Function to determine how much extra padding is required as a result of material properties like dilate, outline thickness, softness, glow, etc...
        internal static float GetPadding(Material material, bool enableExtraPadding, bool isBold)
        {
            //Debug.Log("GetPadding() called.");

            if (isInitialized == false)
                GetShaderPropertyIDs();

            // Return if Material is null
            if (material == null) return 0;

            int extraPadding = enableExtraPadding ? 4 : 0;

            // Check if we are using a non Distance Field Shader
            if (material.HasProperty(ID_GradientScale) == false)
            {
                if (material.HasProperty(ID_Padding))
                    extraPadding += (int)material.GetFloat(ID_Padding);

                return extraPadding + 1.0f;
            }

            // Special handling for new SRP Shaders
            if (material.HasProperty(ID_IsoPerimeter))
            {
                return ComputePaddingForProperties(material) + 0.25f + extraPadding;
            }

            Vector4 padding = Vector4.zero;
            Vector4 maxPadding = Vector4.zero;

            //float weight = 0;
            float faceDilate = 0;
            float faceSoftness = 0;
            float outlineThickness = 0;
            float scaleRatio_A = 0;
            float scaleRatio_B = 0;
            float scaleRatio_C = 0;

            float glowOffset = 0;
            float glowOuter = 0;

            float gradientScale = 0;
            float uniformPadding = 0;
            // Iterate through each of the assigned materials to find the max values to set the padding.

            // Update Shader Ratios prior to computing padding
            UpdateShaderRatios(material);

            string[] shaderKeywords = material.shaderKeywords;

            if (material.HasProperty(ID_ScaleRatio_A))
                scaleRatio_A = material.GetFloat(ID_ScaleRatio_A);

            //weight = 0; // Mathf.Max(material.GetFloat(ID_WeightNormal), material.GetFloat(ID_WeightBold)) / 2.0f * scaleRatio_A;

            if (material.HasProperty(ID_FaceDilate))
                faceDilate = material.GetFloat(ID_FaceDilate) * scaleRatio_A;

            if (material.HasProperty(ID_OutlineSoftness))
                faceSoftness = material.GetFloat(ID_OutlineSoftness) * scaleRatio_A;

            if (material.HasProperty(ID_OutlineWidth))
                outlineThickness = material.GetFloat(ID_OutlineWidth) * scaleRatio_A;

            uniformPadding = outlineThickness + faceSoftness + faceDilate;

            // Glow padding contribution
            if (material.HasProperty(ID_GlowOffset) && shaderKeywords.Contains(Keyword_Glow)) // Generates GC
            {
                if (material.HasProperty(ID_ScaleRatio_B))
                    scaleRatio_B = material.GetFloat(ID_ScaleRatio_B);

                glowOffset = material.GetFloat(ID_GlowOffset) * scaleRatio_B;
                glowOuter = material.GetFloat(ID_GlowOuter) * scaleRatio_B;
            }

            uniformPadding = Mathf.Max(uniformPadding, faceDilate + glowOffset + glowOuter);

            // Underlay padding contribution
            if (material.HasProperty(ID_UnderlaySoftness) && shaderKeywords.Contains(Keyword_Underlay)) // Generates GC
            {
                if (material.HasProperty(ID_ScaleRatio_C))
                    scaleRatio_C = material.GetFloat(ID_ScaleRatio_C);

                float offsetX = 0;
                float offsetY = 0;
                float dilate = 0;
                float softness = 0;

                if (material.HasProperty(ID_UnderlayOffset))
                {
                    Vector2 underlayOffset = material.GetVector(ID_UnderlayOffset);
                    offsetX = underlayOffset.x;
                    offsetY = underlayOffset.y;

                    dilate = material.GetFloat(ID_UnderlayDilate);
                    softness = material.GetFloat(ID_UnderlaySoftness);
                }
                else if (material.HasProperty(ID_UnderlayOffsetX))
                {
                    offsetX = material.GetFloat(ID_UnderlayOffsetX) * scaleRatio_C;
                    offsetY = material.GetFloat(ID_UnderlayOffsetY) * scaleRatio_C;
                    dilate = material.GetFloat(ID_UnderlayDilate) * scaleRatio_C;
                    softness = material.GetFloat(ID_UnderlaySoftness) * scaleRatio_C;
                }

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

            gradientScale = material.GetFloat(ID_GradientScale);
            padding *= gradientScale;

            // Set UniformPadding to the maximum value of any of its components.
            uniformPadding = Mathf.Max(padding.x, padding.y);
            uniformPadding = Mathf.Max(padding.z, uniformPadding);
            uniformPadding = Mathf.Max(padding.w, uniformPadding);

            return uniformPadding + 1.25f;
        }

        static float ComputePaddingForProperties(Material mat)
        {
            Vector4 dilation = mat.GetVector(ID_IsoPerimeter);
            Vector2 outlineOffset1 = mat.GetVector(ID_OutlineOffset1);
            Vector2 outlineOffset2 = mat.GetVector(ID_OutlineOffset2);
            Vector2 outlineOffset3 = mat.GetVector(ID_OutlineOffset3);
            bool isOutlineModeEnabled = mat.GetFloat(ID_OutlineMode) != 0;

            Vector4 softness = mat.GetVector(ID_Softness);
            float gradientScale = mat.GetFloat(ID_GradientScale);

            // Face
            float padding = Mathf.Max(0, dilation.x + softness.x * 0.5f);

            // Outlines
            if (!isOutlineModeEnabled)
            {
                padding = Mathf.Max(padding, dilation.y + softness.y * 0.5f + Mathf.Max(Mathf.Abs(outlineOffset1.x), Mathf.Abs(outlineOffset1.y)));
                padding = Mathf.Max(padding, dilation.z + softness.z * 0.5f + Mathf.Max(Mathf.Abs(outlineOffset2.x), Mathf.Abs(outlineOffset2.y)));
                padding = Mathf.Max(padding, dilation.w + softness.w * 0.5f + Mathf.Max(Mathf.Abs(outlineOffset3.x), Mathf.Abs(outlineOffset3.y)));
            }
            else
            {
                float offsetOutline1 = Mathf.Max(Mathf.Abs(outlineOffset1.x), Mathf.Abs(outlineOffset1.y));
                float offsetOutline2 = Mathf.Max(Mathf.Abs(outlineOffset2.x), Mathf.Abs(outlineOffset2.y));

                padding = Mathf.Max(padding, dilation.y + softness.y * 0.5f + offsetOutline1);
                padding = Mathf.Max(padding, dilation.z + softness.z * 0.5f + offsetOutline2);

                float maxOffset = Mathf.Max(offsetOutline1, offsetOutline2);
                padding += Mathf.Max(0 ,(dilation.w + softness.w * 0.5f) - Mathf.Max(0, padding - maxOffset));
            }

            // Underlay
            Vector2 underlayOffset = mat.GetVector(ID_UnderlayOffset);
            float underlayDilation = mat.GetFloat(ID_UnderlayDilate);
            float underlaySoftness = mat.GetFloat(ID_UnderlaySoftness);
            padding = Mathf.Max(padding, underlayDilation + underlaySoftness * 0.5f + Mathf.Max(Mathf.Abs(underlayOffset.x), Mathf.Abs(underlayOffset.y)));

            return padding * gradientScale;
        }

        // Function to determine how much extra padding is required as a result of material properties like dilate, outline thickness, softness, glow, etc...
        internal static float GetPadding(Material[] materials, bool enableExtraPadding, bool isBold)
        {
            //Debug.Log("GetPadding() called.");

            if (isInitialized == false)
                GetShaderPropertyIDs();

            // Return if Material is null
            if (materials == null) return 0;

            int extraPadding = enableExtraPadding ? 4 : 0;

            // Check if we are using a Bitmap Shader
            if (materials[0].HasProperty(ID_Padding))
                return extraPadding + materials[0].GetFloat(ID_Padding);

            Vector4 padding = Vector4.zero;
            Vector4 maxPadding = Vector4.zero;

            float faceDilate = 0;
            float faceSoftness = 0;
            float outlineThickness = 0;
            float scaleRatio_A = 0;
            float scaleRatio_B = 0;
            float scaleRatio_C = 0;

            float glowOffset = 0;
            float glowOuter = 0;

            float uniformPadding = 0;
            // Iterate through each of the assigned materials to find the max values to set the padding.
            for (int i = 0; i < materials.Length; i++)
            {
                // Update Shader Ratios prior to computing padding
                UpdateShaderRatios(materials[i]);

                string[] shaderKeywords = materials[i].shaderKeywords;

                if (materials[i].HasProperty(ID_ScaleRatio_A))
                    scaleRatio_A = materials[i].GetFloat(ID_ScaleRatio_A);

                if (materials[i].HasProperty(ID_FaceDilate))
                    faceDilate = materials[i].GetFloat(ID_FaceDilate) * scaleRatio_A;

                if (materials[i].HasProperty(ID_OutlineSoftness))
                    faceSoftness = materials[i].GetFloat(ID_OutlineSoftness) * scaleRatio_A;

                if (materials[i].HasProperty(ID_OutlineWidth))
                    outlineThickness = materials[i].GetFloat(ID_OutlineWidth) * scaleRatio_A;

                uniformPadding = outlineThickness + faceSoftness + faceDilate;

                // Glow padding contribution
                if (materials[i].HasProperty(ID_GlowOffset) && shaderKeywords.Contains(Keyword_Glow))
                {
                    if (materials[i].HasProperty(ID_ScaleRatio_B))
                        scaleRatio_B = materials[i].GetFloat(ID_ScaleRatio_B);

                    glowOffset = materials[i].GetFloat(ID_GlowOffset) * scaleRatio_B;
                    glowOuter = materials[i].GetFloat(ID_GlowOuter) * scaleRatio_B;
                }

                uniformPadding = Mathf.Max(uniformPadding, faceDilate + glowOffset + glowOuter);

                // Underlay padding contribution
                if (materials[i].HasProperty(ID_UnderlaySoftness) && shaderKeywords.Contains(Keyword_Underlay))
                {
                    if (materials[i].HasProperty(ID_ScaleRatio_C))
                        scaleRatio_C = materials[i].GetFloat(ID_ScaleRatio_C);

                    float offsetX = materials[i].GetFloat(ID_UnderlayOffsetX) * scaleRatio_C;
                    float offsetY = materials[i].GetFloat(ID_UnderlayOffsetY) * scaleRatio_C;
                    float dilate = materials[i].GetFloat(ID_UnderlayDilate) * scaleRatio_C;
                    float softness = materials[i].GetFloat(ID_UnderlaySoftness) * scaleRatio_C;

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
            }

            float gradientScale = materials[0].GetFloat(ID_GradientScale);
            padding *= gradientScale;

            // Set UniformPadding to the maximum value of any of its components.
            uniformPadding = Mathf.Max(padding.x, padding.y);
            uniformPadding = Mathf.Max(padding.z, uniformPadding);
            uniformPadding = Mathf.Max(padding.w, uniformPadding);

            return uniformPadding + 0.25f;
        }
    }
}
