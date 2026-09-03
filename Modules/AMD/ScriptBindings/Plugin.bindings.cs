// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.AMD
{
    ///<summary>Provides methods to manage loading and unloading AMD module plugins.</summary>
    ///<remarks>
    ///<c>AMDUnityPlugin</c> contains the implementations for AMD's temporal upscaling technologies:
    ///
    ///- <see href="https://gpuopen.com/fidelityfx-superresolution-2/">FidelityFX Super Resolution 2</see>
    ///- <see href="https://gpuopen.com/amd-fsr-upscaling/">FidelityFX Super Resolution 3 and 4</see>
    ///
    ///To access this API, follow these steps:
    ///
    ///1. Open the **Package Manager** window
    ///2. Go to **Built-in packages**.
    ///3. Enable the **AMD** package.
    ///
    ///Once enabled, the plugin is automatically loaded by Unity and becomes available for both built-in and custom FSR integration workflows.
    ///
    ///**Note:** You don't need to use this API to enable FSR in a Unity project. For more information,
    ///refer to Upscaling Framework or <see href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.2/manual/Dynamic-Resolution.html">HDRP Dynamic Resolution</see>.
    ///
    ///<c>AMDUnityPlugin</c> enables advanced users to access and integrate AMD's FSR functionality directly in custom rendering workflows. 
    ///It's primarily intended for users who want to bypass Upscaler Framework or the built-in engine integration available in the High Definition Render Pipeline (HDRP),
    ///to implement their own FSR logic, such as through a <c>CustomPass</c> or other Scriptable Render Pipeline (SRP) extensions.
    ///
    ///When using this API manually, it's good practice to verify that the plugin is available using <see cref="AMDUnityPlugin.IsLoaded" />. 
    ///In case the plugin fails to load automatically (for example, the end user isn't using the native AMD package), you can load it manually using <see cref="AMDUnityPlugin.Load" />.
    ///</remarks>
    ///
    ///<example nocheck="true">
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.Rendering;
    ///using UnityEngine.Rendering.HighDefinition;
    ///using UnityEngine.AMD;
    ///
    /// // Example HDRP custom pass
    ///public class CustomFSRPass : CustomPass
    ///{
    ///    public static bool EnsureAMDPluginLoaded()
    ///    {
    ///        if (!AMDUnityPlugin.IsLoaded())
    ///        {
    ///            Debug.Log("AMDUnityPlugin is not loaded!");
    ///            if (!AMDUnityPlugin.Load())
    ///            {
    ///                Debug.LogError("Unable to load AMDUnityPlugin");
    ///                return false;
    ///            }
    ///        }
    ///
    ///        Debug.Log("AMDUnityPlugin is successfully loaded!");
    ///        return true;
    ///    }
    ///
    ///    void InitializeAMDDevice()
    ///    {
    ///        if (!EnsureAMDPluginLoaded())
    ///            return;
    ///
    ///        // device initialization code
    ///    }
    ///
    ///    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    ///    {
    ///        if (amdDevice == null)
    ///        {
    ///            InitializeAMDDevice();
    ///        }
    ///        
    ///        // other pass setup code
    ///    }
    ///
    ///    protected override void Execute(CustomPassContext ctx)
    ///    {
    ///        // pass execution code
    ///    }
    ///
    ///    protected override void Cleanup()
    ///    {
    ///        // pass cleanup code
    ///    }
    ///
    ///    private GraphicsDevice amdDevice = null;
    ///    // other member variables
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="AMD.FSR2Context" />
    ///<seealso cref="AMD.GraphicsDevice" />
    ///<seealso cref="T:UnityEngine.Rendering.CommandBuffer" />
    ///<seealso cref="T:UnityEngine.Rendering.ScriptableRenderContext" />
    [NativeHeader("Modules/AMD/AMDPlugins.h")]
    public static class AMDUnityPlugin
    {
        ///<summary>Attempts to dynamically load the AMDUnityPlugin.</summary>
        ///<remarks>The result this function returns is only valid the first time you call the function. If you call the function again, the result it returns is the same as the last value it returned. This function is only required if the user is not going through the native AMD package.</remarks>
        ///<returns>Returns true if the function loaded the plugin successfully. Otherwise, returns false.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Rendering;
        ///using UnityEngine.Rendering.HighDefinition;
        ///using UnityEngine.AMD;
        ///
        /// // Example HDRP custom pass 
        ///public class CustomFSRPass : CustomPass
        ///{
        ///    public static bool EnsureAMDPluginLoaded()
        ///    {
        ///        if (!AMDUnityPlugin.IsLoaded())
        ///        {
        ///            Debug.Log("AMDUnityPlugin is not loaded!");
        ///            if (!AMDUnityPlugin.Load())
        ///            {
        ///                Debug.LogError("Unable to load AMDUnityPlugin");
        ///                return false;
        ///            }
        ///        }
        ///
        ///        Debug.Log("AMDUnityPlugin is successfully loaded!");
        ///        return true;
        ///    }
        ///
        ///    void InitializeAMDDevice()
        ///    {
        ///        if (!EnsureAMDPluginLoaded())
        ///            return;
        ///
        ///        // device initialization code
        ///    }
        ///
        ///    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        ///    {
        ///        if (amdDevice == null)
        ///        {
        ///            InitializeAMDDevice();
        ///        }
        ///        
        ///        // other pass setup code
        ///    }
        ///
        ///    protected override void Execute(CustomPassContext ctx)
        ///    {
        ///        // pass execution code
        ///    }
        ///
        ///    protected override void Cleanup()
        ///    {
        ///        // pass cleanup code
        ///    }
        ///
        ///    private GraphicsDevice amdDevice = null;
        ///    // other member variables
        ///}
        ///]]></code>
        ///</example>
        extern public static bool Load();

        ///<summary>Checks whether the <c>AMDUnityPlugin</c> in the AMD native module has been loaded or not.</summary>
        ///<returns>Returns true if the plugin has been loaded. Otherwise, returns false.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Rendering;
        ///using UnityEngine.Rendering.HighDefinition;
        ///using UnityEngine.AMD;
        ///
        /// // Example HDRP custom pass 
        ///public class CustomFSRPass : CustomPass
        ///{
        ///    public static bool EnsureAMDPluginLoaded()
        ///    {
        ///        if (!AMDUnityPlugin.IsLoaded())
        ///        {
        ///            Debug.Log("AMDUnityPlugin is not loaded!");
        ///            if (!AMDUnityPlugin.Load())
        ///            {
        ///                Debug.LogError("Unable to load AMDUnityPlugin");
        ///                return false;
        ///            }
        ///        }
        ///
        ///        Debug.Log("AMDUnityPlugin is successfully loaded!");
        ///        return true;
        ///    }
        ///
        ///    void InitializeAMDDevice()
        ///    {
        ///        if (!EnsureAMDPluginLoaded())
        ///            return;
        ///
        ///        // device initialization code
        ///    }
        ///
        ///    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        ///    {
        ///        if (amdDevice == null)
        ///        {
        ///            InitializeAMDDevice();
        ///        }
        ///        
        ///        // other pass setup code
        ///    }
        ///
        ///    protected override void Execute(CustomPassContext ctx)
        ///    {
        ///        // pass execution code
        ///    }
        ///
        ///    protected override void Cleanup()
        ///    {
        ///        // pass cleanup code
        ///    }
        ///
        ///    private GraphicsDevice amdDevice = null;
        ///    // other member variables
        ///}
        ///]]></code>
        ///</example>
        extern public static bool IsLoaded();
    }
}
