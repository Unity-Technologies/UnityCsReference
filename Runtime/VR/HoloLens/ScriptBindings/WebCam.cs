// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    public enum PhotoCaptureFileOutputFormat
    {
        PNG = 0,
        JPG = 1
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    public enum WebCamMode
    {
        None = 0,
        PhotoMode = 1,
        VideoMode = 2
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    public partial class WebCam
    {
        public static WebCamMode Mode
        {
            get
            {
                return (WebCamMode)WebCam.GetWebCamModeState_Internal();
            }
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
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

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    public partial class VideoCapture : IDisposable
    {
        static readonly long HR_SUCCESS = 0x00000000;

        public enum CaptureResultType
        {
            Success = 0,
            UnknownError = 1,
        }

        public enum AudioState
        {
            MicAudio = 0,
            ApplicationAudio = 1,
            ApplicationAndMicAudio = 2,
            None = 3,
        }

        public struct VideoCaptureResult
        {
            public CaptureResultType resultType;
            public long hResult;

            public bool success
            {
                get
                {
                    return resultType == CaptureResultType.Success;
                }
            }
        }

        static private VideoCaptureResult MakeCaptureResult(CaptureResultType resultType, long hResult)
        {
            VideoCaptureResult result = new VideoCaptureResult();
            result.resultType = resultType;
            result.hResult = hResult;

            return result;
        }

        static private VideoCaptureResult MakeCaptureResult(long hResult)
        {
            VideoCaptureResult result = new VideoCaptureResult();

            CaptureResultType resultType;
            if (hResult == HR_SUCCESS)
            {
                resultType = CaptureResultType.Success;
            }
            else
            {
                resultType = CaptureResultType.UnknownError;
            }

            result.resultType = resultType;
            result.hResult = hResult;

            return result;
        }

        private static Resolution[] s_SupportedResolutions;
        private IntPtr m_NativePtr;

        public delegate void OnVideoCaptureResourceCreatedCallback(VideoCapture captureObject);
        public delegate void OnVideoModeStartedCallback(VideoCaptureResult result);
        public delegate void OnVideoModeStoppedCallback(VideoCaptureResult result);
        public delegate void OnStartedRecordingVideoCallback(VideoCaptureResult result);
        public delegate void OnStoppedRecordingVideoCallback(VideoCaptureResult result);

        //-----------------------------------------------------------------
        public static IEnumerable<Resolution> SupportedResolutions
        {
            get
            {
                if (s_SupportedResolutions == null)
                {
                    s_SupportedResolutions = new Resolution[0];
                }

                return s_SupportedResolutions;
            }
        }

        //-----------------------------------------------------------------
        public static IEnumerable<float> GetSupportedFrameRatesForResolution(Resolution resolution)
        {
            float[] supportedFrameRates = null;
            supportedFrameRates = new float[0];

            return supportedFrameRates;
        }

        //-----------------------------------------------------------------
        public bool IsRecording
        {
            get
            {
                return false;
            }
        }

        //-----------------------------------------------------------------
        public static void CreateAsync(bool showHolograms, OnVideoCaptureResourceCreatedCallback onCreatedCallback)
        {
            if (onCreatedCallback == null)
            {
                throw new ArgumentNullException("onCreatedCallback");
            }

            Instantiate_Internal(showHolograms, onCreatedCallback);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnCreatedVideoCaptureResourceDelegate(OnVideoCaptureResourceCreatedCallback callback, IntPtr nativePtr)
        {
            if (nativePtr == IntPtr.Zero)
            {
                callback(null);
            }
            else
            {
                callback(new VideoCapture(nativePtr));
            }
        }

        //-----------------------------------------------------------------
        private VideoCapture(IntPtr nativeCaptureObject)
        {
            m_NativePtr = nativeCaptureObject;
        }

        //-----------------------------------------------------------------
        public void StartVideoModeAsync(CameraParameters setupParams,
            AudioState audioState,
            OnVideoModeStartedCallback onVideoModeStartedCallback)
        {
            if (m_NativePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("You must create a Video Capture Object before starting its video mode.");
            }

            if (onVideoModeStartedCallback == null)
            {
                throw new ArgumentNullException("onVideoModeStartedCallback");
            }

            if (setupParams.cameraResolutionWidth == default(int) ||  setupParams.cameraResolutionHeight == default(int))
            {
                throw new ArgumentOutOfRangeException("setupParams", "The camera resolution must be set to a supported resolution.");
            }

            if (setupParams.frameRate == default(float))
            {
                throw new ArgumentOutOfRangeException("setupParams", "The camera frame rate must be set to a supported recording frame rate.");
            }

            StartVideoMode_Internal(m_NativePtr,
                (int)audioState,
                onVideoModeStartedCallback,
                setupParams.hologramOpacity,
                setupParams.frameRate,
                setupParams.cameraResolutionWidth,
                setupParams.cameraResolutionHeight,
                (int)setupParams.pixelFormat);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnVideoModeStartedDelegate(OnVideoModeStartedCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void StopVideoModeAsync(OnVideoModeStoppedCallback onVideoModeStoppedCallback)
        {
            if (m_NativePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("You must create a Video Capture Object before stopping its video mode.");
            }

            if (onVideoModeStoppedCallback == null)
            {
                throw new ArgumentNullException("onVideoModeStoppedCallback");
            }

            StopVideoMode_Internal(m_NativePtr,
                onVideoModeStoppedCallback);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnVideoModeStoppedDelegate(OnVideoModeStoppedCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void StartRecordingAsync(string filename, OnStartedRecordingVideoCallback onStartedRecordingVideoCallback)
        {
            if (m_NativePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("You must create a Video Capture Object before recording video.");
            }

            if (onStartedRecordingVideoCallback == null)
            {
                throw new ArgumentNullException("onStartedRecordingVideoCallback");
            }

            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }

            // Make sure we don't have any forward slashes.
            // WinRT Apis do not like forward slashes.
            filename = filename.Replace("/", @"\");

            string directory = System.IO.Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                throw new ArgumentException("The specified directory does not exist.", "filename");
            }

            System.IO.FileInfo fileInfo = new System.IO.FileInfo(filename);
            if (fileInfo.Exists && fileInfo.IsReadOnly)
            {
                throw new ArgumentException("Cannot write to the file because it is read-only.", "filename");
            }

            StartRecordingVideoToDisk_Internal(m_NativePtr, filename, onStartedRecordingVideoCallback);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnStartedRecordingVideoToDiskDelegate(OnStartedRecordingVideoCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void StopRecordingAsync(OnStoppedRecordingVideoCallback onStoppedRecordingVideoCallback)
        {
            if (m_NativePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("You must create a Video Capture Object before recording video.");
            }

            if (onStoppedRecordingVideoCallback == null)
            {
                throw new ArgumentNullException("onStoppedRecordingVideoCallback");
            }

            StopRecordingVideoToDisk_Internal(m_NativePtr, onStoppedRecordingVideoCallback);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnStoppedRecordingVideoToDiskDelegate(OnStoppedRecordingVideoCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public IntPtr GetUnsafePointerToVideoDeviceController()
        {
            return IntPtr.Zero;
        }

        //-----------------------------------------------------------------
        public void Dispose()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                Dispose_Internal(m_NativePtr);
                m_NativePtr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        //-----------------------------------------------------------------
        ~VideoCapture()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                DisposeThreaded_Internal(m_NativePtr);
                m_NativePtr = IntPtr.Zero;
            }
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    public partial class PhotoCapture : IDisposable
    {
        static readonly long HR_SUCCESS = 0x00000000;

        public enum CaptureResultType
        {
            Success = 0,
            UnknownError = 1,
        }

        public struct PhotoCaptureResult
        {
            public CaptureResultType resultType;
            public long hResult;

            public bool success
            {
                get
                {
                    return resultType == CaptureResultType.Success;
                }
            }
        }

        static private PhotoCaptureResult MakeCaptureResult(CaptureResultType resultType, long hResult)
        {
            PhotoCaptureResult result = new PhotoCaptureResult();
            result.resultType = resultType;
            result.hResult = hResult;

            return result;
        }

        static private PhotoCaptureResult MakeCaptureResult(long hResult)
        {
            PhotoCaptureResult result = new PhotoCaptureResult();

            CaptureResultType resultType;
            if (hResult == HR_SUCCESS)
            {
                resultType = CaptureResultType.Success;
            }
            else
            {
                resultType = CaptureResultType.UnknownError;
            }

            result.resultType = resultType;
            result.hResult = hResult;

            return result;
        }

        private static Resolution[] s_SupportedResolutions;
        private IntPtr m_NativePtr;

        public delegate void OnCaptureResourceCreatedCallback(PhotoCapture captureObject);
        public delegate void OnPhotoModeStartedCallback(PhotoCaptureResult result);
        public delegate void OnPhotoModeStoppedCallback(PhotoCaptureResult result);
        public delegate void OnCapturedToDiskCallback(PhotoCaptureResult result);
        public delegate void OnCapturedToMemoryCallback(PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame);

        //-----------------------------------------------------------------
        public static IEnumerable<Resolution> SupportedResolutions
        {
            get
            {
                if (s_SupportedResolutions == null)
                {
                    s_SupportedResolutions = new Resolution[0];
                }

                return s_SupportedResolutions;
            }
        }

        //-----------------------------------------------------------------
        public static void CreateAsync(bool showHolograms, OnCaptureResourceCreatedCallback onCreatedCallback)
        {
            if (onCreatedCallback == null)
            {
                throw new ArgumentNullException("onCreatedCallback");
            }

            Instantiate_Internal(showHolograms, onCreatedCallback);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnCreatedResourceDelegate(OnCaptureResourceCreatedCallback callback, IntPtr nativePtr)
        {
            if (nativePtr == IntPtr.Zero)
            {
                callback(null);
            }
            else
            {
                callback(new PhotoCapture(nativePtr));
            }
        }

        //-----------------------------------------------------------------
        private PhotoCapture(IntPtr nativeCaptureObject)
        {
            m_NativePtr = nativeCaptureObject;
        }

        //-----------------------------------------------------------------
        public void StartPhotoModeAsync(CameraParameters setupParams,
            OnPhotoModeStartedCallback onPhotoModeStartedCallback)
        {
            if (m_NativePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("You must create a Photo Capture Object before starting its photo mode.");
            }

            if (onPhotoModeStartedCallback == null)
            {
                throw new ArgumentException("onPhotoModeStartedCallback");
            }

            if (setupParams.cameraResolutionWidth == default(int) || setupParams.cameraResolutionHeight == default(int))
            {
                throw new ArgumentOutOfRangeException("setupParams", "The camera resolution must be set to a supported resolution.");
            }

            StartPhotoMode_Internal(m_NativePtr,
                onPhotoModeStartedCallback,
                setupParams.hologramOpacity,
                setupParams.frameRate,
                setupParams.cameraResolutionWidth,
                setupParams.cameraResolutionHeight,
                (int)setupParams.pixelFormat);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnPhotoModeStartedDelegate(OnPhotoModeStartedCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void StopPhotoModeAsync(OnPhotoModeStoppedCallback onPhotoModeStoppedCallback)
        {
            if (m_NativePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("You must create a Photo Capture Object before stopping its photo mode.");
            }

            if (onPhotoModeStoppedCallback == null)
            {
                throw new ArgumentException("onPhotoModeStoppedCallback");
            }

            StopPhotoMode_Internal(m_NativePtr,
                onPhotoModeStoppedCallback);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnPhotoModeStoppedDelegate(OnPhotoModeStoppedCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void TakePhotoAsync(string filename, PhotoCaptureFileOutputFormat fileOutputFormat, OnCapturedToDiskCallback onCapturedPhotoToDiskCallback)
        {
            if (m_NativePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("You must create a Photo Capture Object before taking a photo.");
            }

            if (onCapturedPhotoToDiskCallback == null)
            {
                throw new ArgumentNullException("onCapturedPhotoToDiskCallback");
            }

            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }

            // Make sure we don't have any forward slashes.
            // WinRT Apis do not like forward slashes.
            filename = filename.Replace("/", @"\");

            string directory = System.IO.Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                throw new ArgumentException("The specified directory does not exist.", "filename");
            }

            System.IO.FileInfo fileInfo = new System.IO.FileInfo(filename);
            if (fileInfo.Exists && fileInfo.IsReadOnly)
            {
                throw new ArgumentException("Cannot write to the file because it is read-only.", "filename");
            }

            CapturePhotoToDisk_Internal(m_NativePtr, filename, (int)fileOutputFormat, onCapturedPhotoToDiskCallback);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnCapturedPhotoToDiskDelegate(OnCapturedToDiskCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void TakePhotoAsync(OnCapturedToMemoryCallback onCapturedPhotoToMemoryCallback)
        {
            if (m_NativePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("You must create a Photo Capture Object before taking a photo.");
            }

            if (onCapturedPhotoToMemoryCallback == null)
            {
                throw new ArgumentNullException("onCapturedPhotoToMemoryCallback");
            }

            CapturePhotoToMemory_Internal(m_NativePtr,
                onCapturedPhotoToMemoryCallback);
        }

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnCapturedPhotoToMemoryDelegate(OnCapturedToMemoryCallback callback, long hResult, IntPtr photoCaptureFramePtr)
        {
            PhotoCaptureFrame photoCaptureFrame = null;

            if (photoCaptureFramePtr != IntPtr.Zero)
            {
                photoCaptureFrame = new PhotoCaptureFrame(photoCaptureFramePtr);
            }

            callback(MakeCaptureResult(hResult), photoCaptureFrame);
        }

        //-----------------------------------------------------------------
        public IntPtr GetUnsafePointerToVideoDeviceController()
        {
            return IntPtr.Zero;
        }

        //-----------------------------------------------------------------
        public void Dispose()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                Dispose_Internal(m_NativePtr);
                m_NativePtr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        //-----------------------------------------------------------------
        ~PhotoCapture()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                DisposeThreaded_Internal(m_NativePtr);
                m_NativePtr = IntPtr.Zero;
            }
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    public sealed partial class PhotoCaptureFrame : IDisposable
    {
        #pragma warning disable 169
        private IntPtr m_NativePtr;

        public int dataLength
        {
            get;
            private set;
        }

        public bool hasLocationData
        {
            get;
            private set;
        }


        public CapturePixelFormat pixelFormat
        {
            get;
            private set;
        }

        //-----------------------------------------------------------------
        public bool TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix)
        {
            cameraToWorldMatrix = Matrix4x4.identity;
            return false;
        }

        //-----------------------------------------------------------------
        public bool TryGetProjectionMatrix(out Matrix4x4 projectionMatrix)
        {
            projectionMatrix = Matrix4x4.identity;
            return false;
        }

        //-----------------------------------------------------------------
        public bool TryGetProjectionMatrix(float nearClipPlane, float farClipPlane, out Matrix4x4 projectionMatrix)
        {
            projectionMatrix = Matrix4x4.identity;
            return false;
        }

        //-----------------------------------------------------------------
        public void UploadImageDataToTexture(Texture2D targetTexture)
        {
        }

        //-----------------------------------------------------------------
        public IntPtr GetUnsafePointerToBuffer()
        {
            return IntPtr.Zero;
        }

        //-----------------------------------------------------------------
        // The raw image data will be applied to the byteBuffer list
        // provided by the user.
        public void CopyRawImageDataIntoBuffer(List<byte> byteBuffer)
        {
        }

        //-----------------------------------------------------------------
        internal PhotoCaptureFrame(IntPtr nativePtr)
        {
        }

        //-----------------------------------------------------------------
        private void Cleanup()
        {
        }

        //-----------------------------------------------------------------
        public void Dispose()
        {
            Cleanup();

            GC.SuppressFinalize(this);
        }

        //-----------------------------------------------------------------
        ~PhotoCaptureFrame()
        {
            Cleanup();
        }
    }
}
