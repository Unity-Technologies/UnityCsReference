// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;

namespace UnityEngine.LowLevelPhysics2D
{
    internal static partial class PhysicsLowLevelScripting2D
    {
        [NativeMethod(Name = "PhysicsJoint::Destroy", IsThreadSafe = true)] extern internal static bool PhysicsJoint_Destroy(PhysicsJoint joint, int ownerKey);
        [NativeMethod(Name = "PhysicsJoint::DestroyBatch", IsThreadSafe = true)] extern internal static void PhysicsJoint_DestroyBatch(ReadOnlySpan<PhysicsJoint> joints);
        [NativeMethod(Name = "PhysicsJoint::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsJoint_IsValid(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::GetWorld", IsThreadSafe = true)] extern internal static PhysicsWorld PhysicsJoint_GetWorld(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::GetJointType", IsThreadSafe = true)] extern internal static PhysicsJoint.JointType PhysicsJoint_GetJointType(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::GetBodyA", IsThreadSafe = true)] extern internal static PhysicsBody PhysicsJoint_GetBodyA(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::GetBodyB", IsThreadSafe = true)] extern internal static PhysicsBody PhysicsJoint_GetBodyB(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetLocalAnchorA", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetLocalAnchorA(PhysicsJoint joint, PhysicsTransform localAnchor);
        [NativeMethod(Name = "PhysicsJoint::GetLocalAnchorA", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsJoint_GetLocalAnchorA(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetLocalAnchorB", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetLocalAnchorB(PhysicsJoint joint, PhysicsTransform localAnchor);
        [NativeMethod(Name = "PhysicsJoint::GetLocalAnchorB", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsJoint_GetLocalAnchorB(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetForceThreshold", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetForceThreshold(PhysicsJoint joint, float forceThreshold);
        [NativeMethod(Name = "PhysicsJoint::GetForceThreshold", IsThreadSafe = true)] extern internal static float PhysicsJoint_GetForceThreshold(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetTorqueThreshold", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetTorqueThreshold(PhysicsJoint joint, float torqueThreshold);
        [NativeMethod(Name = "PhysicsJoint::GetTorqueThreshold", IsThreadSafe = true)] extern internal static float PhysicsJoint_GetTorqueThreshold(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetCollideConnected", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetCollideConnected(PhysicsJoint joint, bool shouldCollide);
        [NativeMethod(Name = "PhysicsJoint::GetCollideConnected", IsThreadSafe = true)] extern internal static bool PhysicsJoint_GetCollideConnected(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetTuningFrequency", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetTuningFrequency(PhysicsJoint joint, float tuningFrequency);
        [NativeMethod(Name = "PhysicsJoint::GetTuningFrequency", IsThreadSafe = true)] extern internal static float PhysicsJoint_GetTuningFrequency(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetTuningDamping", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetTuningDamping(PhysicsJoint joint, float tuningDamping);
        [NativeMethod(Name = "PhysicsJoint::GetTuningDamping", IsThreadSafe = true)] extern internal static float PhysicsJoint_GetTuningDamping(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetDrawScale", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetDrawScale(PhysicsJoint joint, float drawScale);
        [NativeMethod(Name = "PhysicsJoint::GetDrawScale", IsThreadSafe = true)] extern internal static float PhysicsJoint_GetDrawScale(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::WakeBodies", IsThreadSafe = true)] extern internal static void PhysicsJoint_WakeBodies(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::GetCurrentConstraintForce", IsThreadSafe = true)] extern internal static Vector2 PhysicsJoint_GetCurrentConstraintForce(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::GetCurrentConstraintTorque", IsThreadSafe = true)] extern internal static float PhysicsJoint_GetCurrentConstraintTorque(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::GetCurrentLinearSeparation", IsThreadSafe = true)] extern internal static float PhysicsJoint_GetCurrentLinearSeparation(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::GetCurrentAngularSeparation", IsThreadSafe = true)] extern internal static float PhysicsJoint_GetCurrentAngularSeparation(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::Draw", IsThreadSafe = true)] extern internal static void PhysicsJoint_Draw(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetOwner", IsThreadSafe = true)] extern internal static int PhysicsJoint_SetOwner(PhysicsJoint joint, Object ownerObject);
        [NativeMethod(Name = "PhysicsJoint::GetOwner", IsThreadSafe = true)] extern internal static Object PhysicsJoint_GetOwner(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::IsOwned", IsThreadSafe = true)] extern internal static bool PhysicsJoint_IsOwned(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetCallbackTarget", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetCallbackTarget(PhysicsJoint joint, System.Object callbackTarget);
        [NativeMethod(Name = "PhysicsJoint::GetCallbackTarget", IsThreadSafe = true)] extern internal static System.Object PhysicsJoint_GetCallbackTarget(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetUserData", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetUserData(PhysicsJoint joint, PhysicsUserData physicsUserData);
        [NativeMethod(Name = "PhysicsJoint::GetUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsJoint_GetUserData(PhysicsJoint joint);
        [NativeMethod(Name = "PhysicsJoint::SetOwnerUserData", IsThreadSafe = true)] extern internal static void PhysicsJoint_SetOwnerUserData(PhysicsJoint joint, PhysicsUserData physicsUserData, int ownerKey);
        [NativeMethod(Name = "PhysicsJoint::GetOwnerUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsJoint_GetOwnerUserData(PhysicsJoint joint);

        // PhysicsDistanceJoint.
        [NativeMethod(Name = "PhysicsDistanceJoint::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsDistanceJointDefinition DistanceJoint_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsDistanceJoint::Create", IsThreadSafe = true)] extern internal static PhysicsDistanceJoint DistanceJoint_Create(PhysicsWorld world, PhysicsDistanceJointDefinition definition);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetDistance", IsThreadSafe = true)] extern internal static void DistanceJoint_SetDistance(PhysicsDistanceJoint joint, float distance);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetDistance", IsThreadSafe = true)] extern internal static float DistanceJoint_GetDistance(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetCurrentDistance", IsThreadSafe = true)] extern internal static float DistanceJoint_GetCurrentDistance(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetEnableSpring", IsThreadSafe = true)] extern internal static void DistanceJoint_SetEnableSpring(PhysicsDistanceJoint joint, bool enableSpring);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetEnableSpring", IsThreadSafe = true)] extern internal static bool DistanceJoint_GetEnableSpring(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetSpringLowerForce", IsThreadSafe = true)] extern internal static void DistanceJoint_SetSpringLowerForce(PhysicsDistanceJoint joint, float springLowerForce);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetSpringLowerForce", IsThreadSafe = true)] extern internal static float DistanceJoint_GetSpringLowerForce(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetSpringUpperForce", IsThreadSafe = true)] extern internal static void DistanceJoint_SetSpringUpperForce(PhysicsDistanceJoint joint, float springUpperForce);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetSpringUpperForce", IsThreadSafe = true)] extern internal static float DistanceJoint_GetSpringUpperForce(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetSpringFrequency", IsThreadSafe = true)] extern internal static void DistanceJoint_SetSpringFrequency(PhysicsDistanceJoint joint, float springFrequency);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetSpringFrequency", IsThreadSafe = true)] extern internal static float DistanceJoint_GetSpringFrequency(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetSpringDamping", IsThreadSafe = true)] extern internal static void DistanceJoint_SetSpringDamping(PhysicsDistanceJoint joint, float springDamping);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetSpringDamping", IsThreadSafe = true)] extern internal static float DistanceJoint_GetSpringDamping(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetEnableMotor", IsThreadSafe = true)] extern internal static void DistanceJoint_SetEnableMotor(PhysicsDistanceJoint joint, bool enableMotor);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetEnableMotor", IsThreadSafe = true)] extern internal static bool DistanceJoint_GetEnableMotor(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetMotorSpeed", IsThreadSafe = true)] extern internal static void DistanceJoint_SetMotorSpeed(PhysicsDistanceJoint joint, float motorSpeed);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetMotorSpeed", IsThreadSafe = true)] extern internal static float DistanceJoint_GetMotorSpeed(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetMaxMotorForce", IsThreadSafe = true)] extern internal static void DistanceJoint_SetMaxMotorForce(PhysicsDistanceJoint joint, float maxMotorForce);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetMaxMotorForce", IsThreadSafe = true)] extern internal static float DistanceJoint_GetMaxMotorForce(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetCurrentMotorForce", IsThreadSafe = true)] extern internal static float DistanceJoint_GetCurrentMotorForce(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetEnableLimit", IsThreadSafe = true)] extern internal static void DistanceJoint_SetEnableLimit(PhysicsDistanceJoint joint, bool enableLimit);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetEnableLimit", IsThreadSafe = true)] extern internal static bool DistanceJoint_GetEnableLimit(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetMinDistanceLimit", IsThreadSafe = true)] extern internal static void DistanceJoint_SetMinDistanceLimit(PhysicsDistanceJoint joint, float minDistanceLimit);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetMinDistanceLimit", IsThreadSafe = true)] extern internal static float DistanceJoint_GetMinDistanceLimit(PhysicsDistanceJoint joint);
        [NativeMethod(Name = "PhysicsDistanceJoint::SetMaxDistanceLimit", IsThreadSafe = true)] extern internal static void DistanceJoint_SetMaxDistanceLimit(PhysicsDistanceJoint joint, float maxDistanceLimit);
        [NativeMethod(Name = "PhysicsDistanceJoint::GetMaxDistanceLimit", IsThreadSafe = true)] extern internal static float DistanceJoint_GetMaxDistanceLimit(PhysicsDistanceJoint joint);

        // PhysicsRelativeJoint.
        [NativeMethod(Name = "PhysicsRelativeJoint::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsRelativeJointDefinition RelativeJoint_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsRelativeJoint::Create", IsThreadSafe = true)] extern internal static PhysicsRelativeJoint RelativeJoint_Create(PhysicsWorld world, PhysicsRelativeJointDefinition definition);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetLinearVelocity", IsThreadSafe = true)] extern internal static void RelativeJoint_SetLinearVelocity(PhysicsRelativeJoint joint, Vector2 linearVelocity);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetLinearVelocity", IsThreadSafe = true)] extern internal static Vector2 RelativeJoint_GetLinearVelocity(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetAngularVelocity", IsThreadSafe = true)] extern internal static void RelativeJoint_SetAngularVelocity(PhysicsRelativeJoint joint, float angularVelocity);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetAngularVelocity", IsThreadSafe = true)] extern internal static float RelativeJoint_GetAngularVelocity(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetMaxForce", IsThreadSafe = true)] extern internal static void RelativeJoint_SetMaxForce(PhysicsRelativeJoint joint, float maxForce);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetMaxForce", IsThreadSafe = true)] extern internal static float RelativeJoint_GetMaxForce(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetMaxTorque", IsThreadSafe = true)] extern internal static void RelativeJoint_SetMaxTorque(PhysicsRelativeJoint joint, float maxTorque);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetMaxTorque", IsThreadSafe = true)] extern internal static float RelativeJoint_GetMaxTorque(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetSpringLinearFrequency", IsThreadSafe = true)] extern internal static void RelativeJoint_SetSpringLinearFrequency(PhysicsRelativeJoint joint, float springLinearFrequency);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetSpringLinearFrequency", IsThreadSafe = true)] extern internal static float RelativeJoint_GetSpringLinearFrequency(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetSpringAngularFrequency", IsThreadSafe = true)] extern internal static void RelativeJoint_SetSpringAngularFrequency(PhysicsRelativeJoint joint, float springAngularFrequency);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetSpringAngularFrequency", IsThreadSafe = true)] extern internal static float RelativeJoint_GetSpringAngularFrequency(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetSpringLinearDamping", IsThreadSafe = true)] extern internal static void RelativeJoint_SetSpringLinearDamping(PhysicsRelativeJoint joint, float springLinearDamping);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetSpringLinearDamping", IsThreadSafe = true)] extern internal static float RelativeJoint_GetSpringLinearDamping(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetSpringAngularDamping", IsThreadSafe = true)] extern internal static void RelativeJoint_SetSpringAngularDamping(PhysicsRelativeJoint joint, float springAngularDamping);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetSpringAngularDamping", IsThreadSafe = true)] extern internal static float RelativeJoint_GetSpringAngularDamping(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetSpringMaxForce", IsThreadSafe = true)] extern internal static void RelativeJoint_SetSpringMaxForce(PhysicsRelativeJoint joint, float springMaxForce);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetSpringMaxForce", IsThreadSafe = true)] extern internal static float RelativeJoint_GetSpringMaxForce(PhysicsRelativeJoint joint);
        [NativeMethod(Name = "PhysicsRelativeJoint::SetSpringMaxTorque", IsThreadSafe = true)] extern internal static void RelativeJoint_SetSpringMaxTorque(PhysicsRelativeJoint joint, float springMaxTorque);
        [NativeMethod(Name = "PhysicsRelativeJoint::GetSpringMaxTorque", IsThreadSafe = true)] extern internal static float RelativeJoint_GetSpringMaxTorque(PhysicsRelativeJoint joint);

        // PhysicsIgnoreJoint.
        [NativeMethod(Name = "PhysicsIgnoreJoint::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsIgnoreJointDefinition IgnorePhysicsJoint_GetDefaultDefinition();
        [NativeMethod(Name = "PhysicsIgnoreJoint::Create", IsThreadSafe = true)] extern internal static PhysicsIgnoreJoint IgnorePhysicsJoint_Create(PhysicsWorld world, PhysicsIgnoreJointDefinition definition);

        // PhysicsSliderJoint.
        [NativeMethod(Name = "PhysicsSliderJoint::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsSliderJointDefinition SliderJoint_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsSliderJoint::Create", IsThreadSafe = true)] extern internal static PhysicsSliderJoint SliderJoint_Create(PhysicsWorld world, PhysicsSliderJointDefinition definition);
        [NativeMethod(Name = "PhysicsSliderJoint::SetEnableSpring", IsThreadSafe = true)] extern internal static void SliderJoint_SetEnableSpring(PhysicsSliderJoint joint, bool enableSpring);
        [NativeMethod(Name = "PhysicsSliderJoint::GetEnableSpring", IsThreadSafe = true)] extern internal static bool SliderJoint_GetEnableSpring(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetSpringFrequency", IsThreadSafe = true)] extern internal static void SliderJoint_SetSpringFrequency(PhysicsSliderJoint joint, float springFrequency);
        [NativeMethod(Name = "PhysicsSliderJoint::GetSpringFrequency", IsThreadSafe = true)] extern internal static float SliderJoint_GetSpringFrequency(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetSpringDamping", IsThreadSafe = true)] extern internal static void SliderJoint_SetSpringDamping(PhysicsSliderJoint joint, float damping);
        [NativeMethod(Name = "PhysicsSliderJoint::GetSpringDamping", IsThreadSafe = true)] extern internal static float SliderJoint_GetSpringDamping(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetSpringTargetTranslation", IsThreadSafe = true)] extern internal static void SliderJoint_SetSpringTargetTranslation(PhysicsSliderJoint joint, float targetTranslation);
        [NativeMethod(Name = "PhysicsSliderJoint::GetSpringTargetTranslation", IsThreadSafe = true)] extern internal static float SliderJoint_GetSpringTargetTranslation(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetEnableMotor", IsThreadSafe = true)] extern internal static void SliderJoint_SetEnableMotor(PhysicsSliderJoint joint, bool enableMotor);
        [NativeMethod(Name = "PhysicsSliderJoint::GetEnableMotor", IsThreadSafe = true)] extern internal static bool SliderJoint_GetEnableMotor(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetMotorSpeed", IsThreadSafe = true)] extern internal static void SliderJoint_SetMotorSpeed(PhysicsSliderJoint joint, float motorSpeed);
        [NativeMethod(Name = "PhysicsSliderJoint::GetMotorSpeed", IsThreadSafe = true)] extern internal static float SliderJoint_GetMotorSpeed(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetMaxMotorForce", IsThreadSafe = true)] extern internal static void SliderJoint_SetMaxMotorForce(PhysicsSliderJoint joint, float force);
        [NativeMethod(Name = "PhysicsSliderJoint::GetMaxMotorForce", IsThreadSafe = true)] extern internal static float SliderJoint_GetMaxMotorForce(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::GetCurrentMotorForce", IsThreadSafe = true)] extern internal static float SliderJoint_GetCurrentMotorForce(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::GetCurrentTranslation", IsThreadSafe = true)] extern internal static float SliderJoint_GetCurrentTranslation(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::GetCurrentSpeed", IsThreadSafe = true)] extern internal static float SliderJoint_GetCurrentSpeed(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetEnableLimit", IsThreadSafe = true)] extern internal static void SliderJoint_SetEnableLimit(PhysicsSliderJoint joint, bool enableLimit);
        [NativeMethod(Name = "PhysicsSliderJoint::GetEnableLimit", IsThreadSafe = true)] extern internal static bool SliderJoint_GetEnableLimit(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetLowerTranslationLimit", IsThreadSafe = true)] extern internal static void SliderJoint_SetLowerTranslationLimit(PhysicsSliderJoint joint, float lowerTranslationLimit);
        [NativeMethod(Name = "PhysicsSliderJoint::GetLowerTranslationLimit", IsThreadSafe = true)] extern internal static float SliderJoint_GetLowerTranslationLimit(PhysicsSliderJoint joint);
        [NativeMethod(Name = "PhysicsSliderJoint::SetUpperTranslationLimit", IsThreadSafe = true)] extern internal static void SliderJoint_SetUpperTranslationLimit(PhysicsSliderJoint joint, float upperTranslationLimit);
        [NativeMethod(Name = "PhysicsSliderJoint::GetUpperTranslationLimit", IsThreadSafe = true)] extern internal static float SliderJoint_GetUpperTranslationLimit(PhysicsSliderJoint joint);

        // PhysicsHingeJoint.
        [NativeMethod(Name = "PhysicsHingeJoint::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsHingeJointDefinition HingeJoint_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsHingeJoint::Create", IsThreadSafe = true)] extern internal static PhysicsHingeJoint HingeJoint_Create(PhysicsWorld world, PhysicsHingeJointDefinition definition);
        [NativeMethod(Name = "PhysicsHingeJoint::SetEnableSpring", IsThreadSafe = true)] extern internal static void HingeJoint_SetEnableSpring(PhysicsHingeJoint joint, bool enableSpring);
        [NativeMethod(Name = "PhysicsHingeJoint::GetEnableSpring", IsThreadSafe = true)] extern internal static bool HingeJoint_GetEnableSpring(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetSpringFrequency", IsThreadSafe = true)] extern internal static void HingeJoint_SetSpringFrequency(PhysicsHingeJoint joint, float springFrequency);
        [NativeMethod(Name = "PhysicsHingeJoint::GetSpringFrequency", IsThreadSafe = true)] extern internal static float HingeJoint_GetSpringFrequency(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetSpringDamping", IsThreadSafe = true)] extern internal static void HingeJoint_SetSpringDamping(PhysicsHingeJoint joint, float damping);
        [NativeMethod(Name = "PhysicsHingeJoint::GetSpringDamping", IsThreadSafe = true)] extern internal static float HingeJoint_GetSpringDamping(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetSpringTargetAngle", IsThreadSafe = true)] extern internal static void HingeJoint_SetSpringTargetAngle(PhysicsHingeJoint joint, float targetAngle);
        [NativeMethod(Name = "PhysicsHingeJoint::GetSpringTargetAngle", IsThreadSafe = true)] extern internal static float HingeJoint_GetSpringTargetAngle(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::GetAngle", IsThreadSafe = true)] extern internal static float HingeJoint_GetAngle(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetEnableMotor", IsThreadSafe = true)] extern internal static void HingeJoint_SetEnableMotor(PhysicsHingeJoint joint, bool enableMotor);
        [NativeMethod(Name = "PhysicsHingeJoint::GetEnableMotor", IsThreadSafe = true)] extern internal static bool HingeJoint_GetEnableMotor(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetMotorSpeed", IsThreadSafe = true)] extern internal static void HingeJoint_SetMotorSpeed(PhysicsHingeJoint joint, float motorSpeed);
        [NativeMethod(Name = "PhysicsHingeJoint::GetMotorSpeed", IsThreadSafe = true)] extern internal static float HingeJoint_GetMotorSpeed(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetMaxMotorTorque", IsThreadSafe = true)] extern internal static void HingeJoint_SetMaxMotorTorque(PhysicsHingeJoint joint, float torque);
        [NativeMethod(Name = "PhysicsHingeJoint::GetMaxMotorTorque", IsThreadSafe = true)] extern internal static float HingeJoint_GetMaxMotorTorque(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::GetCurrentMotorTorque", IsThreadSafe = true)] extern internal static float HingeJoint_GetCurrentMotorTorque(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetEnableLimit", IsThreadSafe = true)] extern internal static void HingeJoint_SetEnableLimit(PhysicsHingeJoint joint, bool enableLimit);
        [NativeMethod(Name = "PhysicsHingeJoint::GetEnableLimit", IsThreadSafe = true)] extern internal static bool HingeJoint_GetEnableLimit(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetLowerAngleLimit", IsThreadSafe = true)] extern internal static void HingeJoint_SetLowerLimit(PhysicsHingeJoint joint, float lowerAngleLimit);
        [NativeMethod(Name = "PhysicsHingeJoint::GetLowerAngleLimit", IsThreadSafe = true)] extern internal static float HingeJoint_GetLowerLimit(PhysicsHingeJoint joint);
        [NativeMethod(Name = "PhysicsHingeJoint::SetUpperAngleLimit", IsThreadSafe = true)] extern internal static void HingeJoint_SetUpperLimit(PhysicsHingeJoint joint, float upperAngleLimit);
        [NativeMethod(Name = "PhysicsHingeJoint::GetUpperAngleLimit", IsThreadSafe = true)] extern internal static float HingeJoint_GetUpperLimit(PhysicsHingeJoint joint);

        // PhysicsFixedJoint.
        [NativeMethod(Name = "PhysicsFixedJoint::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsFixedJointDefinition FixedJoint_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsFixedJoint::Create", IsThreadSafe = true)] extern internal static PhysicsFixedJoint FixedJoint_Create(PhysicsWorld world, PhysicsFixedJointDefinition definition);
        [NativeMethod(Name = "PhysicsFixedJoint::SetLinearFrequency", IsThreadSafe = true)] extern internal static void FixedJoint_SetLinearFrequency(PhysicsFixedJoint joint, float linearFrequency);
        [NativeMethod(Name = "PhysicsFixedJoint::GetLinearFrequency", IsThreadSafe = true)] extern internal static float FixedJoint_GetLinearFrequency(PhysicsFixedJoint joint);
        [NativeMethod(Name = "PhysicsFixedJoint::SetLinearDamping", IsThreadSafe = true)] extern internal static void FixedJoint_SetLinearDamping(PhysicsFixedJoint joint, float damping);
        [NativeMethod(Name = "PhysicsFixedJoint::GetLinearDamping", IsThreadSafe = true)] extern internal static float FixedJoint_GetLinearDamping(PhysicsFixedJoint joint);
        [NativeMethod(Name = "PhysicsFixedJoint::SetAngularFrequency", IsThreadSafe = true)] extern internal static void FixedJoint_SetAngularFrequency(PhysicsFixedJoint joint, float angularFrequency);
        [NativeMethod(Name = "PhysicsFixedJoint::GetAngularFrequency", IsThreadSafe = true)] extern internal static float FixedJoint_GetAngularFrequency(PhysicsFixedJoint joint);
        [NativeMethod(Name = "PhysicsFixedJoint::SetAngularDamping", IsThreadSafe = true)] extern internal static void FixedJoint_SetAngularDamping(PhysicsFixedJoint joint, float damping);
        [NativeMethod(Name = "PhysicsFixedJoint::GetAngularDamping", IsThreadSafe = true)] extern internal static float FixedJoint_GetAngularDamping(PhysicsFixedJoint joint);

        // PhysicsWheelJoint.
        [NativeMethod(Name = "PhysicsWheelJoint::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsWheelJointDefinition WheelJoint_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsWheelJoint::Create", IsThreadSafe = true)] extern internal static PhysicsWheelJoint WheelJoint_Create(PhysicsWorld world, PhysicsWheelJointDefinition definition);
        [NativeMethod(Name = "PhysicsWheelJoint::SetEnableSpring", IsThreadSafe = true)] extern internal static void WheelJoint_SetEnableSpring(PhysicsWheelJoint joint, bool enableSpring);
        [NativeMethod(Name = "PhysicsWheelJoint::GetEnableSpring", IsThreadSafe = true)] extern internal static bool WheelJoint_GetEnableSpring(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::SetSpringFrequency", IsThreadSafe = true)] extern internal static void WheelJoint_SetSpringFrequency(PhysicsWheelJoint joint, float springFrequency);
        [NativeMethod(Name = "PhysicsWheelJoint::GetSpringFrequency", IsThreadSafe = true)] extern internal static float WheelJoint_GetSpringFrequency(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::SetSpringDamping", IsThreadSafe = true)] extern internal static void WheelJoint_SetSpringDamping(PhysicsWheelJoint joint, float damping);
        [NativeMethod(Name = "PhysicsWheelJoint::GetSpringDamping", IsThreadSafe = true)] extern internal static float WheelJoint_GetSpringDamping(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::SetEnableMotor", IsThreadSafe = true)] extern internal static void WheelJoint_SetEnableMotor(PhysicsWheelJoint joint, bool enableMotor);
        [NativeMethod(Name = "PhysicsWheelJoint::GetEnableMotor", IsThreadSafe = true)] extern internal static bool WheelJoint_GetEnableMotor(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::SetMotorSpeed", IsThreadSafe = true)] extern internal static void WheelJoint_SetMotorSpeed(PhysicsWheelJoint joint, float motorSpeed);
        [NativeMethod(Name = "PhysicsWheelJoint::GetMotorSpeed", IsThreadSafe = true)] extern internal static float WheelJoint_GetMotorSpeed(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::SetMaxMotorTorque", IsThreadSafe = true)] extern internal static void WheelJoint_SetMaxMotorTorque(PhysicsWheelJoint joint, float torque);
        [NativeMethod(Name = "PhysicsWheelJoint::GetMaxMotorTorque", IsThreadSafe = true)] extern internal static float WheelJoint_GetMaxMotorTorque(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::GetCurrentMotorTorque", IsThreadSafe = true)] extern internal static float WheelJoint_GetCurrentMotorTorque(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::SetEnableLimit", IsThreadSafe = true)] extern internal static void WheelJoint_SetEnableLimit(PhysicsWheelJoint joint, bool enableLimit);
        [NativeMethod(Name = "PhysicsWheelJoint::GetEnableLimit", IsThreadSafe = true)] extern internal static bool WheelJoint_GetEnableLimit(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::SetLowerTranslationLimit", IsThreadSafe = true)] extern internal static void WheelJoint_SetLowerTranslationLimit(PhysicsWheelJoint joint, float lowerTranslationLimit);
        [NativeMethod(Name = "PhysicsWheelJoint::GetLowerTranslationLimit", IsThreadSafe = true)] extern internal static float WheelJoint_GetLowerTranslationLimit(PhysicsWheelJoint joint);
        [NativeMethod(Name = "PhysicsWheelJoint::SetUpperTranslationLimit", IsThreadSafe = true)] extern internal static void WheelJoint_SetUpperTranslationLimit(PhysicsWheelJoint joint, float upperTranslationLimit);
        [NativeMethod(Name = "PhysicsWheelJoint::GetUpperTranslationLimit", IsThreadSafe = true)] extern internal static float WheelJoint_GetUpperTranslationLimit(PhysicsWheelJoint joint);
    }
}
