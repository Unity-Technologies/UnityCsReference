// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [InitializeOnLoad]
    internal static class UIToolkitSceneViewPicking
    {
        static UIToolkitSceneViewPicking()
        {
            // Register with Unity's picking system
            HandleUtility.RegisterRenderPickingCallback(RenderForPicking);
        }

        static Material s_PickingMaterial;
        static Mesh s_QuadMesh;
        static List<IPanelComponent> s_PickedPanels = new();
        static HashSet<Object> s_SystemIgnoreSet; // System ignore/filter set for GetAllOverlapping
        static RenderPickingType s_RenderPickingType; // Type determines how to interpret the ignore set

        const float k_PickingQuadCameraOffset = 0.001f;

        static RenderPickingResult RenderForPicking(in RenderPickingArgs args)
        {
            // Per-element picking only makes sense when the user has opted into the hierarchy
            // integration; otherwise the SceneView should just select the panel GameObject as a whole.
            if (!UIToolkitAuthoringSettings.EnableInSceneUIAuthoring)
                return default;

            // Store system ignore/filter set and type for use in resolve callback
            // The resolve callback doesn't receive the args, so we need to store it here
            s_SystemIgnoreSet = args.renderObjectSetInternal as HashSet<Object>;
            s_RenderPickingType = args.renderPickingType;

            // Find all world-space panel components
            s_PickedPanels.Clear();
            var panelComponents = new List<IPanelComponent>();
            panelComponents.AddRange(Object.FindObjectsByType<UIDocument>());
            panelComponents.AddRange(Object.FindObjectsByType<PanelRenderer>());

            foreach (var component in panelComponents)
            {
                if (!component.gameObject.activeInHierarchy || !component.GetComponentEnabled())
                    continue;

                var panelSettings = component.panelSettings;
                if (panelSettings == null || panelSettings.renderMode != PanelRenderMode.WorldSpace)
                    continue;

                // Skip nested panels whose transform is controlled by an ancestor (Position.Relative).
                // The transform-owning ancestor's quad already covers the area where this panel
                // renders, and PickAll3D on the ancestor walks the shared tree, so cycling reaches
                // both the ancestor's and the nested panel's elements.
                if (!PanelComponentUtils.IsTransformControlledByGameObject(component))
                    continue;

                s_PickedPanels.Add(component);
            }

            if (s_PickedPanels.Count == 0)
                return default;

            // Render each panel's bounds as a pickable quad,
            // Unity will fill the picking buffer based on what we render here
            int pickingIndex = args.pickingIndex;
            foreach (var component in s_PickedPanels)
            {
                RenderPanelBoundsForPicking(component, pickingIndex);
                ++pickingIndex;
            }

            return new RenderPickingResult(s_PickedPanels.Count, ResolvePickedElementByRaycast);
        }

        static void RenderPanelBoundsForPicking(IPanelComponent component, int pickingID)
        {
            if (s_PickingMaterial == null)
            {
                var shader = EditorGUIUtility.LoadRequired("Shaders/UIElements/UIToolkitPickingQuad.shader") as Shader;
                if (shader == null)
                    return;
                s_PickingMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            if (s_QuadMesh == null)
            {
                s_QuadMesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                s_QuadMesh.vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, 0),
                    new Vector3(0.5f, -0.5f, 0),
                    new Vector3(0.5f, 0.5f, 0),
                    new Vector3(-0.5f, 0.5f, 0)
                };
                s_QuadMesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            }

            // Get the picking bounds using the same method as the renderer
            var root = component.GetRootVisualElement();
            if (root == null)
                return;

            var bounds = WorldSpaceInput.GetPicking3DWorldBounds(root);
            if (!float.IsFinite(bounds.size.x) || !float.IsFinite(bounds.size.y) ||
                bounds.size.x <= 0 || bounds.size.y <= 0)
                return;

            // Transform the bounds to world space
            var gameObjectTransform = component.gameObject.transform;
            var worldCenter = gameObjectTransform.TransformPoint(bounds.center);

            // Nudge the quad slightly toward the camera so we always win the ZTest LEqual against
            // the coplanar panel mesh that Internal-UIRDefault's SceneSelectionPass writes into the
            // picking buffer.
            var camera = Camera.current;
            if (camera != null)
                worldCenter -= camera.transform.forward * k_PickingQuadCameraOffset;

            var scale = gameObjectTransform.lossyScale;
            var size = bounds.size;
            var scaledSize = new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);

            // The bounds are already in GameObject local space, so we need to transform them
            // Build the final transform matrix
            var matrix = Matrix4x4.TRS(
                worldCenter,
                gameObjectTransform.rotation,
                scaledSize
            );

            // Set picking ID and render the quad
            s_PickingMaterial.SetVector("_SelectionId", HandleUtility.EncodeSelectionId(pickingID));
            s_PickingMaterial.SetPass(0);
            Graphics.DrawMeshNow(s_QuadMesh, matrix);
        }

        static Object ResolvePickedElementByRaycast(int localPickingIndex, Vector3 worldPos, float depth)
        {
            // localPickingIndex tells us which panel was clicked
            if (localPickingIndex < 0 || localPickingIndex >= s_PickedPanels.Count)
                return null;

            var clickedPanel = s_PickedPanels[localPickingIndex];

            // Camera.current is set by Unity's picking pass to the SceneView camera that's actually doing the pick
            var camera = Camera.current;
            if (camera == null)
                return null;

            // Picking rays are parallel for an orthographic camera (the camera position doesn't matter, every pixel
            // projects along view forward) and convergent at the camera position for a perspective one.
            Ray ray;
            if (camera.orthographic)
            {
                var dir = camera.transform.forward;
                ray = new Ray(worldPos - dir * camera.farClipPlane, dir);
            }
            else
            {
                var origin = camera.transform.position;
                ray = new Ray(origin, (worldPos - origin).normalized);
            }

            // Get all elements at this position in draw order
            var candidates = new List<VisualElement>();
            WorldSpaceInput.PickAll3D(clickedPanel, ray, candidates);

            // While a UXML editing stage is active, the SceneView still picks against the
            // scene-original PanelRenderers but the hierarchy / VisualElementSelections live on
            // the stage's cloned tree. Redirect each picked scene element to its clone so the
            // returned selection matches what the user sees in the hierarchy.
            var stagePanel = (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.GetAuthoringPanel();

            // Find the first element that passes the filter/ignore check
            foreach (var candidate in candidates)
            {
                var selectionTarget = stagePanel != null
                    ? candidate.FindCorrespondingStageClone(stagePanel) ?? candidate
                    : candidate;

                var selectionObject = selectionTarget.GetSelectionObject<VisualElementSelection>();
                if (selectionObject == null)
                    continue;

                // Check against system filter/ignore set
                if (s_SystemIgnoreSet != null && s_SystemIgnoreSet.Count > 0)
                {
                    bool isInSet = s_SystemIgnoreSet.Contains(selectionObject);
                    bool shouldRender = s_RenderPickingType == RenderPickingType.RenderFromFilterSet ? isInSet : !isInSet;

                    if (!shouldRender)
                        continue;
                }

                // Found an element that passes the filter
                var selectionName = selectionObject.Element.GetType().Name;
                if (!string.IsNullOrEmpty(selectionTarget.name))
                    selectionObject.name = $"{selectionName} ({selectionTarget.name})";
                else
                    selectionObject.name = selectionName;

                return selectionObject;
            }

            // No elements passed the filter? Return the GameObject (if it passes the filter, otherwise null)
            var gameObject = clickedPanel.gameObject;
            if (s_SystemIgnoreSet != null && s_SystemIgnoreSet.Count > 0)
            {
                bool isInSet = s_SystemIgnoreSet.Contains(gameObject);
                bool shouldRender = s_RenderPickingType == RenderPickingType.RenderFromFilterSet ? isInSet : !isInSet;
                if (!shouldRender)
                    return null;

                return gameObject;
            }

            return null;
        }

    }
}
