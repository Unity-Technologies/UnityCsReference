// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Collections;

namespace UnityEngine
{
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct HumanPose
{
    
            public Vector3 bodyPosition;
            public Quaternion bodyRotation;
            public float[] muscles;
    
    internal void Init()
        {
            if (muscles != null)
            {
                if (muscles.Length != HumanTrait.MuscleCount)
                {
                    throw new ArgumentException("Bad array size for HumanPose.muscles. Size must equal HumanTrait.MuscleCount");
                }
            }

            if (muscles == null)
            {
                muscles = new float[HumanTrait.MuscleCount];

                if (bodyRotation.x == 0 && bodyRotation.y == 0 && bodyRotation.z == 0 && bodyRotation.w == 0)
                {
                    bodyRotation.w = 1;
                }
            }
        }
    
    
}

public sealed partial class HumanPoseHandler : IDisposable
{
    internal IntPtr m_Ptr;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Dispose () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_HumanPoseHandler (Avatar avatar, Transform root) ;

    public HumanPoseHandler(Avatar avatar, Transform root)
        {
            m_Ptr = IntPtr.Zero;

            if (root == null)
                throw new ArgumentNullException("HumanPoseHandler root Transform is null");

            if (avatar == null)
                throw new ArgumentNullException("HumanPoseHandler avatar is null");

            if (!avatar.isValid)
                throw new ArgumentException("HumanPoseHandler avatar is invalid");

            if (!avatar.isHuman)
                throw new ArgumentException("HumanPoseHandler avatar is not human");

            Internal_HumanPoseHandler(avatar, root);
        }
    
    
    private bool Internal_GetHumanPose (ref Vector3 bodyPosition, ref Quaternion bodyRotation, float[] muscles) {
        return INTERNAL_CALL_Internal_GetHumanPose ( this, ref bodyPosition, ref bodyRotation, muscles );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_GetHumanPose (HumanPoseHandler self, ref Vector3 bodyPosition, ref Quaternion bodyRotation, float[] muscles);
    public void GetHumanPose(ref HumanPose humanPose)
        {
            humanPose.Init();
            if (!Internal_GetHumanPose(ref humanPose.bodyPosition, ref humanPose.bodyRotation, humanPose.muscles))
            {
                Debug.LogWarning("HumanPoseHandler is not initialized properly");
            }
        }
    
    
    private bool Internal_SetHumanPose (ref Vector3 bodyPosition, ref Quaternion bodyRotation, float[] muscles) {
        return INTERNAL_CALL_Internal_SetHumanPose ( this, ref bodyPosition, ref bodyRotation, muscles );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Internal_SetHumanPose (HumanPoseHandler self, ref Vector3 bodyPosition, ref Quaternion bodyRotation, float[] muscles);
    public void SetHumanPose(ref HumanPose humanPose)
        {
            humanPose.Init();
            if (!Internal_SetHumanPose(ref humanPose.bodyPosition, ref humanPose.bodyRotation, humanPose.muscles))
            {
                Debug.LogWarning("HumanPoseHandler is not initialized properly");
            }
        }
    
    
}


}
