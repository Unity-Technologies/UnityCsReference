// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.UIR
{
    // Curved UI.
    //
    // Shared, allocation-free curvature geometry: the conform-chain construction plus the forward bend of a
    // point. It is the single source of truth for how a flat element-local point maps onto the curved surface,
    // reused by BOTH the render mesh modifier (UIRCurvatureMeshModifier, which bends generated vertices) and
    // input picking (VisualElement.IntersectLocalRay, which must hit exactly where the mesh is drawn). Keeping
    // ONE copy of the bend math guarantees picking and rendering never diverge.
    //
    // The geometry composes the chain of curved ancestors (innermost -> outermost); see CountChain / BuildChain
    // / Bend. It carries no renderer draw types, so it is safe to call from the core input path.
    static class UIRCurvatureGeometry
    {
        // Tessellation density: ~1 segment per this many degrees of bend, capped so a steep bend can't explode
        // the segment count. An axis that doesn't bend keeps a single segment.
        const float k_DegreesPerSegment = 5f;
        const int k_MaxSegments = 32;

        // Per-axis segment cap for the pick raycast. Too coarse and the last chord of a strongly-bent surface
        // overshoots the true curved EDGE by ~one segment, so a ray just past the visible rim can hit the
        // extrapolated triangle. 12 segments per axis keeps that under a pixel for the steepest supported bends.
        const int k_MaxPickSegments = 12;

        // The mesh modifier writes Vertex.z = sag * (1/ppu); PickZScale mirrors that exactly so the pick surface
        // (and the picking bounds built from it) coincides with the rendered mesh.
        public static float PickZScale(VisualElement ve)
        {
            return ve.elementPanel is BaseRuntimePanel rp && rp.pixelsPerUnit > UIRUtility.k_Epsilon
                ? 1f / rp.pixelsPerUnit
                : 1f;
        }

        // Render/pick reconciliation for the renderer's m22 forcing. The renderer draws the curved mesh through
        // the element's transformID matrix - the transform relative to its group-transform ancestor, else to the
        // render-tree root (UIRRenderEvents.GetTransformIDTransformInfo) - with m22 FORCED to 1, so the mesh Z
        // (the sag) is consumed as depth unscaled. The pick, however, reaches element-local space through the
        // NORMAL worldTransformInverse. Pre-mapping the bent point by C = T^-1 * T{m22=1} makes the pick surface
        // land exactly where the renderer draws it: worldTransform * (C*v) traverses the same T{m22=1} the GPU
        // applies. T is replicated from the render data so group-transform cases stay exact.
        static Matrix4x4 PickCorrection(VisualElement ve)
        {
            Matrix4x4 T;
            var rd = ve.renderData;
            if (rd == null || rd.renderTree == null)
            {
                Matrix4x4 wf = ve.worldTransform; wf.m22 = 1f;   // not in a render tree yet -> best-effort
                return ve.worldTransformInverse * wf;
            }
            if (rd.groupTransformAncestor != null)
                T = rd.groupTransformAncestor.owner.worldTransformInverse * ve.worldTransform;
            else
                UIRUtility.ComputeMatrixRelativeToRenderTree(rd, out T);
            Matrix4x4 Tf = T; Tf.m22 = 1f;
            return T.inverse * Tf;
        }

        // One curved ancestor in the conform chain: the affine map from the PREVIOUS space (the element, or the
        // next-inner curved root) into this root's flat box, plus the root's extent and per-axis bend angles.
        public readonly struct RootBend
        {
            public readonly Matrix4x4 toSpace;
            public readonly float w;
            public readonly float h;
            public readonly float thetaX;
            public readonly float thetaY;

            public RootBend(Matrix4x4 toSpace, float w, float h, float thetaX, float thetaY)
            {
                this.toSpace = toSpace;
                this.w = w;
                this.h = h;
                this.thetaX = thetaX;
                this.thetaY = thetaY;
            }
        }

        // Number of curved ancestors (including self) that form `ve`'s conform chain. Walks up skipping elements
        // whose curvature is `none`/unset (IsNone); each EXPLICIT declaration is a root. Stops at an explicit 0deg
        // flat barrier (IsNone() is false but both angles are 0) — it renders flat and opts its subtree out of any
        // outer curve — and at the top. Zero when nothing curves (or the nearest declaration is that flat barrier).
        public static int CountChain(VisualElement ve)
        {
            int count = 0;
            VisualElement e = ve;
            while (true)
            {
                while (e != null && e.computedStyle.unityCurvature.IsNone())
                    e = e.hierarchy.parent;
                if (e == null)
                    break;
                Curvature c = e.computedStyle.unityCurvature;
                if (c.x.ToRadians() == 0f && c.y.ToRadians() == 0f)
                    break; // explicit-0deg flat barrier: this root and anything above it do not bend the subtree
                count++;
                e = e.hierarchy.parent;
            }
            return count;
        }

        // Builds the conform chain for `ve` into `chain` (innermost first) and returns, via out params, the map
        // from the outermost root's space back to element-local plus the per-axis segment counts for `ve`'s own
        // arc extent. `chain.Length` must equal CountChain(ve). This mirrors the setup UIRCurvatureMeshModifier.
        // Modify performs before bending: it excludes the element's OWN out-of-plane translate (translate.z) from
        // the conform so the surface carries only the arc + sag (the render GPU transform re-applies translate.z
        // via m23; the pick ray already carries it through worldTransformInverse).
        public static void BuildConformChain(VisualElement ve, Span<RootBend> chain,
            out Matrix4x4 toElement, out int segX, out int segY)
        {
            Matrix4x4 wFlat = ve.worldTransform, wFlatInv = ve.worldTransformInverse;
            float ownTz = ve.computedStyle.translate.z;
            if (ownTz != 0f && ve.hierarchy.parent != null)
            {
                VisualElement parent = ve.hierarchy.parent;
                Matrix4x4 local = parent.worldTransformInverse * ve.worldTransform; // element relative to its parent
                local.m23 -= ownTz;                                                 // drop ONLY the out-of-plane translate
                wFlat = parent.worldTransform * local;
                wFlatInv = wFlat.inverse;
            }

            toElement = BuildChain(ve, wFlat, wFlatInv, chain, out _, out _, out float elemArcX, out float elemArcY);
            segX = SegmentsFor(elemArcX);
            segY = SegmentsFor(elemArcY);
        }

        // Fills `chain` (innermost first) with the map into each curved root's flat box + its extent/angles, sums
        // the absolute angle per axis (for tessellation density), and returns the map from the OUTERMOST root's
        // space back to `ve`-local. `chain.Length` must equal CountChain(ve). Each `toSpace` is built from
        // worldTransform (affine, pre-curvature), so it folds in every transform between the two spaces.
        static Matrix4x4 BuildChain(VisualElement ve, in Matrix4x4 veFrom, in Matrix4x4 veFromInverse, Span<RootBend> chain,
            out float sumThetaX, out float sumThetaY, out float elemArcX, out float elemArcY)
        {
            sumThetaX = 0f;
            sumThetaY = 0f;
            // Per-element tessellation density: the arc angle THIS element's own box subtends on the surface —
            // its extent mapped into each root's flat box, over that root's extent, times the root angle, summed
            // over the chain. A small control spans only a sliver of the arc, so it needs far fewer segments;
            // driving every element by the chain's FULL angle (sumTheta) over-tessellates the whole subtree.
            elemArcX = 0f;
            elemArcY = 0f;
            float elemW = ve.layout.width, elemH = ve.layout.height;
            Matrix4x4 elemToRoot = Matrix4x4.identity; // element-local -> current root's flat space (composed)
            Matrix4x4 prevWorld = veFrom; // space we map FROM (the element, with its own out-of-plane translate excluded)
            VisualElement e = ve;
            for (int i = 0; i < chain.Length; i++)
            {
                while (e.computedStyle.unityCurvature.IsNone())
                    e = e.hierarchy.parent;
                Curvature c = e.computedStyle.unityCurvature;
                // -unity-curvature names the axis the surface rotates AROUND (like transform rotation): x bends
                // the vertical extent (around X), y the horizontal (around Y). thetaX/thetaY here are the
                // horizontal/vertical EXTENT bend angles (paired with w/h in BendInSpace), so they take the
                // opposite component: horizontal bend <- curvature.y, vertical bend <- curvature.x.
                float tX = c.y.ToRadians();
                float tY = c.x.ToRadians();
                Matrix4x4 toSpace = e.worldTransformInverse * prevWorld;
                chain[i] = new RootBend(toSpace, e.layout.width, e.layout.height, tX, tY);
                sumThetaX += Mathf.Abs(tX);
                sumThetaY += Mathf.Abs(tY);

                elemToRoot = toSpace * elemToRoot; // element-local -> this root's flat box
                if (e.layout.width > UIRUtility.k_Epsilon)
                {
                    float xScale = new Vector3(elemToRoot.m00, elemToRoot.m10, elemToRoot.m20).magnitude;
                    elemArcX += Mathf.Abs(tX) * (xScale * elemW) / e.layout.width;
                }
                if (e.layout.height > UIRUtility.k_Epsilon)
                {
                    float yScale = new Vector3(elemToRoot.m01, elemToRoot.m11, elemToRoot.m21).magnitude;
                    elemArcY += Mathf.Abs(tY) * (yScale * elemH) / e.layout.height;
                }

                prevWorld = e.worldTransform;
                e = e.hierarchy.parent;
            }
            return veFromInverse * prevWorld; // outermost root space -> element-local
        }

        // 1 segment per k_DegreesPerSegment of bend (clamped). A non-bending axis stays at 1 segment.
        public static int SegmentsFor(float thetaRad)
        {
            if (thetaRad == 0f)
                return 1;
            int seg = Mathf.CeilToInt(Mathf.Abs(thetaRad) * Mathf.Rad2Deg / k_DegreesPerSegment);
            return Mathf.Clamp(seg, 1, k_MaxSegments);
        }

        // Composes the curved-ancestor CHAIN onto a flat element-local vertex. Each root i maps the running point
        // into its own flat space (`toSpace`, which folds in every transform between the previous space and this
        // root), bends it there, and accumulates the sag into Z so nested surfaces STACK: the leaf conforms to the
        // innermost curve, that curved result conforms to the next, and so on out to the outermost root. `toElement`
        // returns the final point to element-local; the caller re-applies this element's worldTransform, landing it
        // on the composed surface.
        public static Vector3 Bend(Vector3 pos, ReadOnlySpan<RootBend> chain, in Matrix4x4 toElement, float zScale)
        {
            Vector3 p = pos;
            for (int i = 0; i < chain.Length; i++)
            {
                p = chain[i].toSpace.MultiplyPoint3x4(p); // into this root's flat space (carries accumulated sag Z)
                p = BendInSpace(p, in chain[i], zScale);
            }

            // Back-project X/Y to element-local; keep the composed surface depth (p.z = accumulated sag, already
            // pre-scaled by zScale) as the local Z.
            Vector3 outLocal = toElement.MultiplyPoint3x4(p);
            outLocal.z = p.z;
            return outLocal;
        }

        // Bends a point already in a root's flat box ([0,w] x [0,h]) onto that root's bi-axial arc. The horizontal
        // extent wraps by thetaX (around the vertical axis), the vertical by thetaY; the center stays at the near
        // plane and edges recede in Z; the sign selects concave vs. convex. The sag is SUBTRACTED from the incoming
        // Z, so a chain of roots stacks its recessions. The two axes combine via CombineSag (quadrature for a
        // dome/bowl so corners round like a cap; sum for a saddle; a single axis reduces to that cylinder).
        static Vector3 BendInSpace(Vector3 p, in RootBend r, float zScale)
        {
            float x = p.x, y = p.y, z = p.z;
            float sagX = 0f, sagY = 0f; // signed Z recession per axis (sign follows the bend direction)

            if (r.thetaX != 0f && r.w > UIRUtility.k_Epsilon)
            {
                float u = x / r.w;
                float radius = r.w / r.thetaX;
                float a = (u - 0.5f) * r.thetaX;
                SinCos(a, out float s, out float co);
                x = r.w * 0.5f + radius * s;
                sagX = radius * (1f - co);
            }

            if (r.thetaY != 0f && r.h > UIRUtility.k_Epsilon)
            {
                float vv = y / r.h;
                float radius = r.h / r.thetaY;
                float a = (vv - 0.5f) * r.thetaY;
                SinCos(a, out float s, out float co);
                y = r.h * 0.5f + radius * s;
                sagY = radius * (1f - co);
            }

            z -= CombineSag(sagX, sagY) * zScale;

            return new Vector3(x, y, z);
        }

        // Combines the two axes' signed Z recessions. Same sign (dome/bowl, or a single axis where one sag is
        // zero): quadrature, so corners round off like a cap instead of summing into a pinch. Opposite signs
        // (saddle): the natural sum, which is the correct hyperbolic shape.
        static float CombineSag(float sagX, float sagY)
        {
            return sagX * sagY >= 0f
                ? Mathf.Sign(sagX + sagY) * Mathf.Sqrt(sagX * sagX + sagY * sagY)
                : sagX + sagY;
        }

        // Small-angle sin/cos for the bend: a Taylor polynomial accurate to < 1e-3 over |a| <= pi/2 (the per-root
        // bend range is a = +-theta/2), with an exact fallback beyond. Cheaper than libm Sin/Cos, and per-element
        // tessellation density keeps most angles tiny, where it is effectively exact.
        static void SinCos(float a, out float s, out float c)
        {
            if (a < -1.5707964f || a > 1.5707964f) { s = Mathf.Sin(a); c = Mathf.Cos(a); return; }
            float a2 = a * a;
            s = a * (1f + a2 * (-1f / 6f + a2 * (1f / 120f + a2 * (-1f / 5040f))));
            c = 1f + a2 * (-0.5f + a2 * (1f / 24f + a2 * (-1f / 720f)));
        }

        // Picking: intersect a ray, expressed in `ve`'s local space, with the bent surface of `ve`'s flat rect.
        // Returns the FLAT element-local point (z = 0) whose bent image the ray passes through — so downstream
        // rect.Contains / ContainsPoint / event coordinates keep working unchanged — plus the actual surface
        // point on the ray (for depth sorting). The surface is walked cell by cell with no managed allocation;
        // each cell's two triangles are ray-tested (double-sided: the element is drawn from behind too) and the
        // nearest hit wins. `zScale` should be PickZScale(ve) for real picks; the tests pass an explicit value to
        // verify round-trip self-consistency.
        public static bool TryIntersect(VisualElement ve, Ray localRay, float zScale,
            out Vector3 flatLocalPoint, out Vector3 surfaceLocalPoint)
        {
            flatLocalPoint = default;
            surfaceLocalPoint = default;

            int count = CountChain(ve);
            if (count == 0)
                return false;

            Rect rect = ve.rect;
            float w = rect.width, h = rect.height;
            if (w <= UIRUtility.k_Epsilon || h <= UIRUtility.k_Epsilon)
                return false;

            Span<RootBend> chain = stackalloc RootBend[count];
            BuildConformChain(ve, chain, out Matrix4x4 toElement, out int segX, out int segY);
            Matrix4x4 corr = PickCorrection(ve);

            int nx = Mathf.Clamp(segX, 1, k_MaxPickSegments);
            int ny = Mathf.Clamp(segY, 1, k_MaxPickSegments);

            float bestT = float.PositiveInfinity;
            bool hit = false;

            for (int j = 0; j < ny; j++)
            {
                float v0 = h * j / ny, v1 = h * (j + 1) / ny;
                for (int i = 0; i < nx; i++)
                {
                    float u0 = w * i / nx, u1 = w * (i + 1) / nx;

                    var f00 = new Vector3(u0, v0, 0f);
                    var f10 = new Vector3(u1, v0, 0f);
                    var f11 = new Vector3(u1, v1, 0f);
                    var f01 = new Vector3(u0, v1, 0f);

                    Vector3 b00 = corr.MultiplyPoint3x4(Bend(f00, chain, toElement, zScale));
                    Vector3 b10 = corr.MultiplyPoint3x4(Bend(f10, chain, toElement, zScale));
                    Vector3 b11 = corr.MultiplyPoint3x4(Bend(f11, chain, toElement, zScale));
                    Vector3 b01 = corr.MultiplyPoint3x4(Bend(f01, chain, toElement, zScale));

                    if (RayTriangle(localRay, b00, b10, b11, out float t, out float wa, out float wb, out float wc)
                        && t < bestT)
                    {
                        bestT = t;
                        flatLocalPoint = wa * f00 + wb * f10 + wc * f11;
                        hit = true;
                    }
                    if (RayTriangle(localRay, b00, b11, b01, out t, out wa, out wb, out wc)
                        && t < bestT)
                    {
                        bestT = t;
                        flatLocalPoint = wa * f00 + wb * f11 + wc * f01;
                        hit = true;
                    }
                }
            }

            if (!hit)
                return false;

            surfaceLocalPoint = localRay.origin + localRay.direction * bestT;
            return true;
        }

        // Möller–Trumbore ray/triangle intersection, double-sided (no back-face cull). On a hit, returns the ray
        // parameter `t` (> 0) and the barycentric weights (w0, w1, w2) such that hit = w0*a + w1*b + w2*c.
        static bool RayTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c,
            out float t, out float w0, out float w1, out float w2)
        {
            t = 0f; w0 = 0f; w1 = 0f; w2 = 0f;
            const float eps = 1e-8f;

            Vector3 e1 = b - a, e2 = c - a;
            Vector3 pv = Vector3.Cross(ray.direction, e2);
            float det = Vector3.Dot(e1, pv);
            if (det > -eps && det < eps)
                return false; // ray parallel to triangle

            float inv = 1f / det;
            Vector3 tv = ray.origin - a;
            float u = Vector3.Dot(tv, pv) * inv;
            if (u < 0f || u > 1f)
                return false;

            Vector3 qv = Vector3.Cross(tv, e1);
            float v = Vector3.Dot(ray.direction, qv) * inv;
            if (v < 0f || u + v > 1f)
                return false;

            t = Vector3.Dot(e2, qv) * inv;
            if (t <= 0f)
                return false;

            w0 = 1f - u - v;
            w1 = u;
            w2 = v;
            return true;
        }

        // Conservative local-space AABB of `ve`'s bent surface over its flat rect, for the picking bounds
        // (localBoundsPicking3D). Sampled on a small grid and encapsulated; the caller unions this into the
        // element's flat bounds so the ray early-out (bb.IntersectRay) can't drop rays that hit the receded
        // surface. `zScale` should match TryIntersect's (PickZScale) so bounds and pick share one space.
        public static Bounds SagLocalBounds(VisualElement ve, float zScale)
        {
            int count = CountChain(ve);
            Rect rect = ve.rect;
            float w = rect.width, h = rect.height;
            if (count == 0 || w <= UIRUtility.k_Epsilon || h <= UIRUtility.k_Epsilon)
                return new Bounds(rect.center, rect.size);

            Span<RootBend> chain = stackalloc RootBend[count];
            BuildConformChain(ve, chain, out Matrix4x4 toElement, out int segX, out int segY);
            Matrix4x4 corr = PickCorrection(ve);

            // A coarse sample suffices: the arc is monotone per axis, so its Z extremes live at the rect edges
            // and its X/Y stay within the rect. Reuse the pick cap so a steep bend can't blow this up.
            int nx = Mathf.Clamp(segX, 1, k_MaxPickSegments);
            int ny = Mathf.Clamp(segY, 1, k_MaxPickSegments);

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int j = 0; j <= ny; j++)
            {
                float y = h * j / ny;
                for (int i = 0; i <= nx; i++)
                {
                    Vector3 p = corr.MultiplyPoint3x4(Bend(new Vector3(w * i / nx, y, 0f), chain, toElement, zScale));
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        internal static class Testing
        {
            public static float CombineSag(float sagX, float sagY) => UIRCurvatureGeometry.CombineSag(sagX, sagY);

            public static bool TryIntersect(VisualElement ve, Ray localRay, float zScale,
                out Vector3 flatLocalPoint, out Vector3 surfaceLocalPoint)
                => UIRCurvatureGeometry.TryIntersect(ve, localRay, zScale, out flatLocalPoint, out surfaceLocalPoint);

            // Forward bend of a flat element-local point onto the curved surface, for round-trip tests: a ray
            // built through BendPoint(p) must make TryIntersect recover p (with the same zScale).
            public static Vector3 BendPoint(VisualElement ve, Vector3 flatLocalPoint, float zScale)
            {
                int count = CountChain(ve);
                if (count == 0)
                    return flatLocalPoint;
                Span<RootBend> chain = stackalloc RootBend[count];
                BuildConformChain(ve, chain, out Matrix4x4 toElement, out _, out _);
                return PickCorrection(ve).MultiplyPoint3x4(Bend(flatLocalPoint, chain, toElement, zScale));
            }
        }
    }
}
