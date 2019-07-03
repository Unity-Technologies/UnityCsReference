// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    abstract class PrimitiveColliderTool<T> : EditorTool where T : Collider
    {
        public override GUIContent toolbarIcon
        {
            get { return PrimitiveBoundsHandle.editModeButton; }
        }

        protected abstract PrimitiveBoundsHandle boundsHandle { get; }

        protected abstract void CopyColliderPropertiesToHandle(T collider);

        protected abstract void CopyHandlePropertiesToCollider(T collider);

        protected Vector3 InvertScaleVector(Vector3 scaleVector)
        {
            for (int axis = 0; axis < 3; ++axis)
                scaleVector[axis] = scaleVector[axis] == 0f ? 0f : 1f / scaleVector[axis];

            return scaleVector;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var obj in targets)
            {
                var collider = (T)obj;

                if (Mathf.Approximately(collider.transform.lossyScale.sqrMagnitude, 0f))
                    continue;

                // collider matrix is center multiplied by transform's matrix with custom postmultiplied lossy scale matrix
                using (new Handles.DrawingScope(Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, Vector3.one)))
                {
                    CopyColliderPropertiesToHandle(collider);

                    boundsHandle.SetColor(collider.enabled ? Handles.s_ColliderHandleColor : Handles.s_ColliderHandleColorDisabled);

                    EditorGUI.BeginChangeCheck();

                    boundsHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(collider, string.Format("Modify {0}", ObjectNames.NicifyVariableName(target.GetType().Name)));
                        CopyHandlePropertiesToCollider(collider);
                    }
                }
            }
        }

        protected static Vector3 TransformColliderCenterToHandleSpace(Transform colliderTransform, Vector3 colliderCenter)
        {
            return Handles.inverseMatrix * (colliderTransform.localToWorldMatrix * colliderCenter);
        }

        protected static Vector3 TransformHandleCenterToColliderSpace(Transform colliderTransform, Vector3 handleCenter)
        {
            return colliderTransform.localToWorldMatrix.inverse * (Handles.matrix * handleCenter);
        }
    }
}
