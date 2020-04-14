// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.Windows.WebCam
{
    [MovedFrom("UnityEngine.XR.WSA.WebCam")]
    [StaticAccessor("VideoCaptureBindings", StaticAccessorType.DoubleColon)]
    [NativeHeader("PlatformDependent/Win/Webcam/VideoCaptureBindings.h")]
    [StructLayout(LayoutKind.Sequential)]   // needed for IntPtr binding classes
    public class VideoCapture : IDisposable
    {
        internal IntPtr m_NativePtr;

        private static Resolution[] s_SupportedResolutions;
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
                    s_SupportedResolutions = GetSupportedResolutions_Internal();
                }

                return s_SupportedResolutions;
            }
        }

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeName("GetSupportedResolutions")]
        private extern static Resolution[] GetSupportedResolutions_Internal();


        //-----------------------------------------------------------------
        public static IEnumerable<float> GetSupportedFrameRatesForResolution(Resolution resolution)
        {
            float[] supportedFrameRates = null;
            supportedFrameRates = GetSupportedFrameRatesForResolution_Internal(resolution.width, resolution.height);

            return supportedFrameRates;
        }

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeName("GetSupportedFrameRatesForResolution")]
        private extern static float[] GetSupportedFrameRatesForResolution_Internal(int resolutionWidth, int resolutionHeight);

        //-----------------------------------------------------------------
        public extern bool IsRecording
        {
            [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
            [NativeMethod("VideoCaptureBindings::IsRecording", HasExplicitThis = true)]
            get;
        }

        //-----------------------------------------------------------------
        public static void CreateAsync(bool showHolograms, OnVideoCaptureResourceCreatedCallback onCreatedCallback)
        {
            if (onCreatedCallback == null)
            {
                throw new ArgumentNullException("onCreatedCallback");
            }

            // NOTE: This option no longer works in XR SDK and (as specified in the Documentation) the parameter is ignored.
            showHolograms = false;

            Instantiate_Internal(showHolograms, onCreatedCallback);
        }

        public static void CreateAsync(OnVideoCaptureResourceCreatedCallback onCreatedCallback)
        {
            if (onCreatedCallback == null)
            {
                throw new ArgumentNullException("onCreatedCallback");
            }

            Instantiate_Internal(false, onCreatedCallback);
        }

        [NativeName("Instantiate")]
        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        private extern static void Instantiate_Internal(bool showHolograms, OnVideoCaptureResourceCreatedCallback onCreatedCallback);

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

            StartVideoMode_Internal(setupParams, audioState, onVideoModeStartedCallback);
        }

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeMethod("VideoCaptureBindings::StartVideoMode", HasExplicitThis = true)]
        private extern void StartVideoMode_Internal(CameraParameters cameraParameters, AudioState audioState, OnVideoModeStartedCallback onVideoModeStartedCallback);

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnVideoModeStartedDelegate(OnVideoModeStartedCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeMethod("VideoCaptureBindings::StopVideoMode", HasExplicitThis = true)]
        public extern void StopVideoModeAsync([NotNull] OnVideoModeStoppedCallback onVideoModeStoppedCallback);

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnVideoModeStoppedDelegate(OnVideoModeStoppedCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void StartRecordingAsync(string filename, OnStartedRecordingVideoCallback onStartedRecordingVideoCallback)
        {
            if (onStartedRecordingVideoCallback == null)
            {
                throw new ArgumentNullException("onStartedRecordingVideoCallback");
            }

            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }

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

            // WinRT requires the full file path; so pass in the FullName in case filename isn't an absolute path
            // This also takes care of switching '/' separators to '\'
            StartRecordingVideoToDisk_Internal(fileInfo.FullName, onStartedRecordingVideoCallback);
        }

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeMethod("VideoCaptureBindings::StartRecordingVideoToDisk", HasExplicitThis = true)]
        private extern void StartRecordingVideoToDisk_Internal(string filename, OnStartedRecordingVideoCallback onStartedRecordingVideoCallback);

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnStartedRecordingVideoToDiskDelegate(OnStartedRecordingVideoCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeMethod("VideoCaptureBindings::StopRecordingVideoToDisk", HasExplicitThis = true)]
        public extern void StopRecordingAsync([NotNull] OnStoppedRecordingVideoCallback onStoppedRecordingVideoCallback);

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnStoppedRecordingVideoToDiskDelegate(OnStoppedRecordingVideoCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        [ThreadAndSerializationSafe]
        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeMethod("VideoCaptureBindings::GetUnsafePointerToVideoDeviceController", HasExplicitThis = true)]
        public extern IntPtr GetUnsafePointerToVideoDeviceController();

        //-----------------------------------------------------------------
        public void Dispose()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                Dispose_Internal();
                m_NativePtr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeMethod("VideoCaptureBindings::Dispose", HasExplicitThis = true)]
        private extern void Dispose_Internal();

        //-----------------------------------------------------------------
        ~VideoCapture()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                DisposeThreaded_Internal();
                m_NativePtr = IntPtr.Zero;
            }
        }

        [ThreadAndSerializationSafe]
        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_XBOXONE")]
        [NativeMethod("VideoCaptureBindings::DisposeThreaded", HasExplicitThis = true)]
        private extern void DisposeThreaded_Internal();
    }
}

