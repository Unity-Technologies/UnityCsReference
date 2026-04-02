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
using UnityEditor.Inspector.GraphicsSettingsInspectors;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/EditorUtility.bindings.h")]
    [NativeHeader("Editor/Mono/MonoEditorUtility.h")]
    [NativeHeader("Editor/Src/AssetPipeline/UnityExtensions.h")]
    [NativeHeader("Editor/Src/EditorHelperApple.h")]
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

        [FreeFunction]
        internal static extern string SanitizeProductNameForXcode(string name);

        [FreeFunction("RunOpenFolderPanel")]
        public static extern string OpenFolderPanel(string title, string folder, string defaultName);

        [FreeFunction("RunSaveFolderPanel")]
        public static extern string SaveFolderPanel(string title, string folder, string defaultName);

        [FreeFunction("WarnPrefab")]
        public static extern bool WarnPrefab(Object target, string title, string warning, string okButton);

        [StaticAccessor("UnityExtensions::Get()", StaticAccessorType.Dot)]
        [NativeMethod("IsInitialized")]
        public extern static bool IsUnityExtensionsInitialized();

        public static extern bool IsPersistent(Object target);
        public static extern bool IsValidUnityYAML(string yaml);
        public static extern string SaveFilePanel(string title, string directory, string defaultName, string extension);
        [NativeMethod(IsThreadSafe = true)]
        public static extern int NaturalCompare(string a, string b);

        [Obsolete("InstanceIDToObject(int) is obsolete. Use EditorUtility.EntityIdToObject instead.", true)]
        public static Object InstanceIDToObject(int instanceID) => EntityIdToObject((EntityId)instanceID);

        public static extern Object EntityIdToObject(EntityId entityId);
        public static extern void CompressTexture([NotNull] Texture2D texture, TextureFormat format, int quality);
        public static extern void CompressCubemapTexture([NotNull] Cubemap texture, TextureFormat format, int quality);

        private extern static EntityId[] RemapInstanceIds(UnityEngine.Object[] objects, EntityId[] srcIds, EntityId[] dstIds);

        private extern static void RemapAssetReferences(UnityEngine.Object[] objects, string[] sourceAssetPaths, string[] dstAssetPaths, EntityId[] srcIds, EntityId[] dstIds);

        internal static void RemapAssetReferences(UnityEngine.Object[] objects, Dictionary<string, string> assetPathMap, Dictionary<EntityId, EntityId> idMap = null)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            RemapAssetReferences(objects, assetPathMap.Keys.ToArray(), assetPathMap.Values.ToArray(),
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                idMap == null ? Array.Empty<EntityId>() : idMap.Keys.ToArray(),
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                idMap == null ? Array.Empty<EntityId>() : idMap.Values.ToArray()
#pragma warning restore UA2001
            );
        }

        [FreeFunction("EditorUtility::SetDirtyObjectOrScene")]
        public static extern void SetDirty([NotNull] Object target);

        public static extern void ClearDirty([NotNull] Object target);

        [FreeFunction("InvokeDiffTool")]
        public static extern string InvokeDiffTool(string leftTitle, string leftFile, string rightTitle, string rightFile, string ancestorTitle, string ancestorFile);

        [FreeFunction("CopySerialized")]
        public static extern void CopySerialized([NotNull] Object source, [NotNull] Object dest);

        [FreeFunction("CopyScriptSerialized")]
        public static extern void CopySerializedManagedFieldsOnly([NotNull] System.Object source, [NotNull] System.Object dest);

        [FreeFunction("CopySerializedIfDifferent")]
        private static extern void InternalCopySerializedIfDifferent([NotNull] Object source, [NotNull] Object dest);

        [NativeMethod(ThrowsException = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern Object[] CollectDependencies([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] roots);
        public static extern Object[] CollectDeepHierarchy([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] roots);

        [FreeFunction("InstantiateObjectRemoveAllNonAnimationComponents")]
        private static extern Object Internal_InstantiateRemoveAllNonAnimationComponentsSingle([NotNull] Object data, Vector3 pos, Quaternion rot);

        [FreeFunction("ManagedUnloadUnusedAssetsImmediate")]
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

        [RequiredByNativeCode]
        internal static void DelayedForceRebuildInspectors()
        {
            EditorApplication.CallDelayed(() =>
            {
                ForceRebuildInspectors();
                GraphicsSettingsInspectorUtility.ReloadGraphicsSettingsEditorIfNeeded();
            });
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("ExtractOggFile has no effect anymore", false)]
        public static bool ExtractOggFile(Object obj, string path)
        {
            return false;
        }

        internal static extern GameObject Internal_CreateGameObjectWithHideFlags(string name, HideFlags flags);

        [FreeFunction("OpenWithDefaultApp")]
        public static extern void OpenWithDefaultApp(string fileName);

        [NativeMethod(ThrowsException = true)] internal static extern bool WSACreateTestCertificate(string path, string publisher, string password, bool overwrite);
        internal static extern bool WSAGetCertificateExpirationDate(string path, string password, out long expirationDate);

        internal static extern bool IsWindows10OrGreater();

        public static extern void SetCameraAnimateMaterials([NotNull] Camera camera, bool animate);
        public static extern void SetCameraAnimateMaterialsTime([NotNull] Camera camera, float time);

        [FreeFunction("ShaderLab::UpdateGlobalShaderProperties")]
        public static extern void UpdateGlobalShaderProperties(float time);

        [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
        [FreeFunction("GetInvalidFilenameChars")]
        internal static extern string GetInvalidFilenameChars();

        [FreeFunction("GetApplication().IsAutoRefreshEnabled")]
        internal static extern bool IsAutoRefreshEnabled();

        [FreeFunction("GetApplication().GetActiveNativePlatformSupportModuleName")]
        internal static extern string GetActiveNativePlatformSupportModuleName();

        internal static extern bool Internal_AudioMasterMute
        {
            [FreeFunction("GetAudioManager().GetMasterGroupMute")] get;
            [FreeFunction("GetAudioManager().SetMasterGroupMute")] set;
        }

        public static bool audioMasterMute
        {
            get { return Internal_AudioMasterMute; }
            set
            {
                if (value != Internal_AudioMasterMute)
                {
                    Internal_AudioMasterMute = value;
                    onAudioMasterMuteWasUpdated?.Invoke(value);
                }
            }
        }

        internal delegate void AudioMasterMuteWasUpdated(bool value);
        internal static event AudioMasterMuteWasUpdated onAudioMasterMuteWasUpdated;

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

        [FreeFunction("PlatformSupportModuleSetCustomData")]
        internal static extern void PlatformSupportModuleSetCustomData(string target, int customKey, int customValue);

        [FreeFunction("LoadPlatformSupportNativeLibrary")]
        internal static extern void LoadPlatformSupportNativeLibrary(string nativeLibrary);

        [Obsolete("GetDirtyCount(int) is deprecated. Use GetDirtyCount(EntityId) instead.", true)]
        public static int GetDirtyCount(int instanceID) => GetDirtyCount((EntityId)instanceID);
        [NativeMethod("GetDirtyIndex")]
        public static extern int GetDirtyCount(EntityId entityId);
        public static int GetDirtyCount(Object target) { return target != null ? GetDirtyCount(target.GetEntityId()) : 0; }
        [Obsolete("IsDirty(int) is deprecated. Use IsDirty(EntityId) instead.", true)]
        public static bool IsDirty(int instanceID) => IsDirty((EntityId)instanceID);
        public static extern bool IsDirty(EntityId entityId);
        public static bool IsDirty(Object target) => target != null && IsDirty(target.GetEntityId());
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
        [NativeMethod(ThrowsException = true)]
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
