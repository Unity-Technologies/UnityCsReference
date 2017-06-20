// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(HingeJoint)), CanEditMultipleObjects]
    class HingeJointEditor : JointEditor<HingeJoint>
    {
        private static readonly Matrix4x4 s_JointMatrixOffset =
            Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180f, Vector3.forward), Vector3.one);

        private static readonly GUIContent s_WarningMessage =
            EditorGUIUtility.TextContent("Min and max limits must be within the range [-180, 180].");

        private SerializedProperty m_MinLimit;
        private SerializedProperty m_MaxLimit;

        void OnEnable()
        {
            angularLimitHandle.yMotion = ConfigurableJointMotion.Locked;
            angularLimitHandle.zMotion = ConfigurableJointMotion.Locked;

            angularLimitHandle.yHandleColor = Color.clear;
            angularLimitHandle.zHandleColor = Color.clear;

            angularLimitHandle.xRange = new Vector2(-Physics.k_MaxFloatMinusEpsilon, Physics.k_MaxFloatMinusEpsilon);

            m_MinLimit = serializedObject.FindProperty("m_Limits.min");
            m_MaxLimit = serializedObject.FindProperty("m_Limits.max");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            float min = m_MinLimit.floatValue;
            float max = m_MaxLimit.floatValue;

            if (min < -180f || min > 180f || max < -180f || max > 180f)
                EditorGUILayout.HelpBox(s_WarningMessage.text, MessageType.Warning);
        }

        protected override Matrix4x4 GetHandleMatrix(HingeJoint joint)
        {
            return base.GetHandleMatrix(joint) * s_JointMatrixOffset;
        }

        protected override void DoAngularLimitHandles(HingeJoint joint)
        {
            base.DoAngularLimitHandles(joint);

            angularLimitHandle.xMotion = joint.useLimits ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;

            JointLimits limit;

            limit = joint.limits;
            angularLimitHandle.xMin = limit.min;
            angularLimitHandle.xMax = limit.max;

            EditorGUI.BeginChangeCheck();

            angularLimitHandle.radius = GetAngularLimitHandleSize(Vector3.zero);
            angularLimitHandle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(joint, Styles.editAngularLimitsUndoMessage);

                limit = joint.limits;
                limit.min = angularLimitHandle.xMin;
                limit.max = angularLimitHandle.xMax;
                joint.limits = limit;
            }
        }
    }
}
