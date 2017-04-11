// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.Web;

namespace UnityEditor.Collaboration
{
    // Composes Softlock data into structures used by the UI.
    internal static class SoftLockUIData
    {
        private static Dictionary<string, Texture> s_ImageCache = new Dictionary<string, Texture>();
        private static Dictionary<SectionEnum, string> s_ImageNameCache = new Dictionary<SectionEnum, string>();

        public enum SectionEnum
        {
            None,
            Inspector,
            Scene
        }

        #region General

        // Provides the names of all additional users editing the asset
        // with the given 'assetGuid'.
        // Defaults to an empty list.
        public static List<string> GetLocksNamesOnAsset(string assetGuid)
        {
            List<SoftLock> softLocks = null;
            List<string> names = new List<string>();

            if (SoftLockData.TryGetLocksOnAssetGUID(assetGuid, out softLocks))
            {
                foreach (SoftLock softLock in softLocks)
                {
                    names.Add(softLock.displayName);
                }
            }
            return names;
        }

        #endregion
        #region Scene

        // Provides the names of all additional users editing the scene.
        // Defaults to an empty list.
        public static List<string> GetLocksNamesOnScene(Scene scene)
        {
            List<string> names = GetLockNamesOnScenePath(scene.path);
            return names;
        }

        public static List<string> GetLockNamesOnScenePath(string scenePath)
        {
            string assetGuid = AssetDatabase.AssetPathToGUID(scenePath);
            List<string> names = GetLocksNamesOnAsset(assetGuid);
            return names;
        }

        public static string GetSceneNameFromPath(string scenePath)
        {
            string name = "";
            if (null != scenePath)
            {
                name = scenePath;
            }
            return name;
        }

        // Provides the names of all additional users editing each scene.
        // Defaults to an empty list, and may contain empty sub-lists.
        public static List<List<string>> GetLockNamesOnScenes(List<Scene> scenes)
        {
            List<List<string>> namesByScene = new List<List<string>>();

            if (scenes == null)
            {
                return namesByScene;
            }

            foreach (Scene scene in scenes)
            {
                List<string> names = GetLocksNamesOnScene(scene);
                namesByScene.Add(names);
            }
            return namesByScene;
        }

        // For each iteration, returns the pair (scene name : list of other users' names).
        public static IEnumerable<KeyValuePair<string, List<string>>> GetLockNamesOnOpenScenes()
        {
            if (CollabAccess.Instance.IsServiceEnabled())
            {
                for (int sceneIndex = 0; sceneIndex < EditorSceneManager.sceneCount; sceneIndex++)
                {
                    Scene scene = SceneManager.GetSceneAt(sceneIndex);
                    List<string> names = GetLocksNamesOnScene(scene);
                    string sceneName = scene.name;
                    if (String.IsNullOrEmpty(sceneName))
                    {
                        // Default for unnamed scenes.
                        sceneName = "Untitled";
                    }
                    KeyValuePair<string, List<string>> sceneData = new KeyValuePair<string, List<string>>(sceneName, names);
                    yield return sceneData;
                }
            }
        }

        public static int CountOfLocksOnOpenScenes()
        {
            int count = 0;

            foreach (KeyValuePair<string, List<string>> sceneData in GetLockNamesOnOpenScenes())
            {
                count += sceneData.Value.Count;
            }
            return count;
        }

        #endregion
        #region Game Object

        // The usernames of additional people editing the given 'objectWithGUID'.
        // Defaults to an empty list.
        public static List<string> GetLockNamesOnObject(UnityEngine.Object objectWithGUID)
        {
            string assetGUID = null;
            AssetAccess.TryGetAssetGUIDFromObject(objectWithGUID, out assetGUID);
            List<string> names = GetLocksNamesOnAsset(assetGUID);
            return names;
        }

        #endregion
        #region Icons

        // Retrieves the appropriate icon for the softlock area in the
        // editor. 'GUID' is used to determine the soft lock count to display.
        // Defaults to null.
        public static Texture GetIconForGUID(SectionEnum section, string GUID)
        {
            Texture texture = null;
            int count = 0;
            if (SoftLockData.TryGetSoftlockCount(GUID, out count))
            {
                texture = GetIconForSection(section, count);
            }
            return texture;
        }

        // The icon for the particular section in the editor.
        // Defaults to null.
        public static Texture GetIconForSection(SectionEnum section, int lockCount)
        {
            string iconName = IconNameForSection(section, lockCount);
            Texture texture = GetIconForName(iconName);
            return texture;
        }

        private static string IconNameForSection(SectionEnum section, int lockCount)
        {
            string iconName;
            if (!s_ImageNameCache.TryGetValue(section, out iconName))
            {
                iconName = String.Format("Softlock{0}{1}", section.ToString(), ".png");
                s_ImageNameCache.Add(section, iconName);
            }
            return iconName;
        }

        private static Texture GetIconForName(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return null;
            }

            Texture texture;
            // Note: a previous texture may have been destroyed
            // by the system on the c++ side.
            if (!s_ImageCache.TryGetValue(fileName, out texture) || texture == null)
            {
                texture = EditorGUIUtility.LoadIconRequired(fileName) as Texture;
                s_ImageCache.Remove(fileName);
                s_ImageCache.Add(fileName, texture);
            }
            return texture;
        }

        #endregion
    }
}

