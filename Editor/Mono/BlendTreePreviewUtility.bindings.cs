// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/Animation/BlendTreePreviewUtility.h")]
    public class BlendTreePreviewUtility
    {
        extern public static void GetRootBlendTreeChildWeights(Animator animator, int layerIndex, int stateHash, [Out] float[] weightArray);

        extern public static void CalculateRootBlendTreeChildWeights(Animator animator, int layerIndex, int stateHash, [Out] float[] weightArray, float blendX, float blendY);

        public static void CalculateBlendTexture(Animator animator, int layerIndex, int stateHash, Texture2D blendTexture, Texture2D[] weightTextures, Rect rect)
        {
            CalculateBlendTexture(animator, layerIndex, stateHash, blendTexture, weightTextures, rect.x, rect.y, rect.x + rect.width, rect.y + rect.height);
        }

        extern protected static void  CalculateBlendTexture(Animator animator, int layerIndex, int stateHash, Texture2D blendTexture, Texture2D[] weightTextures, float minX, float minY, float maxX, float maxY);
    }
}
