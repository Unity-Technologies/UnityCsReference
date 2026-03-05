// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(HingeJoint2D))]
    [CanEditMultipleObjects]
    internal class HingeJoint2DEditor : AnchoredJoint2DEditor
    {
        private JointAngularLimitHandle2D m_AngularLimitHandle = new JointAngularLimitHandle2D();

        public new void OnEnable()
        {
            base.OnEnable();

            m_AngularLimitHandle.handleColor = Color.white;
            m_AngularLimitHandle.range = new Vector2(-1e+6f, 1e+6f);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(!System.Array.Exists(targets, x => (x as HingeJoint2D).gameObject.activeInHierarchy)))
            {
                EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Angular Limits"), this);
                GUILayout.Space(5);
            }

            base.OnInspectorGUI();
        }

        public new void OnSceneGUI()
        {
            if (!target)
                return;

            base.OnSceneGUI();

            if (target is HingeJoint2D hinge)
                if (hinge.isActiveAndEnabled)
                    HingeJoint2DTool.DrawHandle(false, hinge, m_AngularLimitHandle);
        }
    }
}
