// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum RenderingPath
    {
        UsePlayerSettings = -1,
        VertexLit = 0,
        Forward = 1,
        DeferredLighting = 2,
        DeferredShading = 3
    }

    // Match Camera::TransparencySortMode on C++ side
    public enum TransparencySortMode
    {
        Default = 0,
        Perspective = 1,
        Orthographic = 2,
        CustomAxis = 3
    }

    // Match the TargetEyeMask enum in GfxDeviceTypes.h on C++ side
    // bitshifts must match the StereoscopicEye enum
    public enum StereoTargetEyeMask
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Both = Left | Right
    }

    [Flags]
    public enum CameraType
    {
        Game = 1,
        SceneView = 2,
        Preview = 4,
        VR = 8,
        Reflection = 16
    }

    [Flags]
    public enum ComputeBufferType
    {
        Default = 0,
        Raw = 1,
        Append = 2,
        Counter = 4,
        [System.Obsolete("Enum member DrawIndirect has been deprecated. Use IndirectArguments instead (UnityUpgradable) -> IndirectArguments", false)]
        DrawIndirect = 256,
        IndirectArguments = 256,
        [System.Obsolete("Enum member GPUMemory has been deprecated. All compute buffers now follow the behavior previously defined by this member.", false)]
        GPUMemory = 512
    }

    public enum LightType
    {
        Spot = 0,
        Directional = 1,
        Point = 2,
        Area = 3
    }

    public enum LightRenderMode
    {
        Auto = 0,
        ForcePixel = 1,
        ForceVertex = 2
    }

    public enum LightShadows
    {
        None = 0,
        Hard = 1,
        Soft = 2
    }

    public enum FogMode
    {
        Linear = 1,
        Exponential = 2,
        ExponentialSquared = 3
    }

    public enum LightmapBakeType
    {
        Realtime = 4,
        Baked = 2,
        Mixed = 1
    }

    public enum MixedLightingMode
    {
        IndirectOnly = 0,
        Shadowmask = 2,
        Subtractive = 1
    };

    [Obsolete("See QualitySettings.names, QualitySettings.SetQualityLevel, and QualitySettings.GetQualityLevel")]
    public enum QualityLevel
    {
        Fastest = 0,
        Fast = 1,
        Simple = 2,
        Good = 3,
        Beautiful = 4,
        Fantastic = 5
    }

    public enum ShadowProjection
    {
        CloseFit = 0,
        StableFit = 1
    }

    public enum ShadowQuality
    {
        Disable = 0,
        HardOnly = 1,
        All = 2
    }

    public enum ShadowResolution
    {
        Low = UnityEngine.Rendering.LightShadowResolution.Low,
        Medium = UnityEngine.Rendering.LightShadowResolution.Medium,
        High = UnityEngine.Rendering.LightShadowResolution.High,
        VeryHigh = UnityEngine.Rendering.LightShadowResolution.VeryHigh
    }

    public enum ShadowmaskMode
    {
        Shadowmask = 0,
        DistanceShadowmask = 1
    }

    public enum CameraClearFlags
    {
        Skybox = 1,
        Color = 2,
        SolidColor = 2,
        Depth = 3,
        Nothing = 4
    }

    [Flags]
    public enum DepthTextureMode
    {
        None = 0,
        Depth = 1,
        DepthNormals = 2,
        MotionVectors = 4
    }

    public enum TexGenMode
    {
        None  = 0,
        SphereMap = 1,
        Object = 2,
        EyeLinear = 3,
        CubeReflect = 4,
        CubeNormal = 5
    }

    public enum AnisotropicFiltering
    {
        Disable = 0,
        Enable = 1,
        ForceEnable = 2
    }

    public enum BlendWeights
    {
        OneBone = 1,
        TwoBones = 2,
        FourBones = 4
    }

    public enum MeshTopology
    {
        Triangles = 0,
        Quads = 2,
        Lines = 3,
        LineStrip = 4,
        Points = 5
    }

    public enum SkinQuality
    {
        Auto = 0,
        Bone1 = 1,
        Bone2 = 2,
        Bone4 = 4
    }

    public enum ColorSpace
    {
        Uninitialized = -1,
        Gamma = 0,
        Linear = 1
    }

    // Match ColorGamut on C++ side
    [UsedByNativeCode]
    public enum ColorGamut
    {
        sRGB = 0,
        Rec709 = 1,
        Rec2020 = 2,
        DisplayP3 = 3,
        HDR10 = 4,
        DolbyHDR = 5
    };

    public enum ScreenOrientation
    {
        Unknown = 0,
        Portrait = 1,
        PortraitUpsideDown = 2,
        LandscapeLeft = 3,
        LandscapeRight = 4,
        AutoRotation = 5,
        Landscape = 3
    }

    public enum FilterMode
    {
        Point = 0,
        Bilinear = 1,
        Trilinear = 2,
    }

    public enum TextureWrapMode
    {
        Repeat = 0,
        Clamp = 1,
        Mirror = 2,
        MirrorOnce = 3,
    }

    public enum NPOTSupport
    {
        None = 0,
        Restricted = 1,
        Full = 2
    }

    public enum TextureFormat
    {
        Alpha8      = 1,
        ARGB4444    = 2,
        RGB24       = 3,
        RGBA32      = 4,
        ARGB32      = 5,
        RGB565      = 7,
        R16         = 9,
        DXT1        = 10,
        DXT5        = 12,
        RGBA4444    = 13,
        BGRA32      = 14,

        RHalf       = 15,
        RGHalf      = 16,
        RGBAHalf    = 17,
        RFloat      = 18,
        RGFloat     = 19,
        RGBAFloat   = 20,

        YUY2        = 21,
        RGB9e5Float = 22,

        BC4         = 26,
        BC5         = 27,
        BC6H        = 24,
        BC7         = 25,

        DXT1Crunched = 28,
        DXT5Crunched = 29,

        PVRTC_RGB2  = 30,
        PVRTC_RGBA2 = 31,
        PVRTC_RGB4  = 32,
        PVRTC_RGBA4 = 33,
        ETC_RGB4    = 34,
        ATC_RGB4    = 35,
        ATC_RGBA8   = 36,

        EAC_R = 41,
        EAC_R_SIGNED = 42,
        EAC_RG = 43,
        EAC_RG_SIGNED = 44,
        ETC2_RGB = 45,
        ETC2_RGBA1 = 46,
        ETC2_RGBA8 = 47,

        ASTC_RGB_4x4 = 48,
        ASTC_RGB_5x5 = 49,
        ASTC_RGB_6x6 = 50,
        ASTC_RGB_8x8 = 51,
        ASTC_RGB_10x10 = 52,
        ASTC_RGB_12x12 = 53,

        ASTC_RGBA_4x4 = 54,
        ASTC_RGBA_5x5 = 55,
        ASTC_RGBA_6x6 = 56,
        ASTC_RGBA_8x8 = 57,
        ASTC_RGBA_10x10 = 58,
        ASTC_RGBA_12x12 = 59,

        // Nintendo 3DS
        ETC_RGB4_3DS = 60,
        ETC_RGBA8_3DS = 61,

        RG16 = 62,
        R8 = 63,

        ETC_RGB4Crunched = 64,
        ETC2_RGBA8Crunched = 65,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Enum member TextureFormat.PVRTC_2BPP_RGB has been deprecated. Use PVRTC_RGB2 instead (UnityUpgradable) -> PVRTC_RGB2", true)]
        PVRTC_2BPP_RGB = -127,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Enum member TextureFormat.PVRTC_2BPP_RGBA has been deprecated. Use PVRTC_RGBA2 instead (UnityUpgradable) -> PVRTC_RGBA2", true)]
        PVRTC_2BPP_RGBA = -127,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Enum member TextureFormat.PVRTC_4BPP_RGB has been deprecated. Use PVRTC_RGB4 instead (UnityUpgradable) -> PVRTC_RGB4", true)]
        PVRTC_4BPP_RGB = -127,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Enum member TextureFormat.PVRTC_4BPP_RGBA has been deprecated. Use PVRTC_RGBA4 instead (UnityUpgradable) -> PVRTC_RGBA4", true)]
        PVRTC_4BPP_RGBA = -127
    }

    public enum CubemapFace
    {
        Unknown   = -1,
        PositiveX = 0,
        NegativeX = 1,
        PositiveY = 2,
        NegativeY = 3,
        PositiveZ = 4,
        NegativeZ = 5
    }

    public enum RenderTextureFormat
    {
        ARGB32 = 0,
        Depth = 1,
        ARGBHalf = 2,
        Shadowmap = 3,
        RGB565 = 4,
        ARGB4444 = 5,
        ARGB1555 = 6,
        Default = 7,
        ARGB2101010 = 8,
        DefaultHDR = 9,
        ARGB64 = 10,
        ARGBFloat = 11,
        RGFloat = 12,
        RGHalf = 13,
        RFloat = 14,
        RHalf = 15,
        R8 = 16,
        ARGBInt = 17,
        RGInt = 18,
        RInt = 19,
        BGRA32 = 20,
        // kRTFormatVideo = 21,
        RGB111110Float = 22,
        RG32 = 23,
        RGBAUShort = 24,
        RG16 = 25,
        BGRA10101010_XR = 26,
        BGR101010_XR = 27,
    }

    public enum VRTextureUsage
    {
        None,
        OneEye,
        TwoEyes
    }

    // keep this in sync with the RenderTextureFlags enum in RenderTexture.h
    [Flags]
    public enum RenderTextureCreationFlags
    {
        MipMap = 1 << 0,
        AutoGenerateMips = 1 << 1,
        SRGB = 1 << 2,
        EyeTexture = 1 << 3,
        EnableRandomWrite = 1 << 4,
        CreatedFromScript = 1 << 5, // always set by script.
        // SampleOnlyDepth = 1 << 6, // this is only used internally.
        AllowVerticalFlip = 1 << 7,
        NoResolvedColorSurface = 1 << 8,
        DynamicallyScalable = 1 << 10,
    }

    public enum RenderTextureReadWrite
    {
        Default = 0,
        Linear = 1,
        sRGB = 2
    }

    [Flags]
    public enum RenderTextureMemoryless
    {
        None = 0,
        Color = 1,
        Depth = 2,
        MSAA = 4
    }

    public enum LightmapsMode
    {
        NonDirectional = 0,
        CombinedDirectional = 1,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Enum member LightmapsMode.SeparateDirectional has been removed. Use CombinedDirectional instead (UnityUpgradable) -> CombinedDirectional", true)]
        SeparateDirectional = 2,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Enum member LightmapsMode.Single has been removed. Use NonDirectional instead (UnityUpgradable) -> NonDirectional", true)]
        Single = 0,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Enum member LightmapsMode.Dual has been removed. Use CombinedDirectional instead (UnityUpgradable) -> CombinedDirectional", true)]
        Dual = 1,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("Enum member LightmapsMode.Directional has been removed. Use CombinedDirectional instead (UnityUpgradable) -> CombinedDirectional", true)]
        Directional = 2,
    }

    // Match MaterialGlobalIlluminationFlags on C++ side
    [Flags]
    public enum MaterialGlobalIlluminationFlags
    {
        None = 0,
        RealtimeEmissive = 1 << 0,
        BakedEmissive = 1 << 1,
        EmissiveIsBlack = 1 << 2,
        AnyEmissive = RealtimeEmissive | BakedEmissive
    }

    // Match the enums from LightProbeProxyVolume class on C++ side
    public partial class LightProbeProxyVolume
    {
        public enum ResolutionMode
        {
            Automatic = 0,
            Custom = 1
        }

        public enum BoundingBoxMode
        {
            AutomaticLocal = 0,
            AutomaticWorld = 1,
            Custom = 2
        }

        public enum ProbePositionMode
        {
            CellCorner = 0,
            CellCenter = 1
        }

        public enum RefreshMode
        {
            Automatic = 0,
            EveryFrame = 1,
            ViaScripting = 2,
        }
    }

    public enum CustomRenderTextureInitializationSource
    {
        TextureAndColor = 0,
        Material = 1,
    }

    public enum CustomRenderTextureUpdateMode
    {
        OnLoad = 0,
        Realtime = 1,
        OnDemand = 2,
    }

    public enum CustomRenderTextureUpdateZoneSpace
    {
        Normalized = 0,
        Pixel = 1,
    }

    public enum MotionVectorGenerationMode
    {
        Camera = 0,
        Object = 1,
        ForceNoMotion = 2,
    }

    public enum LineTextureMode
    {
        Stretch = 0,
        Tile = 1,
        DistributePerSegment = 2,
        RepeatPerSegment = 3,
    }

    public enum LineAlignment
    {
        View = 0,
        Local = 1,
    }
} // namespace UnityEngine


