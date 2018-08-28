// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public struct StageHandle : System.IEquatable<StageHandle>
    {
        private bool m_IsMainStage;
        private Scene m_CustomScene;

        internal bool isMainStage { get { return m_IsMainStage; } }
        internal Scene customScene { get { return m_CustomScene; } }

        public bool Contains(GameObject gameObject)
        {
            if (!IsValid())
                throw new System.Exception("Stage is not valid.");

            Scene goScene = gameObject.scene;
            if (goScene.IsValid() && EditorSceneManager.IsPreviewScene(goScene))
                return goScene == customScene;
            else
                return isMainStage;
        }

        public T FindComponentOfType<T>() where T : Component
        {
            if (!IsValid())
                throw new System.Exception("Stage is not valid.");

            T[] components = Resources.FindObjectsOfTypeAll<T>();
            if (isMainStage)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    T obj = components[i];
                    if (!EditorSceneManager.IsPreviewScene(obj.gameObject.scene))
                        return obj;
                }
            }
            else
            {
                for (int i = 0; i < components.Length; i++)
                {
                    T obj = components[i];
                    if (obj.gameObject.scene == customScene)
                        return obj;
                }
            }
            return null;
        }

        public T[] FindComponentsOfType<T>() where T : Component
        {
            if (!IsValid())
                throw new System.Exception("Stage is not valid.");

            T[] components = Resources.FindObjectsOfTypeAll<T>();
            List<T> componentList = new List<T>();
            if (isMainStage)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    T obj = components[i];
                    if (!EditorSceneManager.IsPreviewScene(obj.gameObject.scene))
                        componentList.Add(obj);
                }
            }
            else
            {
                for (int i = 0; i < components.Length; i++)
                {
                    T obj = components[i];
                    if (obj.gameObject.scene == customScene)
                        componentList.Add(obj);
                }
            }
            return componentList.ToArray();
        }

        // Use public API StageUtility.GetMainStage
        internal static StageHandle GetMainStageHandle()
        {
            return new StageHandle() { m_IsMainStage = true };
        }

        // Use public API StageUtility.GetCurrentStage
        internal static StageHandle GetCurrentStageHandle()
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
                return new StageHandle() { m_IsMainStage = true };
            else
                return new StageHandle() { m_CustomScene = prefabStage.scene };
        }

        // Use public API StageUtility.GetStage
        internal static StageHandle GetStageHandle(Scene scene)
        {
            if (scene.IsValid() && EditorSceneManager.IsPreviewScene(scene))
                return new StageHandle() { m_CustomScene = scene };
            else
                return new StageHandle() { m_IsMainStage = true };
        }

        public bool IsValid()
        {
            return m_IsMainStage ^ m_CustomScene.IsValid();
        }

        public static bool operator==(StageHandle s1, StageHandle s2)
        {
            return s1.Equals(s2);
        }

        public static bool operator!=(StageHandle s1, StageHandle s2)
        {
            return !s1.Equals(s2);
        }

        public override bool Equals(object other)
        {
            if (!(other is StageHandle))
                return false;

            StageHandle rhs = (StageHandle)other;
            return m_IsMainStage == rhs.m_IsMainStage && m_CustomScene == rhs.m_CustomScene;
        }

        public bool Equals(StageHandle other)
        {
            return m_IsMainStage == other.m_IsMainStage && m_CustomScene == other.m_CustomScene;
        }

        public override int GetHashCode()
        {
            if (m_IsMainStage)
                return 1;
            return m_CustomScene.GetHashCode();
        }
    }
}
