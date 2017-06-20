// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CharacterJoint)), CanEditMultipleObjects]
    class CharacterJointEditor : JointEditor<CharacterJoint>
    {
        protected override void DoAngularLimitHandles(CharacterJoint joint)
        {
            base.DoAngularLimitHandles(joint);

            angularLimitHandle.xMotion = ConfigurableJointMotion.Limited;
            angularLimitHandle.yMotion = ConfigurableJointMotion.Limited;
            angularLimitHandle.zMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit limit;

            limit = joint.lowTwistLimit;
            angularLimitHandle.xMin = limit.limit;

            limit = joint.highTwistLimit;
            angularLimitHandle.xMax = limit.limit;

            limit = joint.swing1Limit;
            angularLimitHandle.yMax = limit.limit;
            angularLimitHandle.yMin = -limit.limit;

            limit = joint.swing2Limit;
            angularLimitHandle.zMax = limit.limit;
            angularLimitHandle.zMin = -limit.limit;

            EditorGUI.BeginChangeCheck();

            angularLimitHandle.radius = GetAngularLimitHandleSize(Vector3.zero);
            angularLimitHandle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(joint, Styles.editAngularLimitsUndoMessage);

                limit = joint.lowTwistLimit;
                limit.limit = angularLimitHandle.xMin;
                joint.lowTwistLimit = limit;

                limit = joint.highTwistLimit;
                limit.limit = angularLimitHandle.xMax;
                joint.highTwistLimit = limit;

                limit = joint.swing1Limit;
                limit.limit = angularLimitHandle.yMax == limit.limit ? -angularLimitHandle.yMin : angularLimitHandle.yMax;
                joint.swing1Limit = limit;

                limit = joint.swing2Limit;
                limit.limit = angularLimitHandle.zMax == limit.limit ? -angularLimitHandle.zMin : angularLimitHandle.zMax;
                joint.swing2Limit = limit;
            }
        }
    }
}