namespace UnityEngine.Rendering
{
    // Match IndexFormat on C++ side
    public enum IndexFormat
    {
        UInt16 = 0,
        UInt32 = 1,
    }

    // Match Camera::OpaqueSortMode on C++ side
    public enum OpaqueSortMode
    {
        Default = 0,
        FrontToBack = 1,
        NoDistanceSort = 2
    }

    // Match RenderLoopEnums.h on C++ side
    public enum RenderQueue
    {
        Background = 1000,
        Geometry = 2000,
        AlphaTest = 2450, // we want it to be in the end of geometry queue
        GeometryLast = 2500, // last queue that is considered "opaque" by Unity
        Transparent = 3000,
        Overlay = 4000,
    }

    // Make sure the values are in sync with the native side!
    public enum RenderBufferLoadAction
    {
        Load = 0,
        Clear = 1,
        DontCare = 2,
    }

    // Make sure the values are in sync with the native side!
    public enum RenderBufferStoreAction
    {
        Store = 0,
        Resolve = 1, // Resolve the MSAA surface (currently only works with RenderPassSetup)
        StoreAndResolve = 2, // Resolve the MSAA surface into the resolve target, but also store the MSAA version
        DontCare = 3,
    }

    public enum BlendMode
    {
        Zero = 0,
        One = 1,
        DstColor = 2,
        SrcColor = 3,
        OneMinusDstColor = 4,
        SrcAlpha = 5,
        OneMinusSrcColor = 6,
        DstAlpha = 7,
        OneMinusDstAlpha = 8,
        SrcAlphaSaturate = 9,
        OneMinusSrcAlpha = 10
    }

