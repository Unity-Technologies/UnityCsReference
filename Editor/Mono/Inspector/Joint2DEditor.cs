// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Joint2D))]
    [CanEditMultipleObjects]
    internal class Joint2DEditor : Editor
    {
        SerializedProperty m_BreakForce;
        SerializedProperty m_BreakTorque;

        public void OnEnable()
        {
            m_BreakForce = serializedObject.FindProperty("m_BreakForce");
            m_BreakTorque = serializedObject.FindProperty("m_BreakTorque");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_BreakForce);

            // Distance/Spring/Target joints produce no reaction torque so they're not supported.
            var targetType = target.GetType();
            if (targetType != typeof(DistanceJoint2D) &&
                targetType != typeof(TargetJoint2D) &&
                targetType != typeof(SpringJoint2D))
                EditorGUILayout.PropertyField(m_BreakTorque);

            serializedObject.ApplyModifiedProperties();
        }

        public class Styles
        {
            public readonly GUIStyle anchor = "U2D.pivotDot";
            public readonly GUIStyle anchorActive = "U2D.pivotDotActive";
            public readonly GUIStyle connectedAnchor = "U2D.dragDot";
            public readonly GUIStyle connectedAnchorActive = "U2D.dragDotActive";
        }
        protected static Styles s_Styles;

        protected bool HandleAnchor(ref Vector3 position, bool isConnectedAnchor)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            var drawCapFunction = isConnectedAnchor ? (Handles.CapFunction)ConnectedAnchorHandleCap : (Handles.CapFunction)AnchorHandleCap;

            int id = target.GetInstanceID() + (isConnectedAnchor ? 1 : 0);

            EditorGUI.BeginChangeCheck();
            position = Handles.Slider2D(id, position, Vector3.back, Vector3.right, Vector3.up, 0, drawCapFunction, Vector2.zero);
            return EditorGUI.EndChangeCheck();
        }

        public static void AnchorHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (controlID == GUIUtility.keyboardControl)
                HandleCap(controlID, position, s_Styles.anchorActive, eventType);
            else
                HandleCap(controlID, position, s_Styles.anchor, eventType);
        }

        public static void ConnectedAnchorHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (controlID == GUIUtility.keyboardControl)
                HandleCap(controlID, position, s_Styles.connectedAnchorActive, eventType);
            else
                HandleCap(controlID, position, s_Styles.connectedAnchor, eventType);
        }

        static void HandleCap(int controlID, Vector3 position, GUIStyle guiStyle, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Layout:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToRectangleInternal(position, Quaternion.identity, Vector2.zero));
                    break;
                case EventType.Repaint:
                    Handles.BeginGUI();

                    position = HandleUtility.WorldToGUIPoint(position);
                    var w = guiStyle.fixedWidth;
                    var h = guiStyle.fixedHeight;
                    var r = new Rect(position.x - w / 2f, position.y - h / 2f, w, h);

                    guiStyle.Draw(r, GUIContent.none, controlID);
                    Handles.EndGUI();
                    break;
            }
        }

        public static void DrawAALine(Vector3 start, Vector3 end)
        {
            Handles.DrawAAPolyLine(new Vector3[] { start, end });
        }

        public static void DrawDistanceGizmo(Vector3 anchor, Vector3 connectedAnchor, float distance)
        {
            Vector3 direction = (anchor - connectedAnchor).normalized;
            Vector3 endPosition = connectedAnchor + (direction * distance);

            Vector3 normal = Vector3.Cross(direction, Vector3.forward);
            normal *= HandleUtility.GetHandleSize(connectedAnchor) * 0.16f;

            Handles.color = Color.green;

            DrawAALine(anchor, endPosition);
            DrawAALine(connectedAnchor + normal, connectedAnchor - normal);
            DrawAALine(endPosition + normal, endPosition - normal);
        }

        static Matrix4x4 GetAnchorSpaceMatrix(Transform transform)
        {
            // Anchor space transformation matrix
            return Matrix4x4.TRS(transform.position, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z), transform.lossyScale);
        }

        protected static Vector3 TransformPoint(Transform transform, Vector3 position)
        {
            // Local to World
            return GetAnchorSpaceMatrix(transform).MultiplyPoint(position);
        }

        protected static Vector3 InverseTransformPoint(Transform transform, Vector3 position)
        {
            // World to Local
            return GetAnchorSpaceMatrix(transform).inverse.MultiplyPoint(position);
        }

        protected static Vector3 SnapToSprite(SpriteRenderer spriteRenderer, Vector3 position, float snapDistance)
        {
            if (spriteRenderer == null)
                return position;

            snapDistance = HandleUtility.GetHandleSize(position) * snapDistance;

            var x = spriteRenderer.sprite.bounds.size.x / 2;
            var y = spriteRenderer.sprite.bounds.size.y / 2;
            var anchors = new[] {  new Vector2(-x, -y),   new Vector2(0, -y),    new Vector2(x, -y),
                                   new Vector2(-x, 0),    new Vector2(0, 0),     new Vector2(x, 0),
                                   new Vector2(-x, y),    new Vector2(0, y),     new Vector2(x, y)};

            foreach (var anchor in anchors)
            {
                var worldAnchor = spriteRenderer.transform.TransformPoint(anchor);
                if (Vector2.Distance(position, worldAnchor) <= snapDistance)
                    return worldAnchor;
            }

            return position;
        }

        protected static Vector3 SnapToPoint(Vector3 position, Vector3 snapPosition, float snapDistance)
        {
            snapDistance = HandleUtility.GetHandleSize(position) * snapDistance;
            return Vector3.Distance(position, snapPosition) <= snapDistance ? snapPosition : position;
        }

        protected static Vector2 RotateVector2(Vector2 direction, float angle)
        {
            var theta = Mathf.Deg2Rad * -angle;

            var cs = Mathf.Cos(theta);
            var sn = Mathf.Sin(theta);

            var px = direction.x * cs - direction.y * sn;
            var py = direction.x * sn + direction.y * cs;

            return new Vector2(px, py);
        }
    }
}
