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
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Profiling
{


[UsedByNativeCode]
public partial class Sampler
{
    internal IntPtr m_Ptr;
    internal static Sampler s_InvalidSampler = new Sampler();
    
    
    internal Sampler() {}
    
    
    public bool isValid
        {
            get { return m_Ptr != IntPtr.Zero; }
        }
    
    
    public Recorder GetRecorder()
        {
            var recorder = GetRecorderInternal();
            return recorder ?? Recorder.s_InvalidRecorder;
        }
    
    
    public static Sampler Get(string name)
        {
            var sampler = GetSamplerInternal(name);
            return sampler ?? s_InvalidSampler;
        }
    
    
    public static int GetNames(List<string> names)
        {
            return GetSamplerNamesInternal(names);
        }
    
    
    [ThreadAndSerializationSafe ()]
    public extern  string name
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Recorder GetRecorderInternal () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  Sampler GetSamplerInternal (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetSamplerNamesInternal (object namesScriptingPtr) ;

    
    
}

[UsedByNativeCode]
public sealed partial class CustomSampler : Sampler
{
    internal static CustomSampler s_InvalidCustomSampler = new CustomSampler();
    
    
    internal CustomSampler() {}
    
    
    static public CustomSampler Create(string name)
        {
            var sampler = CreateInternal(name);
            return sampler ?? s_InvalidCustomSampler;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  CustomSampler CreateInternal (string name) ;

    [System.Diagnostics.ConditionalAttribute("ENABLE_PROFILER")]
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Begin () ;

    [System.Diagnostics.ConditionalAttribute("ENABLE_PROFILER")]
    public void Begin(UnityEngine.Object targetObject)
        {
            BeginWithObject(targetObject);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void BeginWithObject (UnityEngine.Object targetObject) ;

    [System.Diagnostics.ConditionalAttribute("ENABLE_PROFILER")]
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void End () ;

}

}