    public enum BlendOp
    {
        Add = 0,
        Subtract = 1,
        ReverseSubtract = 2,
        Min = 3,
        Max = 4,
        LogicalClear = 5,
        LogicalSet = 6,
        LogicalCopy = 7,
        LogicalCopyInverted = 8,
        LogicalNoop = 9,
        LogicalInvert = 10,
        LogicalAnd = 11,
        LogicalNand = 12,
        LogicalOr = 13,
        LogicalNor = 14,
        LogicalXor = 15,
        LogicalEquivalence = 16,
        LogicalAndReverse = 17,
        LogicalAndInverted = 18,
        LogicalOrReverse = 19,
        LogicalOrInverted = 20,
        Multiply = 21,
        Screen = 22,
        Overlay = 23,
        Darken = 24,
        Lighten = 25,
        ColorDodge = 26,
        ColorBurn = 27,
        HardLight = 28,
        SoftLight = 29,
        Difference = 30,
        Exclusion = 31,
        HSLHue = 32,
        HSLSaturation = 33,
        HSLColor = 34,
        HSLLuminosity = 35,
    }

    public enum CompareFunction
    {
        Disabled = 0,
        Never = 1,
        Less = 2,
        Equal = 3,
        LessEqual = 4,
        Greater = 5,
        NotEqual = 6,
        GreaterEqual = 7,
        Always = 8
    }

