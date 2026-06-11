// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // SelectionMode can be used to tweak the selection returned by Selection.GetTransforms.
    [Flags]
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
        // Renamed to Editable
        [Obsolete("'OnlyUserModifiable' is obsolete. Use 'Editable' instead. (UnityUpgradable) -> Editable", true)]
        OnlyUserModifiable = 8
    }

    [NativeHeader("Editor/Src/Gizmos/GizmoUtil.h")]
    [NativeHeader("Editor/Src/Selection/Selection.bindings.h")]
    [NativeHeader("Editor/Src/Selection/Selection.h")]
    [NativeHeader("Editor/Src/Selection/SceneInspector.h")]
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

        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern internal static void SetSelectionWithActiveObject([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] newSelection, Object activeObject);

        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern internal static void SetSelectionWithActiveEntityId([NotNull] EntityId[] newSelection, EntityId activeObject);

        [Obsolete("Use SetSelectionWithActiveEntityId instead.", true)]
        internal static void SetSelectionWithActiveInstanceID(int[] newSelection, int activeObject)
        {
            throw new InvalidOperationException("Use SetSelectionWithActiveEntityId.");
        }

        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        internal static extern void SetFullSelection([UnityMarshalAs(NativeType.ScriptingObjectPtr)] Object[] newSelection, Object activeObject, Object context, DataMode dataModeHint, bool notifyOnReselection);
        internal static void SetFullSelection(Object[] newSelection, Object activeObject, Object context, DataMode dataModeHint)
            => SetFullSelection(newSelection, activeObject, context, dataModeHint, false);

        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        internal static extern void SetFullSelectionByID(ReadOnlySpan<EntityId> newSelection, EntityId activeObjectEntityId, EntityId contextEntityId, DataMode dataModeHint, bool notifyOnReselection);
        internal static void SetFullSelectionByID(EntityId[] newSelection, EntityId activeObjectEntityId, EntityId contextEntityId, DataMode dataModeHint)
            => SetFullSelectionByID(newSelection, activeObjectEntityId, contextEntityId, dataModeHint, false);

        [Obsolete("Use EntityId version of SetFullSelectionByID instead.", true)]
        internal static void SetFullSelectionByID(int[] newSelection, int activeObjectInstanceID, int contextInstanceID, DataMode dataModeHint)
        {
            throw new InvalidOperationException("Use EntityId version of SetFullSelectionByID.");
        }

        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        internal static extern void UpdateSelectionMetaData(Object context, DataMode dataModeHint);

        // Returns the active context object
        extern public static Object activeContext
        {
            [NativeMethod("GetActiveContext", true)]
            get;
        }

        internal extern static DataMode dataModeHint
        {
            [NativeMethod("GetDataModeHint", true)]
            get;
        }

        // Returns the EntityId of the actual object selection. Includes prefabs, non-modifiable objects.
        [StaticAccessor("Selection", StaticAccessorType.DoubleColon)]
        [NativeName("ActiveID")]
        extern public static EntityId activeEntityId { get; set; }

        [Obsolete("Use activeEntityId instead.", true)]
        public static int activeInstanceID
        {
            get
            {
                return activeEntityId;
            }
            set
            {
                activeEntityId = value;
            }
        }

        // Returns the active context object's EntityId
        [StaticAccessor("Selection", StaticAccessorType.DoubleColon)]
        [NativeName("ActiveContextID")]
        extern internal static EntityId activeContextEntityId { get; }

        [Obsolete("Use activeEntityId instead.", true)]
        internal static int activeContextInstanceID { get => activeContextEntityId; }

        // The actual unfiltered selection from the Scene.
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern public static Object[] objects { [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]  get; [param:UnityMarshalAs(NativeType.ScriptingObjectPtr)] set; }

        // The actual unfiltered selection from the Scene returned as instance ids instead of ::ref::objects.
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        public static EntityId[] entityIds { get => GetEntityIds(); set => SetEntityIds(value); }

        [Obsolete("Use entityIds instead.", true)]
        public static int[] instanceIDs { get => throw new NotImplementedException("Use entityIds instead."); set => SetInstanceIDs(value); }

        /// <summary>
        /// Returns the current selection as a <see cref="ReadOnlySpan{EntityId}"/>.
        /// </summary>
        /// <remarks>
        /// This is unsafe, the values are not copied in the returned <see cref="ReadOnlySpan{EntityId}"/>.
        /// Changing the selection while keeping a reference to the returned span will leave the span pointing to unknown memory.
        /// </remarks>
        /// <returns>Current selection as a <see cref="ReadOnlySpan{EntityId}"/></returns>
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        [NativeName("GetEntityIds")]
        internal extern static ReadOnlySpan<EntityId> GetEntityIdsUnsafe();

        /// <summary>
        /// Sets the current selection to the provided <see cref="ReadOnlySpan{EntityId}"/>.
        /// </summary>
        /// <param name="entityIds">The entity Ids.</param>
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        [NativeName("SetEntityIds")]
        internal extern static void SetEntityIdsUnsafe(ReadOnlySpan<EntityId> entityIds);

        [Obsolete("Use SetEntityIds() instead.", true)]
        static void SetInstanceIDs(int[] instanceIDs)
        {
            throw new InvalidOperationException("SetInstanceIDs obsolete, use SetEntityIds instead.");
        }

        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern static EntityId[] GetEntityIds();
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        extern static void SetEntityIds([NotNull] EntityId[] entityIds);

        [StaticAccessor("GetSceneTracker()", StaticAccessorType.Dot)]
        [NativeMethod("IsSelected")]
        extern public static bool Contains(EntityId entityId);

        [Obsolete("Use Contains() with EntityId instead.", true)]
        public static bool Contains(int instanceID)
        {
            return Contains((EntityId)instanceID);
        }

        [NativeMethod("SetActiveObjectWithContextInternal", true)]
        extern public static void SetActiveObjectWithContext(Object obj, Object context);

        // Allows for fine grained control of the selection type using the [[SelectionMode]] bitmask.
        [NativeMethod("GetTransformSelection", true)]
        extern public static Transform[] GetTransforms(SelectionMode mode);

        //* undocumented - utility function
        [StaticAccessor("SelectionBindings", StaticAccessorType.DoubleColon)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
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

        [StaticAccessor("Selection", StaticAccessorType.DoubleColon)]
        [NativeName("SelectionCount")]
        public extern static int count { get; }

        [NativeMethod("DoAllGOsHaveConstrainProportionsEnabled", true)]
        internal static extern bool DoAllGOsHaveConstrainProportionsEnabled([NotNull] Object[] targetObjects);
    }
}
