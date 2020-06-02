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
                transform = ve.renderChainData.groupTransformAncestor.worldTransform.inverse * ve.worldTransform;
            else transform = ve.worldTransform;
            transform.m22 = transform.m33 = 1.0f; // Once world-space mode is introduced, this should become conditional
            return transform;
        }

        static Vector4 GetClipRectIDClipInfo(VisualElement ve)
        {
            Debug.Assert(RenderChainVEData.AllocatesID(ve.renderChainData.clipRectID));
            if (ve.renderChainData.groupTransformAncestor == null)
                return UIRUtility.ToVector4(ve.worldClip);

            Rect rect = ve.worldClipMinusGroup;
            // Subtract the transform of the group transform ancestor
            var transform = ve.renderChainData.groupTransformAncestor.worldTransform.inverse;
            var min = transform.MultiplyPoint3x4(new Vector3(rect.xMin, rect.yMin, 0));
            var max = transform.MultiplyPoint3x4(new Vector3(rect.xMax, rect.yMax, 0));

            return new Vector4(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        static void GetVerticesTransformInfo(VisualElement ve, out Matrix4x4 transform)
        {
            if (RenderChainVEData.AllocatesID(ve.renderChainData.transformID) || (ve.renderHints & (RenderHints.GroupTransform)) != 0)
                transform = Matrix4x4.identity;
            else if (ve.renderChainData.boneTransformAncestor != null)
                transform = ve.renderChainData.boneTransformAncestor.worldTransform.inverse * ve.worldTransform;
            else if (ve.renderChainData.groupTransformAncestor != null)
                transform = ve.renderChainData.groupTransformAncestor.worldTransform.inverse * ve.worldTransform;
            else transform = ve.worldTransform;
            transform.m22 = transform.m33 = 1.0f; // Once world-space mode is introduced, this should become conditional
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

            if (painter.totalVertices > 1 << 16)
            {
                Debug.LogError($"A {nameof(VisualElement)} must not allocate more than 65536 vertices.");

                if (data != null)
                {
                    painter.device.Free(data);
                    data = null;
                }

                // Restart without drawing anything.
                painter.Reset();
                painter.Begin(ve);
            }

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

                            CopyTransformVertsPosAndVec(entry.vertices, targetVerticesSlice, transform, xformClipPages, idsAddFlags, opacityPage);
                        }
                        else CopyTransformVertsPos(entry.vertices, targetVerticesSlice, transform, xformClipPages, idsAddFlags, opacityPage);

                        // Copy indices
                        int entryIndexCount = entry.indices.Length;
                        int entryIndexOffset = vertsFilled + indexOffset;
                        var targetIndicesSlice = indices.Slice(indicesFilled, entryIndexCount);
                        if (entry.isClipRegisterEntry || !entry.isStencilClipped)
                            CopyTriangleIndices(entry.indices, targetIndicesSlice, entryIndexOffset);
                        else CopyTriangleIndicesFlipWindingOrder(entry.indices, targetIndicesSlice, entryIndexOffset); // Flip winding order if we're stencil-clipped

                        if (entry.isClipRegisterEntry)
                            painter.LandClipRegisterMesh(targetVerticesSlice, targetIndicesSlice, entryIndexOffset);

                        var cmd = InjectMeshDrawCommand(renderChain, ve, ref cmdPrev, ref cmdNext, data, entryIndexCount, indicesFilled, entry.material, entry.custom, entry.font);
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
                    painter.LandClipUnregisterMeshDrawCommand(InjectClosingMeshDrawCommand(renderChain, ve, ref cmdPrev, ref cmdNext, null, 0, 0, null, null, null)); // Placeholder command that will be filled actually later
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

        static void CopyTransformVertsPos(NativeSlice<Vertex> source, NativeSlice<Vertex> target, Matrix4x4 mat, Color32 xformClipPages, Color32 idsAddFlags, Color32 opacityPage)
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
                target[i] = v;
            }
        }

        static void CopyTransformVertsPosAndVec(NativeSlice<Vertex> source, NativeSlice<Vertex> target, Matrix4x4 mat, Color32 xformClipPages, Color32 idsAddFlags, Color32 opacityPage)
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

        static RenderChainCommand InjectMeshDrawCommand(RenderChain renderChain, VisualElement ve, ref RenderChainCommand cmdPrev, ref RenderChainCommand cmdNext, MeshHandle mesh, int indexCount, int indexOffset, Material material, Texture custom, Texture font)
        {
            var cmd = renderChain.AllocCommand();
            cmd.type = CommandType.Draw;
            cmd.state = new State() { material = material, custom = custom, font = font };
            cmd.mesh = mesh;
            cmd.indexOffset = indexOffset;
            cmd.indexCount = indexCount;
            cmd.owner = ve;
            InjectCommandInBetween(renderChain, cmd, ref cmdPrev, ref cmdNext);
            return cmd;
        }

        static RenderChainCommand InjectClosingMeshDrawCommand(RenderChain renderChain, VisualElement ve, ref RenderChainCommand cmdPrev, ref RenderChainCommand cmdNext, MeshHandle mesh, int indexCount, int indexOffset, Material material, Texture custom, Texture font)
        {
            var cmd = renderChain.AllocCommand();
            cmd.type = CommandType.Draw;
            cmd.closing = true;
            cmd.state = new State() { material = material, custom = custom, font = font };
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

    internal class UIRStylePainter : IStylePainter, IDisposable
    {
        internal struct Entry
        {
            public NativeSlice<Vertex> vertices;
            public NativeSlice<UInt16> indices;
            public Material material; // Responsible for enabling immediate clipping
            public Texture custom, font;
            public RenderChainCommand customCommand;
            public BMPAlloc clipRectID;
            public VertexFlags addFlags;
            public bool uvIsDisplacement;
            public bool isTextEntry;
            public bool isClipRegisterEntry;
            public bool isStencilClipped;
        }

        internal struct ClosingInfo
        {
            public bool needsClosing;
            public bool popViewMatrix;
            public bool popScissorClip;
            public RenderChainCommand clipUnregisterDrawCommand;
            public NativeSlice<Vertex> clipperRegisterVertices;
            public NativeSlice<UInt16> clipperRegisterIndices;
            public int clipperRegisterIndexOffset;
        }

        internal struct TempDataAlloc<T> : IDisposable where T : struct
        {
            int maxPoolElemCount; // Requests larger than this will potentially be served individually without pooling
            NativeArray<T> pool;
            List<NativeArray<T>> excess;
            uint takenFromPool;

            public TempDataAlloc(int maxPoolElems)
            {
                maxPoolElemCount = maxPoolElems;
                pool = new NativeArray<T>();
                excess = new List<NativeArray<T>>();
                takenFromPool = 0;
            }

            public void Dispose()
            {
                foreach (var e in excess)
                    e.Dispose();
                excess.Clear();
                if (pool.IsCreated)
                    pool.Dispose();
            }

            internal NativeSlice<T> Alloc(uint count)
            {
                if (takenFromPool + count <= pool.Length)
                {
                    NativeSlice<T> slice = pool.Slice((int)takenFromPool, (int)count);
                    takenFromPool += count;
                    return slice;
                }

                var exceeding = new NativeArray<T>((int)count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                excess.Add(exceeding);
                return exceeding;
            }

            internal void SessionDone()
            {
                int totalNewSize = pool.Length;
                foreach (var e in excess)
                {
                    if (e.Length < maxPoolElemCount)
                        totalNewSize += e.Length;
                    e.Dispose();
                }
                excess.Clear();
                if (totalNewSize > pool.Length)
                {
                    if (pool.IsCreated)
                        pool.Dispose();
                    pool = new NativeArray<T>(totalNewSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                }
                takenFromPool = 0;
            }
        }

        RenderChain m_Owner;
        List<Entry> m_Entries = new List<Entry>();
        UIRAtlasManager m_AtlasManager;
        VectorImageManager m_VectorImageManager;
        Entry m_CurrentEntry;
        ClosingInfo m_ClosingInfo;
        bool m_StencilClip = false;
        BMPAlloc m_ClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
        int m_SVGBackgroundEntryIndex = -1;
        TempDataAlloc<Vertex> m_VertsPool = new TempDataAlloc<Vertex>(8192);
        TempDataAlloc<UInt16> m_IndicesPool = new TempDataAlloc<UInt16>(8192 << 1);
        List<MeshWriteData> m_MeshWriteDataPool;
        int m_NextMeshWriteDataPoolItem;

        // The delegates must be stored to avoid allocations
        MeshBuilder.AllocMeshData.Allocator m_AllocRawVertsIndicesDelegate;
        MeshBuilder.AllocMeshData.Allocator m_AllocThroughDrawMeshDelegate;

        MeshWriteData GetPooledMeshWriteData()
        {
            if (m_NextMeshWriteDataPoolItem == m_MeshWriteDataPool.Count)
                m_MeshWriteDataPool.Add(new MeshWriteData());
            return m_MeshWriteDataPool[m_NextMeshWriteDataPoolItem++];
        }

        MeshWriteData AllocRawVertsIndices(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            m_CurrentEntry.vertices = m_VertsPool.Alloc(vertexCount);
            m_CurrentEntry.indices = m_IndicesPool.Alloc(indexCount);
            var mwd = GetPooledMeshWriteData();
            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices);
            return mwd;
        }

        MeshWriteData AllocThroughDrawMesh(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            return DrawMesh((int)vertexCount, (int)indexCount, allocatorData.texture, allocatorData.material, allocatorData.flags);
        }

        public UIRStylePainter(RenderChain renderChain)
        {
            m_Owner = renderChain;
            meshGenerationContext = new MeshGenerationContext(this);
            device = renderChain.device;
            m_AtlasManager = renderChain.atlasManager;
            m_VectorImageManager = renderChain.vectorImageManager;
            m_AllocRawVertsIndicesDelegate = AllocRawVertsIndices;
            m_AllocThroughDrawMeshDelegate = AllocThroughDrawMesh;
            int meshWriteDataPoolStartingSize = 32;
            m_MeshWriteDataPool = new List<MeshWriteData>(meshWriteDataPoolStartingSize);
            for (int i = 0; i < meshWriteDataPoolStartingSize; i++)
                m_MeshWriteDataPool.Add(new MeshWriteData());
        }

        public MeshGenerationContext meshGenerationContext { get; }
        public VisualElement currentElement { get; private set; }
        public UIRenderDevice device { get; }
        public List<Entry> entries { get { return m_Entries; } }
        public ClosingInfo closingInfo { get { return m_ClosingInfo; } }
        public int totalVertices { get; private set; }
        public int totalIndices { get; private set; }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_IndicesPool.Dispose();
                m_VertsPool.Dispose();
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public void Begin(VisualElement ve)
        {
            currentElement = ve;
            m_NextMeshWriteDataPoolItem = 0;
            m_SVGBackgroundEntryIndex = -1;
            currentElement.renderChainData.usesLegacyText = currentElement.renderChainData.usesAtlas = currentElement.renderChainData.disableNudging = false;
            currentElement.renderChainData.displacementUVStart = currentElement.renderChainData.displacementUVEnd = 0;
            bool isGroupTransform = (currentElement.renderHints & RenderHints.GroupTransform) != 0;
            if (isGroupTransform)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushView;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popViewMatrix = true;
            }
            if (currentElement.hierarchy.parent != null)
            {
                m_StencilClip = currentElement.hierarchy.parent.renderChainData.isStencilClipped;
                m_ClipRectID = isGroupTransform ? UIRVEShaderInfoAllocator.infiniteClipRect : currentElement.hierarchy.parent.renderChainData.clipRectID;
            }
            else
            {
                m_StencilClip = false;
                m_ClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
            }
        }

        public void LandClipUnregisterMeshDrawCommand(RenderChainCommand cmd)
        {
            Debug.Assert(m_ClosingInfo.needsClosing);
            m_ClosingInfo.clipUnregisterDrawCommand = cmd;
        }

        public void LandClipRegisterMesh(NativeSlice<Vertex> vertices, NativeSlice<UInt16> indices, int indexOffset)
        {
            Debug.Assert(m_ClosingInfo.needsClosing);
            m_ClosingInfo.clipperRegisterVertices = vertices;
            m_ClosingInfo.clipperRegisterIndices = indices;
            m_ClosingInfo.clipperRegisterIndexOffset = indexOffset;
        }

        public MeshWriteData DrawMesh(int vertexCount, int indexCount, Texture texture, Material material, MeshGenerationContext.MeshFlags flags)
        {
            var mwd = GetPooledMeshWriteData();
            if (vertexCount == 0 || indexCount == 0)
            {
                mwd.Reset(new NativeSlice<Vertex>(), new NativeSlice<ushort>());
                return mwd;
            }

            m_CurrentEntry = new Entry()
            {
                vertices = m_VertsPool.Alloc((uint)vertexCount),
                indices = m_IndicesPool.Alloc((uint)indexCount),
                material = material,
                uvIsDisplacement = flags == MeshGenerationContext.MeshFlags.UVisDisplacement,
                clipRectID = m_ClipRectID,
                isStencilClipped = m_StencilClip,
                addFlags = VertexFlags.IsSolid
            };

            Debug.Assert(m_CurrentEntry.vertices.Length == vertexCount);
            Debug.Assert(m_CurrentEntry.indices.Length == indexCount);

            Rect uvRegion = new Rect(0, 0, 1, 1);
            bool isSVGGradients = flags == MeshGenerationContext.MeshFlags.IsSVGGradients;
            bool isCustomSVGGradients = flags == MeshGenerationContext.MeshFlags.IsCustomSVGGradients;
            if (isSVGGradients || isCustomSVGGradients)
            {
                m_CurrentEntry.addFlags = isSVGGradients ? VertexFlags.IsSVGGradients : VertexFlags.IsCustomSVGGradients;
                if (isCustomSVGGradients)
                    m_CurrentEntry.custom = texture;
                currentElement.renderChainData.usesAtlas = true;
            }
            else if (texture != null)
            {
                // Attempt to override with an atlas.
                RectInt atlasRect;
                if (m_AtlasManager != null && m_AtlasManager.TryGetLocation(texture as Texture2D, out atlasRect))
                {
                    m_CurrentEntry.addFlags = texture.filterMode == FilterMode.Point ? VertexFlags.IsAtlasTexturedPoint : VertexFlags.IsAtlasTexturedBilinear;
                    currentElement.renderChainData.usesAtlas = true;
                    uvRegion = new Rect(atlasRect.x, atlasRect.y, atlasRect.width, atlasRect.height);
                }
                else
                {
                    m_CurrentEntry.addFlags = VertexFlags.IsCustomTextured;
                    m_CurrentEntry.custom = texture;
                }
            }

            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices, uvRegion);
            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
            return mwd;
        }

        public void DrawText(MeshGenerationContextUtils.TextParams textParams, TextHandle handle, float pixelsPerPoint)
        {
            if (textParams.font == null)
                return;

            if (currentElement.panel.contextType == ContextType.Editor)
                textParams.fontColor *= textParams.playmodeTintColor;

            if (handle.useLegacy)
                DrawTextNative(textParams, handle, pixelsPerPoint);
            else
                DrawTextCore(textParams, handle, pixelsPerPoint);
        }

        void DrawTextNative(MeshGenerationContextUtils.TextParams textParams, TextHandle handle, float pixelsPerPoint)
        {
            float scaling = TextHandle.ComputeTextScaling(currentElement.worldTransform, pixelsPerPoint);
            TextNativeSettings textSettings = MeshGenerationContextUtils.TextParams.GetTextNativeSettings(textParams, scaling);

            using (NativeArray<TextVertex> textVertices = TextNative.GetVertices(textSettings))
            {
                if (textVertices.Length == 0)
                    return;

                Vector2 localOffset = TextNative.GetOffset(textSettings, textParams.rect);
                m_CurrentEntry.isTextEntry = true;
                m_CurrentEntry.clipRectID = m_ClipRectID;
                m_CurrentEntry.isStencilClipped = m_StencilClip;
                MeshBuilder.MakeText(textVertices, localOffset,  new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate });
                m_CurrentEntry.font = textParams.font.material.mainTexture;
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_CurrentEntry = new Entry();
                currentElement.renderChainData.usesLegacyText = true;
                currentElement.renderChainData.disableNudging = true;
            }
        }

        void DrawTextCore(MeshGenerationContextUtils.TextParams textParams, TextHandle handle, float pixelsPerPoint)
        {
            var textInfo = handle.Update(textParams, pixelsPerPoint);
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].vertexCount == 0)
                    return;
                m_CurrentEntry.isTextEntry = true;
                m_CurrentEntry.clipRectID = m_ClipRectID;
                m_CurrentEntry.isStencilClipped = m_StencilClip;
                MeshBuilder.MakeText(textInfo.meshInfo[i], textParams.rect.min,  new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate });
                m_CurrentEntry.font = textInfo.meshInfo[i].material.mainTexture;
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_CurrentEntry = new Entry();
            }
        }

        public void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            if (currentElement.panel.contextType == ContextType.Editor)
                rectParams.color *= rectParams.playmodeTintColor;

            var meshAlloc = new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                texture = rectParams.texture,
                material = rectParams.material
            };

            if (rectParams.vectorImage != null)
                DrawVectorImage(rectParams);
            else if (rectParams.texture != null)
                MeshBuilder.MakeTexturedRect(rectParams, UIRUtility.k_MeshPosZ, meshAlloc);
            else
                MeshBuilder.MakeSolidRect(rectParams, UIRUtility.k_MeshPosZ, meshAlloc);
        }

        public void DrawBorder(MeshGenerationContextUtils.BorderParams borderParams)
        {
            if (currentElement.panel.contextType == ContextType.Editor)
            {
                borderParams.leftColor *= borderParams.playmodeTintColor;
                borderParams.topColor *= borderParams.playmodeTintColor;
                borderParams.rightColor *= borderParams.playmodeTintColor;
                borderParams.bottomColor *= borderParams.playmodeTintColor;
            }

            MeshBuilder.MakeBorder(borderParams, UIRUtility.k_MeshPosZ, new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                material = borderParams.material,
                texture = null,
                flags = MeshGenerationContext.MeshFlags.UVisDisplacement
            });
        }

        public void DrawImmediate(Action callback, bool cullingEnabled)
        {
            var cmd = m_Owner.AllocCommand();
            cmd.type = cullingEnabled ? CommandType.ImmediateCull : CommandType.Immediate;
            cmd.owner = currentElement;
            cmd.callback = callback;
            m_Entries.Add(new Entry() { customCommand = cmd });
        }

        public VisualElement visualElement { get { return currentElement; } }

        public void DrawVisualElementBackground()
        {
            if (currentElement.layout.width <= Mathf.Epsilon || currentElement.layout.height <= Mathf.Epsilon)
                return;

            var style = currentElement.computedStyle;
            if (style.backgroundColor != Color.clear)
            {
                // Draw solid color background
                var rectParams = new MeshGenerationContextUtils.RectangleParams
                {
                    rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                    color = style.backgroundColor.value,
                    playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                };
                MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                    out rectParams.topLeftRadius,
                    out rectParams.bottomLeftRadius,
                    out rectParams.topRightRadius,
                    out rectParams.bottomRightRadius);
                DrawRectangle(rectParams);
            }

            var background = style.backgroundImage.value;
            if (background.texture != null || background.vectorImage != null)
            {
                // Draw background image (be it from a texture or a vector image)
                var rectParams = new MeshGenerationContextUtils.RectangleParams();
                if (background.texture != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        new Rect(0, 0, 1, 1),
                        background.texture,
                        style.unityBackgroundScaleMode.value,
                        currentElement.panel.contextType);
                }
                else if (background.vectorImage != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeVectorTextured(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        new Rect(0, 0, 1, 1),
                        background.vectorImage,
                        style.unityBackgroundScaleMode.value,
                        currentElement.panel.contextType);
                }

                MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                    out rectParams.topLeftRadius,
                    out rectParams.bottomLeftRadius,
                    out rectParams.topRightRadius,
                    out rectParams.bottomRightRadius);
                rectParams.leftSlice = style.unitySliceLeft.value;
                rectParams.topSlice = style.unitySliceTop.value;
                rectParams.rightSlice = style.unitySliceRight.value;
                rectParams.bottomSlice = style.unitySliceBottom.value;
                if (style.unityBackgroundImageTintColor != Color.clear)
                    rectParams.color = style.unityBackgroundImageTintColor.value;
                DrawRectangle(rectParams);
            }
        }

        public void DrawVisualElementBorder()
        {
            if (currentElement.layout.width >= Mathf.Epsilon && currentElement.layout.height >= Mathf.Epsilon)
            {
                var style = currentElement.computedStyle;
                if (style.borderLeftColor != Color.clear && style.borderLeftWidth.value > 0.0f ||
                    style.borderTopColor != Color.clear && style.borderTopWidth.value > 0.0f ||
                    style.borderRightColor != Color.clear &&  style.borderRightWidth.value > 0.0f ||
                    style.borderBottomColor != Color.clear && style.borderBottomWidth.value > 0.0f)
                {
                    var borderParams = new MeshGenerationContextUtils.BorderParams
                    {
                        rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                        leftColor = style.borderLeftColor.value,
                        topColor = style.borderTopColor.value,
                        rightColor = style.borderRightColor.value,
                        bottomColor = style.borderBottomColor.value,
                        leftWidth = style.borderLeftWidth.value,
                        topWidth = style.borderTopWidth.value,
                        rightWidth = style.borderRightWidth.value,
                        bottomWidth = style.borderBottomWidth.value,
                        playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                    };
                    MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                        out borderParams.topLeftRadius,
                        out borderParams.bottomLeftRadius,
                        out borderParams.topRightRadius,
                        out borderParams.bottomRightRadius);
                    DrawBorder(borderParams);
                }
            }
        }

        public void ApplyVisualElementClipping()
        {
            if (currentElement.renderChainData.clipMethod == ClipMethod.Scissor)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.type = CommandType.PushScissor;
                cmd.owner = currentElement;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popScissorClip = true;
            }
            else if (currentElement.renderChainData.clipMethod == ClipMethod.Stencil)
            {
                if (UIRUtility.IsVectorImageBackground(currentElement))
                    GenerateStencilClipEntryForSVGBackground();
                else GenerateStencilClipEntryForRoundedRectBackground();
            }
            m_ClipRectID = currentElement.renderChainData.clipRectID;
        }

        public void DrawVectorImage(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            var vi = rectParams.vectorImage;
            Debug.Assert(vi != null);

            VertexFlags vertexFlags = vi.atlas != null ? VertexFlags.IsSVGGradients : VertexFlags.IsSolid;
            int settingIndexOffset = 0;

            // If the vetor image has embedded textures/gradients, register them in our atlases
            if (vi.atlas != null && m_VectorImageManager != null)
            {
                var gradientRemap = m_VectorImageManager.AddUser(vi);
                vertexFlags = gradientRemap.isAtlassed ? VertexFlags.IsSVGGradients : VertexFlags.IsCustomSVGGradients;
                settingIndexOffset = gradientRemap.destIndex;
            }

            int entryCountBeforeSVG = m_Entries.Count;

            MeshGenerationContext.MeshFlags meshFlags = MeshGenerationContext.MeshFlags.None;
            if (vertexFlags == VertexFlags.IsSVGGradients)
                meshFlags = MeshGenerationContext.MeshFlags.IsSVGGradients;
            else if (vertexFlags == VertexFlags.IsCustomSVGGradients)
                meshFlags = MeshGenerationContext.MeshFlags.IsCustomSVGGradients;

            var meshAlloc = new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                texture = (vertexFlags == VertexFlags.IsCustomSVGGradients) ? vi.atlas : null,
                flags = meshFlags
            };

            int finalVertexCount;
            int finalIndexCount;
            MeshBuilder.MakeVectorGraphics(rectParams, settingIndexOffset, meshAlloc, out finalVertexCount, out finalIndexCount);

            Debug.Assert(entryCountBeforeSVG <= m_Entries.Count + 1);
            if (entryCountBeforeSVG != m_Entries.Count)
            {
                m_SVGBackgroundEntryIndex = m_Entries.Count - 1;
                if (finalVertexCount != 0 && finalIndexCount != 0)
                {
                    var svgEntry = m_Entries[m_SVGBackgroundEntryIndex];
                    svgEntry.vertices = svgEntry.vertices.Slice(0, finalVertexCount);
                    svgEntry.indices = svgEntry.indices.Slice(0, finalIndexCount);
                    m_Entries[m_SVGBackgroundEntryIndex] = svgEntry;
                }
            }
        }

        internal void Reset()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            ValidateMeshWriteData();

            m_Entries.Clear(); // Doesn't shrink, good
            m_VertsPool.SessionDone();
            m_IndicesPool.SessionDone();
            m_ClosingInfo = new ClosingInfo();
            m_NextMeshWriteDataPoolItem = 0;
            currentElement = null;
            totalVertices = totalIndices = 0;
        }

        void ValidateMeshWriteData()
        {
            // Loop through the used MeshWriteData and make sure the number of indices/vertices were properly filled.
            // Otherwise, we may end up with garbage in the buffers which may cause glitches/driver crashes.
            for (int i = 0; i < m_NextMeshWriteDataPoolItem; ++i)
            {
                var mwd = m_MeshWriteDataPool[i];
                if (mwd.vertexCount > 0 && mwd.currentVertex < mwd.vertexCount)
                {
                    Debug.LogError("Not enough vertices written in generateVisualContent callback " +
                        "(asked for " + mwd.vertexCount + " but only wrote " + mwd.currentVertex + ")");
                    var v = mwd.m_Vertices[0]; // Duplicate the first vertex
                    while (mwd.currentVertex < mwd.vertexCount)
                        mwd.SetNextVertex(v);
                }
                if (mwd.indexCount > 0 && mwd.currentIndex < mwd.indexCount)
                {
                    Debug.LogError("Not enough indices written in generateVisualContent callback " +
                        "(asked for " + mwd.indexCount + " but only wrote " + mwd.currentIndex + ")");
                    while (mwd.currentIndex < mwd.indexCount)
                        mwd.SetNextIndex(0);
                }
            }
        }

        void GenerateStencilClipEntryForRoundedRectBackground()
        {
            if (currentElement.layout.width <= Mathf.Epsilon || currentElement.layout.height <= Mathf.Epsilon)
                return;

            var style = currentElement.computedStyle;
            Vector2 radTL, radTR, radBL, radBR;
            MeshGenerationContextUtils.GetVisualElementRadii(currentElement, out radTL, out radBL, out radTR, out radBR);
            float widthT = style.borderTopWidth.value;
            float widthL = style.borderLeftWidth.value;
            float widthB = style.borderBottomWidth.value;
            float widthR = style.borderRightWidth.value;

            var rp = new MeshGenerationContextUtils.RectangleParams()
            {
                rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                color = Color.white,

                // Adjust the radius of the inner masking shape
                topLeftRadius = Vector2.Max(Vector2.zero, radTL - new Vector2(widthL, widthT)),
                topRightRadius = Vector2.Max(Vector2.zero, radTR - new Vector2(widthR, widthT)),
                bottomLeftRadius = Vector2.Max(Vector2.zero, radBL - new Vector2(widthL, widthB)),
                bottomRightRadius = Vector2.Max(Vector2.zero, radBR - new Vector2(widthR, widthB)),
                playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
            };

            // Only clip the interior shape, skipping the border
            rp.rect.x += widthL;
            rp.rect.y += widthT;
            rp.rect.width -= widthL + widthR;
            rp.rect.height -= widthT + widthB;

            // Skip padding, when requested
            if (style.unityOverflowClipBox == OverflowClipBox.ContentBox)
            {
                rp.rect.x += style.paddingLeft.value.value;
                rp.rect.y += style.paddingTop.value.value;
                rp.rect.width -= style.paddingLeft.value.value + style.paddingRight.value.value;
                rp.rect.height -= style.paddingTop.value.value + style.paddingBottom.value.value;
            }

            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.isStencilClipped = m_StencilClip;
            m_CurrentEntry.isClipRegisterEntry = true;

            MeshBuilder.MakeSolidRect(rp, UIRUtility.k_MaskPosZ, new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate });
            if (m_CurrentEntry.vertices.Length > 0 && m_CurrentEntry.indices.Length > 0)
            {
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_StencilClip = true; // Draw operations following this one should be clipped if not already
                m_ClosingInfo.needsClosing = true;
            }
            m_CurrentEntry = new Entry();
        }

        void GenerateStencilClipEntryForSVGBackground()
        {
            if (m_SVGBackgroundEntryIndex == -1)
                return;

            var svgEntry = m_Entries[m_SVGBackgroundEntryIndex];

            Debug.Assert(svgEntry.vertices.Length > 0);
            Debug.Assert(svgEntry.indices.Length > 0);

            m_StencilClip = true; // Draw operations following this one should be clipped if not already
            m_CurrentEntry.vertices = svgEntry.vertices;
            m_CurrentEntry.indices = svgEntry.indices;
            m_CurrentEntry.uvIsDisplacement = svgEntry.uvIsDisplacement;
            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.isStencilClipped = m_StencilClip;
            m_CurrentEntry.isClipRegisterEntry = true;
            m_ClosingInfo.needsClosing = true;

            // Adjust vertices for stencil clipping
            int vertexCount = m_CurrentEntry.vertices.Length;
            var clipVerts = m_VertsPool.Alloc((uint)vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                Vertex v = m_CurrentEntry.vertices[i];
                v.position.z = UIRUtility.k_MaskPosZ;
                clipVerts[i] = v;
            }
            m_CurrentEntry.vertices = clipVerts;
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;

            m_Entries.Add(m_CurrentEntry);
            m_CurrentEntry = new Entry();
        }
    }

    internal class UIRTextUpdatePainter : IStylePainter, IDisposable
    {
        VisualElement m_CurrentElement;
        int m_TextEntryIndex;
        NativeArray<Vertex> m_DudVerts;
        NativeArray<UInt16> m_DudIndices;
        NativeSlice<Vertex> m_MeshDataVerts;
        Color32 m_XFormClipPages, m_IDsFlags, m_OpacityPagesSettingsIndex;

        public MeshGenerationContext meshGenerationContext { get; }

        public UIRTextUpdatePainter()
        {
            meshGenerationContext = new MeshGenerationContext(this);
        }

        public void Begin(VisualElement ve, UIRenderDevice device)
        {
            Debug.Assert(ve.renderChainData.usesLegacyText && ve.renderChainData.textEntries.Count > 0);
            m_CurrentElement = ve;
            m_TextEntryIndex = 0;
            var oldVertexAlloc = ve.renderChainData.data.allocVerts;
            var oldVertexData = ve.renderChainData.data.allocPage.vertices.cpuData.Slice((int)oldVertexAlloc.start, (int)oldVertexAlloc.size);
            device.Update(ve.renderChainData.data, ve.renderChainData.data.allocVerts.size, out m_MeshDataVerts);
            RenderChainTextEntry firstTextEntry = ve.renderChainData.textEntries[0];
            if (ve.renderChainData.textEntries.Count > 1 || firstTextEntry.vertexCount != m_MeshDataVerts.Length)
                m_MeshDataVerts.CopyFrom(oldVertexData); // Preserve old data because we're not just updating the text vertices, but the entire mesh surrounding it though we won't touch but the text vertices

            // Case 1222517: Background and border are clipped by the parent, which implies that they may have a
            // different clip id when compared to the content, if overflow-clip-box is set to content-box. As a result,
            // we must NOT use the "first vertex" but rather the "first vertex of the first text entry".
            int first = firstTextEntry.firstVertex;
            m_XFormClipPages = oldVertexData[first].xformClipPages;
            m_IDsFlags = oldVertexData[first].idsFlags;
            m_OpacityPagesSettingsIndex = oldVertexData[first].opacityPageSVGSettingIndex;
        }

        public void End()
        {
            Debug.Assert(m_TextEntryIndex == m_CurrentElement.renderChainData.textEntries.Count); // Or else element repaint logic diverged for some reason
            m_CurrentElement = null;
        }

        public void Dispose()
        {
            if (m_DudVerts.IsCreated)
                m_DudVerts.Dispose();
            if (m_DudIndices.IsCreated)
                m_DudIndices.Dispose();
        }

        public void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams) {}
        public void DrawBorder(MeshGenerationContextUtils.BorderParams borderParams) {}
        public void DrawImmediate(Action callback, bool cullingEnabled) {}

        public VisualElement visualElement { get { return m_CurrentElement; } }

        public MeshWriteData DrawMesh(int vertexCount, int indexCount, Texture texture, Material material, MeshGenerationContext.MeshFlags flags)
        {
            // Ideally we should allow returning 0 here and the client would handle that properly
            if (m_DudVerts.Length < vertexCount)
            {
                if (m_DudVerts.IsCreated)
                    m_DudVerts.Dispose();
                m_DudVerts = new NativeArray<Vertex>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            if (m_DudIndices.Length < indexCount)
            {
                if (m_DudIndices.IsCreated)
                    m_DudIndices.Dispose();
                m_DudIndices = new NativeArray<UInt16>(indexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            return new MeshWriteData() { m_Vertices = m_DudVerts.Slice(0, vertexCount), m_Indices = m_DudIndices.Slice(0, indexCount) };
        }

        public void DrawText(MeshGenerationContextUtils.TextParams textParams, TextHandle handle, float pixelsPerPoint)
        {
            if (textParams.font == null)
                return;

            if (m_CurrentElement.panel.contextType == ContextType.Editor)
                textParams.fontColor *= textParams.playmodeTintColor;

            float scaling = TextNative.ComputeTextScaling(m_CurrentElement.worldTransform, pixelsPerPoint);
            TextNativeSettings textSettings = MeshGenerationContextUtils.TextParams.GetTextNativeSettings(textParams, scaling);

            using (NativeArray<TextVertex> textVertices = TextNative.GetVertices(textSettings))
            {
                var textEntry = m_CurrentElement.renderChainData.textEntries[m_TextEntryIndex++];

                Vector2 localOffset = TextNative.GetOffset(textSettings, textParams.rect);
                MeshBuilder.UpdateText(textVertices, localOffset, m_CurrentElement.renderChainData.verticesSpace,
                    m_XFormClipPages, m_IDsFlags, m_OpacityPagesSettingsIndex,
                    m_MeshDataVerts.Slice(textEntry.firstVertex, textEntry.vertexCount));
                textEntry.command.state.font = textParams.font.material.mainTexture;
            }
        }
    }
}
