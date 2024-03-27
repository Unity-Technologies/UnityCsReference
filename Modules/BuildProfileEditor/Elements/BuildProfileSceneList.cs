// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    
    internal class BuildProfileSceneList
    {
        static readonly GUIContent s_AddOpenScene = EditorGUIUtility.TrTextContent("Add Open Scenes");

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

        public VisualElement GetSceneListGUI(bool showOpenScenes)
        {
            TreeViewState m_TreeViewState = new TreeViewState();
            m_SceneTreeView = new BuildProfileSceneTreeView(m_TreeViewState, m_Target);
            m_SceneTreeView.Reload();

            return new IMGUIContainer(() =>
            {
                Rect rect = GUILayoutUtility.GetRect(
                    10000, m_SceneTreeView.totalHeight,
                    GUILayout.MinHeight(50));
                m_SceneTreeView.OnGUI(rect);

                if (showOpenScenes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(s_AddOpenScene))
                        m_SceneTreeView.AddOpenScenes();
                    GUILayout.EndHorizontal();
                }
            });
        }

        public void OnUndoRedo(in UndoRedoInfo info)
        {
            m_SceneTreeView.Reload();
        }
    }
}
