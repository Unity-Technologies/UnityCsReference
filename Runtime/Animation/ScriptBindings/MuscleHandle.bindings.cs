// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Animations
{
    [NativeHeader("Runtime/Animation/Animator.h")] // -> dof enum
    [NativeHeader("Runtime/Animation/MuscleHandle.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct MuscleHandle
    {
        public HumanPartDof humanPartDof
        {
            get;
            private set;
        }
        public int dof
        {
            get;
            private set;
        }

        public MuscleHandle(BodyDof bodyDof)
        {
            humanPartDof = HumanPartDof.Body;
            dof = (int)bodyDof;
        }

        public MuscleHandle(HeadDof headDof)
        {
            humanPartDof = HumanPartDof.Head;
            dof = (int)headDof;
        }

        public MuscleHandle(HumanPartDof partDof, LegDof legDof)
        {
            if (partDof != HumanPartDof.LeftLeg && partDof != HumanPartDof.RightLeg)
                throw new InvalidOperationException("Invalid HumanPartDof for a leg, please use either HumanPartDof.LeftLeg or HumanPartDof.RightLeg.");

            humanPartDof = partDof;
            dof = (int)legDof;
        }

        public MuscleHandle(HumanPartDof partDof, ArmDof armDof)
        {
            if (partDof != HumanPartDof.LeftArm && partDof != HumanPartDof.RightArm)
                throw new InvalidOperationException("Invalid HumanPartDof for an arm, please use either HumanPartDof.LeftArm or HumanPartDof.RightArm.");

            humanPartDof = partDof;
            dof = (int)armDof;
        }

        public MuscleHandle(HumanPartDof partDof, FingerDof fingerDof)
        {
            if (partDof < HumanPartDof.LeftThumb || partDof > HumanPartDof.RightLittle)
                throw new InvalidOperationException("Invalid HumanPartDof for a finger.");

            humanPartDof = partDof;
            dof = (int)fingerDof;
        }

        public string name
        {
            get { return GetName(); }
        }

        public static int muscleHandleCount
        {
            get { return GetMuscleHandleCount(); }
        }

        public extern static void GetMuscleHandles([NotNull][Out] MuscleHandle[] muscleHandles);

        private extern string GetName();

        private extern static int GetMuscleHandleCount();
    }
}

