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
    sealed class VisualElementScaleTool : EditorTool
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
            public Vector3 ScaleValue;
            public Vector3 TranslatePixels;
            public Vector3 OriginWorld;
        }

        ElementSnapshot[] m_ElementSnapshots;

        readonly LiveReloadSuspension m_LiveReloadSuspension = new();

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
                        ScaleValue = el.resolvedStyle.scale.value,
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
            var dragScale = Handles.ScaleHandle(Vector3.one, gizmoPivot, gizmoRotation, HandleUtility.GetHandleSize(gizmoPivot));
            if (!EditorGUI.EndChangeCheck() || !m_IsDragging)
                return;

            ApplyAbsoluteScale(dragScale);
        }

        void ApplyAbsoluteScale(Vector3 dragScale)
        {
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

                // dragScale is a cumulative multiplier (Handles.ScaleHandle was seeded with one),
                // so it's component-wise multiply against the snapshot, not addition.
                var nextScaleValue = VisualElementToolUtility.CleanVector3(Vector3.Scale(snapshot.ScaleValue, dragScale));
                SetInlineStylePropertyCommand<Scale>.Execute(
                    CommandSources.Scene,
                    element,
                    StylePropertyId.Scale,
                    StylePropertyBinding.SetScale,
                    new Scale(nextScaleValue));

                if (!m_StartIsCenterMode)
                    continue;

                var vWorld = m_StartPivotWorld - snapshot.OriginWorld;
                if (vWorld.sqrMagnitude < 1e-8f)
                    continue;
                if (transformOwner == null)
                    continue;

                var elementRotation = VisualElementToolUtility.GetElementWorldRotation(element, m_StartPanel);
                var vLocal = Quaternion.Inverse(elementRotation) * vWorld;
                var scaledVLocal = new Vector3(vLocal.x * dragScale.x, vLocal.y * dragScale.y, vLocal.z * dragScale.z);
                var scaledVWorld = elementRotation * scaledVLocal;
                var compensationWorld = vWorld - scaledVWorld;
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
