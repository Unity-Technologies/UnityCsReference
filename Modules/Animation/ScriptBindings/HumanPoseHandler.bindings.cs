// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    public struct HumanPose
    {
        static int k_NumIkGoals = Enum.GetValues(typeof(AvatarIKGoal)).Length;
        //These must stay in sync with the definition in HumanGetGoalOrientationOffset in human.cpp
        internal static Quaternion[] s_IKGoalOffsets = { new Quaternion(0.5f, -0.5f, 0.5f, 0.5f), new Quaternion(0.5f, -0.5f, 0.5f, 0.5f), new Quaternion(0.707107f, 0, 0.707107f, 0), new Quaternion(0, 0.707107f, 0, 0.707107f) };

        public Vector3 bodyPosition;
        public Quaternion bodyRotation;
        public float[] muscles;
        internal Vector3 [] m_IkGoalPositions;
        internal Quaternion[] m_IkGoalRotations;
        internal Quaternion[] m_OffsetIkGoalRotations;


        public ReadOnlySpan<Vector3> ikGoalPositions => new ReadOnlySpan<Vector3>(m_IkGoalPositions);
        public ReadOnlySpan<Quaternion> internalIkGoalRotations => new ReadOnlySpan<Quaternion>(m_IkGoalRotations);
        public ReadOnlySpan<Quaternion> ikGoalRotations => new ReadOnlySpan<Quaternion>(m_OffsetIkGoalRotations);


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
            
            if (m_IkGoalPositions != null && m_IkGoalPositions.Length != k_NumIkGoals)
            {
                throw new InvalidOperationException("Bad array size for HumanPose.ikGoalPositions. Size must equal AvatakIKGoal size");
            }

            if (m_IkGoalPositions == null)
            {
                m_IkGoalPositions = new Vector3[k_NumIkGoals];
            }

            if (m_IkGoalRotations != null && m_IkGoalRotations.Length != k_NumIkGoals)
            {
                throw new InvalidOperationException("Bad array size for HumanPose.ikGoalPositions. Size must equal AvatakIKGoal size");
            }

            if (m_IkGoalRotations == null)
            {
                m_IkGoalRotations = new Quaternion[k_NumIkGoals];
            }

            if (m_OffsetIkGoalRotations != null && m_OffsetIkGoalRotations.Length != k_NumIkGoals)
            {
                throw new InvalidOperationException("Bad array size for HumanPose.ikGoalPositions. Size must equal AvatakIKGoal size");
            }

            if (m_OffsetIkGoalRotations == null)
            {
                m_OffsetIkGoalRotations = new Quaternion[k_NumIkGoals];
            }
        }
    }

    [NativeHeader("Modules/Animation/HumanPoseHandler.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animation.bindings.h")]
    public class HumanPoseHandler : IDisposable
    {
        internal IntPtr m_Ptr;

        [FreeFunction("AnimationBindings::CreateHumanPoseHandler")]
        extern private static IntPtr Internal_CreateFromRoot(Avatar avatar, Transform root);

        [FreeFunction("AnimationBindings::CreateHumanPoseHandler", IsThreadSafe = true)]
        extern private static IntPtr Internal_CreateFromJointPaths(Avatar avatar, string[] jointPaths);

        [FreeFunction("AnimationBindings::DestroyHumanPoseHandler")]
        extern private static void Internal_Destroy(IntPtr ptr);

        extern private void GetHumanPose(out Vector3 bodyPosition, out Quaternion bodyRotation, [Out] float[] muscles, [Out] Vector3[] ikGoalPositions, [Out] Quaternion[] ikGoalRotations);
        extern private void SetHumanPose(ref Vector3 bodyPosition, ref Quaternion bodyRotation, float[] muscles);

        [ThreadSafe]
        extern private void GetInternalHumanPose(out Vector3 bodyPosition, out Quaternion bodyRotation, [Out] float[] muscles, [Out] Vector3[] ikGoalPositions, [Out] Quaternion[] ikGoalRotation);

        [ThreadSafe]
        extern private void SetInternalHumanPose(ref Vector3 bodyPosition, ref Quaternion bodyRotation, float[] muscles);

        [ThreadSafe]
        extern private unsafe void GetInternalAvatarPose(void* avatarPose, int avatarPoseLength);

        [ThreadSafe]
        extern private unsafe void SetInternalAvatarPose(void* avatarPose, int avatarPoseLength);

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

            m_Ptr = Internal_CreateFromRoot(avatar, root);
        }

        public HumanPoseHandler(Avatar avatar, string[] jointPaths)
        {
            m_Ptr = IntPtr.Zero;

            if (jointPaths == null)
                throw new ArgumentNullException("HumanPoseHandler jointPaths array is null");

            if (avatar == null)
                throw new ArgumentNullException("HumanPoseHandler avatar is null");

            if (!avatar.isValid)
                throw new ArgumentException("HumanPoseHandler avatar is invalid");

            if (!avatar.isHuman)
                throw new ArgumentException("HumanPoseHandler avatar is not human");

            m_Ptr = Internal_CreateFromJointPaths(avatar, jointPaths);
        }

        static void CalculateIKOffsets(in Quaternion[] sourceRotations, ref Quaternion[] destRotations)
        {
            for (int i = 0; i < 4; i++)
                destRotations[i] = sourceRotations[i] * HumanPose.s_IKGoalOffsets[i];
        }

        public void GetHumanPose(ref HumanPose humanPose)
        {
            if (m_Ptr == IntPtr.Zero)
                throw new NullReferenceException("HumanPoseHandler is not initialized properly");

            humanPose.Init();
            GetHumanPose(out humanPose.bodyPosition, out humanPose.bodyRotation, humanPose.muscles, humanPose.m_IkGoalPositions, humanPose.m_IkGoalRotations);
            CalculateIKOffsets(humanPose.m_IkGoalRotations, ref humanPose.m_OffsetIkGoalRotations); 
        }

        public void SetHumanPose(ref HumanPose humanPose)
        {
            if (m_Ptr == IntPtr.Zero)
                throw new NullReferenceException("HumanPoseHandler is not initialized properly");

            humanPose.Init();
            SetHumanPose(ref humanPose.bodyPosition, ref humanPose.bodyRotation, humanPose.muscles);
        }

        public void GetInternalHumanPose(ref HumanPose humanPose)
        {
            if (m_Ptr == IntPtr.Zero)
                throw new NullReferenceException("HumanPoseHandler is not initialized properly");

            humanPose.Init();
            GetInternalHumanPose(out humanPose.bodyPosition, out humanPose.bodyRotation, humanPose.muscles, humanPose.m_IkGoalPositions, humanPose.m_IkGoalRotations);
            CalculateIKOffsets(humanPose.m_IkGoalRotations, ref humanPose.m_OffsetIkGoalRotations);
        }

        public void SetInternalHumanPose(ref HumanPose humanPose)
        {
            if (m_Ptr == IntPtr.Zero)
                throw new NullReferenceException("HumanPoseHandler is not initialized properly");

            humanPose.Init();
            SetInternalHumanPose(ref humanPose.bodyPosition, ref humanPose.bodyRotation, humanPose.muscles);
        }

        public unsafe void GetInternalAvatarPose(NativeArray<float> avatarPose)
        {
            if (m_Ptr == IntPtr.Zero)
                throw new NullReferenceException("HumanPoseHandler is not initialized properly");

            GetInternalAvatarPose(avatarPose.GetUnsafePtr(), avatarPose.Length);
        }

        public unsafe void SetInternalAvatarPose(NativeArray<float> avatarPose)
        {
            if (m_Ptr == IntPtr.Zero)
                throw new NullReferenceException("HumanPoseHandler is not initialized properly");

            SetInternalAvatarPose(avatarPose.GetUnsafeReadOnlyPtr(), avatarPose.Length);
        }
        
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HumanPoseHandler humanPoseHandler) => humanPoseHandler.m_Ptr;
        }
    }
}
