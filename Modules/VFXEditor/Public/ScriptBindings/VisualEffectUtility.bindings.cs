// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Experimental.VFX;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.VFX
{
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectUtility.h")]
    internal static class VisualEffectUtility
    {
        static public VFXSpawnerState GetSpawnerState(VisualEffect effect, uint systemIndex)
        {
            var vfxSpawnerState = new VFXSpawnerState(VFXSpawnerState.Internal_Create(), true);
            GetSpawnerState(effect, vfxSpawnerState.GetPtr(), systemIndex);
            return vfxSpawnerState;
        }

        [FreeFunction(Name = "VisualEffectUtility::GetSpawnerState", ThrowsException = true)] static extern private void GetSpawnerState([NotNull] VisualEffect effect, IntPtr spawnerState, uint systemIndex);

        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<bool>", ThrowsException = true)] static extern public bool GetExpressionBool([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<int>", ThrowsException = true)] static extern public int GetExpressionInt([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<UInt32>", ThrowsException = true)] static extern public uint GetExpressionUInt([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<float>", ThrowsException = true)] static extern public float GetExpressionFloat([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<Vector2f>", ThrowsException = true)] static extern public Vector2 GetExpressionVector2([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<Vector3f>", ThrowsException = true)] static extern public Vector3 GetExpressionVector3([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<Vector4f>", ThrowsException = true)] static extern public Vector4 GetExpressionVector4([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<Matrix4x4f>", ThrowsException = true)] static extern public Matrix4x4 GetExpressionMatrix4x4([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<Texture*>", ThrowsException = true)] static extern public Texture GetExpressionTexture([NotNull] VisualEffect effect, uint expressionIndex);
        [FreeFunction(Name = "VisualEffectUtility::GetExpressionValue<Mesh*>", ThrowsException = true)] static extern public Mesh GetExpressionMesh([NotNull] VisualEffect effect, uint expressionIndex);

        static public AnimationCurve GetExpressionAnimationCurve(VisualEffect effect, uint expressionIndex)
        {
            var animationCurve = new AnimationCurve();
            GetExpressionAnimationCurve(effect, expressionIndex, animationCurve);
            return animationCurve;
        }

        [FreeFunction(Name = "VisualEffectUtility::GetExpressionAnimationCurve", ThrowsException = true)]
        static extern private void GetExpressionAnimationCurve(VisualEffect effect, uint expressionIndex, AnimationCurve curve);
        static public Gradient GetExpressionGradient(VisualEffect effect, uint expressionIndex)
        {
            var gradient = new Gradient();
            GetExpressionGradient(effect, expressionIndex, gradient);
            return gradient;
        }

        [FreeFunction(Name = "VisualEffectUtility::GetExpressionGradient", ThrowsException = true)]
        static extern private void GetExpressionGradient([NotNull] VisualEffect effect, uint expressionIndex, Gradient gradient);

        extern public static bool renderBounds
        {
            [FreeFunction(Name = "VisualEffectUtility::GetRenderBounds")]
            get;
            [FreeFunction(Name = "VisualEffectUtility::SetRenderBounds")]
            set;
        }
    }
}
