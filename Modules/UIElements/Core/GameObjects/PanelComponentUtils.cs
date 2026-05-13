// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    // Utility methods for both UIDocument and PanelRenderer
    [UnityEngine.Bindings.VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal static class PanelComponentUtils
    {
        public static bool IsTransformControlledByGameObject(IPanelComponent panelComponent)
        {
            var panelSettings = panelComponent.panelSettings;
            bool isWorldSpace = panelSettings != null && panelSettings.renderMode == PanelRenderMode.WorldSpace;
            var parentUI = panelComponent.parentUI;
            return isWorldSpace && (parentUI == null || panelComponent.position == Position.Absolute);
        }


        public static void ComputeParentTransform(Vector2 pivotOffset, float pixelsPerUnit, out Matrix4x4 matrix)
        {
            matrix = PanelComponentUtils.TransformToGameObjectMatrix(pivotOffset, pixelsPerUnit);
        }

        public static void ComputeNestedTransform(Transform transform, Transform parentTransform, Vector2 pivotOffset, Vector2 parentPivotOffset, float pixelsPerUnit, out Matrix4x4 matrix)
        {
            var ui2Go = ScaleAndFlipMatrix(pixelsPerUnit);
            var go2Ui = ui2Go.inverse;

            var childGoToWorld = transform.localToWorldMatrix;
            var worldToParentGo = parentTransform.worldToLocalMatrix;

            //                     (VEa To World)*(VEb To VEa) =                             (VEb To World)
            // (GOa To World)*(UI2GO)*(VEa Pivot)*(VEb To VEa) =                   (GOb To World)*(UI2GO)*(VEb Pivot)
            //           (VEb To VEa) = (VEa Pivot)^1*(UI2GO)^-1*(GOa To World)^-1*(GOb To World)*(UI2GO)*(VEb Pivot)
            matrix = go2Ui * worldToParentGo * childGoToWorld * ui2Go;

            MathUtils.PreApply2DOffset(ref matrix, -parentPivotOffset);

            // Apply the nested pivot
            MathUtils.PostApply2DOffset(ref matrix, pivotOffset);
        }

        public static Matrix4x4 TransformToGameObjectMatrix(Vector2 pivotOffset, float pixelsPerUnit)
        {
            var m = PanelComponentUtils.ScaleAndFlipMatrix(pixelsPerUnit);
            MathUtils.PostApply2DOffset(ref m, pivotOffset);
            return m;
        }

        public static Matrix4x4 ScaleAndFlipMatrix(float pixelsPerUnit)
        {
            // This is the root, apply the pixels-per-unit scaling, and the y-flip.
            float ppu = pixelsPerUnit;
            if (!float.IsFinite(ppu) || ppu < Mathf.Epsilon)
            {
                // This isn't a valid PPU, return the identity here, but the renderer will not be serialized
                return Matrix4x4.identity;
            }

            float ppuScale = 1.0f / ppu;

            var scale = Vector3.one * ppuScale;
            var flipRotation = Quaternion.AngleAxis(180.0f, Vector3.right); // Y-axis flip
            return Matrix4x4.TRS(Vector3.zero, flipRotation, scale);
        }


        public static Bounds LocalBoundsFromPivotSource(VisualElement root, PivotReferenceSize pivotReferenceSize)
        {
            var localBounds = root.localBounds3DWithoutNested3D;

            Bounds bb;
            if (pivotReferenceSize == PivotReferenceSize.BoundingBox)
            {
                bb = localBounds;
            }
            else
            {
                // Take the x,y size from the layout, but the z size from the bounds depth
                var layout = root.layout;
                var c = layout.center;
                var s = layout.size;
                float depth = localBounds.size.z;
                bb = new Bounds(new Vector3(c.x, c.y, localBounds.min.z + depth * 0.5f), new Vector3(s.x, s.y, depth));
            }

            return SanitizeRendererBounds(bb);
        }

        public static Bounds SanitizeRendererBounds(Bounds b)
        {
            // The bounds may be invalid if the element is not layed out yet
            if (float.IsNaN(b.size.x) || float.IsNaN(b.size.y) || float.IsNaN(b.size.z))
                b = new Bounds(Vector3.zero, Vector3.zero);
            if (b.size.x < 0.0f || b.size.y < 0.0f)
                b.size = Vector3.zero;
            return b;
        }

        public static Vector2 GetPivotAsPercent(Pivot origin)
        {
            switch (origin)
            {
                case Pivot.Center:
                    return new Vector2(0.5f, 0.5f);
                case Pivot.TopLeft:
                    return new Vector2(0, 0);
                case Pivot.TopCenter:
                    return new Vector2(0.5f, 0);
                case Pivot.TopRight:
                    return new Vector2(1, 0);
                case Pivot.LeftCenter:
                    return new Vector2(0, 0.5f);
                case Pivot.RightCenter:
                    return new Vector2(1, 0.5f);
                case Pivot.BottomLeft:
                    return new Vector2(0, 1);
                case Pivot.BottomCenter:
                    return new Vector2(0.5f, 1);
                case Pivot.BottomRight:
                    return new Vector2(1, 1);
            }
            return new Vector2(0.5f, 0.5f);
        }

        public static bool IsValidBounds(in Bounds b)
        {
            var e = b.extents;
            var validDimensionCount = (e.x > 0 ? 1 : 0) + (e.y > 0 ? 1 : 0) + (e.z > 0 ? 1 : 0);
            // Rays can intersect a plane or a volume, so we need at least 2 workable dimensions
            return validDimensionCount >= 2;
        }

        public static void DrawGizmoBounds(IPanelComponent panelComponent, Vector2 pivotOffset, float pixelsPerUnit)
        {
            var root = panelComponent?.GetRootVisualElement();
            if (root == null)
                return;

            var panelSettings = panelComponent.panelSettings;
            if (panelSettings == null || panelSettings.renderMode != PanelRenderMode.WorldSpace)
                return;

            // Find the first PanelRenderer that's controlled by a GameObject
            var gameObjectPanelComp = panelComponent;
            while (gameObjectPanelComp != null && !(PanelComponentUtils.IsTransformControlledByGameObject(gameObjectPanelComp)))
                gameObjectPanelComp = gameObjectPanelComp.parentUI;

            if (gameObjectPanelComp == null)
                return;

            Bounds bb;
            if (PanelComponentUtils.IsTransformControlledByGameObject(panelComponent))
            {
                bb = PanelComponentUtils.LocalBoundsFromPivotSource(root, panelComponent.pivotReferenceSize);
            }
            else
            {
                // Relative mode gizmos are drawn relative to the next ancestor that's
                // controlled by a GameObject transform
                var bbox = root.boundingBoxWithoutNested;
                bbox = root.ChangeCoordinatesTo(root, bbox);
                bb = new Bounds(bbox.center, bbox.size);
            }

            if (!PanelComponentUtils.IsValidBounds(bb))
                return;

            var toGameObject = PanelComponentUtils.TransformToGameObjectMatrix(pivotOffset, pixelsPerUnit);
            VisualElement.TransformAlignedBounds(ref toGameObject, ref bb);

            var matrixBackup = Gizmos.matrix;

            Vector3 center = bb.center;
            Vector3 size = bb.size;
            Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            Gizmos.matrix = gameObjectPanelComp.gameObject.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);

            Gizmos.matrix = matrixBackup;
        }

        static internal Vector3 GetPanelPosition(GameObject gameObject, IEventHandler pickedElement, Ray worldRay)
        {
            var documentRay = gameObject.transform.worldToLocalMatrix.TransformRay(worldRay);
            ((VisualElement)pickedElement).IntersectWorldRay(documentRay, out var distanceWithinDocument, out _);
            var documentPoint = documentRay.origin + documentRay.direction * distanceWithinDocument;
            return documentPoint;
        }
    }
}
