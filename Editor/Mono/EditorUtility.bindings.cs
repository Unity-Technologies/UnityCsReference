// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/EditorUtility.bindings.h")]
    [NativeHeader("Editor/Mono/MonoEditorUtility.h")]
    [NativeHeader("Runtime/Shaders/ShaderImpl/ShaderUtilities.h")]
    partial class EditorUtility
    {
        public static extern string OpenFilePanel(string title, string directory, string extension);

        private static extern string Internal_OpenFilePanelWithFilters(string title, string directory, string[] filters);
        public static string OpenFilePanelWithFilters(string title, string directory, string[] filters)
        {
            if (filters.Length % 2 != 0)
                throw new Exception("Filters must be declared in pairs { \"FileType\", \"FileExtension\" }; for instance { \"CSharp\", \"cs\" }. ");
            return Internal_OpenFilePanelWithFilters(title, directory, filters);
        }

        [FreeFunction("RevealInFinder")]
        public static extern void RevealInFinder(string path);

        [FreeFunction("DisplayDialog")]
        public static extern bool DisplayDialog(string title, string message, string ok, [DefaultValue("\"\"")] string cancel);

        [ExcludeFromDocs]
        public static bool DisplayDialog(string title, string message, string ok)
        {
            return DisplayDialog(title, message, ok, String.Empty);
        }

        [FreeFunction("DisplayDialogComplex")]
        public static extern int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt);

        [FreeFunction("RunOpenFolderPanel")]
        public static extern string OpenFolderPanel(string title, string folder, string defaultName);

        [FreeFunction("RunSaveFolderPanel")]
        public static extern string SaveFolderPanel(string title, string folder, string defaultName);

        [FreeFunction("WarnPrefab")]
        public static extern bool WarnPrefab(Object target, string title, string warning, string okButton);

        public static extern bool IsPersistent(Object target);
        public static extern string SaveFilePanel(string title, string directory, string defaultName, string extension);
        public static extern int NaturalCompare(string a, string b);
        public static extern void SetDirty([NotNull] Object target);
        public static extern Object InstanceIDToObject(int instanceID);
        public static extern void CompressTexture([NotNull] Texture2D texture, TextureFormat format, int quality);
        public static extern void CompressCubemapTexture([NotNull] Cubemap texture, TextureFormat format, int quality);

        [FreeFunction("InvokeDiffTool")]
        public static extern string InvokeDiffTool(string leftTitle, string leftFile, string rightTitle, string rightFile, string ancestorTitle, string ancestorFile);

        [FreeFunction("CopySerialized")]
        public static extern void CopySerialized(Object source, Object dest);

        [FreeFunction("CopyScriptSerialized")]
        public static extern void CopySerializedManagedFieldsOnly([NotNull] System.Object source, [NotNull] System.Object dest);

        [FreeFunction("CopySerializedIfDifferent")]
        private static extern void InternalCopySerializedIfDifferent(Object source, Object dest);

        public static extern Object[] CollectDependencies(Object[] roots);
        public static extern Object[] CollectDeepHierarchy(Object[] roots);

        [FreeFunction("InstantiateObjectRemoveAllNonAnimationComponents")]
        private static extern Object Internal_InstantiateRemoveAllNonAnimationComponentsSingle(Object data, Vector3 pos, Quaternion rot);

        [FreeFunction("UnloadUnusedAssetsImmediate")]
        private static extern void UnloadUnusedAssets(bool managedObjects);

        [Obsolete("Use EditorUtility.UnloadUnusedAssetsImmediate instead", false)]
        public static void UnloadUnusedAssets()
        {
            UnloadUnusedAssets(true);
        }

        [Obsolete("Use EditorUtility.UnloadUnusedAssetsImmediate instead", false)]
        public static void UnloadUnusedAssetsIgnoreManagedReferences()
        {
            UnloadUnusedAssets(false);
        }

        [FreeFunction("MenuController::DisplayPopupMenu")]
        private static extern void Private_DisplayPopupMenu(Rect position, string menuItemPath, Object context, int contextUserData);

        [FreeFunction("UpdateMenuTitleForLanguage")]
        internal static extern void Internal_UpdateMenuTitleForLanguage(SystemLanguage newloc);

        internal static extern void Internal_UpdateAllMenus();

        [FreeFunction("DisplayObjectContextPopupMenu")]
        internal static extern void DisplayObjectContextPopupMenu(Rect position, Object[] context, int contextUserData);

        [FreeFunction("DisplayCustomContextPopupMenu")]
        private static extern void DisplayCustomContextPopupMenu(Rect screenPosition, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey, bool allowDisplayNames);

        [FreeFunction("DisplayObjectContextPopupMenuWithExtraItems")]
        internal static extern void DisplayObjectContextPopupMenuWithExtraItems(Rect position, Object[] context, int contextUserData, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey);

        [FreeFunction("FormatBytes")]
        public static extern string FormatBytes(long bytes);

        [FreeFunction("DisplayProgressbar")]
        public static extern void DisplayProgressBar(string title, string info, float progress);

        public static extern bool DisplayCancelableProgressBar(string title, string info, float progress);

        [FreeFunction("ClearProgressbar")]
        public static extern void ClearProgressBar();

        [FreeFunction("GetObjectEnabled")]
        public static extern int GetObjectEnabled(Object target);

        [FreeFunction("SetObjectEnabled")]
        public static extern void SetObjectEnabled(Object target, bool enabled);

        public static extern void SetSelectedRenderState(Renderer renderer, EditorSelectedRenderState renderState);

        internal static extern void ForceReloadInspectors();
        internal static extern void ForceRebuildInspectors();

        [FreeFunction("ExtractOggFile")]
        public static extern bool ExtractOggFile(Object obj, string path);

        internal static extern GameObject Internal_CreateGameObjectWithHideFlags(string name, HideFlags flags);

        [FreeFunction("OpenWithDefaultApp")]
        public static extern void OpenWithDefaultApp(string fileName);

        [NativeThrows] internal static extern bool WSACreateTestCertificate(string path, string publisher, string password, bool overwrite);

        internal static extern bool IsWindows10OrGreater();

        public static extern void SetCameraAnimateMaterials([NotNull] Camera camera, bool animate);
        public static extern void SetCameraAnimateMaterialsTime([NotNull] Camera camera, float time);

        [FreeFunction("ShaderLab::UpdateGlobalShaderProperties")]
        public static extern void UpdateGlobalShaderProperties(float time);

        [FreeFunction("GetInvalidFilenameChars")]
        internal static extern string GetInvalidFilenameChars();

        [FreeFunction("GetApplication().IsAutoRefreshEnabled")]
        internal static extern bool IsAutoRefreshEnabled();

        [FreeFunction("GetApplication().GetActiveNativePlatformSupportModuleName")]
        internal static extern string GetActiveNativePlatformSupportModuleName();

        public static extern bool audioMasterMute
        {
            [FreeFunction("GetAudioManager().GetMasterGroupMute")] get;
            [FreeFunction("GetAudioManager().SetMasterGroupMute")] set;
        }

        internal static extern void LaunchBugReporter();

        internal static extern bool audioProfilingEnabled
        {
            [FreeFunction("GetAudioManager().GetProfilingEnabled")] get;
        }

        public static extern bool scriptCompilationFailed {[FreeFunction("HaveEditorCompileErrors")] get; }

        internal static extern bool EventHasDragCopyModifierPressed(Event evt);
        internal static extern bool EventHasDragMoveModifierPressed(Event evt);

        internal static extern void SaveProjectAsTemplate(string targetPath, string name, string displayName, string description, string defaultScene, string version);

        [Obsolete("Use AssetDatabase.LoadAssetAtPath", false),
         FreeFunction("FindAssetWithKlass")]
        public static extern Object FindAsset(string path, Type type);

        [FreeFunction("LoadPlatformSupportModuleNativeDll")]
        internal static extern void LoadPlatformSupportModuleNativeDllInternal(string target);

        [FreeFunction("LoadPlatformSupportNativeLibrary")]
        internal static extern void LoadPlatformSupportNativeLibrary(string nativeLibrary);

        internal static extern int GetDirtyIndex(int instanceID);
        internal static extern bool IsDirty(int instanceID);
        internal static extern string SaveBuildPanel(BuildTarget target, string title, string directory, string defaultName, string extension, out bool updateExistingBuild);
        internal static extern int NaturalCompareObjectNames(Object a, Object b);

        [FreeFunction("RunSavePanelInProject")]
        private static extern string Internal_SaveFilePanelInProject(string title, string defaultName, string extension, string message, string path);

        [RequiredByNativeCode]
        public static void FocusProjectWindow()
        {
            ProjectBrowser prjBrowser = null;
            var focusedView = GUIView.focusedView as HostView;
            if (focusedView != null && focusedView.actualView is ProjectBrowser)
            {
                prjBrowser = (ProjectBrowser)focusedView.actualView;
            }

            if (prjBrowser == null)
            {
                var wins = Resources.FindObjectsOfTypeAll(typeof(ProjectBrowser));
                if (wins.Length > 0)
                {
                    prjBrowser = wins[0] as ProjectBrowser;
                }
            }

            if (prjBrowser != null)
            {
                prjBrowser.Focus(); // This line is to circumvent a limitation where a tabbed window can't be directly targeted by a command: only the focused tab can.
                var commandEvent = EditorGUIUtility.CommandEvent("FocusProjectWindow");
                prjBrowser.SendEvent(commandEvent);
            }
        }
    }
}
