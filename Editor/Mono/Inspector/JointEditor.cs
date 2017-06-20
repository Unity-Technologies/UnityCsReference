// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    abstract class JointEditor<T> : Editor where T : Joint
    {
        protected static class Styles
        {
            public static readonly GUIContent editAngularLimitsButton = new GUIContent(EditorGUIUtility.IconContent("JointAngularLimits"));
            public static readonly string editAngularLimitsUndoMessage = EditorGUIUtility.TextContent("Change Joint Angular Limits").text;

            static Styles()
            {
                editAngularLimitsButton.tooltip = EditorGUIUtility.TextContent("Edit joint angular limits.").text;
            }
        }

        protected static float GetAngularLimitHandleSize(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position);
        }

        protected JointAngularLimitHandle angularLimitHandle { get { return m_AngularLimitHandle; } }
        private JointAngularLimitHandle m_AngularLimitHandle = new JointAngularLimitHandle();

        protected bool editingAngularLimits
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.JointAngularLimits && EditMode.IsOwner(this); }
        }

        public override void OnInspectorGUI()
        {
            DoInspectorEditButtons();
            base.OnInspectorGUI();
        }

        protected void DoInspectorEditButtons()
        {
            T joint = (T)target;
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.JointAngularLimits,
                "Edit Joint Angular Limits",
                Styles.editAngularLimitsButton,
                this
                );
        }

        internal override Bounds GetWorldBoundsOfTarget(UnityObject targetObject)
        {
            var bounds = base.GetWorldBoundsOfTarget(targetObject);
            // ensure joint's anchor point is included in bounds
            bounds.Encapsulate(GetHandleMatrix((T)targetObject).MultiplyPoint3x4(Vector3.zero));
            return bounds;
        }

        protected virtual void OnSceneGUI()
        {
            if (editingAngularLimits)
            {
                T joint = (T)target;
                EditorGUI.BeginChangeCheck();

                using (new Handles.DrawingScope(GetHandleMatrix(joint)))
                    DoAngularLimitHandles(joint);

                // wake up rigidbody in case current orientation is out of bounds of new limits
                if (EditorGUI.EndChangeCheck())
                    joint.GetComponent<Rigidbody>().WakeUp();
            }
        }

        protected virtual Matrix4x4 GetConnectedBodyMatrix(T joint)
        {
            return joint.connectedBody == null ? Matrix4x4.identity : joint.connectedBody.transform.localToWorldMatrix;
        }

        protected virtual Matrix4x4 GetHandleMatrix(T joint)
        {
            return GetConnectedBodyMatrix(joint) * joint.GetJointFrame();
        }

        protected virtual void DoAngularLimitHandles(T joint)
        {
        }
    }
}
