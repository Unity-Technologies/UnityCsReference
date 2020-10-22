using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR.Implementation
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
            stats.recursiveOpacityUpdates++;
            DepthFirstOnOpacityChanged(renderChain, ve.hierarchy.parent != null ? ve.hierarchy.parent.renderChainData.compositeOpacity : 1.0f, ve, dirtyID, ref stats);
        }

        internal static void ProcessOnTransformOrSizeChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
        {
            stats.recursiveTransformUpdates++;
            DepthFirstOnTransformOrSizeChanged(renderChain, ve.hierarchy.parent, ve, dirtyID, renderChain.device, false, false, ref stats);
        }

        internal static void ProcessOnVisualsChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
        {
            bool hierarchical = (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;
            if (hierarchical)
                stats.recursiveVisualUpdates++;
            else stats.nonRecursiveVisualUpdates++;
            var parent = ve.hierarchy.parent;
            var parentHierarchyHidden = parent != null &&
                (parent.renderChainData.isHierarchyHidden || IsElementHierarchyHidden(parent));
            DepthFirstOnVisualsChanged(renderChain, ve, dirtyID, parentHierarchyHidden, hierarchical, ref stats);
        }

        internal static void ProcessRegenText(RenderChain renderChain, VisualElement ve, UIRTextUpdatePainter painter, UIRenderDevice device, ref ChainBuilderStats stats)
        {
            stats.textUpdates++;
            painter.Begin(ve, device);
            ve.InvokeGenerateVisualContent(painter.meshGenerationContext);
            painter.End();
        }

        static Matrix4x4 GetTransformIDTransformInfo(VisualElement ve)
        {
            Debug.Assert(RenderChainVEData.AllocatesID(ve.renderChainData.transformID) || (ve.renderHints & (RenderHints.GroupTransform)) != 0);
            Matrix4x4 transform;
            if (ve.renderChainData.groupTransformAncestor != null)
                VisualElement.MultiplyMatrix34(in ve.renderChainData.groupTransformAncestor.worldTransformInverse, in ve.worldTransformRef, out transform);
            else transform = ve.worldTransform;
            transform.m22 = 1.0f; // Once world-space mode is introduced, this should become conditional
            return transform;
        }

        static Vector4 GetClipRectIDClipInfo(VisualElement ve)
        {
            Debug.Assert(RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID));
            if (ve.renderChainData.groupTransformAncestor == null)
                return UIRUtility.ToVector4(ve.worldClip);

            Rect rect = ve.worldClipMinusGroup;
            // Subtract the transform of the group transform ancestor
            VisualElement.TransformAlignedRect(in ve.renderChainData.groupTransformAncestor.worldTransformInverse, ref rect);
            return UIRUtility.ToVector4(rect);
        }

        static void GetVerticesTransformInfo(VisualElement ve, out Matrix4x4 transform)
        {
            if (RenderChainVEData.AllocatesID(ve.renderChainData.transformID) || (ve.renderHints & (RenderHints.GroupTransform)) != 0)
                transform = Matrix4x4.identity;
            else if (ve.renderChainData.boneTransformAncestor != null)
                VisualElement.MultiplyMatrix34(in ve.renderChainData.boneTransformAncestor.worldTransformInverse, in ve.worldTransformRef, out transform);
            else if (ve.renderChainData.groupTransformAncestor != null)
                VisualElement.MultiplyMatrix34(in ve.renderChainData.groupTransformAncestor.worldTransformInverse, in ve.worldTransformRef, out transform);
            else transform = ve.worldTransform;
            transform.m22 = 1.0f; // Once world-space mode is introduced, this should become conditional
        }

        internal static uint DepthFirstOnChildAdded(RenderChain renderChain, VisualElement parent, VisualElement ve, int index, bool resetState)
        {
            Debug.Assert(ve.panel != null);

            if (ve.renderChainData.isInChain)
                return 0; // Already added, redundant call

            if (resetState)
                ve.renderChainData = new RenderChainVEData();

            ve.renderChainData.isInChain = true;
            ve.renderChainData.verticesSpace = Matrix4x4.identity;
            ve.renderChainData.transformID = UIRVEShaderInfoAllocator.identityTransform;
            ve.renderChainData.clipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
            ve.renderChainData.opacityID = UIRVEShaderInfoAllocator.fullOpacity;
            ve.renderChainData.compositeOpacity = float.MaxValue; // Any unreasonable value will do to trip the opacity composer to work

            if (parent != null)
            {
                if ((parent.renderHints & (RenderHints.GroupTransform)) != 0)
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

            if (!RenderChainVEData.AllocatesID(ve.renderChainData.transformID))
            {
                if (parent != null && (ve.renderHints & RenderHints.GroupTransform) == 0)
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

            if ((ve.renderHints & RenderHints.GroupTransform) != 0)
                renderChain.StopTrackingGroupTransformElement(ve);

            if (ve.renderChainData.isInChain)
            {
                renderChain.ChildWillBeRemoved(ve);
                ResetCommands(renderChain, ve);
                renderChain.ResetTextures(ve);
                ve.renderChainData.isInChain = false;
                ve.renderChainData.clipMethod = ClipMethod.Undetermined;

                if (ve.renderChainData.next != null)
                    ve.renderChainData.next.renderChainData.prev = ve.renderChainData.prev;
                if (ve.renderChainData.prev != null)
                    ve.renderChainData.prev.renderChainData.next = ve.renderChainData.next;

                if (RenderChainVEData.AllocatesID(ve.renderChainData.opacityID))
                {
                    renderChain.shaderInfoAllocator.FreeOpacity(ve.renderChainData.opacityID);
                    ve.renderChainData.opacityID = UIRVEShaderInfoAllocator.fullOpacity;
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
                if (ve.renderChainData.closingData != null)
                {
                    renderChain.device.Free(ve.renderChainData.closingData);
                    ve.renderChainData.closingData = null;
                }
                if (ve.renderChainData.data != null)
                {
                    renderChain.device.Free(ve.renderChainData.data);
                    ve.renderChainData.data = null;
                }
            }
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
            bool inheritedStencilClippedChanged,// MUST be false on the root call.
            UIRenderDevice device,
            ref ChainBuilderStats stats)
        {
            bool upToDate = dirtyID == ve.renderChainData.dirtyID;
            if (upToDate && !inheritedClipRectIDChanged && !inheritedStencilClippedChanged)
                return;

            ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            if (!isRootOfChange)
                stats.recursiveClipUpdatesExpanded++;

            isPendingHierarchicalRepaint |= (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;

            // Internal operations (done in this call) to do:
            bool mustUpdateClipRectID = hierarchical || isRootOfChange || inheritedClipRectIDChanged;
            bool mustUpdateClippingMethod = hierarchical || isRootOfChange;
            bool mustUpdateStencilClippedFlag = hierarchical || isRootOfChange || inheritedStencilClippedChanged;

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
                    if ((ve.renderHints & RenderHints.GroupTransform) == 0)
                    {
                        newClipRectID = ((newClippingMethod != ClipMethod.Scissor) && (parent != null)) ? parent.renderChainData.clipRectID : UIRVEShaderInfoAllocator.infiniteClipRect;
                        newClipRectID.ownedState = OwnedState.Inherited;
                    }
                }

                clipRectIDChanged = !ve.renderChainData.clipRectID.Equals(newClipRectID);
                Debug.Assert((ve.renderHints & RenderHints.GroupTransform) == 0 || !clipRectIDChanged);
                ve.renderChainData.clipRectID = newClipRectID;
            }

            if (oldClippingMethod != newClippingMethod)
            {
                ve.renderChainData.clipMethod = newClippingMethod;

                if (oldClippingMethod == ClipMethod.Stencil || newClippingMethod == ClipMethod.Stencil)
                {
                    mustUpdateStencilClippedFlag = true;

                    // Proper winding order must be used.
                    mustRepaintHierarchy = true;
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

            bool isStencilClippedChanged = false;
            if (mustUpdateStencilClippedFlag)
            {
                bool oldStencilClipped = ve.renderChainData.isStencilClipped;
                bool newStencilClipped = newClippingMethod == ClipMethod.Stencil || (parent != null && parent.renderChainData.isStencilClipped);
                ve.renderChainData.isStencilClipped = newStencilClipped;
                if (oldStencilClipped != newStencilClipped)
                {
                    isStencilClippedChanged = true;

                    // Our children MUST update their isStencilClipped flag
                    mustRecurse = true;
                }
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
                        isStencilClippedChanged,
                        device,
                        ref stats);
            }
        }

        static void DepthFirstOnOpacityChanged(RenderChain renderChain, float parentCompositeOpacity, VisualElement ve,
            uint dirtyID, ref ChainBuilderStats stats, bool isDoingFullVertexRegeneration = false)
        {
            if (dirtyID == ve.renderChainData.dirtyID)
                return;

            ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass
            stats.recursiveOpacityUpdatesExpanded++;
            float oldOpacity = ve.renderChainData.compositeOpacity;
            float newOpacity = ve.resolvedStyle.opacity * parentCompositeOpacity;

            bool compositeOpacityChanged = Mathf.Abs(oldOpacity - newOpacity) > 0.0001f;
            if (compositeOpacityChanged)
            {
                // Avoid updating cached opacity if it changed too little, because we don't want slow changes to
                // update the cache and never trigger the compositeOpacityChanged condition.
                ve.renderChainData.compositeOpacity = newOpacity;
            }

            bool changedOpacityID = false;
            bool hasDistinctOpacity = newOpacity < parentCompositeOpacity - 0.0001f; //assume 0 <= opacity <= 1
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
            else if (oldOpacity < Mathf.Epsilon && newOpacity >= Mathf.Epsilon) // became visible
            {
                renderChain.UIEOnVisualsChanged(ve, true); // Force a full vertex regeneration, as this element was considered as hidden from the hierarchy
                isDoingFullVertexRegeneration = true;
            }
            else if (changedOpacityID && ((ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.Visuals) == 0))
            {
                renderChain.UIEOnVisualsChanged(ve, false); // Changed opacity ID, must update vertices.. we don't do it hierarchical here since our children will go through this too
            }

            if (compositeOpacityChanged || changedOpacityID)
            {
                // Recurse on children
                int childrenCount = ve.hierarchy.childCount;
                for (int i = 0; i < childrenCount; i++)
                {
                    DepthFirstOnOpacityChanged(renderChain, newOpacity, ve.hierarchy[i], dirtyID, ref stats,
                        isDoingFullVertexRegeneration);
                }
            }
        }

        static void DepthFirstOnTransformOrSizeChanged(RenderChain renderChain, VisualElement parent, VisualElement ve, uint dirtyID, UIRenderDevice device, bool isAncestorOfChangeSkinned, bool transformChanged, ref ChainBuilderStats stats)
        {
            if (dirtyID == ve.renderChainData.dirtyID)
                return;

            stats.recursiveTransformUpdatesExpanded++;

            transformChanged |= (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.Transform) != 0;

            if (RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID))
                renderChain.shaderInfoAllocator.SetClipRectValue(ve.renderChainData.clipRectID, GetClipRectIDClipInfo(ve));

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
            else if ((ve.renderHints & RenderHints.GroupTransform) != 0)
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
            else if ((ve.renderChainData.dirtiedValues & (RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy)) == 0 && (ve.renderChainData.data != null))
            {
                // If a visual update will happen, then skip work here as the visual update will incorporate the transformed vertices
                if (!ve.renderChainData.disableNudging && NudgeVerticesToNewSpace(ve, device))
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

            if ((ve.renderHints & RenderHints.GroupTransform) == 0)
            {
                // Recurse on children
                int childrenCount = ve.hierarchy.childCount;
                for (int i = 0; i < childrenCount; i++)
                    DepthFirstOnTransformOrSizeChanged(renderChain, ve, ve.hierarchy[i], dirtyID, device, isAncestorOfChangeSkinned, transformChanged, ref stats);
            }
            else
                renderChain.OnGroupTransformElementChangedTransform(ve); // Hack until UIE moves to TMP
        }

        static void DepthFirstOnVisualsChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, bool parentHierarchyHidden, bool hierarchical, ref ChainBuilderStats stats)
        {
            if (dirtyID == ve.renderChainData.dirtyID)
                return;
            ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            if (hierarchical)
                stats.recursiveVisualUpdatesExpanded++;

            bool wasHierarchyHidden = ve.renderChainData.isHierarchyHidden;
            ve.renderChainData.isHierarchyHidden = parentHierarchyHidden || IsElementHierarchyHidden(ve);
            if (wasHierarchyHidden != ve.renderChainData.isHierarchyHidden)
                hierarchical = true;

            Debug.Assert(ve.renderChainData.clipMethod != ClipMethod.Undetermined);
            Debug.Assert(RenderChainVEData.AllocatesID(ve.renderChainData.transformID) || ve.hierarchy.parent == null || ve.renderChainData.transformID.Equals(ve.hierarchy.parent.renderChainData.transformID) || (ve.renderHints & RenderHints.GroupTransform) != 0);

            UIRStylePainter.ClosingInfo closingInfo = PaintElement(renderChain, ve, ref stats);

            if (hierarchical)
            {
                // Recurse on children
                int childrenCount = ve.hierarchy.childCount;
                for (int i = 0; i < childrenCount; i++)
                    DepthFirstOnVisualsChanged(renderChain, ve.hierarchy[i], dirtyID, ve.renderChainData.isHierarchyHidden, true, ref stats);
            }

            // By closing the element after its children, we can ensure closing data is allocated
            // at a time that would maintain continuity in the index buffer
            if (closingInfo.needsClosing)
                ClosePaintElement(ve, closingInfo, renderChain, ref stats);
        }

        static bool IsElementHierarchyHidden(VisualElement ve)
        {
            return ve.resolvedStyle.opacity < Mathf.Epsilon || ve.resolvedStyle.display == DisplayStyle.None;
        }

        static bool IsElementSelfHidden(VisualElement ve)
        {
            return ve.resolvedStyle.visibility == Visibility.Hidden;
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

        static bool IsParentOrAncestorOf(this VisualElement ve, VisualElement child)
        {
            // O(n) of tree depth, not very cool
            while (child.hierarchy.parent != null)
            {
                if (child.hierarchy.parent == ve)
                    return true;
                child = child.hierarchy.parent;
            }
            return false;
        }

        static ClipMethod DetermineSelfClipMethod(RenderChain renderChain, VisualElement ve)
        {
            if (!ve.ShouldClip())
                return ClipMethod.NotClipped;

            if (!UIRUtility.IsRoundRect(ve) && !UIRUtility.IsVectorImageBackground(ve))
            {
                if ((ve.renderHints & (RenderHints.GroupTransform | RenderHints.ClipWithScissors)) != 0)
                    return ClipMethod.Scissor;
                return ClipMethod.ShaderDiscard;
            }

            if (ve.hierarchy.parent?.renderChainData.isStencilClipped == true)
                return ClipMethod.ShaderDiscard; // Prevent nested stenciling for now, even if inaccurate

            // Stencil clipping is not yet supported in world-space rendering, fallback to a coarse shader discard for now
            return renderChain.drawInCameras ? ClipMethod.ShaderDiscard : ClipMethod.Stencil;
        }

        static bool NeedsTransformID(VisualElement ve)
        {
            return ((ve.renderHints & RenderHints.GroupTransform) == 0) &&
                ((ve.renderHints & RenderHints.BoneTransform) == RenderHints.BoneTransform);
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

        internal static UIRStylePainter.ClosingInfo PaintElement(RenderChain renderChain, VisualElement ve, ref ChainBuilderStats stats)
        {
            var isClippingWithStencil = ve.renderChainData.clipMethod == ClipMethod.Stencil;
            if ((IsElementSelfHidden(ve) && !isClippingWithStencil) || ve.renderChainData.isHierarchyHidden)
            {
                if (ve.renderChainData.data != null)
                {
                    renderChain.painter.device.Free(ve.renderChainData.data);
                    ve.renderChainData.data = null;
                }
                if (ve.renderChainData.firstCommand != null)
                    ResetCommands(renderChain, ve);

                renderChain.ResetTextures(ve);

                return new UIRStylePainter.ClosingInfo();
            }

            // Retain our command insertion points if possible, to avoid paying the cost of finding them again
            RenderChainCommand oldCmdPrev = ve.renderChainData.firstCommand?.prev;
            RenderChainCommand oldCmdNext = ve.renderChainData.lastCommand?.next;
            RenderChainCommand oldClosingCmdPrev, oldClosingCmdNext;
            bool commandsAndClosingCommandsWereConsecutive = (ve.renderChainData.firstClosingCommand != null) && (oldCmdNext == ve.renderChainData.firstClosingCommand);
            if (commandsAndClosingCommandsWereConsecutive)
            {
                oldCmdNext = ve.renderChainData.lastClosingCommand.next;
                oldClosingCmdPrev = oldClosingCmdNext = null;
            }
            else
            {
                oldClosingCmdPrev = ve.renderChainData.firstClosingCommand?.prev;
                oldClosingCmdNext = ve.renderChainData.lastClosingCommand?.next;
            }
            Debug.Assert(oldCmdPrev?.owner != ve);
            Debug.Assert(oldCmdNext?.owner != ve);
            Debug.Assert(oldClosingCmdPrev?.owner != ve);
            Debug.Assert(oldClosingCmdNext?.owner != ve);

            ResetCommands(renderChain, ve);
            renderChain.ResetTextures(ve);

            var painter = renderChain.painter;
            painter.Begin(ve);

            if (ve.visible)
            {
                painter.DrawVisualElementBackground();
                painter.DrawVisualElementBorder();
                painter.ApplyVisualElementClipping();
                ve.InvokeGenerateVisualContent(painter.meshGenerationContext);
            }
            else
            {
                // Even though the element hidden, we still have to push the stencil shape in case any children are visible.
                if (ve.renderChainData.clipMethod == ClipMethod.Stencil)
                    painter.ApplyVisualElementClipping();
            }

            MeshHandle data = ve.renderChainData.data;

            if (painter.totalVertices > renderChain.device.maxVerticesPerPage)
            {
                Debug.LogError($"A {nameof(VisualElement)} must not allocate more than {renderChain.device.maxVerticesPerPage } vertices.");

                if (data != null)
                {
                    painter.device.Free(data);
                    data = null;
                }

                renderChain.ResetTextures(ve);

                // Restart without drawing anything.
                painter.Reset();
                painter.Begin(ve);
            }

            // Convert entries to commands.
            var entries = painter.entries;
            if (entries.Count > 0)
            {
                NativeSlice<Vertex> verts = new NativeSlice<Vertex>();
                NativeSlice<UInt16> indices = new NativeSlice<UInt16>();
                UInt16 indexOffset = 0;

                if (painter.totalVertices > 0)
                    UpdateOrAllocate(ref data, painter.totalVertices, painter.totalIndices, painter.device, out verts, out indices, out indexOffset, ref stats);

                int vertsFilled = 0, indicesFilled = 0;

                RenderChainCommand cmdPrev = oldCmdPrev, cmdNext = oldCmdNext;
                if (oldCmdPrev == null && oldCmdNext == null)
                    FindCommandInsertionPoint(ve, out cmdPrev, out cmdNext);

                // Vertex data, lazily computed
                bool vertexDataComputed = false;
                Matrix4x4 transform = Matrix4x4.identity;
                Color32 xformClipPages = new Color32(0, 0, 0, 0);
                Color32 idsAddFlags = new Color32(0, 0, 0, 0);
                Color32 opacityPage = new Color32(0, 0, 0, 0);

                int firstDisplacementUV = -1, lastDisplacementUVPlus1 = -1;
                foreach (var entry in painter.entries)
                {
                    if (entry.vertices.Length > 0 && entry.indices.Length > 0)
                    {
                        if (!vertexDataComputed)
                        {
                            vertexDataComputed = true;
                            GetVerticesTransformInfo(ve, out transform);
                            ve.renderChainData.verticesSpace = transform; // This is the space for the generated vertices below

                            Color32 transformData = renderChain.shaderInfoAllocator.TransformAllocToVertexData(ve.renderChainData.transformID);
                            Color32 opacityData = renderChain.shaderInfoAllocator.OpacityAllocToVertexData(ve.renderChainData.opacityID);
                            xformClipPages.r = transformData.r;
                            xformClipPages.g = transformData.g;
                            idsAddFlags.r = transformData.b;
                            opacityPage.r = opacityData.r;
                            opacityPage.g = opacityData.g;
                            idsAddFlags.b = opacityData.b;
                        }

                        Color32 clipRectData = renderChain.shaderInfoAllocator.ClipRectAllocToVertexData(entry.clipRectID);
                        xformClipPages.b = clipRectData.r;
                        xformClipPages.a = clipRectData.g;
                        idsAddFlags.g = clipRectData.b;
                        idsAddFlags.a = (byte)entry.addFlags;

                        float textureId = entry.texture.ConvertToGpu();

                        // Copy vertices, transforming them as necessary
                        var targetVerticesSlice = verts.Slice(vertsFilled, entry.vertices.Length);

                        if (entry.uvIsDisplacement)
                        {
                            if (firstDisplacementUV < 0)
                            {
                                firstDisplacementUV = vertsFilled;
                                lastDisplacementUVPlus1 = vertsFilled + entry.vertices.Length;
                            }
                            else if (lastDisplacementUVPlus1 == vertsFilled)
                                lastDisplacementUVPlus1 += entry.vertices.Length;
                            else ve.renderChainData.disableNudging = true; // Disjoint displacement UV entries, we can't keep track of them, so disable nudging optimization altogether

                            CopyTransformVertsPosAndVec(entry.vertices, targetVerticesSlice, transform, xformClipPages, idsAddFlags, opacityPage, textureId);
                        }
                        else CopyTransformVertsPos(entry.vertices, targetVerticesSlice, transform, xformClipPages, idsAddFlags, opacityPage, textureId);

                        // Copy indices
                        int entryIndexCount = entry.indices.Length;
                        int entryIndexOffset = vertsFilled + indexOffset;
                        var targetIndicesSlice = indices.Slice(indicesFilled, entryIndexCount);
                        if (entry.isClipRegisterEntry || !entry.isStencilClipped)
                            CopyTriangleIndices(entry.indices, targetIndicesSlice, entryIndexOffset);
                        else CopyTriangleIndicesFlipWindingOrder(entry.indices, targetIndicesSlice, entryIndexOffset); // Flip winding order if we're stencil-clipped

                        if (entry.isClipRegisterEntry)
                            painter.LandClipRegisterMesh(targetVerticesSlice, targetIndicesSlice, entryIndexOffset);

                        var cmd = InjectMeshDrawCommand(renderChain, ve, ref cmdPrev, ref cmdNext, data, entryIndexCount, indicesFilled, entry.material, entry.texture, entry.font);
                        if (entry.isTextEntry && ve.renderChainData.usesLegacyText)
                        {
                            if (ve.renderChainData.textEntries == null)
                                ve.renderChainData.textEntries = new List<RenderChainTextEntry>(1);
                            ve.renderChainData.textEntries.Add(new RenderChainTextEntry() { command = cmd, firstVertex = vertsFilled, vertexCount = entry.vertices.Length });
                        }

                        vertsFilled += entry.vertices.Length;
                        indicesFilled += entryIndexCount;
                    }
                    else if (entry.customCommand != null)
                    {
                        InjectCommandInBetween(renderChain, entry.customCommand, ref cmdPrev, ref cmdNext);
                    }
                    else
                    {
                        Debug.Assert(false); // Unable to determine what kind of command to generate here
                    }
                }

                if (!ve.renderChainData.disableNudging && (firstDisplacementUV >= 0))
                {
                    ve.renderChainData.displacementUVStart = firstDisplacementUV;
                    ve.renderChainData.displacementUVEnd = lastDisplacementUVPlus1;
                }
            }
            else if (data != null)
            {
                painter.device.Free(data);
                data = null;
            }
            ve.renderChainData.data = data;

            if (ve.renderChainData.usesLegacyText)
                renderChain.AddTextElement(ve);

            if (painter.closingInfo.clipperRegisterIndices.Length == 0 && ve.renderChainData.closingData != null)
            {
                // No more closing data needed, so free it now
                painter.device.Free(ve.renderChainData.closingData);
                ve.renderChainData.closingData = null;
            }

            if (painter.closingInfo.needsClosing)
            {
                RenderChainCommand cmdPrev = oldClosingCmdPrev, cmdNext = oldClosingCmdNext;
                if (commandsAndClosingCommandsWereConsecutive)
                {
                    cmdPrev = ve.renderChainData.lastCommand;
                    cmdNext = cmdPrev.next;
                }
                else if (cmdPrev == null && cmdNext == null)
                    FindClosingCommandInsertionPoint(ve, out cmdPrev, out cmdNext);

                if (painter.closingInfo.clipperRegisterIndices.Length > 0)
                    painter.LandClipUnregisterMeshDrawCommand(InjectClosingMeshDrawCommand(renderChain, ve, ref cmdPrev, ref cmdNext, null, 0, 0, null, TextureId.invalid, null)); // Placeholder command that will be filled actually later
                if (painter.closingInfo.popViewMatrix)
                {
                    var cmd = renderChain.AllocCommand();
                    cmd.type = CommandType.PopView;
                    cmd.closing = true;
                    cmd.owner = ve;
                    InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
                }
                if (painter.closingInfo.popScissorClip)
                {
                    var cmd = renderChain.AllocCommand();
                    cmd.type = CommandType.PopScissor;
                    cmd.closing = true;
                    cmd.owner = ve;
                    InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
                }
            }

            var closingInfo = painter.closingInfo;
            painter.Reset();
            return closingInfo;
        }

        static void ClosePaintElement(VisualElement ve, UIRStylePainter.ClosingInfo closingInfo, RenderChain renderChain, ref ChainBuilderStats stats)
        {
            if (closingInfo.clipperRegisterIndices.Length > 0)
            {
                NativeSlice<Vertex> verts = new NativeSlice<Vertex>();
                NativeSlice<UInt16> indices = new NativeSlice<UInt16>();
                UInt16 indexOffset = 0;

                // Due to device Update limitations, we cannot share the vertices of the registration mesh. It would be great
                // if we can just point winding-flipped indices towards the same vertices as the registration mesh.
                // For now, we duplicate the registration mesh entirely, wasting a bit of vertex memory
                UpdateOrAllocate(ref ve.renderChainData.closingData, closingInfo.clipperRegisterVertices.Length, closingInfo.clipperRegisterIndices.Length, renderChain.painter.device, out verts, out indices, out indexOffset, ref stats);
                verts.CopyFrom(closingInfo.clipperRegisterVertices);
                CopyTriangleIndicesFlipWindingOrder(closingInfo.clipperRegisterIndices, indices, indexOffset - closingInfo.clipperRegisterIndexOffset);
                closingInfo.clipUnregisterDrawCommand.mesh = ve.renderChainData.closingData;
                closingInfo.clipUnregisterDrawCommand.indexCount = indices.Length;
            }
        }

        static void UpdateOrAllocate(ref MeshHandle data, int vertexCount, int indexCount, UIRenderDevice device, out NativeSlice<Vertex> verts, out NativeSlice<UInt16> indices, out UInt16 indexOffset, ref ChainBuilderStats stats)
        {
            if (data != null)
            {
                // Try to fit within the existing allocation, optionally we can change the condition
                // to be an exact match of size to guarantee continuity in draw ranges
                if (data.allocVerts.size >= vertexCount && data.allocIndices.size >= indexCount)
                {
                    device.Update(data, (uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                    stats.updatedMeshAllocations++;
                }
                else
                {
                    // Won't fit in the existing allocated region, free the current one
                    device.Free(data);
                    data = device.Allocate((uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                    stats.newMeshAllocations++;
                }
            }
            else
            {
                data = device.Allocate((uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                stats.newMeshAllocations++;
            }
        }

        static void CopyTransformVertsPos(NativeSlice<Vertex> source, NativeSlice<Vertex> target, Matrix4x4 mat, Color32 xformClipPages, Color32 idsAddFlags, Color32 opacityPage, float textureId)
        {
            int count = source.Length;
            for (int i = 0; i < count; i++)
            {
                Vertex v = source[i];
                v.position = mat.MultiplyPoint3x4(v.position);
                v.xformClipPages = xformClipPages;
                v.idsFlags.r = idsAddFlags.r;
                v.idsFlags.g = idsAddFlags.g;
                v.idsFlags.b = idsAddFlags.b;
                v.idsFlags.a += idsAddFlags.a;
                v.opacityPageSVGSettingIndex.r = opacityPage.r;
                v.opacityPageSVGSettingIndex.g = opacityPage.g;
                v.textureId = textureId;
                target[i] = v;
            }
        }

        static void CopyTransformVertsPosAndVec(NativeSlice<Vertex> source, NativeSlice<Vertex> target, Matrix4x4 mat, Color32 xformClipPages, Color32 idsAddFlags, Color32 opacityPage, float textureId)
        {
            int count = source.Length;
            Vector3 vec = new Vector3(0, 0, UIRUtility.k_MeshPosZ);

            for (int i = 0; i < count; i++)
            {
                Vertex v = source[i];
                v.position = mat.MultiplyPoint3x4(v.position);
                vec.x = v.uv.x;
                vec.y = v.uv.y;
                v.uv = mat.MultiplyVector(vec);
                v.xformClipPages = xformClipPages;
                v.idsFlags.r = idsAddFlags.r;
                v.idsFlags.g = idsAddFlags.g;
                v.idsFlags.b = idsAddFlags.b;
                v.idsFlags.a += idsAddFlags.a;
                v.opacityPageSVGSettingIndex.r = opacityPage.r;
                v.opacityPageSVGSettingIndex.g = opacityPage.g;
                v.textureId = textureId;
                target[i] = v;
            }
        }

        static void CopyTriangleIndicesFlipWindingOrder(NativeSlice<UInt16> source, NativeSlice<UInt16> target)
        {
            Debug.Assert(source != target); // Not a very robust assert, but readers get the point
            int indexCount = source.Length;
            for (int i = 0; i < indexCount; i += 3)
            {
                // Using a temp variable to make reads from source sequential
                UInt16 t = source[i];
                target[i] = source[i + 1];
                target[i + 1] = t;
                target[i + 2] = source[i + 2];
            }
        }

        static void CopyTriangleIndicesFlipWindingOrder(NativeSlice<UInt16> source, NativeSlice<UInt16> target, int indexOffset)
        {
            Debug.Assert(source != target); // Not a very robust assert, but readers get the point
            int indexCount = source.Length;
            for (int i = 0; i < indexCount; i += 3)
            {
                // Using a temp variable to make reads from source sequential
                UInt16 t = (UInt16)(source[i] + indexOffset);
                target[i] = (UInt16)(source[i + 1] + indexOffset);
                target[i + 1] = t;
                target[i + 2] = (UInt16)(source[i + 2] + indexOffset);
            }
        }

        static void CopyTriangleIndices(NativeSlice<UInt16> source, NativeSlice<UInt16> target, int indexOffset)
        {
            int indexCount = source.Length;
            for (int i = 0; i < indexCount; i++)
                target[i] = (UInt16)(source[i] + indexOffset);
        }

        static bool NudgeVerticesToNewSpace(VisualElement ve, UIRenderDevice device)
        {
            Debug.Assert(!ve.renderChainData.disableNudging);

            Matrix4x4 newTransform;
            GetVerticesTransformInfo(ve, out newTransform);
            Matrix4x4 nudgeTransform = newTransform * ve.renderChainData.verticesSpace.inverse;

            // Attempt to reconstruct the absolute transform. If the result diverges from the absolute
            // considerably, then we assume that the vertices have become degenerate beyond restoration.
            // In this case we refuse to nudge, and ask for this element to be fully repainted to regenerate
            // the vertices without error.
            const float kMaxAllowedDeviation = 0.0001f;
            Matrix4x4 reconstructedNewTransform = nudgeTransform * ve.renderChainData.verticesSpace;
            float error;
            error  = Mathf.Abs(newTransform.m00 - reconstructedNewTransform.m00);
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
                return false;

            ve.renderChainData.verticesSpace = newTransform; // This is the new space of the vertices

            int vertCount = (int)ve.renderChainData.data.allocVerts.size;
            NativeSlice<Vertex> oldVerts = ve.renderChainData.data.allocPage.vertices.cpuData.Slice((int)ve.renderChainData.data.allocVerts.start, vertCount);
            NativeSlice<Vertex> newVerts;
            device.Update(ve.renderChainData.data, (uint)vertCount, out newVerts);

            int vertsBeforeUVDisplacement = ve.renderChainData.displacementUVStart;
            int vertsAfterUVDisplacement = ve.renderChainData.displacementUVEnd;

            // Position-only transform loop
            for (int i = 0; i < vertsBeforeUVDisplacement; i++)
            {
                var v = oldVerts[i];
                v.position = nudgeTransform.MultiplyPoint3x4(v.position);
                newVerts[i] = v;
            }

            // Position and UV transform loop
            for (int i = vertsBeforeUVDisplacement; i < vertsAfterUVDisplacement; i++)
            {
                var v = oldVerts[i];
                v.position = nudgeTransform.MultiplyPoint3x4(v.position);
                v.uv = nudgeTransform.MultiplyVector(v.uv);
                newVerts[i] = v;
            }

            // Position-only transform loop
            for (int i = vertsAfterUVDisplacement; i < vertCount; i++)
            {
                var v = oldVerts[i];
                v.position = nudgeTransform.MultiplyPoint3x4(v.position);
                newVerts[i] = v;
            }

            return true;
        }

        static RenderChainCommand InjectMeshDrawCommand(RenderChain renderChain, VisualElement ve, ref RenderChainCommand cmdPrev, ref RenderChainCommand cmdNext, MeshHandle mesh, int indexCount, int indexOffset, Material material, TextureId texture, Texture font)
        {
            var cmd = renderChain.AllocCommand();
            cmd.type = CommandType.Draw;
            cmd.state = new State { material = material, texture = texture, font = font };
            cmd.mesh = mesh;
            cmd.indexOffset = indexOffset;
            cmd.indexCount = indexCount;
            cmd.owner = ve;
            InjectCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
            return cmd;
        }

        static RenderChainCommand InjectClosingMeshDrawCommand(RenderChain renderChain, VisualElement ve, ref RenderChainCommand cmdPrev, ref RenderChainCommand cmdNext, MeshHandle mesh, int indexCount, int indexOffset, Material material, TextureId texture, Texture font)
        {
            var cmd = renderChain.AllocCommand();
            cmd.type = CommandType.Draw;
            cmd.closing = true;
            cmd.state = new State { material = material, texture = texture, font = font };
            cmd.mesh = mesh;
            cmd.indexOffset = indexOffset;
            cmd.indexCount = indexCount;
            cmd.owner = ve;
            InjectClosingCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
            return cmd;
        }

        static void FindCommandInsertionPoint(VisualElement ve, out RenderChainCommand prev, out RenderChainCommand next)
        {
            VisualElement prevDrawingElem = ve.renderChainData.prev;

            // This can be potentially O(n) of VE count
            // It is ok to check against lastCommand to mean the presence of closingCommand too, as we
            // require that closing commands only exist if a startup command exists too
            while (prevDrawingElem != null && prevDrawingElem.renderChainData.lastCommand == null)
                prevDrawingElem = prevDrawingElem.renderChainData.prev;

            if (prevDrawingElem != null && prevDrawingElem.renderChainData.lastCommand != null)
            {
                // A previous drawing element can be:
                // A) A previous sibling (O(1) check time)
                // B) A parent/ancestor (O(n) of tree depth check time - meh)
                // C) A child/grand-child of a previous sibling to an ancestor (lengthy check time, so it is left as the only choice remaining after the first two)
                if (prevDrawingElem.hierarchy.parent == ve.hierarchy.parent) // Case A
                    prev = prevDrawingElem.renderChainData.lastClosingOrLastCommand;
                else if (prevDrawingElem.IsParentOrAncestorOf(ve)) // Case B
                    prev = prevDrawingElem.renderChainData.lastCommand;
                else
                {
                    // Case C, get the last command that isn't owned by us, this is to skip potential
                    // closing commands wrapped after the previous drawing element
                    var lastCommand = prevDrawingElem.renderChainData.lastClosingOrLastCommand;
                    for (;;)
                    {
                        prev = lastCommand;
                        lastCommand = lastCommand.next;
                        if (lastCommand == null || (lastCommand.owner == ve) || !lastCommand.closing) // Once again, we assume closing commands cannot exist without opening commands on the element
                            break;
                        if (lastCommand.owner.IsParentOrAncestorOf(ve))
                            break;
                    }
                }

                next = prev.next;
            }
            else
            {
                VisualElement nextDrawingElem = ve.renderChainData.next;
                // This can be potentially O(n) of VE count, very bad.. must adjust
                while (nextDrawingElem != null && nextDrawingElem.renderChainData.firstCommand == null)
                    nextDrawingElem = nextDrawingElem.renderChainData.next;
                next = nextDrawingElem?.renderChainData.firstCommand;
                prev = null;
                Debug.Assert((next == null) || (next.prev == null));
            }
        }

        static void FindClosingCommandInsertionPoint(VisualElement ve, out RenderChainCommand prev, out RenderChainCommand next)
        {
            // Closing commands for a visual element come after the closing commands of the shallowest child
            // If not found, then after the last command of the last deepest child
            // If not found, then after the last command of self

            VisualElement nextDrawingElem = ve.renderChainData.next;

            // Depth first search for the first VE that has a command (i.e. non empty element).
            // This can be potentially O(n) of VE count
            // It is ok to check against lastCommand to mean the presence of closingCommand too, as we
            // require that closing commands only exist if a startup command exists too
            while (nextDrawingElem != null && nextDrawingElem.renderChainData.firstCommand == null)
                nextDrawingElem = nextDrawingElem.renderChainData.next;

            if (nextDrawingElem != null && nextDrawingElem.renderChainData.firstCommand != null)
            {
                // A next drawing element can be:
                // A) A next sibling of ve (O(1) check time)
                // B) A child/grand-child of self (O(n) of tree depth check time - meh)
                // C) A next sibling of a parent/ancestor (lengthy check time, so it is left as the only choice remaining after the first two)
                if (nextDrawingElem.hierarchy.parent == ve.hierarchy.parent) // Case A
                {
                    next = nextDrawingElem.renderChainData.firstCommand;
                    prev = next.prev;
                }
                else if (ve.IsParentOrAncestorOf(nextDrawingElem)) // Case B
                {
                    // Enclose the last deepest drawing child by our closing command
                    for (;;)
                    {
                        prev = nextDrawingElem.renderChainData.lastClosingOrLastCommand;
                        nextDrawingElem = prev.next?.owner;
                        if (nextDrawingElem == null || !ve.IsParentOrAncestorOf(nextDrawingElem))
                            break;
                    }
                    next = prev.next;
                }
                else
                {
                    // Case C, just wrap ourselves
                    prev = ve.renderChainData.lastCommand;
                    next = prev.next;
                }
            }
            else
            {
                prev = ve.renderChainData.lastCommand;
                next = prev.next; // prev should not be null since we don't support closing commands without opening commands too
            }
        }

        static void InjectCommandInBetween(RenderChain renderChain, RenderChainCommand cmd, ref RenderChainCommand prev, ref RenderChainCommand next)
        {
            if (prev != null)
            {
                cmd.prev = prev;
                prev.next = cmd;
            }
            if (next != null)
            {
                cmd.next = next;
                next.prev = cmd;
            }

            VisualElement ve = cmd.owner;
            ve.renderChainData.lastCommand = cmd;
            if (ve.renderChainData.firstCommand == null)
                ve.renderChainData.firstCommand = cmd;
            renderChain.OnRenderCommandAdded(cmd);

            // Adjust the pointers as a facility for later injections
            prev = cmd;
            next = cmd.next;
        }

        static void InjectClosingCommandInBetween(RenderChain renderChain, RenderChainCommand cmd, ref RenderChainCommand prev, ref RenderChainCommand next)
        {
            Debug.Assert(cmd.closing);
            if (prev != null)
            {
                cmd.prev = prev;
                prev.next = cmd;
            }
            if (next != null)
            {
                cmd.next = next;
                next.prev = cmd;
            }

            VisualElement ve = cmd.owner;
            ve.renderChainData.lastClosingCommand = cmd;
            if (ve.renderChainData.firstClosingCommand == null)
                ve.renderChainData.firstClosingCommand = cmd;

            renderChain.OnRenderCommandAdded(cmd);

            // Adjust the pointers as a facility for later injections
            prev = cmd;
            next = cmd.next;
        }

        static void ResetCommands(RenderChain renderChain, VisualElement ve)
        {
            if (ve.renderChainData.firstCommand != null)
                renderChain.OnRenderCommandsRemoved(ve.renderChainData.firstCommand, ve.renderChainData.lastCommand);

            var prev = ve.renderChainData.firstCommand != null ? ve.renderChainData.firstCommand.prev : null;
            var next = ve.renderChainData.lastCommand != null ? ve.renderChainData.lastCommand.next : null;
            Debug.Assert(prev == null || prev.owner != ve);
            Debug.Assert(next == null || next == ve.renderChainData.firstClosingCommand || next.owner != ve);
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            if (ve.renderChainData.firstCommand != null)
            {
                var c = ve.renderChainData.firstCommand;
                while (c != ve.renderChainData.lastCommand)
                {
                    var nextC = c.next;
                    renderChain.FreeCommand(c);
                    c = nextC;
                }
                renderChain.FreeCommand(c); // Last command
            }
            ve.renderChainData.firstCommand = ve.renderChainData.lastCommand = null;

            prev = ve.renderChainData.firstClosingCommand != null ? ve.renderChainData.firstClosingCommand.prev : null;
            next = ve.renderChainData.lastClosingCommand != null ? ve.renderChainData.lastClosingCommand.next : null;
            Debug.Assert(prev == null || prev.owner != ve);
            Debug.Assert(next == null || next.owner != ve);
            if (prev != null) prev.next = next;
            if (next != null) next.prev = prev;

            if (ve.renderChainData.firstClosingCommand != null)
            {
                renderChain.OnRenderCommandsRemoved(ve.renderChainData.firstClosingCommand, ve.renderChainData.lastClosingCommand);

                var c = ve.renderChainData.firstClosingCommand;
                while (c != ve.renderChainData.lastClosingCommand)
                {
                    var nextC = c.next;
                    renderChain.FreeCommand(c);
                    c = nextC;
                }
                renderChain.FreeCommand(c); // Last closing command
            }
            ve.renderChainData.firstClosingCommand = ve.renderChainData.lastClosingCommand = null;

            if (ve.renderChainData.usesLegacyText)
            {
                Debug.Assert(ve.renderChainData.textEntries.Count > 0);
                renderChain.RemoveTextElement(ve);
                ve.renderChainData.textEntries.Clear();
                ve.renderChainData.usesLegacyText = false;
            }
        }
    }
}
