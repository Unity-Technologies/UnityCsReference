// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;

using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.Scripting.ScriptCompilation;


namespace UnityEditorInternal
{
public enum CanAppendBuild
{
    Unsupported = 0,
    Yes = 1,
    No = 2,
}

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

public sealed partial class InternalEditorUtility
{
    public extern static bool isApplicationActive
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool inBatchMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool isHumanControllingUs
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void BumpMapSettingsFixingWindowReportResult (int result) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool BumpMapTextureNeedsFixingInternal (Material material, string propName, bool flaggedAsNormal) ;

    internal static bool BumpMapTextureNeedsFixing(MaterialProperty prop)
        {
            if (prop.type != MaterialProperty.PropType.Texture)
                return false;

            bool hintIfNormal = ((prop.flags & MaterialProperty.PropFlags.Normal) != 0);

            foreach (Material material in prop.targets)
                if (BumpMapTextureNeedsFixingInternal(material, prop.name, hintIfNormal))
                    return true;

            return false;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void FixNormalmapTextureInternal (Material material, string propName) ;

    internal static void FixNormalmapTexture(MaterialProperty prop)
        {
            foreach (Material material in prop.targets)
                FixNormalmapTextureInternal(material, prop.name);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetEditorAssemblyPath () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetEngineAssemblyPath () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetEngineCoreModuleAssemblyPath () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string CalculateHashForObjectsAndDependencies (Object[] objects) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExecuteCommandOnKeyWindow (string commandName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Material[] InstantiateMaterialsInEditMode (Renderer renderer) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  CanAppendBuild BuildCanBeAppended (BuildTarget target, string location) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void RegisterExtensionDll (string dllLocation, string guid) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void RegisterPrecompiledAssembly (string dllName, string dllLocation) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Assembly LoadAssemblyWrapper (string dllName, string dllLocation) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetPlatformPath (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int AddScriptComponentUncheckedUndoable (GameObject gameObject, MonoScript script) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int CreateScriptableObjectUnchecked (MonoScript script) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RequestScriptReload () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SwitchSkinAndRepaintAllViews () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RepaintAllViews () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetIsInspectorExpanded (Object obj) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetIsInspectorExpanded (Object obj, bool isExpanded) ;

    public extern static int[] expandedProjectWindowItems
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SaveToSerializedFileAndForget (Object[] obj, string path, bool allowTextSerialization) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] LoadSerializedFileAndForget (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  DragAndDropVisualMode ProjectWindowDrag (HierarchyProperty property, bool perform) ;

public enum HierarchyDropMode    
    {
        kHierarchyDragNormal = 0,
        kHierarchyDropUpon = 1 << 0,
        kHierarchyDropBetween = 1 << 1,
        kHierarchyDropAfterParent = 1 << 2,
        kHierarchySearchActive = 1 << 3
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  DragAndDropVisualMode HierarchyWindowDrag (HierarchyProperty property, bool perform, HierarchyDropMode dropMode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  DragAndDropVisualMode InspectorWindowDrag (Object[] targets, bool perform) ;

    public static DragAndDropVisualMode SceneViewDrag (Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, bool perform) {
        return INTERNAL_CALL_SceneViewDrag ( dropUpon, ref worldPosition, ref viewportPosition, perform );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static DragAndDropVisualMode INTERNAL_CALL_SceneViewDrag (Object dropUpon, ref Vector3 worldPosition, ref Vector2 viewportPosition, bool perform);
    public static void SetRectTransformTemporaryRect (RectTransform rectTransform, Rect rect) {
        INTERNAL_CALL_SetRectTransformTemporaryRect ( rectTransform, ref rect );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetRectTransformTemporaryRect (RectTransform rectTransform, ref Rect rect);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasTeamLicense () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasPro () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasFreeLicense () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasEduLicense () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasAdvancedLicenseOnBuildTarget (BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsMobilePlatform (BuildTarget target) ;

    public static Rect GetBoundsOfDesktopAtPoint (Vector2 pos) {
        Rect result;
        INTERNAL_CALL_GetBoundsOfDesktopAtPoint ( ref pos, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetBoundsOfDesktopAtPoint (ref Vector2 pos, out Rect value);
    public extern static string[] tags
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RemoveTag (string tag) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void AddTag (string tag) ;

    public extern static string[] layers
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string[] GetLayersWithId () ;

    public static LayerMask ConcatenatedLayersMaskToLayerMask (int concatenatedLayersMask) {
        LayerMask result;
        INTERNAL_CALL_ConcatenatedLayersMaskToLayerMask ( concatenatedLayersMask, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ConcatenatedLayersMaskToLayerMask (int concatenatedLayersMask, out LayerMask value);
    public static int LayerMaskToConcatenatedLayersMask (LayerMask mask) {
        return INTERNAL_CALL_LayerMaskToConcatenatedLayersMask ( ref mask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_LayerMaskToConcatenatedLayersMask (ref LayerMask mask);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetSortingLayerName (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetSortingLayerUniqueID (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetSortingLayerNameFromUniqueID (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetSortingLayerCount () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetSortingLayerName (int index, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetSortingLayerLocked (int index, bool locked) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool GetSortingLayerLocked (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsSortingLayerDefault (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void AddSortingLayer () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void UpdateSortingLayersOrder () ;

    internal extern static string[] sortingLayerNames
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern static int[] sortingLayerUniqueIDs
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static Vector4 GetSpriteOuterUV (Sprite sprite, bool getAtlasData) {
        Vector4 result;
        INTERNAL_CALL_GetSpriteOuterUV ( sprite, getAtlasData, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSpriteOuterUV (Sprite sprite, bool getAtlasData, out Vector4 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object GetObjectFromInstanceID (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Type GetTypeWithoutLoadingObject (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object GetLoadedObjectFromInstanceID (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetLayerName (int layer) ;

    public extern static string unityPreferencesFolder
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetAssetsFolder () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetEditorFolder () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsInEditorFolder (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ReloadWindowLayoutMenu () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RevertFactoryLayoutSettings (bool quitOnCancel) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void LoadDefaultLayout () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void CalculateAmbientProbeFromSkybox () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetupShaderMenu (Material material) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetFullUnityVersion () ;

    public static Version GetUnityVersion()
        {
            Version version = new Version(GetUnityVersionDigits());
            return new Version(version.Major, version.Minor, version.Build, GetUnityRevision());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetUnityVersionDigits () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetUnityBuildBranch () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetUnityVersionDate () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetUnityRevision () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsUnityBeta () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetUnityCopyright () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetLicenseInfo () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int[] GetLicenseFlags () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetAuthToken () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void OpenEditorConsole () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetGameObjectInstanceIDFromComponent (int instanceID) ;

    public static Color[] ReadScreenPixel (Vector2 pixelPos, int sizex, int sizey) {
        return INTERNAL_CALL_ReadScreenPixel ( ref pixelPos, sizex, sizey );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Color[] INTERNAL_CALL_ReadScreenPixel (ref Vector2 pixelPos, int sizex, int sizey);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetGpuDeviceAndRecreateGraphics (int index, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsGpuDeviceSelectionSupported () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetGpuDevices () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void OpenPlayerConsole () ;

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
    
    
    public extern static float defaultScreenWidth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static float defaultScreenHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static float defaultWebScreenWidth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static float defaultWebScreenHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static float remoteScreenWidth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static float remoteScreenHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetAvailableDiffTools () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetNoDiffToolsDetectedMessage () ;

    public static Bounds TransformBounds (Bounds b, Transform t) {
        Bounds result;
        INTERNAL_CALL_TransformBounds ( ref b, t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_TransformBounds (ref Bounds b, Transform t, out Bounds value);
    public static void SetCustomLightingInternal (Light[] lights, Color ambient) {
        INTERNAL_CALL_SetCustomLightingInternal ( lights, ref ambient );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetCustomLightingInternal (Light[] lights, ref Color ambient);
    public static void SetCustomLighting(Light[] lights, Color ambient)
        {
            if (lights == null)
                throw new System.ArgumentNullException("lights");

            SetCustomLightingInternal(lights, ambient);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void ClearSceneLighting () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RemoveCustomLighting () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void DrawSkyboxMaterial (Material mat, Camera cam) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasFullscreenCamera () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ResetCursor () ;

    public static Bounds CalculateSelectionBounds (bool usePivotOnlyForParticles, bool onlyUseActiveSelection) {
        Bounds result;
        INTERNAL_CALL_CalculateSelectionBounds ( usePivotOnlyForParticles, onlyUseActiveSelection, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CalculateSelectionBounds (bool usePivotOnlyForParticles, bool onlyUseActiveSelection, out Bounds value);
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
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform)
            {
                return new Bounds((Vector3)rectTransform.rect.center, rectTransform.rect.size);
            }

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer is MeshRenderer)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    return filter.sharedMesh.bounds;
            }
            if (renderer is SpriteRenderer)
            {
                return ((SpriteRenderer)renderer).GetSpriteBounds();
            }
            if (renderer is SpriteMask)
            {
                return ((SpriteMask)renderer).GetSpriteBounds();
            }
            if (renderer is UnityEngine.Tilemaps.TilemapRenderer)
            {
                UnityEngine.Tilemaps.Tilemap tilemap = renderer.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                if (tilemap != null)
                    return tilemap.localBounds;
            }
            return new Bounds(Vector3.zero, Vector3.zero);
        }
    
    
    public extern static bool ignoreInspectorChanges
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void OnGameViewFocus (bool focus) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool OpenFileAtLineExternal (string filename, int line) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool WiiUSaveStartupScreenToFile (Texture2D image, string path, int outputWidth, int outputHeight) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool CanConnectToCacheServer () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  UInt64 VerifyCacheServerIntegrity () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  UInt64 FixCacheServerIntegrityErrors () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  DllType DetectDotNetDll (string path) ;

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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetCrashReportFolder () ;

    [uei.ExcludeFromDocs]
internal static bool RunningUnderWindows8 () {
    bool orHigher = true;
    return RunningUnderWindows8 ( orHigher );
}

internal static bool RunningUnderWindows8( [uei.DefaultValue("true")] bool orHigher )
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                OperatingSystem sys = System.Environment.OSVersion;
                int major = sys.Version.Major;
                int minor = sys.Version.Minor;
                if (orHigher)
                    return major > 6 || (major == 6 && minor >= 2);
                else
                    return major == 6 && minor == 2;
            }
            return false;
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int DetermineDepthOrder (Transform lhs, Transform rhs) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ShowPackageManagerWindow () ;

    public static Vector2 PassAndReturnVector2 (Vector2 v) {
        Vector2 result;
        INTERNAL_CALL_PassAndReturnVector2 ( ref v, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_PassAndReturnVector2 (ref Vector2 v, out Vector2 value);
    public static Color32 PassAndReturnColor32 (Color32 c) {
        Color32 result;
        INTERNAL_CALL_PassAndReturnColor32 ( ref c, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_PassAndReturnColor32 (ref Color32 c, out Color32 value);
    public static string CountToString(ulong count)
        {
            string[] names = {"g", "m", "k", ""};
            float[] magnitudes = {1000000000.0f, 1000000.0f, 1000.0f, 1.0f};

            int index = 0;
            while (index < 3 && count < (magnitudes[index] / 2.0f))
            {
                index++;
            }
            float result = count / magnitudes[index];
            return result.ToString("0.0") + names[index];
        }
    
    
    [System.Obsolete ("use EditorSceneManager.EnsureUntitledSceneHasBeenSaved")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool EnsureSceneHasBeenSaved (string operation) ;

    internal static void PrepareDragAndDropTesting(EditorWindow editorWindow)
        {
            if (editorWindow.m_Parent != null)
                PrepareDragAndDropTestingInternal(editorWindow.m_Parent);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void PrepareDragAndDropTestingInternal (GUIView guiView) ;

    public static bool SaveCursorToFile (string path, Texture2D image, Vector2 hotSpot) {
        return INTERNAL_CALL_SaveCursorToFile ( path, image, ref hotSpot );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_SaveCursorToFile (string path, Texture2D image, ref Vector2 hotSpot);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool LaunchApplication (string path, string[] arguments) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string[] GetCompilationDefines (EditorScriptCompilationOptions options, BuildTargetGroup targetGroup, BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  PrecompiledAssembly[] GetUnityAssemblies (bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  PrecompiledAssembly[] GetPrecompiledAssemblies (bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetEditorProfile () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsUnityExtensionsInitialized () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsValidUnityExtensionPath (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsUnityExtensionRegistered (string filename) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsUnityExtensionCompatibleWithEditor (BuildTargetGroup targetGroup, BuildTarget target, string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string[] GetEditorModuleDllNames () ;

}

}
