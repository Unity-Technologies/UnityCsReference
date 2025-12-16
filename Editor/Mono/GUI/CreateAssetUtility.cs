// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.Utils;
using UnityEngine;


namespace UnityEditor
{
    [System.Serializable]
    internal class CreateAssetUtility
    {
        [SerializeField]
        AssetCreationEndAction m_EndAction;
        [SerializeField]
        EntityId m_EntityId;
        [SerializeField]
        string m_Path = "";
        [SerializeField]
        Texture2D m_Icon;
        [SerializeField]
        string m_ResourceFile;

        public void Clear()
        {
            m_EndAction = null;
            m_EntityId = EntityId.None;
            m_Path = "";
            m_Icon = null;
            m_ResourceFile = "";
        }

        public EntityId entityId
        {
            get { return m_EntityId; }
        }

        public Texture2D icon
        {
            get { return m_Icon; }
        }

        public string folder
        {
            get { return Path.GetDirectoryName(m_Path).ConvertSeparatorsToUnity(); }
        }

        public string extension
        {
            get { return Path.GetExtension(m_Path); }
        }

        public string originalName
        {
            get { return Path.GetFileNameWithoutExtension(m_Path); }
        }

        public AssetCreationEndAction endAction
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
            EntityId entityId = AssetDatabase.GetMainAssetEntityId(folder);
            return entityId != EntityId.None;
        }

        // Selection changes when calling BeginNewAsset if it succeeds
        public bool BeginNewAssetCreation(EntityId entityId, AssetCreationEndAction newAssetEndAction, string filePath, Texture2D icon, string newAssetResourceFile, bool selectAssetBeingCreated = true)
        {
            //Sanitize input
            string sanitizedFilePath = filePath != null ? filePath.ConvertSeparatorsToUnity() : filePath;
            string sanitizedNewAssetResourceFile = newAssetResourceFile != null ? newAssetResourceFile.ConvertSeparatorsToUnity() : newAssetResourceFile;

            string uniquePath;
            if (!sanitizedFilePath.StartsWith("assets/", System.StringComparison.CurrentCultureIgnoreCase) && !sanitizedFilePath.StartsWith("packages/", System.StringComparison.CurrentCultureIgnoreCase))
            {
                // If sanitizedFilePath is not already a full asset path, we need to get the full path
                uniquePath = AssetDatabase.GetUniquePathNameAtSelectedPath(sanitizedFilePath);
            }
            else
            {
                uniquePath = AssetDatabase.GenerateUniqueAssetPath(sanitizedFilePath);
            }

            if (!IsPathDataValid(uniquePath))
            {
                Debug.LogErrorFormat("Invalid generated unique path '{0}' (input path '{1}')", uniquePath, sanitizedFilePath);
                Clear();
                return false;
            }

            m_EntityId = entityId;
            m_Path = uniquePath;
            m_Icon = icon;
            m_EndAction = newAssetEndAction;
            m_ResourceFile = sanitizedNewAssetResourceFile;

            if (selectAssetBeingCreated)
            {
                // Change selection to none or instanceID
                Selection.activeObject = EditorUtility.EntityIdToObject(entityId);
            }
            return true;
        }

        // The asset is created here
        public void EndNewAssetCreation(string name)
        {
            string path = folder + "/" + name;
            if ((!String.IsNullOrEmpty(extension)) && (!path.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase)))
                path = path + extension;
            AssetCreationEndAction endAction = m_EndAction;
            EntityId entityId = m_EntityId;
            string resourceFile = m_ResourceFile;
            Clear(); // Ensure clear if anything goes bad in EndNameEditAction and gui is exited.

            ProjectWindowUtil.EndNameEditAction(endAction, entityId, path, resourceFile, true);
        }

        public void EndNewAssetCreationCanceled(string name)
        {
            string path = folder + "/" + name;
            if ((!String.IsNullOrEmpty(extension)) && (!path.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase)))
                path = path + extension;
            ProjectWindowUtil.EndNameEditAction(m_EndAction, m_EntityId, path, m_ResourceFile, false);
        }

        public bool IsCreatingNewAsset()
        {
            return !string.IsNullOrEmpty(m_Path);
        }
    }
} // end namespace UnityEditor
