// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    // This class is in charge of handling Grid component based grid in the scene view (rendering, snapping)
    // It will hide global scene view grid when it has something to render
    internal class SceneViewGridManager : ScriptableSingleton<SceneViewGridManager>
    {
        internal static readonly PrefColor sceneViewGridComponentGizmo = new PrefColor("Scene/Grid Component", 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 25.5f / 255.0f);

        private static Mesh s_GridProxyMesh;
        private static Material s_GridProxyMaterial;
        private static Color s_LastGridProxyColor;
        [SerializeField] private GridLayout m_ActiveGridProxy;

        private bool m_RegisteredEventHandlers;

        private bool active { get { return m_ActiveGridProxy != null; } }
        private GridLayout activeGridProxy { get { return m_ActiveGridProxy; } }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            instance.RegisterEventHandlers();
        }

        void OnEnable()
        {
            RegisterEventHandlers();
        }

        void RegisterEventHandlers()
        {
            if (m_RegisteredEventHandlers)
                return;

            SceneView.onSceneGUIDelegate += OnSceneGuiDelegate;
            Selection.selectionChanged += UpdateCache;
            EditorApplication.hierarchyWindowChanged += UpdateCache;
            EditMode.editModeStarted += OnEditModeStart;
            EditMode.editModeEnded += OnEditModeEnd;
            GridPaintingState.brushChanged += OnBrushChanged;
            GridPaintingState.scenePaintTargetChanged += OnScenePaintTargetChanged;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            GridSnapping.snapPosition = OnSnapPosition;
            GridSnapping.activeFunc = GetActive;

            m_RegisteredEventHandlers = true;
        }

        private void OnBrushChanged(GridBrushBase brush)
        {
            UpdateCache();
        }

        private void OnEditModeEnd(IToolModeOwner owner)
        {
            UpdateCache();
        }

        private void OnEditModeStart(IToolModeOwner owner, EditMode.SceneViewEditMode editMode)
        {
            UpdateCache();
        }

        private void OnScenePaintTargetChanged(GameObject scenePaintTarget)
        {
            UpdateCache();
        }

        private void OnUndoRedoPerformed()
        {
            FlushCachedGridProxy();
        }

        void OnDisable()
        {
            FlushCachedGridProxy();
            SceneView.onSceneGUIDelegate -= OnSceneGuiDelegate;
            Selection.selectionChanged -= UpdateCache;
            EditorApplication.hierarchyWindowChanged -= UpdateCache;
            EditMode.editModeStarted -= OnEditModeStart;
            EditMode.editModeEnded -= OnEditModeEnd;
            GridPaintingState.brushChanged -= OnBrushChanged;
            GridPaintingState.scenePaintTargetChanged -= OnScenePaintTargetChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            GridSnapping.snapPosition = null;
            GridSnapping.activeFunc = null;
            m_RegisteredEventHandlers = false;
        }

        private void UpdateCache()
        {
            GridLayout gridProxy = null;
            if (PaintableGrid.InGridEditMode())
                gridProxy = GridPaintingState.scenePaintTarget != null ? GridPaintingState.scenePaintTarget.GetComponentInParent<GridLayout>() : null;
            else
                gridProxy = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<GridLayout>() : null;
            if (gridProxy != m_ActiveGridProxy)
            {
                m_ActiveGridProxy = gridProxy;
                FlushCachedGridProxy();
            }

            ShowGlobalGrid(!active);
        }

        private void OnSceneGuiDelegate(SceneView sceneView)
        {
            if (active && AnnotationUtility.showGrid)
                DrawGrid(activeGridProxy);
        }

        private static void DrawGrid(GridLayout gridLayout)
        {
            if (sceneViewGridComponentGizmo.Color != s_LastGridProxyColor)
            {
                FlushCachedGridProxy();
                s_LastGridProxyColor = sceneViewGridComponentGizmo.Color;
            }
            GridEditorUtility.DrawGridGizmo(gridLayout, gridLayout.transform, s_LastGridProxyColor, ref s_GridProxyMesh, ref s_GridProxyMaterial);
        }

        private void ShowGlobalGrid(bool value)
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                sceneView.showGlobalGrid = value;
            }
        }

        private bool GetActive()
        {
            return active;
        }

        private Vector3 OnSnapPosition(Vector3 position)
        {
            Vector3 result = position;
            if (active && !EditorGUI.actionKey)
            {
                Vector3 local = activeGridProxy.WorldToLocal(position);
                Vector3 interpolatedCell = activeGridProxy.LocalToCellInterpolated(local);
                Vector3 roundedCell = new Vector3(
                        Mathf.Round(2.0f * interpolatedCell.x) / 2,
                        Mathf.Round(2.0f * interpolatedCell.y) / 2,
                        Mathf.Round(2.0f * interpolatedCell.z) / 2
                        );
                local = activeGridProxy.CellToLocalInterpolated(roundedCell);
                result = activeGridProxy.LocalToWorld(local);
            }
            return result;
        }

        internal static void FlushCachedGridProxy()
        {
            if (s_GridProxyMesh == null)
                return;

            DestroyImmediate(s_GridProxyMesh);
            s_GridProxyMesh = null;
            s_GridProxyMaterial = null;
        }
    }
}
