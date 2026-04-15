// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditor.AssetImporters;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor;

[NativeHeader("Editor/Src/Utility/HierarchyProperty.bindings.h")]
[StructLayout(LayoutKind.Sequential)]
public sealed class HierarchyIterator : IHierarchyIterator
{
    [Obsolete]
    internal static HierarchyIterator UnsafeCastFrom(HierarchyProperty iterator)
    {
        if (iterator == null)
            throw new ArgumentNullException(nameof(iterator));
        return new HierarchyIterator{m_Ptr = iterator.m_Ptr};
    }

    HierarchyIterator(){}//Don't use this constructor, use the other constructors instead.

    /// <summary>
    /// Pointer to the native HierarchyProperty object. Please don't use, only internal because we can then cast it to the old HierarchyProperty.
    /// </summary>
    internal IntPtr m_Ptr;

    public HierarchyIterator(HierarchyType hierarchyType)
        : this(hierarchyType, "Assets", true)
    {
    }

    public HierarchyIterator(HierarchyType hierarchyType, bool forceImport)
        : this(hierarchyType, "Assets", forceImport)
    {
    }

    public HierarchyIterator(string rootPath)
        : this(HierarchyType.Assets, rootPath, true)
    {
    }

    public HierarchyIterator(string rootPath, bool forceImport)
        : this(HierarchyType.Assets, rootPath, forceImport)
    {
    }

    public HierarchyIterator(HierarchyType hierarchyType, string rootPath, bool forceImport)
    {
        m_Ptr = Internal_Create(hierarchyType, rootPath, forceImport);
    }

    [FreeFunction("HierarchyPropertyBindings::Internal_Create")]
    static extern IntPtr Internal_Create(HierarchyType hierarchyType, string rootPath, bool forceImport);

