/**
 * Copyright (c) 2014-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

// BEGIN_UNITY @jonathanma This file is heavily modified to use Unity bindings system 

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.Yoga
{
    using YogaValueType = YogaValue;

    [NativeHeader("Modules/UIElementsNative/YogaNative.bindings.h")]
    internal static partial class Native
    {
        // We don't support setting custom C# logger (only use a default one in native code).
        //[FreeFunction]
        //public static extern void YGInteropSetLogger(YogaLogger logger);

        [FreeFunction]
        static extern IntPtr YGNodeNew();

        [FreeFunction]
        public static extern IntPtr YGNodeNewWithConfig(IntPtr config);

        public static void YGNodeFree(IntPtr ygNode)
        {
            if (ygNode == IntPtr.Zero)
                return;

            YGNodeFreeInternal(ygNode);
        }

        [FreeFunction(Name = "YGNodeFree", IsThreadSafe = true)]
        private static extern void YGNodeFreeInternal(IntPtr ygNode);

        [FreeFunction]
        public static extern void YGNodeReset(IntPtr node);

        // BEGIN_UNITY @jonathanma Added this function to store a reference in native code to the managed object
        // This is used by callback functions.
        [FreeFunction]
        public static extern void YGSetManagedObject(IntPtr ygNode, YogaNode node);
        // END_UNITY

        [FreeFunction]
        public static extern void YGNodeSetConfig(IntPtr ygNode, IntPtr config);

        [FreeFunction]
        public static extern IntPtr YGConfigGetDefault();

        [FreeFunction]
        public static extern IntPtr YGConfigNew();

        public static void YGConfigFree(IntPtr config)
        {
            if (config == IntPtr.Zero)
                return;

            YGConfigFreeInternal(config);
        }

        [FreeFunction(Name = "YGConfigFree", IsThreadSafe = true)]
        static extern void YGConfigFreeInternal(IntPtr config);

        [FreeFunction]
        public static extern int YGNodeGetInstanceCount();

        [FreeFunction]
        public static extern int YGConfigGetInstanceCount();

        [FreeFunction]
        public static extern void YGConfigSetExperimentalFeatureEnabled(
            IntPtr config,
            YogaExperimentalFeature feature,
            bool enabled);

        [FreeFunction]
        public static extern bool YGConfigIsExperimentalFeatureEnabled(
            IntPtr config,
            YogaExperimentalFeature feature);

        [FreeFunction]
        public static extern void YGConfigSetUseWebDefaults(
            IntPtr config,
            bool useWebDefaults);

        [FreeFunction]
        public static extern bool YGConfigGetUseWebDefaults(IntPtr config);

        [FreeFunction]
        public static extern void YGConfigSetPointScaleFactor(
            IntPtr config,
            float pixelsInPoint);

        [FreeFunction]
        public static extern float YGConfigGetPointScaleFactor(
            IntPtr config);

        [FreeFunction]
        public static extern void YGNodeInsertChild(
            IntPtr node,
            IntPtr child,
            uint index);

        [FreeFunction]
        public static extern void YGNodeRemoveChild(IntPtr node, IntPtr child);

        [FreeFunction]
        public static extern void YGNodeCalculateLayout(
            IntPtr node,
            float availableWidth,
            float availableHeight,
            YogaDirection parentDirection);

        [FreeFunction]
        public static extern void YGNodeMarkDirty(IntPtr node);

        [FreeFunction]
        public static extern bool YGNodeIsDirty(IntPtr node);

        [FreeFunction]
        public static extern void YGNodePrint(IntPtr node, YogaPrintOptions options);

        [FreeFunction]
        public static extern void YGNodeCopyStyle(IntPtr dstNode, IntPtr srcNode);

#region YG_NODE_PROPERTY

        [FreeFunction(Name = "YogaCallback::SetMeasureFunc")]
        public static extern void YGNodeSetMeasureFunc(IntPtr node);

        [FreeFunction(Name = "YogaCallback::RemoveMeasureFunc")]
        public static extern void YGNodeRemoveMeasureFunc(IntPtr node);

        [RequiredByNativeCode]
        unsafe public static void YGNodeMeasureInvoke(YogaNode node, float width, YogaMeasureMode widthMode, float height, YogaMeasureMode heightMode, IntPtr returnValueAddress)
        {
            (*(YogaSize*)returnValueAddress) = YogaNode.MeasureInternal(node, width, widthMode, height, heightMode);
        }

        [FreeFunction(Name = "YogaCallback::SetBaselineFunc")]
        public static extern void YGNodeSetBaselineFunc(IntPtr node);

        [FreeFunction(Name = "YogaCallback::RemoveBaselineFunc")]
        public static extern void YGNodeRemoveBaselineFunc(IntPtr node);

        [RequiredByNativeCode]
        unsafe public static void YGNodeBaselineInvoke(YogaNode node, float width, float height, IntPtr returnValueAddress)
        {
            (*(float*)returnValueAddress) = YogaNode.BaselineInternal(node, width, height);
        }

        [FreeFunction]
        public static extern void YGNodeSetHasNewLayout(IntPtr node, bool hasNewLayout);

        [FreeFunction]
        public static extern bool YGNodeGetHasNewLayout(IntPtr node);

#endregion

#region YG_NODE_STYLE_PROPERTY

        [FreeFunction]
        public static extern void YGNodeStyleSetDirection(IntPtr node, YogaDirection direction);

        [FreeFunction]
        public static extern YogaDirection YGNodeStyleGetDirection(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetFlexDirection(IntPtr node, YogaFlexDirection flexDirection);

        [FreeFunction]
        public static extern YogaFlexDirection YGNodeStyleGetFlexDirection(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetJustifyContent(IntPtr node, YogaJustify justifyContent);

        [FreeFunction]
        public static extern YogaJustify YGNodeStyleGetJustifyContent(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetAlignContent(IntPtr node, YogaAlign alignContent);

        [FreeFunction]
        public static extern YogaAlign YGNodeStyleGetAlignContent(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetAlignItems(IntPtr node, YogaAlign alignItems);

        [FreeFunction]
        public static extern YogaAlign YGNodeStyleGetAlignItems(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetAlignSelf(IntPtr node, YogaAlign alignSelf);

        [FreeFunction]
        public static extern YogaAlign YGNodeStyleGetAlignSelf(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetPositionType(IntPtr node, YogaPositionType positionType);

        [FreeFunction]
        public static extern YogaPositionType YGNodeStyleGetPositionType(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetFlexWrap(IntPtr node, YogaWrap flexWrap);

        [FreeFunction]
        public static extern YogaWrap YGNodeStyleGetFlexWrap(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetOverflow(IntPtr node, YogaOverflow flexWrap);

        [FreeFunction]
        public static extern YogaOverflow YGNodeStyleGetOverflow(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetDisplay(IntPtr node, YogaDisplay display);

        [FreeFunction]
        public static extern YogaDisplay YGNodeStyleGetDisplay(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetFlex(IntPtr node, float flex);

        [FreeFunction]
        public static extern void YGNodeStyleSetFlexGrow(IntPtr node, float flexGrow);

        [FreeFunction]
        public static extern float YGNodeStyleGetFlexGrow(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetFlexShrink(IntPtr node, float flexShrink);

        [FreeFunction]
        public static extern float YGNodeStyleGetFlexShrink(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetFlexBasis(IntPtr node, float flexBasis);

        [FreeFunction]
        public static extern void YGNodeStyleSetFlexBasisPercent(IntPtr node, float flexBasis);

        [FreeFunction]
        public static extern void YGNodeStyleSetFlexBasisAuto(IntPtr node);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetFlexBasis(IntPtr node);

        [FreeFunction]
        public static extern float YGNodeGetComputedFlexBasis(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetWidth(IntPtr node, float width);

        [FreeFunction]
        public static extern void YGNodeStyleSetWidthPercent(IntPtr node, float width);

        [FreeFunction]
        public static extern void YGNodeStyleSetWidthAuto(IntPtr node);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetWidth(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetHeight(IntPtr node, float height);

        [FreeFunction]
        public static extern void YGNodeStyleSetHeightPercent(IntPtr node, float height);

        [FreeFunction]
        public static extern void YGNodeStyleSetHeightAuto(IntPtr node);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetHeight(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetMinWidth(IntPtr node, float minWidth);

        [FreeFunction]
        public static extern void YGNodeStyleSetMinWidthPercent(IntPtr node, float minWidth);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetMinWidth(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetMinHeight(IntPtr node, float minHeight);

        [FreeFunction]
        public static extern void YGNodeStyleSetMinHeightPercent(IntPtr node, float minHeight);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetMinHeight(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetMaxWidth(IntPtr node, float maxWidth);

        [FreeFunction]
        public static extern void YGNodeStyleSetMaxWidthPercent(IntPtr node, float maxWidth);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetMaxWidth(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetMaxHeight(IntPtr node, float maxHeight);

        [FreeFunction]
        public static extern void YGNodeStyleSetMaxHeightPercent(IntPtr node, float maxHeight);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetMaxHeight(IntPtr node);

        [FreeFunction]
        public static extern void YGNodeStyleSetAspectRatio(IntPtr node, float aspectRatio);

        [FreeFunction]
        public static extern float YGNodeStyleGetAspectRatio(IntPtr node);

#endregion

#region YG_NODE_STYLE_EDGE_PROPERTY

        [FreeFunction]
        public static extern void YGNodeStyleSetPosition(IntPtr node, YogaEdge edge, float position);

        [FreeFunction]
        public static extern void YGNodeStyleSetPositionPercent(IntPtr node, YogaEdge edge, float position);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetPosition(IntPtr node, YogaEdge edge);

        [FreeFunction]
        public static extern void YGNodeStyleSetMargin(IntPtr node, YogaEdge edge, float margin);

        [FreeFunction]
        public static extern void YGNodeStyleSetMarginPercent(IntPtr node, YogaEdge edge, float margin);

        [FreeFunction]
        public static extern void YGNodeStyleSetMarginAuto(IntPtr node, YogaEdge edge);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetMargin(IntPtr node, YogaEdge edge);

        [FreeFunction]
        public static extern void YGNodeStyleSetPadding(IntPtr node, YogaEdge edge, float padding);

        [FreeFunction]
        public static extern void YGNodeStyleSetPaddingPercent(IntPtr node, YogaEdge edge, float padding);

        [FreeFunction]
        public static extern YogaValueType YGNodeStyleGetPadding(IntPtr node, YogaEdge edge);

        [FreeFunction]
        public static extern void YGNodeStyleSetBorder(IntPtr node, YogaEdge edge, float border);

        [FreeFunction]
        public static extern float YGNodeStyleGetBorder(IntPtr node, YogaEdge edge);

#endregion

#region YG_NODE_LAYOUT_PROPERTY

        [FreeFunction]
        public static extern float YGNodeLayoutGetLeft(IntPtr node);

        [FreeFunction]
        public static extern float YGNodeLayoutGetTop(IntPtr node);

        [FreeFunction]
        public static extern float YGNodeLayoutGetRight(IntPtr node);

        [FreeFunction]
        public static extern float YGNodeLayoutGetBottom(IntPtr node);

        [FreeFunction]
        public static extern float YGNodeLayoutGetWidth(IntPtr node);

        [FreeFunction]
        public static extern float YGNodeLayoutGetHeight(IntPtr node);

        [FreeFunction]
        public static extern float YGNodeLayoutGetMargin(IntPtr node, YogaEdge edge);

        [FreeFunction]
        public static extern float YGNodeLayoutGetPadding(IntPtr node, YogaEdge edge);

        [FreeFunction]
        public static extern YogaDirection YGNodeLayoutGetDirection(IntPtr node);

        #endregion

        #region Context

// BEGIN_UNITY @jonathanma Remove YGNode and YGConfig context to avoid pinning GC objects
// END_UNITY

#endregion
    }
}
// END_UNITY
