// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Internal;

namespace UnityEngine
{
    public enum QueryTriggerInteraction
    {
        UseGlobal = 0,
        Ignore = 1,
        Collide = 2
    }
    public enum SimulationMode
    {
        FixedUpdate = 0,
        Update = 1,
        Script = 2
    }

    public enum SimulationStage : ushort
    {
        None = 0,
        PrepareSimulation = 1 << 0,
        RunSimulation = 1 << 1,
        PublishSimulationResults = 1 << 2,
        All = PrepareSimulation | RunSimulation | PublishSimulationResults
    }

    public enum SimulationOption : ushort
    {
        None = 0,
        SyncTransforms = 1 << 0,
        IgnoreEmptyScenes = 1 << 1,
        All = SyncTransforms | IgnoreEmptyScenes
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct IntegrationInfo
    {
        [Flags]
        internal enum SupportedUnityFeatures
        {
            None = 0,
            DynamicsSupport = 1 << 1,
            SDKVisualDebuggerSupport = 1 << 2,
            ArticulationSupport = 1 << 3,
            ImmediateModeSupport = 1 << 4,
            VehicleSupport = 1 << 5,
            CharacterControllerSupport = 1 << 6
        };

        internal const uint k_InvalidID = 0;
        internal const uint k_FallbackIntegrationId = 0xDECAFBAD;

        [FieldOffset(0)]
        readonly uint m_Id;
        [FieldOffset(4)]
        fixed ushort m_IntegrationVersion[3];
        [FieldOffset(10)]
        fixed ushort m_SdkVersion[3];
        [FieldOffset(16)]
        readonly SupportedUnityFeatures m_Features;
        [FieldOffset(20)]
        fixed byte m_Name[16];
        [FieldOffset(36)]
        fixed byte m_Desc[220];

        public readonly uint id => m_Id;

        public unsafe string name {
            get
            {
                fixed(byte* ptr = m_Name)
                    return Marshal.PtrToStringAnsi(new IntPtr(ptr));
            }
        }

        public unsafe string description
        {
            get
            {
                fixed (byte* ptr = m_Desc)
                    return Marshal.PtrToStringAnsi(new IntPtr(ptr));
            }
        }

        public bool isFallback => id == k_FallbackIntegrationId;
    }

