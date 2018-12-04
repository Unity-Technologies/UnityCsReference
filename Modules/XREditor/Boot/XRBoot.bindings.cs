// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEditor;

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEditor.Experimental.XR
{
    /// <summary>
    /// Helper class to allow us to provide a *hidden* API for setting boot config options
    /// without actually making boot config public.
    /// </summary>
    [ExcludeFromDocs]
    [NativeType(Header = "Modules/XREditor/Boot/XRBoot.h")]
    public class BootOptions
    {
        /// <summary>
        /// API to allow an XR SDK provider to make sure that correct VR tasks are
        /// completed at boot time. Calling this with a valid library name will set
        /// a new option that will allow us to use the correct library to side call
        /// into.
        /// </summary>
        ///
        /// <param name="bootConfigPath">The path to the boot config file</param>
        /// <param name="libraryName">The library name</param>
        [ExcludeFromDocs]
        public static void SetXRSDKPreInitLibrary(string bootConfigPath, string libraryName)
        {
            Internal_SetXRSDKPreInitLibrary(bootConfigPath, libraryName);
        }

        [ExcludeFromDocs]
        [NativeThrows]
        [NativeConditional("ENABLE_XR")]
        [StaticAccessor("XRBoot", StaticAccessorType.DoubleColon)]
        [NativeName("SetXRSDKPreInitLibrary")]
        internal static extern void Internal_SetXRSDKPreInitLibrary(string bootConfigPath, string libraryName);
    }
}
