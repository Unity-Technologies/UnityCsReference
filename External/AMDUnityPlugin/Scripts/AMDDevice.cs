#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: GraphicsDeviceFeatures not yet converted
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditor;


namespace UnityEngine.AMD
{
    // -----------------------------------------------------------------------------------
    //  Enums must match C++ enums found in AMDDevice.h
    // -----------------------------------------------------------------------------------
    #region GraphicsDeviceEnums
    internal enum PluginEvent
    {
        DestroyFeature = 0,

        FSRUpscale2Execute = 1,
        FSRUpscale2PostExecute = 2,
        FSRUpscale2Init = 3,

        FSRUpscale3Execute = 4,
        FSRUpscale3PostExecute = 5,
        FSRUpscale3Init = 6,

        FSRUpscale4Execute = 7,
        FSRUpscale4PostExecute = 8,
        FSRUpscale4Init = 9
    }

    ///<summary>
    /// AMD features that this plugin provides.
    ///</summary>
    public enum GraphicsDeviceFeature
    {
        ///<summary>FSR2 super resolution.</summary>
        FSRUpscale2 = 0,
        ///<summary>FSR3 super resolution.</summary>
        FSRUpscale3 = 1,
        ///<summary>FSR4 super resolution.</summary>
        FSRUpscale4 = 2
    }
    #endregion

    // -----------------------------------------------------------------------------------
    //  Main AMD device. Use to interact with AMD specific features on a unity SRP
    // -----------------------------------------------------------------------------------
    ///<summary>Provides the main entry point for the AMD Module. Use this to interact with the FSR2, FSR3, and FSR4 features.</summary>
    ///<remarks>
    ///The <c>GraphicsDevice</c> includes an interface for creating and managing feature contexts, handling FSR2 command execution, and providing utility methods for quality mode resolution management.
    ///
    ///<c>GraphicsDevice</c> is needed to implement FSR2, FSR3, and FSR4, outside of the built-in integration, for instance,
    ///FSR2 via the <see href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.2/manual/Dynamic-Resolution.html">HDRP Dynamic Resolution</see>.
    ///URP supports them via Upscaler Framework and HDRP supports FSR3 and FSR4 via the framework.
    ///
    ///Before using <c>GraphicsDevice</c>, ensure the <see cref="AMDUnityPlugin" /> is loaded and the device is initialized via <see cref="GraphicsDevice.CreateGraphicsDevice" />.
    ///</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.Rendering;
    ///using UnityEngine.Rendering.HighDefinition;
    ///using UnityEngine.AMD;
    ///
    /// // Example HDRP custom pass 
    ///public class CustomFSRPass : CustomPass
    ///{
    ///    public static bool EnsureAMDPluginLoaded()
    ///    {
    ///        if (!AMDUnityPlugin.IsLoaded())
    ///        {
    ///            Debug.Log("AMDUnityPlugin is not loaded!");
    ///            if (!AMDUnityPlugin.Load())
    ///            {
    ///                Debug.LogError("Unable to load AMDUnityPlugin");
    ///                return false;
    ///            }
    ///        }
    ///        Debug.Log("AMDUnityPlugin is successfully loaded!");
    ///        return true;
    ///    }
    ///
    ///    void InitializeAMDDevice()
    ///    {
    ///        if (!EnsureAMDPluginLoaded())
    ///            return;
    ///
    ///        // AMDUnityPlugin initialization will handle device creation for us.
    ///        // In case the device is not created, we call the static method GraphicsDevice.CreateGraphicsDevice().
    ///        amdDevice = GraphicsDevice.device == null ? GraphicsDevice.CreateGraphicsDevice() : GraphicsDevice.device;
    ///
    ///        Debug.LogFormat("AMD.GraphicsDevice initialized w/ version {0}", GraphicsDevice.version);
    ///    }
    ///
    ///    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    ///    {
    ///        if (amdDevice == null)
    ///        {
    ///            InitializeAMDDevice();
    ///        }
    ///        
    ///        float scalingRatio = fsr2Context == null ? 1.0f : amdDevice.GetUpscaleRatioFromQualityMode(m_Quality);
    ///        fsr2OutputColorBuffer = RTHandles.Alloc(
    ///            new Vector2(scalingRatio, scalingRatio),
    ///            dimension: TextureDimension.Tex2D,
    ///            colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
    ///            name: "fsr2OutputColorBuffer",
    ///            enableRandomWrite: true
    ///        );
    ///
    ///        // other pass setup code
    ///    }
    ///
    ///    protected override void Execute(CustomPassContext ctx)
    ///    {
    ///        bool initializeFsr2Context = fsr2Context == null || HasInputResolutionChanged(ctx) || HasOutputResolutionChanged(ctx);
    ///        if (initializeFsr2Context)
    ///        {
    ///            if (fsr2Context != null)
    ///            {
    ///                amdDevice.DestroyFeature(ctx.cmd, fsr2Context);
    ///                fsr2Context = null;
    ///            }
    ///
    ///            FSR2CommandInitializationData initData = new FSR2CommandInitializationData();
    ///            // populate initData
    ///            fsr2Context = amdDevice.CreateFeature(ctx.cmd, initData);
    ///        }
    ///
    ///        fsr2Context.executeData.enableSharpening = m_EnableSharpening ? 1 : 0;
    ///        // populate rest of fsr2Context.executeData 
    ///        
    ///        FSR2TextureTable fsr2TextureTable = new FSR2TextureTable()
    ///        {
    ///            // populate texture table
    ///        };
    ///
    ///        amdDevice.ExecuteFSR2(ctx.cmd, fsr2Context, fsr2TextureTable);
    ///    }
    ///
    ///    protected override void Cleanup()
    ///    {
    ///        // pass cleanup code
    ///
    ///        // No explicit clean up is necessary for AMD.GraphicsDevice, all handled internally
    ///    }
    ///
    ///    private GraphicsDevice amdDevice = null;
    ///    private FSR2Context fsr2Context = null;
    ///    private RTHandle fsr2OutputColorBuffer;
    ///    // other member variables
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="AMDUnityPlugin" />
    ///<seealso cref="FSR2Context" />
    ///<seealso cref="FSR2TextureTable" />
    ///<seealso cref="FSR2CommandInitializationData" />
    ///<seealso cref="FSR2CommandExecutionData" />
    ///<seealso cref="FSRUpscalerCommandInitializationData" />
    public class GraphicsDevice
    {
        #region Private

