// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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

    struct RemovalInfo
    {
        public bool anyDirtiedClipping;
        public bool anyDirtiedTransformOrSize;
        public bool anyDirtiedVisuals;
    }

    internal static class RenderEvents
    {
        internal static void OnStandardShaderChanged(RenderChain renderChain, Shader standardShader)
        {
            renderChain.device.standardShader = ResolveShader(standardShader);
        }

        internal static uint OnChildAdded(RenderChain renderChain, VisualElement parent, VisualElement ve, int index)
        {
            uint addedCount = DepthFirstOnChildAdded(renderChain, parent, ve, index, true);
            OnClippingChanged(renderChain, ve, true);
            OnVisualsChanged(renderChain, ve, true);
            return addedCount;
        }

        internal static void OnChildrenReordered(RenderChain renderChain, VisualElement ve)
        {
            int childrenCount = ve.hierarchy.childCount;
            var removalInfo = new RemovalInfo();
            for (int i = 0; i < childrenCount; i++)
                DepthFirstOnChildRemoving(renderChain, ve.hierarchy[i], ref removalInfo);
            for (int i = 0; i < childrenCount; i++)
                DepthFirstOnChildAdded(renderChain, ve, ve.hierarchy[i], i, false);

            OnClippingChanged(renderChain, ve, true);
            OnVisualsChanged(renderChain, ve, true);
        }

        internal static uint OnChildRemoving(RenderChain renderChain, VisualElement ve, ref RemovalInfo removalInfo)
        {
            return DepthFirstOnChildRemoving(renderChain, ve, ref removalInfo);
        }

        //internal static void OnChildDestroyed(RenderChain renderChain, VisualElement ve) { }
        internal static void OnVisualsChanged(RenderChain renderChain, VisualElement ve, bool hierarchical)
        {
            if (ve.renderChainData.isInChain)
                renderChain.OnVisualsChanged(ve, hierarchical);
        }

        internal static void OnClippingChanged(RenderChain renderChain, VisualElement ve, bool hierarchical)
        {
            if (ve.renderChainData.isInChain)
                renderChain.OnClippingChanged(ve, hierarchical);
        }

        internal static void OnTransformOrSizeChanged(RenderChain renderChain, VisualElement ve, bool transformChanged, bool sizeChanged)
        {
            if (ve.renderChainData.isInChain)
                renderChain.OnTransformOrSizeChanged(ve, transformChanged, sizeChanged);
        }

        internal static void OnRestoreTransformIDs(VisualElement ve, UIRenderDevice device)
        {
            // A small portion of the logic in DepthFirstOnChildAdded for fast device restoration purposes
            Debug.Assert(ve.renderChainData.isInChain);

            if (NeedsTransformID(ve, ve.renderChainData.clipMethod))
            {
                Debug.Assert(ve.renderChainData.transformID.size == 0);
                ve.renderChainData.transformID = device.AllocateTransform(); // The allocation might fail, it's ok. It will cause the use of manual transform
            }
            else
            {
                var parent = ve.hierarchy.parent;
                if (parent != null)
                {
                    ve.renderChainData.transformID = parent.renderChainData.transformID;
                    ve.renderChainData.transformID.shortLived = true; // Mark this allocation as not owned by us
                }
            }
        }

        internal static Shader ResolveShader(Shader shader)
        {
            if (shader == null)
                shader = Shader.Find(UIRUtility.k_DefaultShaderName);
            Debug.Assert(shader != null, "Failed to load the shader UIRDefault shader");
            return shader;
        }

        internal static void ProcessOnClippingChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, UIRenderDevice device, ref ChainBuilderStats stats)
        {
            stats.recursiveClipUpdates++;
            DepthFirstOnClippingChanged(renderChain, ve.hierarchy.parent, ve, dirtyID, false, true, false, false, false, device, ref stats);
        }

        internal static void ProcessOnTransformOrSizeChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, UIRenderDevice device, ref ChainBuilderStats stats)
        {
            stats.recursiveTransformUpdates++;
            DepthFirstOnTransformOrSizeChanged(renderChain, ve.hierarchy.parent, ve, dirtyID, device, false, false, ref stats);
        }

        internal static void ProcessOnVisualsChanged(RenderChain renderChain, VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
        {
            bool hierarchical = (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;
            if (hierarchical)
                stats.recursiveVisualUpdates++;
            else stats.nonRecursiveVisualUpdates++;
            var parent = ve.hierarchy.parent;
            DepthFirstOnVisualsChanged(renderChain, ve, dirtyID, parent != null ? IsElementHierarchyHidden(parent) : false, hierarchical, ref stats);
        }

        internal static void ProcessRegenText(RenderChain renderChain, VisualElement ve, UIRTextUpdatePainter painter, UIRenderDevice device, ref ChainBuilderStats stats)
        {
            stats.textUpdates++;
            painter.Begin(ve, device);
            ve.Repaint(painter);
            painter.End();
        }

        static Matrix4x4 GetTransformIDTransformInfo(VisualElement ve)
        {
            Debug.Assert(ve.renderChainData.allocatedTransformID || (ve.renderHint & (RenderHint.GroupTransform)) != 0);
            Matrix4x4 transform;
            if (ve.renderChainData.groupTransformAncestor != null)
                transform = ve.renderChainData.groupTransformAncestor.worldTransform.inverse * ve.worldTransform;
            else transform = ve.worldTransform;
            transform.m22 = transform.m33 = 1.0f; // Once world-space mode is introduced, this should become conditional
            return transform;
        }

        static Vector4 GetTransformIDClipInfo(VisualElement ve)
        {
            Debug.Assert(ve.renderChainData.allocatedTransformID);
            if (ve.renderChainData.groupTransformAncestor == null)
                return ve.worldClip.ToVector4();

            Rect rect = ve.worldClipMinusGroup;
            // Subtract the transform of the group transform ancestor
            var transform = ve.renderChainData.groupTransformAncestor.worldTransform.inverse;
            var min = transform.MultiplyPoint3x4(new Vector3(rect.xMin, rect.yMin, 0));
            var max = transform.MultiplyPoint3x4(new Vector3(rect.xMax, rect.yMax, 0));
            return new Vector4(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        static void GetVerticesTransformInfo(VisualElement ve, out Matrix4x4 transform, out float transformID)
        {
            if (ve.renderChainData.allocatedTransformID || (ve.renderHint & (RenderHint.GroupTransform)) != 0)
                transform = Matrix4x4.identity;
            else if (ve.renderChainData.boneTransformAncestor != null)
                transform = ve.renderChainData.boneTransformAncestor.worldTransform.inverse * ve.worldTransform;
            else if (ve.renderChainData.groupTransformAncestor != null)
                transform = ve.renderChainData.groupTransformAncestor.worldTransform.inverse * ve.worldTransform;
            else transform = ve.worldTransform;
            transform.m22 = transform.m33 = 1.0f; // Once world-space mode is introduced, this should become conditional
            transformID = ve.renderChainData.transformID.start;
        }

        static uint DepthFirstOnChildAdded(RenderChain renderChain, VisualElement parent, VisualElement ve, int index, bool resetState)
        {
            Debug.Assert(ve.panel != null);

            if (ve.renderChainData.isInChain)
                return 0; // Already added, redundant call

            if (resetState)
                ve.renderChainData = new RenderChainVEData();

            ve.renderChainData.isInChain = true;
            ve.renderChainData.verticesSpace = Matrix4x4.identity;

            if (parent != null)
            {
                if ((parent.renderHint & (RenderHint.GroupTransform)) != 0)
                    ve.renderChainData.groupTransformAncestor = parent;
                else ve.renderChainData.groupTransformAncestor = parent.renderChainData.groupTransformAncestor;
            }
            else ve.renderChainData.groupTransformAncestor = null;

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

            // Recurse on children
            int childrenCount = ve.hierarchy.childCount;
            uint deepCount = 0;
            for (int i = 0; i < childrenCount; i++)
                deepCount += DepthFirstOnChildAdded(renderChain, ve, ve.hierarchy[i], i, resetState);
            return 1 + deepCount;
        }

        static uint DepthFirstOnChildRemoving(RenderChain renderChain, VisualElement ve, ref RemovalInfo removalInfo)
        {
            // Recurse on children
            int childrenCount = ve.hierarchy.childCount - 1;
            uint deepCount = 0;
            while (childrenCount >= 0)
                deepCount += DepthFirstOnChildRemoving(renderChain, ve.hierarchy[childrenCount--], ref removalInfo);

            if (ve.renderChainData.isInChain)
            {
                ResetCommands(renderChain, ve);
                ve.renderChainData.isInChain = false;
                ve.renderChainData.clipMethod = ClipMethod.Undetermined;

                if (ve.renderChainData.next != null)
                    ve.renderChainData.next.renderChainData.prev = ve.renderChainData.prev;
                if (ve.renderChainData.prev != null)
                    ve.renderChainData.prev.renderChainData.next = ve.renderChainData.next;

                if (ve.renderChainData.allocatedTransformID)
                {
                    renderChain.device.Free(ve.renderChainData.transformID);
                    ve.renderChainData.transformID = new Alloc();
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
                if ((ve.renderChainData.dirtiedValues & (RenderDataDirtyTypes.Clipping | RenderDataDirtyTypes.ClippingHierarchy)) != 0)
                    removalInfo.anyDirtiedClipping = true;
                if ((ve.renderChainData.dirtiedValues & (RenderDataDirtyTypes.Transform | RenderDataDirtyTypes.Size)) != 0)
                    removalInfo.anyDirtiedTransformOrSize = true;
                if ((ve.renderChainData.dirtiedValues & (RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy)) != 0)
                    removalInfo.anyDirtiedVisuals = true;
            }
            return deepCount + 1;
        }

        static void DepthFirstOnClippingChanged(RenderChain renderChain,
            VisualElement parent,
            VisualElement ve,
            uint dirtyID,
            bool hierarchical,                  // MUST be false on the root call. Indicates that ALL descendants MUST be processed.
            bool isRootOfChange,                // MUST be true  on the root call.
            bool isPendingHierarchicalRepaint,  // MUST be false on the root call.
            bool inheritedTransformIDChanged,   // MUST be false on the root call.
            bool inheritedStencilClippedChanged,// MUST be false on the root call.
            UIRenderDevice device,
            ref ChainBuilderStats stats)
        {
            bool upToDate = dirtyID == ve.renderChainData.dirtyID;
            if (upToDate && !inheritedTransformIDChanged && !inheritedStencilClippedChanged)
                return;

            ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass
            stats.recursiveClipUpdatesExpanded++;

            // Despite an originally non-hierarchical processing, we may need to recurse to propagate some values. When
            // doing so, we may update elements that required hierarchical processing. In this case, we need to set
            // this flag because otherwise, their own individual processing may be skipped since the dirtyID has been set.
            hierarchical |= (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.ClippingHierarchy) != 0;

            isPendingHierarchicalRepaint |= (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;

            // Internal operations (done in this call) to do:
            bool mustUpdateTransformID = hierarchical || isRootOfChange || inheritedTransformIDChanged;
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

            // Update the clipping method.
            ClipMethod oldClippingMethod = ve.renderChainData.clipMethod;
            ClipMethod newClippingMethod = oldClippingMethod;
            if (mustUpdateClippingMethod)
                newClippingMethod = DetermineClipMethod(ve);

            // Update the transform ID.
            Alloc oldTransformID = ve.renderChainData.transformID;
            Alloc newTransformID = oldTransformID;
            if (mustUpdateTransformID)
            {
                if (NeedsTransformID(ve, newClippingMethod))
                {
                    if (!ve.renderChainData.allocatedTransformID)
                    {
                        newTransformID = device.AllocateTransform();
                        ve.renderChainData.transformID = newTransformID;
                    }
                }
                else
                {
                    if (ve.renderChainData.allocatedTransformID)
                    {
                        device.Free(ve.renderChainData.transformID);
                        newTransformID = new Alloc();
                        ve.renderChainData.transformID = newTransformID;
                    }
                }

                if (!ve.renderChainData.allocatedTransformID)
                {
                    if (parent != null && (ve.renderHint & RenderHint.GroupTransform) == 0)
                    {
                        if (parent.renderChainData.allocatedTransformID)
                            ve.renderChainData.boneTransformAncestor = parent;
                        else
                            ve.renderChainData.boneTransformAncestor = parent.renderChainData.boneTransformAncestor;

                        newTransformID = parent.renderChainData.transformID;
                        // Mark this allocation as not owned by us. Note that we are hijacking this field since it is
                        // actually unused in the case of transform ids.
                        newTransformID.shortLived = true;
                        ve.renderChainData.transformID = newTransformID;
                    }
                    else
                        ve.renderChainData.boneTransformAncestor = null;
                }
            }

            if (!ve.renderChainData.allocatedTransformID && newClippingMethod == ClipMethod.ShaderDiscard)
                // Fallback to scissoring since we couldn't allocate a transform ID
                newClippingMethod = ClipMethod.Scissor;

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

                if (newClippingMethod == ClipMethod.ShaderDiscard || oldClippingMethod == ClipMethod.ShaderDiscard && ve.renderChainData.allocatedTransformID)
                    // We must update the clipping rects.
                    mustProcessSizeChange = true;
            }

            bool transformIdChanged = false;
            if (TransformIDHasChanged(oldTransformID, newTransformID))
            {
                transformIdChanged = true;

                // Our children MUST update their render data transformIDs
                mustRecurse = true;

                // Our children MUST update their vertex transformIDs
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
                OnVisualsChanged(renderChain, ve, mustRepaintHierarchy);
                isPendingHierarchicalRepaint = true;
            }

            if (mustProcessSizeChange)
                OnTransformOrSizeChanged(renderChain, ve, false, true);

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
                        transformIdChanged,
                        isStencilClippedChanged,
                        device,
                        ref stats);
            }
        }

        static void  DepthFirstOnTransformOrSizeChanged(RenderChain renderChain, VisualElement parent, VisualElement ve, uint dirtyID, UIRenderDevice device, bool isAncestorOfChangeSkinned, bool transformChanged, ref ChainBuilderStats stats)
        {
            if (dirtyID == ve.renderChainData.dirtyID)
                return;

            stats.recursiveTransformUpdatesExpanded++;

            transformChanged |= (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.Transform) != 0;

            bool dirtyHasBeenResolved = true;
            if (ve.renderChainData.allocatedTransformID)
            {
                device.UpdateTransform(ve.renderChainData.transformID, GetTransformIDTransformInfo(ve), GetTransformIDClipInfo(ve));
                isAncestorOfChangeSkinned = true;
                stats.boneTransformed++;
            }
            else if (!transformChanged)
            {
                // Only the clip info had to be updated, we can skip the other cases which are for transform changes only.
            }
            else if ((ve.renderHint & RenderHint.GroupTransform) != 0)
            {
                stats.groupTransformElementsChanged++;
            }
            else if (isAncestorOfChangeSkinned)
            {
                // Children of a bone element inherit the transform and clip data change automatically when the root updates that data, no need to do anything for children
                Debug.Assert(ve.renderChainData.transformID.size > 0); // The element MUST have a transformID that has been inherited from an ancestor
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
                    OnVisualsChanged(renderChain, ve, false); // Nudging not allowed, so do a full visual repaint
                    stats.visualUpdateTransformed++;
                }
            }

            if (dirtyHasBeenResolved)
                ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

            if ((ve.renderHint & RenderHint.GroupTransform) == 0)
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
            Debug.Assert(ve.renderChainData.allocatedTransformID || ve.hierarchy.parent == null || ve.renderChainData.transformID.start == ve.hierarchy.parent.renderChainData.transformID.start || (ve.renderHint & RenderHint.GroupTransform) != 0);

            var painterClosingInfo = new UIRStylePainter.ClosingInfo();
            var painter = PaintElement(renderChain, ve, ref stats);
            if (painter != null)
            {
                painterClosingInfo = painter.closingInfo;
                painter.Reset();
            }

            if (hierarchical)
            {
                // Recurse on children
                int childrenCount = ve.hierarchy.childCount;
                for (int i = 0; i < childrenCount; i++)
                    DepthFirstOnVisualsChanged(renderChain, ve.hierarchy[i], dirtyID, ve.renderChainData.isHierarchyHidden, true, ref stats);
            }

            // By closing the element after its children, we can ensure closing data is allocated
            // at a time that would maintain continuity in the index buffer
            if (painterClosingInfo.needsClosing)
                ClosePaintElement(ve, painterClosingInfo, painter.device, ref stats);
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

        static ClipMethod DetermineClipMethod(VisualElement ve)
        {
            if (!ve.ShouldClip())
                return ClipMethod.NotClipped;

            if (!UIRUtility.IsRoundRect(ve))
            {
                if ((ve.renderHint & (RenderHint.GroupTransform | RenderHint.ClipWithScissors)) != 0)
                    return ClipMethod.Scissor;
                return ClipMethod.ShaderDiscard;
            }

            return ClipMethod.Stencil;
        }

        static bool NeedsTransformID(VisualElement ve, ClipMethod newClipMethod)
        {
            return (ve.renderHint & RenderHint.GroupTransform) == 0 && ((newClipMethod == ClipMethod.ShaderDiscard) || ((ve.renderHint & RenderHint.BoneTransform) == RenderHint.BoneTransform));
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

        internal static UIRStylePainter PaintElement(RenderChain renderChain, VisualElement ve, ref ChainBuilderStats stats)
        {
            if (IsElementSelfHidden(ve) || ve.renderChainData.isHierarchyHidden)
            {
                if (ve.renderChainData.data != null)
                {
                    renderChain.painter.device.Free(ve.renderChainData.data);
                    ve.renderChainData.data = null;
                }
                if (ve.renderChainData.firstCommand != null)
                    ResetCommands(renderChain, ve);
                return null;
            }

            // Retain our command insertion points if possible, to avoid paying the cost of finding them again
            RenderChainCommand oldCmdPrev = ve.renderChainData.firstCommand?.prev;
            RenderChainCommand oldCmdNext = ve.renderChainData.lastCommand?.next;
            RenderChainCommand oldClosingCmdPrev, oldClosingCmdNext;
            bool commandsAndClosingCommandsWereConsequtive = (ve.renderChainData.firstClosingCommand != null) && (oldCmdNext == ve.renderChainData.firstClosingCommand);
            if (commandsAndClosingCommandsWereConsequtive)
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
            painter.currentElement = ve;
            painter.Begin();
            ve.Repaint(painter);
            var entries = painter.entries;

            if (ve.renderChainData.allocatedTransformID)
                painter.device.UpdateTransform(ve.renderChainData.transformID, GetTransformIDTransformInfo(ve), GetTransformIDClipInfo(ve)); // Update the transform if we allocated it

            MeshHandle data = ve.renderChainData.data;
            if (painter.totalVertices <= UInt16.MaxValue && (entries.Count > 0))
            {
                NativeSlice<Vertex> verts = new NativeSlice<Vertex>();
                NativeSlice<UInt16> indices = new NativeSlice<UInt16>();
                UInt16 indexOffset = 0;
                if (painter.totalVertices > 0)
                    UpdateOrAllocate(ref data, painter.totalVertices, painter.totalIndices, painter.device, out verts, out indices, out indexOffset, ref stats);

                int vertsFilled = 0, indicesFilled = 0;

                float transformID;
                Matrix4x4 transform;
                GetVerticesTransformInfo(ve, out transform, out transformID);
                ve.renderChainData.verticesSpace = transform; // This is the space for the generated vertices below

                RenderChainCommand cmdPrev = oldCmdPrev, cmdNext = oldCmdNext;
                if (oldCmdPrev == null && oldCmdNext == null)
                    FindCommandInsertionPoint(ve, out cmdPrev, out cmdNext);

                int firstDisplacementUV = -1, lastDisplacementUVPlus1 = -1;
                foreach (var entry in painter.entries)
                {
                    if (entry.vertices.Length > 0 && entry.indices.Length > 0)
                    {
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

                            CopyTransformVertsPosAndVec(entry.vertices, targetVerticesSlice, transform, transformID, entry.clipRectID);
                        }
                        else CopyTransformVertsPos(entry.vertices, targetVerticesSlice, transform, transformID, entry.clipRectID);

                        // Copy indices
                        int entryIndexCount = entry.indices.Length;
                        int entryIndexOffset = vertsFilled + indexOffset;
                        var targetIndicesSlice = indices.Slice(indicesFilled, entryIndexCount);
                        if (entry.isClipRegisterEntry || !entry.isStencilClipped)
                            CopyTriangleIndices(entry.indices, targetIndicesSlice, entryIndexOffset);
                        else CopyTriangleIndicesFlipWindingOrder(entry.indices, targetIndicesSlice, entryIndexOffset); // Flip winding order if we're stencil-clipped

                        if (entry.isClipRegisterEntry)
                            painter.LandClipRegisterMesh(targetVerticesSlice, targetIndicesSlice, entryIndexOffset);

                        var cmd = InjectMeshDrawCommand(renderChain, ve, ref cmdPrev, ref cmdNext, entry.drawType, data, entryIndexCount, indicesFilled, entry.material, entry.custom, entry.font);
                        if (entry.drawType == VertexFlags.IsText)
                        {
                            Debug.Assert(ve.renderChainData.usesText);
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

            if (ve.renderChainData.usesText)
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
                if (commandsAndClosingCommandsWereConsequtive)
                {
                    cmdPrev = ve.renderChainData.lastCommand;
                    cmdNext = cmdPrev.next;
                }
                else if (oldCmdPrev == null && oldCmdNext == null)
                    FindClosingCommandInsertionPoint(ve, out cmdPrev, out cmdNext);

                if (painter.closingInfo.clipperRegisterIndices.Length > 0)
                    painter.LandClipUnregisterMeshDrawCommand(InjectClosingMeshDrawCommand(renderChain, ve, ref cmdPrev, ref cmdNext, VertexFlags.IsSolid, null, 0, 0, null, null, null)); // Placeholder command that will be filled actually later
                if (painter.closingInfo.popViewMatrix)
                {
                    var cmd = renderChain.AllocCommand();
                    cmd.type = CommandType.PopView;
                    cmd.closing = true;
                    cmd.owner = ve;
                    InjectClosingCommandInBetween(cmd, ref cmdPrev, ref cmdNext);
                }
                if (painter.closingInfo.popScissorClip)
                {
                    var cmd = renderChain.AllocCommand();
                    cmd.type = CommandType.PopScissor;
                    cmd.closing = true;
                    cmd.owner = ve;
                    InjectClosingCommandInBetween(cmd, ref cmdPrev, ref cmdNext);
                }
            }

            return painter;
        }

        static void ClosePaintElement(VisualElement ve, UIRStylePainter.ClosingInfo closingInfo, UIRenderDevice device, ref ChainBuilderStats stats)
        {
            if (closingInfo.clipperRegisterIndices.Length > 0)
            {
                NativeSlice<Vertex> verts = new NativeSlice<Vertex>();
                NativeSlice<UInt16> indices = new NativeSlice<UInt16>();
                UInt16 indexOffset = 0;

                // Due to device Update limitations, we cannot share the vertices of the registration mesh. It would be great
                // if we can just point winding-flipped indices towards the same vertices as the registration mesh.
                // For now, we duplicate the registration mesh entirely, wasting a bit of vertex memory
                UpdateOrAllocate(ref ve.renderChainData.closingData, closingInfo.clipperRegisterVertices.Length, closingInfo.clipperRegisterIndices.Length, device, out verts, out indices, out indexOffset, ref stats);
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

        static void CopyTransformVertsPos(NativeSlice<Vertex> source, NativeSlice<Vertex> target, Matrix4x4 mat, float transformID, float clipRectID)
        {
            int count = source.Length;
            for (int i = 0; i < count; i++)
            {
                Vertex v = source[i];
                v.transformID = transformID;
                v.clipRectID = clipRectID;
                v.position = mat.MultiplyPoint3x4(v.position);
                target[i] = v;
            }
        }

        static void CopyTransformVertsPosAndVec(NativeSlice<Vertex> source, NativeSlice<Vertex> target, Matrix4x4 mat, float transformID, float clipRectID)
        {
            int count = source.Length;
            Vector3 vec = new Vector3(0, 0, UIRUtility.k_MeshPosZ);

            for (int i = 0; i < count; i++)
            {
                Vertex v = source[i];
                v.transformID = transformID;
                v.clipRectID = clipRectID;
                v.position = mat.MultiplyPoint3x4(v.position);
                vec.x = v.uv.x;
                vec.y = v.uv.y;
                v.uv = mat.MultiplyVector(vec);

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

            float transformID;
            Matrix4x4 newTransform;
            GetVerticesTransformInfo(ve, out newTransform, out transformID);
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

        static RenderChainCommand InjectMeshDrawCommand(RenderChain renderChain, VisualElement ve, ref RenderChainCommand cmdPrev, ref RenderChainCommand cmdNext, VertexFlags drawType, MeshHandle mesh, int indexCount, int indexOffset, Material material, Texture custom, Texture font)
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

        static RenderChainCommand InjectClosingMeshDrawCommand(RenderChain renderChain, VisualElement ve, ref RenderChainCommand cmdPrev, ref RenderChainCommand cmdNext, VertexFlags drawType, MeshHandle mesh, int indexCount, int indexOffset, Material material, Texture custom, Texture font)
        {
            var cmd = renderChain.AllocCommand();
            cmd.type = CommandType.Draw;
            cmd.closing = true;
            cmd.state = new State() { material = material, custom = custom, font = font };
            cmd.mesh = mesh;
            cmd.indexOffset = indexOffset;
            cmd.indexCount = indexCount;
            cmd.owner = ve;
            InjectClosingCommandInBetween(cmd, ref cmdPrev, ref cmdNext);
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

            // This can be potentially O(n) of VE count
            // It is ok to check against lastCommand to mean the presence of closingCommand too, as we
            // require that closing commands only exist if a startup command exists too
            while (nextDrawingElem != null && nextDrawingElem.renderChainData.firstCommand == null)
                nextDrawingElem = nextDrawingElem.renderChainData.next;

            if (nextDrawingElem != null && nextDrawingElem.renderChainData.firstCommand != null)
            {
                // A next drawing element can be:
                // A) A next sibling (O(1) check time)
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

        static void InjectClosingCommandInBetween(RenderChainCommand cmd, ref RenderChainCommand prev, ref RenderChainCommand next)
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

            // Adjust the pointers as a facility for later injections
            prev = cmd;
            next = cmd.next;
        }

        static void ResetCommands(RenderChain renderChain, VisualElement ve)
        {
            if (ve.renderChainData.firstCommand != null)
                renderChain.OnRenderCommandRemoved(ve.renderChainData.firstCommand, ve.renderChainData.lastCommand);

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
                    renderChain.FreeCommand(c);
                    c = c.next;
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
                var c = ve.renderChainData.firstClosingCommand;
                while (c != ve.renderChainData.lastClosingCommand)
                {
                    renderChain.FreeCommand(c);
                    c = c.next;
                }
                renderChain.FreeCommand(c); // Last closing command
            }
            ve.renderChainData.firstClosingCommand = ve.renderChainData.lastClosingCommand = null;

            if (ve.renderChainData.usesText)
            {
                Debug.Assert(ve.renderChainData.textEntries.Count > 0);
                renderChain.RemoveTextElement(ve);
                ve.renderChainData.textEntries.Clear();
                ve.renderChainData.usesText = false;
            }
        }
    }

    internal class UIRStylePainter : IStylePainterInternal, IDisposable
    {
        internal struct Entry
        {
            public VertexFlags drawType;
            public NativeSlice<Vertex> vertices;
            public NativeSlice<UInt16> indices;
            public Material material; // Responsible for enabling immediate clipping
            public Texture custom, font;
            public RenderChainCommand customCommand;
            public float clipRectID;
            public bool uvIsDisplacement;
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
        Entry m_CurrentEntry;
        ClosingInfo m_ClosingInfo;
        bool m_StencilClip = false;
        float m_ClipRectID = 0;
        TempDataAlloc<Vertex> m_VertsPool = new TempDataAlloc<Vertex>(8192);
        TempDataAlloc<UInt16> m_IndicesPool = new TempDataAlloc<UInt16>(8192 << 1);

        // The delegate must be stored to avoid allocations.
        UIRMeshBuilder.AllocMeshData m_AllocDelegate;

        UIRMeshBuilder.MeshOutput Alloc(uint vertexCount, uint indexCount)
        {
            m_CurrentEntry.vertices = m_VertsPool.Alloc(vertexCount);
            m_CurrentEntry.indices = m_IndicesPool.Alloc(indexCount);
            Debug.Assert(m_CurrentEntry.vertices.Length == vertexCount);
            Debug.Assert(m_CurrentEntry.indices.Length == indexCount);
            return new UIRMeshBuilder.MeshOutput()
            {
                vertices = m_CurrentEntry.vertices,
                indices = m_CurrentEntry.indices
            };
        }

        public UIRStylePainter(RenderChain renderChain)
        {
            m_Owner = renderChain;
            device = renderChain.device;
            m_AtlasManager = renderChain.atlasManager;
            opacity = 1.0f;
            m_AllocDelegate = Alloc;
        }

        public VisualElement currentElement { get; set; }
        public UIRenderDevice device { get; }
        public List<Entry> entries { get { return m_Entries; } }
        public ClosingInfo closingInfo { get { return m_ClosingInfo; } }
        public int totalVertices { get; private set; }
        public int totalIndices { get; private set; }

        public float opacity { get; set; }

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

        public void Begin()
        {
            opacity = currentElement.resolvedStyle.opacity;
            currentElement.renderChainData.usesText = currentElement.renderChainData.usesAtlas = currentElement.renderChainData.disableNudging = false;
            currentElement.renderChainData.displacementUVStart = currentElement.renderChainData.displacementUVEnd = 0;
            bool isGroupTransform = (currentElement.renderHint & RenderHint.GroupTransform) != 0;
            if (isGroupTransform)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushView;
                m_Entries.Add(new Entry() { drawType = VertexFlags.LastType, customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popViewMatrix = true;
            }
            if (currentElement.hierarchy.parent != null)
            {
                m_StencilClip = currentElement.hierarchy.parent.renderChainData.isStencilClipped;
                m_ClipRectID = isGroupTransform ? 0 : currentElement.hierarchy.parent.renderChainData.transformID.start;
            }
            else
            {
                m_StencilClip = false;
                m_ClipRectID = 0;
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

        public void DrawMesh(MeshStylePainterParameters painterParameters, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData)
        {
            if (painterParameters.vertexCount == 0 || painterParameters.indexCount == 0)
            {
                vertexData = new NativeSlice<Vertex>();
                indexData = new NativeSlice<UInt16>();
                return;
            }

            m_CurrentEntry = new Entry()
            {
                drawType = VertexFlags.IsSolid,
                material = painterParameters.material,
                uvIsDisplacement = painterParameters.uvIsDisplacement,
                clipRectID = m_ClipRectID,
                isStencilClipped = m_StencilClip
            };

            var mesh = Alloc(painterParameters.vertexCount, painterParameters.indexCount);
            vertexData = mesh.vertices;
            indexData = mesh.indices;

            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
        }

        public void DrawRect(RectStylePainterParameters painterParams) { DrawRectImpl(painterParams, UIRUtility.k_MeshPosZ, false); }

        public void DrawText(TextStylePainterParameters painterParams)
        {
            float scaling = TextNative.ComputeTextScaling(currentElement.worldTransform, GUIUtility.pixelsPerPoint);
            TextNativeSettings textSettings = painterParams.GetTextNativeSettings(scaling);
            textSettings.color.a *= opacity;

            textSettings.color *= UIElementsUtility.editorPlayModeTintColor;

            using (NativeArray<TextVertex> textVertices = TextNative.GetVertices(textSettings))
            {
                if (textVertices.Length == 0)
                    return;
                Vector2 localOffset = TextNative.GetOffset(textSettings, painterParams.rect);
                m_CurrentEntry.drawType = VertexFlags.IsText;
                m_CurrentEntry.clipRectID = m_ClipRectID;
                m_CurrentEntry.isStencilClipped = m_StencilClip;
                UIRMeshBuilder.MakeText(textVertices, localOffset, m_AllocDelegate);
                m_CurrentEntry.font = painterParams.font.material.mainTexture;
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_CurrentEntry = new Entry();

                currentElement.renderChainData.usesText = true;
            }
        }

        public void DrawTexture(TextureStylePainterParameters painterParams)
        {
            painterParams.color.a *= opacity;

            painterParams.color *= UIElementsUtility.editorPlayModeTintColor;

            // Handle scaling mode
            Rect screenRect = painterParams.rect;
            Rect sourceRect = painterParams.uv != Rect.zero ? painterParams.uv : new Rect(0, 0, 1, 1);
            Texture texture = painterParams.texture;
            ScaleMode scaleMode = painterParams.scaleMode;
            Rect textureRect = screenRect;

            /// Comparing aspects ratio is error-prone because the <c>screenRect</c> may end up being scaled by the
            /// transform and the corners will end up being pixel aligned, possibly resulting in blurriness.
            float srcAspect = (texture.width * sourceRect.width) / (texture.height * sourceRect.height);
            float destAspect = screenRect.width / screenRect.height;
            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    break;

                case ScaleMode.ScaleAndCrop:
                    if (destAspect > srcAspect)
                    {
                        float stretch = sourceRect.height * (srcAspect / destAspect);
                        float crop = (sourceRect.height - stretch) * 0.5f;
                        sourceRect = new Rect(sourceRect.x, sourceRect.y + crop, sourceRect.width, stretch);
                    }
                    else
                    {
                        float stretch = sourceRect.width * (destAspect / srcAspect);
                        float crop = (sourceRect.width - stretch) * 0.5f;
                        sourceRect = new Rect(sourceRect.x + crop, sourceRect.y, stretch, sourceRect.height);
                    }
                    break;

                case ScaleMode.ScaleToFit:
                    if (destAspect > srcAspect)
                    {
                        float stretch = srcAspect / destAspect;
                        textureRect = new Rect(screenRect.xMin + screenRect.width * (1.0f - stretch) * .5f, screenRect.yMin, stretch * screenRect.width, screenRect.height);
                    }
                    else
                    {
                        float stretch = destAspect / srcAspect;
                        textureRect = new Rect(screenRect.xMin, screenRect.yMin + screenRect.height * (1.0f - stretch) * .5f, screenRect.width, stretch * screenRect.height);
                    }
                    break;
            }

            // Attempt to override with an atlas.
            VertexFlags vertexFlags;
            RectInt atlasRect;
            if (m_AtlasManager != null && m_AtlasManager.TryGetLocation(texture as Texture2D, out atlasRect))
            {
                vertexFlags = VertexFlags.IsTextured;
                sourceRect = new Rect(
                    sourceRect.x * texture.width + atlasRect.x,
                    sourceRect.y * texture.height + atlasRect.y,
                    sourceRect.width * atlasRect.width,
                    sourceRect.height * atlasRect.height);
                currentElement.renderChainData.usesAtlas = true;
            }
            else vertexFlags = VertexFlags.IsCustom;

            painterParams.texture = texture;
            painterParams.rect = textureRect;
            painterParams.uv = sourceRect;

            m_CurrentEntry.drawType = vertexFlags;
            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.isStencilClipped = m_StencilClip;
            m_CurrentEntry.custom = vertexFlags == VertexFlags.IsCustom ? texture : null;
            UIRMeshBuilder.MakeTexture(painterParams, UIRUtility.k_MeshPosZ, vertexFlags, m_AllocDelegate);
            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
        }

        public void DrawImmediate(Action callback)
        {
            var cmd = m_Owner.AllocCommand();
            cmd.type = CommandType.Immediate;
            cmd.owner = currentElement;
            cmd.callback = callback;
            m_Entries.Add(new Entry() { drawType = VertexFlags.LastType, customCommand = cmd });
        }

        public void DrawBackground()
        {
            if (currentElement.layout.width <= Mathf.Epsilon || currentElement.layout.height <= Mathf.Epsilon)
                return;

            var style = currentElement.computedStyle;
            if (style.backgroundColor != Color.clear)
            {
                var painterParams = RectStylePainterParameters.GetDefault(currentElement);
                painterParams.border.SetWidth(0.0f);
                DrawRect(painterParams);
            }

            if (style.backgroundImage.value.texture != null)
            {
                var painterParams = TextureStylePainterParameters.GetDefault(currentElement);
                if (style.unityBackgroundImageTintColor != Color.clear)
                    painterParams.color = style.unityBackgroundImageTintColor.value;
                painterParams.border.SetWidth(0.0f);
                DrawTexture(painterParams);
            }
        }

        public void DrawBorder()
        {
            if (currentElement.layout.width >= Mathf.Epsilon && currentElement.layout.height >= Mathf.Epsilon)
            {
                var style = currentElement.computedStyle;
                if (style.borderColor != Color.clear && (style.borderLeftWidth.value > 0.0f || style.borderTopWidth.value > 0.0f || style.borderRightWidth.value > 0.0f || style.borderBottomWidth.value > 0.0f))
                {
                    var painterParams = RectStylePainterParameters.GetDefault(currentElement);
                    painterParams.color = style.borderColor.value;
                    DrawRect(painterParams);
                }
            }
            ApplyClipping(); // Right after border drawing
        }

        public void DrawText(string text)
        {
            if (!string.IsNullOrEmpty(text) && currentElement.contentRect.width > 0.0f && currentElement.contentRect.height > 0.0f)
            {
                DrawText(TextStylePainterParameters.GetDefault(currentElement, text));
            }
        }

        internal void Reset()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            m_Entries.Clear(); // Doesn't shrink, good
            m_VertsPool.SessionDone();
            m_IndicesPool.SessionDone();
            m_ClosingInfo = new ClosingInfo();
            opacity = 1.0f;
            currentElement = null;
            totalVertices = totalIndices = 0;
        }

        bool DrawRectImpl(RectStylePainterParameters painterParams, float posZ, bool isClipRegisterEntry)
        {
            painterParams.color.a *= opacity;
            m_CurrentEntry.drawType = VertexFlags.IsSolid;
            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.isStencilClipped = m_StencilClip;
            m_CurrentEntry.isClipRegisterEntry = isClipRegisterEntry;

            painterParams.color *= UIElementsUtility.editorPlayModeTintColor;

            bool generatedData = false;
            UIRMeshBuilder.MakeRect(painterParams, posZ, m_AllocDelegate);
            if (m_CurrentEntry.vertices.Length > 0 && m_CurrentEntry.indices.Length > 0)
            {
                m_CurrentEntry.uvIsDisplacement = UIRMeshBuilder.IsBorder(painterParams.border);

                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                generatedData = true;
            }
            m_CurrentEntry = new Entry();
            return generatedData;
        }

        void ApplyClipping()
        {
            if (currentElement.renderChainData.clipMethod == ClipMethod.Scissor)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.type = CommandType.PushScissor;
                cmd.owner = currentElement;
                m_Entries.Add(new Entry() { drawType = VertexFlags.LastType, customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popScissorClip = true;
            }
            else if (currentElement.renderChainData.clipMethod == ClipMethod.Stencil)
            {
                if (DrawRectImpl(GetClipRectangleParameters(currentElement), UIRUtility.k_MaskPosZ, true))
                {
                    m_StencilClip = true; // Draw operations following this one should be clipped if not already
                    m_ClosingInfo.needsClosing = true;
                }
            }
            m_ClipRectID = currentElement.renderChainData.transformID.start;
        }

        static RectStylePainterParameters GetClipRectangleParameters(VisualElement ve)
        {
            var rectParams = RectStylePainterParameters.GetDefault(ve);

            // Only clip the interior shape, skipping the border
            rectParams.rect.x += rectParams.border.leftWidth;
            rectParams.rect.y += rectParams.border.topWidth;
            rectParams.rect.width -= (rectParams.border.leftWidth + rectParams.border.rightWidth);
            rectParams.rect.height -= (rectParams.border.topWidth + rectParams.border.bottomWidth);

            rectParams.rect.width = Mathf.Max(0.0f, rectParams.rect.width);
            rectParams.rect.height = Mathf.Max(0.0f, rectParams.rect.height);

            // Adjust the radius of the inner masking shape.  Unfortunately, the inner corner can have
            // an ellipse shape if the border widths aren't uniform across the shape.  Since we cannot express
            // different x/y radius for the outer shape, we take the mean of the two adjacent widths instead.
            rectParams.border.topLeftRadius -= (rectParams.border.leftWidth + rectParams.border.topWidth) / 2.0f;
            rectParams.border.topRightRadius -= (rectParams.border.rightWidth + rectParams.border.topWidth) / 2.0f;
            rectParams.border.bottomRightRadius -= (rectParams.border.rightWidth + rectParams.border.bottomWidth) / 2.0f;
            rectParams.border.bottomLeftRadius -= (rectParams.border.leftWidth + rectParams.border.bottomWidth) / 2.0f;

            rectParams.border.topLeftRadius = Mathf.Max(0.0f, rectParams.border.topLeftRadius);
            rectParams.border.topRightRadius = Mathf.Max(0.0f, rectParams.border.topRightRadius);
            rectParams.border.bottomRightRadius = Mathf.Max(0.0f, rectParams.border.bottomRightRadius);
            rectParams.border.bottomLeftRadius = Mathf.Max(0.0f, rectParams.border.bottomLeftRadius);

            rectParams.border.SetWidth(0);

            return rectParams;
        }
    }

    internal class UIRTextUpdatePainter : IStylePainterInternal, IDisposable
    {
        VisualElement m_CurrentElement;
        int m_TextEntryIndex;
        NativeArray<Vertex> m_DudVerts;
        NativeArray<UInt16> m_DudIndices;
        NativeSlice<Vertex> m_MeshDataVerts;
        float m_TransformID, m_ClippingRectID;

        public void Begin(VisualElement ve, UIRenderDevice device)
        {
            Debug.Assert(ve.renderChainData.usesText && ve.renderChainData.textEntries.Count > 0);
            m_CurrentElement = ve;
            m_TextEntryIndex = 0;
            var oldVertexAlloc = ve.renderChainData.data.allocVerts;
            var oldVertexData = ve.renderChainData.data.allocPage.vertices.cpuData.Slice((int)oldVertexAlloc.start, (int)oldVertexAlloc.size);
            device.Update(ve.renderChainData.data, ve.renderChainData.data.allocVerts.size, out m_MeshDataVerts);
            if (ve.renderChainData.textEntries.Count > 1 || ve.renderChainData.textEntries[0].vertexCount != m_MeshDataVerts.Length)
                m_MeshDataVerts.CopyFrom(oldVertexData); // Preserve old data because we're not just updating the text vertices, but the entire mesh surrounding it though we won't touch but the text vertices
            m_TransformID = oldVertexData[0].transformID;
            m_ClippingRectID = oldVertexData[0].clipRectID;
            opacity = ve.resolvedStyle.opacity;
        }

        public void End()
        {
            Debug.Assert(m_TextEntryIndex == m_CurrentElement.renderChainData.textEntries.Count); // Or else element repaint logic diverged for some reason
            m_CurrentElement = null;
        }

        public float opacity { get; set; }

        public void Dispose()
        {
            if (m_DudVerts.IsCreated)
                m_DudVerts.Dispose();
            if (m_DudIndices.IsCreated)
                m_DudIndices.Dispose();
        }

        public void DrawBackground() {}
        public void DrawBorder() {}
        public void DrawImmediate(Action callback) {}
        public void DrawMesh(MeshStylePainterParameters painterParameters, out NativeSlice<Vertex> vertexData, out NativeSlice<ushort> indexData)
        {
            // Ideally we should allow returning 0 here and the client would handle that properly
            if (m_DudVerts.Length < painterParameters.vertexCount)
            {
                if (m_DudVerts.IsCreated)
                    m_DudVerts.Dispose();
                m_DudVerts = new NativeArray<Vertex>((int)painterParameters.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            vertexData = m_DudVerts.Slice(0, (int)painterParameters.vertexCount);

            if (m_DudIndices.Length < painterParameters.indexCount)
            {
                if (m_DudIndices.IsCreated)
                    m_DudIndices.Dispose();
                m_DudIndices = new NativeArray<UInt16>((int)painterParameters.indexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            indexData = m_DudIndices.Slice(0, (int)painterParameters.vertexCount);
        }

        public void DrawRect(RectStylePainterParameters painterParams) {}

        public void DrawText(TextStylePainterParameters painterParams)
        {
            float scaling = TextNative.ComputeTextScaling(m_CurrentElement.worldTransform, GUIUtility.pixelsPerPoint);
            TextNativeSettings textSettings = painterParams.GetTextNativeSettings(scaling);
            textSettings.color.a *= opacity;

            textSettings.color *= UIElementsUtility.editorPlayModeTintColor;

            using (NativeArray<TextVertex> textVertices = TextNative.GetVertices(textSettings))
            {
                var textEntry = m_CurrentElement.renderChainData.textEntries[m_TextEntryIndex++];

                Vector2 localOffset = TextNative.GetOffset(textSettings, painterParams.rect);
                UIRMeshBuilder.UpdateText(textVertices, localOffset, m_CurrentElement.renderChainData.verticesSpace, m_TransformID, m_ClippingRectID, m_MeshDataVerts.Slice(textEntry.firstVertex, textEntry.vertexCount));
                textEntry.command.state.font = painterParams.font.material.mainTexture;
            }
        }

        public void DrawText(string text)
        {
            if (!string.IsNullOrEmpty(text) && m_CurrentElement.contentRect.width > 0.0f && m_CurrentElement.contentRect.height > 0.0f)
            {
                DrawText(TextStylePainterParameters.GetDefault(m_CurrentElement, text));
            }
        }

        public void DrawTexture(TextureStylePainterParameters painterParams) {}
    }
}
