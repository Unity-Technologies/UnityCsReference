// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor
{
    // Handles rendering selection outlines for individual VisualElements in the SceneView.
    [InitializeOnLoad]
    internal static class VisualElementSceneViewOverlay
    {
        const float k_MinimumDepthForVisibility = 0.01f;
        const float k_PanelOutlineThickness = 0f;    // Default thin line — match the panel gizmo look.
        const float k_ElementOutlineThickness = 2f;  // Selected-element outline reads as a thicker line.

        // User-facing preferences exposed under Preferences → Colors → UI → Scene View. Both are
        // registered as UIPrefColor with the same category so UIPrefColorSection auto-discovers
        // them. The defaults match the legacy values we used to read from "Scene/RectTransform
        // Wire" (white) and Handles.UIColliderHandleColor (uGUI's soft green).
        const string k_SceneViewCategory = "Scene View";
        [NoAutoStaticsCleanup] // editor pref-color, safe to persist
        static readonly UIPrefColor s_PanelSelectionColor =
            new(k_SceneViewCategory, "Panel Selection", new Color(1f, 1f, 1f, 1f));
        [NoAutoStaticsCleanup] // editor pref-color, safe to persist
        static readonly UIPrefColor s_VisualElementSelectionColor =
            new(k_SceneViewCategory, "VisualElement Selection", new Color(145f / 255f, 244f / 255f, 139f / 255f, 210f / 255f));

        static VisualElementSceneViewOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            UICommandQueue.RegisterHandler<RequestFramingCommand>(OnFramingRequested);

            // Undo can rebuild the panel (live-reload after asset revert) and may fire transient
            // selection events; re-collect so the overlay doesn't disappear after Ctrl+Z.
            Undo.undoRedoPerformed += OnSelectionChanged;
        }

        static void OnFramingRequested(in CommandContext context)
        {
            if (context.Status != CommandExecutionStatus.Success)
                return;
            var command = (RequestFramingCommand)context.Command;
            Frame(command.Element, command.OrientToFace);
        }

        // Frames the element (or the current UI selection when null) in the Scene view, optionally
        // turning the camera to face the panel. A VisualElement has no transform of its own, so we
        // frame its world-space footprint, resolving a staging clone back to its live scene instance.
        static void Frame(VisualElement element, bool orientToFace)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return;

            if (element == null)
            {
                FrameCurrentSelection(sceneView, orientToFace);
                return;
            }

            // The document root (uxml node) has no footprint of its own — frame the whole UI by its host.
            if (element is IPanelComponentRootElement { panelComponent: { } panelComponent })
            {
                if (IsAlive(panelComponent))
                    FrameGameObject(sceneView, panelComponent.gameObject, orientToFace);
                return;
            }

            if (TryGetFramableWorldBounds(element, out var bounds, out var hostTransform))
                ApplyFraming(sceneView, bounds, hostTransform, orientToFace);
        }

        static void FrameCurrentSelection(SceneView sceneView, bool orientToFace)
        {
            switch (Selection.activeObject)
            {
                case VisualElementSelection { Element: { } element }:
                    Frame(element, orientToFace);
                    break;
                case VisualTreeAssetSelection { PanelComponent: { } panelComponent } when IsAlive(panelComponent):
                    FrameGameObject(sceneView, panelComponent.gameObject, orientToFace);
                    break;
            }
        }

        static void FrameGameObject(SceneView sceneView, GameObject gameObject, bool orientToFace)
        {
            if (gameObject == null)
                return;
            var bounds = FloorBounds(GameObjectWorldBounds(gameObject));
            ApplyFraming(sceneView, bounds, gameObject.transform, orientToFace);
        }

        static void ApplyFraming(SceneView sceneView, Bounds bounds, Transform hostTransform, bool orientToFace)
        {
            if (orientToFace)
            {
                // Look head-on along the panel's normal (its front faces the host's +forward).
                var rotation = Quaternion.LookRotation(hostTransform.forward, hostTransform.up);
                sceneView.LookAt(bounds.center, rotation, bounds.extents.magnitude, sceneView.orthographic, instant: false);
            }
            else
            {
                sceneView.Frame(bounds, instant: false);
            }
        }

        [NoAutoStaticsCleanup] // per-frame scratch list, safe to persist
        static readonly List<VisualElement> s_SelectedElements = new();
        [NoAutoStaticsCleanup] // per-frame scratch set, safe to persist
        static readonly HashSet<IPanelComponent> s_PanelsDrawnThisFrame = new();

        public static Bounds GetElementWorldBounds(VisualElement element, IPanelComponent panelComponent)
        {
            var localBounds = element.layout;
            if (localBounds.width <= 0 || localBounds.height <= 0)
                return new Bounds(Vector3.zero, Vector3.one);

            var transformOwner = VisualElementToolUtility.FindTransformOwner(panelComponent);
            if (transformOwner == null)
                return new Bounds(Vector3.zero, Vector3.one);

            var bounds = ElementToTransformOwnerLocalBounds(element, transformOwner);
            if (!PanelComponentUtils.IsValidBounds(bounds))
                return new Bounds(Vector3.zero, Vector3.one);

            // Transform to world space
            var localToWorld = transformOwner.gameObject.transform.localToWorldMatrix;
            var worldCenter = localToWorld.MultiplyPoint3x4(bounds.center);

            // World AABB of the (possibly rotated) local box: sum the absolute components of each
            // rotated extent. Using each axis's magnitude alone would ignore how rotation swaps
            // dimensions across world axes and break SceneView framing for rotated panels.
            var extents = bounds.extents;
            var axisX = localToWorld.MultiplyVector(new Vector3(extents.x, 0, 0));
            var axisY = localToWorld.MultiplyVector(new Vector3(0, extents.y, 0));
            var axisZ = localToWorld.MultiplyVector(new Vector3(0, 0, extents.z));
            var worldSize = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)) * 2f;

            return new Bounds(worldCenter, worldSize);
        }

        static void OnSelectionChanged()
        {
            s_SelectedElements.Clear();
            foreach (var obj in Selection.objects)
            {
                if (obj is VisualElementSelection sel && sel.Element != null)
                    s_SelectedElements.Add(sel.Element);
            }
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            if (s_SelectedElements.Count == 0)
                return;

            s_PanelsDrawnThisFrame.Clear();
            var stagePanel = (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.GetAuthoringPanel();

            for (var i = 0; i < s_SelectedElements.Count; i++)
            {
                var element = s_SelectedElements[i];
                if (element == null)
                    continue;

                // Cached ref may have been detached if the panel rebuilt (e.g. after undo),
                // re-resolve via the asset and update the cache so we keep drawing the outline.
                if (element.panel == null)
                {
                    element = VisualElementToolUtility.FindFirstSceneInstanceOfAsset(element.visualElementAsset);
                    if (element == null)
                        continue;
                    s_SelectedElements[i] = element;
                }

                if (stagePanel != null && element.panel == stagePanel)
                {
                    DrawOutlinesForSceneInstancesOfClone(element);
                    continue;
                }

                var panelComponent = FindPanelComponentForElement(element);
                if (panelComponent == null)
                    continue;

                var panelSettings = panelComponent.panelSettings;
                if (panelSettings == null || panelSettings.renderMode != PanelRenderMode.WorldSpace)
                    continue;

                DrawPanelOutlineOnce(panelComponent);
                DrawElementOutline(element, panelComponent);
            }
        }

        // Dedupes panel outlines so multiple selected elements inside one panel don't overdraw it.
        static void DrawPanelOutlineOnce(IPanelComponent panelComponent)
        {
            if (s_PanelsDrawnThisFrame.Add(panelComponent))
                DrawPanelOutline(panelComponent);
        }

        internal static IPanelComponent FindPanelComponentForElement(VisualElement element)
        {
            var rootElement = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
            return rootElement?.panelComponent;
        }

        // Returns world-space, framing-ready bounds for an element, plus the host transform. Resolves a
        // staging clone back to its live world-space scene instance, falls back to the host GameObject
        // when element bounds aren't available, and floors the flat-quad bounds so SceneView.Frame
        // doesn't dive in. Returns false when there is nothing framable in the scene.
        internal static bool TryGetFramableWorldBounds(VisualElement element, out Bounds bounds, out Transform hostTransform)
        {
            bounds = default;
            hostTransform = null;
            if (element == null)
                return false;

            var sceneElement = element;
            IPanelComponent panelComponent;

            var stagePanel = (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.GetAuthoringPanel();
            if (stagePanel != null && element.panel == stagePanel)
            {
                // Editing-stage clone: map back to a live world-space scene instance by asset id.
                if (!TryFindWorldSpaceSceneInstance(element, out sceneElement, out panelComponent))
                    return false;
            }
            else
            {
                panelComponent = FindPanelComponentForElement(element);
            }

            if (!IsAlive(panelComponent))
                return false;

            var resolved = GetElementWorldBounds(sceneElement, panelComponent);
            if (!IsUsableBounds(resolved))
                resolved = GameObjectWorldBounds(panelComponent.gameObject);

            bounds = FloorBounds(resolved);
            hostTransform = panelComponent.gameObject.transform;
            return true;
        }

        static bool TryFindWorldSpaceSceneInstance(VisualElement cloneElement, out VisualElement sceneElement, out IPanelComponent panelComponent)
        {
            sceneElement = null;
            panelComponent = null;

            var asset = cloneElement.visualElementAsset;
            if (asset == null)
                return false;

            foreach (var (pc, match) in VisualElementToolUtility.EnumerateScenePanelInstancesOfAsset(asset))
            {
                sceneElement = match;
                panelComponent = pc;
                return true;
            }
            return false;
        }

        // Gives a flat/thin box volume so SceneView.Frame pulls back to a usable distance instead of
        // diving in on the panel's zero-thickness axis, while staying centred on the target.
        internal static Bounds FloorBounds(Bounds bounds)
        {
            var size = bounds.size;
            var maxDim = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (maxDim <= 0f)
                maxDim = 1f;
            var minExtent = maxDim * 0.5f;
            bounds.size = new Vector3(
                Mathf.Max(size.x, minExtent),
                Mathf.Max(size.y, minExtent),
                Mathf.Max(size.z, minExtent));
            return bounds;
        }

        internal static Bounds GameObjectWorldBounds(GameObject gameObject)
        {
            var hasRenderer = false;
            var bounds = new Bounds(gameObject.transform.position, Vector3.zero);
            foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                if (!hasRenderer)
                {
                    bounds = renderer.bounds;
                    hasRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return bounds;
        }

        // IPanelComponent is an interface, so a non-null managed reference can still wrap a destroyed
        // Unity object (UIDocument/PanelRenderer). Unity's overloaded == detects the destroyed case;
        // a reference-null check on the interface does not. Use this before touching .gameObject etc.
        internal static bool IsAlive(IPanelComponent panelComponent)
            => panelComponent is UnityEngine.Object obj ? obj != null : panelComponent != null;

        static bool IsUsableBounds(Bounds b)
        {
            var c = b.center;
            var s = b.size;
            if (!float.IsFinite(c.x) || !float.IsFinite(c.y) || !float.IsFinite(c.z) ||
                !float.IsFinite(s.x) || !float.IsFinite(s.y) || !float.IsFinite(s.z))
                return false;
            // GetElementWorldBounds' degenerate fallbacks are Bounds(zero, one) and Bounds(zero, zero).
            if (c == Vector3.zero && (s == Vector3.one || s == Vector3.zero))
                return false;
            return true;
        }

        // For a clone element selected via a UXML editing stage, walks every scene panel that
        // could be displaying the same UXML (matched by visualElementAsset.id) and draws outlines
        // for the corresponding scene element in each. Mirrors what the picker does in reverse.
        static void DrawOutlinesForSceneInstancesOfClone(VisualElement cloneElement)
        {
            foreach (var (panelComponent, sceneElement) in VisualElementToolUtility.EnumerateScenePanelInstancesOfAsset(cloneElement.visualElementAsset))
            {
                DrawPanelOutlineOnce(panelComponent);
                DrawElementOutline(sceneElement, panelComponent);
            }
        }

        static void DrawPanelOutline(IPanelComponent panelComponent)
        {
            var root = panelComponent.GetRootVisualElement();
            if (root == null)
                return;

            // When a PanelRenderer is nested with Position.Relative, its parentUI owns the rendering
            // and the GameObject transform. All pivot/ppu/matrix decisions have to flow from that
            // ancestor, not from the nested panelComponent itself.
            var transformOwner = VisualElementToolUtility.FindTransformOwner(panelComponent);
            if (transformOwner == null)
                return;

            Bounds bb;
            if (transformOwner == panelComponent)
            {
                // This panel owns its own transform — use its own pivot source, same as the
                // PanelRenderer inspector gizmo does in the non-nested case.
                bb = PanelComponentUtils.LocalBoundsFromPivotSource(root, panelComponent.pivotReferenceSize);
            }
            else
            {
                // Nested + relative: the panel's root is laid out inside the parent's UI tree.
                // worldBound returns the rect in the owning panel's pixel space, which is exactly
                // what transformOwner's pivot/ppu matrix expects as input.
                var worldBound = root.worldBound;
                bb = new Bounds(
                    new Vector3(worldBound.center.x, worldBound.center.y, 0),
                    new Vector3(worldBound.size.x, worldBound.size.y, 0));
            }

            if (!PanelComponentUtils.IsValidBounds(bb))
                return;

            VisualElementToolUtility.ApplyTransformOwnerMatrix(transformOwner, ref bb);

            var originalColor = Handles.color;

            Handles.color = s_PanelSelectionColor.Color;

            DrawWireCube(bb.center, bb.size, transformOwner.gameObject.transform.localToWorldMatrix, k_PanelOutlineThickness);

            Handles.color = originalColor;
        }

        static void DrawElementOutline(VisualElement element, IPanelComponent panelComponent)
        {
            var rect = element.rect;
            if (rect.width <= 0 || rect.height <= 0)
                return;

            var transformOwner = VisualElementToolUtility.FindTransformOwner(panelComponent);
            if (transformOwner == null)
                return;

            var ownerRoot = transformOwner.GetRootVisualElement();
            if (ownerRoot == null)
                return;

            var elementToOwnerRoot = ownerRoot.worldTransformInverse * element.worldTransform;
            var elementToWorld = VisualElementToolUtility.GetPanelPixelToWorldMatrix(transformOwner) * elementToOwnerRoot;

            var c0 = elementToWorld.MultiplyPoint3x4(new Vector3(0,          0,           0));
            var c1 = elementToWorld.MultiplyPoint3x4(new Vector3(rect.width, 0,           0));
            var c2 = elementToWorld.MultiplyPoint3x4(new Vector3(rect.width, rect.height, 0));
            var c3 = elementToWorld.MultiplyPoint3x4(new Vector3(0,          rect.height, 0));

            var originalColor = Handles.color;

            Handles.color = s_VisualElementSelectionColor.Color;

            DrawOrientedRect(c0, c1, c2, c3, k_ElementOutlineThickness);

            Handles.color = originalColor;
        }

        // 4-edge outline of an oriented rectangle. Corners are in world space already so the
        // active Handles.matrix stays at identity and DrawLine's thickness stays in screen pixels
        // regardless of any scale baked into the source transform.
        static void DrawOrientedRect(Vector3 c0, Vector3 c1, Vector3 c2, Vector3 c3, float thickness)
        {
            Handles.DrawLine(c0, c1, thickness);
            Handles.DrawLine(c1, c2, thickness);
            Handles.DrawLine(c2, c3, thickness);
            Handles.DrawLine(c3, c0, thickness);
        }

        // Pre-transforms the 8 corners through `toWorld` and draws with Handles.matrix at identity.
        // We can't just set Handles.matrix = toWorld and draw in local space: Handles.DrawLine's
        // thickness parameter is interpreted in the active matrix's space, so any non-unit scale
        // on the GameObject would scale the line thickness too — making outlines render thicker
        // on scaled panels. Pre-multiplying the corners and drawing at identity keeps thickness
        // in screen-space pixels regardless of the panel transform.
        static void DrawWireCube(Vector3 center, Vector3 size, in Matrix4x4 toWorld, float thickness)
        {
            var halfSize = size * 0.5f;
            var corners = new Vector3[8];
            corners[0] = toWorld.MultiplyPoint3x4(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
            corners[1] = toWorld.MultiplyPoint3x4(center + new Vector3( halfSize.x, -halfSize.y, -halfSize.z));
            corners[2] = toWorld.MultiplyPoint3x4(center + new Vector3( halfSize.x,  halfSize.y, -halfSize.z));
            corners[3] = toWorld.MultiplyPoint3x4(center + new Vector3(-halfSize.x,  halfSize.y, -halfSize.z));
            corners[4] = toWorld.MultiplyPoint3x4(center + new Vector3(-halfSize.x, -halfSize.y,  halfSize.z));
            corners[5] = toWorld.MultiplyPoint3x4(center + new Vector3( halfSize.x, -halfSize.y,  halfSize.z));
            corners[6] = toWorld.MultiplyPoint3x4(center + new Vector3( halfSize.x,  halfSize.y,  halfSize.z));
            corners[7] = toWorld.MultiplyPoint3x4(center + new Vector3(-halfSize.x,  halfSize.y,  halfSize.z));

            // Bottom face
            Handles.DrawLine(corners[0], corners[1], thickness);
            Handles.DrawLine(corners[1], corners[2], thickness);
            Handles.DrawLine(corners[2], corners[3], thickness);
            Handles.DrawLine(corners[3], corners[0], thickness);

            // Top face
            Handles.DrawLine(corners[4], corners[5], thickness);
            Handles.DrawLine(corners[5], corners[6], thickness);
            Handles.DrawLine(corners[6], corners[7], thickness);
            Handles.DrawLine(corners[7], corners[4], thickness);

            // Vertical edges
            Handles.DrawLine(corners[0], corners[4], thickness);
            Handles.DrawLine(corners[1], corners[5], thickness);
            Handles.DrawLine(corners[2], corners[6], thickness);
            Handles.DrawLine(corners[3], corners[7], thickness);
        }

        // Converts the element's bounds into the transform owner's GameObject local space.
        // element.worldBound is in the owning panel's pixel space, which matches what the transform
        // owner's pivot/ppu matrix consumes — whether the element lives directly in the transform
        // owner's tree or in a nested relative-positioned child.
        static Bounds ElementToTransformOwnerLocalBounds(VisualElement element, IPanelComponent transformOwner)
        {
            var ownerRoot = transformOwner.GetRootVisualElement();
            if (ownerRoot == null)
                return new Bounds(Vector3.zero, Vector3.zero);

            var worldBounds = element.worldBound;
            var boundsInOwnerRoot = ownerRoot.WorldToLocal(worldBounds);

            var bb = new Bounds(
                new Vector3(boundsInOwnerRoot.center.x, boundsInOwnerRoot.center.y, 0),
                new Vector3(boundsInOwnerRoot.size.x, boundsInOwnerRoot.size.y, k_MinimumDepthForVisibility)
            );

            VisualElementToolUtility.ApplyTransformOwnerMatrix(transformOwner, ref bb);
            return bb;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
