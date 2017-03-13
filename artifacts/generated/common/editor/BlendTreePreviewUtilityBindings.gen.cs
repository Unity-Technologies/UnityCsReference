// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;

namespace UnityEditorInternal
{
public sealed partial class BlendTreePreviewUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void GetRootBlendTreeChildWeights (Animator animator, int layerIndex, int stateHash, float[] weightArray) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CalculateRootBlendTreeChildWeights (Animator animator, int layerIndex, int stateHash, float[] weightArray, float blendX, float blendY) ;

    public static void CalculateBlendTexture (Animator animator, int layerIndex, int stateHash, Texture2D blendTexture, Texture2D[] weightTextures, Rect rect) {
        INTERNAL_CALL_CalculateBlendTexture ( animator, layerIndex, stateHash, blendTexture, weightTextures, ref rect );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CalculateBlendTexture (Animator animator, int layerIndex, int stateHash, Texture2D blendTexture, Texture2D[] weightTextures, ref Rect rect);
}

}
