// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.NVIDIA
{
    ///<summary>Provides methods to manage loading and unloading NVIDIA module plugins.</summary>
    [NativeHeader("Modules/NVIDIA/NVPlugins.h")]
    public static class NVUnityPlugin
    {
        ///<summary>Attempts to dynamically load the plugin NVUnityPlugin.</summary>
        ///<remarks>
        /// The result this function returns is only valid the first time you call the function. If you call the
        /// function again, the result it returns is the same as the last value it returned. This function is only
        /// required if the user is not going through the native package NVIDIA.
        ///</remarks>
        ///<returns>Returns true if the function loaded the plugin successfully. Otherwise, returns false.</returns>
        extern public static bool Load();

        ///<summary>Checks whether the native plugin NVUnityPlugin in the NVIDIA native module has been loaded or not.</summary>
        ///<returns>Returns true if the native plugin has been loaded. Otherwise, returns false.</returns>
        extern public static bool IsLoaded();
    }
}
