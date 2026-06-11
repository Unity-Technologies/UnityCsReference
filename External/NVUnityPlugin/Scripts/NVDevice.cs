using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditor;


namespace UnityEngine.NVIDIA
{
    // -----------------------------------------------------------------------------------
    //  Public Enums must match C++ enums found in NVGraphicsDevice.h
    // -----------------------------------------------------------------------------------
    #region GraphicsDeviceEnums

    /// <summary>Lists every feature ID the <see cref="GraphicsDevice" /> API supports.</summary>
    /// <remarks>For now, this only includes **Deep Learning Super Sampling** (DLSS). To check if the device supports a
    /// feature, call <see cref="GraphicsDevice.IsFeatureAvailable" />.</remarks>
    public enum GraphicsDeviceFeature
    {
        /// <summary>Represents the **Deep Learning Super Sampling** (DLSS) feature.</summary>
        DLSS,
        //Not supported yet. We dont expose these on the main API.
        //DLISP,
        //IP,
        //ISR,
        //SM,
        //VSR,
    }

    internal enum PluginEvent
    {
        DestroyFeature = 0,
        DLSSExecute = 1,
        DLSSInit = 2
    }

    #endregion
    
    /// <summary>Provides the main entry point for the NVIDIA Module.</summary>
    /// <remarks>Use this to interact with specific NVIDIA graphics card features.</remarks>
    public class GraphicsDevice
    {
        //Application ID obtained from NVIDIA or ProjectID of the Unity project
        private static string  s_DefaultProjectID = "231313132";
        private static string  s_DefaultAppDir    = ".\\";

        // -----------------------------------------------------------------------------------
        // Private command buffer / helpers / initializers and utilities
        // -----------------------------------------------------------------------------------
        #region Private

        static private GraphicsDevice sGraphicsDeviceInstance = null;

        private InitDeviceContext m_InitDeviceContext = null;
        private Stack<DLSSContext> s_ContextObjectPool = new Stack<DLSSContext>();

        private GraphicsDevice(String projectId, String engineVersion, String appDir)
        {
            m_InitDeviceContext = new InitDeviceContext(projectId, engineVersion, appDir);
        }

        private bool Initialize()
        {
            return NVUP_InitApi(m_InitDeviceContext.GetInitCmdPtr());
        }

        private void Shutdown()
        {
            NVUP_ShutdownApi();
        }

        ~GraphicsDevice()
        {
            Shutdown();
        }

        private void InsertEventCall(CommandBuffer cmd, PluginEvent pluginEvent, IntPtr ptr)
        {
            cmd.IssuePluginEventAndData(NVUP_GetRenderEventCallback(), (int)pluginEvent + NVUP_GetBaseEventId(), ptr);
        }

        private static GraphicsDevice InternalCreate(String appIdOrProjectId, String engineVersion, String appDir)
        {
            if (sGraphicsDeviceInstance != null)
            {
                sGraphicsDeviceInstance.Shutdown();   //destroy all internal memory.
                sGraphicsDeviceInstance.Initialize(); //Re-initialize device.
                return sGraphicsDeviceInstance;
            }

            var newGraphicsDevice = new GraphicsDevice(appIdOrProjectId, engineVersion, appDir);

            if (newGraphicsDevice.Initialize())
            {
                sGraphicsDeviceInstance = newGraphicsDevice;
                return newGraphicsDevice;
            }

            return null;
        }

        private static int CreateSetTextureUserData(int featureId, int textureSlot, bool clearTextureTable)
        {
            int featureIdMask   = (featureId & 0xffff); //16 bits
            int textureSlotMask = (textureSlot & 0x7fff); //15 bits;
            int clearTableMask  = clearTextureTable ? 0x1 : 0x0; //1 bit
            return (featureIdMask << 16) | (textureSlotMask << 1) | clearTableMask;
        }

        private void SetTexture(CommandBuffer cmd, DLSSContext dlssContext, DLSSCommandExecutionData.Textures textureSlot, Texture texture, bool clearTextureTable = false)
        {
            if (texture == null)
                return;

            uint userData = (uint)CreateSetTextureUserData((int)dlssContext.featureSlot, (int)textureSlot, clearTextureTable);
            cmd.IssuePluginCustomTextureUpdateV2(
                NVUP_GetSetTextureEventCallback(), texture, userData);
        }

