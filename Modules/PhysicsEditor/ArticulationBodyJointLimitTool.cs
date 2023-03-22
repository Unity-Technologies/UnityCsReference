// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor
{
    [EditorTool("Articulation Body Joint Limit Tool", typeof(ArticulationBody))]
    class ArticulationBodyJointLimitsTool : EditorTool
    {
        private const float k_CapScale = 0.12f;
        private const float k_DashSize = 1f;
        private const float k_DashAlpha = 0.8f;
        private const float k_PointerThickness = 1.3f;

        private JointAngularLimitHandle m_AngularLimitHandle = new JointAngularLimitHandle();

        protected static class Styles
        {
            public static readonly GUIContent toolbarIcon = new GUIContent(
                EditorGUIUtility.IconContent("JointAngularLimits").image,
                L10n.Tr("Edit the joint angular limits of this Articulation Body"));
        }

        public override GUIContent toolbarIcon => Styles.toolbarIcon;

        public Handles.CapFunction prismaticHandleDrawFunction { get; set; }

        private void OnEnable()
        {
            prismaticHandleDrawFunction = PrismaticHandleDrawFunction;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var obj in targets)
            {
                ArticulationBody body = obj as ArticulationBody;

                if (body == null)
                    continue;

                EditorGUI.BeginChangeCheck();

                DisplayJointLimits(body);

                if (EditorGUI.EndChangeCheck())
                {
                    body.WakeUp();
                }
            }
        }

        private void DisplayJointLimits(ArticulationBody body)
        {
            ArticulationBody parentBody = ArticulationBodyEditorCommon.FindEnabledParentArticulationBody(body);

            // Consider that the anchors are only actually matched when in play mode.
            // So, if it's not play mode, we need to do that manually for the gizmos to be placed correctly.
            Vector3 anchorPosition;
            Quaternion anchorRotation;
            if (body.matchAnchors & !EditorApplication.isPlaying)
            {
                anchorPosition = body.transform.TransformPoint(body.anchorPosition);
                anchorRotation = body.transform.rotation * body.anchorRotation;
            }
            else
            {
                anchorPosition = parentBody.transform.TransformPoint(body.parentAnchorPosition);
                anchorRotation = parentBody.transform.rotation * body.parentAnchorRotation;
            }
            Matrix4x4 parentAnchorSpace = Matrix4x4.TRS(anchorPosition, anchorRotation, Vector3.one);

            // Show locked gizmo when root body or Fixed joint body
            if (body.isRoot || body.jointType == ArticulationJointType.FixedJoint)
            {
                m_AngularLimitHandle.xMotion = (ConfigurableJointMotion)ArticulationDofLock.LockedMotion;
                m_AngularLimitHandle.yMotion = (ConfigurableJointMotion)ArticulationDofLock.LockedMotion;
                m_AngularLimitHandle.zMotion = (ConfigurableJointMotion)ArticulationDofLock.LockedMotion;

                ShowSphericalLimits(m_AngularLimitHandle, body, parentAnchorSpace);
                return;
            }

            if (body.jointType == ArticulationJointType.PrismaticJoint)
            {
                ShowPrismaticLimits(body, parentAnchorSpace, anchorPosition, anchorRotation);
                return;
            }

            if (body.jointType == ArticulationJointType.RevoluteJoint)
            {
                // For the purposes of drawing Revolute limits, treat Z and Y motion as locked
                m_AngularLimitHandle.xMotion = (ConfigurableJointMotion)body.twistLock;
                m_AngularLimitHandle.yMotion = (ConfigurableJointMotion)ArticulationDofLock.LockedMotion;
                m_AngularLimitHandle.zMotion = (ConfigurableJointMotion)ArticulationDofLock.LockedMotion;

                ShowRevoluteLimits(m_AngularLimitHandle, body, parentAnchorSpace);
                return;
            }

            if (body.jointType == ArticulationJointType.SphericalJoint)
            {
                m_AngularLimitHandle.xMotion = (ConfigurableJointMotion)body.twistLock;
                m_AngularLimitHandle.yMotion = (ConfigurableJointMotion)body.swingYLock;
                m_AngularLimitHandle.zMotion = (ConfigurableJointMotion)body.swingZLock;

                ShowSphericalLimits(m_AngularLimitHandle, body, parentAnchorSpace);
            }
        }

        private void ShowPrismaticLimits(ArticulationBody body, Matrix4x4 parentAnchorSpace, Vector3 anchorPosition, Quaternion anchorRotation)
        {
            Vector3 primaryAxis = Vector3.zero;
            // compute the primary axis of the prismatic
            ArticulationDrive drive = body.xDrive;

            if (body.linearLockX != ArticulationDofLock.LockedMotion)
            {
                primaryAxis = Vector3.right;
                drive = body.xDrive;
            }
            else if (body.linearLockY != ArticulationDofLock.LockedMotion)
            {
                primaryAxis = Vector3.up;
                drive = body.yDrive;
            }
            else if (body.linearLockZ != ArticulationDofLock.LockedMotion)
            {
                primaryAxis = Vector3.forward;
                drive = body.zDrive;
            }

            DisplayPrismaticJointPointer(parentAnchorSpace, body.jointPosition[0], primaryAxis);


            if (body.linearLockX == ArticulationDofLock.FreeMotion || body.linearLockY == ArticulationDofLock.FreeMotion || body.linearLockZ == ArticulationDofLock.FreeMotion)
            {
                DrawFreePrismatic(body, primaryAxis, anchorPosition, anchorRotation);
                return;
            }

            DrawLimitedPrismatic(body, primaryAxis, drive, parentAnchorSpace);
        }

        private void DrawFreePrismatic(ArticulationBody body, Vector3 primaryAxis, Vector3 anchorPosition, Quaternion anchorRotation)
        {
            Vector3 lp, up;
            float paddingAmount = 15;
            Handles.color = new Color(1f, 1f, 1f, k_DashAlpha);
            Vector3 primaryAxisRotated = anchorRotation * primaryAxis;
            Vector3 padding = primaryAxisRotated * paddingAmount;

            // If not in play mode and match anchors is off, use the anchor position
            if (!body.matchAnchors & !EditorApplication.isPlaying)
            {
                lp = anchorPosition - padding;
                up = anchorPosition + padding;
            }
            // If in play mode or match anchors is on, calculate the correct position
            else
            {
                Vector3 bodyPosOnPrimaryAxis = Vector3.Project(body.transform.position - anchorPosition, primaryAxisRotated);

                lp = anchorPosition + bodyPosOnPrimaryAxis - padding;
                up = anchorPosition + bodyPosOnPrimaryAxis + padding;
            }
            Handles.DrawDottedLine(lp, up, k_DashSize);
        }

        private void DrawLimitedPrismatic(ArticulationBody body, Vector3 primaryAxis, ArticulationDrive drive, Matrix4x4 parentAnchorSpace)
        {
            using (new Handles.DrawingScope(parentAnchorSpace))
            {
                Vector3 lowerPoint = primaryAxis * drive.lowerLimit;
                Vector3 upperPoint = primaryAxis * drive.upperLimit;

                Handles.DrawDottedLine(lowerPoint, upperPoint, k_DashSize);

                int idLower = GUIUtility.GetControlID(Handles.s_SliderHash, FocusType.Passive);
                int idUpper = GUIUtility.GetControlID(Handles.s_SliderHash, FocusType.Passive);

                EditorGUI.BeginChangeCheck();
                {
                    Handles.color = Handles.xAxisColor;
                    lowerPoint = Handles.Slider(idLower, lowerPoint, primaryAxis, k_CapScale, prismaticHandleDrawFunction, 0);

                    Handles.color = Handles.yAxisColor;
                    upperPoint = Handles.Slider(idUpper, upperPoint, primaryAxis, k_CapScale, prismaticHandleDrawFunction, 0);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Changing Articulation body parent anchor prismatic limits");
                    float newLowerLimit = drive.lowerLimit;
                    float newUpperLimit = drive.upperLimit;

                    // Disallow moving Lower limit past Upper limit and vice versa, based on which handle is being held down
                    if (GUIUtility.hotControl == idLower)
                    {
                        float directionLower = Mathf.Sign(lowerPoint.x + lowerPoint.y + lowerPoint.z);
                        newLowerLimit = lowerPoint.magnitude * directionLower;
                        if (newLowerLimit > drive.upperLimit) newLowerLimit = drive.upperLimit;
                    }
                    else if (GUIUtility.hotControl == idUpper)
                    {
                        float directionUpper = Mathf.Sign(upperPoint.x + upperPoint.y + upperPoint.z);
                        newUpperLimit = upperPoint.magnitude * directionUpper;
                        if (newUpperLimit < drive.lowerLimit) newUpperLimit = drive.lowerLimit;
                    }

                    ArticulationDrive tempDrive = SetDriveLimits(drive, newLowerLimit, newUpperLimit);

                    if (body.linearLockX == ArticulationDofLock.LimitedMotion)
                    {
                        body.xDrive = tempDrive;
                    }
                    else if (body.linearLockY == ArticulationDofLock.LimitedMotion)
                    {
                        body.yDrive = tempDrive;
                    }
                    else if (body.linearLockZ == ArticulationDofLock.LimitedMotion)
                    {
                        body.zDrive = tempDrive;
                    }
                }
            }
        }

        void DisplayPrismaticJointPointer(Matrix4x4 parentAnchorSpace, float jointPosition, Vector3 primaryAxis)
        {
            if (!Application.isPlaying)
                return;

            using (new Handles.DrawingScope(Color.white, parentAnchorSpace))
            {
                Vector3 discPosition = primaryAxis.normalized * jointPosition;
                Handles.DrawSolidDisc(discPosition, primaryAxis, HandleUtility.GetHandleSize(discPosition) * 0.04f);
            }
        }

        ArticulationDrive SetDriveLimits(ArticulationDrive drive, float lowerLimit, float upperLimit)
        {
            var tempDrive = drive;
            tempDrive.lowerLimit = lowerLimit;
            tempDrive.upperLimit = upperLimit;
            return tempDrive;
        }

        public void PrismaticHandleDrawFunction(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
        {
            Handles.CylinderHandleCap(controlID, position, rotation, size * HandleUtility.GetHandleSize(position), eventType);
        }

        private void ShowRevoluteLimits(JointAngularLimitHandle handle, ArticulationBody body, Matrix4x4 parentAnchorSpace)
        {
            using (new Handles.DrawingScope(parentAnchorSpace))
            {
                handle.xMin = body.xDrive.lowerLimit;
                handle.xMax = body.xDrive.upperLimit;

                DisplayAngularJointPointer(parentAnchorSpace, body, handle, k_PointerThickness);

                EditorGUI.BeginChangeCheck();

                handle.radius = HandleUtility.GetHandleSize(Vector3.zero);
                handle.DrawHandle(true);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Changing Articulation body parent anchor rotation limits");
                    body.xDrive = SetDriveLimits(body.xDrive, handle.xMin, handle.xMax);
                }
            }
        }

        private void ShowSphericalLimits(JointAngularLimitHandle handle, ArticulationBody body, Matrix4x4 parentAnchorSpace)
        {
            using (new Handles.DrawingScope(parentAnchorSpace))
            {
                handle.xMin = body.xDrive.lowerLimit;
                handle.xMax = body.xDrive.upperLimit;

                handle.yMin = body.yDrive.lowerLimit;
                handle.yMax = body.yDrive.upperLimit;

                handle.zMin = body.zDrive.lowerLimit;
                handle.zMax = body.zDrive.upperLimit;

                DisplayAngularJointPointer(parentAnchorSpace, body, handle, k_PointerThickness);
                EditorGUI.BeginChangeCheck();

                handle.radius = HandleUtility.GetHandleSize(Vector3.zero);
                handle.DrawHandle(true);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Changing Articulation body parent anchor rotation limits");

                    body.xDrive = SetDriveLimits(body.xDrive, handle.xMin, handle.xMax);
                    body.yDrive = SetDriveLimits(body.yDrive, handle.yMin, handle.yMax);
                    body.zDrive = SetDriveLimits(body.zDrive, handle.zMin, handle.zMax);
                }
            }
        }

        private void DisplayAngularJointPointer(Matrix4x4 parentAnchorSpace, ArticulationBody body, JointAngularLimitHandle handle, float pointerThickness)
        {
            if (body.isRoot || !Application.isPlaying)
                return;

            // Since ArticulationBody.jointPosition can return either 1, 2 or 3 available values
            // and depending on configuration jointPosition[0] could be any of the enabled drives
            // Though we are sure that they will always be in order X -> Y -> Z if they are enabled.
            int nbEnabledDrives = 0;
            float pointerLength = handle.radius;
            ArticulationReducedSpace jointPosition = body.jointPosition;
            Matrix4x4 twistAxisRotation = Matrix4x4.identity;

            if (body.twistLock != ArticulationDofLock.LockedMotion)
            {
                // In the case of Limited Twist Axis, the whole gizmo will be rotated based on its upper and lower limits
                // Because of that we need to make sure the pointer line also stays within the same plane on Y and Z axes

                if (body.twistLock == ArticulationDofLock.LimitedMotion)
                    twistAxisRotation = Matrix4x4.Rotate(Quaternion.AngleAxis((body.xDrive.upperLimit + body.xDrive.lowerLimit) / 2, Vector3.left));

                // Rotate the end point so that it matches the X axis on the gizmo
                Quaternion axisRotationX = Quaternion.Euler(new Vector3(0, 0, 180));
                var endPoint = GetPointOnAngularJointAxis(jointPosition[nbEnabledDrives], pointerLength, axisRotationX);

                DrawJointPointerLine(parentAnchorSpace, endPoint, handle.xHandleColor, pointerThickness, Vector3.right);
                nbEnabledDrives++;
            }

            if (body.swingYLock != ArticulationDofLock.LockedMotion)
            {
                // Rotate the end point so that it matches the Y axis on the gizmo
                Quaternion axisRotationY = Quaternion.Euler(new Vector3(0, 0, -90));
                var endPoint = GetPointOnAngularJointAxis(jointPosition[nbEnabledDrives], pointerLength, axisRotationY);

                DrawJointPointerLine(parentAnchorSpace * twistAxisRotation, endPoint, handle.yHandleColor, pointerThickness, Vector3.up);
                nbEnabledDrives++;
            }

            if (body.swingZLock != ArticulationDofLock.LockedMotion)
            {
                // Rotate the end point so that it matches the Z axis on the gizmo
                Quaternion axisRotationZ = Quaternion.Euler(new Vector3(-90, 90, 0));
                var endPoint = GetPointOnAngularJointAxis(jointPosition[nbEnabledDrives], pointerLength, axisRotationZ);

                DrawJointPointerLine(parentAnchorSpace * twistAxisRotation, endPoint, handle.zHandleColor, pointerThickness, Vector3.forward);
            }
        }

        private Vector3 GetPointOnAngularJointAxis(float jointPosition, float length, Quaternion axis)
        {
            return axis * (length * new Vector3(0, Mathf.Sin(jointPosition), Mathf.Cos(jointPosition)));
        }

        private void DrawJointPointerLine(Matrix4x4 parentAnchorSpace, Vector3 endPoint, Color color, float thickness, Vector3 discAxis, int dashCount = 4)
        {
            using (new Handles.DrawingScope(color, parentAnchorSpace))
            {
                // Implement a manual dashed line with thickness, since
                // Handles.DrawDottedLine does not have an option for that
                float screenSpaceMult = HandleUtility.GetHandleSize(Vector3.zero);
                float dashLength = endPoint.magnitude / dashCount;
                Vector3 endPointNormalized = endPoint.normalized;
                Vector3 dirMultiplier = endPointNormalized * dashLength;
                for (int i = 0; i < dashCount; i++)
                {
                    Handles.DrawLine(dirMultiplier * i, dirMultiplier * (i + 1) - endPointNormalized * 0.05f * screenSpaceMult, thickness);
                }
                Handles.DrawSolidDisc(endPoint, discAxis, screenSpaceMult * 0.04f);
            }
        }
    }
}
