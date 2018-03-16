// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine.Experimental.XR
{
    public struct FrameReceivedEventArgs
    {
        internal XRCameraSubsystem m_CameraSubsystem;
        public XRCameraSubsystem CameraSubsystem { get { return m_CameraSubsystem; } }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Runtime/Graphics/Texture2D.h")]
    [NativeType(Header = "Modules/XR/Subsystems/Camera/XRCameraSubsystem.h")]
    [UsedByNativeCode]
    [NativeConditional("ENABLE_XR")]
    public class XRCameraSubsystem : Subsystem<XRCameraSubsystemDescriptor>
    {
        public extern int LastUpdatedFrame { get; }

        public extern bool LightEstimationRequested { get; set; }

        public extern Material Material { get; set; }

        public extern Camera Camera { get; set; }

        public extern bool TryGetAverageBrightness(ref float averageBrightness);

        public extern bool TryGetAverageColorTemperature(ref float averageColorTemperature);

        public extern bool TryGetProjectionMatrix(ref Matrix4x4 projectionMatrix);

        public extern bool TryGetDisplayMatrix(ref Matrix4x4 displayMatrix);

        public extern bool TryGetTimestamp(ref Int64 timestampNs);

        public bool TryGetShaderName(ref string shaderName)
        {
            return Internal_TryGetShaderName(ref shaderName);
        }

        private extern bool Internal_TryGetShaderName(ref string shaderName);

        public void GetTextures(List<Texture2D> texturesOut)
        {
            if (texturesOut == null)
                throw new ArgumentNullException("texturesOut");

            GetTexturesAsList(texturesOut);
        }

        private extern void GetTexturesAsList(List<Texture2D> textures);

        private extern Texture2D[] GetTexturesAsFixedArray();

        public event Action<FrameReceivedEventArgs> FrameReceived;

        [RequiredByNativeCode]
        private void InvokeFrameReceivedEvent()
        {
            if (FrameReceived != null)
            {
                FrameReceived(new FrameReceivedEventArgs()
                {
                    m_CameraSubsystem = this
                });
            }
        }
    }
}
