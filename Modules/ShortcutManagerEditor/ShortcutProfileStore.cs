// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    interface IShortcutProfileStore
    {
        bool ValidateProfileId(string id);
        bool ProfileExists(string id);
        void SaveShortcutProfileJson(string id, string json);
        string LoadShortcutProfileJson(string id);
        void DeleteShortcutProfile(string id);
        IEnumerable<string> GetAllProfileIds();
    }

    class ShortcutProfileStore : IShortcutProfileStore
    {
        public bool ValidateProfileId(string id)
        {
            return !string.IsNullOrEmpty(id) &&
                id.Length <= 127 &&
                id.IndexOfAny(Path.GetInvalidFileNameChars()) == -1 &&
                id != ShortcutManager.defaultProfileId &&
                id.IndexOfAny("_%#^".ToCharArray()) == -1 &&
                id.Trim(" ".ToCharArray()).Length == id.Length;
        }

        public bool ProfileExists(string id)
        {
            return File.Exists(GetPathForProfile(id));
        }

        public void SaveShortcutProfileJson(string id, string json)
        {
            var path = GetPathForProfile(id);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }

        public string LoadShortcutProfileJson(string id)
        {
            var path = GetPathForProfile(id);
            return File.ReadAllText(path);
        }

        public string[] LoadAllShortcutProfilesJsonFromDisk()
        {
            string[] profilePaths = GetAllShortcutProfilePaths().ToArray();
            string[] filesJson = new string[profilePaths.Length];
            for (int i = 0; i < profilePaths.Length; ++i)
            {
                filesJson[i] = File.ReadAllText(profilePaths[i]);
            }

            return filesJson;
        }

        public IEnumerable<string> GetAllProfileIds()
        {
            var profilePaths = GetAllShortcutProfilePaths();
            var profileIds = new List<string>(profilePaths.Count());
            foreach (var profilePath in profilePaths)
            {
                profileIds.Add(Path.GetFileNameWithoutExtension(profilePath));
            }
            return profileIds;
        }

        public void DeleteShortcutProfile(string id)
        {
            File.Delete(Path.Combine(GetShortcutFolderPath(), id + ".shortcut"));
        }

        public static string GetShortcutFolderPath()
        {
            return Paths.Combine(InternalEditorUtility.unityPreferencesFolder, "shortcuts", ModeService.currentId);
        }

        static string GetPathForProfile(string id)
        {
            return Paths.Combine(GetShortcutFolderPath(), id + ".shortcut");
        }

        static IEnumerable<string> GetAllShortcutProfilePaths()
        {
            var shortcutsFolderPath = GetShortcutFolderPath();
            if (ModeService.currentId == ModeService.k_DefaultModeId)
            {
                var legacyShortcutFolder = Paths.Combine(InternalEditorUtility.unityPreferencesFolder, "shortcuts");
                if (System.IO.Directory.Exists(legacyShortcutFolder))
                {
                    var legacyShortcutFiles = System.IO.Directory.GetFiles(legacyShortcutFolder, "*.shortcut", System.IO.SearchOption.TopDirectoryOnly).ToArray();
                    if (legacyShortcutFiles.Length > 0 && !System.IO.Directory.Exists(shortcutsFolderPath))
                        System.IO.Directory.CreateDirectory(shortcutsFolderPath);
                    foreach (var shortcutPath in legacyShortcutFiles)
                    {
                        var fileName = Path.GetFileName(shortcutPath);
                        var dst = Path.Combine(shortcutsFolderPath, fileName);
                        if (!File.Exists(dst))
                        {
                            FileUtil.CopyFileIfExists(shortcutPath, dst, false);
                        }
                    }
                }
            }

            if (!System.IO.Directory.Exists(shortcutsFolderPath))
                return Enumerable.Empty<string>();

            return System.IO.Directory.GetFiles(shortcutsFolderPath, "*.shortcut", System.IO.SearchOption.TopDirectoryOnly);
        }
    }
}
