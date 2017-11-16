// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    public struct HumanPose
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
                    throw new InvalidOperationException("Bad array size for HumanPose.muscles. Size must equal HumanTrait.MuscleCount");
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

    [NativeHeader("Runtime/Animation/HumanPoseHandler.h")]
    [NativeHeader("Runtime/Animation/ScriptBindings/Animation.bindings.h")]
    public class HumanPoseHandler : IDisposable
    {
        internal IntPtr m_Ptr;

        [FreeFunction("AnimationBindings::CreateHumanPoseHandler")]
        extern private static IntPtr Internal_Create(Avatar avatar, Transform root);

        [FreeFunction("AnimationBindings::DestroyHumanPoseHandler")]
        extern private static void Internal_Destroy(IntPtr ptr);

        extern private void GetHumanPose(out Vector3 bodyPosition, out Quaternion bodyRotation, [Out] float[] muscles);
        extern private void SetHumanPose(ref Vector3 bodyPosition, ref Quaternion bodyRotation, float[] muscles);

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

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

            m_Ptr = Internal_Create(avatar, root);
        }

        public void GetHumanPose(ref HumanPose humanPose)
        {
            if (m_Ptr == IntPtr.Zero)
                throw new NullReferenceException("HumanPoseHandler is not initialized properly");

            humanPose.Init();
            GetHumanPose(out humanPose.bodyPosition, out humanPose.bodyRotation, humanPose.muscles);
        }

        public void SetHumanPose(ref HumanPose humanPose)
        {
            if (m_Ptr == IntPtr.Zero)
                throw new NullReferenceException("HumanPoseHandler is not initialized properly");

            humanPose.Init();
            SetHumanPose(ref humanPose.bodyPosition, ref humanPose.bodyRotation, humanPose.muscles);
        }
    }
}
