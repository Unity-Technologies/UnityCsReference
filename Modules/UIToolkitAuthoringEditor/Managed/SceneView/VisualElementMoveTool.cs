// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    [EditorToolContext("VisualElement", targetType = typeof(VisualElementSelection))]
    sealed class VisualElementToolContext : EditorToolContext
    {
        protected override Type GetEditorToolType(Tool tool)
        {
            return tool switch
            {
                Tool.Move => typeof(VisualElementMoveTool),
                Tool.Rotate => typeof(VisualElementRotateTool),
                Tool.Scale => typeof(VisualElementScaleTool),
                Tool.Rect => null,
                Tool.Transform => null,
                Tool.Custom => null,
                _ => base.GetEditorToolType(tool),
            };
        }
    }

    [InitializeOnLoad]
    static class SelectionContextRouter
    {
        static SelectionContextRouter()
        {
            Selection.selectionChanged += OnStateChanged;
            StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;
            OnStateChanged();
        }

        static void OnStageChanged(Stage _) => OnStateChanged();

        static void OnStateChanged()
        {
            // Deferred: ToolManager.SetActiveContext queries the SceneView's tool-context
            // resolver, which lags Selection during a stage transition (the handler can fire
            // before the SceneView has refreshed). Running on the next editor tick lets the
            // transition settle first.
            EditorApplication.delayCall += ApplyContextSwitch;
        }

        static void ApplyContextSwitch()
        {
            // VE context only activates inside an authoring stage AND with a VE selected.
            // Outside the stage the VTA is read-only, so the transform tools have nothing to do.
            var wantVeContext = VisualElementToolUtility.IsAuthoringStageActive()
                                && Selection.activeObject is VisualElementSelection;
            var activeIsVeContext = ToolManager.activeContextType == typeof(VisualElementToolContext);

            if (wantVeContext && !activeIsVeContext && ToolManager.CanSetActiveContext<VisualElementToolContext>())
                ToolManager.SetActiveContext<VisualElementToolContext>();
            else if (!wantVeContext && activeIsVeContext)
                ToolManager.SetActiveContext<GameObjectToolContext>();
        }
    }

    sealed class VisualElementMoveTool : EditorTool
    {
        [SerializeField] bool m_IsDragging;
        readonly LiveReloadSuspension m_LiveReloadSuspension = new();

        void OnDisable()
        {
            if (m_IsDragging)
            {
                m_LiveReloadSuspension.Restore();
                m_IsDragging = false;
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView)
                return;

            // The VTA is read-only outside an authoring stage
            if (!VisualElementToolUtility.IsAuthoringStageActive())
                return;

            var allSelected = VisualElementToolUtility.GetSelectedElements();
            if (allSelected.Length == 0)
                return;

            // Active element drives gizmo orientation (local mode) and pivot-mode position.
            var activeElement = VisualElementToolUtility.GetSelectedElement();
            if (activeElement == null)
                return;

            var activePanel = VisualElementSceneViewOverlay.FindPanelComponentForElement(activeElement);
            if (activePanel == null)
                return;

            // Topmost filter: descendants of a selected ancestor move with that ancestor,
            // applying the delta to both would move them twice.
            var topmost = VisualElementToolUtility.GetTopmostElements(allSelected);

            // Pause live reload while dragging so per-frame writes don't rebuild the panel tree.
            var dragInProgress = GUIUtility.hotControl != 0;
            if (dragInProgress && !m_IsDragging)
            {
                m_LiveReloadSuspension.Suspend(activePanel);
                m_IsDragging = true;
            }
            else if (!dragInProgress && m_IsDragging)
            {
                m_LiveReloadSuspension.Restore();
                m_IsDragging = false;
            }

            var pivot = Tools.pivotMode == PivotMode.Center
                ? VisualElementToolUtility.GetSelectionWorldCenter(allSelected, activePanel)
                : VisualElementToolUtility.GetElementWorldCenter(activeElement, activePanel);
            var rotation = VisualElementToolUtility.GetGizmoRotation(activeElement, activePanel);

            EditorGUI.BeginChangeCheck();
            var newPivot = Handles.PositionHandle(pivot, rotation);
            if (!EditorGUI.EndChangeCheck())
                return;

            var worldDelta = newPivot - pivot;
            if (worldDelta.sqrMagnitude < float.Epsilon)
                return;

            // All selected elements share the active panel in stage mode (all redirected from
            // the same UXML), so compute the world -> pixel conversion once.
            var transformOwner = VisualElementToolUtility.FindTransformOwner(activePanel);
            if (transformOwner == null)
                return;
            var worldToPixel = VisualElementToolUtility.GetPanelPixelToWorldMatrix(transformOwner).inverse;
            var pixelDelta = worldToPixel.MultiplyVector(worldDelta);

            foreach (var element in topmost)
                ApplyTranslateDelta(element, pixelDelta);
        }

        static void ApplyTranslateDelta(VisualElement element, Vector3 pixelDelta)
        {
            // Runtime-created VEs without an asset can't be persisted; silent no-op by design.
            if (element.visualElementAsset == null)
                return;

            var current = element.resolvedStyle.translate;
            var sum = VisualElementToolUtility.CleanVector3(new Vector3(
                current.x + pixelDelta.x,
                current.y + pixelDelta.y,
                current.z + pixelDelta.z));
            var next = new Translate(
                new Length(sum.x, LengthUnit.Pixel),
                new Length(sum.y, LengthUnit.Pixel),
                sum.z);

            SetInlineStylePropertyCommand<Translate>.Execute(
                CommandSources.Scene,
                element,
                StylePropertyId.Translate,
                StylePropertyBinding.SetTranslate,
                next);
        }
    }
}
