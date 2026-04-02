// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using CellValueEditorGOBool = Unity.Hierarchy.HierarchyViewCellValueEditor<UnityEngine.GameObject, UnityEngine.UIElements.Toggle, bool>;
using CellValueEditorSceneBool = Unity.Hierarchy.HierarchyViewCellValueEditor<UnityEngine.SceneManagement.Scene, UnityEngine.UIElements.Toggle, bool>;
using Object = UnityEngine.Object;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Utility class containing Column definition for Object Active state in Scene.
    /// Note that cells in this column have ClearCellContent=false. Which means the cell is not Clear after Unbind. Cell will contain a Toggle for editing.
    /// </summary>
    internal sealed class HierarchyWindowColumnActive
    {
        /// <summary>
        /// Column Id for Active property of objects in a scene.
        /// </summary>
        public const string k_ColumnId = "GameObject/Active";

        [HierarchyViewColumnDescriptor(k_ColumnId)]
        internal static void CreateColumnDesc_Active(HierarchyViewColumnDescriptor desc)
        {
            desc.Title = "Active";
            desc.Tooltip = "Enable a scene Object";
            desc.DefaultPriority = 1;
            desc.DefaultWidth = 50;

            desc.UnbindColumn += (column, view) =>
            {
                cellGOPool.Clear(view.GetHashCode());
            };
        }

        [HierarchyViewCellDescriptor(k_ColumnId, typeof(HierarchyGameObjectHandler))]
        internal static void CreateGameObjectCellDesc_Active(HierarchyViewCellDescriptor desc)
        {
            desc.ClearCellContent = false;
            desc.BindCell = cell =>
            {
                var go = HierarchyWindowColumnUtility.GetGameObject(cell);
                if (!HierarchyWindowColumnUtility.IsGameObjectEditable(go))
                {
                    HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
                    return;
                }

                HierarchyWindowColumnUtility.BindCellToValueEditor(go, cell, cellGOPool);
            };

            desc.UnbindCell = cell => HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
        }

        // internal for tests
        internal static void SetGameObjectsActive(HierarchyViewCell cell, bool value)
        {
            using var selectedGameObjects = HierarchyWindowColumnUtility.GetEditableTargetGameObjects(cell, out var length);
            var targets = selectedGameObjects.Span[..length].ToArray();
            Undo.RecordObjects(targets, value ? "Activate GameObjects" : "Deactivate GameObjects");
            foreach (var go in targets)
            {
                go.SetActive(value);
            }
        }

        static CellValueEditorGOBool CreateGOCellValueEditor_Active()
        {
            return new CellValueEditorGOBool(
                getModelValue: ed => ed.Model && ed.Model.activeSelf,
                setModelValue: (ed, value) => SetGameObjectsActive(ed.Cell, value),
                isDefaultValue: (ed, value) => value == true);
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static HierarchyViewColumnContextPool<CellValueEditorGOBool> s_CellGOPool = null;
        internal static HierarchyViewColumnContextPool<CellValueEditorGOBool> cellGOPool => s_CellGOPool ??= new(CreateGOCellValueEditor_Active);
    }

    /// <summary>
    /// Utility class containing Column definition for GameObject and Scene Visibility in Scene.
    /// Note that cells in this column have ClearCellContent=false. Which means the cell is not Clear after Unbind. Cell will contain a Toggle for editing.
    /// </summary>
    internal sealed class HierarchyWindowColumnVisibility
    {
        /// <summary>
        /// Column Id for object Visibility in Scene.
        /// </summary>
        public const string k_ColumnId = "Visibility";

        /// <summary>
        /// USS class for toggle icon used to display object Visibility in Scene.
        /// </summary>
        public static readonly UniqueStyleString k_SceneVisibility = new("toggle-scene-visibility");

        /// <summary>
        /// List of USS classes used to display object Visibility in Scene.
        /// </summary>
        public static readonly UniqueStyleString[] k_Classes = new[] { HierarchyWindowColumnUtility.k_ToggleIcon, k_SceneVisibility };

        [HierarchyViewColumnDescriptor(k_ColumnId)]
        internal static void CreateColumnDesc_Visibility(HierarchyViewColumnDescriptor desc)
        {
            desc.Icon = EditorGUIUtility.LoadIcon("scenevis_visible_hover");
            desc.Tooltip = "Change visibility of a scene object";
            desc.DefaultPriority = -2000;
            desc.DefaultWidth = 20;
            desc.DefaultVisibility = true;

            desc.BindColumn += (column, view) =>
            {
                var updateVisibility = () =>
                {
                    foreach (var editor in cellGOPool.GetActiveObjects(view.GetHashCode()))
                    {
                        editor.SyncEditorValueWithoutNotify();
                    }

                    foreach (var editor in CellScenePool.GetActiveObjects(view.GetHashCode()))
                    {
                        editor.SyncEditorValueWithoutNotify();
                    }
                };

                s_UnregisterCallbacks[view.GetHashCode()] = () => SceneVisibilityManager.visibilityChanged -= updateVisibility;
                SceneVisibilityManager.visibilityChanged += updateVisibility;
            };

            desc.UnbindColumn += (column, view) =>
            {
                if (s_UnregisterCallbacks.TryGetValue(view.GetHashCode(), out var unregister))
                {
                    unregister();
                    s_UnregisterCallbacks.Remove(view.GetHashCode());
                }
                cellGOPool.Clear(view.GetHashCode());
                CellScenePool.Clear(view.GetHashCode());
            };
        }

        static void OnSetSceneVisibilityEditor(CellValueEditorSceneBool editor, bool value)
        {
            editor.Element.showMixedValue =
                SceneVisibilityManager.instance.GetSceneVisibilityState(editor.Model) == SceneVisibilityManager.SceneVisState.Mixed;
            editor.Element.EnableInClassList("is-visible", value);
        }

        // internal for tests
        internal static void SetSceneVisibility(UnityEngine.SceneManagement.Scene scene)
        {
            SceneVisibilityManager.instance.ToggleScene(scene, SceneVisibilityManager.instance.GetSceneVisibilityState(scene));
        }

        static CellValueEditorSceneBool CreateSceneCellValueEditor_Visibility()
        {
            var editor = new CellValueEditorSceneBool(
                    getModelValue: ed => SceneVisibilityManager.instance.GetSceneVisibilityState(ed.Model) != SceneVisibilityManager.SceneVisState.AllHidden,
                    setModelValue: (ed, value) => SetSceneVisibility(ed.Model),
                    isDefaultValue: (ed, value) => !ed.Element.showMixedValue && value,
                    onSetEditorValue: OnSetSceneVisibilityEditor);
            return editor;
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static readonly Dictionary<int, Action> s_UnregisterCallbacks = new();

        [AutoStaticsCleanupOnCodeReload]
        static HierarchyViewColumnContextPool<CellValueEditorSceneBool> s_CellScenePool = null;

        internal static HierarchyViewColumnContextPool<CellValueEditorSceneBool> CellScenePool { get => s_CellScenePool ??= new(CreateSceneCellValueEditor_Visibility); }

        [HierarchyViewCellDescriptor(k_ColumnId, typeof(HierarchySceneHandler))]
        internal static void CreateSceneCellDesc_Visibility(HierarchyViewCellDescriptor desc)
        {
            desc.ClearCellContent = false;
            desc.BindCell = cell =>
            {
                var cellValueEditor = HierarchyWindowColumnUtility.BindCellToValueEditor(HierarchyWindowColumnUtility.GetScene(cell), cell, CellScenePool, k_Classes);
            };

            desc.UnbindCell = cell =>
            {
                HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, CellScenePool);
            };
        }

        static void OnSetGOVisibilityEditor(CellValueEditorGOBool editor, bool value)
        {
            if (!editor.Model)
                return;
            if (value)
            {
                // Object is shown:
                editor.Element.showMixedValue = editor.Model.transform.childCount > 0 &&
                                                !SceneVisibilityManager.instance.AreAllDescendantsVisible(editor.Model);
            }
            else
            {
                editor.Element.showMixedValue = editor.Model.transform.childCount > 0 &&
                                                !SceneVisibilityManager.instance.AreAllDescendantsHidden(editor.Model);
            }
            editor.Element.EnableInClassList("is-visible", value);
        }

        // internal for tests
        internal static void SetGameObjectsVisibility(HierarchyViewCell cell, bool value, bool includeChildren = true)
        {
            using var selectedGameObjects = HierarchyWindowColumnUtility.GetEditableTargetGameObjects(cell, out var length);
            var targets = selectedGameObjects.Span[..length].ToArray();

            if (value)
                SceneVisibilityManager.instance.Show(targets, includeChildren);
            else
                SceneVisibilityManager.instance.Hide(targets, includeChildren);
        }

        static CellValueEditorGOBool CreateGOCellValueEditor_Visibility()
        {
            var editor = new CellValueEditorGOBool(
                    getModelValue: ed => ed.Model && !SceneVisibilityManager.instance.IsHidden(ed.Model),
                    setModelValue: (ed, value) => SetGameObjectsVisibility(ed.Cell, value, !ed.AltKeyPressed),
                    isDefaultValue: (ed, value) => !ed.Element.showMixedValue && value,
                    onSetEditorValue: OnSetGOVisibilityEditor);
            return editor;
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static HierarchyViewColumnContextPool<CellValueEditorGOBool> s_CellGOPool = null;
        internal static HierarchyViewColumnContextPool<CellValueEditorGOBool> cellGOPool { get => s_CellGOPool ??= new(CreateGOCellValueEditor_Visibility); }

        [HierarchyViewCellDescriptor(k_ColumnId, typeof(HierarchyGameObjectHandler))]
        internal static void CreateGameObjectCellDesc_Visibility(HierarchyViewCellDescriptor desc)
        {
            desc.ClearCellContent = false;
            desc.BindCell = cell =>
            {
                var go = HierarchyWindowColumnUtility.GetGameObject(cell);
                if (!HierarchyWindowColumnUtility.IsGameObjectEditable(go))
                {
                    HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
                    return;
                }

                HierarchyWindowColumnUtility.BindCellToValueEditor(go, cell, cellGOPool, k_Classes);
            };

            desc.UnbindCell = cell =>
            {
                HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
            };
        }
    }

    /// <summary>
    /// Utility class containing Column definition for GameObject and Scene Pickable ability in Scene.
    /// Note that cells in this column have ClearCellContent=false. Which means the cell is not Clear after Unbind. Cell will contain a Toggle for editing.
    /// </summary>
    internal sealed class HierarchyWindowColumnPicking
    {
        /// <summary>
        /// Column Id for object Pickability in Scene.
        /// </summary>
        public const string k_ColumnId = "Picking";

        /// <summary>
        /// USS class for toggle icon used to display object Pickability in Scene.
        /// </summary>
        public static readonly UniqueStyleString k_ScenePicking = new("toggle-scene-picking");

        /// <summary>
        /// List of USS classes used to display object Pickability in Scene.
        /// </summary>
        public static readonly UniqueStyleString[] k_Classes = new[] { HierarchyWindowColumnUtility.k_ToggleIcon, k_ScenePicking };

        [HierarchyViewColumnDescriptor(k_ColumnId)]
        internal static void CreateColumnDesc_Picking(HierarchyViewColumnDescriptor desc)
        {
            desc.Icon = EditorGUIUtility.LoadIcon("scenepicking_pickable_hover");
            desc.Tooltip = "Change picking ability of a scene object";
            desc.DefaultPriority = -1000;
            desc.DefaultWidth = 20;
            desc.DefaultVisibility = true;

            desc.BindColumn += (column, view) =>
            {
                Action updatePicking = () =>
                {
                    foreach (var editor in cellGOPool.GetActiveObjects(view.GetHashCode()))
                    {
                        editor.SyncEditorValueWithoutNotify();
                    }

                    foreach (var editor in cellScenePool.GetActiveObjects(view.GetHashCode()))
                    {
                        editor.SyncEditorValueWithoutNotify();
                    }
                };

                s_UnregisterCallbacks[view.GetHashCode()] = () => SceneVisibilityManager.pickingChanged -= updatePicking;
                SceneVisibilityManager.pickingChanged += updatePicking;
            };

            desc.UnbindColumn += (column, view) =>
            {
                if (s_UnregisterCallbacks.TryGetValue(view.GetHashCode(), out var unregister))
                {
                    unregister();
                    s_UnregisterCallbacks.Remove(view.GetHashCode());
                }
                cellGOPool.Clear(view.GetHashCode());
                cellScenePool.Clear(view.GetHashCode());
            };
        }

        static void OnSetScenePickingEditor(CellValueEditorSceneBool editor, bool value)
        {
            editor.Element.showMixedValue =
                SceneVisibilityManager.instance.GetScenePickingState(editor.Model) == SceneVisibilityManager.ScenePickingState.Mixed;
            editor.Element.EnableInClassList("is-pickable", value);
        }

        // internal for tests
        internal static void SetScenePicking(UnityEngine.SceneManagement.Scene scene)
        {
            var pickingState = SceneVisibilityManager.instance.GetScenePickingState(scene);
            if (pickingState == SceneVisibilityManager.ScenePickingState.Mixed ||
                pickingState == SceneVisibilityManager.ScenePickingState.PickingDisabledAll)
            {
                SceneVisibilityManager.instance.EnablePicking(scene);
            }
            else
            {
                SceneVisibilityManager.instance.DisablePicking(scene);
            }
        }

        static CellValueEditorSceneBool CreateSceneCellValueEditor_Picking()
        {
            var editor = new CellValueEditorSceneBool(
                getModelValue: ed =>
                    SceneVisibilityManager.instance.GetScenePickingState(ed.Model) is
                    SceneVisibilityManager.ScenePickingState.PickingEnabledAll or SceneVisibilityManager.ScenePickingState.Mixed,
                setModelValue: (ed, value) => SetScenePicking(ed.Model),
                isDefaultValue: (ed, value) => !ed.Element.showMixedValue && value,
                onSetEditorValue: OnSetScenePickingEditor);
            return editor;
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static readonly Dictionary<int, Action> s_UnregisterCallbacks = new();
        [AutoStaticsCleanupOnCodeReload]
        internal static HierarchyViewColumnContextPool<CellValueEditorSceneBool> s_CellScenePool = null;
        internal static HierarchyViewColumnContextPool<CellValueEditorSceneBool> cellScenePool { get => s_CellScenePool ??= new(CreateSceneCellValueEditor_Picking); }



        [HierarchyViewCellDescriptor(k_ColumnId, typeof(HierarchySceneHandler))]
        internal static void CreateSceneCellDesc_Picking(HierarchyViewCellDescriptor desc)
        {
            desc.ClearCellContent = false;
            desc.BindCell = cell =>
            {
                var cellValueEditor = HierarchyWindowColumnUtility.BindCellToValueEditor(HierarchyWindowColumnUtility.GetScene(cell), cell, cellScenePool, k_Classes);
            };

            desc.UnbindCell = cell =>
            {
                HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellScenePool);
            };
        }

        static void OnSetGOPickingEditor(CellValueEditorGOBool editor, bool value)
        {
            if (!editor.Model)
                return;
            if (value)
            {
                editor.Element.showMixedValue = editor.Model.transform.childCount > 0 &&
                                                !SceneVisibilityManager.instance.IsPickingEnabledOnAllDescendants(editor.Model);
            }
            else
            {
                editor.Element.showMixedValue = editor.Model.transform.childCount > 0 &&
                                                !SceneVisibilityManager.instance.IsPickingDisabledOnAllDescendants(editor.Model);
            }
            editor.Element.EnableInClassList("is-pickable", value);
        }

        // internal for tests
        internal static void SetGameObjectsPicking(HierarchyViewCell cell, bool value, bool includeChildren = true)
        {
            using var selectedGameObjects = HierarchyWindowColumnUtility.GetEditableTargetGameObjects(cell, out var length);
            var targets = selectedGameObjects.Span[..length].ToArray();

            if (value)
                SceneVisibilityManager.instance.EnablePicking(targets, includeChildren);
            else
                SceneVisibilityManager.instance.DisablePicking(targets, includeChildren);
        }

        static CellValueEditorGOBool CreateGOCellValueEditor_Picking()
        {
            var editor = new CellValueEditorGOBool(
                    getModelValue: ed => ed.Model && !SceneVisibilityManager.instance.IsPickingDisabled(ed.Model),
                    setModelValue: (ed, value) => SetGameObjectsPicking(ed.Cell, value, !ed.AltKeyPressed),
                    isDefaultValue: (ed, value) => !ed.Element.showMixedValue && value,
                    onSetEditorValue: OnSetGOPickingEditor);
            return editor;
        }
        [AutoStaticsCleanupOnCodeReload]
        internal static HierarchyViewColumnContextPool<CellValueEditorGOBool> s_CellGOPool = null;
        internal static HierarchyViewColumnContextPool<CellValueEditorGOBool> cellGOPool { get => s_CellGOPool ??= new(CreateGOCellValueEditor_Picking); }

        [HierarchyViewCellDescriptor(k_ColumnId, typeof(HierarchyGameObjectHandler))]
        internal static void CreateGameObjectCellDesc_Picking(HierarchyViewCellDescriptor desc)
        {
            desc.ClearCellContent = false;
            desc.BindCell = cell =>
            {
                var go = HierarchyWindowColumnUtility.GetGameObject(cell);
                if (!HierarchyWindowColumnUtility.IsGameObjectEditable(go))
                {
                    HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
                    return;
                }

                HierarchyWindowColumnUtility.BindCellToValueEditor(go, cell, cellGOPool, k_Classes);
            };

            desc.UnbindCell = cell =>
            {
                HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
            };
        }
    }

    /// <summary>
    /// Utility class containing Column definition for GameObject Tag
    /// Note that cells in this column have ClearCellContent=false. Which means the cell is not Clear after Unbind. Cell will contain a TagField for editing.
    /// </summary>
    internal sealed class HierarchyWindowColumnTag
    {
        /// <summary>
        /// Column Id for object Tag
        /// </summary>
        public const string k_ColumnId = "GameObject/Tag";

        [HierarchyViewColumnDescriptor(k_ColumnId)]
        internal static void CreateColumnDesc(HierarchyViewColumnDescriptor desc)
        {
            desc.Title = "Tag";
            desc.Tooltip = "GameObject Tag";
            desc.DefaultPriority = 100;
            desc.DefaultWidth = 100;

            desc.UnbindColumn += (column, view) =>
            {
                cellGOPool.Clear(view.GetHashCode());
            };
        }

        [HierarchyViewCellDescriptor(k_ColumnId, typeof(HierarchyGameObjectHandler))]
        internal static void CreateGameObjectCellDesc_Tag(HierarchyViewCellDescriptor desc)
        {
            desc.ClearCellContent = false;
            desc.BindCell = cell =>
            {
                var go = HierarchyWindowColumnUtility.GetGameObject(cell);
                if (!HierarchyWindowColumnUtility.IsGameObjectEditable(go))
                {
                    HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
                    return;
                }

                HierarchyWindowColumnUtility.BindCellToValueEditor(go, cell, cellGOPool);
            };
            desc.UnbindCell = cell => HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
        }

        // internal for tests
        internal static void SetGameObjectsTag(HierarchyViewCell cell, string value)
        {
            using var selectedGameObjects = HierarchyWindowColumnUtility.GetEditableTargetGameObjects(cell, out var length);
            var targets = selectedGameObjects.Span[..length].ToArray();

            Undo.RecordObjects(targets, "Change Tag");
            foreach (var go in targets)
            {
                go.tag = value;
            }
        }

        static HierarchyViewCellValueEditor<GameObject, TagField, string> CreateGOCellValueEditor_Tag()
        {
            var editor = new HierarchyViewCellValueEditor<GameObject, TagField, string>(
                    getModelValue: ed => ed.Model.tag,
                    setModelValue: (ed, value) => SetGameObjectsTag(ed.Cell, value),
                    isDefaultValue: (ed, value) => string.IsNullOrEmpty(value) || value == "Untagged");
            return editor;
        }
        [AutoStaticsCleanupOnCodeReload]
        internal static HierarchyViewColumnContextPool<HierarchyViewCellValueEditor<GameObject, TagField, string>> s_CellGOPool = null;
        internal static HierarchyViewColumnContextPool<HierarchyViewCellValueEditor<GameObject, TagField, string>> cellGOPool { get => s_CellGOPool ??= new(CreateGOCellValueEditor_Tag); }
    }

    /// <summary>
    /// Utility class containing Column definition for GameObject Layer
    /// Note that cells in this column have ClearCellContent=false. Which means the cell is not Clear after Unbind. Cell will contain a LayerField for editing.
    /// </summary>
    internal sealed class HierarchyWindowColumnLayer
    {
        /// <summary>
        /// Column Id for object Layer
        /// </summary>
        public const string k_ColumnId = "GameObject/Layer";

        [HierarchyViewColumnDescriptor(k_ColumnId)]
        internal static void CreateColumnDesc(HierarchyViewColumnDescriptor desc)
        {
            desc.Title = "Layer";
            desc.Tooltip = "GameObject Layer";
            desc.DefaultPriority = 100;
            desc.DefaultWidth = 150;

            desc.UnbindColumn += (column, view) =>
            {
                cellGOPool.Clear(view.GetHashCode());
            };
        }

        [HierarchyViewCellDescriptor(k_ColumnId, typeof(HierarchyGameObjectHandler))]
        internal static void CreateGameObjectCellDesc_Layer(HierarchyViewCellDescriptor desc)
        {
            desc.ClearCellContent = false;
            desc.BindCell = cell =>
            {
                var go = HierarchyWindowColumnUtility.GetGameObject(cell);
                if (!HierarchyWindowColumnUtility.IsGameObjectEditable(go))
                {
                    HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
                    return;
                }

                HierarchyWindowColumnUtility.BindCellToValueEditor(go, cell, cellGOPool);
            };
            desc.UnbindCell = cell
                => HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
        }

        // internal for tests
        internal static bool SetGameObjectsLayer(HierarchyViewCell cell, int value)
        {
            using var selectedGameObjects = HierarchyWindowColumnUtility.GetEditableTargetGameObjects(cell, out var length);
            var targets = selectedGameObjects.Span[..length];

            return SceneModeUtility.SetLayer(targets, value, targets.Length == 1 ? targets[0].name : "Selected GameObjects");
        }

        static HierarchyViewCellValueEditor<GameObject, LayerField, int> CreateGOCellValueEditor_Layer()
        {
            var editor = new HierarchyViewCellValueEditor<GameObject, LayerField, int>(
                    getModelValue: ed => ed.Model.layer,
                    setModelValue: (ed, value) =>
                    {
                        if (!SetGameObjectsLayer(ed.Cell, value))
                            ed.Element.SetValueWithoutNotify(ed.Model.layer);
                    },
                    isDefaultValue: (ed, value) => value == 0);
            return editor;
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static HierarchyViewColumnContextPool<HierarchyViewCellValueEditor<GameObject, LayerField, int>> s_CellGOPool = null;
        internal static HierarchyViewColumnContextPool<HierarchyViewCellValueEditor<GameObject, LayerField, int>> cellGOPool { get => s_CellGOPool ??= new(CreateGOCellValueEditor_Layer); }
    }

    /// <summary>
    /// Utility class containing Column definition for GameObject Is Static property.
    /// Note that cells in this column have ClearCellContent=false. Which means the cell is not Clear after Unbind. Cell will contain a Toggle for editing.
    /// </summary>
    internal sealed class HierarchyWindowColumnStatic
    {
        /// <summary>
        /// Column Id for object Static
        /// </summary>
        public const string k_ColumnId = "GameObject/Static";

        [HierarchyViewColumnDescriptor(k_ColumnId)]
        internal static void CreateColumnDesc_Static(HierarchyViewColumnDescriptor desc)
        {
            desc.Title = "Static";
            desc.Tooltip = "Is GameObject Static";
            desc.DefaultPriority = 100;
            desc.DefaultWidth = 75;

            desc.UnbindColumn += (column, view) =>
            {
                cellGOPool.Clear(view.GetHashCode());
            };
        }

        [HierarchyViewCellDescriptor(k_ColumnId, typeof(HierarchyGameObjectHandler))]
        internal static void CreateGameObjectCellDesc_Static(HierarchyViewCellDescriptor desc)
        {
            desc.ClearCellContent = false;
            desc.BindCell = cell =>
            {
                var gameObject = HierarchyWindowColumnUtility.GetGameObject(cell);
                if (!HierarchyWindowColumnUtility.IsGameObjectEditable(gameObject))
                {
                    HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
                    return;
                }

                var ed = HierarchyWindowColumnUtility.BindCellToValueEditor(gameObject, cell, cellGOPool);
                ed.Element.showMixedValue = GameObjectInspector.ShowMixedStaticEditorFlags(GameObjectUtility.GetStaticEditorFlags(gameObject));
            };
            desc.UnbindCell = cell
                => HierarchyWindowColumnUtility.UnbindCellFromValueEditor(cell, cellGOPool);
        }

        // internal for tests
        internal static bool SetGameObjectsStatic(HierarchyViewCell cell, bool value)
        {
            using var selectedGameObjects = HierarchyWindowColumnUtility.GetEditableTargetGameObjects(cell, out var length);
            // SetStaticFlags takes Span<Object> and Span are invariant (ie. Span<GameObject> are not Span<Object>). We need to copy
            using var targets = new RentSpan<Object>(length);
            var count = 0;
            foreach (var gameObject in selectedGameObjects.Span[..length])
            {
                targets.Span[count++] = gameObject;
            }

            return SceneModeUtility.SetStaticFlags(targets.Span, int.MaxValue, value);
        }

        static CellValueEditorGOBool CreateGOCellValueEditor_Static()
        {
            var editor = new CellValueEditorGOBool(
                    getModelValue: ed => ed.Model && ed.Model.isStatic,
                    setModelValue: (ed, value) =>
                    {
                        if (!SetGameObjectsStatic(ed.Cell, value))
                            ed.Element.SetValueWithoutNotify(ed.Model.isStatic);
                    },
                    isDefaultValue: (ed, value) => value == false);
            return editor;
        }
        [AutoStaticsCleanupOnCodeReload]
        internal static HierarchyViewColumnContextPool<CellValueEditorGOBool> s_CellGOPool = null;
        internal static HierarchyViewColumnContextPool<CellValueEditorGOBool> cellGOPool { get => s_CellGOPool ??= new(CreateGOCellValueEditor_Static); }
    }
}
