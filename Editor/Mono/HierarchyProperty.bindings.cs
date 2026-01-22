// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
    [Obsolete("HierarchyProperty is deprecated. Use HierarchyIterator instead.")]
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            SetCustomScenes(scenes.Select(s => s.handle).ToArray());
#pragma warning restore RS0030
        }

        [FreeFunction("HierarchyPropertyBindings::SetCustomScenes", HasExplicitThis = true)]
        public extern void SetCustomScenes([NotNull] SceneHandle[] sceneHandles);

        [Obsolete("SetCustomScenes(int[]) is deprecated. Use SetCustomScenes(SceneHandle[]) instead.")]
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
        public extern int[] ancestors { get; }
        public extern int row { get; }
        public extern int colorCode { get; }

        [FreeFunction("HierarchyPropertyBindings::IsExpanded", HasExplicitThis = true)]
        private extern bool IsExpanded_internal(int[] expanded, bool nonNullEmptyArray);

        public bool IsExpanded(int[] expanded) => IsExpanded_internal(expanded, expanded != null && expanded.Length == 0);

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
        private extern bool Next_internal(int[] expanded, bool nonNullEmptyArray);
        public bool Next(int[] expanded) => Next_internal(expanded, expanded != null && expanded.Length == 0);
        [FreeFunction("HierarchyPropertyBindings::NextWithDepthCheck", HasExplicitThis = true)]
        private extern bool NextWithDepthCheck_internal(int[] expanded, int minDepth, bool nonNullEmptyArray);
        public bool NextWithDepthCheck(int[] expanded, int minDepth) => NextWithDepthCheck_internal(expanded, minDepth, expanded != null && expanded.Length == 0);
        [FreeFunction("HierarchyPropertyBindings::Previous", HasExplicitThis = true)]
        private extern bool Previous_internal(int[] expanded, bool nonNullEmptyArray);
        public bool Previous(int[] expanded) => Previous_internal(expanded, expanded != null && expanded.Length == 0);
        public extern bool Parent();
        [FreeFunction("HierarchyPropertyBindings::Find", HasExplicitThis = true)]
        private extern bool Find_internal(EntityId instanceID, int[] expanded, bool nonNullEmptyArray);
        public bool Find(int instanceID, int[] expanded) => Find_internal((EntityId)instanceID, expanded, expanded != null && expanded.Length == 0);
        [FreeFunction("HierarchyPropertyBindings::Skip", HasExplicitThis = true)]
        private extern bool Skip_internal(int count, int[] expanded, bool nonNullEmptyArray);
        public bool Skip(int count, int[] expanded) =>Skip_internal(count, expanded, expanded != null && expanded.Length == 0);
        [FreeFunction("HierarchyPropertyBindings::CountRemaining", HasExplicitThis = true)]
        private extern int CountRemaining_internal(int[] expanded, bool nonNullEmptyArray);
        public int CountRemaining(int[] expanded) => CountRemaining_internal(expanded, expanded != null && expanded.Length == 0);

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
            SetSearchFilterImpl(SearchFilter.Split(filter.nameFilter), filter.classNames, filter.assetLabels, filter.assetBundleNames, Array.Empty<string>(), Array.Empty<string>(), filter.referencingEntityIds, filter.sceneHandles, filter.GlobToRegex(), filter.productIds, filter.anyWithAssetOrigin, filter.showAllHits, filter.importLogFlags, filter.filterByTypeIntersection);
        }

        [FreeFunction("HierarchyPropertyBindings::SetSearchFilterImpl", HasExplicitThis = true)]
        extern void SetSearchFilterImpl(string[] nameFilters, string[] classNames, string[] assetLabels, string[] assetBundleNames, string[] versionControlStates, string[] softLockControlStates, EntityId[] referencingEntityIds, SceneHandle[] sceneHandles, string[] regex, int[] productIds, bool anyWithAssetOrigin, bool showAllHits, ImportLogFlags importLogFlags, bool filterByTypeIntersection);

        [FreeFunction("HierarchyPropertyBindings::FilterSingleSceneObject")]
        static extern void FilterSingleSceneObject(EntityId instanceID, bool otherVisibilityState);

        public static void FilterSingleSceneObject(int instanceID, bool otherVisibilityState) => FilterSingleSceneObject((EntityId)instanceID, otherVisibilityState);

        [FreeFunction("HierarchyPropertyBindings::FindAllAncestors", HasExplicitThis = true)]
        public extern int[] FindAllAncestors(int[] instanceIDs);
        [FreeFunction("HierarchyPropertyBindings::ClearSceneObjectsFilter")]
        public static extern void ClearSceneObjectsFilter();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyProperty prop) => prop.m_Ptr;
        }
    }
}
