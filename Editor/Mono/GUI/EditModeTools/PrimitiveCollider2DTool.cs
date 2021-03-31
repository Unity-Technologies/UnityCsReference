// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    abstract class PrimitiveCollider2DTool<T> : EditorTool where T : Collider2D
    {
        protected abstract PrimitiveBoundsHandle boundsHandle { get; }

        public override GUIContent toolbarIcon { get { return PrimitiveBoundsHandle.editModeButton; } }

        protected abstract void CopyColliderSizeToHandle(T collider);

        // only return true if the size has changed
        protected abstract bool CopyHandleSizeToCollider(T collider);

        protected virtual Quaternion GetHandleRotation(T collider)
        {
            return Quaternion.identity;
        }

        public virtual void OnEnable()
        {
            boundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var obj in targets)
            {
                T collider = obj as T;

                if (collider == null || Mathf.Approximately(collider.transform.lossyScale.sqrMagnitude, 0f))
                    continue;

                // collider matrix is 2d projection of center multiplied by transform's matrix with custom postmultiplied lossy scale matrix
                // only rotation of transform about z-axis should be considered, if at all
                using (new Handles.DrawingScope(Matrix4x4.TRS(collider.transform.position, GetHandleRotation(collider), Vector3.one)))
                {
                    Matrix4x4 colliderTransformMatrix = collider.transform.localToWorldMatrix;

                    boundsHandle.center =
                        ProjectOntoWorldPlane(Handles.inverseMatrix * (colliderTransformMatrix * collider.offset));

                    CopyColliderSizeToHandle(collider);

                    boundsHandle.SetColor(collider.enabled
                        ? Handles.s_ColliderHandleColor
                        : Handles.s_ColliderHandleColorDisabled);

                    EditorGUI.BeginChangeCheck();

                    boundsHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(collider,
                            string.Format("Modify {0}", ObjectNames.NicifyVariableName(target.GetType().Name)));

                        // because projection of offset is a lossy operation, only do it if the size has actually changed
                        // this check prevents drifting while dragging handle when size is zero (case 863949)
                        if (CopyHandleSizeToCollider(collider))
                            collider.offset = colliderTransformMatrix.inverse *
                                ProjectOntoColliderPlane(Handles.matrix * boundsHandle.center,
                                colliderTransformMatrix);
                    }
                }
            }
        }

        // return specified world vector projected onto collider's x/y plane in world space
        protected Vector3 ProjectOntoColliderPlane(Vector3 worldVector, Matrix4x4 colliderTransformMatrix)
        {
            Plane worldColliderPlane = new Plane(
                Vector3.Cross(colliderTransformMatrix * Vector3.right, colliderTransformMatrix * Vector3.up),
                Vector3.zero
            );

            Ray ray = new Ray(worldVector, Vector3.forward);

            float distance;

            if (worldColliderPlane.Raycast(ray, out distance))
                return ray.GetPoint(distance);

            ray.direction = Vector3.back;

            worldColliderPlane.Raycast(ray, out distance);

            return ray.GetPoint(distance);
        }

        // return specified world vector projected onto world x/y plane
        protected Vector3 ProjectOntoWorldPlane(Vector3 worldVector)
        {
            worldVector.z = 0f;
            return worldVector;
        }
    }
}
