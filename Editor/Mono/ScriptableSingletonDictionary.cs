// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.IO;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Use the LibraryFolderPathAttribute when you want to have your scriptable singleton dictionary
    // to persist between unity sessions.
    // Example: [LibraryFolderPathAttribute("EditorWindows")]
    [AttributeUsage(AttributeTargets.Class)]
    class LibraryFolderPathAttribute : Attribute
    {
        public string folderPath { get; set; }
        public LibraryFolderPathAttribute(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                Debug.LogError("Invalid relative path! (its null or empty)");
                return;
            }

            // We do not want a slash as first char.
            if (relativePath[0] == '/')
                throw new ArgumentException("Folder relative path cannot start with a slash.");

            folderPath = "Library/" + relativePath;
        }
    }

    internal abstract class ScriptableSingletonDictionary<TDerived, TValue> : ScriptableObject
        where TDerived : ScriptableObject
        where TValue : ScriptableObject
    {
        private static TDerived s_Instance;
        static readonly string k_Extension = ".pref";

        protected string m_PreferencesFileName;

        public static TDerived instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = ScriptableObject.CreateInstance<TDerived>();
                    s_Instance.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Instance;
            }
        }

        public TValue this[string preferencesFileName]
        {
            get
            {
                return Load(preferencesFileName);
            }
        }

        private TValue CreateNewValue()
        {
            TValue value = ScriptableObject.CreateInstance<TValue>();
            value.hideFlags |= HideFlags.HideAndDontSave;
            return value;
        }

        private string GetProjectRelativePath(string file)
        {
            return GetFolderPath() + "/" + file + k_Extension;
        }

        // Save() should be called whenever the user of this data store
        // believes the data might have changed and needs to be updated
        // in the pref file. Otherwise, Save() will only be called when
        // switching between pref files by accessing with different keys.
        public void Save(string preferencesFileName, TValue value)
        {
            const bool saveAsText = true;

            Debug.Assert(preferencesFileName != null && value != null, "Should always have valid key/values.");
            if (string.IsNullOrEmpty(preferencesFileName) || value == null)
                return;

            // if there is no key the object does not exist on disk,
            // so there is no guid associated with it
            string file = preferencesFileName;
            if (string.IsNullOrEmpty(file))
                return;

            // make sure the path exists or file write will fail
            string fullPath = Application.dataPath + "/../" + GetFolderPath();
            if (!System.IO.Directory.Exists(fullPath))
                System.IO.Directory.CreateDirectory(fullPath);

            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { value }, GetProjectRelativePath(file), saveAsText);
        }

        public void Clear(string preferencesFileName)
        {
            string fullPath = Application.dataPath + "/../" + GetProjectRelativePath(preferencesFileName);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        private TValue Load(string preferencesFileName)
        {
            TValue value = null;
            string file = preferencesFileName;
            if (!string.IsNullOrEmpty(file))
            {
                var objects = InternalEditorUtility.LoadSerializedFileAndForget(GetProjectRelativePath(file));
                if (objects != null && objects.Length > 0)
                {
                    value = objects[0] as TValue;
                    if (value != null)
                        value.hideFlags |= HideFlags.HideAndDontSave;
                }
            }

            m_PreferencesFileName = preferencesFileName;
            return value ?? CreateNewValue();
        }

        private string GetFolderPath()
        {
            Type type = this.GetType();
            object[] attributes = type.GetCustomAttributes(true);
            foreach (object attr in attributes)
            {
                if (attr is LibraryFolderPathAttribute)
                {
                    LibraryFolderPathAttribute f = attr as LibraryFolderPathAttribute;
                    return f.folderPath;
                }
            }

            // The folder path attribute is required.
            throw new ArgumentException("The LibraryFolderPathAttribute[] attribute is required for this class.");
        }
    }
}
