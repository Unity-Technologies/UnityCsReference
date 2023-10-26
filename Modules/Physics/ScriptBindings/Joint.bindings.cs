// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/Physics/Joint.h")]
    [NativeClass("Unity::Joint")]
    public class Joint : Component
    {
        extern public Rigidbody connectedBody { get; set; }
        extern public ArticulationBody connectedArticulationBody { get; set; }
        extern public Vector3 axis { get; set; }
        extern public Vector3 anchor { get; set; }
        extern public Vector3 connectedAnchor { get; set; }
        extern public bool autoConfigureConnectedAnchor { get; set; }
        extern public float breakForce { get; set; }
        extern public float breakTorque { get; set; }
        extern public bool enableCollision { get; set; }
        extern public bool enablePreprocessing { get; set; }
        extern public float massScale { get; set; }
        extern public float connectedMassScale { get; set; }

        extern private void GetCurrentForces(ref Vector3 linearForce, ref Vector3 angularForce);

        public Vector3 currentForce
        {
            get
            {
                Vector3 force = Vector3.zero;
                Vector3 torque = Vector3.zero;
                GetCurrentForces(ref force, ref torque);
                return force;
            }
        }

        public Vector3 currentTorque
        {
            get
            {
                Vector3 force = Vector3.zero;
                Vector3 torque = Vector3.zero;
                GetCurrentForces(ref force, ref torque);
                return torque;
            }
        }

        extern internal Matrix4x4 GetLocalPoseMatrix(int bodyIndex);
    }
}
