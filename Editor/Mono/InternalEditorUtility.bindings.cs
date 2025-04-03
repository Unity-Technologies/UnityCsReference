// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor;
using System.Reflection;
using UnityEngine.Bindings;
using UnityEditor.Scripting.ScriptCompilation;
using System.Globalization;
using Unity.CodeEditor;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using ShaderPropertyType = UnityEngine.Rendering.ShaderPropertyType;
using ShaderPropertyFlags = UnityEngine.Rendering.ShaderPropertyFlags;

namespace UnityEditorInternal
{
    [System.Obsolete("CanAppendBuild has been deprecated. Use UnityEditor.CanAppendBuild instead (UnityUpgradable) -> [UnityEditor] UnityEditor.CanAppendBuild", true)]
    public enum CanAppendBuild
    {
        Unsupported = 0,
        Yes = 1,
        No = 2,
    }

    // Keep in sync with DllType in MonoEditorUtility.h
    public enum DllType
    {
        Unknown = 0,
        Native = 1,
        UnknownManaged = 2,
        ManagedNET35 = 3,
        ManagedNET40 = 4,
        WinMDNative = 5,
        WinMDNET40 = 6
    }

    [RequiredByNativeCode]
    internal class LoadFileAndForgetOperationHelper
    {
        // When the load operation completes this is invoked to hold the result so that it doesn't
        // get garbage collected
        [RequiredByNativeCode]
        public static void SetObjectResult(LoadFileAndForgetOperation op, UnityEngine.Object result)
        {
            op.m_ObjectReference = result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class LoadFileAndForgetOperation : AsyncOperation
    {
        internal UnityEngine.Object m_ObjectReference;

        public extern UnityEngine.Object Result
        {
            [NativeMethod("GetRequestedObject")]
            get;
        }

        public LoadFileAndForgetOperation() { }

        private LoadFileAndForgetOperation(IntPtr ptr) : base(ptr) { }

        new internal static class BindingsMarshaller
        {
            public static LoadFileAndForgetOperation ConvertToManaged(IntPtr ptr) => new LoadFileAndForgetOperation(ptr);
            public static IntPtr ConvertToNative(LoadFileAndForgetOperation asyncOperation) => asyncOperation.m_Ptr;
        }
    }

    [NativeHeader("Editor/Src/InternalEditorUtility.bindings.h")]

    [NativeHeader("Editor/Mono/MonoEditorUtility.h")]
    [NativeHeader("Editor/Platform/Interface/ColorPicker.h")]
    [NativeHeader("Editor/Platform/Interface/EditorUtility.h")]
    [NativeHeader("Editor/Src/Application/Application.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabase.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseDeprecated.h")]
    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/BumpMapSettings.h")]
    [NativeHeader("Editor/Src/ScriptCompilation/PrecompiledAssemblies.h")]
    [NativeHeader("Editor/Src/AssetPipeline/ObjectHashGenerator.h")]
    [NativeHeader("Editor/Src/AssetPipeline/UnityExtensions.h")]
    [NativeHeader("Editor/Src/Windowing/AuxWindowManager.h")]
    [NativeHeader("Editor/Src/DisplayDialog.h")]
    [NativeHeader("Editor/Src/DragAndDropForwarding.h")]
    [NativeHeader("Editor/Src/EditorHelper.h")]
    [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
    [NativeHeader("Editor/Src/EditorWindowController.h")]
    [NativeHeader("Editor/Src/EditorModules.h")]
    [NativeHeader("Editor/Src/Gizmos/GizmoUtil.h")]
    [NativeHeader("Editor/Src/HierarchyState.h")]
    [NativeHeader("Editor/Src/InspectorExpandedState.h")]
    [NativeHeader("Editor/Src/LoadFileAndForgetOperation.h")]
    [NativeHeader("Runtime/Interfaces/ILicensing.h")]
    [NativeHeader("Editor/Src/RemoteInput/RemoteInput.h")]
    [NativeHeader("Editor/Src/ShaderMenu.h")]
    [NativeHeader("Editor/Src/Undo/ObjectUndo.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseProperty.h")]
    [NativeHeader("Editor/Src/Utility/CustomLighting.h")]
    [NativeHeader("Editor/Src/Utility/DiffTool.h")]
    [NativeHeader("Editor/Src/Utility/GameObjectHierarchyProperty.h")]
    [NativeHeader("Runtime/BaseClasses/TagManager.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Runtime/Camera/RenderManager.h")]
    [NativeHeader("Runtime/Camera/RenderSettings.h")]
    [NativeHeader("Runtime/Camera/Skybox.h")]
    [NativeHeader("Runtime/Transform/RectTransform.h")]
    [NativeHeader("Runtime/Graphics/Renderer.h")]
    [NativeHeader("Runtime/Graphics/ScreenManager.h")]
    [NativeHeader("Runtime/Graphics/SpriteFrame.h")]
    [NativeHeader("Runtime/2D/Common/SpriteTypes.h")]
    [NativeHeader("Runtime/Graphics/GraphicsHelper.h")]
    [NativeHeader("Runtime/Graphics/GpuDeviceManager.h")]
    [NativeHeader("Runtime/Input/Cursor.h")]
    [NativeHeader("Runtime/Misc/GameObjectUtility.h")]
    [NativeHeader("Runtime/Misc/Player.h")]
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    [NativeHeader("Runtime/Serialize/PersistentManager.h")]
    [NativeHeader("Runtime/Shaders/ShaderImpl/FastPropertyName.h")]
    [NativeHeader("Runtime/Serialize/PersistentManager.h")]
    [NativeHeader("Runtime/Threads/ThreadChecks.h")]
    [NativeHeader("Runtime/Utilities/Word.h")]
    [NativeHeader("Editor/Src/BuildPipeline/BuildPlayer.h")]
    [NativeHeader("Editor/Src/BuildPipeline/BuildTargetPlatformSpecific.h")]
    [NativeHeader("Runtime/Utilities/Argv.h")]
    [NativeHeader("Runtime/Utilities/FileUtilities.h")]
    [NativeHeader("Runtime/Utilities/LaunchUtilities.h")]
    [NativeHeader("Runtime/Utilities/UnityConfiguration.h")]
    [NativeHeader("Runtime/Utilities/UnityGitConfiguration.h")]
    public partial class InternalEditorUtility
    {
        public extern static bool isHumanControllingUs
        {
            [FreeFunction("IsHumanControllingUs")]
            get;
        }

        public extern static bool isApplicationActive
        {
            [FreeFunction("IsApplicationActive")]
            get;
        }

        public extern static bool inBatchMode
        {
            [FreeFunction("IsBatchmode")]
            get;
        }

        [StaticAccessor("BumpMapSettings::Get()", StaticAccessorType.Dot)]
        [NativeMethod("PerformUnmarkedBumpMapTexturesFixingAfterDialog")]
        public extern static void BumpMapSettingsFixingWindowReportResult(int result);

        [StaticAccessor("BumpMapSettings::Get()", StaticAccessorType.Dot)]
        [NativeMethod("PerformUnmarkedBumpMapTexturesFixing")]
        public extern static bool PerformUnmarkedBumpMapTexturesFixing();

        [FreeFunction("InternalEditorUtilityBindings::BumpMapTextureNeedsFixingInternal")]
        public extern static bool BumpMapTextureNeedsFixingInternal([NotNull] Material material, string propName, bool flaggedAsNormal);

        internal static bool BumpMapTextureNeedsFixing(MaterialProperty prop)
        {
            if (prop.propertyType != ShaderPropertyType.Texture)
                return false;

            bool hintIfNormal = ((prop.propertyFlags & ShaderPropertyFlags.Normal) != 0);

            foreach (Material material in prop.targets)
                if (BumpMapTextureNeedsFixingInternal(material, prop.name, hintIfNormal))
                    return true;

            return false;
        }

        [FreeFunction("InternalEditorUtilityBindings::FixNormalmapTextureInternal")]
        public extern static void FixNormalmapTextureInternal([NotNull] Material material, string propName);

        internal static void FixNormalmapTexture(MaterialProperty prop)
        {
            foreach (Material material in prop.targets)
                FixNormalmapTextureInternal(material, prop.name);
        }

        [FreeFunction("InternalEditorUtilityBindings::GetEditorAssemblyPath")]
        public extern static string GetEditorAssemblyPath();

        [FreeFunction("InternalEditorUtilityBindings::GetEngineAssemblyPath")]
        public extern static string GetEngineAssemblyPath();

        [FreeFunction("InternalEditorUtilityBindings::GetEngineCoreModuleAssemblyPath")]
        public extern static string GetEngineCoreModuleAssemblyPath();

        [FreeFunction("InternalEditorUtilityBindings::GetBuildSystemVariationArgs")]
        internal extern static string GetBuildSystemVariationArgs();

        [FreeFunction("InternalEditorUtilityBindings::CalculateHashForObjectsAndDependencies")]
        public extern static string CalculateHashForObjectsAndDependencies(Object[] objects);

        [FreeFunction]
        public extern static void ExecuteCommandOnKeyWindow(string commandName);

        [FreeFunction("InternalEditorUtilityBindings::InstantiateMaterialsInEditMode")]
        public extern static Material[] InstantiateMaterialsInEditMode([NotNull] Renderer renderer);

        [System.Obsolete("BuildCanBeAppended has been deprecated. Use UnityEditor.BuildPipeline.BuildCanBeAppended instead (UnityUpgradable) -> [UnityEditor] UnityEditor.BuildPipeline.BuildCanBeAppended(*)", true)]
        public static CanAppendBuild BuildCanBeAppended(BuildTarget target, string location)
        {
            return (CanAppendBuild)BuildPipeline.BuildCanBeAppended(target, location);
        }

        [FreeFunction]
        extern internal static void RegisterPlatformModuleAssembly(string dllName, string dllLocation);

        [FreeFunction]
        extern internal static void RegisterPrecompiledAssembly(string dllName, string dllLocation);

        // This lets you add a MonoScript to a game object directly without any type checks or requiring the .NET representation to be loaded already.
        [FreeFunction("InternalEditorUtilityBindings::AddScriptComponentUncheckedUndoable")]
        extern internal static int AddScriptComponentUncheckedUndoable([NotNull] GameObject gameObject, [NotNull] MonoScript script);

        [FreeFunction("InternalEditorUtilityBindings::CreateScriptableObjectUnchecked")]
        extern internal static int CreateScriptableObjectUnchecked(MonoScript script);

        [Obsolete("RequestScriptReload has been deprecated. Use UnityEditor.EditorUtility.RequestScriptReload instead (UnityUpgradable) -> [UnityEditor] UnityEditor.EditorUtility.RequestScriptReload(*)")]
        public static void RequestScriptReload()
        {
            EditorUtility.RequestScriptReload();
        }

        // Repaint all views on next tick. Used when the user changes skins in the prefs.
        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        [NativeMethod("SwitchSkinAndRepaintAllViews")]
        extern public static void SwitchSkinAndRepaintAllViews();

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        extern internal static bool IsSwitchSkinRequested();

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        [NativeMethod("RequestRepaintAllViews")]
        extern public static void RepaintAllViews();

        [StaticAccessor("GetInspectorExpandedState()", StaticAccessorType.Dot)]
        [NativeMethod("IsInspectorExpanded")]
        extern public static bool GetIsInspectorExpanded(Object obj);

        [StaticAccessor("GetInspectorExpandedState()", StaticAccessorType.Dot)]
        [NativeMethod("SetInspectorExpanded")]
        extern public static void SetIsInspectorExpanded(Object obj, bool isExpanded);

        extern public static int[] expandedProjectWindowItems
        {
            [StaticAccessor("AssetDatabase::GetProjectWindowHierarchyState()", StaticAccessorType.Dot)]
            [NativeMethod("GetExpandedArray")]
            get;
            [FreeFunction("InternalEditorUtilityBindings::SetExpandedProjectWindowItems")]
            set;
        }

        public static Assembly LoadAssemblyWrapper(string dllName, string dllLocation)
        {
            return (Assembly)LoadAssemblyWrapperInternal(dllName, dllLocation);
        }

        [StaticAccessor("GetMonoManager()", StaticAccessorType.Dot)]
        [NativeMethod("LoadAssembly")]
        extern internal static object LoadAssemblyWrapperInternal(string dllName, string dllLocation);

        public static void SaveToSerializedFileAndForget(Object[] obj, string path, bool allowTextSerialization)
        {
            SaveToSerializedFileAndForgetInternal(path, obj, allowTextSerialization);
        }

        [FreeFunction("SaveToSerializedFileAndForget")]
        extern private static void SaveToSerializedFileAndForgetInternal(string path, Object[] obj, bool allowTextSerialization);

        [FreeFunction("InternalEditorUtilityBindings::LoadSerializedFileAndForget")]
        extern public static Object[] LoadSerializedFileAndForget(string path);

        [FreeFunction("LoadFileAndForgetOperation::LoadSerializedFileAndForgetAsync")]
        extern public static LoadFileAndForgetOperation LoadSerializedFileAndForgetAsync(string path, long localIdentifierInFile, ulong offsetInFile=0, long fileSize=-1, Scene destScene = default);

        [FreeFunction("InternalEditorUtilityBindings::ProjectWindowDrag")]
        extern public static DragAndDropVisualMode ProjectWindowDrag([Unmarshalled] HierarchyProperty property, bool perform);

        [FreeFunction("InternalEditorUtilityBindings::HierarchyWindowDrag")]
        extern public static DragAndDropVisualMode HierarchyWindowDrag([Unmarshalled] HierarchyProperty property, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform);

        public static DragAndDropVisualMode HierarchyWindowDragByID(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
            => HierarchyWindowDragByID(dropTargetInstanceID, GOCreationCommands.GetNewObjectPosition(), dropMode, parentForDraggedObjects, perform);

        [FreeFunction("InternalEditorUtilityBindings::HierarchyWindowDragByID")]
        extern internal static DragAndDropVisualMode HierarchyWindowDragByID(int dropTargetInstanceID, Vector3 worldPosition, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform);

        [FreeFunction("InternalEditorUtilityBindings::InspectorWindowDrag")]
        extern internal static DragAndDropVisualMode InspectorWindowDrag(Object[] targets, bool perform);

        [FreeFunction("InternalEditorUtilityBindings::SceneViewDrag")]
        extern public static DragAndDropVisualMode SceneViewDrag(Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform);

        [FreeFunction("InternalEditorUtilityBindings::SetRectTransformTemporaryRect")]
        extern public static void SetRectTransformTemporaryRect([NotNull] RectTransform rectTransform, Rect rect);

        [Obsolete("HasTeamLicense always returns true, no need to call it")]
        public static bool HasTeamLicense() { return true; }

        [FreeFunction("InternalEditorUtilityBindings::HasPro", IsThreadSafe = true)]
        extern public static bool HasPro();

        [FreeFunction("InternalEditorUtilityBindings::HasFreeLicense", IsThreadSafe = true)]
        extern public static bool HasFreeLicense();

        [FreeFunction("InternalEditorUtilityBindings::HasEduLicense", IsThreadSafe = true)]
        extern public static bool HasEduLicense();

        [FreeFunction("InternalEditorUtilityBindings::HasUFSTLicense", IsThreadSafe = true)]
        extern internal static bool HasUFSTLicense();

        [FreeFunction]
        extern public static bool HasAdvancedLicenseOnBuildTarget(BuildTarget target);

        public static bool IsMobilePlatform(BuildTarget target)
        {
            return BuildTargetDiscovery.PlatformHasFlag(target, TargetAttributes.HasIntegratedGPU);
        }

        [NativeThrows]
        [FreeFunction("InternalEditorUtilityBindings::GetBoundsOfDesktopAtPoint")]
        extern public static Rect GetBoundsOfDesktopAtPoint(Vector2 pos);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        [NativeMethod("RemoveTag")]
        extern public static void RemoveTag(string tag);

        [FreeFunction("InternalEditorUtilityBindings::AddTag")]
        extern public static void AddTag(string tag);

        extern public static string[] tags
        {
            [FreeFunction("InternalEditorUtilityBindings::GetTags")]
            get;
        }

        extern public static string[] layers
        {
            [FreeFunction("InternalEditorUtilityBindings::GetLayers")]
            get;
        }

        [FreeFunction("InternalEditorUtilityBindings::GetLayersWithId")]
        extern static internal string[] GetLayersWithId();

        [FreeFunction("InternalEditorUtilityBindings::CanRenameAssetInternal")]
        extern internal static bool CanRenameAsset(int instanceID);

        public static LayerMask ConcatenatedLayersMaskToLayerMask(int concatenatedLayersMask)
        {
            return ConcatenatedLayersMaskToLayerMaskInternal(concatenatedLayersMask);
        }

        [FreeFunction("InternalEditorUtilityBindings::ConcatenatedLayersMaskToLayerMaskInternal")]
        extern private static int ConcatenatedLayersMaskToLayerMaskInternal(int concatenatedLayersMask);

        [FreeFunction("TryOpenErrorFileFromConsole")]
        public extern static bool TryOpenErrorFileFromConsole(string path, int line, int column);

        [FreeFunction("TryOpenErrorFileFromConsoleInternal")]
        internal extern static bool TryOpenErrorFileFromConsoleInternal(string path, int line, int column, bool isDryRun);

        public static bool TryOpenErrorFileFromConsole(string path, int line)
        {
            return TryOpenErrorFileFromConsole(path, line, 0);
        }

        public static int LayerMaskToConcatenatedLayersMask(LayerMask mask)
        {
            return LayerMaskToConcatenatedLayersMaskInternal(mask);
        }

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static string GetSortingLayerName(int index);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static int GetSortingLayerUniqueID(int index);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static string GetSortingLayerNameFromUniqueID(int id);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static int GetSortingLayerCount();

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static void SetSortingLayerName(int index, string name);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static void SetSortingLayerLocked(int index, bool locked);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static bool GetSortingLayerLocked(int index);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static bool IsSortingLayerDefault(int index);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static void AddSortingLayer();

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        extern internal static void UpdateSortingLayersOrder();

        extern internal static string[] sortingLayerNames
        {
            [FreeFunction("InternalEditorUtilityBindings::GetSortingLayerNames")]
            get;
        }

        extern internal static int[] sortingLayerUniqueIDs
        {
            [FreeFunction("InternalEditorUtilityBindings::GetSortingLayerUniqueIDs")]
            get;
        }

        // UV coordinates for the outer part of a sliced Sprite (the whole Sprite)
        [FreeFunction("InternalEditorUtilityBindings::GetSpriteOuterUV")]
        extern public static Vector4 GetSpriteOuterUV([NotNull] Sprite sprite, bool getAtlasData);

        [FreeFunction("PPtr<Object>::FromInstanceID")]
        extern public static Object GetObjectFromInstanceID(int instanceID);

        [FreeFunction("GetTypeWithoutLoadingObject")]
        extern public static Type GetTypeWithoutLoadingObject(int instanceID);

        [FreeFunction("Object::IDToPointer")]
        extern public static Object GetLoadedObjectFromInstanceID(int instanceID);

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        [NativeMethod("LayerToString")]
        extern public static string GetLayerName(int layer);

        extern public static string unityPreferencesFolder
        {
            [FreeFunction]
            get;
        }

        internal static extern string userAppDataFolder
        {
            [FreeFunction("GetUserAppDataFolder")]
            get;
        }

        [FreeFunction]
        extern public static string GetAssetsFolder();

        [FreeFunction]
        extern public static string GetEditorFolder();

        [FreeFunction]
        extern public static bool IsInEditorFolder(string path);

        public static void ReloadWindowLayoutMenu()
        {
            WindowLayout.UpdateWindowLayoutMenu();
        }

        public static void RevertFactoryLayoutSettings(bool quitOnCancel)
        {
            WindowLayout.ResetAllLayouts(quitOnCancel);
        }

        public static void LoadDefaultLayout()
        {
            WindowLayout.LoadDefaultLayout();
        }

        [StaticAccessor("GetRenderSettings()", StaticAccessorType.Dot)]
        extern internal static void CalculateAmbientProbeFromSkybox();

        [Obsolete("SetupShaderMenu is obsolete. You can get list of available shaders with ShaderUtil.GetAllShaderInfos", false)]
        [FreeFunction("SetupShaderPopupMenu")]
        extern public static void SetupShaderMenu([NotNull] Material material);

        [FreeFunction("UnityConfig::GetUnityBuildFullVersion")]
        extern public static string GetFullUnityVersion();

        public static Version GetUnityVersion()
        {
            Version version = new Version(GetUnityVersionDigits());
            return new Version(version.Major, version.Minor, version.Build, GetUnityRevision());
        }

        [FreeFunction("InternalEditorUtilityBindings::GetUnityVersionDigits")]
        extern public static string GetUnityVersionDigits();

        [FreeFunction("UnityConfig::GetUnityBuildBranchName")]
        extern public static string GetUnityBuildBranch();

        [FreeFunction("UnityConfig::GetUnityBuildHash")]
        extern public static string GetUnityBuildHash();

        [FreeFunction("UnityConfig::GetUnityDisplayVersion")]
        extern public static string GetUnityDisplayVersion();

        [FreeFunction("UnityConfig::GetUnityDisplayVersionVerbose")]
        extern public static string GetUnityDisplayVersionVerbose();

        [FreeFunction("UnityConfig::GetUnityBuildTimeSinceEpoch")]
        extern public static int GetUnityVersionDate();

        [FreeFunction("UnityConfig::GetUnityBuildNumericRevision")]
        extern public static int GetUnityRevision();

        [FreeFunction("UnityConfig::GetUnityProductName")]
        extern public static string GetUnityProductName();

        [FreeFunction("InternalEditorUtilityBindings::IsUnityBeta")]
        extern public static bool IsUnityBeta();

        [FreeFunction("InternalEditorUtilityBindings::GetUnityCopyright")]
        extern public static string GetUnityCopyright();

        [FreeFunction("InternalEditorUtilityBindings::GetLicenseInfoText")]
        extern public static string GetLicenseInfo();

        [FreeFunction("InternalEditorUtilityBindings::GetLicenseInfoTypeText")]
        extern internal static string GetLicenseInfoType();

        [FreeFunction("InternalEditorUtilityBindings::GetLicenseInfoSerialText")]
        extern internal static string GetLicenseInfoSerial();

        [Obsolete("GetLicenseFlags is no longer supported", error: true)]
        [FreeFunction("InternalEditorUtilityBindings::GetLicenseFlags")]
        extern public static int[] GetLicenseFlags();

        [FreeFunction("InternalEditorUtilityBindings::GetAuthToken")]
        extern public static string GetAuthToken();

        [FreeFunction("InternalEditorUtilityBindings::OpenEditorConsole")]
        extern public static void OpenEditorConsole();

        [FreeFunction("InternalEditorUtilityBindings::GetGameObjectInstanceIDFromComponent")]
        extern public static int GetGameObjectInstanceIDFromComponent(int instanceID);

        [FreeFunction("InternalEditorUtilityBindings::ReadScreenPixel")]
        extern public static Color[] ReadScreenPixel(Vector2 pixelPos, int sizex, int sizey);

        [FreeFunction("InternalEditorUtilityBindings::ReadScreenPixelUnderCursor")]
        extern public static Color[] ReadScreenPixelUnderCursor(Vector2 cursorPosHint, int sizex, int sizey);

        [FreeFunction("InternalEditorUtilityBindings::IsAllowedToReadPixelOutsideUnity")]
        extern internal static bool IsAllowedToReadPixelOutsideUnity(out string errorMessage);

        [StaticAccessor("GetGpuDeviceManager()", StaticAccessorType.Dot)]
        [NativeMethod("SetDevice")]
        extern public static void SetGpuDeviceAndRecreateGraphics(int index, string name);

        [StaticAccessor("GetGpuDeviceManager()", StaticAccessorType.Dot)]
        [NativeMethod("IsSupported")]
        extern public static bool IsGpuDeviceSelectionSupported();

        [FreeFunction("InternalEditorUtilityBindings::GetGpuDevices")]
        extern public static string[] GetGpuDevices();

        [FreeFunction("InternalEditorUtilityBindings::OpenPlayerConsole")]
        extern public static void OpenPlayerConsole();

        public static string TextifyEvent(Event evt)
        {
            if (evt == null)
                return "none";

            string text = null;

            switch (evt.keyCode)
            {
                case KeyCode.Keypad0: text = "[0]"; break;
                case KeyCode.Keypad1: text = "[1]"; break;
                case KeyCode.Keypad2: text = "[2]"; break;
                case KeyCode.Keypad3: text = "[3]"; break;
                case KeyCode.Keypad4: text = "[4]"; break;
                case KeyCode.Keypad5: text = "[5]"; break;
                case KeyCode.Keypad6: text = "[6]"; break;
                case KeyCode.Keypad7: text = "[7]"; break;
                case KeyCode.Keypad8: text = "[8]"; break;
                case KeyCode.Keypad9: text = "[9]"; break;
                case KeyCode.KeypadPeriod: text = "[.]"; break;
                case KeyCode.KeypadDivide: text = "[/]"; break;
                case KeyCode.KeypadMinus: text = "[-]"; break;
                case KeyCode.KeypadPlus: text = "[+]"; break;
                case KeyCode.KeypadEquals: text = "[=]"; break;

                case KeyCode.KeypadEnter: text = "enter"; break;
                case KeyCode.UpArrow: text = "up"; break;
                case KeyCode.DownArrow: text = "down"; break;
                case KeyCode.LeftArrow: text = "left"; break;
                case KeyCode.RightArrow: text = "right"; break;

                case KeyCode.Insert: text = "insert"; break;
                case KeyCode.Home: text = "home"; break;
                case KeyCode.End: text = "end"; break;
                case KeyCode.PageUp: text = "page up"; break;
                case KeyCode.PageDown: text = "page down"; break;

                case KeyCode.Backspace: text = "backspace"; break;
                case KeyCode.Delete: text = "delete"; break;

                case KeyCode.F1: text = "F1"; break;
                case KeyCode.F2: text = "F2"; break;
                case KeyCode.F3: text = "F3"; break;
                case KeyCode.F4: text = "F4"; break;
                case KeyCode.F5: text = "F5"; break;
                case KeyCode.F6: text = "F6"; break;
                case KeyCode.F7: text = "F7"; break;
                case KeyCode.F8: text = "F8"; break;
                case KeyCode.F9: text = "F9"; break;
                case KeyCode.F10: text = "F10"; break;
                case KeyCode.F11: text = "F11"; break;
                case KeyCode.F12: text = "F12"; break;
                case KeyCode.F13: text = "F13"; break;
                case KeyCode.F14: text = "F14"; break;
                case KeyCode.F15: text = "F15"; break;
                case KeyCode.F16: text = "F16"; break;
                case KeyCode.F17: text = "F17"; break;
                case KeyCode.F18: text = "F18"; break;
                case KeyCode.F19: text = "F19"; break;
                case KeyCode.F20: text = "F20"; break;
                case KeyCode.F21: text = "F21"; break;
                case KeyCode.F22: text = "F22"; break;
                case KeyCode.F23: text = "F23"; break;
                case KeyCode.F24: text = "F24"; break;


                case KeyCode.Escape: text = "[esc]"; break;
                case KeyCode.Return: text = "return"; break;

                default: text = "" + evt.keyCode; break;
            }

            string modifiers = string.Empty;
            if (evt.alt)        modifiers += "Alt+";
            if (evt.command)    modifiers += Application.platform == RuntimePlatform.OSXEditor ? "Cmd+" : "Ctrl+";
            if (evt.control)    modifiers += "Ctrl+";
            if (evt.shift)      modifiers += "Shift+";

            return modifiers + text;
        }

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeProperty("defaultScreenWidth", TargetType.Field)]
        extern public static float defaultScreenWidth
        {
            get;
        }

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeProperty("defaultScreenHeight", TargetType.Field)]
        extern public static float defaultScreenHeight
        {
            get;
        }

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeProperty("defaultWebScreenWidth", TargetType.Field)]
        extern public static float defaultWebScreenWidth
        {
            get;
        }

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeProperty("defaultWebScreenHeight", TargetType.Field)]
        extern public static float defaultWebScreenHeight
        {
            get;
        }

