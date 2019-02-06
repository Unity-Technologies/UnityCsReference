// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;
using UnityEngine.Profiling;

namespace UnityEngine.UIElements
{
    internal class UIRDataChainBuilder
    {
        private UIRStylePainter m_StylePainter;

        bool m_ScaleHasChanged;

        private List<UIRenderData> m_TextRenderData = new List<UIRenderData>(1024);

        public UIRDataChainBuilder(UIRStylePainter stylePainter)
        {
            m_StylePainter = stylePainter;
        }

        // Returns null if nothing was drawn
        public UIRenderData BuildChain(VisualElement visualTree)
        {
            Profiler.BeginSample("UIRDataChainBuilder.BuildChain");

            m_TextRenderData.Clear();

            TraverseVisualTree(visualTree, null, null);
            UpdateText();

            Profiler.EndSample();

            return visualTree.uiRenderData;
        }

        private UIRenderData TraverseVisualTree(VisualElement ve, UIRenderData parentData, UIRenderData previousData)
        {
            var uirData = ve.uiRenderData;
            if (ve.uiRenderData == null)
            {
                uirData = new UIRenderData();
                uirData.visualElement = ve;
                ve.uiRenderData = uirData;
            }
            else
                uirData.Disconnect();

            // Assign inherited data.
            uirData.SetParent(parentData);
            uirData.effectiveRenderHint = ve.renderHint;

            if (ve.resolvedStyle.visibility == Visibility.Hidden || ve.resolvedStyle.opacity < Mathf.Epsilon || ve.resolvedStyle.display == DisplayStyle.None)
                return previousData;

            bool isViewTransform = (ve.renderHint & RenderHint.ViewTransform) == RenderHint.ViewTransform;
            bool isSkinningTransform = (ve.renderHint & RenderHint.SkinningTransform) == RenderHint.SkinningTransform;

            if (isSkinningTransform)
            {
                // Ignore nested skinned transforms
                if (uirData.inheritedSkinningTransformData == null && !uirData.overridesSkinningTransform)
                    CreateSkinningTransform(uirData);
            }


            if (isViewTransform)
            {
                if (uirData.inheritedViewTransformData != null)
                {
                    // Nested view transforms aren't supported
                    uirData.effectiveRenderHint &= ~RenderHint.ViewTransform;
                    isViewTransform = false;
                }
                else
                {
                    // The access to the worldTransform is gated because dirty transforms are very slow to compute. This
                    // can increase processing time by 300%.
                    float scale = ve.worldTransform.m00;
                    if (scale != uirData.scale)
                    {
                        uirData.scale = scale;
                        m_ScaleHasChanged = true;
                    }
                }
            }

            m_StylePainter.Paint(uirData);

            if (uirData.textMeshHandle != null)
                m_TextRenderData.Add(uirData);

            Debug.Assert(!isViewTransform || uirData.overridesViewTransform && uirData.cachedViewRenderer != null, "Invalid view transform draw chain");

            // The style painter must at least add an EmptyRenderer.
            Debug.Assert(uirData.innerBegin != null);

            // Connect.
            if (previousData != null)
            {
                if (parentData == previousData)
                    parentData.SetNextNestedData(uirData);
                else
                    previousData.SetNextData(uirData);
            }

            // Traverse children.
            UIRenderData childPrevious = uirData;
            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                VisualElement child = ve.hierarchy[i];
                childPrevious = TraverseVisualTree(child, uirData, childPrevious);
            }

            m_StylePainter.PostPaint(uirData);

            if (uirData.innerNestedEnd != null)
                return uirData;
            return childPrevious;
        }

        private void UpdateText()
        {
            if (!m_ScaleHasChanged && !m_StylePainter.isFontDirty)
                return;

            Profiler.BeginSample("UpdateText");

            m_StylePainter.isFontDirty = false;

            DrawText();

            if (m_StylePainter.isFontDirty)
            {
                // The font texture was recreated. We need to repaint the text once again since
                // we don't have any guarantee that the glyphs are at the same spot in the font texture.
                DrawText();
            }

            m_ScaleHasChanged = false;
            m_StylePainter.isFontDirty = false;

            Profiler.EndSample();
        }

        private void DrawText()
        {
            foreach (var rd in m_TextRenderData)
            {
                m_StylePainter.BeginText(rd);
                m_StylePainter.DrawText(rd.textParams, rd.textMeshHandle);
                m_StylePainter.EndText();
            }
        }

        private void CreateSkinningTransform(UIRenderData uirData)
        {
            var device = m_StylePainter.renderDevice;
            uirData.skinningAlloc = device.AllocateTransform();
            uirData.overridesSkinningTransform = uirData.skinningAlloc.size > 0;
            if (uirData.overridesSkinningTransform)
                UIRUtility.UpdateSkinningTransform(device, uirData);
        }
    }
}
