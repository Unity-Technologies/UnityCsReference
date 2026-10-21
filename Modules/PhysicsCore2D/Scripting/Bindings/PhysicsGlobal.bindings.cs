// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsCore2D::Global_ReadProjectSettings")] extern internal static void PhysicsGlobal_ReadProjectSettings();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetPhysicsCoreSettings")] extern internal static UnityEngine.Object PhysicsGlobal_GetPhysicsCoreSettings();

        [NativeMethod(Name = "PhysicsCore2D::Global_GetObject", IsThreadSafe = true)] extern internal static UnityEngine.Object PhysicsGlobal_GetObject(EntityId entityId);
        [NativeMethod(Name = "PhysicsCore2D::Global_GetDefaultWorld", IsThreadSafe = true)] extern internal static PhysicsWorld PhysicsGlobal_GetDefaultWorld();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetAllocatedWorldCapacity")] extern internal static int PhysicsGlobal_GetAllocatedWorldCapacity();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetSafetyLocksEnabled")] extern internal static bool PhysicsGlobal_GetSafetyLocksEnabled();
        [NativeMethod(Name = "PhysicsCore2D::Global_SetSafetyLocksEnabled")] extern internal static void PhysicsGlobal_SetSafetyLocksEnabled(bool flag);
        [NativeMethod(Name = "PhysicsCore2D::Global_GetDisableSimulation")]extern internal static bool PhysicsGlobal_GetDisableSimulation();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetAlwaysDrawWorlds")] extern internal static bool PhysicsGlobal_GetAlwaysDrawWorlds();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetConcurrentSimulations")] extern internal static int PhysicsGlobal_GetConcurrentSimulations();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetUsePhysicsLayers")] extern internal static bool PhysicsGlobal_GetUsePhysicsLayers();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetPhysicsLayerNames")] extern internal static object PhysicsGlobal_GetPhysicsLayers();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetLengthUnitsPerMeter")] extern internal static float PhysicsGlobal_GetLengthUnitsPerMeter();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetTransformChangeMode")] extern internal static PhysicsWorld.TransformChangeMode PhysicsGlobal_GetTransformChangeMode();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetRenderingMode")] extern internal static PhysicsWorld.RenderingMode PhysicsGlobal_GetRenderingMode();
        [NativeMethod(Name = "PhysicsCore2D::Global_IsRenderingAllowed")] extern internal static bool PhysicsGlobal_IsRenderingAllowed();
        [NativeMethod(Name = "PhysicsCore2D::Global_CalculateWorldTransformWrite")] extern internal static PhysicsBuffer PhysicsGlobal_CalculateWorldTransformWrite(PhysicsWorld world, PhysicsWorld.TransformPlane transformPlane, PhysicsWorld.TransformPlaneCustom transformPlaneCustom, PhysicsWorld.TransformWriteMode transformWriteMode, IntPtr transformAccessArrayIntPtr);
        [NativeMethod(Name = "PhysicsCore2D::Global_CheckTransformChanges")] extern internal static int PhysicsGlobal_CheckTransformChanges();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetContactFilterMode")] extern internal static PhysicsShape.ContactFilterMode PhysicsGlobal_GetContactFilterMode();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetContactFilterGroupMode")] extern internal static PhysicsShape.ContactFilterGroupMode PhysicsGlobal_GetContactFilterGroupMode();
    }
}