        internal GraphicsDeviceDebugInfo GetDebugInfo(uint debugViewId)
        {
            var debugInfo = new GraphicsDeviceDebugInfo();
            NVUP_GetGraphicsDeviceDebugInfo(debugViewId, out debugInfo);
            return debugInfo;
        }

        internal uint CreateDebugViewId()
        {
            return NVUP_CreateDebugView();
        }

        internal void DeleteDebugViewId(uint debugViewId)
        {
            NVUP_DeleteDebugView(debugViewId);
        }

        #endregion

        // -----------------------------------------------------------------------------------
        // Public API to interact with NVIDIA Features
        // -----------------------------------------------------------------------------------
        #region PublicAPI

        /// <inheritdoc cref="CreateGraphicsDevice(String, String)" />
        public static GraphicsDevice CreateGraphicsDevice()
        {
            return GraphicsDevice.InternalCreate(GraphicsDevice.s_DefaultProjectID, Application.unityVersion, GraphicsDevice.s_DefaultAppDir);
        }

        /// <inheritdoc cref="CreateGraphicsDevice(String, String)" />
        public static GraphicsDevice CreateGraphicsDevice(String projectID)
        {
            return GraphicsDevice.InternalCreate(projectID, Application.unityVersion, GraphicsDevice.s_DefaultAppDir);
        }

        /// <summary>Creates the main API object. Call this method only once in your application.</summary>
        /// <remarks>
        /// This function will instantiate the <see cref="GraphicsDevice" />. It's safe to execute at
        /// any point when the application is alive. Since the device is a direct representation of the hardware
        /// graphics card, this method returns the same device if you call it again. If you pass in an appDir or
        /// projectID, only the first call to this method instantiates the device object using these parameters.
        /// </remarks>
        /// <returns>
        /// The Device API object to access NVIDIA features. If you call this function again, the function returns the
        /// same device, regardless of whether you pass in a different projectID.
        /// </returns>
        /// <param name="projectID">The projectID of the Unity project. Only the first call to this function uses this ID.</param>
        /// <param name="appDir">Specifies the directory in which the NVIDIA DLLS are located at. When not used, the system will locate the DLLs right next to the executable of the editor.</param>
        public static GraphicsDevice CreateGraphicsDevice(String projectID, String appDir)
        {
            return GraphicsDevice.InternalCreate(projectID, Application.unityVersion, appDir);
        }

        /// <summary>Gets the device created by <see cref="o:GraphicsDevice.CreateGraphicsDevice()" />.</summary>
        /// <remarks>If the device hasn't been created this property evaluates to null.</remarks>
        public static GraphicsDevice device { get { return sGraphicsDeviceInstance; } }

        ///<summary>Gets the version that corresponds to Unity's host plugin that manages the NVIDIA.NVUnityPlugin official library.</summary>
        public static uint version { get { return NVUP_GetDeviceVersion(); } }

        ///<summary>Checks if the current NVIDIA graphics card supports the feature you specify using the <see cref="GraphicsDeviceFeature" /> enum.</summary>
        ///<param name="featureID">The Feature enum value that represents the feature to check support for.</param>
        ///<returns>Returns true if the graphics card supports the feature. Otherwise, returns false.</returns>
        public bool IsFeatureAvailable(GraphicsDeviceFeature featureID)
        {
            return NVUP_IsFeatureAvailable(featureID);
        }

        ///<summary>Creates a specific NVIDIA feature.</summary>
        ///<remarks>
        ///  <c>initSettings</c> must belong to the feature you want to create. A usual use case is to call this
        /// function once per view you want to apply the feature too. For example, if your application uses two screens,
        /// call this function for each screen respectively. When the application no longer requires the feature (for
        /// example a view is destroyed), the application must call <see cref="GraphicsDevice.DestroyFeature" /> to
        /// recover the allocated memory.
        /// </remarks>
        /// <param name="cmd">The rendering command buffer to record commands into. This call does not execute the
        /// command buffer, you must execute the command buffer yourself at any time after this call.</param>
        /// <param name="initSettings">Initial settings structure for the specific feature.</param>
        /// <returns>Returns a Deep Learning Super Sampling (DLSS) context object.</returns>
        public DLSSContext CreateFeature(CommandBuffer cmd, in DLSSCommandInitializationData initSettings)
        {
            if (!IsFeatureAvailable(GraphicsDeviceFeature.DLSS))
                return null;

            DLSSContext dlssContext = null;
            if (s_ContextObjectPool.Count == 0)
            {
                dlssContext = new DLSSContext();
            }
            else
            {
                dlssContext = s_ContextObjectPool.Pop();
            }

            dlssContext.Init(initSettings, NVUP_CreateFeatureSlot());
            InsertEventCall(cmd, PluginEvent.DLSSInit, dlssContext.GetInitCmdPtr());
            return dlssContext;
        }

