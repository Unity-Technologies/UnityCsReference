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
using Unity.Bindings;

namespace UnityEngine.CSSLayout
{
	[NativeType(Header = "External/CSSLayout/CSSLayout/CSSLayout.h")]
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
        [NativeMethod(IsFreeFunction = true)]
        public static extern IntPtr CSSNodeNew();

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeInit(IntPtr cssNode);

        public static void CSSNodeFree(IntPtr cssNode)
        {
            if (cssNode == IntPtr.Zero) return;
            CSSNodeSetMeasureFunc(cssNode, null);
            CSSNodeFreeInternal(cssNode);
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe=true, Name="CSSNodeFree")]
        static extern void CSSNodeFreeInternal(IntPtr cssNode);

        public static void CSSNodeReset(IntPtr cssNode)
        {
            CSSNodeSetMeasureFunc(cssNode, null);
            CSSNodeResetInternal(cssNode);
        }

        [NativeMethod(IsFreeFunction = true, Name="CSSNodeReset")]
        static extern void CSSNodeResetInternal(IntPtr cssNode);

        [NativeMethod(IsFreeFunction = true)]
        public static extern int CSSNodeGetInstanceCount();

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSLayoutSetExperimentalFeatureEnabled(
            CSSExperimentalFeature feature,
            bool enabled);

        [NativeMethod(IsFreeFunction = true)]
        public static extern bool CSSLayoutIsExperimentalFeatureEnabled(
            CSSExperimentalFeature feature);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeInsertChild(IntPtr node, IntPtr child, uint index);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeRemoveChild(IntPtr node, IntPtr child);

        [NativeMethod(IsFreeFunction = true)]
        public static extern IntPtr CSSNodeGetChild(IntPtr node, uint index);

        [NativeMethod(IsFreeFunction = true)]
        public static extern uint CSSNodeChildCount(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeCalculateLayout(IntPtr node,
                            float availableWidth,
                            float availableHeight,
                            CSSDirection parentDirection);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeMarkDirty(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern bool CSSNodeIsDirty(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodePrint(IntPtr node, CSSPrintOptions options);

        [NativeMethod(IsFreeFunction = true)]
        public static extern bool CSSValueIsUndefined(float value);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeCopyStyle(IntPtr dstNode, IntPtr srcNode);

        #region CSS_NODE_PROPERTY

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeSetContext(IntPtr node, IntPtr context);

        [NativeMethod(IsFreeFunction = true)]
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

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeSetHasNewLayout(IntPtr node, bool hasNewLayout);

        [NativeMethod(IsFreeFunction = true)]
        public static extern bool CSSNodeGetHasNewLayout(IntPtr node);

        #endregion

        #region CSS_NODE_STYLE_PROPERTY

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetDirection(IntPtr node, CSSDirection direction);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSDirection CSSNodeStyleGetDirection(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetFlexDirection(IntPtr node, CSSFlexDirection flexDirection);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSFlexDirection CSSNodeStyleGetFlexDirection(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetJustifyContent(IntPtr node, CSSJustify justifyContent);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSJustify CSSNodeStyleGetJustifyContent(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetAlignContent(IntPtr node, CSSAlign alignContent);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSAlign CSSNodeStyleGetAlignContent(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetAlignItems(IntPtr node, CSSAlign alignItems);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSAlign CSSNodeStyleGetAlignItems(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetAlignSelf(IntPtr node, CSSAlign alignSelf);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSAlign CSSNodeStyleGetAlignSelf(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetPositionType(IntPtr node, CSSPositionType positionType);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSPositionType CSSNodeStyleGetPositionType(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetFlexWrap(IntPtr node, CSSWrap flexWrap);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSWrap CSSNodeStyleGetFlexWrap(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetOverflow(IntPtr node, CSSOverflow flexWrap);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSOverflow CSSNodeStyleGetOverflow(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetFlex(IntPtr node, float flex);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetFlexGrow(IntPtr node, float flexGrow);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetFlexGrow(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetFlexShrink(IntPtr node, float flexShrink);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetFlexShrink(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetFlexBasis(IntPtr node, float flexBasis);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetFlexBasis(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetWidth(IntPtr node, float width);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetWidth(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetHeight(IntPtr node, float height);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetHeight(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetMinWidth(IntPtr node, float minWidth);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetMinWidth(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetMinHeight(IntPtr node, float minHeight);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetMinHeight(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetMaxWidth(IntPtr node, float maxWidth);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetMaxWidth(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetMaxHeight(IntPtr node, float maxHeight);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetMaxHeight(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetAspectRatio(IntPtr node, float aspectRatio);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetAspectRatio(IntPtr node);

        #endregion

        #region CSS_NODE_STYLE_EDGE_PROPERTY

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetPosition(IntPtr node, CSSEdge edge, float position);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetPosition(IntPtr node, CSSEdge edge);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetMargin(IntPtr node, CSSEdge edge, float margin);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetMargin(IntPtr node, CSSEdge edge);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetPadding(IntPtr node, CSSEdge edge, float padding);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetPadding(IntPtr node, CSSEdge edge);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void CSSNodeStyleSetBorder(IntPtr node, CSSEdge edge, float border);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeStyleGetBorder(IntPtr node, CSSEdge edge);

        #endregion

        #region CSS_NODE_LAYOUT_PROPERTY

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeLayoutGetLeft(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeLayoutGetTop(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeLayoutGetRight(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeLayoutGetBottom(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeLayoutGetWidth(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern float CSSNodeLayoutGetHeight(IntPtr node);

        [NativeMethod(IsFreeFunction = true)]
        public static extern CSSDirection CSSNodeLayoutGetDirection(IntPtr node);

        #endregion
    }
}
