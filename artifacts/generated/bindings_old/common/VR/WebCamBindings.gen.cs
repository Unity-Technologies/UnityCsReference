// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

#pragma warning disable 649

namespace UnityEngine.XR.WSA.WebCam
{


public static partial class WebCam
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetWebCamModeState_Internal () ;

}

public sealed partial class VideoCapture : IDisposable
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private bool IsRecording_Internal (IntPtr videoCaptureObj) ;

    private static IntPtr Instantiate_Internal (bool showHolograms, OnVideoCaptureResourceCreatedCallback onCreatedCallback) {
        IntPtr result;
        INTERNAL_CALL_Instantiate_Internal ( showHolograms, onCreatedCallback, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Instantiate_Internal (bool showHolograms, OnVideoCaptureResourceCreatedCallback onCreatedCallback, out IntPtr value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StartVideoMode_Internal (IntPtr videoCaptureObj, int audioState, OnVideoModeStartedCallback onVideoModeStartedCallback, float hologramOpacity,  float frameRate, int cameraResolutionWidth, int cameraResolutionHeight, int pixelFormat) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StopVideoMode_Internal (IntPtr videoCaptureObj, OnVideoModeStoppedCallback onVideoModeStoppedCallback) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StartRecordingVideoToDisk_Internal (IntPtr videoCaptureObj, string filename, OnStartedRecordingVideoCallback onStartedRecordingVideoCallback) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StopRecordingVideoToDisk_Internal (IntPtr videoCaptureObj, OnStoppedRecordingVideoCallback onStoppedRecordingVideoCallback) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Dispose_Internal (IntPtr videoCaptureObj) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void DisposeThreaded_Internal (IntPtr videoCaptureObj) ;

    [ThreadAndSerializationSafe ()]
    private static IntPtr GetUnsafePointerToVideoDeviceController_Internal (IntPtr videoCaptureObj) {
        IntPtr result;
        INTERNAL_CALL_GetUnsafePointerToVideoDeviceController_Internal ( videoCaptureObj, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetUnsafePointerToVideoDeviceController_Internal (IntPtr videoCaptureObj, out IntPtr value);
}

public sealed partial class PhotoCapture : IDisposable
{
    private static IntPtr Instantiate_Internal (bool showHolograms, OnCaptureResourceCreatedCallback onCreatedCallback) {
        IntPtr result;
        INTERNAL_CALL_Instantiate_Internal ( showHolograms, onCreatedCallback, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Instantiate_Internal (bool showHolograms, OnCaptureResourceCreatedCallback onCreatedCallback, out IntPtr value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StartPhotoMode_Internal (IntPtr photoCaptureObj, OnPhotoModeStartedCallback onPhotoModeStartedCallback, float hologramOpacity,  float frameRate, int cameraResolutionWidth, int cameraResolutionHeight, int pixelFormat) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void StopPhotoMode_Internal (IntPtr photoCaptureObj, OnPhotoModeStoppedCallback onPhotoModeStoppedCallback) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void CapturePhotoToDisk_Internal (IntPtr photoCaptureObj, string filename, int fileOutputFormat, OnCapturedToDiskCallback onCapturedPhotoToDiskCallback) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void CapturePhotoToMemory_Internal (IntPtr photoCaptureObj, OnCapturedToMemoryCallback onCapturedPhotoToMemoryCallback) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Dispose_Internal (IntPtr photoCaptureObj) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void DisposeThreaded_Internal (IntPtr photoCaptureObj) ;

    [ThreadAndSerializationSafe ()]
    private static IntPtr GetUnsafePointerToVideoDeviceController_Internal (IntPtr photoCaptureObj) {
        IntPtr result;
        INTERNAL_CALL_GetUnsafePointerToVideoDeviceController_Internal ( photoCaptureObj, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetUnsafePointerToVideoDeviceController_Internal (IntPtr photoCaptureObj, out IntPtr value);
}

public sealed partial class PhotoCaptureFrame : IDisposable
{
}


}