        static private GraphicsDevice sGraphicsDeviceInstance = null;
        private Stack<FSR2Context> s_ContextObjectPool_FSR2 = new Stack<FSR2Context>();
        private Stack<FSRUpscalerContext> s_ContextObjectPool_SDK = new Stack<FSRUpscalerContext>(); // FSR3/4

        private GraphicsDevice(){}
        private bool Initialize() { return AMDUP_InitApi(); }
        private void Shutdown() { AMDUP_ShutdownApi(); }
#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~GraphicsDevice() { Shutdown(); }
#pragma warning restore UA5000

        private void InsertEventCall(CommandBuffer cmd, PluginEvent pluginEvent, IntPtr ptr)
        {
            cmd.IssuePluginEventAndData(AMDUP_GetRenderEventCallback(), (int)pluginEvent + AMDUP_GetBaseEventId(), ptr);
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

            Debug.LogWarning("Unity has an invalid api for dvice. Init failed[");
            return null;
        }

        private static int CreateSetTextureUserData(int featureId, int textureSlot, bool clearTextureTable)
        {
            int featureIdMask = (featureId & 0xffff); //16 bits
            int textureSlotMask = (textureSlot & 0x7fff); //15 bits;
            int clearTableMask = clearTextureTable ? 0x1 : 0x0; //1 bit
            return (featureIdMask << 16) | (textureSlotMask << 1) | clearTableMask;
        }

        private void SetTexture(CommandBuffer cmd, int featureSlot, FSR2CommandExecutionData.Textures textureSlot, Texture texture, bool clearTextureTable = false)
        {
            if (texture == null)
                return;

            uint userData = (uint)CreateSetTextureUserData(featureSlot, (int)textureSlot, clearTextureTable);
            cmd.IssuePluginCustomTextureUpdateV2(
                AMDUP_GetSetTextureEventCallback(), texture, userData);
        }

        #endregion

        // -----------------------------------------------------------------------------------
        // Public API to interact with AMD Features
        // -----------------------------------------------------------------------------------
        #region PublicAPI

