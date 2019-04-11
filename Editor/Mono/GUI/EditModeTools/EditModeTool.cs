// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.EditorTools;
using UnityEditorInternal;
using UnityEngine;
using SceneViewEditMode = UnityEditorInternal.EditMode.SceneViewEditMode;

namespace UnityEditor
{
    abstract class EditModeTool : EditorTool
    {
        IToolModeOwner m_Owner;
        bool m_CanEditMultipleObjects;

        protected EditModeTool()
        {
            m_CanEditMultipleObjects = editorType.GetCustomAttributes(typeof(CanEditMultipleObjects), false).Length > 0;
        }

        void OnEnable()
        {
            EditorTools.EditorTools.activeToolChanging += ActiveToolChanging;
            EditorTools.EditorTools.activeToolChanged += ActiveToolChanged;
        }

        void OnDisable()
        {
            EditorTools.EditorTools.activeToolChanging -= ActiveToolChanging;
            EditorTools.EditorTools.activeToolChanged -= ActiveToolChanged;
        }

        public abstract SceneViewEditMode editMode { get; }

        public abstract Type editorType { get; }

        public IToolModeOwner owner
        {
            get { return m_Owner; }
            internal set { m_Owner = value; }
        }

        public override bool IsAvailable()
        {
            return m_CanEditMultipleObjects || Selection.count == 1;
        }

        void ActiveToolChanging()
        {
            if (EditorTools.EditorTools.IsActiveTool(this))
                OnDeactivate();
        }

        void ActiveToolChanged()
        {
            if (EditorTools.EditorTools.IsActiveTool(this))
                OnActivate();
        }

        void OnActivate()
        {
            EditMode.EditModeToolStateChanged(owner, editMode);
        }

        void OnDeactivate()
        {
            EditMode.EditModeToolStateChanged(owner, SceneViewEditMode.None);
        }
    }
}