    public enum CullMode
    {
        Off = 0,
        Front = 1,
        Back = 2
    }

    [Flags]
    public enum ColorWriteMask
    {
        Alpha = 1,
        Blue = 2,
        Green = 4,
        Red = 8,
        All = 15
    }

    public enum StencilOp
    {
        Keep = 0,
        Zero = 1,
        Replace = 2,
        IncrementSaturate = 3,
        DecrementSaturate = 4,
        Invert = 5,
        IncrementWrap = 6,
        DecrementWrap = 7
    }

    public enum AmbientMode
    {
        Skybox = 0,
        Trilight = 1,
        Flat = 3,
        Custom = 4
    }

    public enum DefaultReflectionMode
    {
        Skybox = 0,
        Custom = 1
    }

    // Keep in sync with LightmapEditorSettings::ReflectionCompression in Editor/Src/LightmapEditorSettings.h
    public enum ReflectionCubemapCompression
    {
        Uncompressed = 0,
        Compressed = 1,
        Auto = 2,
    }

    // Keep in sync with RenderCameraEventType in Runtime/Graphics/CommandBuffer/RenderingEvents.h
    public enum CameraEvent
    {
        BeforeDepthTexture = 0,
        AfterDepthTexture,
        BeforeDepthNormalsTexture,
        AfterDepthNormalsTexture,
        BeforeGBuffer,
        AfterGBuffer,
        BeforeLighting,
        AfterLighting,
        BeforeFinalPass,
        AfterFinalPass,
        BeforeForwardOpaque,
        AfterForwardOpaque,
        BeforeImageEffectsOpaque,
        AfterImageEffectsOpaque,
        BeforeSkybox,
        AfterSkybox,
        BeforeForwardAlpha,
        AfterForwardAlpha,
        BeforeImageEffects,
        AfterImageEffects,
        AfterEverything,
        BeforeReflections,
        AfterReflections,
        BeforeHaloAndLensFlares,
        AfterHaloAndLensFlares
    }

