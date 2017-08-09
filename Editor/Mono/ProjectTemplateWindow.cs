// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal class ProjectTemplateWindow : EditorWindow
    {
        string m_Name;
        string m_DisplayName;
        string m_Description;
        string m_Version;

        [MenuItem("internal:Project/Save As Template...")]
        internal static void SaveAsTemplate()
        {
            var window = EditorWindow.GetWindow<ProjectTemplateWindow>();
            window.Show();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Save Template");
        }

        void OnGUI()
        {
            m_Name = EditorGUILayout.TextField("Name:", m_Name);
            m_DisplayName = EditorGUILayout.TextField("Display name:", m_DisplayName);
            m_Description = EditorGUILayout.TextField("Description:", m_Description);
            m_Version = EditorGUILayout.TextField("Version:", m_Version);
            if (GUILayout.Button("Save As...", GUILayout.Width(100)))
            {
                string path = EditorUtility.SaveFolderPanel("Save template to folder", "", "");
                if (path.Length > 0)
                {
                    AssetDatabase.SaveAssets();
                    EditorUtility.SaveProjectAsTemplate(path, m_Name, m_DisplayName, m_Description, m_Version);
                }
            }
        }
    }
}