        extern public static float remoteScreenWidth
        {
            [FreeFunction("RemoteScreenWidth")]
            get;
        }

        extern public static float remoteScreenHeight
        {
            [FreeFunction("RemoteScreenHeight")]
            get;
        }

        [FreeFunction]
        extern public static string[] GetAvailableDiffTools();

        [FreeFunction]
        extern public static string GetNoDiffToolsDetectedMessage();

        [FreeFunction("SetCustomDiffToolData")]
        extern internal static void SetCustomDiffToolData(string  path, string diff2Command, string diff3Command, string mergeCommand);

        [FreeFunction("SetCustomDiffToolPrefs")]
        extern internal static void SetCustomDiffToolPrefs(string  path, string diff2Command, string diff3Command, string mergeCommand);

        [FreeFunction("InternalEditorUtilityBindings::TransformBounds")]
        extern public static Bounds TransformBounds(Bounds b, Transform t);

        [StaticAccessor("CustomLighting::Get()", StaticAccessorType.Dot)]
        [NativeMethod("SetCustomLighting")]
        extern public static void SetCustomLightingInternal([Unmarshalled] Light[] lights, Color ambient);

        public static void SetCustomLighting(Light[] lights, Color ambient)
        {
            if (lights == null)
                throw new System.ArgumentNullException("lights");

            SetCustomLightingInternal(lights, ambient);
        }

