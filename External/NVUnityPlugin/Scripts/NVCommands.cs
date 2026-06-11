using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditor;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.NVIDIA
{
    /////////////////////////////////////////////////////////////////////////////////////
    // -----------------------------------------------------------------------------------
    //  Public Enums must match C++ enums found in NVDevice.h and nvsdk_ngx_defs.h
    // -----------------------------------------------------------------------------------
    ///<summary>Options that represent subfeatures of DLSS.</summary>
    [Flags]
    public enum DLSSFeatureFlags
    {
        ///<summary>Disables every subfeature.</summary>
        None          = 0,
        ///<summary>Indicates whether the input buffer uses high dynamic range. If set, the input buffer is raw luminance, if not set, the input buffer is normalized color.</summary>
        IsHDR         = 1 << 0,
        ///<summary>Indicates whether the input motion vector buffer is a lower resolution than the output. If set, you must specify <see cref="DLSSCommandExecutionData.mvScaleX" /> and <see cref="DLSSCommandExecutionData.mvScaleY" />.</summary>
        MVLowRes      = 1 << 1,
        ///<summary>Indicates whether the input motion vectors already include camera jitter. If set, DLSS will subtract the jitter specified by <see cref="DLSSCommandExecutionData.jitterOffsetX" /> and <see cref="DLSSCommandExecutionData.jitterOffsetY" /> from the motion vectors.</summary>
        MVJittered    = 1 << 2,
        ///<summary>Indicates whether or not to invert the depth buffer.</summary>
        DepthInverted = 1 << 3,

        ///<summary>This flag is now obsolete. Indicates whether to use the sharpening feature or not.</summary>
        [Obsolete("Sharpening is deprecated by NVIDIA. It is no longer used and will be removed in a future release.")]
        DoSharpening = 1 << 4,
        ///<summary>Indicates whether or not to use the auto exposure for missing exposure input.</summary>
        AutoExposure  = 1 << 5
    }

    //Must match DLSSQuality in NVCommands.h
    ///<summary>Options for DLSS performance modes.</summary>
    public enum DLSSQuality
    {
        ///<summary>High quality, less performant.</summary>
        MaximumQuality = 2,
        ///<summary>Balances performance with quality.</summary>
        Balanced = 1,
        ///<summary>Fast performance, lower quality.</summary>
        MaximumPerformance = 0,
        ///<summary>Fastest performance, lowest quality.</summary>
        UltraPerformance = 3,
        ///<summary>Deep Learning Anti-Aliasing, no upscaling.</summary>
        DLAA = 4
    }

    // Must match DLSSPReset in NVCommands.h
    ///<summary>Options for DLSS render presets, specified for each DLSSQuality mode.</summary>
    [Flags]
    public enum DLSSPreset
    {
        ///<summary>Default preset set by NVIDIA.</summary>
        Preset_Default = 0,
        ///<summary>CNN model. Marked for deprecation in upcoming DLSS releases. Don't use for new projects.</summary>
        Preset_F = 1 << 0,
        ///<summary>Transformer model. Slightly lowers ghosting but increases flickering. NVIDIA recommends using Preset K instead.</summary>
        Preset_J = 1 << 1,
        ///<summary>Transformer model. Default preset for DLAA/Balanced/Quality modes. Requires fewer resources than Preset L.</summary>
        Preset_K = 1 << 2,
        ///<summary>2nd Gen Transformer model. Delivers a sharper, more stable image with less ghosting than Preset J, K, but lowers performance. Recommended for RTX 40 Series GPUs and above.</summary>
        Preset_L = 1 << 3,
        ///<summary>2nd Gen Transformer model. Provides about the same image quality as Preset L. This preset is slower than presets J and K, but faster than preset L. Recommended for RTX 40 Series GPUs and above.</summary>
        Preset_M = 1 << 4
    }

    /////////////////////////////////////////////////////////////////////////////////////

    // -----------------------------------------------------------------------------------
    //  Command Data Passed to C++ runtime. Must match NVCommands.h
    // -----------------------------------------------------------------------------------
    #region CmdData

    [StructLayout(LayoutKind.Sequential)]
    internal struct InitDeviceCmdData
    {
        //// These properties must match the code in NVCommands.h in C++ ////
        IntPtr m_ProjectId;
        IntPtr m_EngineVersion;
        IntPtr m_AppDir;
        ////////////////////////////////////////////////////////////////////

        /// DO NOT simplify these properties, alignment must match that one of the c++ counter part in NVCommands.h
        public IntPtr projectId     { set { m_ProjectId = value;     } get { return m_ProjectId; } }
        public IntPtr engineVersion { set { m_EngineVersion = value; } get { return m_EngineVersion; } }
        public IntPtr appDir        { set { m_AppDir = value;        } get { return m_AppDir; } }
        ////////////////////////////////////////////////////////////////////
    }

    ///<summary>Represent the initialization state of a <see cref="DLSSContext" />.</summary>
    ///<remarks>
    /// You can only use and set this when calling <see cref="GraphicsDevice.CreateFeature" />.
    ///</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct DLSSCommandInitializationData
    {
        // These properties must match the code in NVCommands.h in C++
        // DO NOT simplify these properties, alignment must match that one of the c++ counter part in NVCommands.h
        uint m_InputRTWidth;
        uint m_InputRTHeight;
        uint m_OutputRTWidth;
        uint m_OutputRTHeight;
        DLSSQuality m_Quality;
        DLSSPreset m_PresetQualityMode;
        DLSSPreset m_PresetBalancedMode;
        DLSSPreset m_PresetPerformanceMode;
        DLSSPreset m_PresetUltraPerformanceMode;
        DLSSPreset m_PresetDlaaMode;
        DLSSFeatureFlags  m_Flags;
        uint m_FeatureSlot;
        ////////////////////////////////////////////////////////////////////

        ///<summary>The texture input width size of the input buffers in texels.</summary>
        public uint inputRTWidth                     { set { m_InputRTWidth               = value; }    get { return m_InputRTWidth;   } }
        ///<summary>The texture input height size of the input buffers in texels.</summary>
        public uint inputRTHeight                    { set { m_InputRTHeight              = value; }    get { return m_InputRTHeight;  } }
        ///<summary>The output buffer width size in texels to upscale to.</summary>
        public uint outputRTWidth                    { set { m_OutputRTWidth              = value; }    get { return m_OutputRTWidth;  } }
        ///<summary>The output buffer height size in texels to upscale to.</summary>
        public uint outputRTHeight                   { set { m_OutputRTHeight             = value; }    get { return m_OutputRTHeight; } }
        ///<summary>The quality mode for DLSS.</summary>
        public DLSSQuality quality                   { set { m_Quality                    = value; }    get { return m_Quality;        } }
        ///<summary>DLSS Render Preset for Quality mode.</summary>
        public DLSSPreset presetQualityMode          { set { m_PresetQualityMode          = value; }    get { return m_PresetQualityMode; } }
        ///<summary>DLSS Render Preset for Balanced mode.</summary>
        public DLSSPreset presetBalancedMode         { set { m_PresetBalancedMode         = value; }    get { return m_PresetBalancedMode; } }
        ///<summary>DLSS Render Preset for Performance mode.</summary>
        public DLSSPreset presetPerformanceMode      { set { m_PresetPerformanceMode      = value; }    get { return m_PresetPerformanceMode; } }
        ///<summary>DLSS Render Preset for Ultra Performance mode.</summary>
        public DLSSPreset presetUltraPerformanceMode { set { m_PresetUltraPerformanceMode = value; }    get { return m_PresetUltraPerformanceMode; } }
        ///<summary>DLSS Render Preset for DLAA mode.</summary>
        public DLSSPreset presetDlaaMode             { set { m_PresetDlaaMode             = value; }    get { return m_PresetDlaaMode; } }
        ///<summary>Bit mask containing feature flags to be used for DLSS.</summary>
        public DLSSFeatureFlags featureFlags         { set { m_Flags                      = value; }    get { return m_Flags;          } }
        internal uint featureSlot                    { set { m_FeatureSlot                = value; }    get { return m_FeatureSlot;    } }
        ///<summary>Helper function. Controls the feature flags used by DLSS. .</summary>
        ///<param name="flag">The feature flag to set or unset.</param>
        ///<param name="value">Indicates whether to set or unset the flag.</param>
        ///<seealso cref="DLSSFeatureFlags" />
        public void SetFlag(DLSSFeatureFlags flag, bool value)
        {
            if (value)
            {
                m_Flags |= flag;
            }
            else
            {
                m_Flags &= ~flag;
            }
        }

        ///<summary>Helper function. Gets whether a DLSS feature is set or unset.</summary>
        ///<param name="flag">The feature flag to get the state from.</param>
        ///<returns>Indicates whether the feature state is set or unset.</returns>
        ///<seealso cref="DLSSFeatureFlags" />
        public bool GetFlag(DLSSFeatureFlags flag)
        {
            return (m_Flags & flag) != 0;
        }

        ////////////////////////////////////////////////////////////////////
    }

    ///<summary>The set of texture slots available for the <see cref="DLSSContext" />.</summary>
    /// <seealso cref="GraphicsDevice.ExecuteDLSS" />
    public struct DLSSTextureTable
    {
        /// <summary>The input color buffer to upsample for <see cref="DLSSContext" />.</summary>
        /// <remarks>This texture is mandatory and you must set it to a non-null value.</remarks>
        public Texture colorInput       { set; get; }
        /// <summary>The output color buffer to write the upsampling result for <see cref="DLSSContext" />.</summary>
        /// <remarks>This must be large enough to fit in the output rect specified in the command.
        /// This texture is mandatory and you must set it to a non-null value.</remarks>
        public Texture colorOutput      { set; get; }
        /// <summary>The input depth buffer.</summary>
        /// <remarks>This must be the same size as the input color buffer.
        /// This texture is mandatory and you must set it to a non-null value.</remarks>
        public Texture depth            { set; get; }
        /// <summary>The motion vectors requested by the <see cref="DLSSContext" />.</summary>
        /// <remarks>Depending on the <see cref="DLSSFeatureFlags" /> specified in <see cref="DLSSContext.initData" />, this buffer can be a smaller scale or the full output resolution.
        /// This texture is mandatory and you must set it to a non-null value.</remarks>
        public Texture motionVectors    { set; get; }
        /// <summary>A transparency bit mask.</summary>
        /// <remarks>This must be the same size as the input texture. This texture helps the <see cref="DLSSContext" />
        /// with ghosting issues. This texture is optional.</remarks>
        public Texture transparencyMask { set; get; }
        /// <summary>A 1x1 texture with pre-exposure values.</summary>
        /// <remarks>If you do not use pre-exposure, do not set this texture. This texture is optional.</remarks>
        public Texture exposureTexture  { set; get; }
        /// <summary>A mask, same size as colorInput, preferably of format R8_UNORM that informs DLSS of possible moving pixels.</summary>
        /// <remarks>If heavy ghosting is encountered, set pixels to this mask to fix the problem. This texture is optional.</remarks>
        public Texture biasColorMask    { set; get; }
    }

    /// <summary>Represents the state of a DLSSContext.</summary>
    /// <remarks>If you call <see cref="GraphicsDevice.ExecuteDLSS" />, Unity sends the values in this struct to the runtime.
    /// After this, you can change the values in this struct without any side effects.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct DLSSCommandExecutionData
    {
        // These properties must match the code in NVCommands.h in C++
        // DO NOT simplify these properties, alignment must match that one of the c++ counter part in NVCommands.h
        internal enum Textures
        {
            ColorInput = 0,
            ColorOutput,
            Depth,
            MotionVectors,
            TransparencyMask,
            ExposureTexture,
            BiasColorMask,
        };

        int   m_Reset;
        float m_Sharpness;
        float m_MVScaleX;
        float m_MVScaleY;
        float m_JitterOffsetX;
        float m_JitterOffsetY;
        float m_PreExposure;
        uint  m_SubrectOffsetX;
        uint  m_SubrectOffsetY;
        uint  m_SubrectWidth;
        uint  m_SubrectHeight;
        uint  m_InvertXAxis;
        uint  m_InvertYAxis;
        uint  m_FeatureSlot;
        ////////////////////////////////////////////////////////////////////

        ///<summary>Indicates whether to invalidate the history buffers.</summary>
        ///<remarks>It is best practice to set this to <c>true</c> during camera cuts. For example, when you instantiate, reset, or teleport a camera.</remarks>
        public   int   reset          { set { m_Reset          = value; } get { return m_Reset;          } }
        ///<summary>Specifies how sharp the frame should look as a value from 0 to 1.</summary>
        ///<remarks>Sharpening is deprecated by NVIDIA and no longer has any effect. This property is retained for backwards  compatibility.</remarks>
        public   float sharpness      { set { m_Sharpness      = value; } get { return m_Sharpness;      } }
        ///<summary>If you set the <see cref="NVIDIA.DLSSFeatureFlags.MVLowRes" /> flag, this value indicates the scale (smaller) of the motion vector buffer input texture used in the x-axis.</summary>
        public   float mvScaleX       { set { m_MVScaleX       = value; } get { return m_MVScaleX;       } }
        ///<summary>If you set the <see cref="NVIDIA.DLSSFeatureFlags.MVLowRes" /> flag, this value indicates the scale (smaller) of the motion vector buffer input texture used in the y-axis.</summary>
        public   float mvScaleY       { set { m_MVScaleY       = value; } get { return m_MVScaleY;       } }
        ///<summary>The x-axis jitter camera offset in device coordinates.</summary>
        public   float jitterOffsetX  { set { m_JitterOffsetX  = value; } get { return m_JitterOffsetX;  } }
        ///<summary>The y-axis jitter camera offset in device coordinates.</summary>
        public   float jitterOffsetY  { set { m_JitterOffsetY  = value; } get { return m_JitterOffsetY;  } }
        ///<summary>Specifies a pre exposure multiplier for the input color texture.</summary>
        public   float preExposure    { set { m_PreExposure    = value; } get { return m_PreExposure;    } }
        ///<summary>The subrectangle x-axis offset of input buffers to use.</summary>
        public   uint  subrectOffsetX { set { m_SubrectOffsetX = value; } get { return m_SubrectOffsetX; } }
        ///<summary>The subrectangle y-axis offset of input buffers to use.</summary>
        public   uint  subrectOffsetY { set { m_SubrectOffsetY = value; } get { return m_SubrectOffsetY; } }
        ///<summary>The subrectangle width of input buffers to use.</summary>
        public   uint  subrectWidth   { set { m_SubrectWidth   = value; } get { return m_SubrectWidth;   } }
        ///<summary>The subrectangle height of input buffers to use.</summary>
        public   uint  subrectHeight  { set { m_SubrectHeight  = value; } get { return m_SubrectHeight;  } }
        ///<summary>Indicates if the X axis is inverted. Set to 0 or 1.</summary>
        public   uint  invertXAxis    { set { m_InvertXAxis    = value; } get { return m_InvertXAxis;    } }
        ///<summary>Indicates if the Y axis is inverted. Set to 0 or 1.</summary>
        public   uint  invertYAxis    { set { m_InvertYAxis    = value; } get { return m_InvertYAxis;    } }
        internal uint  featureSlot    { set { m_FeatureSlot    = value; } get { return m_FeatureSlot;    } }
        ////////////////////////////////////////////////////////////////////
    }

    ///<summary>Represents the performance settings that DLSS recommends based on the system's graphics card and the size of the input and output color buffers. </summary>
    ///<seealso cref="GraphicsDevice.GetOptimalSettings" />
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct OptimalDLSSSettingsData
    {
        // DO NOT simplify these properties, alignment must match that one of the c++ counter part in NVCommands.h
        // These properties must match the code in NVCommands.h in C++
        private readonly uint  m_OutRenderWidth;
        private readonly uint  m_OutRenderHeight;
        private readonly float m_Sharpness;
        private readonly uint  m_MaxWidth;
        private readonly uint  m_MaxHeight;
        private readonly uint  m_MinWidth;
        private readonly uint  m_MinHeight;
        ////////////////////////////////////////////////////////////////////

        ///<summary>The width of the output render resolution that DLSS recommends.</summary>
        public uint  outRenderWidth  { get { return m_OutRenderWidth;  } }
        ///<summary>The height of the output render resolution that DLSS recommends.</summary>
        public uint  outRenderHeight { get { return m_OutRenderHeight; } }
        ///<summary>The sharpness value that DLSS recommends.</summary>
        public float sharpness       { get { return m_Sharpness; } }
        ///<summary>The maximum width that DLSS recommends for dynamic resolution.</summary>
        public uint  maxWidth        { get { return m_MaxWidth;  } }
        ///<summary>The maximum height that DLSS recommends for dynamic resolution.</summary>
        public uint  maxHeight       { get { return m_MaxHeight; } }
        ///<summary>The minimum width that DLSS recommends for dynamic resolution.</summary>
        public uint  minWidth        { get { return m_MinWidth;  } }
        ///<summary>The minimum height that DLSS recommends for dynamic resolution.</summary>
        public uint  minHeight       { get { return m_MinHeight; } }
        ////////////////////////////////////////////////////////////////////
    }

    #endregion


    #region DebugData
    ///<summary>Represents debug information for a particular DLSSContext.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DLSSDebugFeatureInfos
    {
        //// These properties must match the code in NVCommands.h in C++ ////
        private readonly bool               m_ValidFeature;
        private readonly uint               m_FeatureSlot;
        private readonly DLSSCommandExecutionData m_ExecData;
        private readonly DLSSCommandInitializationData    m_InitData;
        ////////////////////////////////////////////////////////////////////

        ///<summary>Debug information that indicates whether the feature last execution was valid or not.</summary>
        public bool validFeature           { get { return m_ValidFeature; } }
        ///<summary>The internal feature slot ID. You can use this feature slot as a unique identifier for DLSSCommand objects. Only use this for debugging purposes.</summary>
        public uint featureSlot            { get { return m_FeatureSlot;  } }
        ///<summary>The last execution data which the DLSSContext during execution. </summary>
        ///<seealso cref="GraphicsDevice.ExecuteDLSS" />
        public DLSSCommandExecutionData      execData { get { return m_ExecData; } }
        ///<summary>The init data which the DLSSContext used.</summary>
        public DLSSCommandInitializationData initData { get { return m_InitData; } }
        ////////////////////////////////////////////////////////////////////
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct GraphicsDeviceDebugInfo
    {
        //// These properties must match the code in NVCommands.h in C++ ////
        public uint NVDeviceVersion;
        public uint NGXVersion;

        // These fields describe the C# buffer we pass to C++.
        public IntPtr outDlssInfoBuffer;
        public uint outDlssInfoBufferCapacity;

        public uint dlssInfoCount;
        ////////////////////////////////////////////////////////////////////
    };
    #endregion

    #region SerializationHelpers

    internal class NativeData<T>
        : IDisposable
        where T : struct
    {
        private IntPtr m_MarshalledValue = IntPtr.Zero;
        public T Value = new T();
        public IntPtr Ptr
        {
            get
            {
                unsafe { UnsafeUtility.CopyStructureToPtr(ref Value, m_MarshalledValue.ToPointer()); }
                return m_MarshalledValue;
            }
        }

        public NativeData()
        {
            m_MarshalledValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_MarshalledValue != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_MarshalledValue);
                m_MarshalledValue = IntPtr.Zero;
            }
        }

        ~NativeData() { Dispose(false); }
    }

    internal class NativeStr : IDisposable
    {
        private String m_Str = null;
        private IntPtr m_MarshalledString = IntPtr.Zero;

        public String Str
        {
            set
            {
                m_Str = value;
                Dispose();
                if (value != null)
                    m_MarshalledString = Marshal.StringToHGlobalUni(m_Str);
            }
        }

        public IntPtr Ptr { get { return m_MarshalledString; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_MarshalledString != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_MarshalledString);
                m_MarshalledString = IntPtr.Zero;
            }
        }

        ~NativeStr() { Dispose(false); }
    }

    #endregion

    #region DeviceCommands

    internal class InitDeviceContext
    {
        private NativeStr m_ProjectId = new NativeStr();
        private NativeStr m_EngineVersion = new NativeStr();
        private NativeStr m_AppDir = new NativeStr();
        private NativeData<InitDeviceCmdData> m_InitData = new NativeData<InitDeviceCmdData>();

        public InitDeviceContext(String projectId, String engineVersion, String appDir)
        {
            m_ProjectId.Str = projectId;
            m_EngineVersion.Str = engineVersion;
            m_AppDir.Str = appDir;
        }

        internal IntPtr GetInitCmdPtr()
        {
            m_InitData.Value.projectId = m_ProjectId.Ptr;
            m_InitData.Value.engineVersion = m_EngineVersion.Ptr;
            m_InitData.Value.appDir = m_AppDir.Ptr;
            return m_InitData.Ptr;
        }
    }

    ///<summary>Represents the state of DLSS.</summary>
    ///<remarks>This class must be persistent, since internally it keeps track of history buffers and other important
    /// implementation details. To modify the results of DLSS, alter values in the ExecuteData property and then run
    /// <see cref="GraphicsDevice.ExecuteDLSS" />.</remarks>
    public class DLSSContext
    {
        private NativeData<DLSSCommandInitializationData> m_InitData = new NativeData<DLSSCommandInitializationData>();
        private NativeData<DLSSCommandExecutionData> m_ExecData = new NativeData<DLSSCommandExecutionData>();

        // UUM-134012: Rate-limited logging for pool exhaustion errors
        private static float s_LastPoolExhaustedLogTime = 0;
        internal static float poolExhaustedLogIntervalSeconds = 1.0f;

        ///<summary>The immutable initialization data the DLSSContext requires.</summary>
        ///<seealso cref="NVIDIA.DLSSCommandInitializationData" />
        public ref readonly DLSSCommandInitializationData initData   { get { return ref m_InitData.Value; } }
        ///<summary>The mutable state of the current DLSS object.</summary>
        ///<remarks>To change the results of DLSS, modify this state prior to calling <see cref="GraphicsDevice.ExecuteDLSS" />.</remarks>
        ///<seealso cref="DLSSCommandExecutionData" />
        public ref DLSSCommandExecutionData executeData { get { return ref m_ExecData.Value; } }
        internal uint                   featureSlot { get { return initData.featureSlot; } }

        internal DLSSContext()
        {
        }

        internal void Init(DLSSCommandInitializationData initSettings, uint featureSlot)
        {
            m_InitData.Value = initSettings;
            m_InitData.Value.featureSlot = featureSlot;
        }

        internal void Reset()
        {
            m_InitData.Value = new DLSSCommandInitializationData();
            m_ExecData.Value = new DLSSCommandExecutionData();
        }

        internal IntPtr GetInitCmdPtr()
        {
            return m_InitData.Ptr;
        }

        internal IntPtr GetExecuteCmdPtr()
        {
            m_ExecData.Value.featureSlot = featureSlot;

            // UUM-134012: Copy data to stable buffer in native plugin
            // This prevents race condition where CPU overwrites data before GPU reads
            // Pass struct by value - P/Invoke marshals directly to native stack,
            // avoiding redundant copy through m_MarshalledValue
            IntPtr stablePtr = GraphicsDevice.NVUP_PrepareExecuteData(m_ExecData.Value);

            if (stablePtr == IntPtr.Zero)
            {
                // Pool exhausted - this frame's DLSS execution will be skipped
                // (safer than allowing race condition with original behavior)
                // Rate-limit logging to avoid performance impact
                float currentTime = Time.unscaledTime;
                if (currentTime - s_LastPoolExhaustedLogTime >= poolExhaustedLogIntervalSeconds)
                {
                    s_LastPoolExhaustedLogTime = currentTime;
                    Debug.LogError("[NVAPI] DLSS execute data pool exhausted - frame will be skipped");
                }
            }

            return stablePtr;
        }
    }

    /// <summary>Represents a memory snapshot of the current feature states.</summary>
    /// <remarks>The memory of the arrays/buffers in this struct are tied to the lifetime of the debug view.</remarks>
    /// <seealso cref="GraphicsDevice.CreateDebugView" />
    /// <seealso cref="GraphicsDevice.UpdateDebugView" />
    /// <seealso cref="GraphicsDevice.DeleteDebugView" />
    public class GraphicsDeviceDebugView
    {
        // Define a constant for the max number of features to display.
        // This should match the UI code.
        internal const int MaxFeatures = 16;

        internal uint m_ViewId = 0;
        internal uint m_DeviceVersion = 0;
        internal uint m_NgxVersion = 0;
        internal readonly DLSSDebugFeatureInfos[] m_DlssDebugFeatures = new DLSSDebugFeatureInfos[MaxFeatures];
        internal uint m_DlssFeatureValidCount = 0;

        internal GraphicsDeviceDebugView(uint viewId)
        {
            m_ViewId = viewId;
        }

        /// <summary>The version that corresponds to Unity's host plugin NVUnityPlugin.</summary>
        public uint deviceVersion { get { return m_DeviceVersion; } }
        /// <summary>The current version id of the official internal NV NGX library.</summary>
        /// <remarks>This version can change if you swap the DLLs for DLSS.</remarks>
        public uint ngxVersion { get { return m_NgxVersion; } }
        
        /// <summary>A snapshot enumeration of all the active dlss features information currently active in the runtime.</summary>
        /// <remarks>The method <see cref="GraphicsDevice.UpdateDebugView" /> will performs update on this snapshot.</remarks>
        [Obsolete("This property causes garbage collection and is inefficient. Use dlssFeatureInfosSpan and dlssFeatureInfoCount instead.", false)]
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IEnumerable<DLSSDebugFeatureInfos> dlssFeatureInfos { get { return m_DlssDebugFeatures.Take((int)m_DlssFeatureValidCount); } }
#pragma warning restore UA2001

        /// <summary>Gets a read-only view into the valid DLSS feature info entries.</summary>
        /// <remarks>Accessing this and iterating it with a for loop is allocation-free.</remarks>
        public ReadOnlySpan<DLSSDebugFeatureInfos> dlssFeatureInfosSpan => new ReadOnlySpan<DLSSDebugFeatureInfos>(m_DlssDebugFeatures, 0, (int)m_DlssFeatureValidCount);
        
    }

    #endregion
} // namespace NVIDIA
