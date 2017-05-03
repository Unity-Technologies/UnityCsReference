// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine.Playables;

namespace UnityEngine.Animations
{
[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AnimationMixerPlayable
{
    private static bool CreateHandleInternal (PlayableGraph graph, int inputCount, bool normalizeWeights, ref PlayableHandle handle) {
        return INTERNAL_CALL_CreateHandleInternal ( ref graph, inputCount, normalizeWeights, ref handle );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CreateHandleInternal (ref PlayableGraph graph, int inputCount, bool normalizeWeights, ref PlayableHandle handle);
}

}
