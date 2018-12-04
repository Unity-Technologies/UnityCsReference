// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UIRDataChainManager
    {
        private UIRenderData m_ChainRoot;
        private UIRDataChainBuilder m_ChainBuilder;

        private IUIRenderDevice m_RenderDevice;
        private UIRStylePainter m_StylePainter;

        private VisualElement m_VisualTree;

        public UIRDataChainManager(IUIRenderDevice renderDevice, UIRStylePainter stylePainter)
        {
            m_RenderDevice = renderDevice;
            m_StylePainter = stylePainter;
            m_ChainBuilder = new UIRDataChainBuilder(m_StylePainter);
        }

        public UIRenderData uirDataRoot { get { return m_ChainRoot; } }

        public void BuildTree(VisualElement root)
        {
            m_VisualTree = root;
            m_ChainRoot = m_ChainBuilder.BuildChain(root);
        }

        public void Update()
        {
            m_ChainRoot = m_ChainBuilder.BuildChain(m_VisualTree);
        }

        public void ProcessHierarchyChange(VisualElement ve, HierarchyChangeType changeType)
        {
            if (changeType == HierarchyChangeType.Remove)
                RemoveUIRDataRecursive(ve);
        }

        private void RemoveUIRDataRecursive(VisualElement ve)
        {
            var uirData = ve.uiRenderData;
            if (uirData != null)
            {
                if (uirData.overridesSkinningTransform)
                {
                    m_RenderDevice.FreeTransform(uirData.skinningAlloc);
                    uirData.overridesSkinningTransform = false;
                }

                if (uirData.overridesClippingRect)
                {
                    m_RenderDevice.FreeClipping(uirData.clippingRectAlloc);
                    uirData.overridesClippingRect = false;
                }

                uirData.Disconnect();
                uirData.ResetInnerChain(m_RenderDevice);
                ve.uiRenderData = null;
            }

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                RemoveUIRDataRecursive(child);
            }
        }
    }
}
