// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(HingeJoint2D))]
    [CanEditMultipleObjects]
    internal class HingeJoint2DEditor : AnchoredJoint2DEditor
    {
        private JointAngularLimitHandle2D m_AngularLimitHandle = new JointAngularLimitHandle2D();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(!targets.Any(x => (x as HingeJoint2D).gameObject.activeInHierarchy)))
            {
                EditorGUILayout.EditorToolbarForTarget(EditorGUIUtility.TrTempContent("Edit Angular Limits"), this);
                GUILayout.Space(5);
            }

            base.OnInspectorGUI();
        }
    }
}