        [StaticAccessor("CustomLighting::Get()", StaticAccessorType.Dot)]
        [NativeMethod("RestoreSceneLighting")]
        extern public static void RemoveCustomLighting();

        [StaticAccessor("GetRenderManager()", StaticAccessorType.Dot)]
        extern public static bool HasFullscreenCamera();

        public static Bounds CalculateSelectionBounds(bool usePivotOnlyForParticles)
        {
            return CalculateSelectionBounds(usePivotOnlyForParticles, false, false);
        }

        public static Bounds CalculateSelectionBounds(bool usePivotOnlyForParticles, bool onlyUseActiveSelection)
        {
            return CalculateSelectionBounds(usePivotOnlyForParticles, onlyUseActiveSelection, false);
        }

        [FreeFunction]
        extern public static Bounds CalculateSelectionBounds(bool usePivotOnlyForParticles, bool onlyUseActiveSelection, bool ignoreEditableField);

        internal static Bounds CalculateSelectionBoundsInSpace(Vector3 position, Quaternion rotation, bool rectBlueprintMode)
        {
            Quaternion inverseRotation = Quaternion.Inverse(rotation);
            Vector3 min = new Vector3(float.MaxValue - 1f, float.MaxValue - 1f, float.MaxValue - 1f);
            Vector3 max = new Vector3(float.MinValue + 1f, float.MinValue + 1f, float.MinValue + 1f);

            Vector3[] minmax = new Vector3[2];
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                Bounds localBounds = GetLocalBounds(gameObject);
                minmax[0] = localBounds.min;
                minmax[1] = localBounds.max;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 point = new Vector3(minmax[x].x, minmax[y].y, minmax[z].z);
                            if (rectBlueprintMode && SupportsRectLayout(gameObject.transform))
                            {
                                Vector3 localPosXY = gameObject.transform.localPosition;
                                localPosXY.z = 0;
                                point = gameObject.transform.parent.TransformPoint(point + localPosXY);
                            }
                            else
                            {
                                point = gameObject.transform.TransformPoint(point);
                            }

                            point = inverseRotation * (point - position);

                            for (int axis = 0; axis < 3; axis++)
                            {
                                min[axis] = Mathf.Min(min[axis], point[axis]);
                                max[axis] = Mathf.Max(max[axis], point[axis]);
                            }
                        }
                    }
                }
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        internal static bool SupportsRectLayout(Transform tr)
        {
            if (tr == null || tr.parent == null)
                return false;
            if (tr.GetComponent<RectTransform>() == null || tr.parent.GetComponent<RectTransform>() == null)
                return false;
            return true;
        }

        private static Bounds GetLocalBounds(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out RectTransform rectTransform))
                return new Bounds(rectTransform.rect.center, rectTransform.rect.size);

            // Account for case where there is a mesh filter but no renderer
            if (gameObject.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                return filter.sharedMesh.bounds;

            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                if (renderer is SpriteRenderer)
                    return ((SpriteRenderer)renderer).GetSpriteBounds();

                if (renderer is SpriteMask)
                    return ((SpriteMask)renderer).GetSpriteBounds();

                if (renderer is UnityEngine.U2D.SpriteShapeRenderer)
                    return ((UnityEngine.U2D.SpriteShapeRenderer)renderer).GetLocalAABB();

                if (renderer is UnityEngine.Tilemaps.TilemapRenderer && renderer.TryGetComponent(out UnityEngine.Tilemaps.Tilemap tilemap))
                    return tilemap.localBounds;
            }

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        [FreeFunction("SetPlayerFocus")]
        extern public static void OnGameViewFocus(bool focus);

        [FreeFunction("OpenScriptFile")]
        extern public static bool OpenFileAtLineExternal(string filename, int line, int column);

        public static bool OpenFileAtLineExternal(string filename, int line)
        {
            if (!CodeEditor.Editor.CurrentCodeEditor.OpenProject(filename, line))
            {
                return OpenFileAtLineExternal(filename, line, 0);
            }
            return true;
        }

        [FreeFunction("AssetDatabaseDeprecated::CanConnectToCacheServer")]
        extern public static bool CanConnectToCacheServer();

        [FreeFunction]
        extern public static DllType DetectDotNetDll(string path);

        [FreeFunction]
        internal static extern bool IsDotNetDll(string path);

        public static bool IsDotNet4Dll(string path)
        {
            var dllType = DetectDotNetDll(path);
            switch (dllType)
            {
                case UnityEditorInternal.DllType.Unknown:
                case UnityEditorInternal.DllType.Native:
                case UnityEditorInternal.DllType.UnknownManaged:
                case UnityEditorInternal.DllType.ManagedNET35:
                    return false;
                case UnityEditorInternal.DllType.ManagedNET40:
                case UnityEditorInternal.DllType.WinMDNative:
                case UnityEditorInternal.DllType.WinMDNET40:
                    return true;
                default:
                    throw new Exception(string.Format("Unknown dll type: {0}", dllType));
            }
        }

        internal static bool RunningUnderWindows8(bool orHigher = true)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                OperatingSystem sys = System.Environment.OSVersion;
                int major = sys.Version.Major;
                int minor = sys.Version.Minor;
                // Window 8 is technically version 6.2
                if (orHigher)
                    return major > 6 || (major == 6 && minor >= 2);
                else
                    return major == 6 && minor == 2;
            }
            return false;
        }

        [FreeFunction("UnityExtensions::IsValidExtensionPath")]
        extern internal static bool IsValidUnityExtensionPath(string path);

        [StaticAccessor("UnityExtensions::Get()", StaticAccessorType.Dot)]
        [NativeMethod("IsRegistered")]
        extern internal static bool IsUnityExtensionRegistered(string filename);

        [StaticAccessor("UnityExtensions::Get()", StaticAccessorType.Dot)]
        [NativeMethod("IsCompatibleWithEditor")]
        extern internal static bool IsUnityExtensionCompatibleWithEditor(BuildTarget target, string path);

        [FreeFunction(IsThreadSafe = true)]
        extern public static bool CurrentThreadIsMainThread();

        // Internal property to check if the currently selected Assets can be renamed, used to unify rename logic between native and c#
        extern internal static bool canRenameSelectedAssets
        {
            [FreeFunction("CanRenameSelectedAssets")]
            get;
        }

        [FreeFunction("InternalEditorUtilityBindings::GetCrashReportFolder")]
        extern public static string GetCrashReportFolder();

        [FreeFunction("InternalEditorUtilityBindings::GetCrashHandlerProcessID")]
        extern public static UInt32 GetCrashHandlerProcessID();

        [FreeFunction("InternalEditorUtilityBindings::DrawSkyboxMaterial")]
        extern internal static void DrawSkyboxMaterial([NotNull] Material mat, [NotNull] Camera cam);

        [FreeFunction("InternalEditorUtilityBindings::ResetCursor")]
        extern public static void ResetCursor();

        [FreeFunction("InternalEditorUtilityBindings::VerifyCacheServerIntegrity")]
        extern public static UInt64 VerifyCacheServerIntegrity();

        [FreeFunction("InternalEditorUtilityBindings::FixCacheServerIntegrityErrors")]
        extern public static UInt64 FixCacheServerIntegrityErrors();

        [FreeFunction]
        extern public static int DetermineDepthOrder(Transform lhs, Transform rhs);


        internal static PrecompiledAssembly[] GetUnityAssemblies(bool buildingForEditor, BuildTarget target)
        {
            return GetUnityAssembliesInternal(buildingForEditor, target);
        }

        [FreeFunction("GetUnityAssembliesManaged")]
        extern private static PrecompiledAssembly[] GetUnityAssembliesInternal(bool buildingForEditor, BuildTarget target);

        [FreeFunction("GetPrecompiledAssemblyPathsManaged")]
        extern internal static string[] GetPrecompiledAssemblyPaths();

        [FreeFunction("GetEditorAssembliesPath")]
        extern internal static string GetEditorScriptAssembliesPath();

        [Obsolete("The Module Manager is deprecated", error: true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void ShowPackageManagerWindow() { throw new NotSupportedException("The Module Manager is deprecated"); }

        // For testing Vector2 marshalling
        [FreeFunction("InternalEditorUtilityBindings::PassAndReturnVector2")]
        extern public static Vector2 PassAndReturnVector2(Vector2 v);

        // For testing Color32 marshalling
        [FreeFunction("InternalEditorUtilityBindings::PassAndReturnColor32")]
        extern public static Color32 PassAndReturnColor32(Color32 c);

        [FreeFunction("InternalEditorUtilityBindings::SaveCursorToFile")]
        extern public static bool SaveCursorToFile(string path, Texture2D image, Vector2 hotSpot);

        [FreeFunction("InternalEditorUtilityBindings::SaveCursorToInMemoryResource")]
        extern internal static bool SaveCursorToInMemoryResource(Texture2D image, Vector2 hotSpot, ushort cursorDataResourceId, IntPtr cursorDirectoryBuffer, uint cursorDirectoryBufferSize, IntPtr cursorDataBuffer, uint cursorDataBufferSize);

        [FreeFunction("GetScriptCompilationDefines")]
        extern internal static string[] GetCompilationDefines(EditorScriptCompilationOptions options, BuildTarget target, int subtarget, ApiCompatibilityLevel apiCompatibilityLevel, string[] extraDefines = null);

        //Launches an application that is kept alive, even during a domain reload
        [FreeFunction("LaunchApplication")]
        extern internal static bool LaunchApplication(string path, string[] arguments);

        public static string CountToString(ulong count)
        {
            string[] names = {"G", "M", "k", ""};
            float[] magnitudes = {1000000000.0f, 1000000.0f, 1000.0f, 1.0f};

            int index = 0;
            while (index < 3 && count < (magnitudes[index] / 2.0f))
            {
                index++;
            }
            float result = count / magnitudes[index];
            return result.ToString("0.0", CultureInfo.InvariantCulture.NumberFormat) + names[index];
        }

        [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
        [NativeMethod("EnsureUntitledSceneHasBeenSaved")]
        [Obsolete("use EditorSceneManager.EnsureUntitledSceneHasBeenSaved")]
        extern public static bool EnsureSceneHasBeenSaved(string operation);

        internal static void PrepareDragAndDropTesting(EditorWindow editorWindow)
        {
            if (editorWindow.m_Parent != null)
                PrepareDragAndDropTestingInternal(editorWindow.m_Parent);
        }

        [FreeFunction("InternalEditorUtilityBindings::PrepareDragAndDropTestingInternal")]
        extern internal static void PrepareDragAndDropTestingInternal(GUIView guiView);

        [FreeFunction("InternalEditorUtilityBindings::LayerMaskToConcatenatedLayersMaskInternal")]
        extern private static int LayerMaskToConcatenatedLayersMaskInternal(int mask);

        [StaticAccessor("GetApplication()", StaticAccessorType.Dot)]
        internal static extern bool IsScriptReloadRequested();

        [FreeFunction("HandleProjectWindowFileDrag")]
        internal static extern DragAndDropVisualMode HandleProjectWindowFileDrag(string newParentPath, string[] paths, bool perform, int defaultPromptAction);

        //Drag and drop current selection to project window
        //Set prefabVariantCreationModeDialogChoice to -1 to allow dialogs to be shown.
        //If set to 0 or 1 no dialogs will be shown. Using a value of 0 is similar to choosing PrefabVariantCreationMode::kOriginal and a value of 1 is similar to choosing PrefabVariantCreationMode::kVariant from the "Create Prefab or Variant" dialog if dropping a Prefab instance to the Project Browser.
        //Non-zero values also blocks the "Cyclic References" error dialog, and it defaults the "Possibly unwanted Prefab replacement" menu to return "Replace Anyway".
        [FreeFunction("InternalEditorUtilityBindings::DragAndDropPrefabsFromHierarchyToProjectWindow")]
        internal static extern DragAndDropVisualMode DragAndDropPrefabsFromHierarchyToProjectWindow(string destAssetGuid, bool perform, int prefabVariantCreationModeDialogChoice);

        [FreeFunction]
        [NativeHeader("Editor/Src/Undo/DefaultParentObjectUndo.h")]
        internal static extern void RegisterSetDefaultParentObjectUndo(string sceneGUID, int instanceID, string undoName);

        // Aux window functionality is quite brittle. It is strongly advised to avoid
        // using this method but if you really need it, consult Desktop team first.
        [StaticAccessor("GetAuxWindowManager()", StaticAccessorType.Dot)]
        internal static extern void RetainAuxWindows();

        [FreeFunction]
        internal static extern bool IsPlaybackEngineDisabled(string engineName);
    }
}
