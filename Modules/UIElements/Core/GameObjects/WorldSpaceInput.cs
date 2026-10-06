// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements;

// A class containing all 3-D input logic at the RuntimePanel level
[VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
internal static class WorldSpaceInput
{
    private struct ChildPickEntry
    {
        public VisualElement child;
        public int zIndex;
        public int seq;
    }

    [ThreadStatic]
    [NoAutoStaticsCleanup] // emptied by the end of every pick
    private static ChildPickEntry[] s_PickScratch;

    [ThreadStatic]
    [NoAutoStaticsCleanup] // reset to the frame base by the end of every pick
    private static int s_PickScratchCount;

    [NoAutoStaticsCleanup] // static method reference; safe to persist
    private static readonly RefComparison<ChildPickEntry> s_CompareEntries = CompareEntries;

    // We don't expose the Pick3D methods in a public class until all the pieces are in place

    /// <summary>
    /// Pick the closest element (in drawing order) from a panel component's hierarchy that intersects the given ray.
    /// </summary>
    /// <param name="panelComponent">The panel component to pick from.</param>
    /// <param name="worldRay">A ray specified in absolute coordinates.</param>
    /// <returns>The closest pickable element that intersects the ray if any, or null if there are none.</returns>
    public static VisualElement Pick3D(IPanelComponent panelComponent, Ray worldRay)
    {
        var documentRay = panelComponent.gameObject.transform.worldToLocalMatrix.TransformRay(worldRay);
        return Pick_Internal(panelComponent, documentRay);
    }

    /// <summary>
    /// Pick all elements from a document's hierarchy that intersect the given ray.
    /// Results are appended in drawing order.
    /// </summary>
    /// <param name="panelComponent">The panel component to pick from.</param>
    /// <param name="worldRay">A ray specified in the absolute coordinates.</param>
    /// <param name="outResults">All elements that intersect the ray, in draw order from closest to farthest.</param>
    public static void PickAll3D(IPanelComponent panelComponent, Ray worldRay, List<VisualElement> outResults)
    {
        var documentRay = panelComponent.gameObject.transform.worldToLocalMatrix.TransformRay(worldRay);
        Pick_Internal(panelComponent, documentRay, outResults);
    }

    /// <summary>
    /// Pick the closest element (in drawing order) from a panel component's hierarchy that intersects the given ray.
    /// </summary>
    /// <param name="panelComponent">The panel component to pick from.</param>
    /// <param name="worldRay">A ray specified in absolute coordinates.</param>
    /// <param name="distance">The element's distance along the ray, or positive infinity if there is none.</param>
    /// <returns>The closest pickable element that intersects the ray if any, or null if there are none.</returns>
    public static VisualElement Pick3D(IPanelComponent panelComponent, Ray worldRay, out float distance)
    {
        var documentRay = panelComponent.gameObject.transform.worldToLocalMatrix.TransformRay(worldRay);
        var pickedElement = Pick_Internal(panelComponent, documentRay);

        if (pickedElement != null)
        {
            pickedElement.IntersectWorldRay(documentRay, out var distanceWithinDocument, out _);
            var documentPoint = documentRay.origin + documentRay.direction * distanceWithinDocument;
            var worldPoint = panelComponent.gameObject.transform.TransformPoint(documentPoint);
            distance = Vector3.Distance(worldRay.origin, worldPoint);
        }
        else
        {
            distance = Mathf.Infinity;
        }

        return pickedElement;
    }

    /// <summary>
    /// Pick the closest element (in drawing order) from a document's hierarchy that intersects the given ray.
    /// </summary>
    /// <param name="panelComponent">The panel component to pick from.</param>
    /// <param name="worldRay">A ray specified in absolute coordinates.</param>
    /// <param name="pickResult">The result of the ray intersection.</param>
    /// <returns>True if a pickable element intersects the ray, false otherwise.</returns>
    public static bool Pick3D(IPanelComponent panelComponent, Ray worldRay, out PickResult pickResult)
    {
        var documentRay = panelComponent.gameObject.transform.worldToLocalMatrix.TransformRay(worldRay);
        var pickedElement = Pick_Internal(panelComponent, documentRay);

        if (pickedElement == null)
        {
            pickResult = PickResult.Empty;
            return false;
        }

        pickedElement.IntersectWorldRay(documentRay, out var distanceWithinDocument, out _);
        var documentPoint = documentRay.origin + documentRay.direction * distanceWithinDocument;
        var worldPoint = panelComponent.gameObject.transform.TransformPoint(documentPoint);
        var distance = Vector3.Distance(worldRay.origin, worldPoint);

        pickResult = new PickResult
        {
            panelComponent = panelComponent,
            pickedElement = pickedElement,
            distance = distance
        };
        pickResult.ComputeCollisionData(worldRay);
        return true;
    }

    /// <summary>
    /// Pick the closest element (in drawing order) from a panel's hierarchy that intersects the given ray.
    /// </summary>
    /// <param name="panel">The panel whose root to start the pick from.</param>
    /// <param name="panelRay">A ray specified in panel world coordinates.</param>
    /// <returns>The closest pickable element that intersects the ray if any, or null if there are none.</returns>
    public static VisualElement Pick3D(IPanel panel, Ray panelRay, List<VisualElement> outResults = null)
    {
        return Pick3D(panel.visualTree, panel.visualTree.WorldToLocal(panelRay), outResults);
    }

    /// <summary>
    /// Pick the closest element (in drawing order) from a subtree hierarchy that intersects the given ray.
    /// </summary>
    /// <param name="rootVisualElement">The root element to start the pick from.</param>
    /// <param name="localRay">A ray specified in rootVisualElement coordinates.</param>
    /// <returns>The closest pickable element that intersects the ray if any, or null if there are none.</returns>
    public static VisualElement Pick3D(VisualElement rootVisualElement, Ray localRay, List<VisualElement> outResults = null)
    {
        rootVisualElement.elementPanel.ValidateLayout();
        return PerformPick(rootVisualElement, localRay, outResults);
    }

    /// <summary>
    /// Finds the intersection point between a ray and the given element.
    /// </summary>
    /// <remarks>The @@element@@ has to be parented to a document.</remarks>
    /// <param name="element">The element to intersect with.</param>
    /// <param name="worldRay">A ray specified in absolute coordinates.</param>
    /// <param name="pickResult">The result of the ray intersection.</param>
    /// <param name="acceptOutside">Should the intersection skip element boundary checks?</param>
    /// <returns>True if the ray intersects the element.</returns>
    public static bool PickElement3D(VisualElement element, Ray worldRay, out PickResult pickResult, bool acceptOutside = false)
    {
        var panelComponent = element.FindRootPanelComponent();
        if (panelComponent == null)
            throw new ArgumentException("Element must be part of a UI Document.");

        var documentRay = panelComponent.gameObject.transform.worldToLocalMatrix.TransformRay(worldRay);
        if (!element.IntersectWorldRay(documentRay, out var distance, out _) && (!acceptOutside || !(distance > 0)))
        {
            pickResult = PickResult.Empty;
            return false;
        }

        pickResult = new PickResult
        {
            panelComponent = panelComponent,
            pickedElement = element,
            distance = distance
        };
        pickResult.ComputeCollisionData(worldRay);
        return true;
    }

    /// <summary>
    /// The result of a Picking operation.
    /// </summary>
    public struct PickResult
    {
        /// <summary>
        /// The result of a Picking operation that intersected no element.
        /// </summary>
        [NoAutoStaticsCleanup]
        public static readonly PickResult Empty = new PickResult { distance = Mathf.Infinity };

        /// <summary>
        /// The collider that ended the pick process.
        /// </summary>
        /// <remarks>
        /// Doesn't always have a UIDocument component associated with it.
        /// </remarks>
        public Collider collider;

        /// <summary>
        /// The panel component containing the picked element.
        /// </summary>
        public IPanelComponent panelComponent;

        /// <summary>
        /// A VisualElement intersected by the ray from the Picking operation.
        /// </summary>
        /// <remarks>
        /// If @@document@@ is null, then @@pickedElement@@ is also null.
        /// However, with pointer capture, it's possible that @@pickedElement@@ is null but not @@document@@.
        /// </remarks>
        public VisualElement pickedElement;

        /// <summary>
        /// The distance between the Ray origin and the world point intersected by the Picking operation.
        /// </summary>
        public float distance;

        /// <summary>
        /// The raycast hit surface normal, expressed in the GameObject world coordinate system.
        /// </summary>
        public Vector3 normal;

        /// <summary>
        /// The intersected point, expressed in the GameObject world coordinate system.
        /// </summary>
        public Vector3 point;

        /// <summary>
        /// The intersected point in the @@pickedElement@@ local coordinate system.
        /// </summary>
        /// <remarks>
        /// If @@pickedElement@@ is null, this is the same as @@point@@..
        /// </remarks>
        public Vector3 localPoint;

        internal void ComputeCollisionData(Ray ray)
        {
            point = ray.origin + ray.direction * distance;

            if (panelComponent != null && pickedElement != null)
            {
                localPoint = pickedElement.worldTransformInverse.MultiplyPoint3x4(
                             panelComponent.gameObject.transform.InverseTransformPoint(point));
                normal = panelComponent.gameObject.transform.TransformDirection(
                         pickedElement.worldTransformRef.MultiplyVector(Vector3.forward));
            }
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
            var panelComponent = hit.collider.GetComponentInParent<IPanelComponent>(includeInactive: true);
            if (panelComponent == null)
            {
                if (distance < bestSoFar.distance)
                    bestSoFar = new PickResult { distance = distance, collider = hit.collider, normal = hit.normal, point = hit.point, localPoint = hit.point };
                break;
            }

            // Add a small epsilon to avoid hitting the same collider again
            const float kEpsilon = 0.001f;
            var rayIncrement = hit.distance + kEpsilon;
            activeRay.origin += activeRay.direction * rayIncrement;
            activeDistance += rayIncrement;

            // Skip inactive or invalid documents (treat them the same as empty documents).
            if (panelComponent?.GetContainerPanel() == null)
                continue;

            // When we've hit a Document, reduce max distance to match the back of the document's bounding box.
            // We can't use hit.collider.bounds.size.magnitude because we allow multiple colliders for the same doc.
            var root = panelComponent.GetRootVisualElement();
            var bb = GetPicking3DLocalBounds(root);
            ref var documentWt = ref root.worldTransformRef;
            Vector3 documentCenter = documentWt.MultiplyPoint3x4(bb.center);
            Vector3 documentDiagonal = documentWt.MultiplyVector(bb.size);
            var transformWt = panelComponent.gameObject.transform.localToWorldMatrix;
            Vector3 worldCenter = documentWt.MultiplyPoint3x4(documentCenter);
            Vector3 worldDiagonal = transformWt.MultiplyVector(documentDiagonal);
            float distanceToBackOfDocument =
                Vector3.Distance(worldRay.origin, worldCenter) + worldDiagonal.magnitude / 2 + kEpsilon;
            maxDistance = Mathf.Min(maxDistance, activeDistance + distanceToBackOfDocument);

            // Early out if no element inside the box even has the potential to beat the best so far.
            // Raycast hit distance is always less or equal to pickedElement's distance.
            if (distance >= bestSoFar.distance)
                continue;

            // Pick closest element by draw order regardless of max distance. Distance is filtered at the next step.
            // This means an element closer than max distance could be rejected because it has another element
            // drawn on top of it, but that other element could then also be rejected because it has a distance
            // larger than the max distance. In that case, no element is accepted because we don't want to interact
            // with an element that's hidden behind another one that's interactable.
            var pickedElement = Pick3D(panelComponent, worldRay, out distance);

            // Compare again with the real distance from the pickedElement
            if (pickedElement != null && distance <= maxDistance && distance < bestSoFar.distance)
            {
                // Update the result but don't fast-forward the activeRay!
                bestSoFar = new PickResult {
                    collider = hit.collider,
                    pickedElement = pickedElement,
                    panelComponent = panelComponent,
                    distance = distance
                };
                bestSoFar.ComputeCollisionData(worldRay);
            }
        }

        return bestSoFar;
    }

    internal static VisualElement Pick_Internal(IPanelComponent panelComponent, Ray documentRay,
        List<VisualElement> outResults = null)
    {
        var containerPanel = (Panel)panelComponent.GetContainerPanel();
        containerPanel.ValidateLayout();

        var root = panelComponent.GetRootVisualElement();
        var ray = root.WorldToLocal(documentRay);

        return PerformPick(root, ray, outResults);
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
        if (root.elementPanel == null)
            return null;

        // Flat path keeps its historical behavior: IntersectLocalRay returns the Z=0-plane point (in or out of
        // rect) and the native pick filters by layout.
        root.IntersectLocalRay(ray, out var point);
        return PerformPickByPoint(root, point, outResults);
    }

    private static unsafe VisualElement PerformPickByPoint(VisualElement root, Vector3 point, List<VisualElement> outResults)
    {
        using var buffer = outResults != null ? UnmanagedHandleBuffer.CreateTemporary() : UnmanagedHandleBuffer.None();

        // Native implementation is 2-3 times faster, so we use it if we can.
        var handle = NativeTransformUtils.PerformPick(root.layoutNode.Handle, point, false, &buffer);
        if (handle.IsUndefined)
            return null;

        var result = root.elementPanel.GetMemberElementFromHandle(handle);

        if (outResults != null)
        {
            outResults.Add(result);
            foreach (var nextHandle in buffer.ReadOnlySpan.Slice(1))
            {
                outResults.Add(root.elementPanel.GetMemberElementFromHandle(nextHandle));
            }
        }

        return result;
    }

    private static VisualElement PerformPick3D(VisualElement root, Ray ray, List<VisualElement> outResults)
    {
        // Skip picking for elements with display: none
        if (root.resolvedStyle.display == DisplayStyle.None)
            return default;

        if (root.pickingMode == PickingMode.Ignore && root.hierarchy.childCount == 0)
            return default;

        // Curved UI: curved elements are handled by the normal recursion below - IntersectLocalRay is
        // curvature-aware, so each element is tested against its OWN composed bend chain. An earlier optimization
        // intersected only the outermost curved root's surface once and resolved the target from the flat layout
        // at the un-bent point, but that is only correct when the whole subtree lies on that single surface: a
        // NESTED curved element places its content on the composed (outer-then-inner) surface, which the
        // single-surface delegation mis-projects - measured as whole-tile mispicks where the inner bend's sag is
        // largest. Per-element testing is exact for any chain depth (and measured identical on single chains).
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

        bool intersects = root.IntersectLocalRay(ray, out var point);
        bool containsPoint = intersects && root.ContainsPoint(point);
        // we only skip children in the case we visually clip them
        if (!containsPoint && root.ShouldClip())
        {
            return default;
        }

        VisualElement returnedChild = default;
        bool rootPicked = false;

        var cCount = root.hierarchy.childCount;
        bool needsZIndexSort = cCount > 0 &&
            (root.transformFlags & VisualElementTransformFlags.MayHaveZIndexedChildren) != 0;

        if (needsZIndexSort)
        {
            if (PickChildrenInZIndexOrder(root, ray, containsPoint, outResults, ref returnedChild, ref rootPicked))
                return returnedChild;
        }
        else
        {
            // Depth first in reverse order, do children
            for (int i = cCount - 1; i >= 0; i--)
            {
                if (PickChild(root, root.hierarchy[i], ray, outResults, ref returnedChild))
                    return returnedChild;
            }
        }

        if (!rootPicked)
            PickRoot(root, containsPoint, outResults, ref returnedChild);

        return returnedChild;
    }

    // Returns true when the pick is resolved and the root no longer needs to be tested.
    private static bool PickChildrenInZIndexOrder(VisualElement root, Ray ray, bool containsPoint,
        List<VisualElement> outResults, ref VisualElement returnedChild, ref bool rootPicked)
    {
        s_PickScratch ??= new ChildPickEntry[16];
        int frameBase = s_PickScratchCount;
        try
        {
            int minZIndex = int.MaxValue;
            int maxZIndex = int.MinValue;

            // Reverse order so equal z-index falls back to reverse paint order.
            int seq = 0;
            for (int i = root.hierarchy.childCount - 1; i >= 0; i--)
            {
                var child = root.hierarchy[i];
                int z = child.computedStyle.zIndex;
                if (z == int.MinValue)
                    z = 0;
                AddScratchEntry(new ChildPickEntry { child = child, zIndex = z, seq = seq++ });
                if (z < minZIndex)
                    minZIndex = z;
                if (z > maxZIndex)
                    maxZIndex = z;
            }
            int frameEnd = s_PickScratchCount;

            // MayHaveZIndexedChildren is never cleared, so it outlives the z-indexed children that set it.
            if (minZIndex != maxZIndex)
                SpanSort.Sort(s_PickScratch.AsSpan(frameBase, frameEnd - frameBase), s_CompareEntries);

            for (int i = frameBase; i < frameEnd; i++)
            {
                var entry = s_PickScratch[i];
                var child = entry.child;

                // Skip children reparented mid-pick by a ContainsPoint override; the ray transform below assumes the old parent.
                if (child.hierarchy.parent != root)
                    continue;

                // A negative z-index child promoted to an ancestor stacking context paints behind this element;
                // one that stayed under it paints on top of it, like any other child.
                if (!rootPicked && entry.zIndex < 0 && IsPromotedOutOfParent(child))
                {
                    rootPicked = true;
                    if (PickRoot(root, containsPoint, outResults, ref returnedChild))
                        return true;
                }

                if (PickChild(root, child, ray, outResults, ref returnedChild))
                    return true;
            }

            return false;
        }
        finally
        {
            for (int i = frameBase; i < s_PickScratchCount; i++)
                s_PickScratch[i].child = null;
            s_PickScratchCount = frameBase;
        }
    }

    // Negative when a comes first: descending z-index, then ascending sequence.
    private static int CompareEntries(ref ChildPickEntry a, ref ChildPickEntry b)
    {
        if (a.zIndex != b.zIndex)
            return a.zIndex > b.zIndex ? -1 : 1;
        return a.seq < b.seq ? -1 : a.seq > b.seq ? 1 : 0;
    }

    private static void AddScratchEntry(ChildPickEntry entry)
    {
        var scratch = s_PickScratch;
        if (s_PickScratchCount == scratch.Length)
        {
            Array.Resize(ref scratch, scratch.Length * 2);
            s_PickScratch = scratch;
        }
        scratch[s_PickScratchCount++] = entry;
    }

    private static bool IsPromotedOutOfParent(VisualElement child)
    {
        return (child.transformFlags & VisualElementTransformFlags.ZIndexPromotedOutOfParent) != 0;
    }

    // Returns true when the pick is resolved and the remaining children can be skipped.
    private static bool PickRoot(VisualElement root, bool containsPoint, List<VisualElement> outResults,
        ref VisualElement returnedChild)
    {
        if (!root.visible || root.pickingMode != PickingMode.Position || !containsPoint)
            return false;

        outResults?.Add(root);
        if (returnedChild == null)
        {
            returnedChild = root;
            return outResults == null;
        }

        return false;
    }

    // Returns true when the pick is resolved and the remaining children can be skipped.
    private static bool PickChild(VisualElement root, VisualElement child, Ray ray, List<VisualElement> outResults,
        ref VisualElement returnedChild)
    {
        var childRay = root.ChangeCoordinatesTo(child, ray);
        var result = PerformPick(child, childRay, outResults);
        if (returnedChild == null && result != null)
        {
            returnedChild = result;
            return outResults == null;
        }

        return false;
    }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal static Bounds GetPicking3DWorldBounds(VisualElement ve)
    {
        var bb = GetPicking3DLocalBounds(ve);
        VisualElement.TransformAlignedBounds(ref ve.worldTransformRef, ref bb);
        return bb;
    }

    internal static Bounds GetPicking3DLocalBounds(VisualElement ve)
    {
        if (ve.needs3DBounds)
            return ve.localBoundsPicking3D;

        Rect bb = ve.boundingBox;
        return new Bounds(bb.center, bb.size);
    }

    /// <summary>
    /// Transforms a point from the element's local coordinate system to the global GameObject
    /// world coordinate system.
    /// </summary>
    /// <remarks>The @@element@@ has to be parented to a document.</remarks>
    /// <param name="element">The element to use for the reference local coordinate system.</param>
    /// <param name="localPoint">The point to transform.</param>
    public static Vector3 LocalPointToGameObjectWorldSpace(VisualElement element, Vector3 localPoint)
    {
        var panelComponent = element.FindRootPanelComponent();
        if (panelComponent == null)
            throw new ArgumentException("Element must be part of a UI Document.");
        var documentPoint = element.LocalToWorld3D(localPoint);
        return panelComponent.gameObject.transform.TransformPoint(documentPoint);
    }

    /// <summary>
    /// Transforms a delta vector from the element's local coordinate system to the global GameObject
    /// world coordinate system.
    /// </summary>
    /// <remarks>The @@element@@ has to be parented to a document.</remarks>
    /// <param name="element">The element to use for the reference local coordinate system.</param>
    /// <param name="localDelta">The vector to transform.</param>
    public static Vector3 LocalDeltaToGameObjectWorldSpace(VisualElement element, Vector3 localDelta)
    {
        return LocalPointToGameObjectWorldSpace(element, localDelta) -
               LocalPointToGameObjectWorldSpace(element, Vector3.zero);
    }

    /// <summary>
    /// Transforms a point from the global GameObject world coordinate system to the element's
    /// local coordinate system.
    /// </summary>
    /// <remarks>The @@element@@ has to be parented to a document.</remarks>
    /// <param name="element">The element to use for the reference local coordinate system.</param>
    /// <param name="worldPoint">The point to transform.</param>
    public static Vector3 GameObjectWorldSpaceToLocalPoint(VisualElement element, Vector3 worldPoint)
    {
        var panelComponent = element.FindRootPanelComponent();
        if (panelComponent == null)
            throw new ArgumentException("Element must be part of a UI Document.");
        var documentPoint = panelComponent.gameObject.transform.InverseTransformPoint(worldPoint);
        return element.WorldToLocal3D(documentPoint);
    }

    /// <summary>
    /// Transforms a delta vector from the global GameObject world coordinate system to the element's
    /// local coordinate system.
    /// </summary>
    /// <remarks>The @@element@@ has to be parented to a document.</remarks>
    /// <param name="element">The element to use for the reference local coordinate system.</param>
    /// <param name="worldDelta">The vector to transform.</param>
    public static Vector3 GameObjectWorldSpaceToLocalDelta(VisualElement element, Vector3 worldDelta)
    {
        return GameObjectWorldSpaceToLocalPoint(element, worldDelta) -
               GameObjectWorldSpaceToLocalPoint(element, Vector3.zero);
    }
}
