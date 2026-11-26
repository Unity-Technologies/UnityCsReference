// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditor;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Intel
{
	#region CmdData

	/// <summary>Bitflags for XeSS initialization.</summary>
	[Flags]
	public enum XeSSInitializationFlags
	{
		/// <summary>Use motion vectors at target resolution.</summary>
		HighResolutionMotionVector              = 1 << 0,
		///<summary>Use inverted (increased precision) depth encoding.</summary>
		InvertedDepth                           = 1 << 1,
		/// <summary>Use exposure texture to scale input color.</summary>
		ExposureScaleTexture                    = 1 << 2,
		/// <summary>Use responsive pixel mask texture.</summary>
		ResponsivePixelMask                     = 1 << 3,
		/// <summary>Use velocity in NDC.</summary>
		UseNdcVelcoity                          = 1 << 4,
		/// <summary>Use external descriptor heap.</summary>
		ExternalDescriptorHeap                  = 1 << 5,
		/// <summary>Disable tonemapping for input and output.</summary>
		LdrInputColor                           = 1 << 6,
		/// <summary>Remove jitter from input velocity.</summary>
		JitteredMotionVector                    = 1 << 7,
		/// <summary>Enable automatic exposure calculation.</summary>
		EnableAutoExposure                      = 1 << 8,
	}

	// One-to-one mapping with the values in xess_quality_settings_t.
	/// <summary>XeSS quality modes.</summary>
	public enum XeSSQuality
	{
		/// <summary>Upscaling with scaling factor of 3.0x.</summary>
		UltraPerformance = 0,
		/// <summary>Upscaling with scaling factor of 2.3x.</summary>
		Performance,
		/// <summary>Upscaling with scaling factor of 2.0x.</summary>
		Balanced,
		/// <summary>Upscaling with scaling factor of 1.7x.</summary>
		Quality,
		/// <summary>Upscaling with scaling factor of 1.5x.</summary>
		UltraQuality,
		/// <summary>Upscaling with scaling factor of 1.3x.</summary>
		UltraQualityPlus,
		/// <summary>Anti-aliasing at native resolution without upscaling.</summary>
		AA
	}

	// Struct layout must match with xess_version_t.
	/// <summary>Represents a version of a XeSS feature.</summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct XeSSFeatureVersion
	{
		/// <summary>Major version.</summary>
		public UInt16 major;
		/// <summary>Minor version.</summary>
		public UInt16 minor;
		/// <summary>Patch version.</summary>
		public UInt16 patch;
		/// <summary>Reserved for future use.</summary>
		public UInt16 reserved;
	}

	/// <summary>Texture parameters for XeSS upscaling.</summary>
	public struct XeSSTextureTable
	{
		/// <summary>Input color texture. This texture is mandatory.</summary>
		public Texture colorInput       { set; get; }
		/// <summary>Output color texture. This texture is mandatory.</summary>
		public Texture colorOutput      { set; get; }
		/// <summary>Input depth texture. This texture is mandatory and must be the same size as the input color texture.</summary>
		public Texture depth            { set; get; }
		/// <summary>Input motion vectors texture. This texture is mandatory and must be the same size as the input color texture.</summary>
		public Texture motionVectors    { set; get; }
		/// <summary>A 1x1 texture that contains exposure value to scale the input color. This texture is optional.</summary>
		public Texture exposureTexture  { set; get; }
		/// <summary>A mask that helps reducing ghosting artifacts where valid motion vectors information is missing. This texture is optional. If provided, it must be the same size as the input color texture.</summary>
		public Texture responsiveMask   { set; get; }
	}

	// Struct layout must match with the definition in XeSSCommands.h.
	/// <summary>Parameters to initialize a XeSS context.</summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct XeSSUpscalerCommandInitializationData
	{
		// Graphics API-agnostic parameters shared by all backends.
		// Each backend adds its own parameters (e.g., D3D12 descriptor heap or VK image views)

		///<summary>The maximum width of input resolution before upscaling.</summary>
		public   uint                    maxRenderSizeWidth;
		///<summary>The maximum height of input resolution before upscaling.</summary>
		public   uint                    maxRenderSizeHeight;
		/// <summary>The width of output resolution after upscaling.</summary>
		public   uint                    displaySizeWidth;
		/// <summary>The height of output resolution after upscaling.</summary>
		public   uint                    displaySizeHeight;
		/// <summary>XeSS quality mode to use.</summary>
		public   XeSSQuality             qualitySetting;
		/// <summary>See <see cref="XeSSInitializationFlags"/>.</summary>
		public   XeSSInitializationFlags xessFlags;

		// Plugin internal variables

		internal UIntPtr                 nativeHandle;
		internal int                     contextId;

		/// <summary>Helper function to set a flag.</summary>
		/// <param name="flag">Flag to set.</param>
		/// <param name="value">Value to assign.</param>
		public void SetFlag(XeSSInitializationFlags flag, bool value)
		{
			if (value) { xessFlags |=  flag; }
			else       { xessFlags &= ~flag; }
		}
		/// <summary>Helper function to get a flag.</summary>
		/// <param name="flag">Flag to get.</param>
		/// <returns>Value assigned to the flag.</returns>
		public bool GetFlag(XeSSInitializationFlags flag) { return (xessFlags & flag) != 0; }
	}

	// Struct layout must match with the definition in XeSSCommands.h.
	/// <summary>Parameters to execute XeSS.</summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct XeSSUpscalerCommandExecutionData
	{
		/// <summary>See <see cref="XeSSTextureTable"/>.</summary>
		internal enum Textures
		{
			ColorInput = 0,
			ColorOutput,
			Depth,
			MotionVectors,
			ExposureTexture,
			ResponsiveMask,
		};

		// Graphics API-agnostic parameters shared by all backends.
		// Each backend adds its own parameters (e.g., D3D12 descriptor heap or VK image views)

		/// <summary>Jitter X coordinate in the range [-0.5, 0.5].</summary>
		public float jitterOffsetX;
		/// <summary>Jitter Y coordinate in the range [-0.5, 0.5].</summary>
		public float jitterOffsetY;
		/// <summary>Optional input color scaling. Default is 1.</summary>
		public float exposureScale;
		/// <summary>Resets the history accumulation in this frame.</summary>
		public uint  resetHistory;
		/// <summary>Input color width.</summary>
		public uint  inputWidth;
		/// <summary>Input color height.</summary>
		public uint  inputHeight;
		/// <summary>Base X coordinate for the input color in the texture. Default is 0.</summary>
		public uint  inputColorBaseX;
		/// <summary>Base Y coordinate for the input color in the texture. Default is 0.</summary>
		public uint  inputColorBaseY;
		/// <summary>Base X coordinate for the input motion vector in the texture. Default is 0.</summary>
		public uint  inputMotionVectorBaseX;
		/// <summary>Base Y coordinate for the input motion vector in the texture. Default is 0.</summary>
		public uint  inputMotionVectorBaseY;
		/// <summary>Base X coordinate for the input depth in the texture. Default is 0.</summary>
		public uint  inputDepthBaseX;
		/// <summary>Base Y coordinate for the input depth in the texture. Default is 0.</summary>
		public uint  inputDepthBaseY;
		/// <summary>Base X coordinate for the input responsive pixel mask in the texture. Default is 0.</summary>
		public uint  inputResponsiveMaskBaseX;
		/// <summary>Base Y coordinate for the input responsive pixel mask in the texture. Default is 0.</summary>
		public uint  inputResponsiveMaskBaseY;
		/// <summary>Reserved parameter.</summary>
		public uint  reserved0X;
		/// <summary>Reserved parameter.</summary>
		public uint  reserved0Y;
		/// <summary>Base X coordinate for the output color. Default is 0.</summary>
		public uint  outputColorBaseX;
		/// <summary>Base Y coordinate for the output color. Default is 0.</summary>
		public uint  outputColorBaseY;

		// Plugin internal variables

		internal UIntPtr nativeHandle;
		internal int     contextId;
	}

	#endregion

	#region SerializationHelpers

	internal class NativeData<T> : IDisposable where T : struct
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

#pragma warning disable UAC0045 // Releases the native handle; destroy is thread-safe and the type has no Dispose, so the finalizer is the only release path.
		~NativeData() { Dispose(false); }
#pragma warning restore UAC0045
	}

	#endregion

	#region DeviceCommands

	#region XeSSUpscaler
	/// <summary>
	/// Persistent context for managing XeSS initialization and per-frame execution state.
	/// </summary>
	public class XeSSNativeContext
	{
		private NativeData<XeSSUpscalerCommandInitializationData> m_InitData = new NativeData<XeSSUpscalerCommandInitializationData>();
		private NativeData<XeSSUpscalerCommandExecutionData> m_ExecData = new NativeData<XeSSUpscalerCommandExecutionData>();
		private UIntPtr m_NativeHandle = UIntPtr.Zero;
		private int m_ContextId = -1;

		/// <seealso cref="XeSSUpscalerCommandInitializationData" />
		public ref readonly XeSSUpscalerCommandInitializationData initData { get { return ref m_InitData.Value; } }

		/// <seealso cref="XeSSUpscalerCommandExecutionData" />
		public ref XeSSUpscalerCommandExecutionData executeData { get { return ref m_ExecData.Value; } }

		internal int contextId { get { return m_ContextId; } }

		internal XeSSNativeContext() { }

		internal void Init(UIntPtr nativeHandle, int contextId, XeSSUpscalerCommandInitializationData initSettings)
		{
			m_NativeHandle = nativeHandle;
			m_ContextId = contextId;

			m_InitData.Value = initSettings;
			m_InitData.Value.nativeHandle = nativeHandle;
			m_InitData.Value.contextId = contextId;

			m_ExecData.Value.nativeHandle = nativeHandle;
		}

		internal void Reset()
		{
			m_NativeHandle = UIntPtr.Zero;
			m_ContextId = -1;
			m_InitData.Value = new XeSSUpscalerCommandInitializationData();
			m_ExecData.Value = new XeSSUpscalerCommandExecutionData();
		}

		internal IntPtr GetInitCmdPtr()
		{
			return m_InitData.Ptr;
		}

		internal IntPtr GetExecuteCmdPtr()
		{
			// m_ExecData is overwritten from outside; refill internal variables.
			m_ExecData.Value.nativeHandle = m_NativeHandle;
			m_ExecData.Value.contextId = contextId;

			return m_ExecData.Ptr;
		}
	}
	#endregion

	#endregion
} // namespace Intel
