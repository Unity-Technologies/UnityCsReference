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
        GUIContent m_Content;
        IToolModeOwner m_Owner;
        bool m_CanEditMultipleObjects;

        protected EditModeTool()
        {
            m_CanEditMultipleObjects = editorType.GetCustomAttributes(typeof(CanEditMultipleObjects), false).Length > 0;
        }

        void OnEnable()
        {
            EditorTools.EditorTools.activeToolChanged += ActiveToolChanged;
        }

        void OnDisable()
        {
            EditorTools.EditorTools.activeToolChanged -= ActiveToolChanged;
        }

        public abstract SceneViewEditMode editMode { get; }

        public abstract Type editorType { get; }

        public override GUIContent toolbarIcon
        {
            get
            {
                if (m_Content == null)
                {
                    var customEditorAttributes = editorType.GetCustomAttributes(typeof(CustomEditor), false);

                    if (customEditorAttributes.Length > 0)
                    {
                        m_Content = new GUIContent(
                            AssetPreview.GetMiniTypeThumbnailFromType(((CustomEditor)customEditorAttributes[0])
                                .m_InspectedType),
                            editorType.ToString());
                    }
                    else
                    {
                        m_Content = new GUIContent(editorType.ToString(), editorType.ToString());
                    }
                }

                return m_Content;
            }
        }

        public IToolModeOwner owner
        {
            get { return m_Owner; }
            internal set { m_Owner = value; }
        }

        public override bool IsAvailable()
        {
            return m_CanEditMultipleObjects || Selection.count == 1;
        }

        void ActiveToolChanged()
        {
            if (EditorTools.EditorTools.IsActiveTool(this))
                OnActivate();
            else
                OnDeactivate();
        }

        void OnActivate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                return;
            EditMode.ChangeEditModeFromToolContext(owner, editMode);
        }

        void OnDeactivate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                return;
            EditMode.ChangeEditModeFromToolContext(owner, SceneViewEditMode.None);
        }
    }
}
