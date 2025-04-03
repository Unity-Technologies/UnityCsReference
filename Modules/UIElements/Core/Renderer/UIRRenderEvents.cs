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

        internal static void ProcessOnClippingChanged(RenderTreeManager renderTreeManager, RenderData renderData, uint dirtyID, ref ChainBuilderStats stats)
        {
            bool hierarchical = (renderData.dirtiedValues & RenderDataDirtyTypes.ClippingHierarchy) != 0;
            if (hierarchical)
                stats.recursiveClipUpdates++;
            else
                stats.nonRecursiveClipUpdates++;

            DepthFirstOnClippingChanged(renderTreeManager, renderData.parent, renderData, dirtyID, hierarchical, true, false, false, false, renderTreeManager.device, ref stats);
        }

        internal static void ProcessOnOpacityChanged(RenderTreeManager renderTreeManager, RenderData renderData, uint dirtyID, ref ChainBuilderStats stats)
        {
            bool hierarchical = (renderData.dirtiedValues & RenderDataDirtyTypes.OpacityHierarchy) != 0;
            stats.recursiveOpacityUpdates++;
            DepthFirstOnOpacityChanged(renderTreeManager, renderData.parent != null ? renderData.parent.compositeOpacity : 1.0f, renderData, dirtyID, hierarchical, ref stats);
        }

        internal static void ProcessOnColorChanged(RenderTreeManager renderTreeManager, RenderData renderData, uint dirtyID, ref ChainBuilderStats stats)
        {
            stats.colorUpdates++;
            OnColorChanged(renderTreeManager, renderData, dirtyID, ref stats);
        }

        internal static void ProcessOnTransformOrSizeChanged(RenderTreeManager renderTreeManager, RenderData renderData, uint dirtyID, ref ChainBuilderStats stats)
        {
            stats.recursiveTransformUpdates++;
            DepthFirstOnTransformOrSizeChanged(renderTreeManager, renderData, dirtyID, renderTreeManager.device, false, false, ref stats);
        }

        static Matrix4x4 GetTransformIDTransformInfo(RenderData renderData)
        {
            Debug.Assert(RenderData.AllocatesID(renderData.transformID) || renderData.isGroupTransform);

            Matrix4x4 transform;
            var groupTransformAncestor = renderData.groupTransformAncestor;
            if (groupTransformAncestor != null)
                VisualElement.MultiplyMatrix34(ref groupTransformAncestor.owner.worldTransformInverse, ref renderData.owner.worldTransformRef, out transform);
            else
                UIRUtility.ComputeMatrixRelativeToRenderTree(renderData, out transform);

            transform.m22 = 1.0f; // Once world-space mode is introduced, this should become conditional
            return transform;
        }

        static Vector4 GetClipRectIDClipInfo(RenderData renderData)
        {
            Rect rect;

            Debug.Assert(RenderData.AllocatesID(renderData.clipRectID));

            if (renderData.groupTransformAncestor == null)
                rect = renderData.clippingRect;
            else
                rect = renderData.clippingRectMinusGroup;

            // See ComputeRelativeClipRectCoords in the shader for details on this computation
            Vector2 min = rect.min;
            Vector2 max = rect.max;
            Vector2 diff = max - min;
            Vector2 mul = new Vector2(1 / (diff.x + 0.0001f), 1 / (diff.y + 0.0001f));
            Vector2 a = 2 * mul;
            Vector2 b = -(min + max) * mul;
            return new Vector4(a.x, a.y, b.x, b.y);
        }

        internal static uint DepthFirstOnChildAdded(RenderTreeManager renderTreeManager, VisualElement parent, VisualElement ve, int index)
        {
            Debug.Assert(ve.panel != null);
            Debug.Assert(ve.renderData == null);
            Debug.Assert(ve.nestedRenderData == null);

            if (ve.insertionIndex >= 0)
                // We may be adding an element that was previously added by an ancestor in the same frame
                renderTreeManager.CancelInsertion(ve);

            RenderData renderData;
            RenderData parentRenderData = null;

            // Regular RenderData
            renderData = renderTreeManager.GetPooledRenderData();
            renderData.owner = ve;
            ve.renderData = renderData;

            if (ve.useRenderTexture)
                renderData.flags |= RenderDataFlags.IsSubTreeQuad;

            if (parent == null)
            {
                renderData.renderTree = renderTreeManager.GetPooledRenderTree(renderTreeManager, renderData);
                renderTreeManager.rootRenderTree = renderData.renderTree;
            }
            else
            {
                parentRenderData = parent.nestedRenderData ?? parent.renderData;
                renderData.parent = parentRenderData;
                renderData.renderTree = renderData.parent.renderTree;
                renderData.depthInRenderTree = renderData.parent.depthInRenderTree + 1;

                if (parentRenderData.isGroupTransform)
                    renderData.groupTransformAncestor = parentRenderData;
                else
                    renderData.groupTransformAncestor = parentRenderData.groupTransformAncestor;
            }

            renderData.renderTree.dirtyTracker.EnsureFits(renderData.depthInRenderTree);

            if ((ve.renderHints & RenderHints.GroupTransform) != 0 && !renderData.isSubTreeQuad && !renderTreeManager.drawInCameras)
                // TODO: For SubTreeQuads, we should convert this to a DynamicTransform
                renderData.flags |= RenderDataFlags.IsGroupTransform;

            // Nested RenderData
            if (renderData.isSubTreeQuad)
            {
                var nestedData = renderTreeManager.GetPooledRenderData();
                ve.nestedRenderData = nestedData;
                nestedData.owner = ve;
                nestedData.flags |= RenderDataFlags.IsNestedRenderTreeRoot;
                nestedData.transformID = UIRVEShaderInfoAllocator.identityTransform; // This is defining a new coordinate space

                nestedData.renderTree = renderTreeManager.GetPooledRenderTree(renderTreeManager, nestedData);
                nestedData.renderTree.dirtyTracker.EnsureFits(nestedData.depthInRenderTree);

                renderTreeManager.UIEOnClippingChanged(ve, true);
                renderTreeManager.UIEOnOpacityChanged(ve);
                renderTreeManager.UIEOnVisualsChanged(ve, true);

                var parentTree = renderData.renderTree;
                Debug.Assert(parentTree != null); // Because we're in the nested case

                // Insert the nested tree as the first child in the parent tree.
                // This implies children are not ordered.
                var nextSiblingTree = parentTree.firstChild;
                parentTree.firstChild = nestedData.renderTree;
                nestedData.renderTree.nextSibling = nextSiblingTree;

                nestedData.renderTree.parent = parentTree;
            }

            UpdateLocalFlipsWinding(renderData);

            // TODO: Refactor this so that we can process the whole subtree first,
            // then connect it with the renderTree.

            // If parent is null, we're a root, and roots by definition have no siblings
            // and initially have no children.
            if (parentRenderData != null)
            {
                // Search for the previous sibling in our parent. They are potentially not yet in the render tree
                // because of the delayed VisualElement additions. Consider the following example:
                //
                //        Root
                //        /  \
                //       C    A
                //           /
                //          B
                //
                // If element B is added first, followed by C, then even though C is part of the VisualElement
                // hierarchy, it is not yet in the render tree because of the postponed additions. Because of that,
                // we search through the parent's left siblings to find the first one that's actually part of the
                // render tree. If none is found, we fallback to the parent case.
                RenderData prevSibling = null;
                for (int i = index - 1; i >= 0; --i)
                {
                    prevSibling = parent.hierarchy[i].renderData;
                    if (prevSibling != null)
                        break;
                }

                RenderData nextSibling;
                if (prevSibling != null)
                {
                    nextSibling = prevSibling.nextSibling;
                    prevSibling.nextSibling = renderData;
                    renderData.prevSibling = prevSibling;
                }
                else
                {
                    nextSibling = parentRenderData.firstChild;
                    parentRenderData.firstChild = renderData;
                }

                if (nextSibling != null)
                {
                    renderData.nextSibling = nextSibling;
                    nextSibling.prevSibling = renderData;
                }
                else
                    parentRenderData.lastChild = renderData;
            }

            // TransformID
            // Since transform type is controlled by render hints which are locked on the VE by now, we can
            // go ahead and prep transform data now and never check on it again under regular circumstances
            Debug.Assert(!RenderData.AllocatesID(renderData.transformID));
            if (NeedsTransformID(ve))
                renderData.transformID = renderTreeManager.shaderInfoAllocator.AllocTransform(); // May fail, that's ok
            else
                renderData.transformID = BMPAlloc.Invalid;
            renderData.boneTransformAncestor = null;

            if (NeedsColorID(ve))
            {
                InitColorIDs(renderTreeManager, ve);
                SetColorValues(renderTreeManager, ve);
            }

            if (!RenderData.AllocatesID(renderData.transformID))
            {
                if (renderData.parent != null && !renderData.isGroupTransform)
                {
                    if (RenderData.AllocatesID(renderData.parent.transformID))
                        renderData.boneTransformAncestor = renderData.parent;
                    else
                        renderData.boneTransformAncestor = renderData.parent.boneTransformAncestor;

                    renderData.transformID = renderData.parent.transformID;
                    renderData.transformID.ownedState = OwnedState.Inherited; // Mark this allocation as not owned by us (inherited)
                }
                else
                    renderData.transformID = UIRVEShaderInfoAllocator.identityTransform;
            }
            else
                renderTreeManager.shaderInfoAllocator.SetTransformValue(renderData.transformID, GetTransformIDTransformInfo(renderData));

            // Recurse on children
            int childrenCount = ve.hierarchy.childCount;
            uint deepCount = 0;
            for (int i = 0; i < childrenCount; i++)
                deepCount += DepthFirstOnChildAdded(renderTreeManager, ve, ve.hierarchy[i], i);
            return 1 + deepCount;
        }

        internal static uint DepthFirstOnElementRemoving(RenderTreeManager renderTreeManager, VisualElement ve)
        {
            // NOTE: When we support Z-index, when recursing on the renderData, if a renderData
            // doesn't change the Z-index, we should skip the reconnections of the renderData's hierarchy.

            if (ve.insertionIndex >= 0)
            {
                // This element is pending insertion, cancel it
                renderTreeManager.CancelInsertion(ve);
            }

            // Recurse and process children first, to make sure we can safely
            // disconnect the nested trees from their parents.
            int childrenCount = ve.hierarchy.childCount - 1;
            uint deepCount = 0;
            while (childrenCount >= 0)
                deepCount += DepthFirstOnElementRemoving(renderTreeManager, ve.hierarchy[childrenCount--]);

            var renderData = ve.renderData;
            var nestedRenderData = ve.nestedRenderData;

            if (renderData != null)
            {
                DepthFirstRemoveRenderData(renderTreeManager, renderData);
                Debug.Assert(ve.renderData == null);
            }

            if (nestedRenderData != null)
            {
                DepthFirstRemoveRenderData(renderTreeManager, nestedRenderData);
                Debug.Assert(ve.nestedRenderData == null);
            }

            return deepCount + 1;
        }

        static void DepthFirstRemoveRenderData(RenderTreeManager renderTreeManager, RenderData renderData)
        {
            DisconnectSubTree(renderData);

            if (renderData.isNestedRenderTreeRoot)
                renderData.owner.nestedRenderData = null;
            else
                renderData.owner.renderData = null;
            RenderData child = renderData.firstChild;
            ResetRenderData(renderTreeManager, renderData);

            while (child != null)
            {
                RenderData nextChild = child.nextSibling;
                DoDepthFirstRemoveRenderData(renderTreeManager, child);
                child = nextChild;
            }
        }

        static void DoDepthFirstRemoveRenderData(RenderTreeManager renderTreeManager, RenderData renderData)
        {
            Debug.Assert(!renderData.isNestedRenderTreeRoot);

            renderData.owner.renderData = null;
            RenderData child = renderData.firstChild;
            ResetRenderData(renderTreeManager, renderData);

            while (child != null)
            {
                RenderData nextChild = child.nextSibling;
                DoDepthFirstRemoveRenderData(renderTreeManager, child);
                child = nextChild;
            }
        }

        static void DisconnectSubTree(RenderData renderData)
        {
            RenderData parentRenderData = renderData.parent;
            if (parentRenderData != null)
            {
                if (renderData.prevSibling == null)
                    parentRenderData.firstChild = renderData.nextSibling;

                if (renderData.nextSibling == null)
                    parentRenderData.lastChild = renderData.prevSibling;
            }

            if (renderData.prevSibling != null)
                renderData.prevSibling.nextSibling = renderData.nextSibling;

            if (renderData.nextSibling != null)
                renderData.nextSibling.prevSibling = renderData.prevSibling;
        }

        static void DisconnectRenderTreeFromParent(RenderTree parentTree, RenderTree nestedTree)
        {
            if (nestedTree == null || parentTree == null || parentTree == nestedTree)
                return;

            if (parentTree.firstChild == nestedTree)
                parentTree.firstChild = nestedTree.nextSibling;
            else
            {
                var prev = parentTree.firstChild;
                while (prev.nextSibling != nestedTree)
                    prev = prev.nextSibling;
                prev.nextSibling = nestedTree.nextSibling;
            }
        }

        static void ResetRenderData(RenderTreeManager renderTreeManager, RenderData renderData)
        {
            renderData.renderTree.ChildWillBeRemoved(renderData);
            CommandManipulator.ResetCommands(renderTreeManager, renderData);

            if (renderData.parent == null)
            {
                var parentTree = renderData.renderTree.parent;
                DisconnectRenderTreeFromParent(parentTree, renderData.renderTree);
                renderTreeManager.ReturnPoolRenderTree(renderData.renderTree);
            }

            renderData.parent = null;
            renderData.prevSibling = null;
            renderData.nextSibling = null;
            renderData.firstChild = null;
            renderData.lastChild = null;
            renderData.renderTree = null;

            renderTreeManager.ResetTextures(renderData);
            if (renderData.hasExtraData)
            {
                renderTreeManager.FreeExtraMeshes(renderData);
                renderTreeManager.FreeExtraData(renderData);
            }

            renderData.clipMethod = ClipMethod.Undetermined;

            if (RenderData.AllocatesID(renderData.textCoreSettingsID))
            {
                renderTreeManager.shaderInfoAllocator.FreeTextCoreSettings(renderData.textCoreSettingsID);
                renderData.textCoreSettingsID = UIRVEShaderInfoAllocator.defaultTextCoreSettings;
            }
            if (RenderData.AllocatesID(renderData.opacityID))
            {
                renderTreeManager.shaderInfoAllocator.FreeOpacity(renderData.opacityID);
                renderData.opacityID = UIRVEShaderInfoAllocator.fullOpacity;
            }
            if (RenderData.AllocatesID(renderData.colorID))
            {
                renderTreeManager.shaderInfoAllocator.FreeColor(renderData.colorID);
                renderData.colorID = BMPAlloc.Invalid;
            }
            if (RenderData.AllocatesID(renderData.backgroundColorID))
            {
                renderTreeManager.shaderInfoAllocator.FreeColor(renderData.backgroundColorID);
                renderData.backgroundColorID = BMPAlloc.Invalid;
            }
            if (RenderData.AllocatesID(renderData.borderLeftColorID))
            {
                renderTreeManager.shaderInfoAllocator.FreeColor(renderData.borderLeftColorID);
                renderData.borderLeftColorID = BMPAlloc.Invalid;
            }
            if (RenderData.AllocatesID(renderData.borderTopColorID))
            {
                renderTreeManager.shaderInfoAllocator.FreeColor(renderData.borderTopColorID);
                renderData.borderTopColorID = BMPAlloc.Invalid;
            }
            if (RenderData.AllocatesID(renderData.borderRightColorID))
            {
                renderTreeManager.shaderInfoAllocator.FreeColor(renderData.borderRightColorID);
                renderData.borderRightColorID = BMPAlloc.Invalid;
            }
            if (RenderData.AllocatesID(renderData.borderBottomColorID))
            {
                renderTreeManager.shaderInfoAllocator.FreeColor(renderData.borderBottomColorID);
                renderData.borderBottomColorID = BMPAlloc.Invalid;
            }
            if (RenderData.AllocatesID(renderData.tintColorID))
            {
                renderTreeManager.shaderInfoAllocator.FreeColor(renderData.tintColorID);
                renderData.tintColorID = BMPAlloc.Invalid;
            }
            if (RenderData.AllocatesID(renderData.clipRectID))
            {
                renderTreeManager.shaderInfoAllocator.FreeClipRect(renderData.clipRectID);
                renderData.clipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
            }
            if (RenderData.AllocatesID(renderData.transformID))
            {
                renderTreeManager.shaderInfoAllocator.FreeTransform(renderData.transformID);
                renderData.transformID = UIRVEShaderInfoAllocator.identityTransform;
            }
            renderData.boneTransformAncestor = renderData.groupTransformAncestor = null;
            if (renderData.tailMesh != null)
            {
                renderTreeManager.device.Free(renderData.tailMesh);
                renderData.tailMesh = null;
            }
            if (renderData.headMesh != null)
            {
                renderTreeManager.device.Free(renderData.headMesh);
                renderData.headMesh = null;
            }

            renderTreeManager.ReturnPoolRenderData(renderData);
        }

        static void DepthFirstOnClippingChanged(RenderTreeManager renderTreeManager,
            RenderData parentRenderData,
            RenderData renderData,
            uint dirtyID,
            bool hierarchical,
            bool isRootOfChange,                // MUST be true  on the root call.
            bool isPendingHierarchicalRepaint,  // MUST be false on the root call.
            bool inheritedClipRectIDChanged,    // MUST be false on the root call.
            bool inheritedMaskingChanged,       // MUST be false on the root call.
            UIRenderDevice device,
            ref ChainBuilderStats stats)
        {
            bool upToDate = dirtyID == renderData.dirtyID;
            if (upToDate && !inheritedClipRectIDChanged && !inheritedMaskingChanged)
                return;

            renderData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            if (!isRootOfChange)
                stats.recursiveClipUpdatesExpanded++;

            isPendingHierarchicalRepaint |= (renderData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;

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

            ClipMethod oldClippingMethod = renderData.clipMethod;
            ClipMethod newClippingMethod = mustUpdateClippingMethod ? DetermineSelfClipMethod(renderTreeManager, renderData) : oldClippingMethod;

            // Shader discard support
            bool clipRectIDChanged = false;
            if (mustUpdateClipRectID)
            {
                BMPAlloc newClipRectID = renderData.clipRectID;
                if (newClippingMethod == ClipMethod.ShaderDiscard)
                {
                    if (!RenderData.AllocatesID(renderData.clipRectID))
                    {
                        newClipRectID = renderTreeManager.shaderInfoAllocator.AllocClipRect();
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
                    if (RenderData.AllocatesID(renderData.clipRectID))
                        renderTreeManager.shaderInfoAllocator.FreeClipRect(renderData.clipRectID);

                    // Inherit parent's clipRectID if possible.
                    // Group transforms shouldn't inherit the clipRectID since they have a new frame of reference,
                    // they provide a new baseline with the _PixelClipRect instead.
                    if (!renderData.isGroupTransform)
                    {
                        newClipRectID = ((newClippingMethod != ClipMethod.Scissor) && (parentRenderData != null)) ? parentRenderData.clipRectID : UIRVEShaderInfoAllocator.infiniteClipRect;
                        newClipRectID.ownedState = OwnedState.Inherited;
                    }
                }

                clipRectIDChanged = !renderData.clipRectID.Equals(newClipRectID);
                Debug.Assert(!renderData.isGroupTransform || !clipRectIDChanged);
                renderData.clipRectID = newClipRectID;
            }

            bool maskingChanged = false;
            if (oldClippingMethod != newClippingMethod)
            {
                renderData.clipMethod = newClippingMethod;

                if (oldClippingMethod == ClipMethod.Stencil || newClippingMethod == ClipMethod.Stencil)
                {
                    maskingChanged = true;
                    mustUpdateChildrenMasking = true;
                }

                if (oldClippingMethod == ClipMethod.Scissor || newClippingMethod == ClipMethod.Scissor)
                    // We need to add/remove scissor push/pop commands
                    mustRepaintThis = true;

                if (newClippingMethod == ClipMethod.ShaderDiscard || oldClippingMethod == ClipMethod.ShaderDiscard && RenderData.AllocatesID(renderData.clipRectID))
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
                if (parentRenderData != null)
                {
                    newChildrenMaskDepth = parentRenderData.childrenMaskDepth;
                    newChildrenStencilRef = parentRenderData.childrenStencilRef;
                }

                if (newClippingMethod == ClipMethod.Stencil)
                {
                    if (newChildrenMaskDepth > newChildrenStencilRef)
                        ++newChildrenStencilRef;
                    ++newChildrenMaskDepth;
                }

                // When applying the MaskContainer hint, we skip because the last depth level because even though we
                // could technically increase the reference value, it would be useless since there won't be more
                // deeply nested masks that could benefit from it.
                if ((renderData.owner.renderHints & RenderHints.MaskContainer) == RenderHints.MaskContainer && newChildrenMaskDepth < UIRUtility.k_MaxMaskDepth)
                    newChildrenStencilRef = newChildrenMaskDepth;

                if (renderData.childrenMaskDepth != newChildrenMaskDepth || renderData.childrenStencilRef != newChildrenStencilRef)
                    maskingChanged = true;

                renderData.childrenMaskDepth = newChildrenMaskDepth;
                renderData.childrenStencilRef = newChildrenStencilRef;
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
                renderData.renderTree.OnRenderDataVisualsChanged(renderData, mustRepaintHierarchy);
                isPendingHierarchicalRepaint = true;
            }

            if (mustProcessSizeChange)
                renderData.renderTree.OnRenderDataTransformOrSizeChanged(renderData, false, true);

            if (mustRecurse)
            {
                var child = renderData.firstChild;
                while (child != null)
                {
                    DepthFirstOnClippingChanged(
                        renderTreeManager,
                        renderData,
                        child,
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

                    child = child.nextSibling;
                }
            }
        }

        static void DepthFirstOnOpacityChanged(RenderTreeManager renderTreeManager, float parentCompositeOpacity, RenderData renderData,
            uint dirtyID, bool hierarchical, ref ChainBuilderStats stats, bool isDoingFullVertexRegeneration = false)
        {
            if (dirtyID == renderData.dirtyID)
                return;

            renderData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            if (renderData.isSubTreeQuad)
                return; // TODO: We will need to process the opacity when implementing the real composite opacity

            stats.recursiveOpacityUpdatesExpanded++;
            float oldOpacity = renderData.compositeOpacity;
            float newOpacity = renderData.owner.resolvedStyle.opacity * parentCompositeOpacity;

            const float meaningfullOpacityChange = 0.0001f;

            bool visiblityTresholdPassed = (oldOpacity < VisibilityTreshold ^ newOpacity < VisibilityTreshold);
            bool compositeOpacityChanged = Mathf.Abs(oldOpacity - newOpacity) > meaningfullOpacityChange || visiblityTresholdPassed;
            if (compositeOpacityChanged)
            {
                // Avoid updating cached opacity if it changed too little, because we don't want slow changes to
                // update the cache and never trigger the compositeOpacityChanged condition.
                // The only small change allowed is when we cross the "visible" boundary of VisibilityTreshold
                renderData.compositeOpacity = newOpacity;
            }

            bool changedOpacityID = false;
            bool hasDistinctOpacity = newOpacity < parentCompositeOpacity - meaningfullOpacityChange; //assume 0 <= opacity <= 1
            if (hasDistinctOpacity)
            {
                if (renderData.opacityID.ownedState == OwnedState.Inherited)
                {
                    changedOpacityID = true;
                    renderData.opacityID = renderTreeManager.shaderInfoAllocator.AllocOpacity();
                }

                if ((changedOpacityID || compositeOpacityChanged) && renderData.opacityID.IsValid())
                    renderTreeManager.shaderInfoAllocator.SetOpacityValue(renderData.opacityID, newOpacity);
            }
            else if (renderData.opacityID.ownedState == OwnedState.Inherited)
            {
                // Just follow my parent's alloc
                if (renderData.parent != null &&
                    !renderData.opacityID.Equals(renderData.parent.opacityID))
                {
                    changedOpacityID = true;
                    renderData.opacityID = renderData.parent.opacityID;
                    renderData.opacityID.ownedState = OwnedState.Inherited;
                }
            }
            else
            {
                // I have an owned allocation, but I must match my parent's opacity, just set the opacity rather than free and inherit our parent's
                if (compositeOpacityChanged && renderData.opacityID.IsValid())
                    renderTreeManager.shaderInfoAllocator.SetOpacityValue(renderData.opacityID, newOpacity);
            }

            if ((renderData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0)
                isDoingFullVertexRegeneration = true;

            if (isDoingFullVertexRegeneration)
            {
                // A parent already called UIEOnVisualsChanged with hierarchical=true
            }
            else if (changedOpacityID && ((renderData.dirtiedValues & RenderDataDirtyTypes.Visuals) == 0) &&
                     (renderData.headMesh != null || renderData.tailMesh != null))
            {
                // Changed opacity ID, must update vertices.. we don't do it hierarchical here since our children will go through this too
                renderData.renderTree.OnRenderDataOpacityIdChanged(renderData);
            }

            if (compositeOpacityChanged || changedOpacityID || hierarchical)
            {
                // Recurse on children
                var child = renderData.firstChild;
                while (child != null)
                {
                    DepthFirstOnOpacityChanged(renderTreeManager, newOpacity, child, dirtyID, hierarchical, ref stats,
                        isDoingFullVertexRegeneration);

                    child = child.nextSibling;
                }
            }
        }

        static void OnColorChanged(RenderTreeManager renderTreeManager, RenderData renderData, uint dirtyID, ref ChainBuilderStats stats)
        {
            if (dirtyID == renderData.dirtyID)
                return;

            renderData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            if (renderData.isSubTreeQuad)
                return;

            stats.colorUpdatesExpanded++;

            var newColor = renderData.owner.resolvedStyle.backgroundColor;

            // UUM-21405: Fully-transparent backgrounds don't generate any geometry. So, we need to
            // force a dirty-repaint if we were transparent before, otherwise we may be trying to
            // change the color of a mesh that doesn't exists.
            if (renderData.backgroundAlpha == 0.0f && newColor.a > 0.0f)
                renderData.renderTree.OnRenderDataVisualsChanged(renderData, false);

            renderData.backgroundAlpha = newColor.a;

            bool shouldUpdateVisuals = false;
            if ((renderData.owner.renderHints & RenderHints.DynamicColor) == RenderHints.DynamicColor && !renderData.isIgnoringDynamicColorHint)
            {
                if (InitColorIDs(renderTreeManager, renderData.owner))
                    // New colors were allocated, we need to update the visuals
                    shouldUpdateVisuals = true;

                SetColorValues(renderTreeManager, renderData.owner);

                if (renderData.owner is TextElement && !RenderEvents.UpdateTextCoreSettings(renderTreeManager, renderData.owner))
                    shouldUpdateVisuals = true;
            }
            else
                shouldUpdateVisuals = true;

            if (shouldUpdateVisuals)
                renderData.renderTree.OnRenderDataVisualsChanged(renderData, false);
        }

        static void DepthFirstOnTransformOrSizeChanged(RenderTreeManager renderTreeManager, RenderData renderData, uint dirtyID, UIRenderDevice device, bool isAncestorOfChangeSkinned, bool transformChanged, ref ChainBuilderStats stats)
        {
            if (dirtyID == renderData.dirtyID)
                return;

            stats.recursiveTransformUpdatesExpanded++;

            renderData.flags |= RenderDataFlags.IsClippingRectDirty;

            transformChanged |= (renderData.dirtiedValues & RenderDataDirtyTypes.Transform) != 0;

            if (RenderData.AllocatesID(renderData.clipRectID))
            {
                Debug.Assert(!renderData.isSubTreeQuad);
                renderTreeManager.shaderInfoAllocator.SetClipRectValue(renderData.clipRectID, GetClipRectIDClipInfo(renderData));
            }

            if (transformChanged)
            {
                if (UpdateLocalFlipsWinding(renderData))
                {
                    // TODO: Optimized flip-winding instead of a full repaint
                    renderData.renderTree.OnRenderDataVisualsChanged(renderData, true);
                }
                UpdateZeroScaling(renderData);
            }

            bool dirtyHasBeenResolved = true;
            if (RenderData.AllocatesID(renderData.transformID))
            {
                Debug.Assert(!renderData.isNestedRenderTreeRoot); // Because they are always an identity
                renderTreeManager.shaderInfoAllocator.SetTransformValue(renderData.transformID, GetTransformIDTransformInfo(renderData));
                isAncestorOfChangeSkinned = true;
                stats.boneTransformed++;
            }
            else if (!transformChanged)
            {
                // Only the clip info had to be updated, we can skip the other cases which are for transform changes only.
            }
            else if (renderData.isGroupTransform)
            {
                stats.groupTransformElementsChanged++;
            }
            else if (isAncestorOfChangeSkinned)
            {
                // Children of a bone element inherit the transform data change automatically when the root updates that data, no need to do anything for children
                Debug.Assert(RenderData.InheritsID(renderData.transformID)); // The element MUST have a transformID that has been inherited from an ancestor
                dirtyHasBeenResolved = false; // We just skipped processing, if another later transform change is queued on this element this pass then we should still process it
                stats.skipTransformed++;
            }
            else if ((renderData.dirtiedValues & (RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy)) == 0 &&
                     (renderData.headMesh != null || renderData.tailMesh != null))
            {
                // If a visual update will happen, then skip work here as the visual update will incorporate the transformed vertices
                if (NudgeVerticesToNewSpace(renderData, renderTreeManager, device))
                    stats.nudgeTransformed++;
                else
                {
                    renderData.renderTree.OnRenderDataVisualsChanged(renderData, false); // Nudging not allowed, so do a full visual repaint
                    stats.visualUpdateTransformed++;
                }
            }

            if (dirtyHasBeenResolved)
                renderData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            // Make sure to pre-evaluate world transform and clip now so we don't do it at render time
            if (renderTreeManager.drawInCameras)
                renderData.owner.EnsureWorldTransformAndClipUpToDate(); // TODO: Re-evaluate if this is needed

            if (!renderData.isGroupTransform)
            {
                // Recurse on children
                var child = renderData.firstChild;
                while (child != null)
                {
                    DepthFirstOnTransformOrSizeChanged(renderTreeManager, child, dirtyID, device, isAncestorOfChangeSkinned, transformChanged, ref stats);
                    child = child.nextSibling;
                }
            }
        }

        public static bool UpdateTextCoreSettings(RenderTreeManager renderTreeManager, VisualElement ve)
        {
            if (ve == null || !TextUtilities.IsFontAssigned(ve))
                return false;

            bool allocatesID = RenderData.AllocatesID(ve.renderData.textCoreSettingsID);

            var settings = TextUtilities.GetTextCoreSettingsForElement(ve, false);

            // If we aren't using a color ID (the DynamicColor flag), the text color will be stored in the vertex data,
            // so there's no need for a color match with the default TextCore settings.
            bool useDefaultColor = !NeedsColorID(ve);

            if (useDefaultColor && !NeedsTextCoreSettings(ve) && !allocatesID)
            {
                // Use default TextCore settings
                ve.renderData.textCoreSettingsID = UIRVEShaderInfoAllocator.defaultTextCoreSettings;
                return true;
            }

            if (!allocatesID)
                ve.renderData.textCoreSettingsID = renderTreeManager.shaderInfoAllocator.AllocTextCoreSettings(settings);

            if (RenderData.AllocatesID(ve.renderData.textCoreSettingsID))
            {
                if (ve.panel.contextType == ContextType.Editor)
                {
                    var playModeTintColor = ve.playModeTintColor;
                    settings.faceColor *= playModeTintColor;
                    settings.outlineColor *= playModeTintColor;
                    settings.underlayColor *= playModeTintColor;
                }

                renderTreeManager.shaderInfoAllocator.SetTextCoreSettingValue(ve.renderData.textCoreSettingsID, settings);
            }

            return true;
        }

        static bool NudgeVerticesToNewSpace(RenderData renderData, RenderTreeManager renderTreeManager, UIRenderDevice device)
        {
            k_NudgeVerticesMarker.Begin();

            Matrix4x4 newTransform;
            UIRUtility.GetVerticesTransformInfo(renderData, out newTransform);
            Matrix4x4 nudgeTransform = newTransform * renderData.verticesSpace.inverse;

            // Attempt to reconstruct the absolute transform. If the result diverges from the absolute
            // considerably, then we assume that the vertices have become degenerate beyond restoration.
            // In this case we refuse to nudge, and ask for this element to be fully repainted to regenerate
            // the vertices without error.
            const float kMaxAllowedDeviation = 0.0001f;
            Matrix4x4 reconstructedNewTransform = nudgeTransform * renderData.verticesSpace;
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

            renderData.verticesSpace = newTransform; // This is the new space of the vertices

            var job = new NudgeJobData
            {
                transform = nudgeTransform
            };

            if (renderData.headMesh != null)
                PrepareNudgeVertices(device, renderData.headMesh, out job.headSrc, out job.headDst, out job.headCount);

            if (renderData.tailMesh != null)
                PrepareNudgeVertices(device, renderData.tailMesh, out job.tailSrc, out job.tailDst, out job.tailCount);

            renderTreeManager.jobManager.Add(ref job);

            if (renderData.hasExtraMeshes)
            {
                ExtraRenderData extraData = renderTreeManager.GetOrAddExtraData(renderData);
                BasicNode<MeshHandle> extraMesh = extraData.extraMesh;
                while (extraMesh != null)
                {
                    var extraJob = new NudgeJobData { transform = job.transform };
                    PrepareNudgeVertices(device, extraMesh.data, out extraJob.headSrc, out extraJob.headDst, out extraJob.headCount);
                    renderTreeManager.jobManager.Add(ref extraJob);
                    extraMesh = extraMesh.next;
                }
            }

            k_NudgeVerticesMarker.End();
            return true;
        }

        static unsafe void PrepareNudgeVertices(UIRenderDevice device, MeshHandle mesh, out IntPtr src, out IntPtr dst, out int count)
        {
            int vertCount = (int)mesh.allocVerts.size;
            NativeSlice<Vertex> oldVerts = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, vertCount);
            NativeSlice<Vertex> newVerts;
            device.Update(mesh, (uint)vertCount, out newVerts);

            src = (IntPtr)oldVerts.GetUnsafePtr();
            dst = (IntPtr)newVerts.GetUnsafePtr();
            count = vertCount;
        }

        static ClipMethod DetermineSelfClipMethod(RenderTreeManager renderTreeManager, RenderData renderData)
        {
            if (renderData.isSubTreeQuad)
                return ClipMethod.NotClipped;

            if (!renderData.owner.ShouldClip())
                return ClipMethod.NotClipped;

            if (renderTreeManager.drawInCameras)
                return ClipMethod.ShaderDiscard; // World-space panels only support ShaderDiscard

            // Even though GroupTransform does not formally imply the use of scissors, we prefer to use them because
            // this way, we can avoid updating nested clipping rects.
            bool preferScissors = renderData.isGroupTransform || (renderData.owner.renderHints & RenderHints.ClipWithScissors) != 0;
            ClipMethod rectClipMethod = preferScissors ? ClipMethod.Scissor : ClipMethod.ShaderDiscard;

            if (!renderTreeManager.elementBuilder.RequiresStencilMask(renderData.owner))
                return rectClipMethod;

            int inheritedMaskDepth = 0;
            var parent = renderData.parent;
            if (parent != null)
                inheritedMaskDepth = parent.childrenMaskDepth;

            // We're already at the deepest level, we can't go any deeper.
            if (inheritedMaskDepth == UIRUtility.k_MaxMaskDepth)
                return rectClipMethod;

            // Default to stencil
            return ClipMethod.Stencil;
        }

        // Returns true when a change was detected
        static bool UpdateLocalFlipsWinding(RenderData renderData)
        {
            if (!renderData.owner.elementPanel.isFlat)
                return false;

            bool newFlipsWinding = false;
            if (!renderData.isNestedRenderTreeRoot) // Otherwise, the transform is an identity
            {
                Vector3 scale = renderData.owner.resolvedStyle.scale.value;
                float winding = scale.x * scale.y;
                if (Math.Abs(winding) < 0.001f)
                {
                    return false; // Close to zero, preserve the current value
                }

                newFlipsWinding = winding < 0;
            }

            bool oldFlipsWinding = renderData.localFlipsWinding;
            if (oldFlipsWinding != newFlipsWinding)
            {
                renderData.localFlipsWinding = newFlipsWinding;
                return true;
            }

            return false;
        }

        static void UpdateZeroScaling(RenderData renderData)
        {
            if (renderData.isNestedRenderTreeRoot) // Otherwise, the transform is an identity
                return;

            var ve = renderData.owner;
            bool transformScaleZero = Math.Abs(ve.resolvedStyle.scale.value.x * ve.resolvedStyle.scale.value.y) < 0.001f;

            bool parentTransformScaleZero = false;
            VisualElement parent = ve.hierarchy.parent;
            if (parent != null)
                parentTransformScaleZero = parent.renderData.worldTransformScaleZero;

            renderData.worldTransformScaleZero = parentTransformScaleZero | transformScaleZero;
        }

        static bool NeedsTransformID(VisualElement ve)
        {
            return !ve.renderData.isGroupTransform && (ve.renderHints & RenderHints.BoneTransform) != 0;
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
            var settings = TextUtilities.GetTextCoreSettingsForElement(ve, true);
            if (settings.outlineWidth != 0.0f || settings.underlayOffset != Vector2.zero || settings.underlaySoftness != 0.0f)
                return true;

            return false;
        }

        static bool InitColorIDs(RenderTreeManager renderTreeManager, VisualElement ve)
        {
            var style = ve.resolvedStyle;
            bool hasAllocated = false;
            if (!ve.renderData.colorID.IsValid() && ve is TextElement)
            {
                ve.renderData.colorID = renderTreeManager.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderData.backgroundColorID.IsValid())
            {
                ve.renderData.backgroundColorID = renderTreeManager.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderData.borderLeftColorID.IsValid() && style.borderLeftWidth > 0.0f) // Size change will trigger a re-tessellation
            {
                ve.renderData.borderLeftColorID = renderTreeManager.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderData.borderTopColorID.IsValid() && style.borderTopWidth > 0.0f) // Size change will trigger a re-tessellation
            {
                ve.renderData.borderTopColorID = renderTreeManager.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderData.borderRightColorID.IsValid() && style.borderRightWidth > 0.0f) // Size change will trigger a re-tessellation
            {
                ve.renderData.borderRightColorID = renderTreeManager.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderData.borderBottomColorID.IsValid() && style.borderBottomWidth > 0.0f) // Size change will trigger a re-tessellation
            {
                ve.renderData.borderBottomColorID = renderTreeManager.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            if (!ve.renderData.tintColorID.IsValid())
            {
                ve.renderData.tintColorID = renderTreeManager.shaderInfoAllocator.AllocColor();
                hasAllocated = true;
            }
            return hasAllocated;
        }

        static void ResetColorIDs(VisualElement ve)
        {
            ve.renderData.colorID = BMPAlloc.Invalid;
            ve.renderData.backgroundColorID = BMPAlloc.Invalid;
            ve.renderData.borderLeftColorID = BMPAlloc.Invalid;
            ve.renderData.borderTopColorID = BMPAlloc.Invalid;
            ve.renderData.borderRightColorID = BMPAlloc.Invalid;
            ve.renderData.borderBottomColorID = BMPAlloc.Invalid;
            ve.renderData.tintColorID = BMPAlloc.Invalid;
        }

        public static void SetColorValues(RenderTreeManager renderTreeManager, VisualElement ve)
        {
            var style = ve.resolvedStyle;
            if (ve.renderData.colorID.IsValid())
                renderTreeManager.shaderInfoAllocator.SetColorValue(ve.renderData.colorID, style.color);
            if (ve.renderData.backgroundColorID.IsValid())
                renderTreeManager.shaderInfoAllocator.SetColorValue(ve.renderData.backgroundColorID, style.backgroundColor);
            if (ve.renderData.borderLeftColorID.IsValid())
                renderTreeManager.shaderInfoAllocator.SetColorValue(ve.renderData.borderLeftColorID, style.borderLeftColor);
            if (ve.renderData.borderTopColorID.IsValid())
                renderTreeManager.shaderInfoAllocator.SetColorValue(ve.renderData.borderTopColorID, style.borderTopColor);
            if (ve.renderData.borderRightColorID.IsValid())
                renderTreeManager.shaderInfoAllocator.SetColorValue(ve.renderData.borderRightColorID, style.borderRightColor);
            if (ve.renderData.borderBottomColorID.IsValid())
                renderTreeManager.shaderInfoAllocator.SetColorValue(ve.renderData.borderBottomColorID, style.borderBottomColor);
            if (ve.renderData.tintColorID.IsValid())
                renderTreeManager.shaderInfoAllocator.SetColorValue(ve.renderData.tintColorID, style.unityBackgroundImageTintColor);
        }
    }
}
