// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    sealed class VisualElementRotateTool : EditorTool
    {
        [SerializeField] bool m_IsDragging;
        [SerializeField] bool m_StartIsCenterMode;
        [SerializeField] Vector3 m_StartPivotWorld;
        [SerializeField] Quaternion m_StartGizmoRotation;
        [SerializeField] Quaternion m_StartPanelRotation;
        IPanelComponent m_StartPanel;

        struct ElementSnapshot
        {
            public VisualElement Element;
            public Rotate Rotate;
            public Vector3 TranslatePixels;
            public Vector3 OriginWorld;
        }

        ElementSnapshot[] m_ElementSnapshots;

        readonly LiveReloadSuspension m_LiveReloadSuspension = new();

        static readonly Quaternion s_FlipYZ = Quaternion.AngleAxis(180f, Vector3.right);

        void OnDisable()
        {
            if (m_IsDragging)
            {
                m_LiveReloadSuspension.Restore();
                m_IsDragging = false;
            }
            m_ElementSnapshots = null;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView)
                return;
            if (!VisualElementToolUtility.IsAuthoringStageActive())
                return;

            var allSelected = VisualElementToolUtility.GetSelectedElements();
            if (allSelected.Length == 0)
                return;

            var activeElement = VisualElementToolUtility.GetSelectedElement();
            if (activeElement == null)
                return;

            var activePanel = VisualElementSceneViewOverlay.FindPanelComponentForElement(activeElement);
            if (activePanel == null)
                return;

            var topmost = VisualElementToolUtility.GetTopmostElements(allSelected);
            var dragInProgress = GUIUtility.hotControl != 0;

            if (dragInProgress && !m_IsDragging)
            {
                m_StartIsCenterMode = Tools.pivotMode == PivotMode.Center;
                var pivot = m_StartIsCenterMode
                    ? VisualElementToolUtility.GetSelectionWorldCenter(allSelected, activePanel)
                    : VisualElementToolUtility.GetElementWorldPivot(activeElement, activePanel);

                m_StartPivotWorld = pivot;
                m_StartGizmoRotation = VisualElementToolUtility.GetGizmoRotation(activeElement, activePanel);
                m_StartPanelRotation = activePanel.gameObject.transform.rotation;
                m_StartPanel = activePanel;

                m_ElementSnapshots = new ElementSnapshot[topmost.Length];
                for (var i = 0; i < topmost.Length; i++)
                {
                    var el = topmost[i];
                    var translate = el.resolvedStyle.translate;
                    m_ElementSnapshots[i] = new ElementSnapshot
                    {
                        Element = el,
                        Rotate = el.resolvedStyle.rotate,
                        TranslatePixels = translate,
                        OriginWorld = VisualElementToolUtility.GetElementWorldPivot(el, activePanel),
                    };
                }
                m_LiveReloadSuspension.Suspend(m_StartPanel);
                m_IsDragging = true;
            }
            else if (!dragInProgress && m_IsDragging)
            {
                m_LiveReloadSuspension.Restore();
                m_IsDragging = false;
                m_ElementSnapshots = null;
            }

            Vector3 gizmoPivot;
            Quaternion gizmoRotation;
            if (m_IsDragging)
            {
                gizmoPivot = m_StartPivotWorld;
                gizmoRotation = m_StartGizmoRotation;
            }
            else
            {
                gizmoPivot = Tools.pivotMode == PivotMode.Center
                    ? VisualElementToolUtility.GetSelectionWorldCenter(allSelected, activePanel)
                    : VisualElementToolUtility.GetElementWorldPivot(activeElement, activePanel);
                gizmoRotation = VisualElementToolUtility.GetGizmoRotation(activeElement, activePanel);
            }

            EditorGUI.BeginChangeCheck();
            var newRotation = Handles.RotationHandle(gizmoRotation, gizmoPivot);
            if (!EditorGUI.EndChangeCheck() || !m_IsDragging)
                return;

            ApplyAbsoluteRotation(newRotation);
        }

        void ApplyAbsoluteRotation(Quaternion newRotation)
        {
            // Drag in world space, converted to the panel's pixel frame (USS rotate's axis is
            // interpreted in panel-pixel space). Pixel space has its Y/Z flipped vs GameObject
            // local, so we conjugate by the same flip ScaleAndFlipMatrix bakes into the chain.
            var totalWorldRotation = newRotation * Quaternion.Inverse(m_StartGizmoRotation);
            var totalLocalGO = Quaternion.Inverse(m_StartPanelRotation) * totalWorldRotation * m_StartPanelRotation;
            var totalDragPixel = s_FlipYZ * totalLocalGO * s_FlipYZ;

            var transformOwner = VisualElementToolUtility.FindTransformOwner(m_StartPanel);
            var worldToPixel = transformOwner != null
                ? VisualElementToolUtility.GetPanelPixelToWorldMatrix(transformOwner).inverse
                : Matrix4x4.identity;

            for (var i = 0; i < m_ElementSnapshots.Length; i++)
            {
                var snapshot = m_ElementSnapshots[i];
                var element = snapshot.Element;
                if (element?.visualElementAsset == null)
                    continue;

                // Compose drag * snapshot in pixel space, then write the resulting quaternion.
                var snapshotQuatPixel = Quaternion.AngleAxis(snapshot.Rotate.angle.ToDegrees(), snapshot.Rotate.axis);
                var nextQuatPixel = totalDragPixel * snapshotQuatPixel;
                SetInlineStylePropertyCommand<Rotate>.Execute(
                    CommandSources.Scene,
                    element,
                    StylePropertyId.Rotate,
                    StylePropertyBinding.SetRotate,
                    VisualElementToolUtility.CleanRotate(nextQuatPixel));

                // Orbit compensation only in center-mode,
                // in pivot-mode, each element rotates around its own transform-origin
                if (!m_StartIsCenterMode)
                    continue;

                var v = m_StartPivotWorld - snapshot.OriginWorld;
                if (v.sqrMagnitude < 1e-8f)
                    continue;
                if (transformOwner == null)
                    continue;

                var compensationWorld = v - totalWorldRotation * v;
                var compensationPixel = worldToPixel.MultiplyVector(compensationWorld);
                var sum = VisualElementToolUtility.CleanVector3(snapshot.TranslatePixels + compensationPixel);
                var nextTranslate = new Translate(
                    new Length(sum.x, LengthUnit.Pixel),
                    new Length(sum.y, LengthUnit.Pixel),
                    sum.z);

                SetInlineStylePropertyCommand<Translate>.Execute(
                    CommandSources.Scene,
                    element,
                    StylePropertyId.Translate,
                    StylePropertyBinding.SetTranslate,
                    nextTranslate);
            }
        }
    }
}
