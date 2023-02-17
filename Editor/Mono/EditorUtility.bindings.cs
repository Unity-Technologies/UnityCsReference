// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using System.Collections.Generic;
using System.Linq;
using static UnityEditor.EditorGUI;

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
        static extern bool DisplayDialogImpl(string title, string message, string ok, string cancel);

        public static bool DisplayDialog(string title, string message, string ok, [DefaultValue("\"\"")] string cancel)
        {
            // i am not sure how picky should we be about the params
            // for example, for the buttons we have code that ignores empty strings,
            //   to allow having "ok"-only dialogs (information panels, so to say)
            // same with title+message: it sounds like we should allow skipping one of those
            // hence we make sure that at least one button is present, and some message
            // we can go more picky if we want in the future
            if(string.IsNullOrEmpty(ok) && string.IsNullOrEmpty(cancel))
                throw new ArgumentException("Both 'ok' and 'cancel' strings are null or empty");
            if(string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message))
                throw new ArgumentException("Both 'title' and 'message' strings are null or empty");

            using (new DisabledGuiViewInputScope(GUIView.current, true))
            {
                return DisplayDialogImpl(title, message, ok, cancel);
            }
        }
        [ExcludeFromDocs]
        public static bool DisplayDialog(string title, string message, string ok)
        {
            return DisplayDialog(title, message, ok, String.Empty);
        }

        [FreeFunction("DisplayDialogComplex")]
        static extern int DisplayDialogComplexImpl(string title, string message, string ok, string cancel, string alt);

        public static int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt)
        {
            // see the comment above in DisplayDialog
            // our implementation allows setting some strings empty (the button will be skipped then)
            // but we should totally ensure some buttons are set

            if(string.IsNullOrEmpty(ok) && string.IsNullOrEmpty(cancel) && string.IsNullOrEmpty(alt))
                throw new ArgumentException("All three 'ok', 'cancel' and 'alt' strings are null or empty");
            if(string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message))
                throw new ArgumentException("Both 'title' and 'message' strings are null or empty");

            return DisplayDialogComplexImpl(title, message, ok, cancel, alt);
        }

        [FreeFunction("RunOpenFolderPanel")]
        public static extern string OpenFolderPanel(string title, string folder, string defaultName);

        [FreeFunction("RunSaveFolderPanel")]
        public static extern string SaveFolderPanel(string title, string folder, string defaultName);

        [FreeFunction("WarnPrefab")]
        public static extern bool WarnPrefab(Object target, string title, string warning, string okButton);

        public static extern bool IsPersistent(Object target);
        public static extern string SaveFilePanel(string title, string directory, string defaultName, string extension);
        public static extern int NaturalCompare(string a, string b);
        public static extern Object InstanceIDToObject(int instanceID);
        public static extern void CompressTexture([NotNull] Texture2D texture, TextureFormat format, int quality);
        public static extern void CompressCubemapTexture([NotNull] Cubemap texture, TextureFormat format, int quality);

        private extern static int[] RemapInstanceIds(UnityEngine.Object[] objects, int[] srcIds, int[] dstIds);

        internal static int[] RemapInstanceIds(UnityEngine.Object[] objects, Dictionary<int, int> idMap)
        {
            return RemapInstanceIds(objects, idMap.Keys.ToArray(), idMap.Values.ToArray());
        }

        private extern static void RemapAssetReferences(UnityEngine.Object[] objects, string[] sourceAssetPaths, string[] dstAssetPaths, int[] srcIds, int[] dstIds);

        internal static void RemapAssetReferences(UnityEngine.Object[] objects, Dictionary<string, string> assetPathMap, Dictionary<int, int> idMap = null)
        {
            RemapAssetReferences(objects, assetPathMap.Keys.ToArray(), assetPathMap.Values.ToArray(),
                idMap == null ? new int[0] : idMap.Keys.ToArray(),
                idMap == null ? new int[0] : idMap.Values.ToArray()
            );
        }

        [FreeFunction("EditorUtility::SetDirtyObjectOrScene")]
        public static extern void SetDirty([NotNull] Object target);

        public static extern void ClearDirty([NotNull] Object target);

        [FreeFunction("InvokeDiffTool")]
        public static extern string InvokeDiffTool(string leftTitle, string leftFile, string rightTitle, string rightFile, string ancestorTitle, string ancestorFile);

        [FreeFunction("CopySerialized")]
        public static extern void CopySerialized([NotNull("NullExceptionObject")] Object source, [NotNull("NullExceptionObject")] Object dest);

        [FreeFunction("CopyScriptSerialized")]
        public static extern void CopySerializedManagedFieldsOnly([NotNull] System.Object source, [NotNull] System.Object dest);

        [FreeFunction("CopySerializedIfDifferent")]
        private static extern void InternalCopySerializedIfDifferent([NotNull("NullExceptionObject")] Object source, [NotNull("NullExceptionObject")] Object dest);

        [NativeThrows]
        public static extern Object[] CollectDependencies(Object[] roots);
        public static extern Object[] CollectDeepHierarchy(Object[] roots);

        [FreeFunction("InstantiateObjectRemoveAllNonAnimationComponents")]
        private static extern Object Internal_InstantiateRemoveAllNonAnimationComponentsSingle([NotNull("NullExceptionObject")] Object data, Vector3 pos, Quaternion rot);

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
        private static extern void Private_DisplayPopupMenu(Rect position, string menuItemPath, Object context, int contextUserData, bool shouldDiscardMenuOnSecondClick = false);

        [FreeFunction("UpdateMenuTitleForLanguage")]
        internal static extern void Internal_UpdateMenuTitleForLanguage(SystemLanguage newloc);

        internal static extern void RebuildAllMenus();
        internal static extern void Internal_UpdateAllMenus();
        internal static extern void LogAllMenus();
        internal static extern string ParseMenuName(string menuName);

        [FreeFunction("DisplayObjectContextPopupMenu")]
        internal static extern void DisplayObjectContextPopupMenu(Rect position, Object[] context, int contextUserData);

        [FreeFunction("DisplayCustomContextPopupMenu")]
        private static extern void DisplayCustomContextPopupMenu(Rect screenPosition, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey, bool allowDisplayNames, bool shouldDiscardMenuOnSecondClick = false);

        [FreeFunction("DisplayObjectContextPopupMenuWithExtraItems")]
        internal static extern void DisplayObjectContextPopupMenuWithExtraItems(Rect position, Object[] context, int contextUserData, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey);

        [FreeFunction("FormatBytes")]
        public static extern string FormatBytes(long bytes);

        [FreeFunction("DisplayProgressbarLegacy")]
        public static extern void DisplayProgressBar(string title, string info, float progress);

        public static extern bool DisplayCancelableProgressBar(string title, string info, float progress);

        [FreeFunction("ClearProgressbarLegacy")]
        public static extern void ClearProgressBar();

        [FreeFunction("BusyProgressDialogDelayChanged")]
        internal static extern void BusyProgressDialogDelayChanged(float delay);

        [FreeFunction("GetObjectEnabled")]
        public static extern int GetObjectEnabled(Object target);

        [FreeFunction("SetObjectEnabled")]
        public static extern void SetObjectEnabled(Object target, bool enabled);

        public static extern void SetSelectedRenderState(Renderer renderer, EditorSelectedRenderState renderState);

        internal static extern void ForceReloadInspectors();
        internal static extern void ForceRebuildInspectors();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("ExtractOggFile has no effect anymore", false)]
        public static bool ExtractOggFile(Object obj, string path)
        {
            return false;
        }

        internal static extern GameObject Internal_CreateGameObjectWithHideFlags(string name, HideFlags flags);

        [FreeFunction("OpenWithDefaultApp")]
        public static extern void OpenWithDefaultApp(string fileName);

        [NativeThrows] internal static extern bool WSACreateTestCertificate(string path, string publisher, string password, bool overwrite);
        internal static extern bool WSAGetCertificateExpirationDate(string path, string password, out long expirationDate);

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
         FreeFunction("FindAssetWithKlass", ThrowsException = true)]
        public static extern Object FindAsset(string path, Type type);

        [FreeFunction("LoadPlatformSupportModuleNativeDll")]
        internal static extern void LoadPlatformSupportModuleNativeDllInternal(string target);

        [FreeFunction("ReloadPlatformSupportModuleNativeDll")]
        internal static extern void ReloadPlatformSupportModuleNativeDllInternal(string target);

        [FreeFunction("LoadPlatformSupportNativeLibrary")]
        internal static extern void LoadPlatformSupportNativeLibrary(string nativeLibrary);

        [NativeMethod("GetDirtyIndex")]
        public static extern int GetDirtyCount(int instanceID);
        public static int GetDirtyCount(Object target) { return target != null ? GetDirtyCount(target.GetInstanceID()) : 0; }
        public static extern bool IsDirty(int instanceID);
        public static bool IsDirty(Object target) { return target != null ? IsDirty(target.GetInstanceID()) : false; }
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

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        extern public static void RequestScriptReload();

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        [NativeThrows]
        extern internal static void RequestPartialScriptReload();

        internal static extern bool isInSafeMode
        {
            [FreeFunction("GetApplication().IsInSafeMode")]
            get;
        }

        [FreeFunction("IsRunningUnderCPUEmulation", IsThreadSafe = true)]
        extern public static bool IsRunningUnderCPUEmulation();
    }
}
