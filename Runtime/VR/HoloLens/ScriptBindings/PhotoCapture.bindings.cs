// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.XR.WSA.WebCam
{
    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    public enum PhotoCaptureFileOutputFormat
    {
        PNG = 0,
        JPG = 1
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    [StaticAccessor("PhotoCapture", StaticAccessorType.DoubleColon)]
    [NativeHeader("Runtime/VR/HoloLens/WebCam/PhotoCapture.h")]
    [NativeHeader("VRScriptingClasses.h")]
    [StructLayout(LayoutKind.Sequential)]   // needed for IntPtr binding classes
    public class PhotoCapture : IDisposable
    {
        internal IntPtr m_NativePtr;

        private static Resolution[] s_SupportedResolutions;
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

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("GetSupportedResolutions")]
        private extern static Resolution[] GetSupportedResolutions_Internal();

        //-----------------------------------------------------------------
        public static void CreateAsync(bool showHolograms, OnCaptureResourceCreatedCallback onCreatedCallback)
        {
            if (onCreatedCallback == null)
            {
                throw new ArgumentNullException("onCreatedCallback");
            }

            Instantiate_Internal(showHolograms, onCreatedCallback);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("Instantiate")]
        private extern static IntPtr Instantiate_Internal(bool showHolograms, OnCaptureResourceCreatedCallback onCreatedCallback);

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
            if (onPhotoModeStartedCallback == null)
            {
                throw new ArgumentException("onPhotoModeStartedCallback");
            }

            if (setupParams.cameraResolutionWidth == default(int) || setupParams.cameraResolutionHeight == default(int))
            {
                throw new ArgumentOutOfRangeException("setupParams", "The camera resolution must be set to a supported resolution.");
            }

            StartPhotoMode_Internal(setupParams, onPhotoModeStartedCallback);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("StartPhotoMode")]
        private extern void StartPhotoMode_Internal(CameraParameters setupParams, OnPhotoModeStartedCallback onPhotoModeStartedCallback);

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnPhotoModeStartedDelegate(OnPhotoModeStartedCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("StopPhotoMode")]
        public extern void StopPhotoModeAsync(OnPhotoModeStoppedCallback onPhotoModeStoppedCallback);

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnPhotoModeStoppedDelegate(OnPhotoModeStoppedCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void TakePhotoAsync(string filename, PhotoCaptureFileOutputFormat fileOutputFormat, OnCapturedToDiskCallback onCapturedPhotoToDiskCallback)
        {
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

            CapturePhotoToDisk_Internal(filename, fileOutputFormat, onCapturedPhotoToDiskCallback);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("CapturePhotoToDisk")]
        private extern void CapturePhotoToDisk_Internal(string filename, PhotoCaptureFileOutputFormat fileOutputFormat, OnCapturedToDiskCallback onCapturedPhotoToDiskCallback);

        //-----------------------------------------------------------------
        [RequiredByNativeCode]
        private static void InvokeOnCapturedPhotoToDiskDelegate(OnCapturedToDiskCallback callback, long hResult)
        {
            callback(MakeCaptureResult(hResult));
        }

        //-----------------------------------------------------------------
        public void TakePhotoAsync(OnCapturedToMemoryCallback onCapturedPhotoToMemoryCallback)
        {
            if (onCapturedPhotoToMemoryCallback == null)
            {
                throw new ArgumentNullException("onCapturedPhotoToMemoryCallback");
            }

            CapturePhotoToMemory_Internal(onCapturedPhotoToMemoryCallback);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("CapturePhotoToMemory")]
        private extern void CapturePhotoToMemory_Internal(OnCapturedToMemoryCallback onCapturedPhotoToMemoryCallback);

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
        [ThreadAndSerializationSafe]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("GetUnsafePointerToVideoDeviceController")]
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

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("Dispose")]
        private extern void Dispose_Internal();

        //-----------------------------------------------------------------
        ~PhotoCapture()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                DisposeThreaded_Internal();
                m_NativePtr = IntPtr.Zero;
            }
        }

        [ThreadAndSerializationSafe]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("DisposeThreaded")]
        private extern void DisposeThreaded_Internal();
    }

    [MovedFrom("UnityEngine.VR.WSA.WebCam")]
    [NativeHeader("Runtime/VR/HoloLens/WebCam/PhotoCaptureFrame.h")]
    public sealed class PhotoCaptureFrame : IDisposable
    {
#pragma warning disable 169
        internal IntPtr m_NativePtr;

        public int dataLength { get; private set; }

        public bool hasLocationData { get; private set; }

        public CapturePixelFormat pixelFormat { get; private set; }


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

        [ThreadAndSerializationSafe]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("UploadImageDataToTexture")]
        private extern void UploadImageDataToTexture_Internal(Texture2D targetTexture);

        //-----------------------------------------------------------------
        [ThreadAndSerializationSafe]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        public extern IntPtr GetUnsafePointerToBuffer();

        //-----------------------------------------------------------------
        // The raw image data will be applied to the byteBuffer list
        // provided by the user.
        public void CopyRawImageDataIntoBuffer(List<byte> byteBuffer)
        {
        }

        [ThreadAndSerializationSafe]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("CopyRawImageDataIntoBuffer")]
        internal extern void CopyRawImageDataIntoBuffer_Internal([Out] byte[] byteArray);

        //-----------------------------------------------------------------
        internal PhotoCaptureFrame(IntPtr nativePtr)
        {
        }

        //-----------------------------------------------------------------
        private void Cleanup()
        {
        }

        [ThreadAndSerializationSafe]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("Dispose")]
        private extern void Dispose_Internal();

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
