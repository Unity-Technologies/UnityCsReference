// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.Lighting.Utilities
{
    static class TextureExposure
    {
        const string k_ExposureHistogramPath = "Shaders/TexturesExposureHistogram.compute";
        const string k_MinMaxKernelName = "GetMinMaxLuminance";
        const string k_HistogramKernelName = "GetHistogram";
        const float k_MiddleGrey = 0.18f;

        struct UVShaderProperties
        {
            public static readonly int k_Mip = Shader.PropertyToID("_Mip");
            public static readonly int k_Exposure = Shader.PropertyToID("_Exposure");
            public static readonly int k_ManualTex2Srgb = Shader.PropertyToID("_ManualTex2SRGB");
        }

        struct TextureHistogramShaderProperties
        {
            public static readonly int k_TextureCount = Shader.PropertyToID("textureCount");
            public static readonly int k_HistogramBins = Shader.PropertyToID("histogramBins");
            public static readonly int k_TextureSize = Shader.PropertyToID("textureSize");
            public static readonly int k_AdjustForGamma = Shader.PropertyToID("adjustForGamma");
            public static readonly int k_ToIntFactor = Shader.PropertyToID("toIntFactor");
            public static readonly int k_TextureArray = Shader.PropertyToID("textureArray");
            public static readonly int k_LumMinMax = Shader.PropertyToID("LumMinMax");
            public static readonly int k_Histogram = Shader.PropertyToID("Histogram");
        }

        struct LuminanceHistogramData
        {
            public float min;
            public float max;
            public int[] histogram;
            public int numBins;
        }

        static ComputeBuffer CreateStructuredBuffer<T>(int count)
        {
            return new ComputeBuffer(count, GetStride<T>());
        }

        static int GetStride<T>()
        {
            return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        }

        static void Dispatch(ComputeShader cs, int numIterationsX, int numIterationsY = 1, int numIterationsZ = 1, int kernelIndex = 0)
        {
            Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
            int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
            int numGroupsY = Mathf.CeilToInt(numIterationsY / (float)threadGroupSizes.y);
            int numGroupsZ = Mathf.CeilToInt(numIterationsZ / (float)threadGroupSizes.z);
            cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
        }

        static Vector3Int GetThreadGroupSizes(ComputeShader compute, int kernelIndex = 0)
        {
            uint x, y, z;
            compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
            return new Vector3Int((int)x, (int)y, (int)z);
        }

        internal static Texture2DArray GetUniformSizeTextureArray(Texture2D[] textures)
        {
            if (textures == null || textures.Length == 0)
                return null;

            var targetWidth = 128;
            var targetHeight = 128;

            Texture2DArray textureArray = new Texture2DArray(
                targetWidth, targetHeight, textures.Length, TextureFormat.RGBAFloat, false, true);

            RenderTexture tempRT = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGBFloat);

            try
            {
                for (int i = 0; i < textures.Length; i++)
                {
                    if (textures[i] != null)
                    {
                        Graphics.Blit(textures[i], tempRT);
                        Graphics.CopyTexture(tempRT, 0, 0, textureArray, i, 0);
                    }
                }
            }
            finally
            {
                RenderTexture.ReleaseTemporary(tempRT);
            }

            return textureArray;
        }

        static float GetExposureFromTargetLum(LuminanceHistogramData histogramData, int totalSamples, float targetLuminance = k_MiddleGrey)
        {
            float targetPercentile = 0.5f;
            float cumulative = 0f;
            int percentileBin = 0;

            for (int i = 0; i < histogramData.numBins; i++)
            {
                cumulative += (float)histogramData.histogram[i] / totalSamples;
                if (cumulative >= targetPercentile)
                {
                    percentileBin = i;
                    break;
                }
            }
            float binWidth = (histogramData.max - histogramData.min) / histogramData.numBins;
            float medianLuminance = histogramData.min + (percentileBin + 0.5f) * binWidth;
            float exposureValue = Mathf.Log(targetLuminance / Mathf.Max(medianLuminance, 0.0001f), 2.0f);

            return exposureValue;
        }

        static LuminanceHistogramData GetLuminanceHistogram(Texture2DArray textures)
        {
            ComputeShader exposureHistogramShader = EditorResources.Load<ComputeShader>(k_ExposureHistogramPath);
            LuminanceHistogramData histogramData = new LuminanceHistogramData() { numBins = 256 };
            int maxWidth = textures.width;
            int maxHeight = textures.height;
            int textureCount = textures.depth;

            int minMaxKernel = exposureHistogramShader.FindKernel(k_MinMaxKernelName);
            int histogramKernel = exposureHistogramShader.FindKernel(k_HistogramKernelName);

            exposureHistogramShader.SetInt(TextureHistogramShaderProperties.k_TextureCount, textureCount);
            exposureHistogramShader.SetInt(TextureHistogramShaderProperties.k_HistogramBins, histogramData.numBins);
            exposureHistogramShader.SetInts(
                TextureHistogramShaderProperties.k_TextureSize,
                maxWidth,
                maxHeight);
            exposureHistogramShader.SetBool(TextureHistogramShaderProperties.k_AdjustForGamma, false);
            exposureHistogramShader.SetInt(TextureHistogramShaderProperties.k_ToIntFactor, 1000);
            exposureHistogramShader.SetTexture(minMaxKernel, TextureHistogramShaderProperties.k_TextureArray, textures);
            exposureHistogramShader.SetTexture(histogramKernel, TextureHistogramShaderProperties.k_TextureArray, textures);

            ComputeBuffer minMaxBuffer = null;
            ComputeBuffer histogramBuffer = null;
            try
            {
                minMaxBuffer = CreateStructuredBuffer<int>(2);
                histogramBuffer = CreateStructuredBuffer<int>(histogramData.numBins);

                minMaxBuffer.SetData(new[] { int.MaxValue, int.MinValue });
                histogramBuffer.SetData(new int[histogramData.numBins]);

                exposureHistogramShader.SetBuffer(minMaxKernel, TextureHistogramShaderProperties.k_LumMinMax, minMaxBuffer);
                exposureHistogramShader.SetBuffer(histogramKernel, TextureHistogramShaderProperties.k_Histogram, histogramBuffer);
                exposureHistogramShader.SetBuffer(histogramKernel, TextureHistogramShaderProperties.k_LumMinMax, minMaxBuffer);

                Dispatch(
                    exposureHistogramShader,
                    maxWidth,
                    maxHeight,
                    textureCount,
                    minMaxKernel
                );
                var minMaxRaw = new int[2];
                minMaxBuffer.GetData(minMaxRaw);
                histogramData.min = minMaxRaw[0] / (float)1000;
                histogramData.max = minMaxRaw[1] / (float)1000;

                Dispatch(
                    exposureHistogramShader,
                    maxWidth,
                    maxHeight,
                    textureCount,
                    histogramKernel
                );

                histogramData.histogram = new int[histogramData.numBins];
                histogramBuffer.GetData(histogramData.histogram);
            }
            finally
            {
                minMaxBuffer?.Release();
                histogramBuffer?.Release();
            }

            return histogramData;
        }

        public static float GetAutoExposure(Texture2DArray textureArray, float targetLum)
        {
            var histogramData = GetLuminanceHistogram(textureArray);
            int totalSamples = textureArray.width * textureArray.height * textureArray.depth;
            return GetExposureFromTargetLum(histogramData, totalSamples, targetLum);
        }

        public static Texture2D GetTextureUVOverlay(Texture texture, float exposure)
        {
            RenderTexture savedRT = RenderTexture.active;
            RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Material mat = EditorGUI.GetMaterialForSpecialTexture(texture, EditorGUI.colorMaterial);

            mat.SetFloat(UVShaderProperties.k_Mip, -1);
            mat.SetFloat(UVShaderProperties.k_Exposure, exposure);
            mat.SetInt(UVShaderProperties.k_ManualTex2Srgb, 0);
            Graphics.Blit(texture, tmp, mat);

            RenderTexture.active = tmp;
            Texture2D exposureTexture = new Texture2D(tmp.width, tmp.height, TextureFormat.RGBA64, false);

            exposureTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            exposureTexture.Apply();
            RenderTexture.ReleaseTemporary(tmp);
            EditorGUIUtility.SetRenderTextureNoViewport(savedRT);

            return exposureTexture;
        }
    }
}
