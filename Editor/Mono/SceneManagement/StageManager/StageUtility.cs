// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEditor.SceneManagement
{
    public static partial class StageUtility
    {
        public static bool IsGameObjectRenderedByCamera(GameObject gameObject, Camera camera)
        {
            return IsGameObjectRenderedByCameraInternal(gameObject, camera);
        }

        internal static void SetSceneToRenderInStage(Scene scene, Stage stage)
        {
            if (!stage.IsValid())
                throw new System.ArgumentException("Stage is not valid.", nameof(stage));
            if (stage.isMainStage)
                SetSceneToRenderInMainStageInternal(scene.handle);
            else
                SetSceneToRenderInSameStageAsOtherSceneInternal(scene.handle, stage.customScene.handle);
        }

        public static Stage GetCurrentStage()
        {
            return Stage.GetCurrentStage();
        }

        public static Stage GetMainStage()
        {
            return Stage.GetMainStage();
        }

        public static Stage GetStage(GameObject gameObject)
        {
            return Stage.GetStage(gameObject.scene);
        }

        public static Stage GetStage(Scene scene)
        {
            return Stage.GetStage(scene);
        }

        public static void GoToMainStage()
        {
            StageNavigationManager.instance.GoToMainStage(false, StageNavigationManager.Analytics.ChangeType.GoToMainViaUnknown);
        }

        public static void GoBackToPreviousStage()
        {
            StageNavigationManager.instance.NavigateBack(StageNavigationManager.Analytics.ChangeType.NavigateBackViaUnknown);
        }

        public static void PlaceGameObjectInCurrentStage(GameObject gameObject)
        {
            StageNavigationManager.instance.PlaceGameObjectInCurrentStage(gameObject);
        }

        internal static string CreateWindowAndStageIdentifier(string windowGUID, StageNavigationItem stage)
        {
            // Limit guids to prevent long file names on Windows
            string windowID = windowGUID.Substring(0, 6);
            string stageID = stage.isMainStage ? "mainStage" : AssetDatabase.AssetPathToGUID(stage.prefabAssetPath).Substring(0, 18);
            return windowID + "-" + stageID;
        }
    }
}
