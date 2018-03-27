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
        public HumanPartDoF humanPartDoF
        {
            get;
            private set;
        }
        public int dof
        {
            get;
            private set;
        }

        public MuscleHandle(BodyDoF bodyDoF)
        {
            humanPartDoF = HumanPartDoF.Body;
            dof = (int)bodyDoF;
        }

        public MuscleHandle(HeadDoF headDoF)
        {
            humanPartDoF = HumanPartDoF.Head;
            dof = (int)headDoF;
        }

        public MuscleHandle(HumanPartDoF partDoF, BodyDoF bodyDoF)
        {
            if (partDoF != HumanPartDoF.Body)
                throw new InvalidOperationException("Invalid HumanPartDoF for body, please use HumanPartDoF.Body");
            humanPartDoF = HumanPartDoF.Body;
            dof = (int)bodyDoF;
        }

        public MuscleHandle(HumanPartDoF partDoF, HeadDoF headDoF)
        {
            if (partDoF != HumanPartDoF.Head)
                throw new InvalidOperationException("Invalid HumanPartDoF for head, please use HumanPartDoF.Head");

            humanPartDoF = HumanPartDoF.Head;
            dof = (int)headDoF;
        }

        public MuscleHandle(HumanPartDoF partDoF, LegDoF legDoF)
        {
            if (partDoF != HumanPartDoF.LeftLeg && partDoF != HumanPartDoF.RightLeg)
                throw new InvalidOperationException("Invalid HumanPartDoF for a leg, please use either HumanPartDoF.LeftLeg or HumanPartDoF.RightLeg.");

            humanPartDoF = partDoF;
            dof = (int)legDoF;
        }

        public MuscleHandle(HumanPartDoF partDoF, ArmDoF armDoF)
        {
            if (partDoF != HumanPartDoF.LeftArm && partDoF != HumanPartDoF.RightArm)
                throw new InvalidOperationException("Invalid HumanPartDoF for an arm, please use either HumanPartDoF.LeftArm or HumanPartDoF.RightArm.");

            humanPartDoF = partDoF;
            dof = (int)armDoF;
        }

        public MuscleHandle(HumanPartDoF partDoF, FingerDoF fingerDoF)
        {
            if (partDoF < HumanPartDoF.LeftThumb || partDoF > HumanPartDoF.RightLittle)
                throw new InvalidOperationException("Invalid HumanPartDoF for a finger.");

            humanPartDoF = partDoF;
            dof = (int)fingerDoF;
        }

        public string name
        {
            get { return GetName(); }
        }

        public static int muscleHandlesCount
        {
            get { return GetMuscleHandlesCount(); }
        }

        public extern static void GetMuscleHandles([NotNull][Out] MuscleHandle[] muscleHandles);

        private extern string GetName();

        private extern static int GetMuscleHandlesCount();
    }
}

