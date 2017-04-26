// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables.Audio;
using Object = UnityEngine.Object;

namespace UnityEngine.Playables
{


public static partial class AudioPlayableGraphExtensions
{
    public static AudioPlayableOutput CreateAudioOutput(this PlayableGraph graph, string name, AudioSource target)
        {
            AudioPlayableOutput output = new AudioPlayableOutput();
            if (!InternalCreateAudioOutput(ref graph, name, out output.m_Output))
                return AudioPlayableOutput.Null;

            output.target = target;

            return output;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool InternalCreateAudioOutput (ref PlayableGraph graph, string name, out PlayableOutput output) ;

    public static void DestroyOutput(this PlayableGraph graph, AudioPlayableOutput output)
        {
            PlayableGraph.InternalDestroyOutput(ref graph, ref output.m_Output);
        }
    
    
}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AudioPlayableOutput
{
    internal PlayableOutput m_Output;
    
    
    public static AudioPlayableOutput Null
        {
            get { return new AudioPlayableOutput() { m_Output = PlayableOutput.Null }; }
        }
    
    
    internal Object referenceObject
        {
            get { return PlayableOutput.GetInternalReferenceObject(ref m_Output); }
            set { PlayableOutput.SetInternalReferenceObject(ref m_Output, value); }
        }
    
    
    public Object userData
        {
            get { return PlayableOutput.GetInternalUserData(ref m_Output); }
            set { PlayableOutput.SetInternalUserData(ref m_Output, value); }
        }
    
    
    public bool IsValid()
        {
            return PlayableOutput.IsValidInternal(ref m_Output);
        }
    
    
    public AudioSource target
        {
            get { return InternalGetTarget(ref m_Output); }
            set { InternalSetTarget(ref m_Output, value); }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  AudioSource InternalGetTarget (ref PlayableOutput output) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InternalSetTarget (ref PlayableOutput output, AudioSource target) ;

    public PlayableHandle sourcePlayable
        {
            get { return PlayableOutput.InternalGetSourcePlayable(ref m_Output); }
            set { PlayableOutput.InternalSetSourcePlayable(ref m_Output, ref value);  }
        }
    
    
    public int sourceInputPort
        {
            get { return PlayableOutput.InternalGetSourceInputPort(ref m_Output); }
            set { PlayableOutput.InternalSetSourceInputPort(ref m_Output, value); }
        }
    
    
    
}


}
