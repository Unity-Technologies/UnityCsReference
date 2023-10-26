// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public partial struct RaycastCommand
    {
        [Obsolete("This struct signature is no longer supported. Use struct with a QueryParameters instead", false)]
        public RaycastCommand(Vector3 from, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers, int maxHits = 1)
        {
            this.from = from;
            this.direction = direction;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = QueryParameters.Default;
            this.distance = distance;
            this.layerMask = layerMask;
        }
        [Obsolete("This struct signature is no longer supported. Use struct with a QueryParameters instead", false)]
        public RaycastCommand(PhysicsScene physicsScene, Vector3 from, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers, int maxHits = 1)
        {
            this.from = from;
            this.direction = direction;
            this.physicsScene = physicsScene;
            this.queryParameters = QueryParameters.Default;
            this.distance = distance;
            this.layerMask = layerMask;
        }
        [Obsolete("maxHits property was moved to be a part of RaycastCommand.ScheduleBatch.", false)]
        public int maxHits { get { return 1; } set {} }
        [Obsolete("Layer Mask is now a part of QueryParameters struct", false)]
        public int layerMask { get { return queryParameters.layerMask; } set { queryParameters.layerMask = value; }}
    }

    public partial struct SpherecastCommand
    {
        [Obsolete("This struct signature is no longer supported. Use struct with a QueryParameters instead", false)]
        public SpherecastCommand(Vector3 origin, float radius, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.origin = origin;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = QueryParameters.Default;
            this.layerMask = layerMask;
        }
        [Obsolete("This struct signature is no longer supported. Use struct with a QueryParameters instead", false)]
        public SpherecastCommand(PhysicsScene physicsScene, Vector3 origin,  float radius, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.origin = origin;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = QueryParameters.Default;
            this.layerMask = layerMask;
        }

        [Obsolete("Layer Mask is now a part of QueryParameters struct", false)]
        public int layerMask { get { return queryParameters.layerMask; } set { queryParameters.layerMask = value; }}
    }

    public partial struct CapsulecastCommand
    {
        [Obsolete("This struct signature is no longer supported. Use struct with a QueryParameters instead", false)]
        public CapsulecastCommand(Vector3 p1, Vector3 p2, float radius, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.point1 = p1;
            this.point2 = p2;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = QueryParameters.Default;
            this.layerMask = layerMask;
        }
        [Obsolete("This struct signature is no longer supported. Use struct with a QueryParameters instead", false)]
        public CapsulecastCommand(PhysicsScene physicsScene, Vector3 p1, Vector3 p2, float radius, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.point1 = p1;
            this.point2 = p2;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = QueryParameters.Default;
            this.layerMask = layerMask;
        }

        [Obsolete("Layer Mask is now a part of QueryParameters struct", false)]
        public int layerMask { get { return queryParameters.layerMask; } set { queryParameters.layerMask = value; }}
    }

    public partial struct BoxcastCommand
    {
        [Obsolete("This struct signature is no longer supported. Use struct with a QueryParameters instead", false)]
        public BoxcastCommand(Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.direction = direction;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = QueryParameters.Default;
            this.layerMask = layerMask;
        }
        [Obsolete("This struct signature is no longer supported. Use struct with a QueryParameters instead", false)]
        public BoxcastCommand(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.direction = direction;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = QueryParameters.Default;
            this.layerMask = layerMask;
        }

        [Obsolete("Layer Mask is now a part of QueryParameters struct", false)]
        public int layerMask { get { return queryParameters.layerMask; } set { queryParameters.layerMask = value; }}
    }
}