        ///<summary>Destroys a specific feature created with <see cref="GraphicsDevice.CreateFeature" />.</summary>
        ///<param name="cmd">The rendering command buffer to record commands into. This call does not execute the
        /// command buffer, you must execute the command buffer yourself at any time after this call.</param>
        ///<param name="dlssContext">The command object to destroy.</param>
        public void DestroyFeature(CommandBuffer cmd, DLSSContext dlssContext)
        {
            InsertEventCall(cmd, PluginEvent.DestroyFeature, new IntPtr(dlssContext.featureSlot));
            dlssContext.Reset();
            s_ContextObjectPool.Push(dlssContext);
        }

        /// <summary>Records the execution of DLSS into a rendering command buffer.</summary>
        /// <remarks>This call does not execute the command buffer, it only appends custom commands into it.</remarks>
        /// <param name="cmd">The rendering command buffer to record commands into. This call does not execute the
        /// command buffer, you must execute the command buffer yourself at any time after this call.</param>
        /// <param name="dlssContext">The source feature context to execute. You must set the parameters for this
        /// command in the <see cref="DLSSContext" /> object prior to this call.</param>
        /// <param name="textures">The texture table, where inputs / outputs are specified for DLSS to execute.</param>
        public void ExecuteDLSS(CommandBuffer cmd, DLSSContext dlssContext, in DLSSTextureTable textures)
        {
            // UUM-134012: Get stable pointer first - if pool exhausted, skip entire execution
            IntPtr execDataPtr = dlssContext.GetExecuteCmdPtr();
            if (execDataPtr == IntPtr.Zero)
                return;

            SetTexture(cmd, dlssContext, DLSSCommandExecutionData.Textures.ColorInput,       textures.colorInput, true);
            SetTexture(cmd, dlssContext, DLSSCommandExecutionData.Textures.ColorOutput,      textures.colorOutput);
            SetTexture(cmd, dlssContext, DLSSCommandExecutionData.Textures.Depth,            textures.depth);
            SetTexture(cmd, dlssContext, DLSSCommandExecutionData.Textures.MotionVectors,    textures.motionVectors);
            SetTexture(cmd, dlssContext, DLSSCommandExecutionData.Textures.TransparencyMask, textures.transparencyMask);
            SetTexture(cmd, dlssContext, DLSSCommandExecutionData.Textures.ExposureTexture,  textures.exposureTexture);
            SetTexture(cmd, dlssContext, DLSSCommandExecutionData.Textures.BiasColorMask,    textures.biasColorMask);
            InsertEventCall(cmd, PluginEvent.DLSSExecute, execDataPtr);
        }

        /// <summary>Returns a structure containing the optimal settings for a specific target resolution and quality.</summary>
        /// <param name="targetWidth">Target width in pixels.</param>
        /// <param name="targetHeight">Target height in pixels.</param>
        /// <param name="quality">Current quality mode.</param>
        /// <param name="optimalSettings">Output structure, which will be filled with the recommended optimal settings data.</param>
        /// <returns>True if the function has successfully populated optimalSettings. False if it failed.</returns>
        public bool GetOptimalSettings(uint targetWidth, uint targetHeight, DLSSQuality quality, out OptimalDLSSSettingsData optimalSettings)
        {
            return NVUP_GetOptimalSettings(targetWidth, targetHeight, quality, out optimalSettings);
        }

        /// <summary>Creates an object containing debug information of the device.</summary>
        /// <remarks>It is best practice to call this function once per application instantiation. If you call this function, you must call <see cref="GraphicsDevice.DeleteDebugView" /> during application destruction.</remarks>
        /// <returns>Returns an object of type <see cref="NVIDIA.GraphicsDeviceDebugView" />. This object contains a snapshot of the debug information of the device..</returns>
        /// <seealso cref="UpdateDebugView" />
        public GraphicsDeviceDebugView CreateDebugView()
        {
            return new GraphicsDeviceDebugView(CreateDebugViewId());
        }

