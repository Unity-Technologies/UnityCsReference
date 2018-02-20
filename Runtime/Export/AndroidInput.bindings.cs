// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;

namespace UnityEngine
{

    // AndroidInput provides support for off-screen touch input, such as a touchpad.
    [NativeHeader("Runtime/Input/GetInput.h")]
    public class AndroidInput
    {
        // Hide constructor
        private AndroidInput() {}

        // Returns object representing status of a specific touch on a secondary touchpad (Does not allocate temporary variables).
        public static Touch GetSecondaryTouch(int index)
        {
            return new Touch();
        }


        // Number of secondary touches. Guaranteed not to change throughout the frame. (RO).
        public static int touchCountSecondary
        {
            get { return GetTouchCount_Bindings(); }
        }

        [FreeFunction]
        [NativeConditional("PLATFORM_ANDROID")]
        internal static extern int GetTouchCount_Bindings();

        // Property indicating whether the system provides secondary touch input.
        public static bool secondaryTouchEnabled
        {
            get { return IsInputDeviceEnabled_Bindings(); }
        }

        [FreeFunction]
        [NativeConditional("PLATFORM_ANDROID")]
        internal static extern bool IsInputDeviceEnabled_Bindings();

        // Property indicating the width of the secondary touchpad.
        public static int secondaryTouchWidth
        {
            get { return GetTouchpadWidth(); }
        }

        [FreeFunction]
        [NativeConditional("PLATFORM_ANDROID")]
        internal static extern int GetTouchpadWidth();

        // Property indicating the height of the secondary touchpad.
        public static int secondaryTouchHeight
        {
            get { return GetTouchpadHeight(); }
        }

        [FreeFunction]
        [NativeConditional("PLATFORM_ANDROID")]
        internal static extern int GetTouchpadHeight();
    }
}
