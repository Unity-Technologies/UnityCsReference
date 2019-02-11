// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UIRRepaintUpdater : BaseVisualTreeHierarchyTrackerUpdater
    {
        private UIRStylePainter m_StylePainter;
        private UIRDataChainManager m_UIRDataChainManager;
        private UIRAtlasManager m_AtlasManager;

        private bool m_Started;
        private bool m_Disposed;

        static UIRRepaintUpdater()
        {
            UIR.Utility.GraphicsResourcesRecreate += OnGraphicsResourcesRecreate;
            Font.textureRebuilt += OnFontTextureRebuilt;
            UIR.Utility.EngineUpdate += OnEngineUpdate;
        }

        public UIRRepaintUpdater()
        {
            panelChanged += OnPanelChanged;
        }

        private void OnPanelChanged(BaseVisualElementPanel panel)
        {
            if (panel != null)
                panel.standardShaderChanged += OnPanelStandardShaderChanged;

            Start();
        }

        protected virtual IUIRenderDevice CreateRenderDevice()
        {
            var shader = ResolveShader(panel != null ? panel.standardShader : null);
            return new UIRenderDevice(shader, 1500, 3000, 100, 400);
        }

        private void Start()
        {
            if (m_Disposed)
            {
                Debug.LogError("Attempting to start a disposed repaint updater.");
                return;
            }

            if (m_Started)
                return;

            m_AtlasManager = new UIRAtlasManager();
            m_AtlasManager.ResetPerformed += OnAtlasReset;

            renderDevice = CreateRenderDevice();

            m_StylePainter = new UIRStylePainter(renderDevice, m_AtlasManager);
            m_UIRDataChainManager = new UIRDataChainManager(renderDevice, m_StylePainter);

            m_Started = true;
        }

        private void Stop()
        {
            if (m_Disposed)
            {
                Debug.LogError("Attempting to stop a disposed repaint updater.");
                return;
            }

            if (!m_Started)
                return;

            UIRUtility.FreeDrawChain(renderDevice, rendererChain);

            m_UIRDataChainManager = null;
            m_StylePainter = null;

            if (renderDevice != null)
            {
                renderDevice.Dispose();
                renderDevice = null;
            }

            if (m_AtlasManager != null)
            {
                m_AtlasManager.Dispose();
                m_AtlasManager = null;
            }

            ClearRenderData(visualTree);

            m_Started = false;
        }

        static void ClearRenderData(VisualElement ve)
        {
            ve.uiRenderData = null;

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                VisualElement child = ve.hierarchy[i];
                ClearRenderData(child);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
            {
                Stop();

                if (panel != null)
                    panel.standardShaderChanged -= OnPanelStandardShaderChanged;
            }

            panelChanged -= OnPanelChanged;

            base.Dispose(disposing);
            m_Disposed = true;
        }

        internal UIRenderData uirDataChain { get { return m_UIRDataChainManager.uirDataRoot; } }

        public IUIRenderDevice renderDevice { get; private set; }
        public RendererBase rendererChain { get { return uirDataChain != null ? uirDataChain.innerBegin : null; } }

        internal UIRStylePainter stylePainter { get { return m_StylePainter; } }

        public override string description
        {
            get { return "UIRepaint"; }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            base.OnVersionChanged(ve, versionChangeType);

            if (!m_Started)
                return;

            if (((versionChangeType & VersionChangeType.Transform) == VersionChangeType.Transform) && UIRUtility.IsSkinnedTransformWithoutNesting(ve))
            {
                var renderData = ve.uiRenderData;
                if (renderData != null && renderData.skinningAlloc.size > 0)
                    UIRUtility.UpdateSkinningTransform(renderDevice, renderData);
            }

            if ((versionChangeType & VersionChangeType.Repaint) == VersionChangeType.Repaint)
            {
                var uirData = ve.uiRenderData;
                if (uirData != null)
                    uirData.IncrementVersion();
            }
        }

        public override void Update()
        {
            base.Update();

            if (!m_Started)
                return;

            var topRect = visualTree.layout;
            var projection = Matrix4x4.Ortho(topRect.xMin, topRect.xMax, topRect.yMax, topRect.yMin, -1, 1);
            m_StylePainter.projection = projection;

            if (m_AtlasManager.RequiresReset())
                m_AtlasManager.Reset();

            if (uirDataChain == null)
            {
                m_UIRDataChainManager.BuildTree(panel.visualTree);
            }
            else
            {
                m_UIRDataChainManager.Update();
            }

            m_AtlasManager.Update();

            DrawChain(topRect, projection);
        }

        private void OnPanelStandardShaderChanged()
        {
            if (!m_Started)
                return;

            renderDevice.standardShader = ResolveShader(panel.standardShader);
        }

        protected override void OnHierarchyChange(VisualElement ve, HierarchyChangeType type)
        {
            if (!m_Started)
                return;

            m_UIRDataChainManager.ProcessHierarchyChange(ve, type);
        }

        public event Action<UIRRepaintUpdater> BeforeDrawChain;

        // Overriden in tests
        protected virtual void DrawChain(Rect topRect, Matrix4x4 projection)
        {
            if (BeforeDrawChain != null)
                BeforeDrawChain(this);

            Profiler.BeginSample("DrawChain");
            renderDevice.DrawChain(rendererChain, topRect, projection, m_AtlasManager.atlas);
            Profiler.EndSample();
        }

        private static void ResetAllRepaintUpdaters(bool stop, bool start)
        {
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                var panel = it.Current.Value;
                var uirUpdater = panel.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
                if (uirUpdater == null)
                    continue;

                if (stop)
                    uirUpdater.Stop();

                if (start)
                    uirUpdater.Start();
            }
        }

        private static void OnGraphicsResourcesRecreate(bool recreate)
        {
            if (!recreate)
                UIRenderDevice.PrepareForGfxDeviceRecreate();

            ResetAllRepaintUpdaters(!recreate, recreate);

            if (!recreate)
                UIRenderDevice.FlushAllPendingDeviceDisposes();
            else UIRenderDevice.WrapUpGfxDeviceRecreate();
        }

        static void ProcessAtlasResetRecursive(VisualElement ve)
        {
            if (ve.uiRenderData != null && ve.uiRenderData.usesTextures)
                ve.MarkDirtyRepaint();

            int childCount = ve.hierarchy.childCount;
            for (int i = 0; i < childCount; ++i)
                ProcessAtlasResetRecursive(ve.hierarchy[i]);
        }

        private void OnAtlasReset(object sender, EventArgs e)
        {
            // We don't care about elements for which we don't have render data yet (e.g. recently added), because we
            // don't need to mark them dirty: they are already dirty. However, we might include false positives that
            // have been removed.
            ProcessAtlasResetRecursive(visualTree);
        }

        private Shader ResolveShader(Shader shader)
        {
            if (shader == null)
                shader = Shader.Find(UIRUtility.k_DefaultShaderName);
            Debug.Assert(shader != null, "Failed to load the shader UIRDefault shader");
            return shader;
        }

        private static void PerformRequiredAtlasManagerReset()
        {
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                var panel = it.Current.Value;
                var updater = panel.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
                if (updater == null)
                    continue;

                UIRAtlasManager atlasManager = updater.m_AtlasManager;
                if (atlasManager.RequiresReset())
                    atlasManager.Reset();
            }
        }

        private static void PerformFontReset(Font obj)
        {
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                var panel = it.Current.Value;
                var updater = panel.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
                if (updater == null)
                    continue;

                updater.stylePainter.OnFontTextureRebuilt(obj);
                updater.visualTree.IncrementVersion(VersionChangeType.Repaint);
            }
        }

        private static void OnFontTextureRebuilt(Font obj)
        {
            PerformFontReset(obj);
        }

        private static void OnEngineUpdate()
        {
            PerformRequiredAtlasManagerReset();
        }
    }
}
