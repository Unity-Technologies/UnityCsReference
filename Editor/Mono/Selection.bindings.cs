// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // SelectionMode can be used to tweak the selection returned by Selection.GetTransforms.
    public enum SelectionMode
    {
        // Return the whole selection.
        Unfiltered = 0,
        // Only return the topmost selected transform. A selected child of another selected transform will be filtered out.
        TopLevel = 1,
        // Return the selection and all child transforms of the selection.
        Deep = 2,
        // Excludes any prefabs from the selection.
        ExcludePrefab = 4,
        // Excludes any objects which shall not be modified.
        Editable = 8,
        // Only return objects that are assets in the Asset directory.
        Assets = 16,
        // If the selection contains folders, also include all assets and subfolders within that folder in the file hierarchy.
        DeepAssets = 32,
        // Return a selection that only contains top level selection of all visible assets
        //TopLevelAssets = 64,
        // renamed to Editable
        OnlyUserModifiable = 8
    }

    [NativeHeader("Editor/Src/Selection.bindings.h")]
    [NativeHeader("Editor/Src/Gizmos/GizmoUtil.h")]
    [NativeHeader("Editor/Src/Selection.h")]
    [NativeHeader("Editor/Src/SceneInspector.h")]
    public sealed partial class Selection
    {
        // Returns the top level selection, excluding prefabs.
        public extern static Transform[] transforms
        {
            [NativeMethod("GetTransformSelection", true)]
            get;
        }

        // Returns the actual game object selection. Includes prefabs, non-modifyable objects.
        public extern static Transform activeTransform
        {
            [NativeMethod("GetActiveTransform", true)]
            get;
            [NativeMethod("SetActiveObject", true)]
            set;
        }

        // Returns the actual game object selection. Includes prefabs, non-modifyable objects.
        public extern static GameObject[] gameObjects
        {
            [NativeMethod("GetGameObjectSelection", true)]
            get;
        }

        // Returns the active game object. (The one shown in the inspector)
        public extern static GameObject activeGameObject
        {
            [NativeMethod("GetActiveGO", true)]
            get;
            [NativeMethod("SetActiveObject", true)]
            set;
        }

        // Returns the actual object selection. Includes prefabs, non-modifyable objects.
        extern public static Object activeObject
        {
            [NativeMethod("GetActiveObject", true)]
            get;
            [NativeMethod("SetActiveObject", true)]
            set;
        }

        // Returns the active context object
        extern public static Object activeContext
        {
            [NativeMethod("GetActiveContext", true)]
            get;
        }

        // Returns the instanceID of the actual object selection. Includes prefabs, non-modifyable objects.
        [StaticAccessor("Selection", StaticAccessorType.DoubleColon)]
        [NativeName("ActiveID")]
        extern public static int activeInstanceID { get; set; }

        // The actual unfiltered selection from the Scene.
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern public static Object[] objects { get; set; }

        // The actual unfiltered selection from the Scene returned as instance ids instead of ::ref::objects.
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern public static int[] instanceIDs { get; set; }

        [StaticAccessor("GetSceneTracker()", StaticAccessorType.Dot)]
        [NativeMethod("IsSelected")]
        extern public static bool Contains(int instanceID);

        [NativeMethod("SetActiveObjectWithContextInternal", true)]
        extern public static void SetActiveObjectWithContext(Object obj, Object context);

        // Allows for fine grained control of the selection type using the [[SelectionMode]] bitmask.
        [NativeMethod("GetTransformSelection", true)]
        extern public static Transform[] GetTransforms(SelectionMode mode);

        //* undocumented - utility function
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern internal static Object[] GetObjectsMode(SelectionMode mode);

        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern internal static string[] assetGUIDsDeepSelection
        {
            [NativeMethod("GetSelectedAssetGUIDStringsDeep")]
            get;
        }

        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern public static string[] assetGUIDs
        {
            [NativeMethod("GetSelectedAssetGUIDStrings")]
            get;
        }
    }
}
