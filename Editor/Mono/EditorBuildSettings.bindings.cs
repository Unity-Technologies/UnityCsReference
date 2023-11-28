// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Object = UnityEngine.Object;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace UnityEditor
{
    [Serializable]
    [NativeHeader("Editor/Src/EditorBuildSettings.h")]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public class EditorBuildSettingsScene : IComparable
    {
        [NativeName("enabled"), SerializeField]
        bool m_enabled;
        [NativeName("path"), SerializeField]
        string m_path;
        [NativeName("guid"), SerializeField]
        GUID m_guid;
        public EditorBuildSettingsScene() {}
        public EditorBuildSettingsScene(string path, bool enabled)
        {
            m_path = path.Replace("\\", "/");
            m_enabled = enabled;
            GUID.TryParse(AssetDatabase.AssetPathToGUID(path), out m_guid);
        }

        public EditorBuildSettingsScene(GUID guid, bool enabled)
        {
            m_guid = guid;
            m_enabled = enabled;
            m_path = AssetDatabase.GUIDToAssetPath(guid.ToString());
        }

        public bool enabled { get { return m_enabled; } set { m_enabled = value; } }
        public string path { get { return m_path; } set { m_path = value.Replace("\\", "/"); } }
        public GUID guid { get { return m_guid; } set { m_guid = value; } }
        public static string[] GetActiveSceneList(EditorBuildSettingsScene[] scenes)
        {
            return scenes.Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path)).Select(scene => scene.path).ToArray();
        }

        public int CompareTo(object obj)
        {
            if (obj is EditorBuildSettingsScene)
            {
                EditorBuildSettingsScene temp = (EditorBuildSettingsScene)obj;
                return temp.path.CompareTo(path);
            }
            throw new ArgumentException("object is not a EditorBuildSettingsScene");
        }

        [RequiredByNativeCode, UsedImplicitly]
        private static void DeconstructArrayElement(EditorBuildSettingsScene[] arr, int index, out bool enabled, out string path, out GUID guid)
        {
            var item = arr[index];
            enabled = item.enabled;
            path = item.path;
            guid = item.guid;
        }
    }

    [NativeHeader("Editor/Src/EditorBuildSettings.h")]
    public partial class EditorBuildSettings : UnityEngine.Object
    {
        private EditorBuildSettings() {}
        public static event Action sceneListChanged;
        [RequiredByNativeCode]
        internal static void SceneListChanged()
        {
            if (sceneListChanged != null)
                sceneListChanged();
        }

        public static EditorBuildSettingsScene[] scenes
        {
            get
            {
                if (EditorUserBuildSettings.IsBuildProfileWorkflowEnabled()
                    && BuildProfileContext.instance.activeProfile is not null)
                {
                    return BuildProfileContext.instance.activeProfile.scenes;
                }

                return GetEditorBuildSettingsScenes();
            }
            set => SetEditorBuildSettingsScenes(value);
        }

        [RequiredByNativeCode]
        static EditorBuildSettingsScene[] GetActiveBuildProfileSceneList()
        {
            if (BuildProfileContext.instance.activeProfile is null)
                return null;

            return BuildProfileContext.instance.activeProfile.scenes;
        }

        static extern EditorBuildSettingsScene[] GetEditorBuildSettingsScenes();
        static extern void SetEditorBuildSettingsScenes(EditorBuildSettingsScene[] scenes);

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static extern void SetEditorBuildSettingsSceneIgnoreProfile(EditorBuildSettingsScene[] scenes);

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static extern EditorBuildSettingsScene[] GetEditorBuildSettingsSceneIgnoreProfile();

        enum ConfigObjectResult
        {
            Succeeded,
            FailedEntryNotFound,
            FailedNullObj,
            FailedNonPersistedObj,
            FailedEntryExists,
            FailedTypeMismatch
        }

        public static extern bool UseParallelAssetBundleBuilding { get; set; }

        [NativeMethod("AddConfigObject")]
        static extern ConfigObjectResult AddConfigObjectInternal(string name, Object obj, bool overwrite);
        public static extern bool RemoveConfigObject(string name);
        public static extern string[] GetConfigObjectNames();
        static extern Object GetConfigObject(string name);
        public static void AddConfigObject(string name, Object obj, bool overwrite)
        {
            var result = AddConfigObjectInternal(name, obj, overwrite);
            if (result == ConfigObjectResult.Succeeded)
                return;
            switch (result)
            {
                case ConfigObjectResult.FailedEntryExists: throw new Exception("Config object with name '" + name + "' already exists.");
                case ConfigObjectResult.FailedNonPersistedObj: throw new Exception("Cannot add non-persisted config object with name '" + name + "'.");
                case ConfigObjectResult.FailedNullObj: throw new Exception("Cannot add null config object with name '" + name + "'.");
            }
        }

        public static bool TryGetConfigObject<T>(string name, out T result) where T : Object
        {
            result = GetConfigObject(name) as T;
            return result != null;
        }
    }
}