        ///<summary>Creates the main API object. Call this method only once in your application.</summary>
        ///<returns>The Device API object to access AMD features. If you call this function again, the function returns the same device.</returns>
        public static GraphicsDevice CreateGraphicsDevice() { return InternalCreate(); }
		
		///<summary>Gets the device created by GraphicsDevice.CreateGraphicsDevice. If the device hasn't been created this property evaluates to null.</summary>
        public static GraphicsDevice device { get { return sGraphicsDeviceInstance; } }
		
        ///<summary>Gets the version that corresponds to the Unity host plugin that manages the AMD.AMDUnityPlugin official library.</summary>
        public static uint version { get { return AMDUP_GetDeviceVersion(); } }

        ///<summary>
        ///Checks if current platform supports given feature.
        ///</summary>
        ///<param name="featureID">The feature to check.</param>
        ///<returns>Returns true on success. False otherwise.</returns>
        public bool IsFeatureAvailable(GraphicsDeviceFeature featureID) { return AMDUP_IsFeatureAvailable(featureID);}

        ///<summary>
        ///Gets the version of each feature.
        ///</summary>
        ///Before using it, ensure that the device is initialized via <see cref="GraphicsDevice.CreateGraphicsDevice" />.
        ///<param name="feature">The feature to query its version.</param>
        ///<param name="major">The major version of the feature.</param>
        ///<param name="minor">The minor version of the feature.</param>
        ///<param name="patch">The patch version of the feature.</param>
        ///<returns>Returns true if the query was successful, false otherwise.</returns>
        public static bool GetFeatureVersion(GraphicsDeviceFeature feature, out uint major, out uint minor, out uint patch)
        {
            return AMDUP_GetFeatureVersion(feature, out major, out minor, out patch);
        }

        ///<summary>
        ///Creates a FSR2 context.
        ///</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the command buffer. You must execute the command buffer yourself at any time after this call.</param>
        ///<param name="initSettings">A set of initialization settings for FSR2. If you want to change some options in it, then you need to create a new context.</param>
        ///<returns>A FSR2 context.</returns>
        public FSR2Context CreateFSRUpscale2(CommandBuffer cmd, in FSR2CommandInitializationData initSettings)
        {
            var fsrContext = s_ContextObjectPool_FSR2.Count == 0 ? new FSR2Context() : s_ContextObjectPool_FSR2.Pop();
            fsrContext.Init(initSettings, AMDUP_CreateFeatureSlot());
            InsertEventCall(cmd, PluginEvent.FSRUpscale2Init, fsrContext.GetInitCmdPtr());
            return fsrContext;
        }
		
        ///<summary>
        ///Creates a FSR3 context.
        ///</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the command buffer. You must execute the command buffer yourself at any time after this call.</param>
        ///<param name="initSettings">A set of initialization settings for FSR3. If you want to change some options in it, then you need to create a new context.</param>
        ///<returns>A FSR3 context.</returns>
        public FSRUpscalerContext CreateFSRUpscale3(CommandBuffer cmd, in FSRUpscalerCommandInitializationData initSettings)
        {
            var fsrContext = s_ContextObjectPool_SDK.Count == 0 ? new FSRUpscalerContext() : s_ContextObjectPool_SDK.Pop();
            fsrContext.Init(initSettings, AMDUP_CreateFeatureSlot());
            InsertEventCall(cmd, PluginEvent.FSRUpscale3Init, fsrContext.GetInitCmdPtr());
            return fsrContext;
        }

        ///<summary>
        ///Creates a FSR4 context.
        ///</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the command buffer. You must execute the command buffer yourself at any time after this call.</param>
        ///<param name="initSettings">A set of initialization settings for FSR4. If you want to change some options in it, then you need to create a new context.</param>
        ///<returns>A FSR4 context.</returns>
        public FSRUpscalerContext CreateFSRUpscale4(CommandBuffer cmd, in FSRUpscalerCommandInitializationData initSettings)
        {
            var fsrContext = s_ContextObjectPool_SDK.Count == 0 ? new FSRUpscalerContext() : s_ContextObjectPool_SDK.Pop();
            fsrContext.Init(initSettings, AMDUP_CreateFeatureSlot());
            InsertEventCall(cmd, PluginEvent.FSRUpscale4Init, fsrContext.GetInitCmdPtr());
            return fsrContext;
        }

