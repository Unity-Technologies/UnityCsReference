// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal abstract class ColliderEditorBase : Editor
    {
        protected virtual void OnEditStart() {}
        protected virtual void OnEditEnd() {}

        public bool editingCollider
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.Collider && EditMode.IsOwner(this); }
        }

        public virtual void OnEnable()
        {
            EditMode.editModeStarted += OnEditModeStart;
            EditMode.editModeEnded += OnEditModeEnd;
        }

        public virtual void OnDisable()
        {
            EditMode.editModeStarted -= OnEditModeStart;
            EditMode.editModeEnded -= OnEditModeEnd;
        }

        protected virtual GUIContent editModeButton { get { return EditorGUIUtility.IconContent("EditCollider"); } }

        protected void InspectorEditButtonGUI()
        {
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Edit Collider",
                editModeButton,
                this
                );
        }

        internal override Bounds GetWorldBoundsOfTarget(Object targetObject)
        {
            if (targetObject is Collider2D)
                return ((Collider2D)targetObject).bounds;
            else if (targetObject is Collider)
                return ((Collider)targetObject).bounds;
            else
                return base.GetWorldBoundsOfTarget(targetObject);
        }

        protected void OnEditModeStart(IToolModeOwner owner, EditMode.SceneViewEditMode mode)
        {
            if (mode == EditMode.SceneViewEditMode.Collider && owner == (IToolModeOwner)this)
                OnEditStart();
        }

        protected void OnEditModeEnd(IToolModeOwner owner)
        {
            if (owner == (IToolModeOwner)this)
                OnEditEnd();
        }
    }
}
