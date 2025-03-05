// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Bindings;
using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace UnityEngine.UIElements;

// A class containing all 3-D input logic at the RuntimePanel level
internal static class WorldSpaceInput
{
    // We don't expose the Pick3D methods in a public class until all the pieces are in place

    /// <summary>
    /// Pick the closest element (in drawing order) from a document's hierarchy that intersects the given ray.
    /// </summary>
    /// <param name="document">The UI Document to pick from.</param>
    /// <param name="worldRay">A ray specified in absolute coordinates.</param>
    /// <returns>The closest pickable element that intersects the ray if any, or null if there are none.</returns>
    public static VisualElement Pick3D(UIDocument document, Ray worldRay)
    {
        var documentRay = document.transform.worldToLocalMatrix.TransformRay(worldRay);
        return Pick_Internal(document, documentRay);
    }

    /// <summary>
    /// Pick all elements from a document's hierarchy that intersect the given ray.
    /// Results are appended in drawing order.
    /// </summary>
    /// <param name="document">The UI Document to pick from.</param>
    /// <param name="worldRay">A ray specified in the absolute coordinates.</param>
    /// <param name="outResults">All elements that intersect the ray, in draw order from closest to farthest.</param>
    public static void PickAll3D(UIDocument document, Ray worldRay, List<VisualElement> outResults)
    {
        var documentRay = document.transform.worldToLocalMatrix.TransformRay(worldRay);
        Pick_Internal(document, documentRay, outResults);
    }

    /// <summary>
    /// Pick the closest element (in drawing order) from a document's hierarchy that intersects the given ray.
    /// </summary>
    /// <param name="document">The UI Document to pick from.</param>
    /// <param name="worldRay">A ray specified in absolute coordinates.</param>
    /// <param name="distance">The element's distance along the ray, or positive infinity if there is none.</param>
    /// <returns>The closest pickable element that intersects the ray if any, or null if there are none.</returns>
    public static VisualElement Pick3D(UIDocument document, Ray worldRay, out float distance)
    {
        var documentRay = document.transform.worldToLocalMatrix.TransformRay(worldRay);
        var pickedElement = Pick_Internal(document, documentRay);

        if (pickedElement != null)
        {
            pickedElement.IntersectWorldRay(documentRay, out var distanceWithinDocument, out _);
            var documentPoint = documentRay.origin + documentRay.direction * distanceWithinDocument;
            var worldPoint = document.transform.TransformPoint(documentPoint);
            distance = Vector3.Distance(worldRay.origin, worldPoint);
        }
        else
        {
            distance = Mathf.Infinity;
        }

        return pickedElement;
    }

    /// <summary>
    /// The result of a Picking operation.
    /// </summary>
    public struct PickResult
    {
        /// <summary>
        /// The result of a Picking operation that intersected no element.
        /// </summary>
        public static readonly PickResult Empty = new PickResult { distance = Mathf.Infinity };

        /// <summary>
        /// The document containing the picked element.
        /// </summary>
        public UIDocument document;

        /// <summary>
        /// A VisualElement intersected by the ray from the Picking operation.
        /// </summary>
        public VisualElement pickedElement;

        /// <summary>
        /// The distance between the Ray origin and the world point intersected by the Picking operation.
        /// </summary>
        public float distance;

        // Assume elements come from distinct documents. DrawOrder within document isn't guaranteed by this comparison.
        internal int CompareDrawOrder([NotNull] UIDocument otherDocument, float otherDistance)
        {
            if (document == null)
                return 1;

            var panelSortingOrder =
                document.panelSettings.sortingOrder.CompareTo(otherDocument.panelSettings.sortingOrder);
            if (panelSortingOrder != 0)
                return panelSortingOrder;

            var documentSortingOrder = document.sortingOrder.CompareTo(otherDocument.sortingOrder);
            if (documentSortingOrder != 0)
                return documentSortingOrder;

            return distance.CompareTo(otherDistance);
        }
    }

