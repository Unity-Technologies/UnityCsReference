// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    internal enum PresetFileLocation { PreferencesFolder, ProjectFolder } // ProjectFolder: We look in all Editor folders in Assets

    internal static class PresetLibraryLocations
    {
        public static string defaultLibraryLocation
        {
            get { return GetDefaultFilePathForFileLocation(PresetFileLocation.PreferencesFolder); }
        }

        public static string defaultPresetLibraryPath
        {
            get { return Path.Combine(defaultLibraryLocation, defaultLibraryName); }
        }

        public static string defaultLibraryName
        {
            get { return "Default"; }
        }

        public static List<string> GetAvailableFilesWithExtensionOnTheHDD(PresetFileLocation fileLocation, string fileExtensionWithoutDot)
        {
            List<string> folderPaths = GetDirectoryPaths(fileLocation);
            List<string> files = GetFilesWithExentionFromFolders(folderPaths, fileExtensionWithoutDot);
            for (int i = 0; i < files.Count; ++i)
                files[i] = ConvertToUnitySeperators(files[i]);
            return files;
        }

        public static string GetDefaultFilePathForFileLocation(PresetFileLocation fileLocation)
        {
            switch (fileLocation)
            {
                case PresetFileLocation.PreferencesFolder:
                    return InternalEditorUtility.unityPreferencesFolder + "/Presets/";

                case PresetFileLocation.ProjectFolder:
                    return "Assets/Editor/";

                default:
                    Debug.LogError("Enum not handled!");
                    return "";
            }
        }

        static List<string> GetDirectoryPaths(PresetFileLocation fileLocation)
        {
            List<string> folderPaths = new List<string>();
            switch (fileLocation)
            {
                case PresetFileLocation.PreferencesFolder:
                    folderPaths.Add(GetDefaultFilePathForFileLocation(PresetFileLocation.PreferencesFolder));
                    break;

                case PresetFileLocation.ProjectFolder:
                    string[] editorFolders = Directory.GetDirectories("Assets/", "Editor", SearchOption.AllDirectories);
                    folderPaths.AddRange(editorFolders);
                    break;

                default:
                    Debug.LogError("Enum not handled!");
                    break;
            }

            return folderPaths;
        }

        static List<string> GetFilesWithExentionFromFolders(List<string> folderPaths, string fileExtensionWithoutDot)
        {
            // First get all potential files
            var files = new List<string>();
            foreach (string editorFolder in folderPaths)
            {
                string[] filePaths = Directory.GetFiles(editorFolder, "*." + fileExtensionWithoutDot);
                files.AddRange(filePaths);
            }
            return files;
        }

        public static PresetFileLocation GetFileLocationFromPath(string path)
        {
            if (path.Contains(InternalEditorUtility.unityPreferencesFolder))
                return PresetFileLocation.PreferencesFolder;
            if (path.Contains("Assets/"))
                return PresetFileLocation.ProjectFolder;

            Debug.LogError("Could not determine preset file location type " + path);
            return PresetFileLocation.ProjectFolder;
        }

        static string ConvertToUnitySeperators(string path)
        {
            return path.Replace('\\', '/');
        }

        static public string GetParticleCurveLibraryExtension(bool singleCurve, bool signedRange)
        {
            string extension = "particle";
            if (singleCurve)
                extension += "Curves";
            else
                extension += "DoubleCurves";

            if (signedRange)
                extension += "Signed";
            else
                extension += "";

            return extension;
        }

        static public string GetCurveLibraryExtension(bool normalized)
        {
            if (normalized)
                return "curvesNormalized";
            return "curves";
        }
    }


    internal class PresetLibraryManager : ScriptableSingleton<PresetLibraryManager>
    {
        static string s_LastError = null;
        private List<LibraryCache> m_LibraryCaches = new List<LibraryCache>();

        private HideFlags libraryHideFlag
        {
            get { return HideFlags.DontSave; } // Use of DontSave prevents library from being nulled when going out of playmode
        }

        // Returns lists of filepaths for libraries with a given extension found on the HDD
        public void GetAvailableLibraries<T>(ScriptableObjectSaveLoadHelper<T> helper, out List<string> preferencesLibs, out List<string> projectLibs) where T : ScriptableObject
        {
            preferencesLibs = PresetLibraryLocations.GetAvailableFilesWithExtensionOnTheHDD(PresetFileLocation.PreferencesFolder, helper.fileExtensionWithoutDot);
            projectLibs = PresetLibraryLocations.GetAvailableFilesWithExtensionOnTheHDD(PresetFileLocation.ProjectFolder, helper.fileExtensionWithoutDot);
        }

        string GetLibaryNameFromPath(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        public T CreateLibrary<T>(ScriptableObjectSaveLoadHelper<T> helper, string presetLibraryPathWithoutExtension) where T : ScriptableObject
        {
            string libraryName = GetLibaryNameFromPath(presetLibraryPathWithoutExtension);
            if (!InternalEditorUtility.IsValidFileName(libraryName))
            {
                string invalid = InternalEditorUtility.GetDisplayStringOfInvalidCharsOfFileName(libraryName);
                if (invalid.Length > 0)
                    s_LastError = string.Format("A library filename cannot contain the following character{0}:  {1}", invalid.Length > 1 ? "s" : "", invalid);
                else
                    s_LastError = "Invalid filename";
                return null;
            }

            if (GetLibrary(helper, presetLibraryPathWithoutExtension) != null)
            {
                s_LastError = "Library '" + libraryName + "' already exists! Ensure a unique name.";
                return null;
            }

            T library = helper.Create();
            library.hideFlags = libraryHideFlag;
            LibraryCache set = GetPresetLibraryCache(helper.fileExtensionWithoutDot);
            set.loadedLibraries.Add(library);
            set.loadedLibraryIDs.Add(presetLibraryPathWithoutExtension);
            s_LastError = null;
            return library;
        }

        public T GetLibrary<T>(ScriptableObjectSaveLoadHelper<T> helper, string presetLibraryPathWithoutExtension) where T : ScriptableObject
        {
            LibraryCache set = GetPresetLibraryCache(helper.fileExtensionWithoutDot);

            // Did we already load the lib
            for (int i = 0; i < set.loadedLibraryIDs.Count; ++i)
            {
                if (set.loadedLibraryIDs[i] == presetLibraryPathWithoutExtension)
                {
                    if (set.loadedLibraries[i] != null)
                        return set.loadedLibraries[i] as T;
                    else
                    {
                        // The library has been destroyed. Remove it from the lists so it can be reloaded
                        set.loadedLibraries.RemoveAt(i);
                        set.loadedLibraryIDs.RemoveAt(i);
                        Debug.LogError("Invalid library detected: Reload " + set.loadedLibraryIDs[i] + " from HDD");
                        break;
                    }
                }
            }

            // Debug.Log ("Not loaded yet " + typeof(T));

            // Can we find on the hdd
            T library = helper.Load(presetLibraryPathWithoutExtension);

            if (library != null)
            {
                library.hideFlags = libraryHideFlag; // ensure correct hideflag with pre 4.3 versions
                set.loadedLibraries.Add(library);
                set.loadedLibraryIDs.Add(presetLibraryPathWithoutExtension);
                return library;
            }

            // Debug.Log ("Not found on hdd");

            return null;
        }

        public void UnloadAllLibrariesFor<T>(ScriptableObjectSaveLoadHelper<T> helper) where T : ScriptableObject
        {
            for (int i = 0; i < m_LibraryCaches.Count; ++i)
            {
                if (m_LibraryCaches[i].identifier == helper.fileExtensionWithoutDot)
                {
                    m_LibraryCaches[i].UnloadScriptableObjects();
                    m_LibraryCaches.RemoveAt(i);
                    break;
                }
            }
        }

        public void SaveLibrary<T>(ScriptableObjectSaveLoadHelper<T> helper, T library, string presetLibraryPathWithoutExtension) where T : ScriptableObject
        {
            bool fileExistedBeforeSaving = File.Exists(presetLibraryPathWithoutExtension + "." + helper.fileExtensionWithoutDot);

            helper.Save(library, presetLibraryPathWithoutExtension);

            if (!fileExistedBeforeSaving)
                AssetDatabase.Refresh();
        }

        public string GetLastError()
        {
            string errorString = s_LastError;
            s_LastError = null;
            return errorString;
        }

        private LibraryCache GetPresetLibraryCache(string identifier)
        {
            foreach (LibraryCache libraryCache in m_LibraryCaches)
                if (libraryCache.identifier == identifier)
                    return libraryCache;

            // Add if not found
            LibraryCache set = new LibraryCache(identifier);
            m_LibraryCaches.Add(set);
            return set;
        }

        private class LibraryCache
        {
            string m_Identifier; // Identifier for a group of libraries. For now its the file extension

            // Should have been a Dictonary but we cannot serialize those properly yet...
            List<ScriptableObject> m_LoadedLibraries = new List<ScriptableObject>(); // 1:1 with m_LoadedLibraryIDs
            List<string> m_LoadedLibraryIDs = new List<string>(); // 1:1 with m_LoadedLibraries

            // Interface
            public string identifier { get { return m_Identifier; } }
            public List<ScriptableObject> loadedLibraries { get { return m_LoadedLibraries; } }
            public List<string> loadedLibraryIDs { get { return m_LoadedLibraryIDs; } } // List of paths without extension
            public void UnloadScriptableObjects()
            {
                foreach (ScriptableObject sobj in m_LoadedLibraries)
                    ScriptableObject.DestroyImmediate(sobj);
                m_LoadedLibraries.Clear();
                m_LoadedLibraryIDs.Clear();
            }

            public LibraryCache(string identifier)
            {
                m_Identifier = identifier;
            }
        }
    }
}