        ///<summary>Queries the resolution configuration from a specified quality mode preset.</summary>
        ///<param name="qualityMode">The input quality mode. See <see cref="FSR2Quality" /> for the list of quality modes.</param>
        ///<param name="displayWidth">The input display resolution width.</param>
        ///<param name="displayHeight">The input display resolution height.</param>
        ///<param name="renderWidth">The output resolution width calculated.</param>
        ///<param name="renderHeight">The output resolution height calculated.</param>
        ///<returns>Returns true on success. False otherwise.</returns>
        public bool GetRenderResolutionFromQualityMode(FSR2Quality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight)
        {
            return AMDUP_GetRenderResolutionFromQualityMode(qualityMode, displayWidth, displayHeight, out renderWidth, out renderHeight);
        }
		
        ///<summary>Queries the resolution configuration from a specified quality mode preset.</summary>
        ///<param name="qualityMode">The input quality mode. See <see cref="FSR3Quality" /> for the list of quality modes.</param>
        ///<param name="displayWidth">The input display resolution width.</param>
        ///<param name="displayHeight">The input display resolution height.</param>
        ///<param name="renderWidth">The output resolution width calculated.</param>
        ///<param name="renderHeight">The output resolution height calculated.</param>
        ///<returns>Returns true on success. False otherwise.</returns>
        public bool GetRenderResolutionFromQualityModeFSR3(FSR3Quality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight)
        {
            return AMDUP_GetRenderResolutionFromQualityModeFSR3(qualityMode, displayWidth, displayHeight, out renderWidth, out renderHeight);
        }

        ///<summary>Queries the resolution configuration from a specified quality mode preset.</summary>
        ///<param name="qualityMode">The input quality mode. See <see cref="FSR4Quality" /> for the list of quality modes.</param>
        ///<param name="displayWidth">The input display resolution width.</param>
        ///<param name="displayHeight">The input display resolution height.</param>
        ///<param name="renderWidth">The output resolution width calculated.</param>
        ///<param name="renderHeight">The output resolution height calculated.</param>
        ///<returns>Returns true on success. False otherwise.</returns>
        public bool GetRenderResolutionFromQualityModeFSR4(FSR4Quality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight)
        {
            return AMDUP_GetRenderResolutionFromQualityModeFSR4(qualityMode, displayWidth, displayHeight, out renderWidth, out renderHeight);
        }
		
		///<summary>Gets a precomputed upscaling ratio based on a preset quality setting.</summary>
        ///<param name="qualityMode">The input quality mode. See <see cref="FSR2Quality" /> for the list of quality modes.</param>
        ///<returns>The upscaling per-dimension ratio.</returns>
        public float GetUpscaleRatioFromQualityMode(FSR2Quality qualityMode) { return AMDUP_GetUpscaleRatioFromQualityMode(qualityMode); }

        ///<summary>Gets a precomputed upscaling ratio based on a preset quality setting.</summary>
        ///<param name="qualityMode">The input quality mode. See <see cref="FSR3Quality" /> for the list of quality modes.</param>
        ///<returns>The upscaling per-dimension ratio.</returns>
        public float GetUpscaleRatioFromQualityMode(FSR3Quality qualityMode) { return AMDUP_GetUpscaleRatioFromQualityModeFSR3(qualityMode); }

        ///<summary>Gets a precomputed upscaling ratio based on a preset quality setting.</summary>
        ///<param name="qualityMode">The input quality mode. See <see cref="FSR4Quality" /> for the list of quality modes.</param>
        ///<returns>The upscaling per-dimension ratio.</returns>
        public float GetUpscaleRatioFromQualityMode(FSR4Quality qualityMode) { return AMDUP_GetUpscaleRatioFromQualityModeFSR4(qualityMode); }

        // =========================================================================
        // LEGACY API - Thin wrappers for backwards compatibility
        // =========================================================================

        ///<summary>This method has been deprecated. See GraphicsDevice.CreateFSRUpscale2.</summary>
        [System.Obsolete("Use CreateFSRUpscale2 instead. Both APIs remain supported.", false)]
        public FSR2Context CreateFeature(CommandBuffer cmd, in FSR2CommandInitializationData initSettings)
        {
            return CreateFSRUpscale2(cmd, initSettings);
        }

