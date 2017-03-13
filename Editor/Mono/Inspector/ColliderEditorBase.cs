// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class ColliderEditorBase : Editor
    {
        protected virtual void OnEditStart() {}
        protected virtual void OnEditEnd() {}

        public bool editingCollider
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.Collider && EditMode.IsOwner(this); }
        }

        public virtual void OnEnable()
        {
            EditMode.onEditModeStartDelegate += OnEditModeStart;
            EditMode.onEditModeEndDelegate += OnEditModeEnd;
        }

        public virtual void OnDisable()
        {
            EditMode.onEditModeStartDelegate -= OnEditModeStart;
            EditMode.onEditModeEndDelegate -= OnEditModeEnd;
        }

        protected virtual GUIContent editModeButton { get { return EditorGUIUtility.IconContent("EditCollider"); } }

        protected void InspectorEditButtonGUI()
        {
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Edit Collider",
                editModeButton,
                GetColliderBounds(target),
                this
                );
        }

        private static Bounds GetColliderBounds(Object collider)
        {
            if (collider is Collider2D)
                return (collider as Collider2D).bounds;
            else if (collider is Collider)
                return (collider as Collider).bounds;

            return new Bounds();
        }

        protected void OnEditModeStart(Editor editor, EditMode.SceneViewEditMode mode)
        {
            if (mode == EditMode.SceneViewEditMode.Collider && editor == this)
                OnEditStart();
        }

        protected void OnEditModeEnd(Editor editor)
        {
            if (editor == this)
                OnEditEnd();
        }
    }
}