    /// <summary>
    /// Pick the closest element that intersects the given ray, if any, from the closest UIDocument hierarchy.
    /// </summary>
    /// <param name="worldRay">A ray specified in world coordinates.</param>
    /// <param name="maxDistance">The length of the ray, or positive infinity if unspecified.</param>
    /// <param name="layerMask">The combination of Physics layers to consider during the raycast.</param>
    /// <returns>A document with its closest element that intersects the ray if any, or PickResult.Empty if there are none.</returns>
    /// <remarks>If there is an object in the UI layer that blocks the UIDocument, the behavior of this method is unspecified.</remarks>
    public static PickResult PickDocument3D(Ray worldRay, float maxDistance = Mathf.Infinity,
        int layerMask = Physics.DefaultRaycastLayers)
    {
        const int kMaxIterations = 100;
        var bestSoFar = new PickResult { distance = Mathf.Infinity };
        var activeDistance = 0f;
        var activeRay = worldRay;

        int it = 0;
        while (activeDistance < maxDistance)
        {
            if (++it > kMaxIterations)
            {
                Debug.LogWarning("PickDocument3D exceeded iteration limit of " + kMaxIterations +
                                 ". Returned values may be incorrect.");
                break;
            }

            // Find the best candidate along the ray. Assumes all documents have physics colliders.
            // We can't stop after the first hit because we might hit elements closer in documents whose bounding box
            // is smaller and thus behind in the Physics world.

            // Stop if we can't hit anything anymore. Assume there can't be two colliders at exactly the same distance.
            if (!Physics.Raycast(activeRay, out var hit, maxDistance - activeDistance, layerMask,
                    QueryTriggerInteraction.Collide))
                break;

            var distance = hit.distance + activeDistance;

            // Non-Document objects on the same layer can block raycasts.
            // If we hit a real-world object, we can conclude our search with the best candidate so far.
            var document = hit.transform.GetComponentInParent<UIDocument>(includeInactive: true);
            if (document == null)
            {
                if (distance < bestSoFar.distance)
                    bestSoFar = new PickResult { distance = distance };
                break;
            }

            // Add a small epsilon to avoid hitting the same collider again
            const float kEpsilon = 0.001f;
            var rayIncrement = hit.distance + kEpsilon;
            activeRay.origin += activeRay.direction * rayIncrement;
            activeDistance += rayIncrement;

            // Skip inactive or invalid documents (treat them the same as empty documents).
            if (document.containerPanel == null)
                continue;

            // When we've hit a Document, reduce max distance to match the back of the document's bounding box.
            // We can't use hit.collider.bounds.size.magnitude because we allow multiple colliders for the same doc.
            var bb = GetPicking3DLocalBounds(document.rootVisualElement);
            ref var documentWt = ref document.rootVisualElement.worldTransformRef;
            Vector3 documentCenter = documentWt.MultiplyPoint3x4(bb.center);
            Vector3 documentDiagonal = documentWt.MultiplyVector(bb.size);
            var transformWt = document.transform.localToWorldMatrix;
            Vector3 worldCenter = documentWt.MultiplyPoint3x4(documentCenter);
            Vector3 worldDiagonal = transformWt.MultiplyVector(documentDiagonal);
            float distanceToBackOfDocument =
                Vector3.Distance(worldRay.origin, worldCenter) + worldDiagonal.magnitude / 2 + kEpsilon;
            maxDistance = Mathf.Min(maxDistance, activeDistance + distanceToBackOfDocument);

            // Early out if no element inside the box even has the potential to beat the best so far.
            // Raycast hit distance is always less or equal to pickedElement's distance.
            if (bestSoFar.CompareDrawOrder(document, distance) <= 0)
                continue;

            // Pick closest element by draw order regardless of max distance. Distance is filtered at the next step.
            // This means an element closer than max distance could be rejected because it has another element
            // drawn on top of it, but that other element could then also be rejected because it has a distance
            // larger than the max distance. In that case, no element is accepted because we don't want to interact
            // with an element that's hidden behind another one that's interactable.
            var pickedElement = Pick3D(document, worldRay, out distance);

            // Compare again with the real distance from the pickedElement
            if (pickedElement != null && distance <= maxDistance && bestSoFar.CompareDrawOrder(document, distance) > 0)
            {
                // Update the result but don't fast-forward the activeRay!
                bestSoFar = new PickResult { pickedElement = pickedElement, document = document, distance = distance };
            }
        }

        return bestSoFar;
    }

    internal static VisualElement Pick_Internal(UIDocument document, Ray documentRay,
        List<VisualElement> outResults = null)
    {
        document.containerPanel.ValidateLayout();

        var root = document.rootVisualElement;
        var ray = root.WorldToLocal(documentRay);

        return PerformPick(root, ray, null);
    }

