// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.XR.WSA.WebCam
{
    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    public enum CapturePixelFormat
    {
        BGRA32 = 0,
        NV12 = 1,
        JPEG = 2,
        PNG = 3
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    public enum WebCamMode
    {
        None = 0,
        PhotoMode = 1,
        VideoMode = 2
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    [StaticAccessor("WebCam::GetInstance()", StaticAccessorType.Dot)]
    [NativeHeader("Runtime/VR/HoloLens/WebCam/WebCam.h")]
    public class WebCam
    {
        public static WebCamMode Mode { get { return WebCamMode.None; } }
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/VR/HoloLens/WebCam/CameraParameters.h")]
    public struct CameraParameters
    {
        private float m_HologramOpacity;
        private float m_FrameRate;
        private int m_CameraResolutionWidth;
        private int m_CameraResolutionHeight;
        private CapturePixelFormat m_PixelFormat;

        public float hologramOpacity { get { return m_HologramOpacity; } set { m_HologramOpacity = value; } }
        public float frameRate { get { return m_FrameRate; } set { m_FrameRate = value; } }
        public int cameraResolutionWidth { get { return m_CameraResolutionWidth; } set { m_CameraResolutionWidth = value; } }
        public int cameraResolutionHeight { get { return m_CameraResolutionHeight; } set { m_CameraResolutionHeight = value; } }
        public CapturePixelFormat pixelFormat { get { return m_PixelFormat; } set { m_PixelFormat = value; } }

        //-------------------------------------------------------------------------------------------------------
        public CameraParameters(WebCamMode webCamMode)
        {
            m_HologramOpacity = 1.0f;
            m_PixelFormat = CapturePixelFormat.BGRA32;
            m_FrameRate = default(float);
            m_CameraResolutionWidth = default(int);
            m_CameraResolutionHeight = default(int);

            if (webCamMode == WebCamMode.PhotoMode)
            {
                Resolution photoCaptureCameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

                m_CameraResolutionWidth = photoCaptureCameraResolution.width;
                m_CameraResolutionHeight = photoCaptureCameraResolution.height;
            }
            else if (webCamMode == WebCamMode.VideoMode)
            {
                Resolution videoCaptureCameraResolution = VideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
                float cameraFramerate = VideoCapture.GetSupportedFrameRatesForResolution(videoCaptureCameraResolution).OrderByDescending((fps) => fps).First();

                m_CameraResolutionWidth = videoCaptureCameraResolution.width;
                m_CameraResolutionHeight = videoCaptureCameraResolution.height;
                m_FrameRate = cameraFramerate;
            }
        }
    }
}

