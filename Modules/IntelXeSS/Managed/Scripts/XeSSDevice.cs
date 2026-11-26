// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Intel
{
	// These enums must match with C++ enums in XeSSRenderAPI.h
	#region GraphicsDevice Data
	internal enum PluginEvent
	{
		XeSSUpscaleInit                = 0,
		XeSSUpscaleExecute             = 1,
		XeSSUpscaleDestroy             = 2,
	}

	/// <summary>XeSS features provided by this plugin.</summary>
	public enum GraphicsDeviceFeature
	{
		/// <summary>XeSS Super Resolution.</summary>
		XeSSUpscale                    = 0,
	}
	#endregion

	/// <summary>
	/// Provides the main entry point for the XeSS module. Use this to interact with XeSS features.
	/// </summary>
	/// <remarks>
	/// Before using <c>GraphicsDevice</c>, ensure that <see cref="UnityEngine.Intel.XeSSUnityPlugin"/> is loaded and the device is initialized via <see cref="GraphicsDevice.CreateGraphicsDevice" />.
	/// </remarks>
	public class GraphicsDevice
	{
		#region Private

		[NoAutoStaticsCleanup]
		static private GraphicsDevice sGraphicsDeviceInstance = null;

		private Stack<XeSSNativeContext> s_ContextObjectPool_SDK = new Stack<XeSSNativeContext>();

		private GraphicsDevice(){}

#pragma warning disable UAC0045 // Releases the native handle; destroy is thread-safe and the type has no Dispose, so the finalizer is the only release path.
		~GraphicsDevice() { Shutdown(); }
#pragma warning restore UAC0045

		private bool Initialize()
		{
			return true;
		}

		private void Shutdown()
		{
		}

		private static GraphicsDevice InternalCreate()
		{
			if (sGraphicsDeviceInstance != null)
			{
				sGraphicsDeviceInstance.Shutdown();
				sGraphicsDeviceInstance.Initialize();
				return sGraphicsDeviceInstance;
			}

			var newGraphicsDevice = new GraphicsDevice();
			if (newGraphicsDevice.Initialize())
			{
				sGraphicsDeviceInstance = newGraphicsDevice;
				return newGraphicsDevice;
			}

			Debug.LogWarning("Unity has an invalid api for device. Init failed.");
			return null;
		}

		private void InsertEventCall(CommandBuffer cmd, PluginEvent pluginEvent, IntPtr ptr)
		{
			cmd.IssuePluginEventAndData(XESSUP_GetRenderEventCallback(), (int)pluginEvent + XESSUP_GetBaseEventId(), ptr);
		}

		private static int CreateSetTextureUserData(int xessContextId, int textureSlot, bool clearTextureTable)
		{
			int xessContextIdMask = (xessContextId & 0xffff); //16 bits
			int textureSlotMask = (textureSlot & 0x7fff); //15 bits;
			int clearTableMask = clearTextureTable ? 0x1 : 0x0; //1 bit
			return (xessContextIdMask << 16) | (textureSlotMask << 1) | clearTableMask;
		}

		private void SetTexture(CommandBuffer cmd, int xessContextId, XeSSUpscalerCommandExecutionData.Textures textureSlot, Texture texture, bool clearTextureTable = false)
		{
			if (texture == null)
				return;

			uint userData = (uint)CreateSetTextureUserData(xessContextId, (int)textureSlot, clearTextureTable);
			cmd.IssuePluginCustomTextureUpdateV2(XESSUP_GetSetTextureEventCallback(), texture, userData);
		}

		private void SetUpscalerTextures(CommandBuffer cmd, int xessContextId, in XeSSTextureTable textures)
		{
			SetTexture(cmd, xessContextId, XeSSUpscalerCommandExecutionData.Textures.ColorInput, textures.colorInput, true);
			SetTexture(cmd, xessContextId, XeSSUpscalerCommandExecutionData.Textures.ColorOutput, textures.colorOutput);
			SetTexture(cmd, xessContextId, XeSSUpscalerCommandExecutionData.Textures.Depth, textures.depth);
			SetTexture(cmd, xessContextId, XeSSUpscalerCommandExecutionData.Textures.MotionVectors, textures.motionVectors);
			SetTexture(cmd, xessContextId, XeSSUpscalerCommandExecutionData.Textures.ResponsiveMask, textures.responsiveMask);
			SetTexture(cmd, xessContextId, XeSSUpscalerCommandExecutionData.Textures.ExposureTexture, textures.exposureTexture);
		}

		#endregion

		#region PublicAPI

		/// <summary>Creates a unique device instance. If already exist, the existing instance is re-initialized.</summary>
		/// <returns>The device instance.</returns>
		public static GraphicsDevice CreateGraphicsDevice() => InternalCreate();

		/// <summary>Gets the unique instance of the device.</summary>
		public static GraphicsDevice device => sGraphicsDeviceInstance;

		/// <summary>Gets current version of the device.</summary>
		public static uint version => XESSUP_GetDeviceVersion();

		/// <summary>Gets current version of the given XeSS feature.</summary>
		/// <param name="feature">XeSS feature ID.</param>
		/// <returns>Current version.</returns>
		public static XeSSFeatureVersion GetFeatureVersion(GraphicsDeviceFeature feature) => XESSUP_GetFeatureVersion(feature);

		/// <summary>Checks if the given XeSS feature is supported on current device. It's recommended to invoke it once and cache the result.</summary>
		/// <param name="feature">XeSS feature ID.</param>
		/// <returns>true if supported, false otherwise.</returns>
		public bool IsFeatureAvailable(GraphicsDeviceFeature feature) => XESSUP_IsFeatureAvailable(feature);

		/// <summary>Given quality mode and output resolution, calculates the optimal input resolution.</summary>
		/// <param name="qualityMode">XeSS quality mode.</param>
		/// <param name="displayWidth">Width of output resolution.</param>
		/// <param name="displayHeight">Height of output resolution.</param>
		/// <param name="renderWidth">Width of optimal input resolution.</param>
		/// <param name="renderHeight">Height of optimal input resolution.</param>
		/// <returns>true if the query was successful, false otherwise.</returns>
		public bool GetRenderResolutionFromQualityMode(XeSSQuality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight)
		{
			return XESSUP_GetRenderResolutionFromQualityMode(qualityMode, displayWidth, displayHeight, out renderWidth, out renderHeight);
		}

		/// <summary>Creates a XeSS context and initializes it for upscaling.</summary>
		/// <remarks>It may fail if XeSS is not supported on current device. Check if it's supported first by using <see cref="GraphicsDevice.IsFeatureAvailable" />.</remarks>
		/// <param name="cmd">Command buffer to record context initialization commands.</param>
		/// <param name="initSettings">Initialization settings.</param>
		/// <returns>A XeSS context.</returns>
		public XeSSNativeContext CreateNativeContext(CommandBuffer cmd, in XeSSUpscalerCommandInitializationData initSettings)
		{
			if (XESSUP_CreateContextHandle(out var xessHandle, out var xessContextId))
			{
				var ctx = s_ContextObjectPool_SDK.Count == 0 ? new XeSSNativeContext() : s_ContextObjectPool_SDK.Pop();
				ctx.Init(xessHandle, xessContextId, initSettings);
				InsertEventCall(cmd, PluginEvent.XeSSUpscaleInit, ctx.GetInitCmdPtr());
				return ctx;
			}
			return null;
		}

		/// <summary>Destroys a XeSS context.</summary>
		/// <param name="cmd">Command buffer to record context destruction commands.</param>
		/// <param name="xessContext">The XeSS context to destroy.</param>
		public void DestroyNativeContext(CommandBuffer cmd, XeSSNativeContext xessContext)
		{
			// Destroys the xess context handle internally; no need to call XESSUP_DestroyContextHandle.
			InsertEventCall(cmd, PluginEvent.XeSSUpscaleDestroy, new IntPtr(xessContext.contextId));

			xessContext.Reset();
			s_ContextObjectPool_SDK.Push(xessContext);
		}
		
		/// <summary>Sets motion vector scaling factor for the given context. Useful when the motion vector texture stores motion vector values in units other than pixels.</summary>
		/// <param name="xessContext">The XeSS context to set motion vector scale.</param>
		/// <param name="scale">The motion vector scale value to set.</param>
		/// <returns>true if successful, false otherwise.</returns>
		public bool SetMotionVectorScale(XeSSNativeContext xessContext, Vector2 scale)
		{
			return XESSUP_SetVelocityScale((int)xessContext.contextId, scale.x, scale.y);
		}
		
		/// <summary>Sets exposure multiplier for the given context. It applies to all exposure modes: auto exposure, exposure texture, and exposure scale parameter.</summary>
		/// <param name="xessContext">The XeSS context to set exposure multiplier.</param>
		/// <param name="scale">The exposure multiplier value to set.</param>
		/// <returns>true if successful, false otherwise.</returns>
		public bool SetExposureMultiplier(XeSSNativeContext xessContext, float scale)
		{
			return XESSUP_SetExposureMultiplier((int)xessContext.contextId, scale);
		}

		/// <summary>Execute XeSS upscaling with given texture parameters.</summary>
		/// <param name="cmd">Command buffer to record upscaling commands.</param>
		/// <param name="xessContext">A valid XeSS context.</param>
		/// <param name="textures">Texture parameters for XeSS.</param>
		public void ExecuteUpscale(CommandBuffer cmd, XeSSNativeContext xessContext, in XeSSTextureTable textures)
		{
			SetUpscalerTextures(cmd, (int)xessContext.contextId, in textures);
			InsertEventCall(cmd, PluginEvent.XeSSUpscaleExecute, xessContext.GetExecuteCmdPtr());
		}
		#endregion

		// -----------------------------------------------------------------------------------
		// All required imports for the plugin
		// -----------------------------------------------------------------------------------

		#region Imports

		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		private static extern uint XESSUP_GetDeviceVersion();

		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		private static extern XeSSFeatureVersion XESSUP_GetFeatureVersion(GraphicsDeviceFeature feature);

		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.I1)]
		private extern static bool XESSUP_IsFeatureAvailable(GraphicsDeviceFeature featureID);

		// Creates a XeSS context handle along with a unique id.
		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.I1)]
		private extern static bool XESSUP_CreateContextHandle(out UIntPtr hContext, out int contextId);

		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.I1)]
		private extern static bool XESSUP_DestroyContextHandle(int xessContextId);

		// Base ID for plugin-specific events.
		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		private static extern int XESSUP_GetBaseEventId();

		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr XESSUP_GetRenderEventCallback();

		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr XESSUP_GetSetTextureEventCallback();

		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.I1)]
		private static extern bool XESSUP_GetRenderResolutionFromQualityMode(XeSSQuality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight);
		
		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.I1)]
		private static extern bool XESSUP_SetVelocityScale(int xessContextId, float x, float y);
		
		[DllImport("XeSSUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.I1)]
		private static extern bool XESSUP_SetExposureMultiplier(int xessContextId, float scale);

		#endregion
	};
} // namespace Intel
