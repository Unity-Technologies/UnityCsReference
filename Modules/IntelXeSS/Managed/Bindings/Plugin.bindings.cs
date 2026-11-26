// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Intel
{
    /// <summary>Provides methods to manage loading and unloading Intel XeSS plugin.</summary>
    /// <remarks>
    /// <para>
    /// <c>XeSSUnityPlugin integrates Intel's XeSS Super Resolution technology into Unity.</c>
    /// </para>
    /// <para>
    /// To access this API, follow these steps:
    /// <list type="number">
    /// <item><description>Open the <b>Package Manager</b> window.</description></item>
    /// <item><description>Go to <b>Built-in packages</b>.</description></item>
    /// <item><description>Enable the <b>Intel XeSS</b> package.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Once enabled, the plugin is automatically loaded by Unity and becomes available.
    /// </para>
    /// <para>
    /// <b>Note:</b> You don't need to use this API to enable XeSS in a Unity project. Once you enable the package as described above,
    /// XeSS can be enabled via the upscaling framework provided by Unity's scriptable render pipeline. It's primarily intended for
    /// users who want to integrate XeSS in their custom render pipelines.
    /// </para>
    /// </remarks>
    /// <seealso cref="UnityEngine.Intel.XeSSNativeContext" />
    /// <seealso cref="UnityEngine.Intel.GraphicsDevice" />
    /// <seealso cref="T:UnityEngine.Rendering.CommandBuffer" />
    /// <seealso cref="T:UnityEngine.Rendering.ScriptableRenderContext" />
    [NativeHeader("Modules/IntelXeSS/IntelXeSSPlugin.h")]
    public static class XeSSUnityPlugin
    {
        /// <summary>Attempts to dynamically load the XeSSUnityPlugin.</summary>
        /// <remarks>The result is cached and only valid the first time you call the function.</remarks>
        /// <returns>true if the plugin is loaded successfully, false otherwise.</returns>
        extern public static bool Load();

        /// <summary>Check whether the <c>XeSSUnityPlugin</c> has been loaded or not.</summary>
        /// <returns>true if the plugin has been loaded, false otherwise.</returns>
        extern public static bool IsLoaded();
    }
}
