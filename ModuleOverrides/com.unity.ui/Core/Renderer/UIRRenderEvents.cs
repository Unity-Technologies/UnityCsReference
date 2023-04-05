// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    internal enum ClipMethod
    {
        Undetermined,
        NotClipped,
        Scissor,
        ShaderDiscard,
        Stencil
    }

    internal static class RenderEvents
    {
        static readonly ProfilerMarker k_NudgeVerticesMarker = new ("UIR.NudgeVertices");

        private static readonly float VisibilityTreshold = UIRUtility.k_Epsilon;

        internal static void ProcessOnClippingChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
        {
            bool hierarchical = (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.ClippingHierarchy) != 0;
            if (hierarchical)
                stats.recursiveClipUpdates++;
            else stats.nonRecursiveClipUpdates++;
            DepthFirstOnClippingChanged(renderChain, ve.hierarchy.parent, ve, dirtyID, hierarchical, true, false, false, false, renderChain.device, ref stats);
        }

        internal static void ProcessOnOpacityChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
        {
            bool hierarchical = (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.OpacityHierarchy) != 0;
            stats.recursiveOpacityUpdates++;
            DepthFirstOnOpacityChanged(renderChain, ve.hierarchy.parent != null ? ve.hierarchy.parent.renderChainData.compositeOpacity : 1.0f, ve, dirtyID, hierarchical, ref stats);
        }

        internal static void ProcessOnColorChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
        {
            stats.colorUpdates++;
            OnColorChanged(renderChain, ve, dirtyID, ref stats);
        }

        internal static void ProcessOnTransformOrSizeChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
        {
            stats.recursiveTransformUpdates++;
            DepthFirstOnTransformOrSizeChanged(renderChain, ve.hierarchy.parent, ve, dirtyID, renderChain.device, false, false, ref stats);
        }

        static Matrix4x4 GetTransformIDTransformInfo(VisualElement ve)
        {
            Debug.Assert(RenderChainVEData.AllocatesID(ve.renderChainData.transformID) || ve.renderChainData.isGroupTransform);
            Matrix4x4 transform;
            if (ve.renderChainData.groupTransformAncestor != null)
                VisualElement.MultiplyMatrix34(ref ve.renderChainData.groupTransformAncestor.worldTransformInverse, ref ve.worldTransformRef, out transform);
            else transform = ve.worldTransform;
            transform.m22 = 1.0f; // Once world-space mode is introduced, this should become conditional
            return transform;
        }

        static Vector4 GetClipRectIDClipInfo(VisualElement ve)
        {
            Rect rect;

            Debug.Assert(RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID));
            if (ve.renderChainData.groupTransformAncestor == null)
                rect = ve.worldClip;
            else
            {
                rect = ve.worldClipMinusGroup;
                // Subtract the transform of the group transform ancestor
                VisualElement.TransformAlignedRect(ref ve.renderChainData.groupTransformAncestor.worldTransformInverse, ref rect);
            }

            // See ComputeRelativeClipRectCoords in the shader for details on this computation
            Vector2 min = rect.min;
            Vector2 max = rect.max;
            Vector2 diff = max - min;
            Vector2 mul = new Vector2(1 / (diff.x + 0.0001f), 1 / (diff.y + 0.0001f));
            Vector2 a = 2 * mul;
            Vector2 b = -(min + max) * mul;
            return new Vector4(a.x, a.y, b.x, b.y);
        }

        internal static uint DepthFirstOnChildAdded(RenderChain renderChain, VisualElement parent, VisualElement ve, int index, bool resetState)
        {
            Debug.Assert(ve.panel != null);

            if (ve.renderChainData.isInChain)
                return 0; // Already added, redundant call

            if (resetState)
                ve.renderChainData = new RenderChainVEData();

            ve.renderChainData.flags = RenderDataFlags.IsInChain;
            ve.renderChainData.verticesSpace = Matrix4x4.identity;
            ve.renderChainData.transformID = UIRVEShaderInfoAllocator.identityTransform;
            ve.renderChainData.clipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
            ve.renderChainData.opacityID = UIRVEShaderInfoAllocator.fullOpacity;
            ve.renderChainData.colorID = BMPAlloc.Invalid;
            ve.renderChainData.backgroundColorID = BMPAlloc.Invalid;
            ve.renderChainData.borderLeftColorID = BMPAlloc.Invalid;
            ve.renderChainData.borderTopColorID = BMPAlloc.Invalid;
            ve.renderChainData.borderRightColorID = BMPAlloc.Invalid;
            ve.renderChainData.borderBottomColorID = BMPAlloc.Invalid;
            ve.renderChainData.tintColorID = BMPAlloc.Invalid;
            ve.renderChainData.textCoreSettingsID = UIRVEShaderInfoAllocator.defaultTextCoreSettings;
            ve.renderChainData.compositeOpacity = float.MaxValue; // Any unreasonable value will do to trip the opacity composer to work
            UpdateLocalFlipsWinding(ve);

            if ((ve.renderHints & RenderHints.GroupTransform) != 0 && !renderChain.drawInCameras)
                ve.renderChainData.flags |= RenderDataFlags.IsGroupTransform;

            if (parent != null)
            {
                if (parent.renderChainData.isGroupTransform)
                    ve.renderChainData.groupTransformAncestor = parent;
                else ve.renderChainData.groupTransformAncestor = parent.renderChainData.groupTransformAncestor;
                ve.renderChainData.hierarchyDepth = parent.renderChainData.hierarchyDepth + 1;
            }
            else
            {
                ve.renderChainData.groupTransformAncestor = null;
                ve.renderChainData.hierarchyDepth = 0;
            }

            renderChain.EnsureFitsDepth(ve.renderChainData.hierarchyDepth);

            if (index > 0)
            {
                Debug.Assert(parent != null);
                ve.renderChainData.prev = GetLastDeepestChild(parent.hierarchy[index - 1]);
            }
            else ve.renderChainData.prev = parent;
            ve.renderChainData.next = ve.renderChainData.prev != null ? ve.renderChainData.prev.renderChainData.next : null;

            if (ve.renderChainData.prev != null)
                ve.renderChainData.prev.renderChainData.next = ve;
            if (ve.renderChainData.next != null)
                ve.renderChainData.next.renderChainData.prev = ve;

            // TransformID
            // Since transform type is controlled by render hints which are locked on the VE by now, we can
            // go ahead and prep transform data now and never check on it again under regular circumstances
            Debug.Assert(!RenderChainVEData.AllocatesID(ve.renderChainData.transformID));
            if (NeedsTransformID(ve))
                ve.renderChainData.transformID = renderChain.shaderInfoAllocator.AllocTransform(); // May fail, that's ok
            else ve.renderChainData.transformID = BMPAlloc.Invalid;
            ve.renderChainData.boneTransformAncestor = null;

            if (NeedsColorID(ve))
            {
                InitColorIDs(renderChain, ve);
                SetColorValues(renderChain, ve);
            }

            if (!RenderChainVEData.AllocatesID(ve.renderChainData.transformID))
            {
                if (parent != null && !ve.renderChainData.isGroupTransform)
                {
                    if (RenderChainVEData.AllocatesID(parent.renderChainData.transformID))
                        ve.renderChainData.boneTransformAncestor = parent;
                    else
                        ve.renderChainData.boneTransformAncestor = parent.renderChainData.boneTransformAncestor;

                    ve.renderChainData.transformID = parent.renderChainData.transformID;
                    ve.renderChainData.transformID.ownedState = OwnedState.Inherited; // Mark this allocation as not owned by us (inherited)
                }
                else ve.renderChainData.transformID = UIRVEShaderInfoAllocator.identityTransform;
            }
            else renderChain.shaderInfoAllocator.SetTransformValue(ve.renderChainData.transformID, GetTransformIDTransformInfo(ve));

            // Recurse on children
            int childrenCount = ve.hierarchy.childCount;
            uint deepCount = 0;
            for (int i = 0; i < childrenCount; i++)
                deepCount += DepthFirstOnChildAdded(renderChain, ve, ve.hierarchy[i], i, resetState);
            return 1 + deepCount;
        }

        internal static uint DepthFirstOnChildRemoving(RenderChain renderChain, VisualElement ve)
        {
            // Recurse on children
            int childrenCount = ve.hierarchy.childCount - 1;
            uint deepCount = 0;
            while (childrenCount >= 0)
                deepCount += DepthFirstOnChildRemoving(renderChain, ve.hierarchy[childrenCount--]);

            if (ve.renderChainData.isInChain)
            {
                renderChain.ChildWillBeRemoved(ve);
                CommandManipulator.ResetCommands(renderChain, ve);
                renderChain.ResetTextures(ve);
                ve.renderChainData.flags &= ~RenderDataFlags.IsInChain;
                ve.renderChainData.clipMethod = ClipMethod.Undetermined;

                if (ve.renderChainData.next != null)
                    ve.renderChainData.next.renderChainData.prev = ve.renderChainData.prev;
                if (ve.renderChainData.prev != null)
                    ve.renderChainData.prev.renderChainData.next = ve.renderChainData.next;

                if (RenderChainVEData.AllocatesID(ve.renderChainData.textCoreSettingsID))
                {
                    renderChain.shaderInfoAllocator.FreeTextCoreSettings(ve.renderChainData.textCoreSettingsID);
                    ve.renderChainData.textCoreSettingsID = UIRVEShaderInfoAllocator.defaultTextCoreSettings;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.opacityID))
                {
                    renderChain.shaderInfoAllocator.FreeOpacity(ve.renderChainData.opacityID);
                    ve.renderChainData.opacityID = UIRVEShaderInfoAllocator.fullOpacity;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.colorID))
                {
                    renderChain.shaderInfoAllocator.FreeColor(ve.renderChainData.colorID);
                    ve.renderChainData.colorID = BMPAlloc.Invalid;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.backgroundColorID))
                {
                    renderChain.shaderInfoAllocator.FreeColor(ve.renderChainData.backgroundColorID);
                    ve.renderChainData.backgroundColorID = BMPAlloc.Invalid;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.borderLeftColorID))
                {
                    renderChain.shaderInfoAllocator.FreeColor(ve.renderChainData.borderLeftColorID);
                    ve.renderChainData.borderLeftColorID = BMPAlloc.Invalid;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.borderTopColorID))
                {
                    renderChain.shaderInfoAllocator.FreeColor(ve.renderChainData.borderTopColorID);
                    ve.renderChainData.borderTopColorID = BMPAlloc.Invalid;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.borderRightColorID))
                {
                    renderChain.shaderInfoAllocator.FreeColor(ve.renderChainData.borderRightColorID);
                    ve.renderChainData.borderRightColorID = BMPAlloc.Invalid;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.borderBottomColorID))
                {
                    renderChain.shaderInfoAllocator.FreeColor(ve.renderChainData.borderBottomColorID);
                    ve.renderChainData.borderBottomColorID = BMPAlloc.Invalid;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.tintColorID))
                {
                    renderChain.shaderInfoAllocator.FreeColor(ve.renderChainData.tintColorID);
                    ve.renderChainData.tintColorID = BMPAlloc.Invalid;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID))
                {
                    renderChain.shaderInfoAllocator.FreeClipRect(ve.renderChainData.clipRectID);
                    ve.renderChainData.clipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
                }
                if (RenderChainVEData.AllocatesID(ve.renderChainData.transformID))
                {
                    renderChain.shaderInfoAllocator.FreeTransform(ve.renderChainData.transformID);
                    ve.renderChainData.transformID = UIRVEShaderInfoAllocator.identityTransform;
                }
                ve.renderChainData.boneTransformAncestor = ve.renderChainData.groupTransformAncestor = null;
                if (ve.renderChainData.tailMesh != null)
                {
                    renderChain.device.Free(ve.renderChainData.tailMesh);
                    ve.renderChainData.tailMesh = null;
                }
                if (ve.renderChainData.headMesh != null)
                {
                    renderChain.device.Free(ve.renderChainData.headMesh);
                    ve.renderChainData.headMesh = null;
                }
            }

            ve.renderChainData.prev = null;
            ve.renderChainData.next = null;

            return deepCount + 1;
        }

        static void DepthFirstOnClippingChanged(RenderChain renderChain,
            VisualElement parent,
            VisualElement ve,
            uint dirtyID,
            bool hierarchical,
            bool isRootOfChange,                // MUST be true  on the root call.
            bool isPendingHierarchicalRepaint,  // MUST be false on the root call.
            bool inheritedClipRectIDChanged,    // MUST be false on the root call.
            bool inheritedMaskingChanged,       // MUST be false on the root call.
            UIRenderDevice device,
            ref ChainBuilderStats stats)
        {
            bool upToDate = dirtyID == ve.renderChainData.dirtyID;
            if (upToDate && !inheritedClipRectIDChanged && !inheritedMaskingChanged)
                return;

            ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            if (!isRootOfChange)
                stats.recursiveClipUpdatesExpanded++;

            isPendingHierarchicalRepaint |= (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;

            // Internal operations (done in this call) to do:
            bool mustUpdateClipRectID = hierarchical || isRootOfChange || inheritedClipRectIDChanged;
            bool mustUpdateClippingMethod = hierarchical || isRootOfChange;
            bool mustUpdateChildrenMasking = hierarchical || isRootOfChange || inheritedMaskingChanged;

            // External operations (done by recursion or postponed) to do:
            bool mustRepaintThis = false;
            bool mustRepaintHierarchy = false;
            bool mustProcessSizeChange = false;
            // mustRecurse implies recursing on all children, but doesn't force anything beyond them.
            // hierarchical implies recursing on all descendants
            // As a result, hierarchical implies mustRecurse
            bool mustRecurse = hierarchical;

            ClipMethod oldClippingMethod = ve.renderChainData.clipMethod;
            ClipMethod newClippingMethod = mustUpdateClippingMethod ? DetermineSelfClipMethod(renderChain, ve) : oldClippingMethod;

            // Shader discard support
            bool clipRectIDChanged = false;
            if (mustUpdateClipRectID)
            {
                BMPAlloc newClipRectID = ve.renderChainData.clipRectID;
                if (newClippingMethod == ClipMethod.ShaderDiscard)
                {
                    if (!RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID))
                    {
                        newClipRectID = renderChain.shaderInfoAllocator.AllocClipRect();
                        if (!newClipRectID.IsValid())
                        {
                            newClippingMethod = ClipMethod.Scissor; // Fallback to scissor since we couldn't allocate a clipRectID
                            // Both shader discard and scisorring work with world-clip rectangles, so no need
                            // to inherit any clipRectIDs for such elements, our own scissor rect clips up correctly
                            newClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
                        }
                    }
                }
                else
                {
                    if (RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID))
                        renderChain.shaderInfoAllocator.FreeClipRect(ve.renderChainData.clipRectID);

                    // Inherit parent's clipRectID if possible.
                    // Group transforms shouldn't inherit the clipRectID since they have a new frame of reference,
                    // they provide a new baseline with the _PixelClipRect instead.
                    if (!ve.renderChainData.isGroupTransform)
                    {
                        newClipRectID = ((newClippingMethod != ClipMethod.Scissor) && (parent != null)) ? parent.renderChainData.clipRectID : UIRVEShaderInfoAllocator.infiniteClipRect;
                        newClipRectID.ownedState = OwnedState.Inherited;
                    }
                }

                clipRectIDChanged = !ve.renderChainData.clipRectID.Equals(newClipRectID);
                Debug.Assert(!ve.renderChainData.isGroupTransform || !clipRectIDChanged);
                ve.renderChainData.clipRectID = newClipRectID;
            }

            bool maskingChanged = false;
            if (oldClippingMethod != newClippingMethod)
            {
                ve.renderChainData.clipMethod = newClippingMethod;

                if (oldClippingMethod == ClipMethod.Stencil || newClippingMethod == ClipMethod.Stencil)
                {
                    maskingChanged = true;
                    mustUpdateChildrenMasking = true;
                }

                if (oldClippingMethod == ClipMethod.Scissor || newClippingMethod == ClipMethod.Scissor)
                    // We need to add/remove scissor push/pop commands
                    mustRepaintThis = true;

                if (newClippingMethod == ClipMethod.ShaderDiscard || oldClippingMethod == ClipMethod.ShaderDiscard && RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID))
                    // We must update the clipping rects.
                    mustProcessSizeChange = true;
            }

            if (clipRectIDChanged)
            {
                // Our children MUST update their render data clipRectIDs
                mustRecurse = true;

                // Our children MUST update their vertex clipRectIDs
                mustRepaintHierarchy = true;
            }

            if (mustUpdateChildrenMasking)
            {
                int newChildrenMaskDepth = 0;
                int newChildrenStencilRef = 0;
                if (parent != null)
                {
                    newChildrenMaskDepth = parent.renderChainData.childrenMaskDepth;
                    newChildrenStencilRef = parent.renderChainData.childrenStencilRef;
                    if (newClippingMethod == ClipMethod.Stencil)
                    {
                        if (newChildrenMaskDepth > newChildrenStencilRef)
                            ++newChildrenStencilRef;
                        ++newChildrenMaskDepth;
                    }

                    // When applying the MaskContainer hint, we skip because the last depth level because even though we
                    // could technically increase the reference value, it would be useless since there won't be more
                    // deeply nested masks that could benefit from it.
                    if ((ve.renderHints & RenderHints.MaskContainer) == RenderHints.MaskContainer && newChildrenMaskDepth < UIRUtility.k_MaxMaskDepth)
                        newChildrenStencilRef = newChildrenMaskDepth;
                }

                if (ve.renderChainData.childrenMaskDepth != newChildrenMaskDepth || ve.renderChainData.childrenStencilRef != newChildrenStencilRef)
                    maskingChanged = true;

                ve.renderChainData.childrenMaskDepth = newChildrenMaskDepth;
                ve.renderChainData.childrenStencilRef = newChildrenStencilRef;
            }

            if (maskingChanged)
            {
                mustRecurse = true; // Our children must update their inherited state.

                // These optimizations would allow to skip repainting the hierarchy:
                // a) We could update the stencilRef in the commands without repainting
                // b) The winding order could be reversed without repainting (when required)
                // In the meantime, we have no other choice but to request a hierarchical repaint.
                mustRepaintHierarchy = true;
            }

            if ((mustRepaintThis || mustRepaintHierarchy) && !isPendingHierarchicalRepaint)
            {
                renderChain.UIEOnVisualsChanged(ve, mustRepaintHierarchy);
                isPendingHierarchicalRepaint = true;
            }

            if (mustProcessSizeChange)
                renderChain.UIEOnTransformOrSizeChanged(ve, false, true);

            if (mustRecurse)
            {
                int childrenCount = ve.hierarchy.childCount;
                for (int i = 0; i < childrenCount; i++)
                    DepthFirstOnClippingChanged(
                        renderChain,
                        ve,
                        ve.hierarchy[i],
                        dirtyID,
                        // Having to recurse doesn't mean that we need to process ALL descendants. For example, the
                        // propagation of the transformId may stop if a group or a bone is encountered.
                        hierarchical,
                        false,
                        isPendingHierarchicalRepaint,
                        clipRectIDChanged,
                        maskingChanged,
                        device,
                        ref stats);
            }
        }

        static void DepthFirstOnOpacityChanged(RenderChain renderChain, float parentCompositeOpacity, VisualElement ve,
            uint dirtyID, bool hierarchical, ref ChainBuilderStats stats, bool isDoingFullVertexRegeneration = false)
        {
            if (dirtyID == ve.renderChainData.dirtyID)
                return;

            ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass
            stats.recursiveOpacityUpdatesExpanded++;
            float oldOpacity = ve.renderChainData.compositeOpacity;
            float newOpacity = ve.resolvedStyle.opacity * parentCompositeOpacity;

            const float meaningfullOpacityChange = 0.0001f;

            bool visiblityTresholdPassed = (oldOpacity < VisibilityTreshold ^ newOpacity < VisibilityTreshold);
            bool compositeOpacityChanged = Mathf.Abs(oldOpacity - newOpacity) > meaningfullOpacityChange || visiblityTresholdPassed;
            if (compositeOpacityChanged)
            {
                // Avoid updating cached opacity if it changed too little, because we don't want slow changes to
                // update the cache and never trigger the compositeOpacityChanged condition.
                // The only small change allowed is when we cross the "visible" boundary of VisibilityTreshold
                ve.renderChainData.compositeOpacity = newOpacity;
            }

            bool changedOpacityID = false;
            bool hasDistinctOpacity = newOpacity < parentCompositeOpacity - meaningfullOpacityChange; //assume 0 <= opacity <= 1
            if (hasDistinctOpacity)
            {
                if (ve.renderChainData.opacityID.ownedState == OwnedState.Inherited)
                {
                    changedOpacityID = true;
                    ve.renderChainData.opacityID = renderChain.shaderInfoAllocator.AllocOpacity();
                }

                if ((changedOpacityID || compositeOpacityChanged) && ve.renderChainData.opacityID.IsValid())
                    renderChain.shaderInfoAllocator.SetOpacityValue(ve.renderChainData.opacityID, newOpacity);
            }
            else if (ve.renderChainData.opacityID.ownedState == OwnedState.Inherited)
            {
                // Just follow my parent's alloc
                if (ve.hierarchy.parent != null &&
                    !ve.renderChainData.opacityID.Equals(ve.hierarchy.parent.renderChainData.opacityID))
                {
                    changedOpacityID = true;
                    ve.renderChainData.opacityID = ve.hierarchy.parent.renderChainData.opacityID;
                    ve.renderChainData.opacityID.ownedState = OwnedState.Inherited;
                }
            }
            else
            {
                // I have an owned allocation, but I must match my parent's opacity, just set the opacity rather than free and inherit our parent's
                if (compositeOpacityChanged && ve.renderChainData.opacityID.IsValid())
                    renderChain.shaderInfoAllocator.SetOpacityValue(ve.renderChainData.opacityID, newOpacity);
            }

            if (isDoingFullVertexRegeneration)
            {
                // A parent already called UIEOnVisualsChanged with hierarchical=true
            }
            else if (changedOpacityID && ((ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.Visuals) == 0) &&
                     (ve.renderChainData.headMesh != null || ve.renderChainData.tailMesh != null))
            {
                renderChain.UIEOnOpacityIdChanged(ve); // Changed opacity ID, must update vertices.. we don't do it hierarchical here since our children will go through this too
            }

            if (compositeOpacityChanged || changedOpacityID || hierarchical)
            {
                // Recurse on children
                int childrenCount = ve.hierarchy.childCount;
                for (int i = 0; i < childrenCount; i++)
                {
                    DepthFirstOnOpacityChanged(renderChain, newOpacity, ve.hierarchy[i], dirtyID, hierarchical, ref stats,
                        isDoingFullVertexRegeneration);
                }
            }
        }

        static void OnColorChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
        {
            if (dirtyID == ve.renderChainData.dirtyID)
                return;

            ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass
            stats.colorUpdatesExpanded++;

            var newColor = ve.resolvedStyle.backgroundColor;

            // UUM-21405: Fully-transparent backgrounds don't generate any geometry. So, we need to
            // force a dirty-repaint if we were transparent before, otherwise we may be trying to
            // change the color of a mesh that doesn't exists.
            if (ve.renderChainData.backgroundColor.a == 0.0f && newColor.a > 0.0f)
                renderChain.UIEOnVisualsChanged(ve, false);

            ve.renderChainData.backgroundColor = newColor;

            bool shouldUpdateVisuals = false;
            if ((ve.renderHints & RenderHints.DynamicColor) == RenderHints.DynamicColor)
            {
                if (InitColorIDs(renderChain, ve))
                    // New colors were allocated, we need to update the visuals
                    shouldUpdateVisuals = true;

                SetColorValues(renderChain, ve);

                if (ve is TextElement && !RenderEvents.UpdateTextCoreSettings(renderChain, ve))
                    shouldUpdateVisuals = true;
            }
            else
                shouldUpdateVisuals = true;

            if (shouldUpdateVisuals)
                renderChain.UIEOnVisualsChanged(ve, false);
        }

        static void DepthFirstOnTransformOrSizeChanged(RenderChain renderChain, VisualElement parent, VisualElement ve, uint dirtyID, UIRenderDevice device, bool isAncestorOfChangeSkinned, bool transformChanged, ref ChainBuilderStats stats)
        {
            if (dirtyID == ve.renderChainData.dirtyID)
                return;

            stats.recursiveTransformUpdatesExpanded++;

            transformChanged |= (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.Transform) != 0;

            if (RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID))
                renderChain.shaderInfoAllocator.SetClipRectValue(ve.renderChainData.clipRectID, GetClipRectIDClipInfo(ve));

            if (transformChanged && UpdateLocalFlipsWinding(ve))
                renderChain.UIEOnVisualsChanged(ve, true);

            if (transformChanged)
            {
                UpdateZeroScaling(ve);
            }

            bool dirtyHasBeenResolved = true;
            if (RenderChainVEData.AllocatesID(ve.renderChainData.transformID))
            {
                renderChain.shaderInfoAllocator.SetTransformValue(ve.renderChainData.transformID, GetTransformIDTransformInfo(ve));
                isAncestorOfChangeSkinned = true;
                stats.boneTransformed++;
            }
            else if (!transformChanged)
            {
                // Only the clip info had to be updated, we can skip the other cases which are for transform changes only.
            }
            else if (ve.renderChainData.isGroupTransform)
            {
                stats.groupTransformElementsChanged++;
            }
            else if (isAncestorOfChangeSkinned)
            {
                // Children of a bone element inherit the transform data change automatically when the root updates that data, no need to do anything for children
                Debug.Assert(RenderChainVEData.InheritsID(ve.renderChainData.transformID)); // The element MUST have a transformID that has been inherited from an ancestor
                dirtyHasBeenResolved = false; // We just skipped processing, if another later transform change is queued on this element this pass then we should still process it
                stats.skipTransformed++;
            }
            else if ((ve.renderChainData.dirtiedValues & (RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy)) == 0 &&
                     (ve.renderChainData.headMesh != null || ve.renderChainData.tailMesh != null))
            {
                // If a visual update will happen, then skip work here as the visual update will incorporate the transformed vertices
                if (NudgeVerticesToNewSpace(ve, renderChain, device))
                    stats.nudgeTransformed++;
                else
                {
                    renderChain.UIEOnVisualsChanged(ve, false); // Nudging not allowed, so do a full visual repaint
                    stats.visualUpdateTransformed++;
                }
            }

            if (dirtyHasBeenResolved)
                ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            // Make sure to pre-evaluate world transform and clip now so we don't do it at render time
            if (renderChain.drawInCameras)
                ve.EnsureWorldTransformAndClipUpToDate();

            if (!ve.renderChainData.isGroupTransform)
            {
                // Recurse on children
                int childrenCount = ve.hierarchy.childCount;
                for (int i = 0; i < childrenCount; i++)
                    DepthFirstOnTransformOrSizeChanged(renderChain, ve, ve.hierarchy[i], dirtyID, device, isAncestorOfChangeSkinned, transformChanged, ref stats);
            }
        }

        public static bool UpdateTextCoreSettings(RenderChain renderChain, VisualElement ve)
        {
            if (ve == null || !TextUtilities.IsFontAssigned(ve))
                return false;

            bool allocatesID = RenderChainVEData.AllocatesID(ve.renderChainData.textCoreSettingsID);

            var settings = TextUtilities.GetTextCoreSettingsForElement(ve);

            // If we aren't using a color ID (the DynamicColor flag), the text color will be stored in the vertex data,
            // so there's no need for a color match with the default TextCore settings.
            bool useDefaultColor = !NeedsColorID(ve);

            if (useDefaultColor && !NeedsTextCoreSettings(ve) && !allocatesID)
            {
                // Use default TextCore settings
                ve.renderChainData.textCoreSettingsID = UIRVEShaderInfoAllocator.defaultTextCoreSettings;
                return true;
            }

            if (!allocatesID)
                ve.renderChainData.textCoreSettingsID = renderChain.shaderInfoAllocator.AllocTextCoreSettings(settings);

            if (RenderChainVEData.AllocatesID(ve.renderChainData.textCoreSettingsID))
            {
                if (ve.panel.contextType == ContextType.Editor)
                {
                    settings.faceColor *= UIElementsUtility.editorPlayModeTintColor;
                    settings.outlineColor *= UIElementsUtility.editorPlayModeTintColor;
                    settings.underlayColor *= UIElementsUtility.editorPlayModeTintColor;
                }
                renderChain.shaderInfoAllocator.SetTextCoreSettingValue(ve.renderChainData.textCoreSettingsID, settings);
            }

            return true;
        }

        static bool NudgeVerticesToNewSpace(VisualElement ve, RenderChain renderChain, UIRenderDevice device)
        {
            k_NudgeVerticesMarker.Begin();

            Matrix4x4 newTransform;
            UIRUtility.GetVerticesTransformInfo(ve, out newTransform);
            Matrix4x4 nudgeTransform = newTransform * ve.renderChainData.verticesSpace.inverse;

            // Attempt to reconstruct the absolute transform. If the result diverges from the absolute
            // considerably, then we assume that the vertices have become degenerate beyond restoration.
            // In this case we refuse to nudge, and ask for this element to be fully repainted to regenerate
            // the vertices without error.
            const float kMaxAllowedDeviation = 0.0001f;
            Matrix4x4 reconstructedNewTransform = nudgeTransform * ve.renderChainData.verticesSpace;
            float error;
            error = Mathf.Abs(newTransform.m00 - reconstructedNewTransform.m00);
            error += Mathf.Abs(newTransform.m01 - reconstructedNewTransform.m01);
            error += Mathf.Abs(newTransform.m02 - reconstructedNewTransform.m02);
            error += Mathf.Abs(newTransform.m03 - reconstructedNewTransform.m03);
            error += Mathf.Abs(newTransform.m10 - reconstructedNewTransform.m10);
            error += Mathf.Abs(newTransform.m11 - reconstructedNewTransform.m11);
            error += Mathf.Abs(newTransform.m12 - reconstructedNewTransform.m12);
            error += Mathf.Abs(newTransform.m13 - reconstructedNewTransform.m13);
            error += Mathf.Abs(newTransform.m20 - reconstructedNewTransform.m20);
            error += Mathf.Abs(newTransform.m21 - reconstructedNewTransform.m21);
            error += Mathf.Abs(newTransform.m22 - reconstructedNewTransform.m22);
            error += Mathf.Abs(newTransform.m23 - reconstructedNewTransform.m23);
            if (error > kMaxAllowedDeviation)
            {
                k_NudgeVerticesMarker.End();
                return false;
            }

            ve.renderChainData.verticesSpace = newTransform; // This is the new space of the vertices

            var job = new NudgeJobData
            {
                transform = nudgeTransform
            };

            if (ve.renderChainData.headMesh != null)
                PrepareNudgeVertices(ve, device, ve.renderChainData.headMesh, out job.headSrc, out job.headDst, out job.headCount);

            if (ve.renderChainData.tailMesh != null)
                PrepareNudgeVertices(ve, device, ve.renderChainData.tailMesh, out job.tailSrc, out job.tailDst, out job.tailCount);

            renderChain.jobManager.Add(ref job);

            k_NudgeVerticesMarker.End();
            return true;
        }

        static unsafe void PrepareNudgeVertices(VisualElement ve, UIRenderDevice device, MeshHandle mesh, out IntPtr src, out IntPtr dst, out int count)
        {
            int vertCount = (int)mesh.allocVerts.size;
            NativeSlice<Vertex> oldVerts = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, vertCount);
            NativeSlice<Vertex> newVerts;
            device.Update(mesh, (uint)vertCount, out newVerts);

            src = (IntPtr)oldVerts.GetUnsafePtr();
            dst = (IntPtr)newVerts.GetUnsafePtr();
            count = vertCount;
        }
        static VisualElement GetLastDeepestChild(VisualElement ve)
        {
            // O(n) of the visual tree depth, usually not too bad.. probably 10-15 in really bad cases
            int childCount = ve.hierarchy.childCount;
            while (childCount > 0)
            {
                ve = ve.hierarchy[childCount - 1];
                childCount = ve.hierarchy.childCount;
            }
            return ve;
        }

        static VisualElement GetNextDepthFirst(VisualElement ve)
        {
            // O(n) of the visual tree depth, usually not too bad.. probably 10-15 in really bad cases
            VisualElement parent = ve.hierarchy.parent;
            while (parent != null)
            {
                int childIndex = parent.hierarchy.IndexOf(ve);
                int childCount = parent.hierarchy.childCount;
                if (childIndex < childCount - 1)
                    return parent.hierarchy[childIndex + 1];
                ve = parent;
                parent = parent.hierarchy.parent;
            }
            return null;
        }

        static ClipMethod DetermineSelfClipMethod(RenderChain renderChain, VisualElement ve)
        {
            if (!renderChain.isFlat)
                return ClipMethod.NotClipped;

            if (!ve.ShouldClip())
                return ClipMethod.NotClipped;

            // Even though GroupTransform does not formally imply the use of scissors, we prefer to use them because
            // this way, we can avoid updating nested clipping rects.
            bool preferScissors = ve.renderChainData.isGroupTransform || (ve.renderHints & RenderHints.ClipWithScissors) != 0;
            ClipMethod rectClipMethod = preferScissors ? ClipMethod.Scissor : ClipMethod.ShaderDiscard;

            if (!renderChain.elementBuilder.RequiresStencilMask(ve))
                return rectClipMethod;

            int inheritedMaskDepth = 0;
            VisualElement parent = ve.hierarchy.parent;
            if (parent != null)
                inheritedMaskDepth = parent.renderChainData.childrenMaskDepth;

            // We're already at the deepest level, we can't go any deeper.
            if (inheritedMaskDepth == UIRUtility.k_MaxMaskDepth)
                return rectClipMethod;

            // Default to stencil
            return ClipMethod.Stencil;
        }

        // Returns true when a change was detected
        static bool UpdateLocalFlipsWinding(VisualElement ve)
        {
            if (!ve.elementPanel.isFlat)
                return false;

            bool oldFlipsWinding = ve.renderChainData.localFlipsWinding;
            Vector3 scale = ve.transform.scale;
            float winding = scale.x * scale.y;
            if (Math.Abs(winding) < 0.001f)
            {
                return false; // Close to zero, preserve the current value
            }

            bool newFlipsWinding = winding < 0;
            if (oldFlipsWinding != newFlipsWinding)
            {
                ve.renderChainData.localFlipsWinding = newFlipsWinding;
                return true;
            }

            return false;
        }

        static void UpdateZeroScaling(VisualElement ve)
        {
            ve.renderChainData.localTransformScaleZero = Math.Abs(ve.transform.scale.x * ve.transform.scale.y) < 0.001f;
        }

        static bool NeedsTransformID(VisualElement ve)
        {
            return !ve.renderChainData.isGroupTransform && (ve.renderHints & RenderHints.BoneTransform) != 0;
        }

        // Indicates whether the transform id assigned to an element has changed. It does not care who the owner is.
        static bool TransformIDHasChanged(Alloc before, Alloc after)
        {
            if (before.size == 0 && after.size == 0)
                // Whatever start is, both are invalid allocations.
                return false;

            if (before.size != after.size || before.start != after.start)
                return true;

            return false;
        }

        internal static bool NeedsColorID(VisualElement ve)
        {
            return (ve.renderHints & RenderHints.DynamicColor) == RenderHints.DynamicColor;
        }

        internal static bool NeedsTextCoreSettings(VisualElement ve)
        {
            // We may require a color ID when using non-trivial TextCore settings.
            var settings = TextUtilities.GetTextCoreSettingsForElement(ve);
            if (settings.outlineWidth != 0.0f || settings.underlayOffset != Vector2.zero || settings.underlaySoftness != 0.0f)
                return true;

            return false;
        }

        static bool InitColorIDs(RenderChain renderChain, VisualElement ve)
        {
            var style = ve.resolvedStyle;
            bool hasAllocated = false;
            if (!ve.renderChainData.colorID.IsValid() && style.color != Color.white)
            {
                ve.renderChainData.colorID = renderChain.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderChainData.backgroundColorID.IsValid() && style.backgroundColor != Color.clear)
            {
                ve.renderChainData.backgroundColorID = renderChain.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderChainData.borderLeftColorID.IsValid() && style.borderLeftWidth > 0.0f)
            {
                ve.renderChainData.borderLeftColorID = renderChain.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderChainData.borderTopColorID.IsValid() && style.borderTopWidth > 0.0f)
            {
                ve.renderChainData.borderTopColorID = renderChain.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderChainData.borderRightColorID.IsValid() && style.borderRightWidth > 0.0f)
            {
                ve.renderChainData.borderRightColorID = renderChain.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderChainData.borderBottomColorID.IsValid() && style.borderBottomWidth > 0.0f)
            {
                ve.renderChainData.borderBottomColorID = renderChain.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderChainData.tintColorID.IsValid() && style.unityBackgroundImageTintColor != Color.white)
            {
                ve.renderChainData.tintColorID = renderChain.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            return hasAllocated;
        }

        static void ResetColorIDs(VisualElement ve)
        {
            ve.renderChainData.colorID = BMPAlloc.Invalid;
            ve.renderChainData.backgroundColorID = BMPAlloc.Invalid;
            ve.renderChainData.borderLeftColorID = BMPAlloc.Invalid;
            ve.renderChainData.borderTopColorID = BMPAlloc.Invalid;
            ve.renderChainData.borderRightColorID = BMPAlloc.Invalid;
            ve.renderChainData.borderBottomColorID = BMPAlloc.Invalid;
            ve.renderChainData.tintColorID = BMPAlloc.Invalid;
        }

        public static void SetColorValues(RenderChain renderChain, VisualElement ve)
        {
            var style = ve.resolvedStyle;
            if (ve.renderChainData.colorID.IsValid())
                renderChain.shaderInfoAllocator.SetColorValue(ve.renderChainData.colorID, style.color);
            if (ve.renderChainData.backgroundColorID.IsValid())
                renderChain.shaderInfoAllocator.SetColorValue(ve.renderChainData.backgroundColorID, style.backgroundColor);
            if (ve.renderChainData.borderLeftColorID.IsValid())
                renderChain.shaderInfoAllocator.SetColorValue(ve.renderChainData.borderLeftColorID, style.borderLeftColor);
            if (ve.renderChainData.borderTopColorID.IsValid())
                renderChain.shaderInfoAllocator.SetColorValue(ve.renderChainData.borderTopColorID, style.borderTopColor);
            if (ve.renderChainData.borderRightColorID.IsValid())
                renderChain.shaderInfoAllocator.SetColorValue(ve.renderChainData.borderRightColorID, style.borderRightColor);
            if (ve.renderChainData.borderBottomColorID.IsValid())
                renderChain.shaderInfoAllocator.SetColorValue(ve.renderChainData.borderBottomColorID, style.borderBottomColor);
            if (ve.renderChainData.tintColorID.IsValid())
                renderChain.shaderInfoAllocator.SetColorValue(ve.renderChainData.tintColorID, style.unityBackgroundImageTintColor);
        }
    }
}