        ///<summary>This method has been deprecated. See GraphicsDevice.ExecuteFSRUpscale2.</summary>
        [System.Obsolete("Use ExecuteFSRUpscale2 instead. Both APIs remain supported.", false)]
        public void ExecuteFSR2(CommandBuffer cmd, FSR2Context fsr2Context, in FSR2TextureTable textures)
        {
            ExecuteFSRUpscale2(cmd, fsr2Context, textures);
        }

        ///<summary>Destroys a specific FSR2Context created with GraphicsDevice.CreateFSRUpscale2.</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the command buffer. You must execute the command buffer yourself at any time after this call.</param>
        ///<param name="fsrContext">The command object to destroy.</param>
        public void DestroyFeature(CommandBuffer cmd, FSR2Context fsrContext)
        {
            InsertEventCall(cmd, PluginEvent.DestroyFeature, new IntPtr(fsrContext.featureSlot));
            fsrContext.Reset();
            s_ContextObjectPool_FSR2.Push(fsrContext);
        }
		
        ///<summary>Destroys a specific FSRUpscalerContext created with GraphicsDevice.CreateFSRUpscale3 or GraphicsDevice.CreateFSRUpscale4.</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the command buffer. You must execute the command buffer yourself at any time after this call.</param>
        ///<param name="fsrContext">The command object to destroy.</param>
        public void DestroyFeature(CommandBuffer cmd, FSRUpscalerContext fsrContext)
        {
            InsertEventCall(cmd, PluginEvent.DestroyFeature, new IntPtr(fsrContext.featureSlot));
            fsrContext.Reset();
            s_ContextObjectPool_SDK.Push(fsrContext);
        }

		///<summary>Records the execution of the FSR2 pass into a rendering command buffer. This call does not execute the command buffer, it only appends custom commands into it.</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the command buffer. You must execute the command buffer yourself at any time after this call.</param>
        ///<param name="fsr2Context">The source feature context to execute. You must set the parameters for this command in the <see cref="FSR2Context" /> object prior to this call.</param>
        ///<param name="textures">The collection of textures represented by <see cref="FSR2TextureTable" />, where inputs/outputs are specified for the FSR2 pass to execute.</param>
        public void ExecuteFSRUpscale2(CommandBuffer cmd, FSR2Context fsr2Context, in FSR2TextureTable textures)
        {
            SetUpscalerTextures(cmd, (int)fsr2Context.featureSlot, in textures);
            InsertEventCall(cmd, PluginEvent.FSRUpscale2Execute, fsr2Context.GetExecuteCmdPtr());

            // D3D12 requires to pump submission into its own thread.
            // this is caused by the current implementation of the plugin.
            // this function is probably noop in other graphics APIs
            InsertEventCall(cmd, PluginEvent.FSRUpscale2PostExecute, fsr2Context.GetExecuteCmdPtr());
        }

        ///<summary>Records the execution of the FSR3 pass into a rendering command buffer. This call does not execute the command buffer, it only appends custom commands into it.</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the command buffer. You must execute the command buffer yourself at any time after this call.</param>
        ///<param name="fsr3Context">The source feature context to execute. You must set the parameters for this command in the <see cref="FSRUpscalerContext" /> object prior to this call.</param>
        ///<param name="textures">The collection of textures represented by <see cref="FSR2TextureTable" />, where inputs/outputs are specified for the FSR3 pass to execute (FSR2 and FSR3 share the same texture input table).</param>
        public void ExecuteFSRUpscale3(CommandBuffer cmd, FSRUpscalerContext fsr3Context, in FSR2TextureTable textures)
        {
            SetUpscalerTextures(cmd, (int)fsr3Context.featureSlot, in textures);
            InsertEventCall(cmd, PluginEvent.FSRUpscale3Execute, fsr3Context.GetExecuteCmdPtr());
            InsertEventCall(cmd, PluginEvent.FSRUpscale3PostExecute, fsr3Context.GetExecuteCmdPtr());
        }

