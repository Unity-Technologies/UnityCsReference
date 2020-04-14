// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public abstract class Stage : ScriptableObject
    {
        static List<Stage> s_AllStages = new List<Stage>();
        internal static ReadOnlyCollection<Stage> allStages {  get { return s_AllStages.AsReadOnly(); } }

        // Stage interface

        // Called when the user have accepted to switch away from previous stage. This method should load stage contents.
        // Should return 'true' if the stage was opened succesfully otherwise 'false'.
        protected internal abstract bool OnOpenStage();

        // Called when the stage is destroyed (when OnDestroy() is called). Should unload the contents of the stage.
        // Only called if OnOpenStage was called.
        protected abstract void OnCloseStage();

        // Called when returning to a previous open stage (e.g by clicking a non-current breadcrumb)
        protected internal virtual void OnReturnToStage() {}

        internal bool opened { get; set; }

        internal virtual bool isValid { get { return true; } }

        internal virtual bool isAssetMissing { get { return false; } }

        public virtual string assetPath { get { return string.Empty; } }

        internal abstract int sceneCount { get; }
        internal abstract Scene GetSceneAt(int index);

        internal virtual bool SupportsSaving() { return false; }
        internal virtual bool hasUnsavedChanges { get { return false; } }
        internal virtual bool Save()
        {
            if (!SupportsSaving() || !hasUnsavedChanges)
                return true;
            throw new System.NotImplementedException("This Stage returns true for SupportsSaving() but has not implemented an override for the Save() method.");
        }

        internal virtual bool SaveAsNew()
        {
            if (!SupportsSaving())
                return true;
            throw new System.NotImplementedException("This Stage returns true for SupportsSaving() but has not implemented an override for the SaveAsNew() method.");
        }

        // Transient state since it is set every time we switch stage
        internal bool setSelectionAndScrollWhenBecomingCurrentStage { get; set; } = true;

        internal virtual string GetErrorMessage()
        {
            return null;
        }

        protected internal abstract GUIContent CreateHeaderContent();

        internal virtual BreadcrumbBar.Item CreateBreadcrumbItem()
        {
            var history = StageNavigationManager.instance.stageHistory;
            bool isLastCrumb = this == history.Last();
            var style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBold : BreadcrumbBar.DefaultStyles.label;

            return new BreadcrumbBar.Item
            {
                content = CreateHeaderContent(),
                guistyle = style,
                userdata = this,
                separatorstyle = BreadcrumbBar.SeparatorStyle.Line
            };
        }

        internal virtual ulong GetSceneCullingMask() { return EditorSceneManager.DefaultSceneCullingMask; }

        public virtual StageHandle stageHandle
        {
            get { return StageHandle.GetMainStageHandle(); }
        }

        public virtual ulong GetCombinedSceneCullingMaskForCamera() { return GetSceneCullingMask(); }

        internal virtual Stage GetContextStage() { return this; }

        internal virtual Color GetBackgroundColor() { return SceneView.kSceneViewBackground.Color; }

        // Called before and after the Scene view renders.
        internal virtual void OnPreSceneViewRender(SceneView sceneView) {}
        internal virtual void OnPostSceneViewRender(SceneView sceneView) {}

        // Called when new stage is created, after script reloads, and possibly at other times by the stage itself.
        internal abstract void SyncSceneViewToStage(SceneView sceneView);
        internal abstract void SyncSceneHierarchyToStage(SceneHierarchyWindow sceneHierarchyWindow);

        internal virtual void SaveHierarchyState(SceneHierarchyWindow hierarchyWindow) {}
        internal virtual void LoadHierarchyState(SceneHierarchyWindow hierarchyWindow) {}

        // Called after respective sync methods first time this stage is opened in this window.
        protected internal virtual void OnFirstTimeOpenStageInSceneView(SceneView sceneView) {}
        internal virtual void OnFirstTimeOpenStageInSceneHierachyWindow(SceneHierarchyWindow sceneHierarchyWindow) {}

        internal virtual void OnControlsGUI(SceneView sceneView) {}

        internal virtual void Tick()
        {
        }

        // Called on the the current stage when trying to switch to new stage. Should return true if it OK to continue to switch away
        // from current stage. False if not ok to continue.
        internal virtual bool AskUserToSaveModifiedStageBeforeSwitchingStage()
        {
            return true;
        }

        internal abstract void PlaceGameObjectInStage(GameObject rootGameObject);

        // Used for writing state files to HDD
        // Should typically be a persistent unique hash per content shown.
        // For stages that use the assetPath property, the hash is by default based on the asset GUID.
        protected internal virtual Hash128 GetHashForStateStorage()
        {
            if (!string.IsNullOrEmpty(assetPath))
                return Hash128.Compute(AssetDatabase.AssetPathToGUID(assetPath));

            // If no assetPath is specified, the default behavior is that
            // every stage of the same type will reuse the same state files.
            return new Hash128();
        }

        // Lifetime callbacks from ScriptableObject
        private void Awake()
        {
        }

        private void OnDestroy()
        {
            if (opened)
            {
                OnCloseStage();
                opened = false;
            }
        }

        protected virtual void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            s_AllStages.Add(this);
        }

        protected virtual void OnDisable()
        {
            s_AllStages.Remove(this);
        }

        public T FindComponentOfType<T>() where T : Component
        {
            return stageHandle.FindComponentOfType<T>();
        }

        public T[] FindComponentsOfType<T>() where T : Component
        {
            return stageHandle.FindComponentsOfType<T>();
        }
    }

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
                    if (!EditorUtility.IsPersistent(obj) && !EditorSceneManager.IsPreviewScene(obj.gameObject.scene))
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
                    if (!EditorUtility.IsPersistent(obj) && !EditorSceneManager.IsPreviewScene(obj.gameObject.scene))
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
            if (StageNavigationManager.instance != null && StageNavigationManager.instance.currentStage != null)
                return StageNavigationManager.instance.currentStage.stageHandle;
            return new StageHandle() { m_IsMainStage = true };
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
