// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using System.IO;

namespace UnityEditor
{
    public enum SaveType { Binary, Text }


    // Class for making it easy to save and load ScriptableObjects manually (i.e without using the AssetDatabase)

    class ScriptableObjectSaveLoadHelper<T> where T : ScriptableObject
    {
        public string fileExtensionWithoutDot { get; private set; }
        private SaveType saveType { get; set; }

        public ScriptableObjectSaveLoadHelper(string fileExtensionWithoutDot, SaveType saveType)
        {
            this.saveType = saveType;
            this.fileExtensionWithoutDot = fileExtensionWithoutDot.TrimStart('.'); // Ensure no dot
        }

        // If 'filePath' does not include an extension the local 'fileExtensionWithoutDot' is used.
        public T Load(string filePath)
        {
            filePath = AppendFileExtensionIfNeeded(filePath);

            // Try to load
            if (!string.IsNullOrEmpty(filePath))
            {
                Object[] objects = InternalEditorUtility.LoadSerializedFileAndForget(filePath);
                if (objects != null && objects.Length > 0)
                    return objects[0] as T;
            }

            return null;
        }

        public T Create()
        {
            T t = ScriptableObject.CreateInstance<T>();
            return t;
        }

        // If 'filePath' does not include an extension the local 'fileExtensionWithoutDot' is used.
        public void Save(T t, string filePath)
        {
            if (t == null)
            {
                Debug.LogError("Cannot save scriptableObject: its null!");
                return;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("Invalid path: '" + filePath + "'");
                return;
            }

            // Ensure folder exists
            string folderPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            filePath = AppendFileExtensionIfNeeded(filePath);

            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { t }, filePath, saveType == SaveType.Text);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", fileExtensionWithoutDot, saveType);
        }

        string AppendFileExtensionIfNeeded(string path)
        {
            if (!Path.HasExtension(path) && !string.IsNullOrEmpty(fileExtensionWithoutDot))
                return path + "." + fileExtensionWithoutDot;
            return path;
        }
    }
}
