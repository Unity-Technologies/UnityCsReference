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
using Object = UnityEngine.Object;

namespace UnityEngine.Playables
{


public static partial class AudioPlayableGraphExtensions
{
    internal static bool InternalCreateAudioOutput (ref PlayableGraph graph, string name, out PlayableOutputHandle handle) {
        return INTERNAL_CALL_InternalCreateAudioOutput ( ref graph, name, out handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_InternalCreateAudioOutput (ref PlayableGraph graph, string name, out PlayableOutputHandle handle);
}


}
