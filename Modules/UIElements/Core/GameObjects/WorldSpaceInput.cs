// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public int parentPath;   // index in s_PickPath
        public int zIndex;
        public int seq;
    }

    private struct PickPathNode
    {
        public VisualElement element;
        public Ray ray;
        public int parent;   // -1 for the pick root
    }

    [ThreadStatic]
    [NoAutoStaticsCleanup] // emptied by the end of every pick
    private static ChildPickEntry[] s_PickScratch;

    [ThreadStatic]
    [NoAutoStaticsCleanup] // reset to the frame base by the end of every pick
    private static int s_PickScratchCount;

    [ThreadStatic]
    [NoAutoStaticsCleanup] // reset to the frame base by the end of every pick
    private static List<PickPathNode> s_PickPath;

    [ThreadStatic]
    [NoAutoStaticsCleanup] // reset to the frame base by the end of every pick
    private static List<VisualElement> s_PickChildSnapshot;

    // A child equal to ZIndexPromotionPath under this mask stays in its parent's stacking context and holds promoted descendants.
    private const VisualElementTransformFlags k_PromotionDescentFlags = VisualElementTransformFlags.ZIndexPromotedOutOfParent
        | VisualElementTransformFlags.ZIndexPromotionPath | VisualElementTransformFlags.ZIndexPromotionRoot;

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
        var intersection = element.IntersectWorldRay(documentRay, out var distance, out _);
        if (intersection != RayIntersection.Inside &&
            (!acceptOutside || intersection == RayIntersection.NoSurfacePoint || !(distance > 0)))
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
        return PerformPick(root, ray, outResults, true);
    }

    // rootCollectsPromoted: also pick z-index descendants promoted past the root, ordered as if the root were their stacking context.
    private static VisualElement PerformPick(VisualElement root, Ray ray, List<VisualElement> outResults, bool rootCollectsPromoted)
    {
        return root.needs3DBounds
            ? PerformPick3D(root, ray, outResults, rootCollectsPromoted)
            : PerformPick2D(root, ray, outResults, rootCollectsPromoted);
    }

    private static VisualElement PerformPick2D(VisualElement root, Ray ray, List<VisualElement> outResults, bool rootCollectsPromoted)
    {
        if (root.elementPanel == null)
            return null;

        // Flat path keeps its historical behavior: IntersectLocalRay returns the Z=0-plane point (in or out of
        // rect) and the native pick filters by layout.
        if (root.IntersectLocalRay(ray, out var point) == RayIntersection.NoSurfacePoint)
            return null;

        return PerformPickByPoint(root, point, outResults, rootCollectsPromoted);
    }

    private static unsafe VisualElement PerformPickByPoint(VisualElement root, Vector3 point, List<VisualElement> outResults, bool rootCollectsPromoted)
    {
        using var buffer = outResults != null ? UnmanagedHandleBuffer.CreateTemporary() : UnmanagedHandleBuffer.None();

        // Native implementation is 2-3 times faster, so we use it if we can.
        var handle = NativeTransformUtils.PerformPick(root.layoutNode.Handle, point, false, rootCollectsPromoted, &buffer);
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

    private static VisualElement PerformPick3D(VisualElement root, Ray ray, List<VisualElement> outResults, bool collectsPromoted)
    {
        // Skip picking for elements with display: none
        if (root.computedStyle.display == DisplayStyle.None)
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
        if (!IntersectRayAABB(ray, bb))
        {
            return default;
        }

        // Problem here: every time we pick, we need to do that expensive transformation.
        // The default Contains() compares with rect, while we could cache the rect in world space (transform 2 points, 4 if there is rotation) and be done
        // here we have to transform 1 point at every call.
        // Now since this is a virtual, we can't just start to call it with global pos... we could break client code.
        // EdgeControl and port connectors in GraphView overload this.

        // A default ContainsPoint depends only on rect and ray, so its result can be computed on demand by PickRoot.
        bool? containsPoint = null;
        bool clips = root.ShouldClip();
        if (clips || root.usesContainsPoint)
        {
            containsPoint = ContainsLocalRay(root, ray);
            // we only skip children in the case we visually clip them
            if (!containsPoint.Value && clips)
                return default;
        }

        VisualElement returnedChild = default;
        bool rootPicked = false;

        var cCount = root.hierarchy.childCount;
        bool collectsPromotedHere = cCount > 0 &&
            ((collectsPromoted && HasFlag(root, VisualElementTransformFlags.ZIndexPromotionPath)) ||
                HasFlag(root, VisualElementTransformFlags.ZIndexPromotionRoot));
        bool needsZIndexSort = collectsPromotedHere ||
            (cCount > 0 && HasFlag(root, VisualElementTransformFlags.MayHaveZIndexedChildren));

        if (needsZIndexSort)
        {
            if (PickChildrenInZIndexOrder(root, ray, collectsPromotedHere, ref containsPoint, outResults, ref returnedChild, ref rootPicked))
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
            PickRoot(root, ray, ref containsPoint, outResults, ref returnedChild);

        return returnedChild;
    }

    private static bool ContainsLocalRay(VisualElement ve, Ray ray)
    {
        // The default ContainsPoint is the rect test IntersectLocalRay already performed.
        return ve.IntersectLocalRay(ray, out var point) == RayIntersection.Inside && (!ve.usesContainsPoint || ve.ContainsPoint(point));
    }

    // Returns true when the pick is resolved and the root no longer needs to be tested.
    private static bool PickChildrenInZIndexOrder(VisualElement root, Ray ray, bool collectsPromoted, ref bool? containsPoint,
        List<VisualElement> outResults, ref VisualElement returnedChild, ref bool rootPicked)
    {
        s_PickScratch ??= new ChildPickEntry[16];
        s_PickPath ??= new List<PickPathNode>();
        s_PickChildSnapshot ??= new List<VisualElement>();
        int frameBase = s_PickScratchCount;
        int pathBase = s_PickPath.Count;
        int snapshotBase = s_PickChildSnapshot.Count;
        try
        {
            s_PickPath.Add(new PickPathNode { element = root, ray = ray, parent = -1 });

            bool needsSort = true;
            if (collectsPromoted)
            {
                // Paint draws a stacking context's children and the descendants promoted into it as one list,
                // sorted by z-index, ties in tree order.
                int seq = 0;
                AddPromotedPickEntries(pathBase, true, ref seq);
            }
            else
            {
                int minZIndex = int.MaxValue;
                int maxZIndex = int.MinValue;

                // Reverse order so equal z-index falls back to reverse paint order.
                int seq = 0;
                for (int i = root.hierarchy.childCount - 1; i >= 0; i--)
                {
                    var child = root.hierarchy[i];
                    // A promoted child is picked by the stacking context root it paints in.
                    if (IsPromotedOutOfParent(child))
                        continue;
                    int z = PickZIndex(child);
                    AddScratchEntry(new ChildPickEntry { child = child, parentPath = pathBase, zIndex = z, seq = seq++ });
                    if (z < minZIndex)
                        minZIndex = z;
                    if (z > maxZIndex)
                        maxZIndex = z;
                }

                // MayHaveZIndexedChildren is never cleared, so it outlives the z-indexed children that set it.
                needsSort = minZIndex != maxZIndex;
            }
            int frameEnd = s_PickScratchCount;

            if (needsSort)
                SpanSort.Sort(s_PickScratch.AsSpan(frameBase, frameEnd - frameBase), s_CompareEntries);

            bool isPromotionRoot = HasFlag(root, VisualElementTransformFlags.ZIndexPromotionRoot);
            for (int i = frameBase; i < frameEnd; i++)
            {
                var entry = s_PickScratch[i];
                var child = entry.child;
                var parentNode = s_PickPath[entry.parentPath];

                // Skip children reparented mid-pick by a ContainsPoint override; the ray transform below assumes the old path.
                if (child.hierarchy.parent != parentNode.element || !IsPickPathUnchanged(entry.parentPath))
                    continue;

                // Collected at a pick root that is not their stacking context: negative ones paint behind it.
                if (!rootPicked && !isPromotionRoot && entry.zIndex < 0 && IsPromotedOutOfParent(child))
                {
                    rootPicked = true;
                    if (PickRoot(root, ray, ref containsPoint, outResults, ref returnedChild))
                        return true;
                }

                if (PickChild(parentNode.element, child, parentNode.ray, outResults, ref returnedChild))
                    return true;
            }

            return false;
        }
        finally
        {
            for (int i = frameBase; i < s_PickScratchCount; i++)
                s_PickScratch[i].child = null;
            s_PickScratchCount = frameBase;
            s_PickPath.RemoveRange(pathBase, s_PickPath.Count - pathBase);
            s_PickChildSnapshot.RemoveRange(snapshotBase, s_PickChildSnapshot.Count - snapshotBase);
        }
    }

    // Adds, in tree order, the children of the path node (all of them, or only the promoted ones) and the elements promoted
    // out of the subtrees below, each with the path node to pick it from. Seq decreases, so a later entry picks first.
    private static void AddPromotedPickEntries(int parentPath, bool addAllChildren, ref int seq)
    {
        var parentNode = s_PickPath[parentPath];
        var parent = parentNode.element;

        // Snapshot: a ContainsPoint override below can remove children.
        int snapshotBase = s_PickChildSnapshot.Count;
        int childCount = parent.hierarchy.childCount;
        for (int i = 0; i < childCount; i++)
            s_PickChildSnapshot.Add(parent.hierarchy[i]);

        for (int i = snapshotBase; i < snapshotBase + childCount; i++)
        {
            var child = s_PickChildSnapshot[i];
            if (child.hierarchy.parent != parent)
                continue;

            bool isPromoted = IsPromotedOutOfParent(child);
            if (isPromoted || addAllChildren)
                AddScratchEntry(new ChildPickEntry { child = child, parentPath = parentPath, zIndex = PickZIndex(child), seq = seq-- });

            if ((child.transformFlags & k_PromotionDescentFlags) == VisualElementTransformFlags.ZIndexPromotionPath)
            {
                var childRay = ToChildRay(parent, child, parentNode.ray);
                if (PromotedPickCanPassThrough(child, childRay))
                {
                    s_PickPath.Add(new PickPathNode { element = child, ray = childRay, parent = parentPath });
                    AddPromotedPickEntries(s_PickPath.Count - 1, false, ref seq);
                }
            }
        }

        s_PickChildSnapshot.RemoveRange(snapshotBase, s_PickChildSnapshot.Count - snapshotBase);
    }

    private static bool IsPickPathUnchanged(int path)
    {
        for (var node = s_PickPath[path]; node.parent >= 0; node = s_PickPath[node.parent])
        {
            if (node.element.hierarchy.parent != s_PickPath[node.parent].element)
                return false;
        }
        return true;
    }

    private static bool PromotedPickCanPassThrough(VisualElement ve, Ray ray)
    {
        if (ve.computedStyle.display == DisplayStyle.None)
            return false;

        if (!IntersectRayAABB(ray, GetPicking3DLocalBounds(ve)))
            return false;

        return !ve.ShouldClip() || ContainsLocalRay(ve, ray);
    }

    private static int PickZIndex(VisualElement ve)
    {
        int z = ve.computedStyle.zIndex;
        return z == int.MinValue ? 0 : z;
    }

    private static bool HasFlag(VisualElement ve, VisualElementTransformFlags flag) => (ve.transformFlags & flag) != 0;

    private static Ray ToChildRay(VisualElement parent, VisualElement child, Ray ray)
    {
        return child.hasDefaultRotationAndScale
            ? new Ray(ray.origin - child.positionWithLayout, ray.direction)
            : parent.ChangeCoordinatesTo(child, ray);
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

    private static bool IsPromotedOutOfParent(VisualElement child) => HasFlag(child, VisualElementTransformFlags.ZIndexPromotedOutOfParent);

    // Returns true when the pick is resolved and the remaining children can be skipped.
    private static bool PickRoot(VisualElement root, Ray ray, ref bool? containsPoint, List<VisualElement> outResults,
        ref VisualElement returnedChild)
    {
        if (root.computedStyle.visibility != Visibility.Visible || root.pickingMode != PickingMode.Position)
            return false;

        containsPoint ??= ContainsLocalRay(root, ray);
        if (!containsPoint.Value)
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
        var result = PerformPick(child, ToChildRay(root, child, ray), outResults, false);
        if (returnedChild == null && result != null)
        {
            returnedChild = result;
            return outResults == null;
        }

        return false;
    }

    // Same semantics as Bounds.IntersectRay (native IntersectRayAABB), without the icall.
    internal static bool IntersectRayAABB(Ray ray, Bounds bounds)
    {
        var center = bounds.center;
        var extents = bounds.extents;
        var origin = ray.origin;
        var direction = ray.direction;

        float tmin = float.NegativeInfinity;
        float tmax = float.PositiveInfinity;
        return IntersectSlab(center.x - origin.x, extents.x, direction.x, ref tmin, ref tmax)
            && IntersectSlab(center.y - origin.y, extents.y, direction.y, ref tmin, ref tmax)
            && IntersectSlab(center.z - origin.z, extents.z, direction.z, ref tmin, ref tmax);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    static bool IntersectSlab(float p, float extent, float direction, ref float tmin, ref float tmax)
    {
        float f = 1.0f / direction;
        float t0 = (p + extent) * f;
        float t1 = (p - extent) * f;
        if (t0 < t1)
        {
            if (t0 > tmin)
                tmin = t0;
            if (t1 < tmax)
                tmax = t1;
        }
        else
        {
            if (t1 > tmin)
                tmin = t1;
            if (t0 < tmax)
                tmax = t0;
        }

        return !(tmin > tmax) && !(tmax < 0.0f);
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