        ///<summary>Updates a snapshot of the debug information for the view object passed.</summary>
        ///<param name="debugView">The object to update. You must create this using <see cref="GraphicsDevice.CreateDebugView" /> before you call this function.</param>
        public void UpdateDebugView(GraphicsDeviceDebugView debugView)
        {
            if (debugView == null)
                return;

            // Pin the managed array to get a stable pointer.
            var handle = GCHandle.Alloc(debugView.m_DlssDebugFeatures, GCHandleType.Pinned);
            try
            {
                // Prepare the struct to pass to C++.
                var debugInfoCmd = new GraphicsDeviceDebugInfo
                {
                    outDlssInfoBuffer = handle.AddrOfPinnedObject(),
                    outDlssInfoBufferCapacity = (uint)debugView.m_DlssDebugFeatures.Length
                };

                // Call the native function. C++ will fill our buffer and the struct fields.
                NVUP_GetGraphicsDeviceDebugInfo(debugView.m_ViewId, out debugInfoCmd);

                // Update the view with the results.
                debugView.m_DeviceVersion = debugInfoCmd.NVDeviceVersion;
                debugView.m_NgxVersion = debugInfoCmd.NGXVersion;
                debugView.m_DlssFeatureValidCount = debugInfoCmd.dlssInfoCount;
            }
            finally
            {
                // ALWAYS unpin the handle.
                handle.Free();
            }
        }

        ///<summary>Deletes a debug view created with <see cref="CreateDebugView" />.</summary>
        ///<param name="debugView">The debug view object to use. This is the object that <see cref="CreateDebugView" /> returns.</param>
        public void DeleteDebugView(GraphicsDeviceDebugView debugView)
        {
            if (debugView == null)
                return;

            DeleteDebugViewId(debugView.m_ViewId);
        }

        ///<summary>Gets the explanation for the given preset.</summary>
        ///<param name="preset">DLSS render preset.</param>
        ///<returns>Returns a string containing the explanation for the given DLSSPreset.</returns>
        public static string GetDLSSPresetExplanation(DLSSPreset preset)
        {
            IntPtr ptr = NVUP_GetDLSSPresetExplanation(preset);
            return Marshal.PtrToStringAnsi(ptr);
        }

        ///<summary>Gets a bit mask of available presets for the given DLSSQuality.</summary>
        ///<param name="perfQuality">DLSS quality mode.</param>
        ///<returns>Returns a bit mask of DLSSPreset enums.</returns>
        public static uint GetAvailableDLSSPresetsForQuality(DLSSQuality perfQuality)
        {
            return NVUP_GetAvailableDLSSPresetsForQuality(perfQuality);
        }

        #endregion

        // -----------------------------------------------------------------------------------
        // All required imports for the plugin
        // -----------------------------------------------------------------------------------

        #region Imports

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static bool NVUP_InitApi(IntPtr initData);

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static void NVUP_ShutdownApi();

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static bool NVUP_IsFeatureAvailable(GraphicsDeviceFeature featureID);

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static bool NVUP_GetOptimalSettings(uint inTargetWidth, uint inTargetHeight, DLSSQuality inPerfVQuality, out OptimalDLSSSettingsData data);

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr NVUP_GetRenderEventCallback();

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr NVUP_GetSetTextureEventCallback();

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern uint NVUP_CreateFeatureSlot();

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern uint NVUP_GetDeviceVersion();

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern uint NVUP_CreateDebugView();

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void NVUP_GetGraphicsDeviceDebugInfo(uint debugViewId, out GraphicsDeviceDebugInfo data);

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern void NVUP_DeleteDebugView(uint debugViewId);

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern int NVUP_GetBaseEventId();

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr NVUP_GetDLSSPresetExplanation(DLSSPreset preset);

        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern uint NVUP_GetAvailableDLSSPresetsForQuality(DLSSQuality perfQuality);

        // Copies execute data to stable buffer that won't be overwritten until GPU is done
        // Takes struct by value - P/Invoke marshals directly, avoiding intermediate copy
        [DllImport("NVUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr NVUP_PrepareExecuteData(DLSSCommandExecutionData srcData);

        #endregion
    };
} // namespace NVIDIA
