// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.Apple
{
    [NativeHeader("Runtime/Export/Apple/FrameCaptureMetalScriptBindings.h")]
    public enum FrameCaptureDestination
    {
        DevTools = 1,
        GPUTraceDocument = 2,
    }

    // please note that (unlike ios/tvos) we do NOT have cs define for "macos code"
    // so we cant make this api platform-conditional at cs side (at least not now)

    [NativeHeader("Runtime/Export/Apple/FrameCaptureMetalScriptBindings.h")]
    [NativeConditional("PLATFORM_IOS || PLATFORM_TVOS || PLATFORM_OSX")]
    public class FrameCapture
    {
        private FrameCapture() {}

        [FreeFunction("FrameCaptureMetalScripting::IsDestinationSupported")] extern private static bool IsDestinationSupportedImpl(FrameCaptureDestination dest);
        [FreeFunction("FrameCaptureMetalScripting::BeginCapture")] extern private static void BeginCaptureImpl(FrameCaptureDestination dest, string path);
        [FreeFunction("FrameCaptureMetalScripting::EndCapture")] extern private static void EndCaptureImpl();

        [FreeFunction("FrameCaptureMetalScripting::CaptureNextFrame")] extern private static void CaptureNextFrameImpl(FrameCaptureDestination dest, string path);

        public static bool IsDestinationSupported(FrameCaptureDestination dest)
        {
            // i am not sure how paranoid should we be about enum values
            // but this is way easier to ignore error checks in lower level code, so we check here once
            if (dest != FrameCaptureDestination.DevTools && dest != FrameCaptureDestination.GPUTraceDocument)
                throw new ArgumentException("dest", "Argument dest has bad value (not one of FrameCaptureDestination enum values)");

            return IsDestinationSupportedImpl(dest);
        }

        public static void BeginCaptureToXcode()
        {
            if (!IsDestinationSupported(FrameCaptureDestination.DevTools))
                throw new InvalidOperationException("Frame Capture with DevTools is not supported.");

            BeginCaptureImpl(FrameCaptureDestination.DevTools, null);
        }

        public static void BeginCaptureToFile(string path)
        {
            if (!IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument))
                throw new InvalidOperationException("Frame Capture to file is not supported.");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path", "Path must be supplied when capture destination is GPUTraceDocument.");
            if (System.IO.Path.GetExtension(path) != ".gputrace")
                throw new ArgumentException("path", "Destination file should have .gputrace extension.");

            BeginCaptureImpl(FrameCaptureDestination.GPUTraceDocument, new Uri(path).AbsoluteUri);
        }

        public static void EndCapture()
        {
            EndCaptureImpl();
        }

        public static void CaptureNextFrameToXcode()
        {
            if (!IsDestinationSupported(FrameCaptureDestination.DevTools))
                throw new InvalidOperationException("Frame Capture with DevTools is not supported.");

            CaptureNextFrameImpl(FrameCaptureDestination.DevTools, null);
        }

        public static void CaptureNextFrameToFile(string path)
        {
            if (!IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument))
                throw new InvalidOperationException("Frame Capture to file is not supported.");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path", "Path must be supplied when capture destination is GPUTraceDocument.");
            if (System.IO.Path.GetExtension(path) != ".gputrace")
                throw new ArgumentException("path", "Destination file should have .gputrace extension.");

            CaptureNextFrameImpl(FrameCaptureDestination.GPUTraceDocument, new Uri(path).AbsoluteUri);
        }
    }
}
