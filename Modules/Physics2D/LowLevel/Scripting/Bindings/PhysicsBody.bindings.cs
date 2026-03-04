// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using Unity.Collections;

using UnityEngine.Bindings;

namespace UnityEngine.LowLevelPhysics2D
{
    internal static partial class PhysicsLowLevelScripting2D
    {
        [NativeMethod(Name = "PhysicsBody::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsBodyDefinition PhysicsBody_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsBody::Create")] extern internal static PhysicsBody PhysicsBody_Create(PhysicsWorld world, PhysicsBodyDefinition definition);
        [NativeMethod(Name = "PhysicsBody::CreateBatch")] extern internal static PhysicsBuffer PhysicsBody_CreateBatch(PhysicsWorld world, ReadOnlySpan<PhysicsBodyDefinition> definitions, int bodyCount, Allocator allocator);
        [NativeMethod(Name = "PhysicsBody::Destroy")] extern internal static bool PhysicsBody_Destroy(PhysicsBody body, int ownerKey);
        [NativeMethod(Name = "PhysicsBody::DestroyBatch")] extern internal static void PhysicsBody_DestroyBatch(ReadOnlySpan<PhysicsBody> bodies);
        [NativeMethod(Name = "PhysicsBody::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsBody_IsValid(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetBatchVelocity", IsThreadSafe = true)] extern internal static void PhysicsBody_SetBatchVelocity(ReadOnlySpan<PhysicsBody.BatchVelocity> batch);
        [NativeMethod(Name = "PhysicsBody::SetBatchForce", IsThreadSafe = true)] extern internal static void PhysicsBody_SetBatchForce(ReadOnlySpan<PhysicsBody.BatchForce> batch);
        [NativeMethod(Name = "PhysicsBody::SetBatchImpulse", IsThreadSafe = true)] extern internal static void PhysicsBody_SetBatchImpulse(ReadOnlySpan<PhysicsBody.BatchImpulse> batch);
        [NativeMethod(Name = "PhysicsBody::SetBatchTransform", IsThreadSafe = true)] extern internal static void PhysicsBody_SetBatchTransform(ReadOnlySpan<PhysicsBody.BatchTransform> batch);
        [NativeMethod(Name = "PhysicsBody::WriteDefinition")] extern internal static void PhysicsBody_WriteDefinition(PhysicsBody body, PhysicsBodyDefinition definition, bool onlyExtendedProperties);
        [NativeMethod(Name = "PhysicsBody::ReadDefinition")] extern internal static PhysicsBodyDefinition PhysicsBody_ReadDefinition(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::GetWorld", IsThreadSafe = true)] extern internal static PhysicsWorld PhysicsBody_GetWorld(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::GetBodyType", IsThreadSafe = true)] extern internal static PhysicsBody.BodyType PhysicsBody_GetBodyType(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetBodyType")] extern internal static void PhysicsBody_SetBodyType(PhysicsBody body, PhysicsBody.BodyType type);
        [NativeMethod(Name = "PhysicsBody::SetBodyConstraints")] extern internal static void PhysicsBody_SetBodyConstraints(PhysicsBody body, PhysicsBody.BodyConstraints constraints);
        [NativeMethod(Name = "PhysicsBody::GetBodyConstraints", IsThreadSafe = true)] extern internal static PhysicsBody.BodyConstraints PhysicsBody_GetBodyConstraints(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::GetPosition", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetPosition(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetPosition", IsThreadSafe = true)] extern internal static void PhysicsBody_SetPosition(PhysicsBody body, Vector2 position);
        [NativeMethod(Name = "PhysicsBody::GetRotation", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsBody_GetRotation(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetRotation", IsThreadSafe = true)] extern internal static void PhysicsBody_SetRotation(PhysicsBody body, PhysicsRotate rotation);
        [NativeMethod(Name = "PhysicsBody::GetTransform", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsBody_GetTransform(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetTransform", IsThreadSafe = true)] extern internal static void PhysicsBody_SetTransform(PhysicsBody body, PhysicsTransform transform);
        [NativeMethod(Name = "PhysicsBody::SetTransformTarget", IsThreadSafe = true)] extern internal static void PhysicsBody_SetTransformTarget(PhysicsBody body, PhysicsTransform transform, float deltaTime);
        [NativeMethod(Name = "PhysicsBody::GetLocalPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetLocalPoint(PhysicsBody body, Vector2 worldPoint);
        [NativeMethod(Name = "PhysicsBody::GetWorldPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetWorldPoint(PhysicsBody body, Vector2 localPoint);
        [NativeMethod(Name = "PhysicsBody::GetLocalVector", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetLocalVector(PhysicsBody body, Vector2 worldVector);
        [NativeMethod(Name = "PhysicsBody::GetWorldVector", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetWorldVector(PhysicsBody body, Vector2 localVector);
        [NativeMethod(Name = "PhysicsBody::GetLocalPointVelocity", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetLocalPointVelocity(PhysicsBody body, Vector2 localPoint);
        [NativeMethod(Name = "PhysicsBody::GetWorldPointVelocity", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetWorldPointVelocity(PhysicsBody body, Vector2 worldPoint);
        [NativeMethod(Name = "PhysicsBody::GetLinearVelocity", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetLinearVelocity(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetLinearVelocity", IsThreadSafe = true)] extern internal static void PhysicsBody_SetLinearVelocity(PhysicsBody body, Vector2 linearVelocity);
        [NativeMethod(Name = "PhysicsBody::GetAngularVelocity", IsThreadSafe = true)] extern internal static float PhysicsBody_GetAngularVelocity(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetAngularVelocity", IsThreadSafe = true)] extern internal static void PhysicsBody_SetAngularVelocity(PhysicsBody body, float angularVelocity);
        [NativeMethod(Name = "PhysicsBody::GetMass", IsThreadSafe = true)] extern internal static float PhysicsBody_GetMass(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::GetRotationalInertia", IsThreadSafe = true)] extern internal static float PhysicsBody_GetRotationalInertia(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::GetLocalCenterOfMass", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetLocalCenterOfMass(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::GetWorldCenterOfMass", IsThreadSafe = true)] extern internal static Vector2 PhysicsBody_GetWorldCenterOfMass(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetMassConfiguration", IsThreadSafe = true)] extern internal static void PhysicsBody_SetMassConfiguration(PhysicsBody body, PhysicsBody.MassConfiguration massData);
        [NativeMethod(Name = "PhysicsBody::GetMassConfiguration", IsThreadSafe = true)] extern internal static PhysicsBody.MassConfiguration PhysicsBody_GetMassConfiguration(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::ApplyMassFromShapes", IsThreadSafe = true)] extern internal static void PhysicsBody_ApplyMassFromShapes(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetLinearDamping", IsThreadSafe = true)] extern internal static void PhysicsBody_SetLinearDamping(PhysicsBody body, float linearDamping);
        [NativeMethod(Name = "PhysicsBody::GetLinearDamping", IsThreadSafe = true)] extern internal static float PhysicsBody_GetLinearDamping(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetAngularDamping", IsThreadSafe = true)] extern internal static void PhysicsBody_SetAngularDamping(PhysicsBody body, float angularDamping);
        [NativeMethod(Name = "PhysicsBody::GetAngularDamping", IsThreadSafe = true)] extern internal static float PhysicsBody_GetAngularDamping(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetGravityScale", IsThreadSafe = true)] extern internal static void PhysicsBody_SetGravityScale(PhysicsBody body, float gravityScale);
        [NativeMethod(Name = "PhysicsBody::GetGravityScale", IsThreadSafe = true)] extern internal static float PhysicsBody_GetGravityScale(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetAwake", IsThreadSafe = true)] extern internal static void PhysicsBody_SetAwake(PhysicsBody body, bool flag);
        [NativeMethod(Name = "PhysicsBody::GetAwake", IsThreadSafe = true)] extern internal static bool PhysicsBody_GetAwake(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetSleepingAllowed", IsThreadSafe = true)] extern internal static void PhysicsBody_SetSleepingAllowed(PhysicsBody body, bool enableSleep);
        [NativeMethod(Name = "PhysicsBody::GetSleepingAllowed", IsThreadSafe = true)] extern internal static bool PhysicsBody_GetSleepingAllowed(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetSleepThreshold", IsThreadSafe = true)] extern internal static void PhysicsBody_SetSleepThreshold(PhysicsBody body, float threshold);
        [NativeMethod(Name = "PhysicsBody::GetSleepThreshold", IsThreadSafe = true)] extern internal static float PhysicsBody_GetSleepThreshold(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetEnabled")] extern internal static void PhysicsBody_SetEnabled(PhysicsBody body, bool flag);
        [NativeMethod(Name = "PhysicsBody::GetEnabled", IsThreadSafe = true)] extern internal static bool PhysicsBody_GetEnabled(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetFastRotationAllowed", IsThreadSafe = true)] extern internal static void PhysicsBody_SetFastRotationAllowed(PhysicsBody body, bool flag);
        [NativeMethod(Name = "PhysicsBody::GetFastRotationAllowed", IsThreadSafe = true)] extern internal static bool PhysicsBody_GetFastRotationAllowed(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetFastCollisionsAllowed")] extern internal static void PhysicsBody_SetFastCollisionsAllowed(PhysicsBody body, bool flag);
        [NativeMethod(Name = "PhysicsBody::GetFastCollisionsAllowed", IsThreadSafe = true)] extern internal static bool PhysicsBody_GetFastCollisionsAllowed(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::ApplyForce", IsThreadSafe = true)] extern internal static void PhysicsBody_ApplyForce(PhysicsBody body, Vector2 force, Vector2 point, bool wake);
        [NativeMethod(Name = "PhysicsBody::ApplyForceToCenter", IsThreadSafe = true)] extern internal static void PhysicsBody_ApplyForceToCenter(PhysicsBody body, Vector2 force, bool wake);
        [NativeMethod(Name = "PhysicsBody::ApplyTorque", IsThreadSafe = true)] extern internal static void PhysicsBody_ApplyTorque(PhysicsBody body, float torque, bool wake);
        [NativeMethod(Name = "PhysicsBody::ApplyLinearImpulse", IsThreadSafe = true)] extern internal static void PhysicsBody_ApplyLinearImpulse(PhysicsBody body, Vector2 impulse, Vector2 point, bool wake);
        [NativeMethod(Name = "PhysicsBody::ApplyLinearImpulseToCenter", IsThreadSafe = true)] extern internal static void PhysicsBody_ApplyLinearImpulseToCenter(PhysicsBody body, Vector2 impulse, bool wake);
        [NativeMethod(Name = "PhysicsBody::ApplyAngularImpulse", IsThreadSafe = true)] extern internal static void PhysicsBody_ApplyAngularImpulse(PhysicsBody body, float impulse, bool wake);
        [NativeMethod(Name = "PhysicsBody::ClearForces", IsThreadSafe = true)] extern internal static void PhysicsBody_ClearForces(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::WakeTouching", IsThreadSafe = true)] extern internal static void PhysicsBody_WakeTouching(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetContactEvents", IsThreadSafe = true)] extern internal static void PhysicsBody_SetContactEvents(PhysicsBody body, bool flag);
        [NativeMethod(Name = "PhysicsBody::SetHitEvents", IsThreadSafe = true)] extern internal static void PhysicsBody_SetHitEvents(PhysicsBody body, bool flag);
        [NativeMethod(Name = "PhysicsBody::GetShapeCount", IsThreadSafe = true)] extern internal static int PhysicsBody_GetShapeCount(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::GetShapes", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsBody_GetShapes(PhysicsBody PhysicsBody, Allocator allocator);
        [NativeMethod(Name = "PhysicsBody::GetJointCount", IsThreadSafe = true)] extern internal static int PhysicsBody_GetJointCount(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::GetJoints", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsBody_GetJoints(PhysicsBody body, Allocator allocator);
        [NativeMethod(Name = "PhysicsBody::GetContacts", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsBody_GetContacts(PhysicsBody body, Allocator allocator);
        [NativeMethod(Name = "PhysicsBody::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsBody_CalculateAABB(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::Draw", IsThreadSafe = true)] extern internal static void PhysicsBody_Draw(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetOwner", IsThreadSafe = true)] extern internal static int PhysicsBody_SetOwner(PhysicsBody body, Object ownerObject);
        [NativeMethod(Name = "PhysicsBody::GetOwner", IsThreadSafe = true)] extern internal static Object PhysicsBody_GetOwner(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::IsOwned", IsThreadSafe = true)] extern internal static bool PhysicsBody_IsOwned(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetCallbackTarget", IsThreadSafe = true)] extern internal static void PhysicsBody_SetCallbackTarget(PhysicsBody body, System.Object callbackTarget);
        [NativeMethod(Name = "PhysicsBody::GetCallbackTarget", IsThreadSafe = true)] extern internal static System.Object PhysicsBody_GetCallbackTarget(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetUserData", IsThreadSafe = true)] extern internal static void PhysicsBody_SetUserData(PhysicsBody body, PhysicsUserData physicsUserData);
        [NativeMethod(Name = "PhysicsBody::GetUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsBody_GetUserData(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetOwnerUserData", IsThreadSafe = true)] extern internal static void PhysicsBody_SetOwnerUserData(PhysicsBody body, PhysicsUserData physicsUserData, int ownerKey);
        [NativeMethod(Name = "PhysicsBody::GetOwnerUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsBody_GetOwnerUserData(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetTransformObject", IsThreadSafe = true)] extern internal static void PhysicsBody_SetTransformObject(PhysicsBody body, Transform transform);
        [NativeMethod(Name = "PhysicsBody::GetTransformObject", IsThreadSafe = true)] extern internal static Transform PhysicsBody_GetTransformObject(PhysicsBody body);
        [NativeMethod(Name = "PhysicsBody::SetTransformWriteMode", IsThreadSafe = true)] extern internal static void PhysicsBody_SetTransformWriteMode(PhysicsBody body, PhysicsBody.TransformWriteMode writeMode);
        [NativeMethod(Name = "PhysicsBody::GetTransformWriteMode", IsThreadSafe = true)] extern internal static PhysicsBody.TransformWriteMode PhysicsBody_GetTransformWriteMode(PhysicsBody body);
    }
}
