// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Animations
{
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationHumanStream.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationHumanStream.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationHumanStream
    {
        private System.IntPtr stream;

        public bool isValid
        {
            get { return stream != System.IntPtr.Zero; }
        }

        private void ThrowIfInvalid()
        {
            if (!isValid)
                throw new InvalidOperationException("The AnimationHumanStream is invalid.");
        }

        public float humanScale { get { ThrowIfInvalid(); return GetHumanScale(); } }

        public float leftFootHeight { get { ThrowIfInvalid(); return GetFootHeight(true); } }

        public float rightFootHeight { get { ThrowIfInvalid(); return GetFootHeight(false); } }

        public Vector3 bodyLocalPosition
        {
            get { ThrowIfInvalid(); return InternalGetBodyLocalPosition(); }
            set { ThrowIfInvalid(); InternalSetBodyLocalPosition(value); }
        }

        public Quaternion bodyLocalRotation
        {
            get { ThrowIfInvalid(); return InternalGetBodyLocalRotation(); }
            set { ThrowIfInvalid(); InternalSetBodyLocalRotation(value); }
        }

        public Vector3 bodyPosition
        {
            get { ThrowIfInvalid(); return InternalGetBodyPosition(); }
            set { ThrowIfInvalid(); InternalSetBodyPosition(value); }
        }

        public Quaternion bodyRotation
        {
            get { ThrowIfInvalid(); return InternalGetBodyRotation(); }
            set { ThrowIfInvalid(); InternalSetBodyRotation(value); }
        }

        public float GetMuscle(MuscleHandle muscle) { ThrowIfInvalid(); return InternalGetMuscle(muscle); }
        public void  SetMuscle(MuscleHandle muscle, float value) { ThrowIfInvalid(); InternalSetMuscle(muscle, value); }

        public Vector3 leftFootVelocity  { get { ThrowIfInvalid(); return GetLeftFootVelocity(); } }
        public Vector3 rightFootVelocity { get { ThrowIfInvalid(); return GetRightFootVelocity(); } }

        // IK goals
        public void ResetToStancePose() { ThrowIfInvalid(); InternalResetToStancePose(); }

        public Vector3 GetGoalPositionFromPose(AvatarIKGoal index) { ThrowIfInvalid(); return InternalGetGoalPositionFromPose(index); }
        public Quaternion GetGoalRotationFromPose(AvatarIKGoal index) { ThrowIfInvalid(); return InternalGetGoalRotationFromPose(index); }

        public Vector3 GetGoalLocalPosition(AvatarIKGoal index)                 { ThrowIfInvalid(); return InternalGetGoalLocalPosition(index); }
        public void SetGoalLocalPosition(AvatarIKGoal index,  Vector3 pos)      { ThrowIfInvalid(); InternalSetGoalLocalPosition(index, pos); }
        public Quaternion GetGoalLocalRotation(AvatarIKGoal index)              { ThrowIfInvalid(); return InternalGetGoalLocalRotation(index); }
        public void SetGoalLocalRotation(AvatarIKGoal index, Quaternion rot)    { ThrowIfInvalid(); InternalSetGoalLocalRotation(index, rot); }
        public Vector3 GetGoalPosition(AvatarIKGoal index)                      { ThrowIfInvalid(); return InternalGetGoalPosition(index); }
        public void SetGoalPosition(AvatarIKGoal index,  Vector3 pos)           { ThrowIfInvalid(); InternalSetGoalPosition(index, pos); }
        public Quaternion GetGoalRotation(AvatarIKGoal index)                   { ThrowIfInvalid(); return InternalGetGoalRotation(index); }
        public void SetGoalRotation(AvatarIKGoal index, Quaternion rot)         { ThrowIfInvalid(); InternalSetGoalRotation(index, rot); }
        public void SetGoalWeightPosition(AvatarIKGoal index, float value)      { ThrowIfInvalid(); InternalSetGoalWeightPosition(index, value); }
        public void SetGoalWeightRotation(AvatarIKGoal index, float value)      { ThrowIfInvalid(); InternalSetGoalWeightRotation(index, value); }
        public float GetGoalWeightPosition(AvatarIKGoal index)                  { ThrowIfInvalid(); return InternalGetGoalWeightPosition(index); }
        public float GetGoalWeightRotation(AvatarIKGoal index)                  { ThrowIfInvalid(); return InternalGetGoalWeightRotation(index); }

        // IK Hints
        public Vector3 GetHintPosition(AvatarIKHint index)                      { ThrowIfInvalid(); return InternalGetHintPosition(index); }
        public void SetHintPosition(AvatarIKHint index,  Vector3 pos)           { ThrowIfInvalid(); InternalSetHintPosition(index, pos); }
        public void SetHintWeightPosition(AvatarIKHint index, float value)      { ThrowIfInvalid(); InternalSetHintWeightPosition(index, value); }
        public float GetHintWeightPosition(AvatarIKHint index)                  { ThrowIfInvalid(); return InternalGetHintWeightPosition(index); }

        // Lookat
        public void SetLookAtPosition(Vector3 lookAtPosition)                   { ThrowIfInvalid(); InternalSetLookAtPosition(lookAtPosition); }
        public void SetLookAtClampWeight(float weight)                          { ThrowIfInvalid(); InternalSetLookAtClampWeight(weight); }
        public void SetLookAtBodyWeight(float weight)                           { ThrowIfInvalid(); InternalSetLookAtBodyWeight(weight); }
        public void SetLookAtHeadWeight(float weight)                           { ThrowIfInvalid(); InternalSetLookAtHeadWeight(weight); }
        public void SetLookAtEyesWeight(float weight)                           { ThrowIfInvalid(); InternalSetLookAtEyesWeight(weight); }
        public void SolveIK()                                                   { ThrowIfInvalid(); InternalSolveIK(); }

        [NativeMethod(IsThreadSafe = true)]
        private extern float GetHumanScale();

        [NativeMethod(IsThreadSafe = true)]
        private extern float GetFootHeight(bool left);

        [NativeMethod(Name = "ResetToStancePose", IsThreadSafe = true)]
        private extern void InternalResetToStancePose();

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetGoalPositionFromPose", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 InternalGetGoalPositionFromPose(AvatarIKGoal index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetGoalRotationFromPose", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion InternalGetGoalRotationFromPose(AvatarIKGoal index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetBodyLocalPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 InternalGetBodyLocalPosition();
        [NativeMethod(Name = "AnimationHumanStreamBindings::SetBodyLocalPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetBodyLocalPosition(Vector3 value);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetBodyLocalRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion InternalGetBodyLocalRotation();

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetBodyLocalRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetBodyLocalRotation(Quaternion value);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetBodyPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 InternalGetBodyPosition();

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetBodyPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetBodyPosition(Vector3 value);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetBodyRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion InternalGetBodyRotation();

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetBodyRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetBodyRotation(Quaternion value);

        [NativeMethod(Name = "GetMuscle", IsThreadSafe = true)]
        private extern float InternalGetMuscle(MuscleHandle muscle);

        [NativeMethod(Name = "SetMuscle", IsThreadSafe = true)]
        private extern void InternalSetMuscle(MuscleHandle muscle, float value);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetLeftFootVelocity", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetLeftFootVelocity();

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetRightFootVelocity", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetRightFootVelocity();

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetGoalLocalPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 InternalGetGoalLocalPosition(AvatarIKGoal index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetGoalLocalPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetGoalLocalPosition(AvatarIKGoal index, Vector3 pos);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetGoalLocalRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion InternalGetGoalLocalRotation(AvatarIKGoal index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetGoalLocalRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetGoalLocalRotation(AvatarIKGoal index, Quaternion rot);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetGoalPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 InternalGetGoalPosition(AvatarIKGoal index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetGoalPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetGoalPosition(AvatarIKGoal index, Vector3 pos);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetGoalRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion InternalGetGoalRotation(AvatarIKGoal index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetGoalRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetGoalRotation(AvatarIKGoal index, Quaternion rot);

        [NativeMethod(Name = "SetGoalWeightPosition", IsThreadSafe = true)]
        private extern void InternalSetGoalWeightPosition(AvatarIKGoal index, float value);

        [NativeMethod(Name = "SetGoalWeightRotation", IsThreadSafe = true)]
        private extern void InternalSetGoalWeightRotation(AvatarIKGoal index, float value);

        [NativeMethod(Name = "GetGoalWeightPosition", IsThreadSafe = true)]
        private extern float InternalGetGoalWeightPosition(AvatarIKGoal index);

        [NativeMethod(Name = "GetGoalWeightRotation", IsThreadSafe = true)]
        private extern float InternalGetGoalWeightRotation(AvatarIKGoal index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::GetHintPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 InternalGetHintPosition(AvatarIKHint index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetHintPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetHintPosition(AvatarIKHint index, Vector3 pos);

        [NativeMethod(Name = "SetHintWeightPosition", IsThreadSafe = true)]
        private extern void InternalSetHintWeightPosition(AvatarIKHint index, float value);

        [NativeMethod(Name = "GetHintWeightPosition", IsThreadSafe = true)]
        private extern float InternalGetHintWeightPosition(AvatarIKHint index);

        [NativeMethod(Name = "AnimationHumanStreamBindings::SetLookAtPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void InternalSetLookAtPosition(Vector3 lookAtPosition);

        [NativeMethod(Name = "SetLookAtClampWeight", IsThreadSafe = true)]
        private extern void InternalSetLookAtClampWeight(float weight);

        [NativeMethod(Name = "SetLookAtBodyWeight", IsThreadSafe = true)]
        private extern void InternalSetLookAtBodyWeight(float weight);

        [NativeMethod(Name = "SetLookAtHeadWeight", IsThreadSafe = true)]
        private extern void InternalSetLookAtHeadWeight(float weight);

        [NativeMethod(Name = "SetLookAtEyesWeight", IsThreadSafe = true)]
        private extern void InternalSetLookAtEyesWeight(float weight);

        [NativeMethod(Name = "SolveIK", IsThreadSafe = true)]
        private extern void InternalSolveIK();
    }
}
