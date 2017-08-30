// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityScript.Scripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using UnityEditor.Collaboration;
using UnityEditor.Connect;
using UnityEngine.Video;

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
    Packages = 3
}

public enum IconDrawStyle
{
    NonTexture         = 0,
    Texture            = 1,
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
    }


public sealed partial class HierarchyProperty : IHierarchyProperty
{
            #pragma warning disable 169
    IntPtr m_Property;
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public HierarchyProperty (HierarchyType hierarchyType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Reset () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Dispose () ;

    ~HierarchyProperty() { Dispose(); }
    
    
    public extern  int instanceID
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  Object pptrValue
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  string name
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Scene GetScene () {
        Scene result;
        INTERNAL_CALL_GetScene ( this, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetScene (HierarchyProperty self, out Scene value);
    public extern  bool hasChildren
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int depth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int[] ancestors
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int row
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int colorCode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool IsExpanded (int[] expanded) ;

    public extern  string guid
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool alphaSorted
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  bool isValid
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isMainRepresentation
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool hasFullPreviewImage
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  IconDrawStyle iconDrawStyle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  bool isFolder
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Next (int[] expanded) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool NextWithDepthCheck (int[] expanded, int minDepth) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Previous (int[] expanded) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Parent () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Find (int instanceID, int[] expanded) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool Skip (int count, int[] expanded) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int CountRemaining (int[] expanded) ;

    public extern  Texture2D icon
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public void SetSearchFilter(string searchString, int mode)
        {
            SearchFilter filter = SearchableEditorWindow.CreateFilter(searchString, (SearchableEditorWindow.SearchMode)mode);
            SetSearchFilter(filter);
        }
    
    
    internal void SetSearchFilter(SearchFilter filter)
        {
            SetSearchFilterINTERNAL(SearchFilter.Split(filter.nameFilter), filter.classNames, filter.assetLabels, filter.assetBundleNames, filter.versionControlStates, filter.softLockControlStates, filter.referencingInstanceIDs, filter.scenePaths, filter.showAllHits);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SetSearchFilterINTERNAL (string[] nameFilters, string[] classNames, string[] assetLabels, string[] assetBundleNames, string[] versionControlStates, string[] softLockControlStates, int[] referencingInstanceIDs, string[] scenePaths, bool showAllHits) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int[] FindAllAncestors (int[] instanceIDs) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearSceneObjectsFilter () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void FilterSingleSceneObject (int instanceID, bool otherVisibilityState) ;

}

[Flags]
public enum AssetMoveResult
{
    
    DidNotMove = 0,
    
    FailedMove = 1,
    
    DidMove = 2
}

[Flags]
public enum AssetDeleteResult
{
    
    DidNotDelete = 0,
    
    FailedDelete = 1,
    
    DidDelete = 2
}

[System.Obsolete ("Use UnityEditor.AssetModificationProcessor")]
public partial class SaveAssetsProcessor : UnityEditor.AssetModificationProcessor
{
}

internal sealed partial class InternalMeshUtil
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetPrimitiveCount (Mesh mesh) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int CalcTriangleCount (Mesh mesh) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasNormals (Mesh mesh) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetVertexFormat (Mesh mesh) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetCachedMeshSurfaceArea (MeshRenderer meshRenderer) ;

}




internal sealed partial class VideoUtil
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  GUID StartPreview (VideoClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StopPreview (GUID id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void PlayPreview (GUID id, bool loop) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void PausePreview (GUID id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsPreviewPlaying (GUID id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture GetPreviewTexture (GUID id) ;

}




public sealed partial class MeshUtility
{
    public static void SetPerTriangleUV2(Mesh src, Vector2[] triUV)
        {
            int triCount = InternalMeshUtil.CalcTriangleCount(src);
            int uvCount  = triUV.Length;

            if (uvCount != 3 * triCount)
            {
                Debug.LogError("mesh contains " + triCount + " triangles but " + uvCount + " uvs are provided");
                return;
            }

            SetPerTriangleUV2NoCheck(src, triUV);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetPerTriangleUV2NoCheck (Mesh src, Vector2[] triUV) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Vector2[] ComputeTextureBoundingHull (Texture texture, int vertexCount) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetMeshCompression (Mesh mesh, ModelImporterMeshCompression compression) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  ModelImporterMeshCompression GetMeshCompression (Mesh mesh) ;

}

public sealed partial class ArrayUtility
{
    
    
    public static void Add<T>(ref T[] array, T item)
        {
            System.Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = item;
        }
    
    public static bool ArrayEquals<T>(T[] lhs, T[] rhs)
        {
            if (lhs == null || rhs == null)
                return lhs == rhs;

            if (lhs.Length != rhs.Length)
                return false;

            for (int i = 0; i < lhs.Length; i++)
            {
                if (!lhs[i].Equals(rhs[i]))
                    return false;

            }
            return true;
        }
    
    public static bool ArrayReferenceEquals<T>(T[] lhs, T[] rhs)
        {
            if (lhs == null || rhs == null)
                return lhs == rhs;

            if (lhs.Length != rhs.Length)
                return false;

            for (int i = 0; i < lhs.Length; i++)
            {
                if (!object.ReferenceEquals(lhs[i], rhs[i]))
                    return false;

            }
            return true;
        }
    
    public static void AddRange<T>(ref T[] array, T[] items)
        {
            int size = array.Length;
            System.Array.Resize(ref array, array.Length + items.Length);
            for (int i = 0; i < items.Length; i++)
                array[size + i] = items[i];
        }
    
    public static void Insert<T>(ref T[] array, int index, T item)
        {
            ArrayList a = new ArrayList();
            a.AddRange(array);
            a.Insert(index, item);
            array = a.ToArray(typeof(T)) as T[];
        }
    
    public static void Remove<T>(ref T[] array, T item)
        {
            List<T> newList = new List<T>(array);
            newList.Remove(item);
            array = newList.ToArray();
        }
    
    public static List<T> FindAll<T>(T[] array, Predicate<T> match)
        {
            List<T> list = new List<T>(array);
            return list.FindAll(match);
        }
    
    public static T Find<T>(T[] array, Predicate<T> match)
        {
            List<T> list = new List<T>(array);
            return list.Find(match);
        }
    
    public static int FindIndex<T>(T[] array, Predicate<T> match)
        {
            List<T> list = new List<T>(array);
            return list.FindIndex(match);
        }
    
    public static int IndexOf<T>(T[] array, T value)
        {
            List<T> list = new List<T>(array);
            return list.IndexOf(value);
        }
    
    public static int LastIndexOf<T>(T[] array, T value)
        {
            List<T> list = new List<T>(array);
            return list.LastIndexOf(value);
        }
    
    public static void RemoveAt<T>(ref T[] array, int index)
        {
            List<T> list = new List<T>(array);
            list.RemoveAt(index);
            array = list.ToArray();
        }
    
    public static bool Contains<T>(T[] array, T item)
        {
            List<T> list = new List<T>(array);
            return list.Contains(item);
        }
    
    public static void Clear<T>(ref T[] array)
        {
            System.Array.Clear(array, 0, array.Length);
            System.Array.Resize(ref array, 0);
        }
    
    
}

internal sealed partial class OSUtil
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetDefaultApps (string fileType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetAppFriendlyName (string app) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetDefaultAppPath (string fileType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetDefaultCachePath () ;

}

internal sealed partial class TerrainInspectorUtil
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetTreePlacementSize (TerrainData terrainData, int prototypeIndex, float spacing, float treeCount) ;

    public static bool CheckTreeDistance (TerrainData terrainData, Vector3 position, int prototypeIndex, float distanceBias) {
        return INTERNAL_CALL_CheckTreeDistance ( terrainData, ref position, prototypeIndex, distanceBias );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CheckTreeDistance (TerrainData terrainData, ref Vector3 position, int prototypeIndex, float distanceBias);
    public static Vector3 GetPrototypeExtent (TerrainData terrainData, int prototypeIndex) {
        Vector3 result;
        INTERNAL_CALL_GetPrototypeExtent ( terrainData, prototypeIndex, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPrototypeExtent (TerrainData terrainData, int prototypeIndex, out Vector3 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetPrototypeCount (TerrainData terrainData) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool PrototypeIsRenderable (TerrainData terrainData, int prototypeIndex) ;

}

internal sealed partial class PhysicsManager : ProjectSettingsBase
{
}

internal sealed partial class AudioManager : ProjectSettingsBase
{
}

internal sealed partial class Physics2DSettings : ProjectSettingsBase
{
}

internal sealed partial class MonoManager : ProjectSettingsBase
{
}

internal sealed partial class TagManager : ProjectSettingsBase
{
}

internal sealed partial class InputManager : ProjectSettingsBase
{
}

internal sealed partial class TimeManager : ProjectSettingsBase
{
}

[System.Obsolete("DDSImporter is obsolete. Use IHVImageFormatImporter instead (UnityUpgradable) -> IHVImageFormatImporter", true)]
public class DDSImporter : AssetImporter
    {
        public bool isReadable { get {return false; } set {} }
    }


public sealed partial class IHVImageFormatImporter : AssetImporter
{
    public extern bool isReadable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern FilterMode filterMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeU
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeV
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextureWrapMode wrapModeW
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct UnwrapParam
{
    public   float angleError;
    public   float areaError;
    public   float hardAngle;
    public   float packMargin;
    
    
    internal int   recollectVertices;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetDefaults (out UnwrapParam param) ;

}

public sealed partial class Unwrapping
{
    public static Vector2[] GeneratePerTriangleUV(Mesh src)
        {
            UnwrapParam settings = new UnwrapParam();
            UnwrapParam.SetDefaults(out settings);

            return GeneratePerTriangleUV(src, settings);
        }
    
    
    public static Vector2[] GeneratePerTriangleUV(Mesh src, UnwrapParam settings)
        {
            return GeneratePerTriangleUVImpl(src, settings);
        }
    
    
    internal static Vector2[] GeneratePerTriangleUVImpl (Mesh src, UnwrapParam settings) {
        return INTERNAL_CALL_GeneratePerTriangleUVImpl ( src, ref settings );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Vector2[] INTERNAL_CALL_GeneratePerTriangleUVImpl (Mesh src, ref UnwrapParam settings);
    public static void GenerateSecondaryUVSet(Mesh src)
        {
            MeshUtility.SetPerTriangleUV2(src, GeneratePerTriangleUV(src));
        }
    
    
    public static void GenerateSecondaryUVSet(Mesh src, UnwrapParam settings)
        {
            MeshUtility.SetPerTriangleUV2(src, GeneratePerTriangleUV(src, settings));
        }
    
    
}

public sealed partial class StaticOcclusionCulling
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool Compute () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GenerateInBackground () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void InvalidatePrevisualisationData () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Cancel () ;

    public extern static bool isRunning
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Clear () ;

    public extern static float smallestOccluder
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float smallestHole
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static float backfaceThreshold
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool doesSceneHaveManualPortals
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static int umbraDataSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetDefaultOcclusionBakeSettings () ;

}

public sealed partial class StaticOcclusionCullingVisualization
{
    public extern static bool showOcclusionCulling
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showPreVisualization
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showViewVolumes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showDynamicObjectBounds
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showPortals
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showVisibilityLines
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool showGeometryCulling
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool isPreviewOcclusionCullingCameraInPVS
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static Camera previewOcclusionCamera
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static Camera previewOcclucionCamera
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}


}

namespace UnityEditorInternal
{


internal sealed partial class AnimationCurvePreviewCache
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearCache () ;

    public static Texture2D GetPropertyPreview (int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, SerializedProperty property, Color color) {
        return INTERNAL_CALL_GetPropertyPreview ( previewWidth, previewHeight, useCurveRanges, ref curveRanges, property, ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetPropertyPreview (int previewWidth, int previewHeight, bool useCurveRanges, ref Rect curveRanges, SerializedProperty property, ref Color color);
    public static Texture2D GetPropertyPreviewFilled (int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, SerializedProperty property, Color color, Color topFillColor, Color bottomFillColor) {
        return INTERNAL_CALL_GetPropertyPreviewFilled ( previewWidth, previewHeight, useCurveRanges, ref curveRanges, property, ref color, ref topFillColor, ref bottomFillColor );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetPropertyPreviewFilled (int previewWidth, int previewHeight, bool useCurveRanges, ref Rect curveRanges, SerializedProperty property, ref Color color, ref Color topFillColor, ref Color bottomFillColor);
    public static Texture2D GetPropertyPreviewRegion (int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, SerializedProperty property, SerializedProperty property2, Color color) {
        return INTERNAL_CALL_GetPropertyPreviewRegion ( previewWidth, previewHeight, useCurveRanges, ref curveRanges, property, property2, ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetPropertyPreviewRegion (int previewWidth, int previewHeight, bool useCurveRanges, ref Rect curveRanges, SerializedProperty property, SerializedProperty property2, ref Color color);
    public static Texture2D GetPropertyPreviewRegionFilled (int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, SerializedProperty property, SerializedProperty property2, Color color, Color topFillColor, Color bottomFillColor) {
        return INTERNAL_CALL_GetPropertyPreviewRegionFilled ( previewWidth, previewHeight, useCurveRanges, ref curveRanges, property, property2, ref color, ref topFillColor, ref bottomFillColor );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetPropertyPreviewRegionFilled (int previewWidth, int previewHeight, bool useCurveRanges, ref Rect curveRanges, SerializedProperty property, SerializedProperty property2, ref Color color, ref Color topFillColor, ref Color bottomFillColor);
    public static Texture2D GetCurvePreview (int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, AnimationCurve curve, Color color) {
        return INTERNAL_CALL_GetCurvePreview ( previewWidth, previewHeight, useCurveRanges, ref curveRanges, curve, ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetCurvePreview (int previewWidth, int previewHeight, bool useCurveRanges, ref Rect curveRanges, AnimationCurve curve, ref Color color);
    public static Texture2D GetCurvePreviewFilled (int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, AnimationCurve curve, Color color, Color topFillColor, Color bottomFillColor) {
        return INTERNAL_CALL_GetCurvePreviewFilled ( previewWidth, previewHeight, useCurveRanges, ref curveRanges, curve, ref color, ref topFillColor, ref bottomFillColor );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetCurvePreviewFilled (int previewWidth, int previewHeight, bool useCurveRanges, ref Rect curveRanges, AnimationCurve curve, ref Color color, ref Color topFillColor, ref Color bottomFillColor);
    public static Texture2D GetCurvePreviewRegion (int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, AnimationCurve curve, AnimationCurve curve2, Color color) {
        return INTERNAL_CALL_GetCurvePreviewRegion ( previewWidth, previewHeight, useCurveRanges, ref curveRanges, curve, curve2, ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetCurvePreviewRegion (int previewWidth, int previewHeight, bool useCurveRanges, ref Rect curveRanges, AnimationCurve curve, AnimationCurve curve2, ref Color color);
    public static Texture2D GetCurvePreviewRegionFilled (int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, AnimationCurve curve, AnimationCurve curve2, Color color, Color topFillColor, Color bottomFillColor) {
        return INTERNAL_CALL_GetCurvePreviewRegionFilled ( previewWidth, previewHeight, useCurveRanges, ref curveRanges, curve, curve2, ref color, ref topFillColor, ref bottomFillColor );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Texture2D INTERNAL_CALL_GetCurvePreviewRegionFilled (int previewWidth, int previewHeight, bool useCurveRanges, ref Rect curveRanges, AnimationCurve curve, AnimationCurve curve2, ref Color color, ref Color topFillColor, ref Color bottomFillColor);
    public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, SerializedProperty property2, Color color, Rect curveRanges)
        {
            return GetPreview(previewWidth, previewHeight, property, property2, color, Color.clear, Color.clear);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, SerializedProperty property2, Color color, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            if (property2 == null)
                return GetPropertyPreviewFilled(previewWidth, previewHeight, true, curveRanges, property, color, topFillColor, bottomFillColor);
            else
                return GetPropertyPreviewRegionFilled(previewWidth, previewHeight, true, curveRanges, property, property2, color, topFillColor, bottomFillColor);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, SerializedProperty property2, Color color)
        {
            return GetPreview(previewWidth, previewHeight, property, property2, color, Color.clear, Color.clear);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, SerializedProperty property2, Color color, Color topFillColor, Color bottomFillColor)
        {
            if (property2 == null)
                return GetPropertyPreviewFilled(previewWidth, previewHeight, false, new Rect(), property, color, topFillColor, bottomFillColor);
            else
                return GetPropertyPreviewRegionFilled(previewWidth, previewHeight, false, new Rect(), property, property2, color, topFillColor, bottomFillColor);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, AnimationCurve curve2, Color color, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            return GetCurvePreviewRegionFilled(previewWidth, previewHeight, true, curveRanges, curve, curve2, color, topFillColor, bottomFillColor);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, AnimationCurve curve2, Color color, Rect curveRanges)
        {
            return GetPreview(previewWidth, previewHeight, curve, curve2, color, Color.clear, Color.clear, curveRanges);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, AnimationCurve curve2, Color color, Color topFillColor, Color bottomFillColor)
        {
            return GetCurvePreviewRegionFilled(previewWidth, previewHeight, false, new Rect(), curve, curve2, color, topFillColor, bottomFillColor);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, AnimationCurve curve2, Color color)
        {
            return GetPreview(previewWidth, previewHeight, curve, curve2, color, Color.clear, Color.clear, new Rect());
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, Color color, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            return GetPropertyPreviewFilled(previewWidth, previewHeight, true, curveRanges, property, color, topFillColor, bottomFillColor);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, Color color, Rect curveRanges)
        {
            return GetPreview(previewWidth, previewHeight, property, color, Color.clear, Color.clear, curveRanges);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, Color color, Color topFillColor, Color bottomFillColor)
        {
            return GetPropertyPreviewFilled(previewWidth, previewHeight, false, new Rect(), property, color, topFillColor, bottomFillColor);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, Color color)
        {
            return GetPreview(previewWidth, previewHeight, property, color, Color.clear, Color.clear, new Rect());
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, Color color, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            return GetCurvePreviewFilled(previewWidth, previewHeight, true, curveRanges, curve, color, topFillColor, bottomFillColor);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, Color color, Rect curveRanges)
        {
            return GetPreview(previewWidth, previewHeight, curve, color, Color.clear, Color.clear, curveRanges);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, Color color, Color topFillColor, Color bottomFillColor)
        {
            return GetCurvePreviewFilled(previewWidth, previewHeight, false, new Rect(), curve, color, topFillColor, bottomFillColor);
        }
    
    
    public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, Color color)
        {
            return GetPreview(previewWidth, previewHeight, curve, color, Color.clear, Color.clear, new Rect());
        }
    
    
}

internal sealed partial class GradientPreviewCache
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearCache () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture2D GetPropertyPreview (SerializedProperty property) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture2D GetGradientPreview (Gradient curve) ;

}

internal partial class ProjectSettingsBase : Object
{
}

}