        ///<summary>Records the execution of the FSR4 pass into a rendering command buffer. This call does not execute the command buffer, it only appends custom commands into it.</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the command buffer. You must execute the command buffer yourself at any time after this call.</param>
        ///<param name="fsr4Context">The source feature context to execute. You must set the parameters for this command in the <see cref="FSRUpscalerContext" /> object prior to this call.</param>
        ///<param name="textures">The collection of textures represented by <see cref="FSR2TextureTable" />, where inputs/outputs are specified for the FSR4 pass to execute (FSR2 and FSR4 share the same texture input table).</param>
        public void ExecuteFSRUpscale4(CommandBuffer cmd, FSRUpscalerContext fsr4Context, in FSR2TextureTable textures)
        {
            SetUpscalerTextures(cmd, (int)fsr4Context.featureSlot, in textures);
            InsertEventCall(cmd, PluginEvent.FSRUpscale4Execute, fsr4Context.GetExecuteCmdPtr());
            InsertEventCall(cmd, PluginEvent.FSRUpscale4PostExecute, fsr4Context.GetExecuteCmdPtr());
        }

        // =========================================================================
        // Utils
        // =========================================================================
        private void SetUpscalerTextures(CommandBuffer cmd, int featureSlot, in FSR2TextureTable textures)
        {
            SetTexture(cmd, featureSlot, FSR2CommandExecutionData.Textures.ColorInput, textures.colorInput, true);
            SetTexture(cmd, featureSlot, FSR2CommandExecutionData.Textures.ColorOutput, textures.colorOutput);
            SetTexture(cmd, featureSlot, FSR2CommandExecutionData.Textures.Depth, textures.depth);
            SetTexture(cmd, featureSlot, FSR2CommandExecutionData.Textures.MotionVectors, textures.motionVectors);
            SetTexture(cmd, featureSlot, FSR2CommandExecutionData.Textures.ReactiveMask, textures.reactiveMask);
            SetTexture(cmd, featureSlot, FSR2CommandExecutionData.Textures.TransparencyMask, textures.transparencyMask);
            SetTexture(cmd, featureSlot, FSR2CommandExecutionData.Textures.ExposureTexture, textures.exposureTexture);
            SetTexture(cmd, featureSlot, FSR2CommandExecutionData.Textures.BiasColorMask, textures.biasColorMask);
        }

        #endregion

        // -----------------------------------------------------------------------------------
        // Debug View Support
        // -----------------------------------------------------------------------------------

        #region DebugView

        // Must match with C++ definition of FSRUpscaleFeatureDebugInfo.
        ///<summary>Debug info for a single FSR feature slot.</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct FSRUpscaleFeatureDebugInfo
        {
            ///<summary>True if this struct contains valid data.</summary>
            public bool validFeature;

            ///<summary>The slot index associated with the feature.</summary>
            public uint featureSlot;

            ///<summary>Major version of the feature.</summary>
            public uint majorVersion;

            ///<summary>Minor version of the feature.</summary>
            public uint minorVersion;

            ///<summary>Patch version of the feature.</summary>
            public uint patchVersion;

            ///<summary>Upscaler context initialization parameters for this feature.</summary>
            public FSR2CommandInitializationData initData;

            /// <summary>Actual render width. Be aware that initData.maxRenderSizeWidth is the width of the allocated render target and might be greater than actual render width.</summary>
            public uint renderWidth;

            /// <summary>Actual render height. Be aware that initData.maxRenderSizeHeight is the height of the allocated render target and might be greater than actual render height.</summary>
            public uint renderHeight;

            ///<summary>0: custom scale, 1: native AA, 2: quality, 3: balanced, 4: performance, 5: ultra performance. Be aware that the values do not match with the values of FSR2Quality, FSR3Quality, nor FSR4Quality.</summary>
            public uint qualityMode;
        }

        ///<summary>Provides information to implement a debug view, including versions of the device and FidelityFX SDK, and available features.</summary>
        public class GraphicsDeviceDebugView
        {
            // Main debug view handle. An opaque pointer managed by C++ plugin.
            internal IntPtr nativeHandle;

            ///<summary>The value returned by GraphicsDevice.version.</summary>
            public uint deviceVersion;

            ///<summary>The major version of FidelityFX SDK.</summary>
            public uint fidelityFxSdkVersionMajor;

            ///<summary>The minor version of FidelityFX SDK.</summary>
            public uint fidelityFxSdkVersionMinor;

