// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class ProjectTemplateWindow : EditorWindow
    {
        string m_Path;
        string m_Name;
        string m_DisplayName;
        string m_Description;
        string m_DefaultScene;
        string m_Version;

        [MenuItem("internal:Project/Save As Template...")]
        internal static void SaveAsTemplate()
        {
            var window = EditorWindow.GetWindow<ProjectTemplateWindow>();
            window.Show();
        }

        void OnEnable()
        {
            titleContent = EditorGUIUtility.TrTextContent("Save Template");
        }

        string ReadJsonString(JSONValue json, string key)
        {
            if (json.ContainsKey(key) && !json[key].IsNull())
            {
                return json[key].AsString();
            }
            return null;
        }

        void OnGUI()
        {
            if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
            {
                m_Path = EditorUtility.SaveFolderPanel("Choose target folder", "", "");
                var packageJsonPath = Path.Combine(m_Path, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    var packageJson = File.ReadAllText(packageJsonPath);
                    var json = new JSONParser(packageJson).Parse();
                    m_Name = ReadJsonString(json, "name");
                    m_DisplayName = ReadJsonString(json, "displayName");
                    m_Description = ReadJsonString(json, "description");
                    m_DefaultScene = ReadJsonString(json, "defaultScene");
                    m_Version = ReadJsonString(json, "version");
                }
            }
            EditorGUILayout.TextField("Path:", m_Path);

            m_Name = EditorGUILayout.TextField("Name:", m_Name);
            m_DisplayName = EditorGUILayout.TextField("Display name:", m_DisplayName);
            m_Description = EditorGUILayout.TextField("Description:", m_Description);
            m_DefaultScene = EditorGUILayout.TextField("Default scene:", m_DefaultScene);
            m_Version = EditorGUILayout.TextField("Version:", m_Version);

            if (GUILayout.Button("Save", GUILayout.Width(100)))
            {
                if (String.IsNullOrEmpty(m_Path))
                {
                    m_Path = EditorUtility.SaveFolderPanel("Save template to folder", "", "");
                }
                AssetDatabase.SaveAssets();
                EditorUtility.SaveProjectAsTemplate(m_Path, m_Name, m_DisplayName, m_Description, m_DefaultScene, m_Version);
            }
        }
    }
}