    // Keep in sync with RenderLightEventType in Runtime/Graphics/CommandBuffer/RenderingEvents.h
    public enum LightEvent
    {
        BeforeShadowMap = 0,
        AfterShadowMap,
        BeforeScreenspaceMask,
        AfterScreenspaceMask,
        BeforeShadowMapPass,
        AfterShadowMapPass,
    }

    // Keep in sync with RenderShadowMapPassType in Runtime/Graphics/CommandBuffer/RenderingEvents.h
    [Flags]
    public enum ShadowMapPass
    {
        PointlightPositiveX     = 1 << 0,
        PointlightNegativeX     = 1 << 1,
        PointlightPositiveY     = 1 << 2,
        PointlightNegativeY     = 1 << 3,
        PointlightPositiveZ     = 1 << 4,
        PointlightNegativeZ     = 1 << 5,

        DirectionalCascade0     = 1 << 6,
        DirectionalCascade1     = 1 << 7,
        DirectionalCascade2     = 1 << 8,
        DirectionalCascade3     = 1 << 9,

        Spotlight               = 1 << 10,
        Pointlight              = PointlightPositiveX | PointlightNegativeX | PointlightPositiveY | PointlightNegativeY | PointlightPositiveZ | PointlightNegativeZ,
        Directional             = DirectionalCascade0 | DirectionalCascade1 | DirectionalCascade2 | DirectionalCascade3,
        All                     = Pointlight | Spotlight | Directional,
    }

    public enum BuiltinRenderTextureType
    {
        PropertyName = -4, // Property id
        BufferPtr = -3, // Raw buffer pointer
        RenderTexture = -2, // Render texture. We cannot just use BufferPtr because we need lazy resolve of the buffer pointer here (the RT might not be created yet)
        BindableTexture = -1, // a bindable texture, of any dimension, that is not a render texture
        None = 0, // "nothing", just need zero default value for RenderTargetIdentifier
        CurrentActive = 1,  // currently active RT
        CameraTarget = 2, // current camera target
        Depth = 3,      // camera's depth texture
        DepthNormals = 4,   // camera's depth+normals texture
        ResolvedDepth = 5, // resolved depth buffer from deferred
        //SeparatePassDepth = 6, // "separate pass workaround" depth buffer from deferred
        PrepassNormalsSpec = 7,
        PrepassLight = 8,
        PrepassLightSpec = 9,
        GBuffer0 = 10,
        GBuffer1 = 11,
        GBuffer2 = 12,
        GBuffer3 = 13,
        Reflections = 14,
        MotionVectors = 15,
        GBuffer4 = 16,
        GBuffer5 = 17,
        GBuffer6 = 18,
        GBuffer7 = 19,
    };

    // Match ShaderPassType on C++ side
    public enum PassType
    {
        Normal = 0,
        Vertex = 1,
        VertexLM = 2,

        [System.Obsolete("VertexLMRGBM PassType is obsolete. Please use VertexLM PassType together with DecodeLightmap shader function.")]
        VertexLMRGBM = 3,

        ForwardBase = 4,
        ForwardAdd = 5,
        LightPrePassBase = 6,
        LightPrePassFinal = 7,
        ShadowCaster = 8,
        // ShadowCollector = 9 -- not needed starting with 5.0
        Deferred = 10,
        Meta = 11,
        MotionVectors = 12
    }

