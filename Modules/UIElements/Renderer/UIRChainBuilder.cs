// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define UIR_DEBUG_CHAIN_BUILDER
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.UIElements.UIR
{
    internal struct ChainBuilderStats
    {
        public uint elementsAdded, elementsRemoved;
        public uint recursiveClipUpdates, recursiveClipUpdatesExpanded;
        public uint recursiveTransformUpdates, recursiveTransformUpdatesExpanded;
        public uint recursiveVisualUpdates, recursiveVisualUpdatesExpanded, nonRecursiveVisualUpdates;
        public uint nudgeTransformed, boneTransformed, skipTransformed, visualUpdateTransformed;
        public uint updatedMeshAllocations, newMeshAllocations;
        public uint groupTransformElementsChanged;
        public uint textUpdates;
        public uint clipListCleanup, transformListCleanup, visualListCleanup;
    }

    internal class RenderChain : IDisposable
    {
        RenderChainCommand m_FirstCommand;
        uint m_DirtyID; // A monotonically increasing ID used to avoid double processing of some elements
        VisualElement m_FirstDirtyVisuals, m_FirstDirtyTransform, m_FirstDirtyClipping;
        VisualElement m_LastDirtyVisuals, m_LastDirtyTransform, m_LastDirtyClipping;
        Pool<RenderChainCommand> m_CommandPool = new Pool<RenderChainCommand>();
        ChainBuilderStats m_Stats;
        uint m_StatsElementsAdded, m_StatsElementsRemoved;
        uint m_StatsClipListCleanup, m_StatsTransformListCleanup, m_StatsVisualListCleanup;


        // Text regen stuff. Will be removed when UIE uses SDF fonts
        VisualElement m_FirstTextElement;
        Implementation.UIRTextUpdatePainter m_TextUpdatePainter;
        int m_TextElementCount;
        int m_DirtyTextStartIndex;
        int m_DirtyTextRemaining;
        bool m_FontWasReset;
        Vector2 m_LastGroupTransformElementScale = new Vector2(1, 1);

        internal RenderChainCommand firstCommand { get { return m_FirstCommand; } }

        // Profiling
        static CustomSampler s_RenderSampler = CustomSampler.Create("RenderChain.Draw");
        static CustomSampler s_ClipProcessingSampler = CustomSampler.Create("RenderChain.UpdateClips");
        static CustomSampler s_TransformProcessingSampler = CustomSampler.Create("RenderChain.UpdateTransforms");
        static CustomSampler s_VisualsProcessingSampler = CustomSampler.Create("RenderChain.UpdateVisuals");
        static CustomSampler s_TextRegenSampler = CustomSampler.Create("RenderChain.RegenText");


        public RenderChain(IPanel panel, Shader standardShader)
        {
            if (disposed)
                DisposeHelper.NotifyDisposedUsed(this);

            this.panel = panel;
            device = new UIRenderDevice(Implementation.RenderEvents.ResolveShader(standardShader));

            atlasManager = new UIRAtlasManager();
            atlasManager.ResetPerformed += OnAtlasReset;

            painter = new Implementation.UIRStylePainter(this);

            Font.textureRebuilt += OnFontReset;
        }

        protected RenderChain(IPanel panel, UIRenderDevice device, UIRAtlasManager atlasManager)
        {
            if (disposed)
                DisposeHelper.NotifyDisposedUsed(this);

            this.panel = panel;
            this.device = device;
            this.atlasManager = atlasManager;
            if (atlasManager != null)
                atlasManager.ResetPerformed += OnAtlasReset;
            painter = new Implementation.UIRStylePainter(this);
            Font.textureRebuilt += OnFontReset;
        }

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
                Font.textureRebuilt -= OnFontReset;
                painter?.Dispose();
                m_TextUpdatePainter?.Dispose();
                atlasManager?.Dispose();
                device?.Dispose();

                painter = null;
                m_TextUpdatePainter = null;
                atlasManager = null;
                device = null;
            }
            else
                DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        internal ChainBuilderStats stats { get { return m_Stats; } }

        public void Render(Rect topRect, Matrix4x4 projection)
        {
            s_RenderSampler.Begin();
            m_Stats = new ChainBuilderStats();
            m_Stats.elementsAdded += m_StatsElementsAdded;
            m_Stats.elementsRemoved += m_StatsElementsRemoved;
            m_Stats.clipListCleanup += m_StatsClipListCleanup;
            m_Stats.transformListCleanup += m_StatsTransformListCleanup;
            m_Stats.visualListCleanup += m_StatsVisualListCleanup;
            m_StatsElementsAdded = m_StatsElementsRemoved = 0;
            m_StatsClipListCleanup = m_StatsTransformListCleanup = m_StatsVisualListCleanup = 0;

            if (atlasManager?.RequiresReset() == true)
                atlasManager.Reset(); // May cause a dirty repaint


            m_DirtyID++;
            VisualElement dirty = m_FirstDirtyClipping;
            s_ClipProcessingSampler.Begin();
            while (dirty != null)
            {
                if (dirty.renderChainData.isInChain && dirty.renderChainData.dirtyID != m_DirtyID)
                    Implementation.RenderEvents.ProcessOnClippingChanged(this, dirty, m_DirtyID, device, ref m_Stats);
                dirty.renderChainData.dirtiedClipping = false;
                var old = dirty;
                dirty = dirty.renderChainData.nextDirtyClipping;
                old.renderChainData.nextDirtyClipping = null;
            }
            s_ClipProcessingSampler.End();
            m_FirstDirtyClipping = m_LastDirtyClipping = null;

            m_DirtyID++;
            dirty = m_FirstDirtyTransform;
            s_TransformProcessingSampler.Begin();
            while (dirty != null)
            {
                if (dirty.renderChainData.isInChain && dirty.renderChainData.dirtyID != m_DirtyID)
                    Implementation.RenderEvents.ProcessOnTransformChanged(this, dirty, m_DirtyID, device, ref m_Stats);
                dirty.renderChainData.dirtiedTransform = false;
                var old = dirty;
                dirty = dirty.renderChainData.nextDirtyTransform;
                old.renderChainData.nextDirtyTransform = null;
            }
            s_TransformProcessingSampler.End();
            m_FirstDirtyTransform = m_LastDirtyTransform = null;

            m_DirtyID++;
            dirty = m_FirstDirtyVisuals;
            s_VisualsProcessingSampler.Begin();
            while (dirty != null)
            {
                if (dirty.renderChainData.isInChain && dirty.renderChainData.dirtyID != m_DirtyID)
                    Implementation.RenderEvents.ProcessOnVisualsChanged(this, dirty, m_DirtyID, ref m_Stats);
                dirty.renderChainData.dirtiedVisuals = 0;
                var old = dirty;
                dirty = dirty.renderChainData.nextDirtyVisuals;
                old.renderChainData.nextDirtyVisuals = null;
            }
            s_VisualsProcessingSampler.End();
            m_FirstDirtyVisuals = m_LastDirtyVisuals = null;

            ProcessTextRegen(true);

            if (m_FontWasReset)
            {
                // We regenerate the text when the font texture was reset since we don't have any guarantees
                // the the glyphs are going to end up at the same spot in the texture.
                // Up to two passes may be necessary with time-slicing turned off to fully update the text.
                const int kMaxTextPasses = 2;
                for (int i = 0; i < kMaxTextPasses; ++i)
                {
                    if (!m_FontWasReset)
                        break;
                    m_FontWasReset = false;
                    ProcessTextRegen(false);
                }
            }


            atlasManager?.Update(); // Commit new requests if any

            if (BeforeDrawChain != null)
                BeforeDrawChain(device);

            Exception immediateException = null;
            device.DrawChain(m_FirstCommand, topRect, projection, atlasManager?.atlas, ref immediateException);

            s_RenderSampler.End();

            if (immediateException != null)
                throw immediateException;

            if (drawStats)
                DrawStats();
        }

        private void ProcessTextRegen(bool timeSliced)
        {
            if ((timeSliced && m_DirtyTextRemaining == 0) || m_TextElementCount == 0)
                return;

            s_TextRegenSampler.Begin();
            if (m_TextUpdatePainter == null)
                m_TextUpdatePainter = new Implementation.UIRTextUpdatePainter();

            var dirty = m_FirstTextElement;
            m_DirtyTextStartIndex = timeSliced ? m_DirtyTextStartIndex % m_TextElementCount : 0;
            for (int i = 0; i < m_DirtyTextStartIndex; i++)
                dirty = dirty.renderChainData.nextText;
            if (dirty == null)
                dirty = m_FirstTextElement;

            int maxCount = timeSliced ? Math.Min(50, m_DirtyTextRemaining) : m_TextElementCount;
            for (int i = 0; i < maxCount; i++)
            {
                Implementation.RenderEvents.ProcessRegenText(this, dirty, m_TextUpdatePainter, device, ref m_Stats);
                dirty = dirty.renderChainData.nextText;
                m_DirtyTextStartIndex++;
                if (dirty == null)
                {
                    dirty = m_FirstTextElement;
                    m_DirtyTextStartIndex = 0;
                }
            }

            m_DirtyTextRemaining = Math.Max(0, m_DirtyTextRemaining - maxCount);
            if (m_DirtyTextRemaining > 0)
                (panel as BaseVisualElementPanel)?.OnVersionChanged(m_FirstTextElement, VersionChangeType.Transform); // Force a window refresh
            s_TextRegenSampler.End();
        }

        public event Action<UIRenderDevice> BeforeDrawChain;

        #region UIElements event handling callbacks
        public void UIEOnStandardShaderChanged(Shader standardShader) { Implementation.RenderEvents.OnStandardShaderChanged(this, standardShader); }
        public void UIEOnChildAdded(VisualElement parent, VisualElement ve, int index)
        {
            if (parent != null && !parent.renderChainData.isInChain)
                return; // Ignore it until its parent gets ultimately added
            m_StatsElementsAdded += Implementation.RenderEvents.OnChildAdded(this, parent, ve, index);
            Debug.Assert(ve.renderChainData.isInChain);
        }

        public void UIEOnChildrenReordered(VisualElement ve)
        {
            Implementation.RenderEvents.OnChildrenReordered(this, ve);
        }

        public void UIEOnChildRemoving(VisualElement ve)
        {
            var removalInfo = new Implementation.RemovalInfo();
            m_StatsElementsRemoved += Implementation.RenderEvents.OnChildRemoving(this, ve, ref removalInfo);
            Debug.Assert(!ve.renderChainData.isInChain);
            CleanupDirtyLists(removalInfo.anyDirtiedClipping, removalInfo.anyDirtiedTransform, removalInfo.anyDirtiedVisuals);
        }

        public void CleanupDirtyLists(bool cleanClipping, bool cleanTransform, bool cleanVisuals)
        {
            if (cleanClipping)
            {
                VisualElement first = null;
                VisualElement last = null;
                VisualElement current = m_FirstDirtyClipping;
                VisualElement next = null;
                while (current != null)
                {
                    next = current.renderChainData.nextDirtyClipping;
                    if (current.renderChainData.isInChain)
                    {
                        first = first ?? current;
                        last = current;
                    }
                    else
                    {
                        if (last != null)
                            last.renderChainData.nextDirtyClipping = next;
                        current.renderChainData.nextDirtyClipping = null;
                    }

                    current = next;
                    m_StatsClipListCleanup++;
                }

                m_FirstDirtyClipping = first;
                m_LastDirtyClipping = last;
            }

            if (cleanTransform)
            {
                // Reset transform.
                VisualElement first = null;
                VisualElement last = null;
                VisualElement current = m_FirstDirtyTransform;
                VisualElement next = null;
                while (current != null)
                {
                    next = current.renderChainData.nextDirtyTransform;
                    if (current.renderChainData.isInChain)
                    {
                        first = first ?? current;
                        last = current;
                    }
                    else
                    {
                        if (last != null)
                            last.renderChainData.nextDirtyTransform = next;
                        current.renderChainData.nextDirtyTransform = null;
                    }

                    current = next;
                    m_StatsTransformListCleanup++;
                }

                m_FirstDirtyTransform = first;
                m_LastDirtyTransform = last;
            }

            if (cleanVisuals)
            {
                VisualElement first = null;
                VisualElement last = null;
                VisualElement current = m_FirstDirtyVisuals;
                VisualElement next = null;
                while (current != null)
                {
                    next = current.renderChainData.nextDirtyVisuals;
                    if (current.renderChainData.isInChain)
                    {
                        first = first ?? current;
                        last = current;
                    }
                    else
                    {
                        if (last != null)
                            last.renderChainData.nextDirtyVisuals = next;
                        current.renderChainData.nextDirtyVisuals = null;
                    }

                    current = next;
                    m_StatsVisualListCleanup++;
                }

                m_FirstDirtyVisuals = first;
                m_LastDirtyVisuals = last;
            }
        }

        public void UIEOnTransformChanged(VisualElement ve) { Implementation.RenderEvents.OnTransformChanged(this, ve); }
        public void UIEOnClippingChanged(VisualElement ve) { Implementation.RenderEvents.OnClippingChanged(this, ve); }
        public void UIEOnVisualsChanged(VisualElement ve, bool hierarchical) { Implementation.RenderEvents.OnVisualsChanged(this, ve, hierarchical); }
        #endregion

        internal IPanel panel { get; private set; }
        internal UIRenderDevice device { get; private set; }
        internal UIRAtlasManager atlasManager { get; private set; }
        internal Implementation.UIRStylePainter painter { get; private set; }
        internal bool drawStats { get; set; }

        internal RenderChainCommand AllocCommand()
        {
            var cmd = m_CommandPool.Get();
            cmd.Reset();
            return cmd;
        }

        internal void FreeCommand(RenderChainCommand cmd)
        {
            m_CommandPool.Return(cmd);
        }

        internal void OnRenderCommandAdded(RenderChainCommand firstCommand)
        {
            if (firstCommand.prev == null)
                m_FirstCommand = firstCommand;
        }

        internal void OnRenderCommandRemoved(RenderChainCommand firstCommand, RenderChainCommand lastCommand)
        {
            if (firstCommand.prev == null)
                m_FirstCommand = lastCommand.next;
        }

        internal void AddTextElement(VisualElement ve)
        {
            if (m_FirstTextElement != null)
            {
                m_FirstTextElement.renderChainData.prevText = ve;
                ve.renderChainData.nextText = m_FirstTextElement;
            }
            m_FirstTextElement = ve;
            m_TextElementCount++;
        }

        internal void RemoveTextElement(VisualElement ve)
        {
            if (ve.renderChainData.prevText != null)
                ve.renderChainData.prevText.renderChainData.nextText = ve.renderChainData.nextText;
            if (ve.renderChainData.nextText != null)
                ve.renderChainData.nextText.renderChainData.prevText = ve.renderChainData.prevText;
            if (m_FirstTextElement == ve)
                m_FirstTextElement = ve.renderChainData.nextText;
            ve.renderChainData.prevText = ve.renderChainData.nextText = null;
            m_TextElementCount--;
        }

        internal void OnGroupTransformElementChangedTransform(VisualElement ve)
        {
            // This is a hack for graph view until UIE moves to TMP
            if ((ve.worldTransform.m00 != m_LastGroupTransformElementScale.x) ||
                (ve.worldTransform.m11 != m_LastGroupTransformElementScale.y))
            {
                m_DirtyTextRemaining = m_TextElementCount;
                m_LastGroupTransformElementScale.x = ve.worldTransform.m00;
                m_LastGroupTransformElementScale.y = ve.worldTransform.m11;
            }
        }

        internal void OnVisualsChanged(VisualElement ve, bool hierarchical)
        {
            ve.renderChainData.dirtiedVisuals = hierarchical ? (byte)2 : (byte)1;
            if (m_LastDirtyVisuals != null)
            {
                m_LastDirtyVisuals.renderChainData.nextDirtyVisuals = ve;
                m_LastDirtyVisuals = ve;
            }
            else m_FirstDirtyVisuals = m_LastDirtyVisuals = ve;
        }

        internal void OnClippingChanged(VisualElement ve)
        {
            ve.renderChainData.dirtiedClipping = true;
            if (m_LastDirtyClipping != null)
            {
                m_LastDirtyClipping.renderChainData.nextDirtyClipping = ve;
                m_LastDirtyClipping = ve;
            }
            else m_FirstDirtyClipping = m_LastDirtyClipping = ve;
        }

        internal void OnTransformChanged(VisualElement ve)
        {
            ve.renderChainData.dirtiedTransform = true;
            if (m_LastDirtyTransform != null)
            {
                m_LastDirtyTransform.renderChainData.nextDirtyTransform = ve;
                m_LastDirtyTransform = ve;
            }
            else m_FirstDirtyTransform = m_LastDirtyTransform = ve;
        }

        internal void BeforeRenderDeviceRelease()
        {
            Debug.Assert(device != null);

            // Simply zero out all mesh data allocations since the entire device will be disposed, so no need to be nice about freeing
            // The actual render commands may still hold onto mesh handles, but we don't care, as these
            // will be regenerated upon recreation. It is important though that they maintain their links
            // as to avoid the slow relinking code path
            var ve = GetFirstElementInPanel(m_FirstCommand?.owner);
            while (ve != null)
            {
                ve.renderChainData.closingData = ve.renderChainData.data = null;
                ve.renderChainData.transformID = new Alloc();
                ve = ve.renderChainData.next;
            }

            painter.Dispose();
            painter = null;
            device.Dispose();
            device = null;
        }

        internal void AfterRenderDeviceRelease()
        {
            if (disposed)
                DisposeHelper.NotifyDisposedUsed(this);

            Debug.Assert(device == null);
            device = new UIRenderDevice(Implementation.RenderEvents.ResolveShader((panel as BaseVisualElementPanel)?.standardShader));

            Debug.Assert(painter == null);
            painter = new Implementation.UIRStylePainter(this);
            var ve = GetFirstElementInPanel(m_FirstCommand?.owner);
            while (ve != null)
            {
                Implementation.RenderEvents.OnRestoreTransformIDs(ve, device);
                UIEOnVisualsChanged(ve, false); // Marking dirty will repaint and have the data regenerated
                ve = ve.renderChainData.next;
            }
        }

        void OnAtlasReset(object sender, EventArgs e)
        {
            // Cause a regen on textured elements to get the new UVs from the atlas
            var ve = m_FirstCommand?.owner;
            while (ve != null)
            {
                if (ve.renderChainData.usesAtlas)
                    UIEOnVisualsChanged(ve, false);
                ve = ve.renderChainData.next;
            }
        }

        void OnFontReset(Font font)
        {
            m_FontWasReset = true;
        }

        void DrawStats()
        {
            Color defaultContentColor = GUI.contentColor;
            bool realDevice = device as UIRenderDevice != null;
            float y_off = 12;
            var rc = new Rect(30, 60, 1000, 100);
            GUI.Box(new Rect(20, 40, 200, realDevice ? 380 : 268), "UIElements Draw Stats");
            GUI.Label(rc, "Elements added\t: " + m_Stats.elementsAdded); rc.y += y_off;
            GUI.Label(rc, "Elements removed\t: " + m_Stats.elementsRemoved); rc.y += y_off;
            GUI.Label(rc, "Mesh allocs allocated\t: " + m_Stats.newMeshAllocations); rc.y += y_off;
            GUI.Label(rc, "Mesh allocs updated\t: " + m_Stats.updatedMeshAllocations); rc.y += y_off;
            GUI.Label(rc, "Clip update roots\t: " + m_Stats.recursiveClipUpdates); rc.y += y_off;
            GUI.Label(rc, "Clip update total\t: " + m_Stats.recursiveClipUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Xform update roots\t: " + m_Stats.recursiveTransformUpdates); rc.y += y_off;
            GUI.Label(rc, "Xform update total\t: " + m_Stats.recursiveTransformUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Xformed by bone\t: " + m_Stats.boneTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by skipping\t: " + m_Stats.skipTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by nudging\t: " + m_Stats.nudgeTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by repaint\t: " + m_Stats.visualUpdateTransformed); rc.y += y_off;
            GUI.Label(rc, "Visual update roots\t: " + m_Stats.recursiveVisualUpdates); rc.y += y_off;
            GUI.Label(rc, "Visual update total\t: " + m_Stats.recursiveVisualUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Visual update flats\t: " + m_Stats.nonRecursiveVisualUpdates); rc.y += y_off;
            GUI.Label(rc, "Group-xform updates\t: " + m_Stats.groupTransformElementsChanged); rc.y += y_off;
            GUI.Label(rc, "Text regens\t: " + m_Stats.textUpdates); rc.y += y_off;

            // Mark the following three stats with red if they exceed a certain value to help identify when they bust the reasonable limits
            GUI.contentColor = m_Stats.clipListCleanup < 10 ? defaultContentColor : Color.red;
            GUI.Label(rc, "Clip list cleanup\t: " + m_Stats.clipListCleanup); rc.y += y_off;

            GUI.contentColor = m_Stats.transformListCleanup < 10 ? defaultContentColor : Color.red;
            GUI.Label(rc, "Xform list cleanup\t: " + m_Stats.transformListCleanup); rc.y += y_off;

            GUI.contentColor = m_Stats.visualListCleanup < 10 ? defaultContentColor : Color.red;
            GUI.Label(rc, "Visual list cleanup\t: " + m_Stats.visualListCleanup); rc.y += y_off;

            GUI.contentColor = defaultContentColor;

            if (!realDevice)
                return;

            rc.y += y_off;
            var drawStats = ((UIRenderDevice)device).GatherDrawStatistics();
            GUI.Label(rc, "Frame index\t: " + drawStats.currentFrameIndex); rc.y += y_off;
            GUI.Label(rc, "Command count\t: " + drawStats.commandCount); rc.y += y_off;
            GUI.Label(rc, "Draw commands\t: " + drawStats.drawCommandCount); rc.y += y_off;
            GUI.Label(rc, "Draw range start\t: " + drawStats.currentDrawRangeStart); rc.y += y_off;
            GUI.Label(rc, "Draw ranges\t: " + drawStats.drawRangeCount); rc.y += y_off;
            GUI.Label(rc, "Draw range calls\t: " + drawStats.drawRangeCallCount); rc.y += y_off;
            GUI.Label(rc, "Material sets\t: " + drawStats.materialSetCount); rc.y += y_off;
            GUI.Label(rc, "Total triangles\t: " + (drawStats.totalIndices / 3)); rc.y += y_off;
        }

        static VisualElement GetFirstElementInPanel(VisualElement ve)
        {
            while (ve != null && ve.renderChainData.prev?.renderChainData.isInChain == true)
                ve = ve.renderChainData.prev;
            return ve;
        }

    }

    internal struct RenderChainVEData
    {
        internal VisualElement prev, next; // This is a flattened view of the visual element hierarchy
        internal VisualElement groupTransformAncestor, boneTransformAncestor;
        internal VisualElement nextDirtyVisuals, nextDirtyTransform, nextDirtyClipping; // Embedded linked list for dirty updates
        internal RenderChainCommand firstCommand, lastCommand; // Sequential for the same owner
        internal RenderChainCommand firstClosingCommand, lastClosingCommand; // Optional, sequential for the same owner, the presence of closing commands requires starting commands too, otherwise certain optimizations will become invalid
        internal bool isInChain, isStencilClipped, isHierarchyHidden;
        internal bool usesText, usesAtlas, disableNudging, disableTransformID;
        internal byte dirtiedVisuals; // 0, 1 is for self, and 2 is hierarchical
        internal bool dirtiedClipping, dirtiedTransform;
        internal Implementation.ClipMethod clipMethod;
        internal MeshHandle data, closingData;
        internal Alloc transformID;
        internal Matrix4x4 verticesSpace; // Transform describing the space which the vertices in 'data' are relative to
        internal int displacementUVStart, displacementUVEnd;
        internal uint dirtyID;

        // Text update acceleration
        internal VisualElement prevText, nextText;
        internal List<RenderChainTextEntry> textEntries;

        internal bool allocatedTransformID { get { return transformID.size > 0 && !transformID.shortLived; } }
        internal RenderChainCommand lastClosingOrLastCommand { get { return lastClosingCommand ?? lastCommand; } }
    }

    internal struct RenderChainTextEntry
    {
        internal RenderChainCommand command;
        internal int firstVertex, vertexCount;
    }
}
