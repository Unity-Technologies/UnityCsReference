using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditor;


namespace UnityEngine.AMD
{
    // -----------------------------------------------------------------------------------
    //  Public Enums must match C++ enums found in AMDDevice.h
    // -----------------------------------------------------------------------------------
    #region GraphicsDeviceEnums
    internal enum PluginEvent
    {
        DestroyFeature = 0,
        FSR2Execute = 1,
        FSR2PostExecute = 2,
        FSR2Init = 3
    }
    #endregion

    // -----------------------------------------------------------------------------------
    //  Main AMD device. Use to interact with AMD specific features on a unity SRP
    // -----------------------------------------------------------------------------------
    public class GraphicsDevice
    {
        #region Private

        static private GraphicsDevice sGraphicsDeviceInstance = null;
        private Stack<FSR2Context> s_ContextObjectPool = new Stack<FSR2Context>();

        private GraphicsDevice()
        {
        }

        private bool Initialize()
        {
            return AMDUP_InitApi();
        }

        private void Shutdown()
        {
            AMDUP_ShutdownApi();
        }

        ~GraphicsDevice()
        {
            Shutdown();
        }

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
            int textureSlotMask   = (textureSlot & 0x7fff); //15 bits;
            int clearTableMask    = clearTextureTable ? 0x1 : 0x0; //1 bit
            return (featureIdMask << 16) | (textureSlotMask << 1) | clearTableMask;
        }

        private void SetTexture(CommandBuffer cmd, FSR2Context fsr2Context, FSR2CommandExecutionData.Textures textureSlot, Texture texture, bool clearTextureTable = false)
        {
            if (texture == null)
                return;

            uint userData = (uint)CreateSetTextureUserData((int)fsr2Context.featureSlot, (int)textureSlot, clearTextureTable);
            cmd.IssuePluginCustomTextureUpdateV2(
                AMDUP_GetSetTextureEventCallback(), texture, userData);
        }

        #endregion

        // -----------------------------------------------------------------------------------
        // Public API to interact with AMD Features
        // -----------------------------------------------------------------------------------
        #region PublicAPI

        public static GraphicsDevice CreateGraphicsDevice()
        {
            return GraphicsDevice.InternalCreate();
        }

        public static GraphicsDevice device { get { return sGraphicsDeviceInstance; } }

        public static uint version  { get { return AMDUP_GetDeviceVersion();} }

        public FSR2Context CreateFeature(CommandBuffer cmd, in FSR2CommandInitializationData initSettings)
        {
            FSR2Context fsrContext = null;
            if (s_ContextObjectPool.Count == 0)
            {
                fsrContext = new FSR2Context();
            }
            else
            {
                fsrContext = s_ContextObjectPool.Pop();
            }

            fsrContext.Init(initSettings, AMDUP_CreateFeatureSlot());
            InsertEventCall(cmd, PluginEvent.FSR2Init, fsrContext.GetInitCmdPtr());
            return fsrContext;
        }

        public bool GetRenderResolutionFromQualityMode(FSR2Quality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight)
        {
            return AMDUP_GetRenderResolutionFromQualityMode(qualityMode, displayWidth, displayHeight, out renderWidth, out renderHeight);
        }

        public float GetUpscaleRatioFromQualityMode(FSR2Quality qualityMode)
        {
            return AMDUP_GetUpscaleRatioFromQualityMode(qualityMode);
        }

        public void DestroyFeature(CommandBuffer cmd, FSR2Context fsrContext)
        {
            InsertEventCall(cmd, PluginEvent.DestroyFeature, new IntPtr(fsrContext.featureSlot));
            fsrContext.Reset();
            s_ContextObjectPool.Push(fsrContext);
        }

        public void ExecuteFSR2(CommandBuffer cmd, FSR2Context fsr2Context, in FSR2TextureTable textures)
        {
            SetTexture(cmd, fsr2Context, FSR2CommandExecutionData.Textures.ColorInput,       textures.colorInput, true);
            SetTexture(cmd, fsr2Context, FSR2CommandExecutionData.Textures.ColorOutput,      textures.colorOutput);
            SetTexture(cmd, fsr2Context, FSR2CommandExecutionData.Textures.Depth,            textures.depth);
            SetTexture(cmd, fsr2Context, FSR2CommandExecutionData.Textures.MotionVectors,    textures.motionVectors);
            SetTexture(cmd, fsr2Context, FSR2CommandExecutionData.Textures.TransparencyMask, textures.transparencyMask);
            SetTexture(cmd, fsr2Context, FSR2CommandExecutionData.Textures.ExposureTexture,  textures.exposureTexture);
            SetTexture(cmd, fsr2Context, FSR2CommandExecutionData.Textures.BiasColorMask,    textures.biasColorMask);
            InsertEventCall(cmd, PluginEvent.FSR2Execute, fsr2Context.GetExecuteCmdPtr());

            // D3D12 requires to pump submission into its own thread.
            // this is caused by the current implementation of the plugin. 
            // this function is probably noop in other graphics APIs
            InsertEventCall(cmd, PluginEvent.FSR2PostExecute, fsr2Context.GetExecuteCmdPtr());
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
        private static extern IntPtr AMDUP_GetRenderEventCallback();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr AMDUP_GetSetTextureEventCallback();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern uint AMDUP_CreateFeatureSlot();

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool AMDUP_GetRenderResolutionFromQualityMode(FSR2Quality qualityMode, uint displayWidth, uint displayHeight, out uint renderWidth, out uint renderHeight);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern float AMDUP_GetUpscaleRatioFromQualityMode(FSR2Quality qualityMode);

        [DllImport("AMDUnityPlugin", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern int AMDUP_GetBaseEventId();

        #endregion
    };
} // namespace AMD
