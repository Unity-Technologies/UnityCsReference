using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditor;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.AMD
{
    #region CmdData

    //Flags Verbatim from ffx_fsr.h
    [Flags]
    public enum FfxFsr2InitializationFlags
    {
        EnableHighDynamicRange                  = (1<<0),   ///< A bit indicating if the input color data provided is using a high-dynamic range.
        EnableDisplayResolutionMotionVectors    = (1<<1),   ///< A bit indicating if the motion vectors are rendered at display resolution.
        EnableMotionVectorsJitterCancellation   = (1<<2),   ///< A bit indicating that the motion vectors have the jittering pattern applied to them.
        DepthInverted                           = (1<<3),   ///< A bit indicating that the input depth buffer data provided is inverted [1..0].
        EnableDepthInfinite                     = (1<<4),   ///< A bit indicating that the input depth buffer data provided is using an infinite far plane.
        EnableAutoExposure                      = (1<<5),   ///< A bit indicating if automatic exposure should be applied to input color data.
        EnableDynamicResolution                 = (1<<6),   ///< A bit indicating that the application uses dynamic resolution scaling.
        EnableTexture1DUsage                    = (1<<7)    ///< A bit indicating that the backend should use 1D textures.
    }

    //Quality mode verbatim from AMDCommands.h
    public enum FSR2Quality
    {
        Quality = 0,
        Balanced,
        Performance,
        UltraPerformance
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FSR2CommandInitializationData
    {
        //// These properties must match the code in AMDCommands.h in C++ ////
        public uint maxRenderSizeWidth;
        public uint maxRenderSizeHeight;
        public uint displaySizeWidth;
        public uint displaySizeHeight;
        public FfxFsr2InitializationFlags ffxFsrFlags;
        internal uint featureSlot;
        ////////////////////////////////////////////////////////////////////

        public void SetFlag(FfxFsr2InitializationFlags flag, bool value)
        {
            if (value)
            {
                ffxFsrFlags |= flag;
            }
            else
            {
                ffxFsrFlags &= ~flag;
            }
        }

        public bool GetFlag(FfxFsr2InitializationFlags flag)
        {
            return (ffxFsrFlags & flag) != 0;
        }
    }

    public struct FSR2TextureTable
    {
        public Texture colorInput       { set; get; }
        public Texture colorOutput      { set; get; }
        public Texture depth            { set; get; }
        public Texture motionVectors    { set; get; }
        public Texture transparencyMask { set; get; }
        public Texture exposureTexture  { set; get; }
        public Texture reactiveMask     { set; get; }
        public Texture biasColorMask    { set; get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FSR2CommandExecutionData
    {
        //// These properties must match the code in AMDCommands.h in C++ ////
        internal enum Textures
        {
            ColorInput = 0,
            ColorOutput,
            Depth,
            MotionVectors,
            TransparencyMask,
            ExposureTexture,
            ReactiveMask,
            BiasColorMask,
        };

        public float    jitterOffsetX;
        public float    jitterOffsetY;
        public float    MVScaleX;
        public float    MVScaleY;
        public uint     renderSizeWidth;
        public uint     renderSizeHeight;
        public int      enableSharpening;
        public float    sharpness;
        public float    frameTimeDelta;
        public float    preExposure;
        public int      reset;
        public float    cameraNear;
        public float    cameraFar;
        public float    cameraFovAngleVertical;
        internal uint  featureSlot;
        ////////////////////////////////////////////////////////////////////
    }

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

    #endregion

    #region DeviceCommands

    public class FSR2Context
    {
        private NativeData<FSR2CommandInitializationData> m_InitData = new NativeData<FSR2CommandInitializationData>();
        private NativeData<FSR2CommandExecutionData> m_ExecData = new NativeData<FSR2CommandExecutionData>();

        public ref readonly FSR2CommandInitializationData initData   { get { return ref m_InitData.Value; } }
        public ref FSR2CommandExecutionData executeData { get { return ref m_ExecData.Value; } }
        internal uint                   featureSlot { get { return initData.featureSlot; } }

        internal FSR2Context()
        {
        }

        internal void Init(FSR2CommandInitializationData initSettings, uint featureSlot)
        {
            m_InitData.Value = initSettings;
            m_InitData.Value.featureSlot = featureSlot;
        }

        internal void Reset()
        {
            m_InitData.Value = new FSR2CommandInitializationData();
            m_ExecData.Value = new FSR2CommandExecutionData();
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

    #endregion
} // namespace AMD