    // Match ShadowCastingMode enum on C++ side
    public enum ShadowCastingMode
    {
        Off = 0,
        On = 1,
        TwoSided = 2,
        ShadowsOnly = 3
    }

    // Match LightShadowResolution on C++ side
    public enum LightShadowResolution
    {
        FromQualitySettings = -1,
        Low = 0,
        Medium = 1,
        High = 2,
        VeryHigh = 3
    }

    // Match GfxDeviceRenderer enum on C++ side
    [UsedByNativeCode]
    public enum GraphicsDeviceType
    {
        [System.Obsolete("OpenGL2 is no longer supported in Unity 5.5+")]
        OpenGL2 = 0,
        [System.Obsolete("Direct3D 9 is no longer supported in Unity 2017.2+")]
        Direct3D9 = 1,
        Direct3D11 = 2,
        [System.Obsolete("PS3 is no longer supported in Unity 5.5+")]
        PlayStation3 = 3,
        Null = 4,
        [System.Obsolete("Xbox360 is no longer supported in Unity 5.5+")]
        Xbox360 = 6,
        OpenGLES2 = 8,
        OpenGLES3 = 11,
        PlayStationVita = 12,
        PlayStation4 = 13,
        XboxOne = 14,
        PlayStationMobile = 15,
        Metal = 16,
        OpenGLCore = 17,
        Direct3D12 = 18,
        N3DS = 19,
        Vulkan = 21,
        Switch = 22,
        XboxOneD3D12 = 23
    }

    public enum GraphicsTier
    {
        Tier1 = 0,
        Tier2 = 1,
        Tier3 = 2,
    }