    [NativeHeader("Modules/Physics/PhysicsQuery.h")]
    [NativeHeader("Modules/Physics/PhysicsManager.h")]
    [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
    public partial class Physics
    {
        //Matches kFloatMaxMinusEpsilon in PhysicsConstants.h; currently used in e.g., EnforceJointLimitsConsistency()
        internal const float k_MaxFloatMinusEpsilon = 340282326356119260000000000000000000000f;

        public const int IgnoreRaycastLayer = 1 << 2;
        public const int DefaultRaycastLayers = ~IgnoreRaycastLayer;
        public const int AllLayers = ~0;

        extern private unsafe static void GetIntegrationInfos(out IntPtr integrations, out ulong integrationCount);
        extern private unsafe static void GetCurrentIntegrationInfo(out IntPtr integration);

        internal static ReadOnlySpan<IntegrationInfo> GetIntegrationInfos()
        {
            unsafe
            {
                IntPtr integrations;
                ulong count;
                GetIntegrationInfos(out integrations, out count);

                return new ReadOnlySpan<IntegrationInfo>(integrations.ToPointer(), (int)count);
            }
        }

        public unsafe static IntegrationInfo GetCurrentIntegrationInfo()
        {
            IntPtr infoPtr;
            GetCurrentIntegrationInfo(out infoPtr);

            return *(IntegrationInfo*)infoPtr.ToPointer();
        }

        extern public static Vector3 gravity { [ThreadSafe] get; set; }
        extern public static float defaultContactOffset { get; set; }
        extern public static float sleepThreshold { get; set; }
        extern public static bool queriesHitTriggers { get; set; }
        extern public static bool queriesHitBackfaces { get; set; }
        extern public static float bounceThreshold { get; set; }
        extern public static float defaultMaxDepenetrationVelocity { get; set; }
        extern public static int defaultSolverIterations { get; set; }
        extern public static int defaultSolverVelocityIterations { get; set; }
        extern public static SimulationMode simulationMode { get; set; }

        extern static public float defaultMaxAngularSpeed { get; set; }
        extern static public bool improvedPatchFriction { get; set; }

        extern static public bool invokeCollisionCallbacks { get; set; }

        [NativeProperty("DefaultPhysicsSceneHandle", true, TargetType.Function, true)]
        extern public static PhysicsScene defaultPhysicsScene { get; }

        extern public static void IgnoreCollision([NotNull] Collider collider1, [NotNull] Collider collider2, [DefaultValue("true")] bool ignore);

        [ExcludeFromDocs]
        public static void IgnoreCollision(Collider collider1, Collider collider2)
        {
            IgnoreCollision(collider1, collider2, true);
        }

        [NativeName("IgnoreCollision")]
        extern public static void IgnoreLayerCollision(int layer1, int layer2, [DefaultValue("true")] bool ignore);

        [ExcludeFromDocs]
        public static void IgnoreLayerCollision(int layer1, int layer2)
        {
            IgnoreLayerCollision(layer1, layer2, true);
        }

        extern public static bool GetIgnoreLayerCollision(int layer1, int layer2);

        extern public static bool GetIgnoreCollision([NotNull] Collider collider1, [NotNull] Collider collider2);
        static public bool Raycast(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        // This is not actually called by native code, but needs the [RequiredByNativeCode]
        // attribute as it is called by reflection from GraphicsRaycaster.cs, to avoid a hard
        // dependency to this module.
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool Raycast(Ray ray, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool Raycast(Ray ray, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool Linecast(Vector3 start, Vector3 end, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dir = end - start;
            return defaultPhysicsScene.Raycast(start, dir, dir.magnitude, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, int layerMask)
        {
            return Linecast(start, end, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end)
        {
            return Linecast(start, end, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dir = end - start;
            return defaultPhysicsScene.Raycast(start, dir, out hitInfo, dir.magnitude, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask)
        {
            return Linecast(start, end, out hitInfo, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo)
        {
            return Linecast(start, end, out hitInfo, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hit;
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, out hit, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return CapsuleCast(point1, point2, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance)
        {
            return CapsuleCast(point1, point2, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction)
        {
            return CapsuleCast(point1, point2, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return SphereCast(origin, radius, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool SphereCast(Ray ray, float radius, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return SphereCast(ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, float maxDistance, int layerMask)
        {
            return SphereCast(ray, radius, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, float maxDistance)
        {
            return SphereCast(ray, radius, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius)
        {
            return SphereCast(ray, radius, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCast(ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo)
        {
            return SphereCast(ray, radius, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCast(center, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance)
        {
            return BoxCast(center, halfExtents, direction, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation)
        {
            return BoxCast(center, halfExtents, direction, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction)
        {
            return BoxCast(center, halfExtents, direction, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::RaycastAll")]
        extern static RaycastHit[] Internal_RaycastAll(PhysicsScene physicsScene, Ray ray, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);
                return Internal_RaycastAll(defaultPhysicsScene, ray, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            return RaycastAll(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return RaycastAll(origin, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction)
        {
            return RaycastAll(origin, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public RaycastHit[] RaycastAll(Ray ray, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        // This is not actually called by native code, but needs the [RequiredByNativeCode]
        // attribute as it is called by reflection from GraphicsRaycaster.cs, to avoid a hard
        // dependency to this module.
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray, float maxDistance)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray)
        {
            return RaycastAll(ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        // This is not actually called by native code, but needs the [RequiredByNativeCode]
        // attribute as it is called by reflection from GraphicsRaycaster.cs, to avoid a hard
        // dependency to this module.
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::CapsuleCastAll")]
        extern private static RaycastHit[] Query_CapsuleCastAll(PhysicsScene physicsScene, Vector3 p0, Vector3 p1, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_CapsuleCastAll(defaultPhysicsScene, point1, point2, radius, normalizedDirection, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction)
        {
            return CapsuleCastAll(point1, point2, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::SphereCastAll")]
        extern private static RaycastHit[] Query_SphereCastAll(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_SphereCastAll(defaultPhysicsScene, origin, radius, normalizedDirection, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction)
        {
            return SphereCastAll(origin, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public RaycastHit[] SphereCastAll(Ray ray, float radius, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCastAll(ray.origin, radius, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, int layerMask)
        {
            return SphereCastAll(ray, radius, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance)
        {
            return SphereCastAll(ray, radius, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius)
        {
            return SphereCastAll(ray, radius, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::OverlapCapsule")]
        extern private static Collider[] OverlapCapsule_Internal(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapCapsule_Internal(defaultPhysicsScene, point0, point1, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask)
        {
            return OverlapCapsule(point0, point1, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius)
        {
            return OverlapCapsule(point0, point1, radius, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::OverlapSphere")]
        extern private static Collider[] OverlapSphere_Internal(PhysicsScene physicsScene, Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static Collider[] OverlapSphere(Vector3 position, float radius, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapSphere_Internal(defaultPhysicsScene, position, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask)
        {
            return OverlapSphere(position, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapSphere(Vector3 position, float radius)
        {
            return OverlapSphere(position, radius, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("Simulate")]
        extern internal static void Simulate_Internal(PhysicsScene physicsScene, float step, SimulationStage stages, SimulationOption options);

        public static void Simulate(float step)
        {
            if (simulationMode != SimulationMode.Script)
            {
                Debug.LogWarning("Physics.Simulate(...) was called but simulation mode is not set to Script. You should set simulation mode to Script first before calling this function therefore the simulation was not run.");
                return;
            }

            Simulate_Internal(defaultPhysicsScene, step, SimulationStage.All, SimulationOption.All);
        }

        [NativeName("InterpolateBodies")]
        extern internal static void InterpolateBodies_Internal(PhysicsScene physicsScene);

        [NativeName("ResetInterpolatedTransformPosition")]
        extern internal static void ResetInterpolationPoses_Internal(PhysicsScene physicsScene);

        extern public static void SyncTransforms();
        extern public static bool autoSyncTransforms { get; set; }
        extern public static bool reuseCollisionCallbacks { get; set; }

        [FreeFunction("Physics::ComputePenetration")]
        extern private static bool Query_ComputePenetration([NotNull] Collider colliderA, Vector3 positionA, Quaternion rotationA, [NotNull] Collider colliderB, Vector3 positionB, Quaternion rotationB, ref Vector3 direction, ref float distance);

        public static bool ComputePenetration(Collider colliderA, Vector3 positionA, Quaternion rotationA, Collider colliderB, Vector3 positionB, Quaternion rotationB, out Vector3 direction, out float distance)
        {
            direction = Vector3.zero;
            distance = 0f;
            return Query_ComputePenetration(colliderA, positionA, rotationA, colliderB, positionB, rotationB, ref direction, ref distance);
        }

        [FreeFunction("Physics::ClosestPoint")]
        extern private static Vector3 Query_ClosestPoint([NotNull] Collider collider, Vector3 position, Quaternion rotation, Vector3 point);

        public static Vector3 ClosestPoint(Vector3 point, Collider collider, Vector3 position, Quaternion rotation)
        {
            return Query_ClosestPoint(collider, position, rotation, point);
        }

        [StaticAccessor("GetPhysicsManager()")]
        public extern static float interCollisionDistance {[NativeName("GetClothInterCollisionDistance")] get; [NativeName("SetClothInterCollisionDistance")] set; }

        [StaticAccessor("GetPhysicsManager()")]
        public extern static float interCollisionStiffness {[NativeName("GetClothInterCollisionStiffness")] get; [NativeName("SetClothInterCollisionStiffness")] set; }

        [StaticAccessor("GetPhysicsManager()")]
        public extern static bool interCollisionSettingsToggle {[NativeName("GetClothInterCollisionSettingsToggle")] get; [NativeName("SetClothInterCollisionSettingsToggle")] set; }

        extern public static Vector3 clothGravity { [ThreadSafe] get; set; }

        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapSphere(position, radius, results, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask)
        {
            return OverlapSphereNonAlloc(position, radius, results, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results)
        {
            return OverlapSphereNonAlloc(position, radius, results, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::SphereTest")]
        extern private static bool CheckSphere_Internal(PhysicsScene physicsScene, Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static bool CheckSphere(Vector3 position, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckSphere_Internal(defaultPhysicsScene, position, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckSphere(Vector3 position, float radius, int layerMask)
        {
            return CheckSphere(position, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckSphere(Vector3 position, float radius)
        {
            return CheckSphere(position, radius, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.SphereCast(origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCastNonAlloc(ray.origin, radius, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results)
        {
            return SphereCastNonAlloc(ray, radius, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::CapsuleTest")]
        extern private static bool CheckCapsule_Internal(PhysicsScene physicsScene, Vector3 start, Vector3 end, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckCapsule_Internal(defaultPhysicsScene, start, end, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask)
        {
            return CheckCapsule(start, end, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius)
        {
            return CheckCapsule(start, end, radius, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::BoxTest")]
        extern private static bool CheckBox_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layermask, QueryTriggerInteraction queryTriggerInteraction);
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("DefaultRaycastLayers")] int layermask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckBox_Internal(defaultPhysicsScene, center, halfExtents, orientation, layermask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
        {
            return CheckBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            return CheckBox(center, halfExtents, orientation, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents)
        {
            return CheckBox(center, halfExtents, Quaternion.identity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::OverlapBox")]
        extern private static Collider[] OverlapBox_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapBox_Internal(defaultPhysicsScene, center, halfExtents, orientation, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
        {
            return OverlapBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            return OverlapBox(center, halfExtents, orientation, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents)
        {
            return OverlapBox(center, halfExtents, Quaternion.identity, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("AllLayers")] int mask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapBox(center, halfExtents, results, orientation, mask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int mask)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, mask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::BoxCastAll")]
        private static extern RaycastHit[] Internal_BoxCastAll(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Internal_BoxCastAll(defaultPhysicsScene, center, halfExtents, normalizedDirection, orientation, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction)
        {
            return BoxCastAll(center, halfExtents, direction, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapCapsule(point0, point1, radius, results, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [StaticAccessor("GetPhysicsManager()")]
        [ThreadSafe]
        public static extern void BakeMesh(int meshID, bool convex, MeshColliderCookingOptions cookingOptions);

        public static void BakeMesh(int meshID, bool convex)
        {
            BakeMesh(meshID, convex, MeshColliderCookingOptions.CookForFasterSimulation |
                                     MeshColliderCookingOptions.EnableMeshCleaning |
                                     MeshColliderCookingOptions.WeldColocatedVertices |
                                     MeshColliderCookingOptions.UseFastMidphase);
        }

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern bool ConnectPhysicsSDKVisualDebugger();

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern void DisconnectPhysicsSDKVisualDebugger();

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        extern internal static Collider GetColliderByInstanceID(int instanceID);

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern Component GetBodyByInstanceID(int instanceID);

        [ThreadSafe]
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern uint TranslateTriangleIndexFromID(int instanceID, uint faceIndex);

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionEnter(Component component, Collision collision);
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionStay(Component component,  Collision collision);
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionExit(Component component,  Collision collision);
    }
}
