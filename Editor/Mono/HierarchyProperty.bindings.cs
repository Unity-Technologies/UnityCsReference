// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;
using UnityEditor.AssetImporters;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public enum InspectorMode
    {
        Normal = 0,
        Debug = 1,
        DebugInternal = 2
    }

    public enum HierarchyType
    {
        Assets = 1,
        GameObjects = 2,
    }

    public enum IconDrawStyle
    {
        NonTexture = 0,
        Texture = 1,
    }

    [NativeHeader("Editor/Src/Utility/HierarchyProperty.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [Obsolete("HierarchyProperty is deprecated. Use HierarchyIterator instead.", true)]
    public sealed class HierarchyProperty
    {
        internal static HierarchyProperty UnsafeCastFrom(HierarchyIterator iterator)
        {
            if (iterator == null)
                throw new ArgumentNullException(nameof(iterator));
            return new HierarchyProperty{m_Ptr = iterator.m_Ptr};
        }

        HierarchyProperty(){}//Don't use this constructor, use the other constructors instead.


        /// <summary>
        /// Pointer to the native HierarchyProperty object. Please don't use, only internal because we can then cast it to the old HierarchyProperty.
        /// </summary>
        internal IntPtr m_Ptr;

        public HierarchyProperty(HierarchyType hierarchyType)
            : this(hierarchyType, "Assets", true)
        {
        }

        public HierarchyProperty(HierarchyType hierarchyType, bool forceImport)
            : this(hierarchyType, "Assets", forceImport)
        {
        }

        public HierarchyProperty(string rootPath)
            : this(HierarchyType.Assets, rootPath, true)
        {
        }

        public HierarchyProperty(string rootPath, bool forceImport)
            : this(HierarchyType.Assets, rootPath, forceImport)
        {
        }

        public HierarchyProperty(HierarchyType hierarchyType, string rootPath, bool forceImport)
        {
            m_Ptr = Internal_Create(hierarchyType, rootPath, forceImport);
        }

        [FreeFunction("HierarchyPropertyBindings::Internal_Create")]
        static extern IntPtr Internal_Create(HierarchyType hierarchyType, string rootPath, bool forceImport);

        internal void SetCustomScenes(Scene[] scenes)
        {
            if (scenes == null)
                throw new ArgumentNullException(nameof(scenes));
            SetCustomScenes(Array.ConvertAll(scenes, s => s.handle));
        }

        [FreeFunction("HierarchyPropertyBindings::SetCustomScenes", HasExplicitThis = true)]
        public extern void SetCustomScenes([NotNull] SceneHandle[] sceneHandles);

        [Obsolete("SetCustomScenes(int[]) is deprecated. Use SetCustomScenes(SceneHandle[]) instead.",true)]
        public void SetCustomScenes(int[] sceneHandles) => SetCustomScenes(sceneHandles?.ToSceneHandleArray() ?? Array.Empty<SceneHandle>());

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

        ~HierarchyProperty() { Dispose(false); }

        extern EntityId entityId { get; }
        public int instanceID => entityId;
        public extern Object pptrValue { [FreeFunction("HierarchyPropertyBindings::PPtrValue", HasExplicitThis = true)] get; }
        public extern string name { get; }

        [FreeFunction("HierarchyPropertyBindings::GetScene", HasExplicitThis = true, ThrowsException = true)]
        public extern Scene GetScene();

        public extern bool hasChildren { [NativeName("HasChildren")] get; }
        public extern int depth { get; }
        public int[] ancestors { get { throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead."); } }
        public extern int row { get; }
        public extern int colorCode { get; }

        public bool IsExpanded(int[] expanded) => throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead.");

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

        public bool Next(int[] expanded) => throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead.");
        public bool NextWithDepthCheck(int[] expanded, int minDepth) => throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead.");
        public bool Previous(int[] expanded) => throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead.");
        public extern bool Parent();
        public bool Find(int instanceID, int[] expanded) => throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead.");
        public bool Skip(int count, int[] expanded) => throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead.");
        public int CountRemaining(int[] expanded) => throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead.");

        internal extern EntityId GetEntityIdIfImported();
        public int GetInstanceIDIfImported() => GetEntityIdIfImported();

        public extern Texture2D icon { [NativeName("GetCachedIcon")] get; }

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

        [FreeFunction("HierarchyPropertyBindings::SetSearchFilterImpl", HasExplicitThis = true)]
        extern void SetSearchFilterImpl(string[] nameFilters, string[] classNames, string[] assetLabels, string[] assetBundleNames, string[] versionControlStates, string[] softLockControlStates, EntityId[] referencingEntityIds, SceneHandle[] sceneHandles, string[] regex, int[] productIds, bool anyWithAssetOrigin, bool showAllHits, ImportLogFlags importLogFlags, bool filterByTypeIntersection, bool excludeSceneAssets);

        [FreeFunction("HierarchyPropertyBindings::FilterSingleSceneObject")]
        static extern void FilterSingleSceneObject(EntityId instanceID, bool otherVisibilityState);

        public static void FilterSingleSceneObject(int instanceID, bool otherVisibilityState) => FilterSingleSceneObject((EntityId)instanceID, otherVisibilityState);

        public int[] FindAllAncestors(int[] instanceIDs) => throw new System.NotImplementedException("HierarchyProperty is deprecated. Use HierarchyIterator instead.");
        [FreeFunction("HierarchyPropertyBindings::ClearSceneObjectsFilter")]
        public static extern void ClearSceneObjectsFilter();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyProperty prop) => prop.m_Ptr;
        }
    }

    internal static class HierarchyPropertyCustomMarshaller
    {
        [Obsolete("HierarchyProperty is deprecated. This attribute is needed to avoid errors.")]
        public static IntPtr ConvertToUnmanaged(HierarchyProperty prop)
                => prop != null ? prop.m_Ptr : IntPtr.Zero;
    }
}
