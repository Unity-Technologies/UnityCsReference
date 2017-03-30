/**
 * Copyright (c) 2014-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using System;
using UnityEngine.Scripting;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.CSSLayout
{
	[NativeHeader("External/CSSLayout/CSSLayout/Native.bindings.h")]
    internal static partial class Native
    {
        private const string DllName = "CSSLayout";

        static Dictionary<IntPtr, WeakReference> s_MeasureFunctions = new Dictionary<IntPtr, WeakReference>();

// BEGIN_UNITY
// TODO we don't support the logging feature yet
//         [DllImport(DllName)]
//         public static extern void CSSInteropSetLogger(
//             [MarshalAs(UnmanagedType.FunctionPtr)] CSSLogger.Func func);
// END_UNITY
        [FreeFunction]
        public static extern IntPtr CSSNodeNew();

        [FreeFunction]
        public static extern void CSSNodeInit(IntPtr cssNode);

        public static void CSSNodeFree(IntPtr cssNode)
        {
            if (cssNode == IntPtr.Zero) return;
            CSSNodeSetMeasureFunc(cssNode, null);
            CSSNodeFreeInternal(cssNode);
        }

        [NativeMethod(Name = "CSSNodeFree", IsFreeFunction = true, IsThreadSafe = true)]
        static extern void CSSNodeFreeInternal(IntPtr cssNode);

        public static void CSSNodeReset(IntPtr cssNode)
        {
            CSSNodeSetMeasureFunc(cssNode, null);
            CSSNodeResetInternal(cssNode);
        }

        [NativeMethod(Name = "CSSNodeReset", IsFreeFunction = true)]
        static extern void CSSNodeResetInternal(IntPtr cssNode);

        [FreeFunction]
        public static extern int CSSNodeGetInstanceCount();

        [FreeFunction]
        public static extern void CSSLayoutSetExperimentalFeatureEnabled(
            CSSExperimentalFeature feature,
            bool enabled);

        [FreeFunction]
        public static extern bool CSSLayoutIsExperimentalFeatureEnabled(
            CSSExperimentalFeature feature);

        [FreeFunction]
        public static extern void CSSNodeInsertChild(IntPtr node, IntPtr child, uint index);

        [FreeFunction]
        public static extern void CSSNodeRemoveChild(IntPtr node, IntPtr child);

        [FreeFunction]
        public static extern IntPtr CSSNodeGetChild(IntPtr node, uint index);

        [FreeFunction]
        public static extern uint CSSNodeChildCount(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeCalculateLayout(IntPtr node,
                            float availableWidth,
                            float availableHeight,
                            CSSDirection parentDirection);

        [FreeFunction]
        public static extern void CSSNodeMarkDirty(IntPtr node);

        [FreeFunction]
        public static extern bool CSSNodeIsDirty(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodePrint(IntPtr node, CSSPrintOptions options);

        [FreeFunction]
        public static extern bool CSSValueIsUndefined(float value);

        [FreeFunction]
        public static extern void CSSNodeCopyStyle(IntPtr dstNode, IntPtr srcNode);

        #region CSS_NODE_PROPERTY

        [FreeFunction]
        public static extern void CSSNodeSetContext(IntPtr node, IntPtr context);

        [FreeFunction]
        public static extern IntPtr CSSNodeGetContext(IntPtr node);

        public static void CSSNodeSetMeasureFunc(IntPtr node, CSSMeasureFunc measureFunc)
        {
            if (measureFunc != null)
            {
                s_MeasureFunctions[node] = new WeakReference(measureFunc);
                CSSLayoutCallbacks.RegisterWrapper(node);
            }
            else if (s_MeasureFunctions.ContainsKey(node))
            {
                s_MeasureFunctions.Remove(node);
                CSSLayoutCallbacks.UnegisterWrapper(node);
            }
        }

        public static CSSMeasureFunc CSSNodeGetMeasureFunc(IntPtr node)
        {
            WeakReference reference = null;
            if (s_MeasureFunctions.TryGetValue(node, out reference) && reference.IsAlive)
            {
                return reference.Target as CSSMeasureFunc;
            }
            else
            {
                return null;
            }
        }

        [RequiredByNativeCode]
        unsafe public static void CSSNodeMeasureInvoke(IntPtr node, float width, CSSMeasureMode widthMode, float height, CSSMeasureMode heightMode, IntPtr returnValueAddress)
        {
            CSSMeasureFunc func = CSSNodeGetMeasureFunc(node);
            if (func != null)
            {
                (*(CSSSize*)returnValueAddress) = func.Invoke(node, width, widthMode, height, heightMode);
            }
        }

        [FreeFunction]
        public static extern void CSSNodeSetHasNewLayout(IntPtr node, bool hasNewLayout);

        [FreeFunction]
        public static extern bool CSSNodeGetHasNewLayout(IntPtr node);

        #endregion

        #region CSS_NODE_STYLE_PROPERTY

        [FreeFunction]
        public static extern void CSSNodeStyleSetDirection(IntPtr node, CSSDirection direction);

        [FreeFunction]
        public static extern CSSDirection CSSNodeStyleGetDirection(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetFlexDirection(IntPtr node, CSSFlexDirection flexDirection);

        [FreeFunction]
        public static extern CSSFlexDirection CSSNodeStyleGetFlexDirection(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetJustifyContent(IntPtr node, CSSJustify justifyContent);

        [FreeFunction]
        public static extern CSSJustify CSSNodeStyleGetJustifyContent(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetAlignContent(IntPtr node, CSSAlign alignContent);

        [FreeFunction]
        public static extern CSSAlign CSSNodeStyleGetAlignContent(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetAlignItems(IntPtr node, CSSAlign alignItems);

        [FreeFunction]
        public static extern CSSAlign CSSNodeStyleGetAlignItems(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetAlignSelf(IntPtr node, CSSAlign alignSelf);

        [FreeFunction]
        public static extern CSSAlign CSSNodeStyleGetAlignSelf(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetPositionType(IntPtr node, CSSPositionType positionType);

        [FreeFunction]
        public static extern CSSPositionType CSSNodeStyleGetPositionType(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetFlexWrap(IntPtr node, CSSWrap flexWrap);

        [FreeFunction]
        public static extern CSSWrap CSSNodeStyleGetFlexWrap(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetOverflow(IntPtr node, CSSOverflow flexWrap);

        [FreeFunction]
        public static extern CSSOverflow CSSNodeStyleGetOverflow(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetFlex(IntPtr node, float flex);

        [FreeFunction]
        public static extern void CSSNodeStyleSetFlexGrow(IntPtr node, float flexGrow);

        [FreeFunction]
        public static extern float CSSNodeStyleGetFlexGrow(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetFlexShrink(IntPtr node, float flexShrink);

        [FreeFunction]
        public static extern float CSSNodeStyleGetFlexShrink(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetFlexBasis(IntPtr node, float flexBasis);

        [FreeFunction]
        public static extern float CSSNodeStyleGetFlexBasis(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetWidth(IntPtr node, float width);

        [FreeFunction]
        public static extern float CSSNodeStyleGetWidth(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetHeight(IntPtr node, float height);

        [FreeFunction]
        public static extern float CSSNodeStyleGetHeight(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetMinWidth(IntPtr node, float minWidth);

        [FreeFunction]
        public static extern float CSSNodeStyleGetMinWidth(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetMinHeight(IntPtr node, float minHeight);

        [FreeFunction]
        public static extern float CSSNodeStyleGetMinHeight(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetMaxWidth(IntPtr node, float maxWidth);

        [FreeFunction]
        public static extern float CSSNodeStyleGetMaxWidth(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetMaxHeight(IntPtr node, float maxHeight);

        [FreeFunction]
        public static extern float CSSNodeStyleGetMaxHeight(IntPtr node);

        [FreeFunction]
        public static extern void CSSNodeStyleSetAspectRatio(IntPtr node, float aspectRatio);

        [FreeFunction]
        public static extern float CSSNodeStyleGetAspectRatio(IntPtr node);

        #endregion

        #region CSS_NODE_STYLE_EDGE_PROPERTY

        [FreeFunction]
        public static extern void CSSNodeStyleSetPosition(IntPtr node, CSSEdge edge, float position);

        [FreeFunction]
        public static extern float CSSNodeStyleGetPosition(IntPtr node, CSSEdge edge);

        [FreeFunction]
        public static extern void CSSNodeStyleSetMargin(IntPtr node, CSSEdge edge, float margin);

        [FreeFunction]
        public static extern float CSSNodeStyleGetMargin(IntPtr node, CSSEdge edge);

        [FreeFunction]
        public static extern void CSSNodeStyleSetPadding(IntPtr node, CSSEdge edge, float padding);

        [FreeFunction]
        public static extern float CSSNodeStyleGetPadding(IntPtr node, CSSEdge edge);

        [FreeFunction]
        public static extern void CSSNodeStyleSetBorder(IntPtr node, CSSEdge edge, float border);

        [FreeFunction]
        public static extern float CSSNodeStyleGetBorder(IntPtr node, CSSEdge edge);

        #endregion

        #region CSS_NODE_LAYOUT_PROPERTY

        [FreeFunction]
        public static extern float CSSNodeLayoutGetLeft(IntPtr node);

        [FreeFunction]
        public static extern float CSSNodeLayoutGetTop(IntPtr node);

        [FreeFunction]
        public static extern float CSSNodeLayoutGetRight(IntPtr node);

        [FreeFunction]
        public static extern float CSSNodeLayoutGetBottom(IntPtr node);

        [FreeFunction]
        public static extern float CSSNodeLayoutGetWidth(IntPtr node);

        [FreeFunction]
        public static extern float CSSNodeLayoutGetHeight(IntPtr node);

        [FreeFunction]
        public static extern CSSDirection CSSNodeLayoutGetDirection(IntPtr node);

        #endregion
    }
}
