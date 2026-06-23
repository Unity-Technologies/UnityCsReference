// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Suspends the live-reload system on a panel for the duration of a transform drag.
    // The transform tool will modify the inline style sheet of the selected element(s), possibly adding
    // a new inline rule, which would trigger a reload by the live-reload system that would invalidate
    // VE references mid-frame.
    internal sealed class LiveReloadSuspension
    {
        ILiveReloadSystem m_LiveReloadSystem;
        LiveReloadTrackers m_PreviousTrackers;

        public void Suspend(IPanelComponent panelComponent)
        {
            if (m_LiveReloadSystem != null)
                return;
            if (panelComponent?.GetRootVisualElement()?.panel is not BaseVisualElementPanel panel)
                return;
            m_LiveReloadSystem = panel.liveReloadSystem;
            m_PreviousTrackers = m_LiveReloadSystem.enabledTrackers;
            m_LiveReloadSystem.enabledTrackers = m_PreviousTrackers & ~LiveReloadTrackers.Document;
        }

        public void Restore()
        {
            if (m_LiveReloadSystem != null)
                m_LiveReloadSystem.enabledTrackers = m_PreviousTrackers;
            m_LiveReloadSystem = null;
        }
    }

    internal static class VisualElementToolUtility
    {
        // Prevent scientific notation values for UXML/USS serialization
        const float k_CleanFloatEpsilon = 1e-4f;

        public static bool IsAuthoringStageActive()
            => StageUtility.GetCurrentStage() is VisualElementEditingStage;

        public static float CleanFloat(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
            return Mathf.Abs(v) < k_CleanFloatEpsilon ? 0f : v;
        }

        public static Vector3 CleanVector3(Vector3 v) => new(CleanFloat(v.x), CleanFloat(v.y), CleanFloat(v.z));

        public static Rotate CleanRotate(Quaternion q)
        {
            q.ToAngleAxis(out var angle, out var axis);
            angle = CleanFloat(angle);
            if (angle == 0f)
                return new Rotate(new Angle(0f, AngleUnit.Degree));
            return new Rotate(new Angle(angle, AngleUnit.Degree), CleanVector3(axis));
        }

        public static VisualElement GetSelectedElement()
        {
            // Transform tools write inline styles, so only Styles-editable selections qualify.
            // Read-only elements (template roots, attribute-only instanced elements) are filtered
            // out here so each tool's `if (activeElement == null) return;` naturally suppresses
            // gizmo activity.
            var sel = Selection.activeObject as VisualElementSelection;
            if (!CanEditStyles(sel))
                return null;
            return sel.Element != null ? RedirectStageCloneToScene(sel.Element) : null;
        }

        public static VisualElement[] GetSelectedElements()
        {
            var objects = Selection.objects;
            var result = new List<VisualElement>(objects.Length);
            foreach (var obj in objects)
            {
                if (obj is not VisualElementSelection sel)
                    continue;
                if (!CanEditStyles(sel))
                    continue;
                if (sel.Element == null)
                    continue;

                var redirected = RedirectStageCloneToScene(sel.Element);
                if (redirected != null)
                    result.Add(redirected);
            }
            return result.ToArray();
        }

        public static bool CanEditStyles(VisualElementSelection selection)
            => selection != null && (selection.EditFlags & VisualElementEditFlags.Styles) != 0;

        static VisualElement RedirectStageCloneToScene(VisualElement element)
        {
            var stagePanel = (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.GetAuthoringPanel();
            if (stagePanel == null || element.panel != stagePanel)
                return element;

            return FindFirstSceneInstanceOfAsset(element.visualElementAsset);
        }

        public static VisualElement FindFirstSceneInstanceOfAsset(VisualElementAsset asset)
        {
            // First live scene-panel instance of the given asset, or null. Use this to recover from
            // stale element refs (panel rebuilt after undo) or to redirect a stage clone to scene.
            foreach (var (_, sceneElement) in EnumerateScenePanelInstancesOfAsset(asset))
                return sceneElement;
            return null;
        }

        public static VisualElement[] GetTopmostElements(IReadOnlyList<VisualElement> elements)
        {
            // Drops descendants of selected ancestors, otherwise the delta applies twice (the
            // descendant moves with its ancestor, then again from the explicit apply).
            if (elements == null || elements.Count == 0)
                return Array.Empty<VisualElement>();

            var set = new HashSet<VisualElement>(elements);
            var result = new List<VisualElement>(elements.Count);
            foreach (var element in elements)
            {
                var hasSelectedAncestor = false;
                for (var p = element.parent; p != null; p = p.parent)
                {
                    if (set.Contains(p))
                    {
                        hasSelectedAncestor = true;
                        break;
                    }
                }
                if (!hasSelectedAncestor)
                    result.Add(element);
            }
            return result.ToArray();
        }

        public static Vector3 GetSelectionWorldCenter(IReadOnlyList<VisualElement> elements, IPanelComponent panel)
        {
            // Centroid of the selected elements' worldspace centers, gizmo placement in multi-select.
            if (elements == null || elements.Count == 0 || panel == null)
                return Vector3.zero;

            Bounds bounds = default;
            var initialized = false;
            foreach (var element in elements)
            {
                var center = GetElementWorldCenter(element, panel);
                if (!initialized)
                {
                    bounds = new Bounds(center, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(center);
                }
            }
            return initialized ? bounds.center : Vector3.zero;
        }

        public static IEnumerable<(IPanelComponent panel, VisualElement element)> EnumerateScenePanelInstancesOfAsset(VisualElementAsset asset)
        {
            // Active, enabled, world-space scene panels currently displaying the given UXML asset,
            // paired with the matching scene element
            if (asset == null)
                yield break;

            foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>())
                if (TryGetSceneInstance(doc, asset, out var element))
                    yield return (doc, element);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<PanelRenderer>())
                if (TryGetSceneInstance(renderer, asset, out var element))
                    yield return (renderer, element);
        }

        static bool TryGetSceneInstance(IPanelComponent panelComponent, VisualElementAsset asset, out VisualElement sceneElement)
        {
            sceneElement = null;
            if (!panelComponent.gameObject.activeInHierarchy || !panelComponent.GetComponentEnabled())
                return false;
            var panelSettings = panelComponent.panelSettings;
            if (panelSettings == null || panelSettings.renderMode != PanelRenderMode.WorldSpace)
                return false;
            sceneElement = panelComponent.GetRootVisualElement()?.FindElementByAsset(asset);
            return sceneElement != null;
        }

        public static IPanelComponent FindTransformOwner(IPanelComponent panelComponent)
        {
            var owner = panelComponent;
            while (owner != null && !PanelComponentUtils.IsTransformControlledByGameObject(owner))
                owner = owner.parentUI;
            return owner;
        }

        public static Vector3 GetElementWorldCenter(VisualElement element, IPanelComponent panelComponent)
        {
            var transformOwner = FindTransformOwner(panelComponent);
            if (transformOwner == null)
                return Vector3.zero;

            var ownerRoot = transformOwner.GetRootVisualElement();
            if (ownerRoot == null)
                return Vector3.zero;

            // Strip the owner root's contribution (already in Go2World) before re-applying it
            var elementToOwnerRoot = ownerRoot.worldTransformInverse * element.worldTransform;
            var elementToWorld = GetPanelPixelToWorldMatrix(transformOwner) * elementToOwnerRoot;

            var rect = element.rect;
            return elementToWorld.MultiplyPoint3x4(new Vector3(rect.width * 0.5f, rect.height * 0.5f, 0f));
        }

        public static Vector3 GetElementWorldPivot(VisualElement element, IPanelComponent panelComponent)
        {
            // The USS rotate/scale pivot in world space. worldTransform = T(origin)*R*S*T(-origin),
            // so evaluating it at origin gives a point that's stable under rotation/scale.
            var transformOwner = FindTransformOwner(panelComponent);
            if (transformOwner == null)
                return Vector3.zero;

            var ownerRoot = transformOwner.GetRootVisualElement();
            if (ownerRoot == null)
                return Vector3.zero;

            var elementToOwnerRoot = ownerRoot.worldTransformInverse * element.worldTransform;
            var elementToWorld = GetPanelPixelToWorldMatrix(transformOwner) * elementToOwnerRoot;
            return elementToWorld.MultiplyPoint3x4(element.resolvedStyle.transformOrigin);
        }

        public static Quaternion GetElementWorldRotation(VisualElement element, IPanelComponent panelComponent)
        {
            var panelRotation = panelComponent.gameObject.transform.rotation;
            return ComposeWorldRotation(panelRotation, element.resolvedStyle.rotate);
        }

        public static Quaternion ComposeWorldRotation(Quaternion panelRotation, Rotate cssRotate)
        {
            // Y/Z are flipped between panel-pixel and GameObject space (Y down vs Y up).
            // ToDegrees() handles rad/turn/grad styles — AngleAxis only takes degrees.
            var goAxis = new Vector3(cssRotate.axis.x, -cssRotate.axis.y, -cssRotate.axis.z);
            return panelRotation * Quaternion.AngleAxis(cssRotate.angle.ToDegrees(), goAxis);
        }

        public static Quaternion GetGizmoRotation(VisualElement element, IPanelComponent panelComponent)
        {
            // Honors the toolbar's Local/Global/Grid toggle.
            return Tools.pivotRotation switch
            {
                PivotRotation.Global => Quaternion.identity,
                PivotRotation.Grid => EditorSnapSettings.gridRotation,
                _ => GetElementWorldRotation(element, panelComponent),
            };
        }

        public static Matrix4x4 GetPanelPixelToGameObjectMatrix(IPanelComponent transformOwner)
        {
            // panel-pixel to GameObject-local (pivot offset + 1/ppu scale + Y/Z flip).
            var pivotOffset = ComputePivotOffset(transformOwner);
            var pixelsPerUnit = GetPixelsPerUnit(transformOwner);
            return PanelComponentUtils.TransformToGameObjectMatrix(pivotOffset, pixelsPerUnit);
        }

        public static Matrix4x4 GetPanelPixelToWorldMatrix(IPanelComponent transformOwner)
        {
            // Composite panel-pixel to world. Use MultiplyVector with this when converting deltas;
            // MultiplyPoint3x4 with the inverse to convert world positions back to panel-pixel.
            var ownerToGameObject = GetPanelPixelToGameObjectMatrix(transformOwner);
            var goToWorld = transformOwner.gameObject.transform.localToWorldMatrix;
            return goToWorld * ownerToGameObject;
        }

        public static void ApplyTransformOwnerMatrix(IPanelComponent transformOwner, ref Bounds bb)
        {
            // Transforms BBox (in panel-pixel space) into the transform owner's GameObject local space.
            var toGameObject = GetPanelPixelToGameObjectMatrix(transformOwner);
            VisualElement.TransformAlignedBounds(ref toGameObject, ref bb);
        }

        static Vector2 ComputePivotOffset(IPanelComponent panelComponent)
        {
            var root = panelComponent.GetRootVisualElement();
            if (root == null)
                return Vector2.zero;

            var pivotPercent = PanelComponentUtils.GetPivotAsPercent(panelComponent.pivot);
            var localBounds = PanelComponentUtils.LocalBoundsFromPivotSource(root, panelComponent.pivotReferenceSize);

            return new Vector2(
                -localBounds.center.x - (pivotPercent.x - 0.5f) * localBounds.size.x,
                -localBounds.center.y - (pivotPercent.y - 0.5f) * localBounds.size.y);
        }

        static float GetPixelsPerUnit(IPanelComponent panelComponent)
        {
            var root = panelComponent.GetRootVisualElement();
            if (root?.panel == null)
                return 1.0f;

            var runtimePanel = root.panel as BaseRuntimePanel;
            return runtimePanel?.pixelsPerUnit ?? 1.0f;
        }
    }
}
