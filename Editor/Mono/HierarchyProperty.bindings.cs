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
using UnityEngine.Bindings;
using UnityEngine.Scripting;

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

    interface IHierarchyProperty
    {
        void Reset();
        int instanceID { get; }
        Object pptrValue { get; }
        string name { get; }
        bool hasChildren { get; }
        int depth { get; }
        int row { get; }
        int colorCode { get; }
        string guid { get; }
        Texture2D icon { get; }
        bool isValid { get; }
        bool isMainRepresentation { get; }
        bool hasFullPreviewImage { get; }
        IconDrawStyle iconDrawStyle { get; }
        bool isFolder { get; }
        GUID[] dynamicDependencies { get; }

        bool IsExpanded(int[] expanded);
        bool Next(int[] expanded);
        bool NextWithDepthCheck(int[] expanded, int minDepth);
        bool Previous(int[] expanded);
        bool Parent();

        int[] ancestors { get; }

        bool Find(int instanceID, int[] expanded);
        int[] FindAllAncestors(int[] instanceIDs);

        bool Skip(int count, int[] expanded);
        int CountRemaining(int[] expanded);
        int GetInstanceIDIfImported();
    }

    [NativeHeader("Editor/Src/Utility/HierarchyProperty.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyProperty : IHierarchyProperty
    {
    #pragma warning disable 169
        IntPtr m_Ptr;
    #pragma warning restore 169

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
            SetCustomScenes(scenes.Select(s => s.handle).ToArray());
        }

        [FreeFunction("HierarchyPropertyBindings::SetCustomScenes", HasExplicitThis = true)]
        public extern void SetCustomScenes([NotNull] int[] sceneHandles);
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

        public extern int instanceID { get; }
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
        public extern bool IsExpanded(int[] expanded);

        public extern string guid { [FreeFunction("HierarchyPropertyBindings::GetGuid", HasExplicitThis = true)] get; }
        public extern bool alphaSorted { [NativeName("IsAlphaSorted")] get; set; }
        public extern bool showSceneHeaders { [NativeName("IsShowingSceneHeaders")] get; [NativeName("SetShowingSceneHeaders")] set; }
        public extern bool isValid { [NativeName("IsValid")] get; }
        public extern bool isMainRepresentation { [NativeName("IsMainAssetRepresentation")] get; }
        public extern bool hasFullPreviewImage { [NativeName("HasFullPreviewImage")] get; }
        public extern IconDrawStyle iconDrawStyle { get; }
        public extern bool isFolder { [NativeName("IsFolder")] get; }
        public extern GUID[] dynamicDependencies { get; }

        [FreeFunction("HierarchyPropertyBindings::Next", HasExplicitThis = true)]
        public extern bool Next(int[] expanded);
        [FreeFunction("HierarchyPropertyBindings::NextWithDepthCheck", HasExplicitThis = true)]
        public extern bool NextWithDepthCheck(int[] expanded, int minDepth);
        [FreeFunction("HierarchyPropertyBindings::Previous", HasExplicitThis = true)]
        public extern bool Previous(int[] expanded);
        public extern bool Parent();
        [FreeFunction("HierarchyPropertyBindings::Find", HasExplicitThis = true)]
        public extern bool Find(int instanceID, int[] expanded);
        [FreeFunction("HierarchyPropertyBindings::Skip", HasExplicitThis = true)]
        public extern bool Skip(int count, int[] expanded);
        [FreeFunction("HierarchyPropertyBindings::CountRemaining", HasExplicitThis = true)]
        public extern int CountRemaining(int[] expanded);

        public extern int GetInstanceIDIfImported();

        public extern Texture2D icon { [NativeName("GetCachedIcon")] get; }

        [RequiredByNativeCode]
        static void SetFilter(HierarchyProperty hierarchy, string filter)
        {
            SearchFilter search = new SearchFilter();
            SearchUtility.ParseSearchString(filter, search);
            hierarchy.SetSearchFilter(search);
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
            SetSearchFilterImpl(SearchFilter.Split(filter.nameFilter), filter.classNames, filter.assetLabels, filter.assetBundleNames, filter.versionControlStates, filter.softLockControlStates, filter.referencingInstanceIDs, filter.sceneHandles, filter.GlobToRegex().ToArray(), filter.showAllHits);
        }

        [FreeFunction("HierarchyPropertyBindings::SetSearchFilterImpl", HasExplicitThis = true)]
        extern void SetSearchFilterImpl(string[] nameFilters, string[] classNames, string[] assetLabels, string[] assetBundleNames, string[] versionControlStates, string[] softLockControlStates, int[] referencingInstanceIDs, int[] sceneHandles, string[] regex, bool showAllHits);
        [FreeFunction("HierarchyPropertyBindings::FindAllAncestors", HasExplicitThis = true)]
        public extern int[] FindAllAncestors(int[] instanceIDs);
        [FreeFunction("HierarchyPropertyBindings::ClearSceneObjectsFilter")]
        public static extern void ClearSceneObjectsFilter();
        [FreeFunction("HierarchyPropertyBindings::FilterSingleSceneObject")]
        public static extern void FilterSingleSceneObject(int instanceID, bool otherVisibilityState);
        [FreeFunction("HierarchyPropertyBindings::SetFilteredVisibility", HasExplicitThis = true)]
        internal extern void SetFilteredVisibility(bool visible);
    }
}
