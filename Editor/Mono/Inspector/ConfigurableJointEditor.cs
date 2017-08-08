// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(ConfigurableJoint)), CanEditMultipleObjects]
    class ConfigurableJointEditor : JointEditor<ConfigurableJoint>
    {
        protected override void GetActors(
            ConfigurableJoint joint,
            out Rigidbody dynamicActor,
            out Rigidbody connectedActor,
            out int jointFrameActorIndex,
            out bool rightHandedLimit
            )
        {
            base.GetActors(joint, out dynamicActor, out connectedActor, out jointFrameActorIndex, out rightHandedLimit);
            if (joint.swapBodies)
            {
                jointFrameActorIndex = 0;
                rightHandedLimit = true;
            }
        }

        protected override void DoAngularLimitHandles(ConfigurableJoint joint)
        {
            base.DoAngularLimitHandles(joint);

            angularLimitHandle.xMotion = joint.angularXMotion;
            angularLimitHandle.yMotion = joint.angularYMotion;
            angularLimitHandle.zMotion = joint.angularZMotion;

            SoftJointLimit limit;

            limit = joint.lowAngularXLimit;
            angularLimitHandle.xMin = limit.limit;

            limit = joint.highAngularXLimit;
            angularLimitHandle.xMax = limit.limit;

            limit = joint.angularYLimit;
            angularLimitHandle.yMax = limit.limit;
            angularLimitHandle.yMin = -limit.limit;

            limit = joint.angularZLimit;
            angularLimitHandle.zMax = limit.limit;
            angularLimitHandle.zMin = -limit.limit;

            EditorGUI.BeginChangeCheck();

            angularLimitHandle.radius = GetAngularLimitHandleSize(Vector3.zero);
            angularLimitHandle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(joint, Styles.editAngularLimitsUndoMessage);

                limit = joint.lowAngularXLimit;
                limit.limit = angularLimitHandle.xMin;
                joint.lowAngularXLimit = limit;

                limit = joint.highAngularXLimit;
                limit.limit = angularLimitHandle.xMax;
                joint.highAngularXLimit = limit;

                limit = joint.angularYLimit;
                limit.limit = angularLimitHandle.yMax == limit.limit ? -angularLimitHandle.yMin : angularLimitHandle.yMax;
                joint.angularYLimit = limit;

                limit = joint.angularZLimit;
                limit.limit = angularLimitHandle.zMax == limit.limit ? -angularLimitHandle.zMin : angularLimitHandle.zMax;
                joint.angularZLimit = limit;
            }
        }
    }
}