    internal void SetCustomScenes(Scene[] scenes)
    {
        if (scenes == null)
            throw new ArgumentNullException(nameof(scenes));
        var sceneHandles = new SceneHandle[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            sceneHandles[i] = scenes[i].handle;
        SetCustomScenes(sceneHandles);
    }

    [FreeFunction("HierarchyPropertyBindings::SetCustomScenes", HasExplicitThis = true)]
    public extern void SetCustomScenes([NotNull] SceneHandle[] sceneHandles);
    [FreeFunction("HierarchyPropertyBindings::SetSubScenes", HasExplicitThis = true)]
    public extern void SetSubScenes([NotNull] SceneHierarchyHooks.SubSceneInfo[] subScenes);
    public extern void Reset();

    [FreeFunction("HierarchyPropertyBindings::Internal_Destroy", IsThreadSafe = true)]
    static extern void Internal_Destroy(IntPtr ptr);

    void Dispose(bool disposing)
    {
        if (m_Ptr != IntPtr.Zero)
        {
            Internal_Destroy(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }
    }

    void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~HierarchyIterator() { Dispose(false); }

    public extern EntityId entityId { get; }
    public extern Object pptrValue { [FreeFunction("HierarchyPropertyBindings::PPtrValue", HasExplicitThis = true)] get; }
    public extern string name { get; }

    [FreeFunction("HierarchyPropertyBindings::GetScene", HasExplicitThis = true, ThrowsException = true)]
    public extern Scene GetScene();

    public extern bool hasChildren { [NativeName("HasChildren")] get; }
    public extern int depth { get; }
    public extern EntityId[] ancestors { get; }
    public extern int row { get; }
    public extern int colorCode { get; }

    [FreeFunction("HierarchyPropertyBindings::IsExpanded", HasExplicitThis = true)]
    private extern bool IsExpanded_internal(EntityId[] expanded, bool nonNullEmptyArray);

    public bool IsExpanded(EntityId[] expanded) => IsExpanded_internal(expanded, expanded != null && expanded.Length == 0);

    public extern string guid { [FreeFunction("HierarchyPropertyBindings::GetGuid", HasExplicitThis = true)] get; }
    public extern GUID assetGUID { [FreeFunction("HierarchyPropertyBindings::GetAssetGUID", HasExplicitThis = true)] get; }
    public extern bool alphaSorted { [NativeName("IsAlphaSorted")] get; set; }
    public extern bool showSceneHeaders { [NativeName("IsShowingSceneHeaders")] get; [NativeName("SetShowingSceneHeaders")] set; }
    public extern bool isSceneHeader { [NativeName("IsSceneHeader")] get; }
    public extern bool isValid { [NativeName("IsValid")] get; }
    public extern bool isMainRepresentation { [NativeName("IsMainAssetRepresentation")] get; }
    public extern bool hasFullPreviewImage { [NativeName("HasFullPreviewImage")] get; }
    public extern IconDrawStyle iconDrawStyle { get; }
    public extern bool isFolder { [NativeName("IsFolder")] get; }
    public extern GUID[] dynamicDependencies { get; }

    [FreeFunction("HierarchyPropertyBindings::Next", HasExplicitThis = true)]
    private extern bool Next_internal(EntityId[] expanded, bool nonNullEmptyArray);
    public bool Next(EntityId[] expanded) => Next_internal(expanded, expanded != null && expanded.Length == 0);
    [FreeFunction("HierarchyPropertyBindings::NextWithDepthCheck", HasExplicitThis = true)]
    private extern bool NextWithDepthCheck_internal(EntityId[] expanded, int minDepth, bool nonNullEmptyArray);
    public bool NextWithDepthCheck(EntityId[] expanded, int minDepth) => NextWithDepthCheck_internal(expanded, minDepth, expanded != null && expanded.Length == 0);
    [FreeFunction("HierarchyPropertyBindings::Previous", HasExplicitThis = true)]
    private extern bool Previous_internal(EntityId[] expanded, bool nonNullEmptyArray);
    public bool Previous(EntityId[] expanded) => Previous_internal(expanded, expanded != null && expanded.Length == 0);
    public extern bool Parent();
    [FreeFunction("HierarchyPropertyBindings::Find", HasExplicitThis = true)]
    private extern bool Find_internal(EntityId instanceID, EntityId[] expanded, bool nonNullEmptyArray);
    public bool Find(EntityId entityId, EntityId[] expanded) => Find_internal(entityId, expanded, expanded != null && expanded.Length == 0);
    [FreeFunction("HierarchyPropertyBindings::Skip", HasExplicitThis = true)]
    private extern bool Skip_internal(int count, EntityId[] expanded, bool nonNullEmptyArray);
    public bool Skip(int count, EntityId[] expanded) =>Skip_internal(count, expanded, expanded != null && expanded.Length == 0);
    [FreeFunction("HierarchyPropertyBindings::CountRemaining", HasExplicitThis = true)]
    private extern int CountRemaining_internal(EntityId[] expanded, bool nonNullEmptyArray);
    public int CountRemaining(EntityId[] expanded) => CountRemaining_internal(expanded, expanded != null && expanded.Length == 0);

    public extern EntityId GetEntityIdIfImported();

    public extern Texture2D icon { [NativeName("GetCachedIcon")] get; }

    [RequiredByNativeCode]
    static void SetFilterFromNativePtr(IntPtr nativePtr, string filter)
    {
        var hierarchy = new HierarchyIterator { m_Ptr = nativePtr };

        try
        {
            SearchFilter search = new SearchFilter();
            SearchUtility.ParseSearchString(filter, search);
            hierarchy.SetSearchFilter(search);
        }
        finally
        {
            // Clear to prevent finalizer from deleting the borrowed native pointer
            hierarchy.m_Ptr = IntPtr.Zero;
        }
    }

    // Pre 4.0 interface (kept for backwards compability)
    public void SetSearchFilter(string searchString, int mode)
    {
        SearchFilter filter = SearchableEditorWindow.CreateFilter(searchString, (SearchableEditorWindow.SearchMode)mode);
        SetSearchFilter(filter);
    }

    // 4.0 interface (made internal for now)
    internal void SetSearchFilter(SearchFilter filter)
    {
        SetSearchFilterImpl(SearchFilter.Split(filter.nameFilter), filter.classNames, filter.assetLabels, filter.assetBundleNames, Array.Empty<string>(), Array.Empty<string>(), filter.referencingEntityIds, filter.sceneHandles, filter.GlobToRegex(), filter.productIds, filter.anyWithAssetOrigin, filter.showAllHits, filter.importLogFlags, filter.filterByTypeIntersection, filter.excludeSceneAssets);
    }

    internal void CopySearchFilterFrom(HierarchyIterator other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
        CopySearchFilterImpl(other);
    }

    [FreeFunction("HierarchyPropertyBindings::SetSearchFilterImpl", HasExplicitThis = true)]
    extern void SetSearchFilterImpl(string[] nameFilters, string[] classNames, string[] assetLabels, string[] assetBundleNames, string[] versionControlStates, string[] softLockControlStates, EntityId[] referencingEntityIds, SceneHandle[] sceneHandles, string[] regex, int[] productIds, bool anyWithAssetOrigin, bool showAllHits, ImportLogFlags importLogFlags, bool filterByTypeIntersection, bool excludeSceneAssets);

    [FreeFunction("HierarchyPropertyBindings::CopySearchFilterImpl", HasExplicitThis = true)]
    extern void CopySearchFilterImpl(HierarchyIterator other);

    [FreeFunction("HierarchyPropertyBindings::FindAllAncestors", HasExplicitThis = true)]
    public extern EntityId[] FindAllAncestors(EntityId[] entityIds);
    [FreeFunction("HierarchyPropertyBindings::ClearSceneObjectsFilter")]
    public static extern void ClearSceneObjectsFilter();

    internal static void ClearSceneObjectsFilterInScene(Scene[] scenes)
    {
        if (scenes == null)
            throw new ArgumentNullException(nameof(scenes));
        var sceneHandles = new SceneHandle[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            sceneHandles[i] = scenes[i].handle;
        ClearSceneObjectsFilterInScene(sceneHandles);
    }

    [FreeFunction("HierarchyPropertyBindings::ClearSceneObjectsFilterInScene")]
    internal static extern void ClearSceneObjectsFilterInScene([NotNull] SceneHandle[] sceneHandles);

    [FreeFunction("HierarchyPropertyBindings::FilterSingleSceneObject")]
    public static extern void FilterSingleSceneObject(EntityId instanceID, bool otherVisibilityState);

    internal static void FilterSingleSceneObjectInScene(EntityId instanceID, bool otherVisibilityState, Scene[] scenes)
    {
        if (scenes == null)
            throw new ArgumentNullException(nameof(scenes));
        var sceneHandles = new SceneHandle[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            sceneHandles[i] = scenes[i].handle;
        FilterSingleSceneObjectInScene(instanceID, otherVisibilityState, sceneHandles);
    }

    [FreeFunction("HierarchyPropertyBindings::FilterSingleSceneObjectInScene")]
    internal static extern void FilterSingleSceneObjectInScene(EntityId instanceID, bool otherVisibilityState, [NotNull] SceneHandle[] sceneHandles);

    [FreeFunction("HierarchyPropertyBindings::SetFilteredVisibility", HasExplicitThis = true)]
    internal extern void SetFilteredVisibility(bool visible);

    internal static class BindingsMarshaller
    {
        public static IntPtr ConvertToNative(HierarchyIterator prop) => prop.m_Ptr;

        public static IntPtr ConvertToUnmanaged(HierarchyIterator prop)
            => prop != null ? prop.m_Ptr : IntPtr.Zero;
    }
}

interface IHierarchyIterator
{
    void Reset();
    EntityId entityId { get; }
    Object pptrValue { get; }
    string name { get; }
    bool hasChildren { get; }
    int depth { get; }
    int row { get; }
    int colorCode { get; }
    string guid { get; }
    GUID assetGUID { get; }
    Texture2D icon { get; }
    bool isValid { get; }
    bool isMainRepresentation { get; }
    bool hasFullPreviewImage { get; }
    IconDrawStyle iconDrawStyle { get; }
    bool isFolder { get; }
    GUID[] dynamicDependencies { get; }

    bool IsExpanded(EntityId[] expanded);
    bool Next(EntityId[] expanded);
    bool NextWithDepthCheck(EntityId[] expanded, int minDepth);
    bool Previous(EntityId[] expanded);
    bool Parent();

    EntityId[] ancestors { get; }

    bool Find(EntityId _EntityId, EntityId[] expanded);
    EntityId[] FindAllAncestors(EntityId[] entityIds);

    bool Skip(int count, EntityId[] expanded);
    int CountRemaining(EntityId[] expanded);
    EntityId GetEntityIdIfImported();
}
