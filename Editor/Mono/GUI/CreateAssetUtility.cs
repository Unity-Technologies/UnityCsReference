// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;


namespace UnityEditor
{
    [System.Serializable]
    internal class CreateAssetUtility
    {
        [SerializeField]
        EndNameEditAction m_EndAction;
        [SerializeField]
        int m_InstanceID;
        [SerializeField]
        string m_Path = "";
        [SerializeField]
        Texture2D m_Icon;
        [SerializeField]
        string m_ResourceFile;

        public void Clear()
        {
            m_EndAction = null;
            m_InstanceID = 0;
            m_Path = "";
            m_Icon = null;
            m_ResourceFile = "";
        }

        public int instanceID
        {
            get { return m_InstanceID; }
        }

        public Texture2D icon
        {
            get { return m_Icon; }
        }

        public string folder
        {
            get { return Path.GetDirectoryName(m_Path); }
        }

        public string extension
        {
            get { return Path.GetExtension(m_Path); }
        }

        public string originalName
        {
            get { return Path.GetFileNameWithoutExtension(m_Path); }
        }

        public EndNameEditAction endAction
        {
            get { return m_EndAction; }
        }

        static bool IsPathDataValid(string filePath)
        {
            // Ensure some path
            if (string.IsNullOrEmpty(filePath))
                return false;

            // Ensure valid folder to place the asset in
            string folder = Path.GetDirectoryName(filePath);
            int instanceID = AssetDatabase.GetMainAssetInstanceID(folder);
            return instanceID != 0;
        }

        // Selection changes when calling BeginNewAsset if it succeeds
        public bool BeginNewAssetCreation(int instanceID, EndNameEditAction newAssetEndAction, string filePath, Texture2D icon, string newAssetResourceFile)
        {
            string uniquePath;
            if (!filePath.StartsWith("assets/", System.StringComparison.CurrentCultureIgnoreCase))
            {
                uniquePath = AssetDatabase.GetUniquePathNameAtSelectedPath(filePath);
            }
            else
            {
                uniquePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
            }

            if (!IsPathDataValid(uniquePath))
            {
                Debug.LogErrorFormat("Invalid generated unique path '{0}' (input path '{1}')", uniquePath, filePath);
                Clear();
                return false;
            }

            m_InstanceID = instanceID;
            m_Path = uniquePath;
            m_Icon = icon;
            m_EndAction = newAssetEndAction;
            m_ResourceFile = newAssetResourceFile;

            // Change selection to none or instanceID
            Selection.activeObject = EditorUtility.InstanceIDToObject(instanceID);
            return true;
        }

        // The asset is created here
        public void EndNewAssetCreation(string name)
        {
            string path = folder + "/" + name + extension;
            EndNameEditAction endAction = m_EndAction;
            int instanceID = m_InstanceID;
            string resourceFile = m_ResourceFile;
            Clear(); // Ensure clear if anything goes bad in EndNameEditAction and gui is exited.

            ProjectWindowUtil.EndNameEditAction(endAction, instanceID, path, resourceFile);
        }

        public bool IsCreatingNewAsset()
        {
            return !string.IsNullOrEmpty(m_Path);
        }
    }
} // end namespace UnityEditor
