// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    internal abstract class PrimitiveCollider3DEditor : Collider3DEditorBase
    {
        protected abstract PrimitiveBoundsHandle boundsHandle { get; }

        protected abstract void CopyColliderPropertiesToHandle();

        protected abstract void CopyHandlePropertiesToCollider();

        protected override GUIContent editModeButton { get { return PrimitiveBoundsHandle.editModeButton; } }

        protected Vector3 InvertScaleVector(Vector3 scaleVector)
        {
            for (int axis = 0; axis < 3; ++axis)
                scaleVector[axis] = scaleVector[axis] == 0f ? 0f : 1f / scaleVector[axis];
            return scaleVector;
        }

        protected virtual void OnSceneGUI()
        {
            if (!editingCollider)
                return;

            Collider collider = (Collider)target;

            if (Mathf.Approximately(collider.transform.lossyScale.sqrMagnitude, 0f))
                return;

            // collider matrix is center multiplied by transform's matrix with custom postmultiplied lossy scale matrix
            using (new Handles.DrawingScope(Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, Vector3.one)))
            {
                CopyColliderPropertiesToHandle();

                boundsHandle.SetColor(collider.enabled ? Handles.s_ColliderHandleColor : Handles.s_ColliderHandleColorDisabled);
                EditorGUI.BeginChangeCheck();
                boundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(collider, string.Format("Modify {0}", ObjectNames.NicifyVariableName(target.GetType().Name)));
                    CopyHandlePropertiesToCollider();
                }
            }
        }

        protected Vector3 TransformColliderCenterToHandleSpace(Transform colliderTransform, Vector3 colliderCenter)
        {
            return Handles.inverseMatrix * (colliderTransform.localToWorldMatrix * colliderCenter);
        }

        protected Vector3 TransformHandleCenterToColliderSpace(Transform colliderTransform, Vector3 handleCenter)
        {
            return colliderTransform.localToWorldMatrix.inverse * (Handles.matrix * handleCenter);
        }
    }
}
