// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.IO;
using UnityEditorInternal;


namespace UnityEditor
{
    // Use the FilePathAttribute when you want to have your scriptable singleton to persist between unity sessions.
    // Example: [FilePathAttribute("Library/SearchFilters.ssf", FilePathAttribute.Location.ProjectFolder)]
    // Ensure to call Save() from client code (derived instance)
    [AttributeUsage(AttributeTargets.Class)]
    public class FilePathAttribute : Attribute
    {
        public enum Location { PreferencesFolder, ProjectFolder }

        private string m_FilePath;
        private string m_RelativePath;
        private Location m_Location;

        internal string filepath
        {
            get
            {
                if (m_FilePath == null && m_RelativePath != null)
                {
                    m_FilePath = CombineFilePath(m_RelativePath, m_Location);
                    m_RelativePath = null;
                }

                return m_FilePath;
            }
        }

        public FilePathAttribute(string relativePath, Location location)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException("Invalid relative path (it is empty)");
            }

            m_RelativePath = relativePath;
            m_Location = location;
        }

        static string CombineFilePath(string relativePath, Location location)
        {
            // We do not want a slash as first char
            if (relativePath[0] == '/')
                relativePath = relativePath.Substring(1);

            switch (location)
            {
                case Location.PreferencesFolder: return InternalEditorUtility.unityPreferencesFolder + "/" + relativePath;
                case Location.ProjectFolder:     return relativePath;
                default:
                    Debug.LogError("Unhandled enum: " + location);
                    return relativePath; // fallback to ProjectFolder relative path
            }
        }
    }

    public class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        static T s_Instance;

        public static T instance
        {
            get
            {
                if (s_Instance == null)
                    CreateAndLoad();

                return s_Instance;
            }
        }

        // On domain reload ScriptableObject objects gets reconstructed from a backup. We therefore set the s_Instance here
        protected ScriptableSingleton()
        {
            if (s_Instance != null)
            {
                Debug.LogError("ScriptableSingleton already exists. Did you query the singleton in a constructor?");
            }
            else
            {
                object casted = this;
                s_Instance = casted as T;
                System.Diagnostics.Debug.Assert(s_Instance != null);
            }
        }

        private static void CreateAndLoad()
        {
            System.Diagnostics.Debug.Assert(s_Instance == null);

            // Load
            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                // If a file exists the
                InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            }

            if (s_Instance == null)
            {
                // Create
                T t = CreateInstance<T>();
                t.hideFlags = HideFlags.HideAndDontSave;
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);
        }

        protected virtual void Save(bool saveAsText)
        {
            if (s_Instance == null)
            {
                Debug.LogError("Cannot save ScriptableSingleton: no instance!");
                return;
            }

            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                string folderPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { s_Instance }, filePath, saveAsText);
            }
        }

        protected static string GetFilePath()
        {
            Type type = typeof(T);
            object[] attributes = type.GetCustomAttributes(true);
            foreach (object attr in attributes)
            {
                if (attr is FilePathAttribute)
                {
                    FilePathAttribute f = attr as FilePathAttribute;
                    return f.filepath;
                }
            }
            return string.Empty;
        }
    }
}
