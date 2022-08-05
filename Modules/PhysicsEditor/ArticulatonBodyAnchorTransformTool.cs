// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor
{
    [EditorTool("Articulation Body Anchor Transform Tool", typeof(ArticulationBody))]
    class ArticulationBodyAnchorTransformTool : EditorTool
    {
        protected static class Styles
        {
            public static readonly GUIContent toolbarIcon = new GUIContent(
                EditorGUIUtility.IconContent("AnchorTransformTool").image,
                L10n.Tr("Edit the anchor transforms of this Articulation Body"));
        }

        public override GUIContent toolbarIcon => Styles.toolbarIcon;

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var obj in targets)
            {
                ArticulationBody body = obj as ArticulationBody;

                if (body == null || body.isRoot)
                    continue;

                ArticulationBody parentBody = ArticulationBodyEditorCommon.FindEnabledParentArticulationBody(body);

                {
                    Vector3 localAnchorT = body.anchorPosition;
                    Quaternion localAnchorR = body.anchorRotation;

                    EditorGUI.BeginChangeCheck();

                    DisplayProperAnchorHandle(body, ref localAnchorT, ref localAnchorR);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Changing Articulation body anchor position/rotation");
                        body.anchorPosition = localAnchorT;
                        body.anchorRotation = localAnchorR;
                    }
                }

                if (!body.matchAnchors)
                {
                    Vector3 localAnchorT = body.parentAnchorPosition;
                    Quaternion localAnchorR = body.parentAnchorRotation;

                    EditorGUI.BeginChangeCheck();

                    DisplayProperAnchorHandle(parentBody, ref localAnchorT, ref localAnchorR);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Changing Articulation body parent anchor position/rotation");
                        body.parentAnchorPosition = localAnchorT;
                        body.parentAnchorRotation = localAnchorR;
                    }
                }
            }
        }

        private void DisplayProperAnchorHandle(ArticulationBody body, ref Vector3 anchorPos, ref Quaternion anchorRot)
        {
            // Anchors are relative to the body here, and that includes scale.
            // However, we don't want to pass scale to DrawingScope - because that transforms the gizmos themselves.
            // For that reason, we add and remove scale to the position manually when drawing.
            Vector3 lossyScale = body.transform.lossyScale;
            Vector3 inverseScale = new Vector3(1 / lossyScale.x, 1 / lossyScale.y, 1 / lossyScale.z);
            var bodySpace = Matrix4x4.TRS(body.transform.position, body.transform.rotation, Vector3.one);
            using (new Handles.DrawingScope(bodySpace))
            {
                Vector3 scaledPos = Vector3.Scale(anchorPos, lossyScale);
                anchorRot = Handles.RotationHandle(anchorRot, scaledPos);
                // We use the scaled vector to position the gizmo, but we need to unscale it before applying the position when editing
                anchorPos = Vector3.Scale(Handles.PositionHandle(scaledPos, anchorRot), inverseScale);
            }
        }
    }
}
