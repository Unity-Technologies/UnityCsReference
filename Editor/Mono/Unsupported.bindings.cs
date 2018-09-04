// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine.Bindings;

namespace UnityEditor
{
    //*undocumented*
    [NativeHeader("Editor/Mono/Unsupported.bindings.h")]
    public static class Unsupported
    {
        // These MUST be synchronized with ScreenManager.h
        internal enum DisallowCursorLockReasons
        {
            None = 0,
            PlayPause = 1 << 0,
            Other = 1 << 1,
            Focus = 1 << 2,
            SizeMove = 1 << 3,
            ModalDialog = 1 << 4,
        };

        internal static extern Vector3 MakeNiceVector3(Vector3 vector);

        [FreeFunction]
        public static extern void CaptureScreenshotImmediate(string filePath, int x, int y, int width, int height);

        [FreeFunction("MenuController::ExtractSubmenusCommands")]
        public static extern string[] GetSubmenusCommands(string menuPath);

        public static extern Type GetTypeFromFullName(string fullName);

        // Extracts a list of all submenus that can be executed
        public static extern string[] GetSubmenus(string menuPath);

        // Extracts a list of all submenus that can be executed
        internal static extern string[] GetSubmenusLocalized(string menuPath);

        // Extracts a list of all submenus
        public static extern string[] GetSubmenusIncludingSeparators(string menuPath);

        public static extern void PrepareObjectContextMenu(UnityEngine.Object c, int contextUserData);

        public static bool IsDeveloperBuild()
        {
            return IsSourceBuild();
        }

        [FreeFunction]
        public static extern bool IsDeveloperMode();

        [FreeFunction]
        public static extern bool IsSourceBuild();

        public static extern bool IsBleedingEdgeBuild();

        public static extern bool IsDestroyScriptableObject(ScriptableObject target);

        public static extern bool IsNativeCodeBuiltInReleaseMode();

        [FreeFunction]
        public static extern string GetBaseUnityDeveloperFolder();

        public static extern void StopPlayingImmediately();

        [FreeFunction("GetSceneTracker().FlushDirty")]
        public static extern void SceneTrackerFlushDirty();

        [FreeFunction("GetScreenManager().SetAllowCursorHide")]
        public static extern void SetAllowCursorHide(bool allow);

        [FreeFunction("GetScreenManager().SetAllowCursorLock")]
        internal static extern void SetAllowCursorLock(bool allow, DisallowCursorLockReasons reasons);

        public static bool SetOverrideRenderSettings(Scene scene)
        {
            return SetOverrideRenderSettingsInternal(scene.handle);
        }

        internal static extern bool SetOverrideRenderSettingsInternal(int sceneHandle);


        public static extern void RestoreOverrideRenderSettings();

        [FreeFunction("GetRenderSettings().SetUseFogNoDirty")]
        public static extern void SetRenderSettingsUseFogNoDirty(bool fog);

        [FreeFunction("GetQualitySettings().SetShadowDistanceTemporarily")]
        public static extern void SetQualitySettingsShadowDistanceTemporarily(float distance);

        [FreeFunction("DeleteGameObjectSelection")]
        public static extern void DeleteGameObjectSelection();

        [FreeFunction]
        public static extern void CopyGameObjectsToPasteboard();

        [FreeFunction]
        public static extern void PasteGameObjectsFromPasteboard();

        [FreeFunction("AssetDatabase::GetSingletonAsset")]
        public static extern UnityEngine.Object GetSerializedAssetInterfaceSingleton(string className);

        [FreeFunction]
        public static extern void DuplicateGameObjectsUsingPasteboard();

        [FreeFunction]
        public static extern bool CopyComponentToPasteboard(Component component);

        [FreeFunction]
        public static extern bool PasteComponentFromPasteboard(GameObject go);

        [FreeFunction]
        public static extern bool PasteComponentValuesFromPasteboard(Component component);

        [FreeFunction("UnityEditor::StateMachineTransitionCopyPaste::HasParametersInPasteboard")]
        public static extern bool HasStateMachineTransitionDataInPasteboard();