    // Used by tests
    [VisibleToOtherModules("Assembly-CSharp-testable")]
    internal static VisualElement PerformPick(VisualElement root, Ray ray, List<VisualElement> outResults)
    {
        return root.needs3DBounds
            ? PerformPick3D(root, ray, outResults)
            : PerformPick2D(root, ray, outResults);
    }

    private static VisualElement PerformPick2D(VisualElement root, Ray ray, List<VisualElement> outResults)
    {
        root.IntersectLocalRay(ray, out var point);

        // Don't use Panel.PerformPick(root, root.LocalToWorld(point), outResults) because it only supports 2-D points.
        return PerformPick2D_LocalPoint(root, point, outResults);
    }

    private static VisualElement PerformPick3D(VisualElement root, Ray ray, List<VisualElement> outResults)
    {
        // Skip picking for elements with display: none
        if (root.resolvedStyle.display == DisplayStyle.None)
            return default;

        if (root.pickingMode == PickingMode.Ignore && root.hierarchy.childCount == 0)
            return default;

        var bb = GetPicking3DLocalBounds(root);
        if (!bb.IntersectRay(ray))
        {
            return default;
        }

        // Problem here: every time we pick, we need to do that expensive transformation.
        // The default Contains() compares with rect, while we could cache the rect in world space (transform 2 points, 4 if there is rotation) and be done
        // here we have to transform 1 point at every call.
        // Now since this is a virtual, we can't just start to call it with global pos... we could break client code.
        // EdgeControl and port connectors in GraphView overload this.

        bool containsPoint = root.IntersectLocalRay(ray, out var point) &&
                             root.ContainsPoint(point);
        // we only skip children in the case we visually clip them
        if (!containsPoint && root.ShouldClip())
        {
            return default;
        }

        VisualElement returnedChild = default;

        // Depth first in reverse order, do children
        var cCount = root.hierarchy.childCount;
        for (int i = cCount - 1; i >= 0; i--)
        {
            var child = root.hierarchy[i];
            var childRay = root.ChangeCoordinatesTo(child, ray);
            var result = PerformPick(child, childRay, outResults);
            if (returnedChild == null && result != null)
            {
                if (outResults == null)
                    return result;
                returnedChild = result;
            }
        }

        if (root.visible && root.pickingMode == PickingMode.Position && containsPoint)
        {
            outResults?.Add(root);
            if (returnedChild == null)
                returnedChild = root;
        }

        return returnedChild;
    }

    private static VisualElement PerformPick2D_LocalPoint(VisualElement root, Vector3 localPoint,
        List<VisualElement> picked = null)
    {
        // Skip picking for elements with display: none
        if (root.resolvedStyle.display == DisplayStyle.None)
            return null;

        if (root.pickingMode == PickingMode.Ignore && root.hierarchy.childCount == 0)
        {
            return null;
        }

        if (!root.boundingBox.Contains(localPoint))
        {
            return null;
        }

        bool containsPoint = root.ContainsPoint(localPoint);
        // we only skip children in the case we visually clip them
        if (!containsPoint && root.ShouldClip())
        {
            return null;
        }

        VisualElement returnedChild = null;
        // Depth first in reverse order, do children
        var cCount = root.hierarchy.childCount;
        for (int i = cCount - 1; i >= 0; i--)
        {
            var child = root.hierarchy[i];
            var childPoint = root.ChangeCoordinatesTo(child, localPoint);
            var result = PerformPick2D_LocalPoint(child, childPoint, picked);
            if (returnedChild == null && result != null)
            {
                if (picked == null)
                {
                    return result;
                }

                returnedChild = result;
            }
        }

        if (root.visible && root.pickingMode == PickingMode.Position && containsPoint)
        {
            picked?.Add(root);
            if (returnedChild == null)
                returnedChild = root;
        }

        return returnedChild;
    }

    public static Bounds GetPicking3DWorldBounds(VisualElement ve)
    {
        var bb = GetPicking3DLocalBounds(ve);
        VisualElement.TransformAlignedBounds(ref ve.worldTransformRef, ref bb);
        return bb;
    }

    public static Bounds GetPicking3DLocalBounds(VisualElement ve)
    {
        if (ve.needs3DBounds)
            return ve.localBounds3D;

        Rect bb = ve.boundingBox;
        return new Bounds(bb.center, bb.size);
    }
}
