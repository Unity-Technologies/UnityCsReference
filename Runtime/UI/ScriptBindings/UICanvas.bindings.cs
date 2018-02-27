// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum RenderMode
    {
        ScreenSpaceOverlay = 0,
        ScreenSpaceCamera = 1,
        WorldSpace = 2
    }

    [Flags]
    public enum AdditionalCanvasShaderChannels
    {
        None = 0,
        TexCoord1 = 1 << 0,
        TexCoord2 = 1 << 1,
        TexCoord3 = 1 << 2,
        Normal = 1 << 3,
        Tangent = 1 << 4
    }

    [RequireComponent(typeof(RectTransform)),
     NativeClass("UI::Canvas"),
     NativeHeader("Runtime/UI/Canvas.h"),
     NativeHeader("Runtime/UI/UIStructs.h")]
    public sealed class Canvas : Behaviour
    {
        public delegate void WillRenderCanvases();
        public static event WillRenderCanvases willRenderCanvases;

        public extern RenderMode renderMode { get; set; }
        public extern bool isRootCanvas { get; }
        public extern Rect pixelRect { get; }
        public extern float scaleFactor { get; set; }
        public extern float referencePixelsPerUnit { get; set; }
        public extern bool overridePixelPerfect { get; set; }
        public extern bool pixelPerfect { get; set; }
        public extern float planeDistance { get; set; }
        public extern int renderOrder { get; }
        public extern bool overrideSorting  { get; set; }
        public extern int sortingOrder  { get; set; }
        public extern int targetDisplay  { get; set; }
        public extern int sortingLayerID { get; set; }
        public extern int cachedSortingLayerValue { get; }
        public extern AdditionalCanvasShaderChannels additionalShaderChannels { get; set; }
        public extern string sortingLayerName { get; set; }
        public extern Canvas rootCanvas { get; }

        [NativeProperty("Camera", false, TargetType.Function)] public extern Camera worldCamera { get; set; }
        [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)] public extern float normalizedSortingGridSize { get; set; }

        [Obsolete("Setting normalizedSize via a int is not supported. Please use normalizedSortingGridSize", false)]
        [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)] public extern int sortingGridNormalizedSize { get; set; }

        [Obsolete("Shared default material now used for text and general UI elements, call Canvas.GetDefaultCanvasMaterial()", false)]
        [FreeFunction("UI::GetDefaultUIMaterial")] public static extern Material GetDefaultCanvasTextMaterial();

        [FreeFunction("UI::GetDefaultUIMaterial")] public static extern Material GetDefaultCanvasMaterial();
        [FreeFunction("UI::GetETC1SupportedCanvasMaterial")] public static extern Material GetETC1SupportedCanvasMaterial();

        public static void ForceUpdateCanvases()
        {
            SendWillRenderCanvases();
        }

        [RequiredByNativeCode]
        private static void SendWillRenderCanvases()
        {
            willRenderCanvases?.Invoke();
        }
    }

    [NativeHeader("Runtime/UI/Canvas.h"),
     StaticAccessor("UI::SystemProfilerApi", StaticAccessorType.DoubleColon)]
    public static class UISystemProfilerApi
    {
        public enum SampleType
        {
            Layout,
            Render
        }

        public static extern void BeginSample(SampleType type);
        public static extern void EndSample(SampleType type);
        public static extern void AddMarker(string name, Object obj);
    }
}