            ///<summary>The patch version of FidelityFX SDK.</summary>
            public uint fidelityFxSdkVersionPatch;

            ///<summary>The number of currently active features.</summary>
            public uint upscaleFeatureInfoCount;

            internal FSRUpscaleFeatureDebugInfo[] upscaleFeatureInfoArray;

            internal GraphicsDeviceDebugView()
            {
                upscaleFeatureInfoArray = new FSRUpscaleFeatureDebugInfo[16];  // Max 16 feature slots
            }

            ///<summary>Span of active features.</summary>
            public Span<FSRUpscaleFeatureDebugInfo> upscaleFeatureInfoSpan
            {
                get { return new Span<FSRUpscaleFeatureDebugInfo>(upscaleFeatureInfoArray, 0, (int)upscaleFeatureInfoCount); }
            }
        }

        ///<summary>Allocates and returns a debug view. The view must be updated with UpdateDebugView() and deallocated with DeleteDebugView().</summary>
        public GraphicsDeviceDebugView CreateDebugView()
        {
            var debugView = new GraphicsDeviceDebugView();
            debugView.nativeHandle = AMDUP_CreateDebugView();
            return debugView;
        }

        ///<summary>Deletes a debug view.</summary>
        public void DeleteDebugView(GraphicsDeviceDebugView debugView)
        {
            if (debugView != null && debugView.nativeHandle != IntPtr.Zero)
            {
                AMDUP_DeleteDebugView(debugView.nativeHandle);
                debugView.nativeHandle = IntPtr.Zero;
            }
        }

        ///<summary>Updates a debug view.</summary>
        public void UpdateDebugView(GraphicsDeviceDebugView debugView)
        {
            if (debugView == null || debugView.nativeHandle == IntPtr.Zero)
                return;

            debugView.deviceVersion = AMDUP_GetDeviceVersion();
            AMDUP_GetFidelityFxSdkVersion(out debugView.fidelityFxSdkVersionMajor, out debugView.fidelityFxSdkVersionMinor, out debugView.fidelityFxSdkVersionPatch);

            debugView.upscaleFeatureInfoCount = AMDUP_GetDebugFeatureCount(debugView.nativeHandle);

            unsafe
            {
                fixed (FSRUpscaleFeatureDebugInfo* ptr = debugView.upscaleFeatureInfoArray)
                {
                    AMDUP_GetDebugFeatureInfo(debugView.nativeHandle, (IntPtr)ptr, (uint)debugView.upscaleFeatureInfoArray.Length);
                }
            }
        }

        #endregion

        // -----------------------------------------------------------------------------------
        // All required imports for the plugin
        // -----------------------------------------------------------------------------------

        #region Imports

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static bool AMDUP_InitApi();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static void AMDUP_ShutdownApi();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern uint AMDUP_GetDeviceVersion();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static bool AMDUP_IsFeatureAvailable(GraphicsDeviceFeature featureID);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr AMDUP_GetRenderEventCallback();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr AMDUP_GetSetTextureEventCallback();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern uint AMDUP_CreateFeatureSlot();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool AMDUP_GetRenderResolutionFromQualityMode(FSR2Quality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool AMDUP_GetRenderResolutionFromQualityModeFSR3(FSR3Quality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool AMDUP_GetRenderResolutionFromQualityModeFSR4(FSR4Quality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern float AMDUP_GetUpscaleRatioFromQualityMode(FSR2Quality qualityMode);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern float AMDUP_GetUpscaleRatioFromQualityModeFSR3(FSR3Quality qualityMode);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern float AMDUP_GetUpscaleRatioFromQualityModeFSR4(FSR4Quality qualityMode);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern int AMDUP_GetBaseEventId();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr AMDUP_CreateDebugView();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void AMDUP_DeleteDebugView(IntPtr debugView);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool AMDUP_GetFidelityFxSdkVersion(out uint major, out uint minor, out uint patch);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool AMDUP_GetFeatureVersion(GraphicsDeviceFeature feature, out uint major, out uint minor, out uint patch);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern uint AMDUP_GetDebugFeatureCount(IntPtr debugView);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void AMDUP_GetDebugFeatureInfo(IntPtr debugView, IntPtr outInfoArray, uint arraySize);

        #endregion
    };
} // namespace AMD
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
