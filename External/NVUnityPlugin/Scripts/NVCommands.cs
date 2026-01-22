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
    [Flags]
    public enum DLSSFeatureFlags
    {
        None          = 0,
        IsHDR         = 1 << 0,
        MVLowRes      = 1 << 1,
        MVJittered    = 1 << 2,
        DepthInverted = 1 << 3,

        [Obsolete("Sharpening is deprecated by NVIDIA. It is no longer used and will be removed in a future release.")]
        DoSharpening = 1 << 4,
        AutoExposure  = 1 << 5
    }

    //Must match DLSSQuality in NVCommands.h
    public enum DLSSQuality
    {
        MaximumQuality = 2,
        Balanced = 1,
        MaximumPerformance = 0,
        UltraPerformance = 3,
        DLAA = 4
    }

    // Must match DLSSPReset in NVCommands.h
    [Flags]
    public enum DLSSPreset
    {
        Preset_Default = 0,
        Preset_F = 1 << 0,
        Preset_J = 1 << 1,
        Preset_K = 1 << 2
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

    [StructLayout(LayoutKind.Sequential)]
    public struct DLSSCommandInitializationData
    {
        //// These properties must match the code in NVCommands.h in C++ ////
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

        /// DO NOT simplify these properties, alignment must match that one of the c++ counter part in NVCommands.h
        public uint inputRTWidth                     { set { m_InputRTWidth               = value; }    get { return m_InputRTWidth;   } }
        public uint inputRTHeight                    { set { m_InputRTHeight              = value; }    get { return m_InputRTHeight;  } }
        public uint outputRTWidth                    { set { m_OutputRTWidth              = value; }    get { return m_OutputRTWidth;  } }
        public uint outputRTHeight                   { set { m_OutputRTHeight             = value; }    get { return m_OutputRTHeight; } }
        public DLSSQuality quality                   { set { m_Quality                    = value; }    get { return m_Quality;        } }
        public DLSSPreset presetQualityMode          { set { m_PresetQualityMode          = value; }    get { return m_PresetQualityMode; } }
        public DLSSPreset presetBalancedMode         { set { m_PresetBalancedMode         = value; }    get { return m_PresetBalancedMode; } }
        public DLSSPreset presetPerformanceMode      { set { m_PresetPerformanceMode      = value; }    get { return m_PresetPerformanceMode; } }
        public DLSSPreset presetUltraPerformanceMode { set { m_PresetUltraPerformanceMode = value; }    get { return m_PresetUltraPerformanceMode; } }
        public DLSSPreset presetDlaaMode             { set { m_PresetDlaaMode             = value; }    get { return m_PresetDlaaMode; } }
        public DLSSFeatureFlags featureFlags         { set { m_Flags                      = value; }    get { return m_Flags;          } }
        internal uint featureSlot                    { set { m_FeatureSlot                = value; }    get { return m_FeatureSlot;    } }
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

        public bool GetFlag(DLSSFeatureFlags flag)
        {
            return (m_Flags & flag) != 0;
        }

        ////////////////////////////////////////////////////////////////////
    }

    public struct DLSSTextureTable
    {
        public Texture colorInput       { set; get; }
        public Texture colorOutput      { set; get; }
        public Texture depth            { set; get; }
        public Texture motionVectors    { set; get; }
        public Texture transparencyMask { set; get; }
        public Texture exposureTexture  { set; get; }
        public Texture biasColorMask    { set; get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DLSSCommandExecutionData
    {
        //// These properties must match the code in NVCommands.h in C++ ////
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

        /// DO NOT simplify these properties, alignment must match that one of the c++ counter part in NVCommands.h
        public   int   reset          { set { m_Reset          = value; } get { return m_Reset;          } }
        public   float sharpness      { set { m_Sharpness      = value; } get { return m_Sharpness;      } }
        public   float mvScaleX       { set { m_MVScaleX       = value; } get { return m_MVScaleX;       } }
        public   float mvScaleY       { set { m_MVScaleY       = value; } get { return m_MVScaleY;       } }
        public   float jitterOffsetX  { set { m_JitterOffsetX  = value; } get { return m_JitterOffsetX;  } }
        public   float jitterOffsetY  { set { m_JitterOffsetY  = value; } get { return m_JitterOffsetY;  } }
        public   float preExposure    { set { m_PreExposure    = value; } get { return m_PreExposure;    } }
        public   uint  subrectOffsetX { set { m_SubrectOffsetX = value; } get { return m_SubrectOffsetX; } }
        public   uint  subrectOffsetY { set { m_SubrectOffsetY = value; } get { return m_SubrectOffsetY; } }
        public   uint  subrectWidth   { set { m_SubrectWidth   = value; } get { return m_SubrectWidth;   } }
        public   uint  subrectHeight  { set { m_SubrectHeight  = value; } get { return m_SubrectHeight;  } }
        public   uint  invertXAxis    { set { m_InvertXAxis    = value; } get { return m_InvertXAxis;    } }
        public   uint  invertYAxis    { set { m_InvertYAxis    = value; } get { return m_InvertYAxis;    } }
        internal uint  featureSlot    { set { m_FeatureSlot    = value; } get { return m_FeatureSlot;    } }
        ////////////////////////////////////////////////////////////////////
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct OptimalDLSSSettingsData
    {
        //// These properties must match the code in NVCommands.h in C++ ////
        private readonly uint  m_OutRenderWidth;
        private readonly uint  m_OutRenderHeight;
        private readonly float m_Sharpness;
        private readonly uint  m_MaxWidth;
        private readonly uint  m_MaxHeight;
        private readonly uint  m_MinWidth;
        private readonly uint  m_MinHeight;
        ////////////////////////////////////////////////////////////////////

        /// DO NOT simplify these properties, alignment must match that one of the c++ counter part in NVCommands.h
        public uint  outRenderWidth  { get { return m_OutRenderWidth;  } }
        public uint  outRenderHeight { get { return m_OutRenderHeight; } }
        public float sharpness       { get { return m_Sharpness; } }
        public uint  maxWidth        { get { return m_MaxWidth;  } }
        public uint  maxHeight       { get { return m_MaxHeight; } }
        public uint  minWidth        { get { return m_MinWidth;  } }
        public uint  minHeight       { get { return m_MinHeight; } }
        ////////////////////////////////////////////////////////////////////
    }

    #endregion


    #region DebugData
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DLSSDebugFeatureInfos
    {
        //// These properties must match the code in NVCommands.h in C++ ////
        private readonly bool               m_ValidFeature;
        private readonly uint               m_FeatureSlot;
        private readonly DLSSCommandExecutionData m_ExecData;
        private readonly DLSSCommandInitializationData    m_InitData;
        ////////////////////////////////////////////////////////////////////

        //// These properties must match the code in NVCommands.h in C++ ////
        public bool validFeature           { get { return m_ValidFeature; } }
        public uint featureSlot            { get { return m_FeatureSlot;  } }
        public DLSSCommandExecutionData      execData { get { return m_ExecData; } }
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

    public class DLSSContext
    {
        private NativeData<DLSSCommandInitializationData> m_InitData = new NativeData<DLSSCommandInitializationData>();
        private NativeData<DLSSCommandExecutionData> m_ExecData = new NativeData<DLSSCommandExecutionData>();

        public ref readonly DLSSCommandInitializationData initData   { get { return ref m_InitData.Value; } }
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
            return m_ExecData.Ptr;
        }
    }

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

        public uint deviceVersion { get { return m_DeviceVersion; } }
        public uint ngxVersion { get { return m_NgxVersion; } }

        /// <summary>
        /// Gets an enumerable of the DLSS feature info.
        /// </summary>
        [Obsolete("This property causes garbage collection and is inefficient. Use dlssFeatureInfosSpan and dlssFeatureInfoCount instead.", false)]
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IEnumerable<DLSSDebugFeatureInfos> dlssFeatureInfos { get { return m_DlssDebugFeatures.Take((int)m_DlssFeatureValidCount); } }
#pragma warning restore RS0030

        /// <summary>
        /// Gets a read-only view into the valid DLSS feature info entries. Accessing this and iterating it with a for loop is allocation-free.
        /// </summary>
        public ReadOnlySpan<DLSSDebugFeatureInfos> dlssFeatureInfosSpan => new ReadOnlySpan<DLSSDebugFeatureInfos>(m_DlssDebugFeatures, 0, (int)m_DlssFeatureValidCount);
        
    }

    #endregion
} // namespace NVIDIA
