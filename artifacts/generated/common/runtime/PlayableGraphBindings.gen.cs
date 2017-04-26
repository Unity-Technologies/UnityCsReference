// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngine.Playables
{
public enum DirectorUpdateMode
{
    DSPClock = 0,
    GameTime = 1,
    UnscaledGameTime = 2,
    Manual   = 3,
}

public enum DataStreamType
{
    Animation = 0,
    Audio  = 1,
    Video  = 2,
    None   = 3
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct PlayableGraph
{
    internal IntPtr m_Handle;
    internal Int32  m_Version;
    
    
    public bool IsValid()
        {
            return IsValidInternal(ref this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool IsValidInternal (ref PlayableGraph graph) ;

    public static PlayableGraph CreateGraph()
        {
            PlayableGraph g = new PlayableGraph();
            InternalCreate(ref g);

            return g;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalCreate (ref PlayableGraph graph) ;

    public bool isDone
        {
            get { return InternalIsDone(ref this); }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool InternalIsDone (ref PlayableGraph graph) ;

    public IExposedPropertyTable resolver { get {return InternalGetResolver(ref this); }  set {InternalSetResolver(ref this, value); }}
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  IExposedPropertyTable InternalGetResolver (ref PlayableGraph graph) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalSetResolver (ref PlayableGraph graph, IExposedPropertyTable resolver) ;

    public void Play()
        {
            InternalPlay(ref this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalPlay (ref PlayableGraph graph) ;

    public void Stop()
        {
            InternalStop(ref this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalStop (ref PlayableGraph graph) ;

    public int playableCount
        {
            get { return InternalPlayableCount(ref this); }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int InternalPlayableCount (ref PlayableGraph graph) ;

    public DirectorUpdateMode timeUpdateMode
        {
            get { return InternalGetUpdateMode(ref this); }
            set { InternalSetUpdateMode(ref this, value); }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  DirectorUpdateMode InternalGetUpdateMode (ref PlayableGraph graph) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InternalSetUpdateMode (ref PlayableGraph graph, DirectorUpdateMode mode) ;

    public ScriptPlayableOutput CreateScriptOutput(string name)
        {
            ScriptPlayableOutput output = new ScriptPlayableOutput();
            if (!InternalCreateScriptOutput(ref this, name, out output.m_Output))
                return ScriptPlayableOutput.Null;
            return output;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool InternalCreateScriptOutput (ref PlayableGraph graph, string name, out PlayableOutput output) ;

    public PlayableHandle CreatePlayable()
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreatePlayable(ref this, ref handle))
                return PlayableHandle.Null;
            return handle;
        }
    
    
    [uei.ExcludeFromDocs]
public PlayableHandle CreateGenericMixerPlayable () {
    int inputCount = 0;
    return CreateGenericMixerPlayable ( inputCount );
}

public PlayableHandle CreateGenericMixerPlayable( [uei.DefaultValue("0")] int inputCount )
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreatePlayable(ref this, ref handle))
                return PlayableHandle.Null;
            handle.inputCount = inputCount;
            return handle;
        }

    
    
    private static bool InternalCreatePlayable (ref PlayableGraph graph, ref PlayableHandle handle) {
        return INTERNAL_CALL_InternalCreatePlayable ( ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreatePlayable (ref PlayableGraph graph, ref PlayableHandle handle);
    public void Destroy()
        {
            DestroyInternal(ref this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void DestroyInternal (ref PlayableGraph graph) ;

    public bool Connect(PlayableHandle source, int sourceOutputPort, PlayableHandle destination, int destinationInputPort)
        {
            return ConnectInternal(ref this, source, sourceOutputPort, destination, destinationInputPort);
        }
    
    
    public bool Connect(Playable source, int sourceOutputPort, Playable destination, int destinationInputPort)
        {
            return ConnectInternal(ref this, source.handle, sourceOutputPort, destination.handle, destinationInputPort);
        }
    
    
    private static bool ConnectInternal (ref PlayableGraph graph, PlayableHandle source, int sourceOutputPort, PlayableHandle destination, int destinationInputPort) {
        return INTERNAL_CALL_ConnectInternal ( ref graph, ref source, sourceOutputPort, ref destination, destinationInputPort );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ConnectInternal (ref PlayableGraph graph, ref PlayableHandle source, int sourceOutputPort, ref PlayableHandle destination, int destinationInputPort);
    public void Disconnect(Playable playable, int inputPort)
        {
            var handle = playable.handle;
            DisconnectInternal(ref this, ref handle, inputPort);
        }
    
    
    public void Disconnect(PlayableHandle playable, int inputPort)
        {
            DisconnectInternal(ref this, ref playable, inputPort);
        }
    
    
    private static void DisconnectInternal (ref PlayableGraph graph, ref PlayableHandle playable, int inputPort) {
        INTERNAL_CALL_DisconnectInternal ( ref graph, ref playable, inputPort );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DisconnectInternal (ref PlayableGraph graph, ref PlayableHandle playable, int inputPort);
    public void DestroyPlayable(PlayableHandle playable)
        {
            InternalDestroyPlayable(ref this, ref playable);
        }
    
    
    private static void InternalDestroyPlayable (ref PlayableGraph graph, ref PlayableHandle playable) {
        INTERNAL_CALL_InternalDestroyPlayable ( ref graph, ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalDestroyPlayable (ref PlayableGraph graph, ref PlayableHandle playable);
    public void DestroyOutput(ScriptPlayableOutput output)
        {
            InternalDestroyOutput(ref this, ref output.m_Output);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalDestroyOutput (ref PlayableGraph graph, ref PlayableOutput output) ;

    public void DestroySubgraph(PlayableHandle playable)
        {
            InternalDestroySubgraph(ref this, playable);
        }
    
    
    private static void InternalDestroySubgraph (ref PlayableGraph graph, PlayableHandle playable) {
        INTERNAL_CALL_InternalDestroySubgraph ( ref graph, ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalDestroySubgraph (ref PlayableGraph graph, ref PlayableHandle playable);
    [uei.ExcludeFromDocs]
public void Evaluate () {
    float deltaTime = 0;
    Evaluate ( deltaTime );
}

public void Evaluate( [uei.DefaultValue("0")] float deltaTime )
        {
            InternalEvaluate(ref this, deltaTime);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InternalEvaluate (ref PlayableGraph graph, float deltaTime) ;

    public int rootPlayableCount
        {
            get { return InternalRootPlayableCount(ref this); }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int InternalRootPlayableCount (ref PlayableGraph graph) ;

    public PlayableHandle GetRootPlayable(int index)
        {
            PlayableHandle handle = PlayableHandle.Null;
            InternalGetRootPlayable(index, ref this, ref handle);
            return handle;
        }
    
    
    internal static void InternalGetRootPlayable (int index, ref PlayableGraph graph, ref PlayableHandle handle) {
        INTERNAL_CALL_InternalGetRootPlayable ( index, ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalGetRootPlayable (int index, ref PlayableGraph graph, ref PlayableHandle handle);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int InternalScriptOutputCount (ref PlayableGraph graph) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool InternalGetScriptOutput (ref PlayableGraph graph, int index, out PlayableOutput output) ;

    private static void SetScriptInstance (ref PlayableHandle handle, object instance) {
        INTERNAL_CALL_SetScriptInstance ( ref handle, instance );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetScriptInstance (ref PlayableHandle handle, object instance);
}


}
