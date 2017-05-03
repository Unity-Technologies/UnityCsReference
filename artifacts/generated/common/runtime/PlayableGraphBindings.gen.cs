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
    
    
    private static bool IsValidInternal (ref PlayableGraph graph) {
        return INTERNAL_CALL_IsValidInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsValidInternal (ref PlayableGraph graph);
    public static PlayableGraph Create()
        {
            PlayableGraph g = new PlayableGraph();
            CreateInternal(ref g);

            return g;
        }
    
    
    internal static void CreateInternal (ref PlayableGraph graph) {
        INTERNAL_CALL_CreateInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CreateInternal (ref PlayableGraph graph);
    public bool IsDone()
        {
            return IsDoneInternal(ref this);
        }
    
    
    internal static bool IsDoneInternal (ref PlayableGraph graph) {
        return INTERNAL_CALL_IsDoneInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_IsDoneInternal (ref PlayableGraph graph);
    public IExposedPropertyTable GetResolver()
        {
            return InternalGetResolver(ref this);
        }
    
    
    public void SetResolver(IExposedPropertyTable value)
        {
            InternalSetResolver(ref this, value);
        }
    
    
    internal static IExposedPropertyTable InternalGetResolver (ref PlayableGraph graph) {
        return INTERNAL_CALL_InternalGetResolver ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static IExposedPropertyTable INTERNAL_CALL_InternalGetResolver (ref PlayableGraph graph);
    internal static void InternalSetResolver (ref PlayableGraph graph, IExposedPropertyTable resolver) {
        INTERNAL_CALL_InternalSetResolver ( ref graph, resolver );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_InternalSetResolver (ref PlayableGraph graph, IExposedPropertyTable resolver);
    public void Play()
        {
            PlayInternal(ref this);
        }
    
    
    internal static void PlayInternal (ref PlayableGraph graph) {
        INTERNAL_CALL_PlayInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_PlayInternal (ref PlayableGraph graph);
    public void Stop()
        {
            StopInternal(ref this);
        }
    
    
    internal static void StopInternal (ref PlayableGraph graph) {
        INTERNAL_CALL_StopInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_StopInternal (ref PlayableGraph graph);
    public int GetPlayableCount()
        {
            return GetPlayableCountInternal(ref this);
        }
    
    
    internal static int GetPlayableCountInternal (ref PlayableGraph graph) {
        return INTERNAL_CALL_GetPlayableCountInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetPlayableCountInternal (ref PlayableGraph graph);
    public DirectorUpdateMode GetTimeUpdateMode()
        {
            return GetUpdateModeInternal(ref this);
        }
    
    
    public void SetTimeUpdateMode(DirectorUpdateMode value)
        {
            SetUpdateModeInternal(ref this, value);
        }
    
    
    private static DirectorUpdateMode GetUpdateModeInternal (ref PlayableGraph graph) {
        return INTERNAL_CALL_GetUpdateModeInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static DirectorUpdateMode INTERNAL_CALL_GetUpdateModeInternal (ref PlayableGraph graph);
    private static void SetUpdateModeInternal (ref PlayableGraph graph, DirectorUpdateMode mode) {
        INTERNAL_CALL_SetUpdateModeInternal ( ref graph, mode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetUpdateModeInternal (ref PlayableGraph graph, DirectorUpdateMode mode);
    internal static bool CreateScriptOutputInternal (ref PlayableGraph graph, string name, out PlayableOutputHandle handle) {
        return INTERNAL_CALL_CreateScriptOutputInternal ( ref graph, name, out handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CreateScriptOutputInternal (ref PlayableGraph graph, string name, out PlayableOutputHandle handle);
    internal PlayableHandle CreatePlayableHandle()
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreatePlayableHandleInternal(ref this, ref handle))
                return PlayableHandle.Null;
            return handle;
        }
    
    
    private static bool CreatePlayableHandleInternal (ref PlayableGraph graph, ref PlayableHandle handle) {
        return INTERNAL_CALL_CreatePlayableHandleInternal ( ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CreatePlayableHandleInternal (ref PlayableGraph graph, ref PlayableHandle handle);
    public void Destroy()
        {
            DestroyInternal(ref this);
        }
    
    
    private static void DestroyInternal (ref PlayableGraph graph) {
        INTERNAL_CALL_DestroyInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DestroyInternal (ref PlayableGraph graph);
    private static bool ConnectInternal (ref PlayableGraph graph, PlayableHandle source, int sourceOutputPort, PlayableHandle destination, int destinationInputPort) {
        return INTERNAL_CALL_ConnectInternal ( ref graph, ref source, sourceOutputPort, ref destination, destinationInputPort );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_ConnectInternal (ref PlayableGraph graph, ref PlayableHandle source, int sourceOutputPort, ref PlayableHandle destination, int destinationInputPort);
    private static void DisconnectInternal (ref PlayableGraph graph, PlayableHandle playable, int inputPort) {
        INTERNAL_CALL_DisconnectInternal ( ref graph, ref playable, inputPort );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DisconnectInternal (ref PlayableGraph graph, ref PlayableHandle playable, int inputPort);
    private static void DestroyPlayableInternal (ref PlayableGraph graph, PlayableHandle playable) {
        INTERNAL_CALL_DestroyPlayableInternal ( ref graph, ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DestroyPlayableInternal (ref PlayableGraph graph, ref PlayableHandle playable);
    internal static void DestroyOutputInternal (ref PlayableGraph graph, PlayableOutputHandle handle) {
        INTERNAL_CALL_DestroyOutputInternal ( ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DestroyOutputInternal (ref PlayableGraph graph, ref PlayableOutputHandle handle);
    private static void DestroySubgraphInternal (ref PlayableGraph graph, PlayableHandle playable) {
        INTERNAL_CALL_DestroySubgraphInternal ( ref graph, ref playable );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DestroySubgraphInternal (ref PlayableGraph graph, ref PlayableHandle playable);
    [uei.ExcludeFromDocs]
public void Evaluate () {
    float deltaTime = 0;
    Evaluate ( deltaTime );
}

public void Evaluate( [uei.DefaultValue("0")] float deltaTime )
        {
            EvaluateInternal(ref this, deltaTime);
        }

    
    
    internal static void EvaluateInternal (ref PlayableGraph graph, float deltaTime) {
        INTERNAL_CALL_EvaluateInternal ( ref graph, deltaTime );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_EvaluateInternal (ref PlayableGraph graph, float deltaTime);
    public int GetRootPlayableCount()
        {
            return GetRootPlayableCountInternal(ref this);
        }
    
    
    internal static int GetRootPlayableCountInternal (ref PlayableGraph graph) {
        return INTERNAL_CALL_GetRootPlayableCountInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetRootPlayableCountInternal (ref PlayableGraph graph);
    public Playable GetRootPlayable(int index)
        {
            PlayableHandle handle = PlayableHandle.Null;
            GetRootPlayableInternal(index, ref this, ref handle);
            return new Playable(handle);
        }
    
    
    internal static void GetRootPlayableInternal (int index, ref PlayableGraph graph, ref PlayableHandle handle) {
        INTERNAL_CALL_GetRootPlayableInternal ( index, ref graph, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetRootPlayableInternal (int index, ref PlayableGraph graph, ref PlayableHandle handle);
    private static int GetOutputCountInternal (ref PlayableGraph graph) {
        return INTERNAL_CALL_GetOutputCountInternal ( ref graph );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetOutputCountInternal (ref PlayableGraph graph);
    private static bool GetOutputInternal (ref PlayableGraph graph, int index, out PlayableOutputHandle handle) {
        return INTERNAL_CALL_GetOutputInternal ( ref graph, index, out handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetOutputInternal (ref PlayableGraph graph, int index, out PlayableOutputHandle handle);
    private static int GetOutputCountByTypeInternal (ref PlayableGraph graph, Type outputType) {
        return INTERNAL_CALL_GetOutputCountByTypeInternal ( ref graph, outputType );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_GetOutputCountByTypeInternal (ref PlayableGraph graph, Type outputType);
    private static bool GetOutputByTypeInternal (ref PlayableGraph graph, Type outputType, int index, out PlayableOutputHandle handle) {
        return INTERNAL_CALL_GetOutputByTypeInternal ( ref graph, outputType, index, out handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetOutputByTypeInternal (ref PlayableGraph graph, Type outputType, int index, out PlayableOutputHandle handle);
}


}