    // Note: match layout of C++ MonoRenderTargetIdentifier!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct RenderTargetIdentifier
    {
        // constructors
        public RenderTargetIdentifier(BuiltinRenderTextureType type)
        {
            m_Type = type;
            m_NameID = -1; // FastPropertyName kInvalidIndex
            m_InstanceID = 0;
            m_BufferPointer = IntPtr.Zero;
            m_MipLevel = 0;
            m_CubeFace = CubemapFace.Unknown;
            m_DepthSlice = 0;
        }

        public RenderTargetIdentifier(string name)
        {
            m_Type = BuiltinRenderTextureType.PropertyName;
            m_NameID = Shader.PropertyToID(name);
            m_InstanceID = 0;
            m_BufferPointer = IntPtr.Zero;
            m_MipLevel = 0;
            m_CubeFace = CubemapFace.Unknown;
            m_DepthSlice = 0;
        }

        public RenderTargetIdentifier(string name, int mipLevel = 0, CubemapFace cubeFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            m_Type = BuiltinRenderTextureType.PropertyName;
            m_NameID = Shader.PropertyToID(name);
            m_InstanceID = 0;
            m_BufferPointer = IntPtr.Zero;
            m_MipLevel = mipLevel;
            m_CubeFace = cubeFace;
            m_DepthSlice = depthSlice;
        }

        public RenderTargetIdentifier(int nameID)
        {
            m_Type = BuiltinRenderTextureType.PropertyName;
            m_NameID = nameID;
            m_InstanceID = 0;
            m_BufferPointer = IntPtr.Zero;
            m_MipLevel = 0;
            m_CubeFace = CubemapFace.Unknown;
            m_DepthSlice = 0;
        }

        public RenderTargetIdentifier(int nameID, int mipLevel = 0, CubemapFace cubeFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            m_Type = BuiltinRenderTextureType.PropertyName;
            m_NameID = nameID;
            m_InstanceID = 0;
            m_BufferPointer = IntPtr.Zero;
            m_MipLevel = mipLevel;
            m_CubeFace = cubeFace;
            m_DepthSlice = depthSlice;
        }

        public RenderTargetIdentifier(Texture tex)
        {
            if (tex == null)
            {
                m_Type = BuiltinRenderTextureType.None;
            }
            else if (tex is RenderTexture)
            {
                m_Type = BuiltinRenderTextureType.RenderTexture;
            }
            else
            {
                m_Type = BuiltinRenderTextureType.BindableTexture;
            }
            m_BufferPointer = IntPtr.Zero;
            m_NameID = -1; // FastPropertyName kInvalidIndex
            m_InstanceID = tex ? tex.GetInstanceID() : 0;
            m_MipLevel = 0;
            m_CubeFace = CubemapFace.Unknown;
            m_DepthSlice = 0;
        }

        public RenderTargetIdentifier(Texture tex, int mipLevel = 0, CubemapFace cubeFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            if (tex == null)
            {
                m_Type = BuiltinRenderTextureType.None;
            }
            else if (tex is RenderTexture)
            {
                m_Type = BuiltinRenderTextureType.RenderTexture;
            }
            else
            {
                m_Type = BuiltinRenderTextureType.BindableTexture;
            }
            m_BufferPointer = IntPtr.Zero;
            m_NameID = -1; // FastPropertyName kInvalidIndex
            m_InstanceID = tex ? tex.GetInstanceID() : 0;
            m_MipLevel = mipLevel;
            m_CubeFace = cubeFace;
            m_DepthSlice = depthSlice;
        }

        public RenderTargetIdentifier(RenderBuffer buf, int mipLevel = 0, CubemapFace cubeFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            m_Type = BuiltinRenderTextureType.BufferPtr;
            m_NameID = -1;
            m_InstanceID = buf.m_RenderTextureInstanceID;
            m_BufferPointer = buf.m_BufferPtr;
            m_MipLevel = mipLevel;
            m_CubeFace = cubeFace;
            m_DepthSlice = depthSlice;
        }

        // implicit conversion operators
        public static implicit operator RenderTargetIdentifier(BuiltinRenderTextureType type)
        {
            return new RenderTargetIdentifier(type);
        }

        public static implicit operator RenderTargetIdentifier(string name)
        {
            return new RenderTargetIdentifier(name);
        }

        public static implicit operator RenderTargetIdentifier(int nameID)
        {
            return new RenderTargetIdentifier(nameID);
        }

        public static implicit operator RenderTargetIdentifier(Texture tex)
        {
            return new RenderTargetIdentifier(tex);
        }

        public static implicit operator RenderTargetIdentifier(RenderBuffer buf)
        {
            return new RenderTargetIdentifier(buf);
        }

        public override string ToString()
        {
            return UnityString.Format("Type {0} NameID {1} InstanceID {2}", m_Type, m_NameID, m_InstanceID);
        }

        public override int GetHashCode()
        {
            return (m_Type.GetHashCode() * 23 + m_NameID.GetHashCode()) * 23 + m_InstanceID.GetHashCode();
        }

        public bool Equals(RenderTargetIdentifier rhs)
        {
            return m_Type == rhs.m_Type &&
                m_NameID == rhs.m_NameID &&
                m_InstanceID == rhs.m_InstanceID &&
                m_BufferPointer == rhs.m_BufferPointer &&
                m_MipLevel == rhs.m_MipLevel &&
                m_CubeFace == rhs.m_CubeFace &&
                m_DepthSlice == rhs.m_DepthSlice;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RenderTargetIdentifier))
                return false;
            RenderTargetIdentifier rhs = (RenderTargetIdentifier)obj;
            return Equals(rhs);
        }

        public static bool operator==(RenderTargetIdentifier lhs, RenderTargetIdentifier rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(RenderTargetIdentifier lhs, RenderTargetIdentifier rhs)
        {
            return !lhs.Equals(rhs);
        }

        // private variable never read: we match struct in native side
        #pragma warning disable 0414
        private BuiltinRenderTextureType m_Type;
        private int m_NameID;
        private int m_InstanceID;
        private IntPtr m_BufferPointer;
        private int m_MipLevel;
        private CubemapFace m_CubeFace;
        private int m_DepthSlice;
        #pragma warning restore 0414
    }

    // Keep in sync with ReflectionProbeUsage in Runtime\Camera\ReflectionProbeTypes.h
    public enum ReflectionProbeUsage
    {
        Off,
        BlendProbes,
        BlendProbesAndSkybox,
        Simple
    }

    public enum ReflectionProbeType
    {
        Cube = 0,
        Card = 1,
    }

    public enum ReflectionProbeClearFlags
    {
        Skybox = 1,
        SolidColor = 2
    }

    public enum ReflectionProbeMode
    {
        Baked = 0,
        Realtime = 1,
        Custom = 2
    }

    [UsedByNativeCode]
    public struct ReflectionProbeBlendInfo
    {
        public ReflectionProbe probe;
        public float weight;
    }

    public enum ReflectionProbeRefreshMode
    {
        OnAwake = 0,
        EveryFrame = 1,
        ViaScripting = 2,
    }

    public enum ReflectionProbeTimeSlicingMode
    {
        AllFacesAtOnce = 0,
        IndividualFaces = 1,
        NoTimeSlicing = 2,
    }

    // Match ShadowSamplingMode on C++ side
    public enum ShadowSamplingMode
    {
        CompareDepths = 0,
        RawDepth = 1,
        None = 2
    }

    // Match BaseRenderer::LightProbeUsage on C++ side
    public enum LightProbeUsage
    {
        Off = 0,
        BlendProbes = 1,
        UseProxyVolume = 2
    }

    // Match GraphicsSettings::BuiltinShaderType on C++ side
    public enum BuiltinShaderType
    {
        DeferredShading = 0,
        DeferredReflections = 1,
        LegacyDeferredLighting = 2,
        ScreenSpaceShadows = 3,
        DepthNormals = 4,
        MotionVectors = 5,
        LightHalo = 6,
        LensFlare = 7,
    }

    // Match BuiltinShaderSettings::BuiltinShaderMode on C++ side
    public enum BuiltinShaderMode
    {
        Disabled = 0,
        UseBuiltin = 1,
        UseCustom = 2,
    }

    // Match platform_caps_keywords enum on C++ side (Runtime/Graphics/PlatformCapsKeywords.h)
    // with enum names being actual shader defines (from platform_caps_keywords::KeywordDefine)
    public enum BuiltinShaderDefine
    {
        UNITY_NO_DXT5nm,
        UNITY_NO_RGBM,
        UNITY_USE_NATIVE_HDR,
        UNITY_ENABLE_REFLECTION_BUFFERS,
        UNITY_FRAMEBUFFER_FETCH_AVAILABLE,
        UNITY_ENABLE_NATIVE_SHADOW_LOOKUPS,
        UNITY_METAL_SHADOWS_USE_POINT_FILTERING,
        UNITY_NO_CUBEMAP_ARRAY,
        UNITY_NO_SCREENSPACE_SHADOWS,
        UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS,
        UNITY_PBS_USE_BRDF1,
        UNITY_PBS_USE_BRDF2,
        UNITY_PBS_USE_BRDF3,
        UNITY_NO_FULL_STANDARD_SHADER,
        UNITY_SPECCUBE_BOX_PROJECTION,
        UNITY_SPECCUBE_BLENDING,
        UNITY_ENABLE_DETAIL_NORMALMAP,
        SHADER_API_MOBILE,
        SHADER_API_DESKTOP,
        UNITY_HARDWARE_TIER1,
        UNITY_HARDWARE_TIER2,
        UNITY_HARDWARE_TIER3,
        UNITY_COLORSPACE_GAMMA,
        UNITY_LIGHT_PROBE_PROXY_VOLUME,
        UNITY_LIGHTMAP_DLDR_ENCODING,
        UNITY_LIGHTMAP_RGBM_ENCODING,
        UNITY_LIGHTMAP_FULL_HDR,
    }

    // Match TextureDimension on C++ side
    public enum TextureDimension
    {
        Unknown = -1,
        None = 0,
        Any = 1,
        Tex2D = 2,
        Tex3D = 3,
        Cube = 4,
        Tex2DArray = 5,
        CubeArray = 6,
    }

    // Match CopyTextureSupport on C++ side
    [Flags]
    public enum CopyTextureSupport
    {
        None = 0,
        Basic = (1 << 0),
        Copy3D = (1 << 1),
        DifferentTypes = (1 << 2),
        TextureToRT = (1 << 3),
        RTToTexture = (1 << 4),
    }

    public enum CameraHDRMode
    {
        FP16 = 1,
        R11G11B10 = 2
    }

    public enum RealtimeGICPUUsage
    {
        Low = 25,
        Medium = 50,
        High = 75,
        Unlimited = 100
    }
} // namespace UnityEngine.Rendering

namespace UnityEngineInternal
{
    internal enum LightmapType
    {
        NoLightmap      = -1,
        StaticLightmap  = 0,
        DynamicLightmap = 1,
    }
}
