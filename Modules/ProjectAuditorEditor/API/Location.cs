// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Represents the location of a reported issue.
    /// </summary>
    [Serializable]
    public class Location : ISerializationCallbackReceiver
    {
        [SerializeField] int line;
        [SerializeField] string path; // path relative to the project folder, in JSON format (with UNITY_PATH instead of applicationContentsPath)
        Func<string> m_PathGenerator;

        /// <summary>
        /// File extension
        /// </summary>
        public string Extension
        {
            get
            {
                if (m_Extension == null)
                {
                    m_Extension = System.IO.Path.GetExtension(Path) ?? string.Empty;
                    if (m_Extension.StartsWith("."))
                        m_Extension = m_Extension.Substring(1);
                }

                return m_Extension;
            }
        }

        /// <summary>
        /// Filename
        /// </summary>
        public string Filename
        {
            get
            {
                if (m_Filename == null)
                    m_Filename = string.IsNullOrEmpty(Path) ? string.Empty : System.IO.Path.GetFileName(Path);
                return m_Filename;
            }
        }

        /// <summary>
        /// Formatted filename with line number
        /// </summary>
        public string FormattedFilename
        {
            get
            {
                if (m_FormattedFilename == null)
                    m_FormattedFilename = GetFormattedPath(Filename, line);
                return m_FormattedFilename;
            }
        }

        /// <summary>
        /// Formatted path with line number
        /// </summary>
        public string FormattedPath
        {
            get
            {
                if (m_FormattedPath == null)
                    m_FormattedPath = GetFormattedPath(Path, line);
                return m_FormattedPath;
            }
        }

        /// <summary>
        /// Checks whether the location is valid
        /// </summary>
        /// <value>True if the location is valid</value>
        public bool IsValid => !string.IsNullOrEmpty(Path);

        /// <summary>
        /// Line number
        /// </summary>
        public int Line => line;

        /// <summary>
        /// Full path
        /// </summary>
        public string Path
        {
            get
            {
                RunPathGenerator();
                if (string.IsNullOrEmpty(path))
                    return string.Empty;
                return path.Replace("UNITY_PATH/Data", EditorApplication.applicationContentsPath);
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                    path = string.Empty;
                else
                    path = value.Replace(EditorApplication.applicationContentsPath, "UNITY_PATH/Data");
            }
        }

        internal string PathForJson => path;

        private void RunPathGenerator()
        {
            if (path == null && m_PathGenerator != null)
            {
                path = m_PathGenerator.Invoke().Replace($"{ProjectAuditor.ProjectPath}/", string.Empty);
                path = path.Replace(EditorApplication.applicationContentsPath, "UNITY_PATH/Data");
            }
            m_PathGenerator = null;
        }

        // Cached string storage to reduce constant string manipulation in UI
        string m_Extension;
        string m_Filename;
        string m_FormattedFilename;
        string m_FormattedPath;

        /// <summary>
        /// Constructor
        /// </summary>
        public Location() : this((string)null, 0)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">File path</param>
        public Location(string path)
        {
            Path = path.Replace($"{ProjectAuditor.ProjectPath}/", string.Empty);
        }

        /// <summary>
        /// Constructor with line number
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="line">Line number</param>
        public Location(string path, int line)
        {
            if (path != null)
                Path = path.Replace($"{ProjectAuditor.ProjectPath}/", string.Empty);
            this.line = line;
        }

        internal Location(Func<string> pathGenerator, int line)
        {
            if (pathGenerator != null)
                m_PathGenerator = pathGenerator;
            this.line = line;
        }

        static string GetFormattedPath(string path, int line)
        {
            if (path.EndsWith(".cs"))
                return $"{path}:{line}";
            return path;
        }

        /// <summary>
        /// Pre-serialize callback.
        /// </summary>
        public void OnBeforeSerialize()
        {
            RunPathGenerator();
        }

        /// <summary>
        /// Post-serialize callback.
        /// </summary>
        public void OnAfterDeserialize()
        {
        }
    }
}
