// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    static partial class UIRUtility
    {
        static readonly ProfilerMarker k_ComputeTransformMatrixMarker = new("UIR.ComputeTransformMatrix");

        public static readonly string k_DefaultShaderName = UIR.Shaders.k_Default;

        // We provide our own epsilon to avoid issues such as case 1335430. Some native plugin
        // disable float-denormalization, which can lead to the wrong Mathf.Epsilon being used.
        public const float k_Epsilon = 1.0E-30f;

        public const float k_ClearZ = 0.99f; // At the far plane like standard Unity rendering
        public const float k_MeshPosZ = 0.0f; // The correct z value to draw a shape
        public const float k_MaskPosZ = 1.0f; // The correct z value to push/pop a mask
        public const int k_MaxMaskDepth = 7; // Requires 3 bits in the stencil
        public const float k_RenderTextureMargin = 1;

        // Keep in sync with UIRVertexUtility.h
        public const byte k_DynamicColorDisabled = 0;
        public const byte k_DynamicColorEnabled = 1;
        public const byte k_DynamicColorEnabledText = 2;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool ShapeWindingIsClockwise(int maskDepth, int stencilRef)
        {
            Debug.Assert(maskDepth == stencilRef || maskDepth == stencilRef + 1);
            return maskDepth == stencilRef;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect Encapsulate(Rect a, Rect b)
        {
            Vector2 min = Vector2.Min(a.min, b.min);
            Vector2 max = Vector2.Max(a.max, b.max);
            return new Rect(min, max - min);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect Inflate(Rect r, float i)
        {
            return new Rect(r.xMin - i, r.yMin - i, r.width + i + i, r.height + i + i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect InflateByMargins(Rect r, PostProcessingMargins margins)
        {
            return new Rect(
                r.xMin - margins.left,
                r.yMin - margins.top,
                r.width + margins.left + margins.right,
                r.height + margins.top + margins.bottom);
        }

        public static void ComputeMatrixRelativeToAncestor(RenderData renderData, RenderData ancestor, out Matrix4x4 transform)
        {
            ref var ancestorInverse = ref ancestor.owner.GetWorldTransformInverse(out bool isSingular);
            if (isSingular)
                ComputeTransformMatrix(renderData, ancestor, out transform);
            else
                VisualElement.MultiplyMatrix34(ref ancestorInverse, ref renderData.owner.worldTransformRef, out transform);
        }

        public static void ComputeMatrixRelativeToRenderTree(RenderData renderData, out Matrix4x4 transform)
        {
            var treeRoot = renderData.renderTree.rootRenderData;
            if (treeRoot.isNestedRenderTreeRoot)
                ComputeMatrixRelativeToAncestor(renderData, treeRoot, out transform); // TODO: Cache the inverse in the RenderTree to speed this up
            else
                // We are in the topmost render tree
                transform = renderData.owner.worldTransform;
        }

        // Returns the transform to be applied to vertices that are in their local space
        public static void GetVerticesTransformInfo(RenderData renderData, out Matrix4x4 transform)
        {
            if (RenderData.AllocatesID(renderData.transformID) || renderData.isGroupTransform || renderData.isNestedRenderTreeRoot)
                transform = Matrix4x4.identity;
            else if (renderData.boneTransformAncestor != null)
                ComputeMatrixRelativeToAncestor(renderData, renderData.boneTransformAncestor, out transform);
            else if (renderData.groupTransformAncestor != null)
                ComputeMatrixRelativeToAncestor(renderData, renderData.groupTransformAncestor, out transform);
            else
                ComputeMatrixRelativeToRenderTree(renderData, out transform);

            if (renderData.owner.elementPanel is { isFlat: true })
                transform.m22 = 1.0f; // Scaling in z means nothing for 2d ui, and break masking
        }

        // Fallback for ComputeMatrixRelativeToAncestor when the ancestor's world matrix is singular
        // (VisualElement.isWorldTransformSingular): composes local matrices instead of multiplying by an inverse. UUM-4171.
        internal static void ComputeTransformMatrix(RenderData renderData, RenderData ancestor, out Matrix4x4 result)
        {
            Debug.Assert(renderData.renderTree == ancestor.renderTree);

            using (k_ComputeTransformMatrixMarker.Auto())
            {
                if (renderData == ancestor)
                {
                    result = Matrix4x4.identity;
                    return;
                }

                // Each pivoted matrix is relative to the hierarchy parent, so the chain must follow the
                // hierarchy: the render-tree parent skips visual ancestors of z-index-promoted elements.
                var ancestorOwner = ancestor.owner;
                var owner = renderData.owner;
                owner.GetPivotedMatrixWithLayout(out result);
                var current = owner.hierarchy.parent;
                if (current == null || current == ancestorOwner)
                    return;

                Matrix4x4 temp = new Matrix4x4();
                bool destIsTemp = true;

                do
                {
                    current.GetPivotedMatrixWithLayout(out Matrix4x4 ancestorMatrix);
                    if (destIsTemp)
                        VisualElement.MultiplyMatrix34(ref ancestorMatrix, ref result, out temp);
                    else
                        VisualElement.MultiplyMatrix34(ref ancestorMatrix, ref temp, out result);

                    current = current.hierarchy.parent;
                    destIsTemp = !destIsTemp;
                } while (current != null && current != ancestorOwner);

                // Invert logic as destIsTemp is changed each iteration
                if (!destIsTemp)
                    result = temp;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RectHasArea(Rect rect)
        {
            return rect.width > k_Epsilon && rect.height > k_Epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RectHasArea(RectInt rect)
        {
            return rect.width > 0 && rect.height > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect CastToRect(RectInt rect)
        {
            return new Rect(rect.xMin, rect.yMin, rect.width, rect.height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectInt CastToRectInt(Rect rect)
        {
            return new RectInt(
                Mathf.FloorToInt(rect.xMin),
                Mathf.FloorToInt(rect.yMin),
                Mathf.CeilToInt(rect.width),
                Mathf.CeilToInt(rect.height));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRoundRect(VisualElement ve)
        {
            var style = ve.resolvedStyle;
            return !(style.borderTopLeftRadius < k_Epsilon &&
                style.borderTopRightRadius < k_Epsilon &&
                style.borderBottomLeftRadius < k_Epsilon &&
                style.borderBottomRightRadius < k_Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply2D(this Quaternion rotation, ref Vector2 point)
        {
            // Even though Quaternion coordinates aren't the same as Euler angles, it so happens that a rotation only
            // in the z axis will also have only a z (and w) value that is non-zero. Cool, heh!
            // Here we'll assume rotation.x = rotation.y = 0.
            float z = rotation.z * 2f;
            float zz = 1f - rotation.z * z;
            float wz = rotation.w * z;
            point = new Vector2(zz * point.x - wz * point.y, wz * point.x + zz * point.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVectorImageBackground(VisualElement ve)
        {
            return ve.computedStyle.backgroundImage.vectorImage != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsElementSelfHidden(VisualElement ve)
        {
            return ve.resolvedStyle.visibility == Visibility.Hidden;
        }

        public static void Destroy(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        public static int GetPrevPow2(int n)
        {
            int bits = 0;
            while (n > 1)
            {
                n >>= 1;
                ++bits;
            }

            return 1 << bits;
        }

        public static int GetNextPow2(int n)
        {
            int test = 1;
            while (test < n)
                test <<= 1;
            return test;
        }

        public static int GetNextPow2Exp(int n)
        {
            int test = 1;
            int exp = 0;
            while (test < n)
            {
                test <<= 1;
                ++exp;
            }

            return exp;
        }

        [ThreadStatic]
        static int? s_ThreadIndex;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static int GetThreadIndex()
        {
            // This code was optimized to avoid querying TLS twice.
            int? threadIndex = s_ThreadIndex;
            if (threadIndex.HasValue)
                return threadIndex.Value;

            int initValue = JobsUtility.ThreadIndex;
            s_ThreadIndex = initValue;
            return initValue;
        }
    }
}
