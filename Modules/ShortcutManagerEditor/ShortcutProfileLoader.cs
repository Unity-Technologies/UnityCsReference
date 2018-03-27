// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Utils;
using UnityEditorInternal;

namespace UnityEditor.ShortcutManagement
{
    interface IShortcutProfileLoader
    {
        string LoadShortcutProfileJson(string id);
        bool ProfileExists(string id);

        void SaveShortcutProfileJson(string id, string json);
    }

    class ShortcutProfileLoader : IShortcutProfileLoader
    {
        public string LoadShortcutProfileJson(string id)
        {
            var path = GetPathForProfile(id);
            return File.ReadAllText(path);
        }

        static string GetPathForProfile(string id)
        {
            //TODO: review the usage of internal API and review path
            return Paths.Combine(InternalEditorUtility.unityPreferencesFolder, "shortcuts", id + ".shortcut");
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
    }
}
