// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Rendering;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditorInternal.FrameDebuggerInternal
{
    internal class FrameDebuggerHelper
    {
        // Constants
        private const int k_ArraySizeBitMask = 0x3FF;
        private const int k_ShaderTypeBits = (int)ShaderType.Count;

        // Properties
        internal static bool isOnLinuxOpenGL => Application.platform == RuntimePlatform.LinuxEditor && SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
        internal static Material frameDebuggerMaterial
        {
            get {
                if (s_Material == null)
                    s_Material = new Material(Resources.GetBuiltinResource<Material>("PerformanceTools/FrameDebuggerRenderTargetDisplay.mat"));

                return s_Material;
            }
        }

        internal static Material shadingRateImageMaterial
        {
            get {
                if (s_ShadingRateImageMaterial == null)
                    s_ShadingRateImageMaterial = new Material(Shader.Find("Hidden/VisualizeShadingRateImage"));
                return s_ShadingRateImageMaterial;
            }
        }

        // Utility functions...
        internal static bool IsAValidFrame(int curEventIndex, int descsLength) => (curEventIndex >= 0 && curEventIndex < descsLength);
        internal static bool IsAClearEvent(FrameEventType eventType) => eventType >= FrameEventType.ClearNone && eventType <= FrameEventType.ClearAll;
        internal static bool IsAResolveEvent(FrameEventType eventType) => eventType == FrameEventType.ResolveRT || eventType == FrameEventType.ResolveDepth;
        internal static bool IsAComputeEvent(FrameEventType eventType) => eventType == FrameEventType.ComputeDispatch;
        internal static bool IsARayTracingEvent(FrameEventType eventType) => eventType == FrameEventType.RayTracingDispatch;
        internal static bool IsAConfigureFoveatedRenderingEvent(FrameEventType eventType) => eventType == FrameEventType.ConfigureFoveatedRendering;
        internal static bool IsAHiddenEvent(FrameEventType eventType) => eventType == FrameEventType.BeginSubpass;
        internal static bool IsAHierarchyLevelBreakEvent(FrameEventType eventType) => eventType == FrameEventType.HierarchyLevelBreak;
        internal static bool IsCurrentEventMouseDown() => Event.current.type == EventType.MouseDown;
        internal static bool IsClickingRect(Rect rect) => rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown;
        internal static bool IsHoveringRect(Rect rect) => rect.Contains(Event.current.mousePosition);
        internal static bool IsARenderTexture(ref Texture t) => t != null && (t as RenderTexture) != null;
        internal static bool IsADepthTexture(ref Texture t) => IsARenderTexture(ref t) && ((t as RenderTexture).graphicsFormat == GraphicsFormat.None);
        internal static bool IsVRSSupported() => SystemInfo.supportsVariableRateShading;


        // Private Static Variables
        [NoAutoStaticsCleanup] // lazy material cache; lazy-init pattern, no code-reload-sensitive state
        private static Material s_Material = null;
        [NoAutoStaticsCleanup] // lazy material cache for VRS visualization; same pattern as s_Material
        private static Material s_ShadingRateImageMaterial = null;
        [NoAutoStaticsCleanup] // reusable scratch buffer for building shader stage strings
        private static StringBuilder s_StringBuilder = new StringBuilder();

        // Functions
        private struct ShaderPropertyIDs
        {
            internal const string _MSAA_2 = "_MSAA_2";
            internal const string _MSAA_4 = "_MSAA_4";
            internal const string _MSAA_8 = "_MSAA_8";
            internal const string _TEX2DARRAY = "_TEX2DARRAY";
            internal const string _CUBEMAP = "_CUBEMAP";
            internal static readonly int _Levels = Shader.PropertyToID("_Levels");
            internal static readonly int _MainTex = Shader.PropertyToID("_MainTex");
            internal static readonly int _MainTexDepth = Shader.PropertyToID("_MainTexDepth");
            internal static readonly int _Channels = Shader.PropertyToID("_Channels");
            internal static readonly int _ShouldYFlip = Shader.PropertyToID("_ShouldYFlip");
            internal static readonly int _UndoOutputSRGB = Shader.PropertyToID("_UndoOutputSRGB");
            internal static readonly int _MainTexWidth = Shader.PropertyToID("_MainTexWidth");
            internal static readonly int _MainTexHeight = Shader.PropertyToID("_MainTexHeight");
            internal static readonly int _VisualizationLut = Shader.PropertyToID("_VisualizationLut");
        }

        internal static void BlitToRenderTexture(
            ref Texture t,
            ref RenderTexture output,
            int width,
            int height,
            int depth,
            Vector4 channels,
            Vector4 levels,
            bool shouldYFlip,
            bool undoOutputSRGB)
        {
            if (t == null || output == null)
                return;

            output.name = t.name;
            int msaaValue = GetMSAAValue(ref t);
            TextureDimension samplerType = t.dimension;
            SetMaterialProperties(width, height, depth, samplerType, msaaValue, channels, levels, shouldYFlip, undoOutputSRGB);
            frameDebuggerMaterial.SetTexture(ShaderPropertyIDs._MainTex, t);

            Blit(ref output, frameDebuggerMaterial);
        }

        internal static void BlitToRenderTexture(
            ref RenderTexture rt,
            ref RenderTexture output,
            int width,
            int height,
            Vector4 channels,
            Vector4 levels,
            bool shouldYFlip,
            bool undoOutputSRGB)
        {
            if (rt == null || output == null)
                return;

            output.name = rt.name;

            int msaaValue = GetMSAAValue(ref rt);
            TextureDimension samplerType = rt.dimension;
            SetMaterialProperties(width, height, rt.volumeDepth, samplerType, msaaValue, channels, levels, shouldYFlip, undoOutputSRGB);
            frameDebuggerMaterial.SetTexture(ShaderPropertyIDs._MainTex, rt);

            Blit(ref output, frameDebuggerMaterial);
        }

        private static void SetKeyWord(Material mat, string keyWord, bool value)
        {
            if (value)
                mat.EnableKeyword(keyWord);
            else
                mat.DisableKeyword(keyWord);
        }

        private static void SetMaterialProperties(
            int width,
            int height,
            int depth,
            TextureDimension samplerType,
            int msaaValue,
            Vector4 channels,
            Vector4 levels,
            bool shouldYFlip,
            bool undoOutputSRGB)
        {
            Material mat = frameDebuggerMaterial;

            bool isCubeMap = samplerType == TextureDimension.Cube;
            SetKeyWord(mat, ShaderPropertyIDs._CUBEMAP, isCubeMap);
            SetKeyWord(mat, ShaderPropertyIDs._TEX2DARRAY, samplerType == TextureDimension.Tex2DArray);
            
            // Cubemaps don't support MSAA, so only enable MSAA keywords for non-cubemap textures
            SetKeyWord(mat, ShaderPropertyIDs._MSAA_2, msaaValue == 2 && !isCubeMap);
            SetKeyWord(mat, ShaderPropertyIDs._MSAA_4, msaaValue == 4 && !isCubeMap);
            SetKeyWord(mat, ShaderPropertyIDs._MSAA_8, msaaValue == 8 && !isCubeMap);

            // Set shader properties
            mat.SetFloat(ShaderPropertyIDs._MainTexWidth, width);
            mat.SetFloat(ShaderPropertyIDs._MainTexHeight, height);
            mat.SetFloat(ShaderPropertyIDs._MainTexDepth, depth);
            mat.SetVector(ShaderPropertyIDs._Channels, channels);
            mat.SetVector(ShaderPropertyIDs._Levels, levels);
            mat.SetFloat(ShaderPropertyIDs._ShouldYFlip, shouldYFlip ? 1.0f : 0.0f);
            mat.SetFloat(ShaderPropertyIDs._UndoOutputSRGB, undoOutputSRGB ? 1.0f : 0.0f);
        }

        private static void Blit(ref RenderTexture rt, Material mat)
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            // Blit to the Render Texture
            Graphics.Blit(null, rt, mat, 0);

            // Restore previously active render texture
            RenderTexture.active = currentActiveRT;
        }

        internal static void ConvertShadingRateImage(
            ref Texture t,
            ref RenderTexture output,
            int sriWidth,
            int sriHeight,
            GraphicsBuffer shadingRateLut)
        {
            if (t == null || output == null)
                return;

            output.name = t.name;

            shadingRateImageMaterial.SetFloat(ShaderPropertyIDs._MainTexWidth, sriWidth);
            shadingRateImageMaterial.SetFloat(ShaderPropertyIDs._MainTexHeight, sriHeight);
            shadingRateImageMaterial.SetTexture(ShaderPropertyIDs._MainTex, t);
            shadingRateImageMaterial.SetBuffer(ShaderPropertyIDs._VisualizationLut, shadingRateLut);

            Blit(ref output, shadingRateImageMaterial);
        }

        internal static int GetVolumeDepth(ref Texture t)
        {
            RenderTexture rt = t as RenderTexture;
            return GetVolumeDepth(ref rt);
        }

        internal static int GetVolumeDepth(ref RenderTexture rt)
        {
            if (rt != null)
                return rt.volumeDepth;

            return 1;
        }

        internal static int GetMSAAValue(ref Texture t)
        {
            RenderTexture rt = t as RenderTexture;
            return GetMSAAValue(ref rt);
        }

        internal static int GetMSAAValue(ref RenderTexture rt)
        {
            if (rt != null && rt.bindTextureMS)
                return rt.antiAliasing;

            return 1;
        }

        internal static string GetFoveatedRenderingModeString(int fovMode)
        {
            switch ((FoveatedRenderingMode)fovMode)
            {
                case FoveatedRenderingMode.Disabled:
                    return "Disabled";
                default:
                    return "Enabled";
            }
        }

        internal static string GetStencilString(int stencil)
        {
            const int k_NumOfUsedStencilBits = 8;
            return $"(0b{Convert.ToString(stencil, 2).PadLeft(k_NumOfUsedStencilBits, '0')}) {stencil}";
        }

        internal static string GetColorMask(uint mask)
        {
            string colorMask = String.Empty;
            if (mask == 0)
                colorMask = "0";
            else
            {
                if ((mask & 8) != 0)
                    colorMask += 'R';

                if ((mask & 4) != 0)
                    colorMask += 'G';

                if ((mask & 2) != 0)
                    colorMask += 'B';

                if ((mask & 1) != 0)
                    colorMask += 'A';
            }

            return colorMask;
        }

        internal static string GetColorFormat(ref Texture t)
        {
            switch (t)
            {
                case Texture2D texture2D:
                    return texture2D.format.ToString();

                case Cubemap cubemap:
                    return cubemap.format.ToString();

                case Texture2DArray texture2DArray:
                    return texture2DArray.format.ToString();

                case Texture3D texture3D:
                    return texture3D.format.ToString();

                case CubemapArray cubemapArray:
                    return cubemapArray.format.ToString();

                case RenderTexture renderTexture:
                    return renderTexture.graphicsFormat.ToString();

                case null:
                default:
                    return string.Empty;
            }
        }

        internal static string GetSize(ref Texture t)
        {
            if (t == null)
                return string.Empty;

            string size = $"{t.width}x{t.height}";

            switch (t)
            {
                case Texture2DArray texture2DArray:
                    size += $"x{texture2DArray.depth}";
                    break;

                case Texture3D texture3D:
                    size += $"x{texture3D.depth}";
                    break;

                case CubemapArray cubemapArray:
                    size += $"x{cubemapArray.cubemapCount}";
                    break;

                case RenderTexture renderTexture when renderTexture.dimension != TextureDimension.Tex2D:
                    size += $"x{renderTexture.volumeDepth}";
                    break;
            }

            return size;
        }

        internal static string GetDepthStencilFormat(ref Texture t)
        {
            // Render Textures are the only ones who have this as they are split in to 2 textures...
            if (IsARenderTexture(ref t))
                return (t as RenderTexture).depthStencilFormat.ToString();

            return FrameDebuggerStyles.EventDetails.k_NotAvailable;
        }

        internal static int GetNumberOfValuesFromFlags(int flags)
        {
            return (flags >> k_ShaderTypeBits) & k_ArraySizeBitMask;
        }

        internal static string GetShaderStageString(int flags)
        {
            s_StringBuilder.Clear();

            // Lowest bits of flags are set for each shader stage that property is used in; matching ShaderType C++ enum
            const int k_VertexShaderFlag = (1 << 1);
            const int k_FragmentShaderFlag = (1 << 2);
            const int k_GeometryShaderFlag = (1 << 3);
            const int k_HullShaderFlag = (1 << 4);
            const int k_DomainShaderFlag = (1 << 5);

            if ((flags & k_VertexShaderFlag) != 0)
                s_StringBuilder.Append("vs/");

            if ((flags & k_FragmentShaderFlag) != 0)
                s_StringBuilder.Append("fs/");

            if ((flags & k_GeometryShaderFlag) != 0)
                s_StringBuilder.Append("gs/");

            if ((flags & k_HullShaderFlag) != 0)
                s_StringBuilder.Append("hs/");

            if ((flags & k_DomainShaderFlag) != 0)
                s_StringBuilder.Append("ds/");

            if (s_StringBuilder.Length == 0)
                return FrameDebuggerStyles.EventDetails.k_NotAvailable;

            s_StringBuilder.Remove(s_StringBuilder.Length - 1, 1);
            return s_StringBuilder.ToString();
        }

        internal static void DestroyTexture(ref Texture t)
        {
            if (t == null)
                return;

            RenderTexture rt = t as RenderTexture;
            if (rt != null)
                DestroyTexture(ref rt);
            else
            {
                Texture.DestroyImmediate(t);
                t = null;
            }
        }

        internal static void DestroyTexture(ref RenderTexture rt)
        {
            if (rt == null)
                return;

            rt.Release();
            RenderTexture.DestroyImmediate(rt);
            rt = null;
        }

        internal static void ReleaseTemporaryTexture(ref RenderTexture rt)
        {
            if (rt == null)
                return;

            RenderTexture.ReleaseTemporary(rt);
            rt = null;
        }

        // May look a bit silly but seems to be a pretty fast way of doing this :)
        // https://stackoverflow.com/a/51099524
        internal static int CountDigits(int num)
        {
            if (num >= 0)
            {
                if (num < 10) return 1;
                if (num < 100) return 2;
                if (num < 1000) return 3;
                if (num < 10000) return 4;
                if (num < 100000) return 5;
                if (num < 1000000) return 6;
                if (num < 10000000) return 7;
                if (num < 100000000) return 8;
                if (num < 1000000000) return 9;
                return 10;
            }
            else
            {
                if (num > -10) return 2;
                if (num > -100) return 3;
                if (num > -1000) return 4;
                if (num > -10000) return 5;
                if (num > -100000) return 6;
                if (num > -1000000) return 7;
                if (num > -10000000) return 8;
                if (num > -100000000) return 9;
                return 10;
            }
        }

        internal static void SpliceText(ref string text, int maxWidth)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxWidth)
                return;

            StringBuilder sb = new StringBuilder(text.Length * 2);
            while (text.Length > 0)
            {
                if (text.Length <= maxWidth)
                {
                    sb.Append(text);
                    break;
                }

                string chunk = text.Substring(0, maxWidth);
                if (char.IsWhiteSpace(text[maxWidth]))
                {
                    sb.AppendLine(chunk);
                    text = text.Substring(chunk.Length + 1);
                }
                else
                {
                    int splitIndex = chunk.LastIndexOf(' ');
                    if (splitIndex != -1)
                        chunk = chunk.Substring(0, splitIndex);
                    text = text.Substring(chunk.Length + (splitIndex == -1 ? 0 : 1));
                    sb.AppendLine(chunk);
                }
            }
            text = sb.ToString();
        }
    }
}
