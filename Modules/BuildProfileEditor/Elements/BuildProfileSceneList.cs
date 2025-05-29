// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace UnityEditor.Build.Profile.Elements
{
    
    internal class BuildProfileSceneList
    {
        BuildProfile m_Target;
        BuildProfileSceneTreeView m_SceneTreeView;

        public BuildProfileSceneList()
        {
            m_Target = null;
        }

        public BuildProfileSceneList(BuildProfile target)
        {
            m_Target = target;
        }

        public VisualElement GetSceneListGUI()
        {
            m_SceneTreeView = new BuildProfileSceneTreeView(new TreeViewState(), m_Target);
            m_SceneTreeView.Reload();

            return new IMGUIContainer(() =>
            {
                Rect rect = GUILayoutUtility.GetRect(
                    10000, m_SceneTreeView.totalHeight,
                    GUILayout.MinHeight(50));
                m_SceneTreeView.OnGUI(rect);
            });
        }

        public void AddOpenScenes()
        {
            m_SceneTreeView.AddOpenScenes();
        }

        public void OnUndoRedo(in UndoRedoInfo info)
        {
            m_SceneTreeView.Reload();
        }
    }
}
