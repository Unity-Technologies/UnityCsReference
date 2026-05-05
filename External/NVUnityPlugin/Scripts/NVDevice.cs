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

    public enum GraphicsDeviceFeature
    {
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

    // -----------------------------------------------------------------------------------
    //  Main NVIDIA device. Use to interact with NVIDIA specific features on a unity SRP
    // -----------------------------------------------------------------------------------
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

        public static GraphicsDevice CreateGraphicsDevice()
        {
            return GraphicsDevice.InternalCreate(GraphicsDevice.s_DefaultProjectID, Application.unityVersion, GraphicsDevice.s_DefaultAppDir);
        }

        public static GraphicsDevice CreateGraphicsDevice(String projectID)
        {
            return GraphicsDevice.InternalCreate(projectID, Application.unityVersion, GraphicsDevice.s_DefaultAppDir);
        }

        public static GraphicsDevice CreateGraphicsDevice(String projectID, String appDir)
        {
            return GraphicsDevice.InternalCreate(projectID, Application.unityVersion, appDir);
        }

        public static GraphicsDevice device { get { return sGraphicsDeviceInstance; } }

        public static uint version { get { return NVUP_GetDeviceVersion(); } }

        public bool IsFeatureAvailable(GraphicsDeviceFeature featureID)
        {
            return NVUP_IsFeatureAvailable(featureID);
        }

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

        public void DestroyFeature(CommandBuffer cmd, DLSSContext dlssContext)
        {
            InsertEventCall(cmd, PluginEvent.DestroyFeature, new IntPtr(dlssContext.featureSlot));
            dlssContext.Reset();
            s_ContextObjectPool.Push(dlssContext);
        }

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

        public bool GetOptimalSettings(uint targetWidth, uint targetHeight, DLSSQuality quality, out OptimalDLSSSettingsData optimalSettings)
        {
            return NVUP_GetOptimalSettings(targetWidth, targetHeight, quality, out optimalSettings);
        }

        public GraphicsDeviceDebugView CreateDebugView()
        {
            return new GraphicsDeviceDebugView(CreateDebugViewId());
        }

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

        public void DeleteDebugView(GraphicsDeviceDebugView debugView)
        {
            if (debugView == null)
                return;

            DeleteDebugViewId(debugView.m_ViewId);
        }

        public static string GetDLSSPresetExplanation(DLSSPreset preset)
        {
            IntPtr ptr = NVUP_GetDLSSPresetExplanation(preset);
            return Marshal.PtrToStringAnsi(ptr);
        }

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