        public static extern bool AreAllParametersInDestination(UnityEngine.Object transition, AnimatorController controller, List<string> missingParameters);

        public static extern bool DestinationHasCompatibleParameterTypes(UnityEngine.Object transition, AnimatorController controller, List<string> mismatchedParameters);

        public static extern bool CanPasteParametersToTransition(UnityEngine.Object transition, AnimatorController controller);

        [FreeFunction("UnityEditor::StateMachineTransitionCopyPaste::CopyParametersToPasteboard")]
        public static extern void CopyStateMachineTransitionParametersToPasteboard(UnityEngine.Object transition, AnimatorController controller);

        public static void PasteToStateMachineTransitionParametersFromPasteboard(UnityEngine.Object transition, AnimatorController controller, bool conditions, bool parameters)
        {
            Undo.RegisterCompleteObjectUndo(transition, "Paste to Transition");
            PasteToStateMachineTransitionParametersFromPasteboardInternal(transition, controller, conditions, parameters);
        }

        [FreeFunction("UnityEditor::StateMachineTransitionCopyPaste::PasteParametersFromPasteboard")]
        internal static extern void PasteToStateMachineTransitionParametersFromPasteboardInternal(UnityEngine.Object transition, AnimatorController controller, bool conditions, bool parameters);

        public static void CopyStateMachineDataToPasteboard(UnityEngine.Object stateMachineObject, AnimatorController controller, int layerIndex)
        {
            CopyStateMachineDataToPasteboard(new UnityEngine.Object[] {stateMachineObject}, null, new Vector3[] {new Vector3()}, controller, layerIndex);
        }

        [FreeFunction("UnityEditor::StateMachineCopyPaste::CopyDataToPasteboard")]
        internal static extern void CopyStateMachineDataToPasteboard(UnityEngine.Object[] stateMachineObjects, AnimatorStateMachine context, Vector3[] monoPositions, AnimatorController controller, int layerIndex);

        public static void PasteToStateMachineFromPasteboard(AnimatorStateMachine sm, AnimatorController controller, int layerIndex, Vector3 position)
        {
            Undo.RegisterCompleteObjectUndo(sm, "Paste to StateMachine");
            PasteToStateMachineFromPasteboardInternal(sm, controller, layerIndex, position);
        }

        [FreeFunction("UnityEditor::StateMachineCopyPaste::PasteDataFromPasteboard")]
        internal static extern void PasteToStateMachineFromPasteboardInternal(AnimatorStateMachine sm, AnimatorController controller, int layerIndex, Vector3 position);

        [FreeFunction("UnityEditor::StateMachineCopyPaste::HasDataInPasteboard")]
        public static extern bool HasStateMachineDataInPasteboard();

        [FreeFunction("SmartResetObject")]
        public static extern void SmartReset(UnityEngine.Object obj);

        [FreeFunction("ResolveSymlinks")]
        public static extern string ResolveSymlinks(string path);

        [FreeFunction("AssetDatabaseDeprecated::SetApplicationSettingCompressAssetsOnImport")]
        public static extern void SetApplicationSettingCompressAssetsOnImport(bool value);

        [StaticAccessor("AssetDatabaseDeprecated", StaticAccessorType.DoubleColon)]
        public static extern bool GetApplicationSettingCompressAssetsOnImport();

        // This function has always wrongly returned int but we have test data
        // that relies on this returning int, specifically the model importer test
        [FreeFunction("GetPersistentManager().GetLocalFileID")]
        public static extern int GetLocalIdentifierInFile(int instanceID);

        internal static extern UInt64 GetFileIDHint([NotNull] UnityEngine.Object obj);

        [NativeThrows]
        internal static extern UInt64 GenerateFileIDHint([NotNull] UnityEngine.Object obj);

        [FreeFunction]
        public static extern bool IsHiddenFile(string path);

        [FreeFunction]
        public static extern void ClearSkinCache();

        [StaticAccessor("GetRenderManager()", StaticAccessorType.Dot)]
        public static extern bool useScriptableRenderPipeline { get; set; }
    }
}
