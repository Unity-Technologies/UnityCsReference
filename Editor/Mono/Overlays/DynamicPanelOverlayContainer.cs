// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class DynamicPanelOverlayContainer : OverlayContainer
    {
        // The height of a toolbar overlay in panel mode. This is validated in a test for future proofing of style changes.
        public const float minChunkHeight = 44f; //Use rounded numbers to take into account 1x screens

        // Enough width to show all overlay actions. This is validated in a test for future proofing of style changes and additional actions.
        public const float minWidth = 56;

        public const float maxPercentWidth = 50;

        [Serializable]
        public new class UxmlSerializedData : OverlayContainer.UxmlSerializedData
        {
#pragma warning disable 649
            [SerializeField] bool alignRight;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags alignRight_UxmlAttributeFlags;
            public override object CreateInstance() => new DynamicPanelOverlayContainer();
#pragma warning restore 649

            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(alignRight), "align-right")
                }, true);
            }

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (DynamicPanelOverlayContainer)obj;
                if (ShouldWriteAttributeValue(alignRight_UxmlAttributeFlags))
                    e.alignRight = alignRight;
            }
        }


        float m_Width = 20;
        public float width => m_Width;

        const string k_ClassName = "unity-overlay-dynamic-panel-container";
        const string k_FirstElementClassName = k_ClassName + "__first-element";
        const string k_LastElementClassName = k_ClassName + "__last-element";
        const string k_NoVisibleElementElementClassName = k_ClassName + "--no-visible-element";
        public const string k_ClassNameLeft = k_ClassName + "--left";
        public const string k_ClassNameRight = k_ClassName + "--right";
        const string k_AdditionalClassNameToolbar = "overlay-layout--toolbar-vertical";
        const string k_BorderClassName = "overlay-dynamic-panel-container__border";
        public const string k_WidthDraggerClassName = "overlay-dynamic-panel-container__width-dragger"; // Used in tests.

        readonly List<ChunkData> m_Chunks = new List<ChunkData>();
        readonly Dictionary<Overlay, Action<bool>> m_OverlayToDisplayCallback = new Dictionary<Overlay, Action<bool>>();
        readonly ContainerSection<MetaData> m_Section;
        readonly DraggerElement m_WidthDragger;
        readonly OverlayActions m_OverlayActions;
        readonly ScrollView m_ScrollView;
        readonly OverlayDropZoneBase m_ToolbarDropzone;

        bool m_AlignRight;
        bool m_ChunksEnabled;
        bool m_DelayHeightUpdateToGeometryChange;
        readonly PanelState k_PanelState = new PanelState();
        readonly ToolbarState k_ToolbarState = new ToolbarState();
        readonly MinimizedState k_MinimizedState = new MinimizedState();
        StateImplementation m_CurrentState;

        public bool alignRight
        {
            get => m_AlignRight;
            set
            {
                m_AlignRight = value;
                EnableInClassList(k_ClassNameLeft, !m_AlignRight);
                EnableInClassList(k_ClassNameRight, m_AlignRight);
            }
        }

        [Serializable]
        public struct ContainerSaveData
        {
            public State state;
            public List<OverlaySaveData> overlayData;
        }

        [Serializable]
        public struct OverlaySaveData
        {
            public string overlayId;
            public MetaData metaData;
        }

        [Serializable]
        public struct MetaData
        {
            public static readonly MetaData @default = new MetaData(-1);

            public float currentHeight;

            public MetaData(float currentHeight)
            {
                this.currentHeight = currentHeight;
            }

            public bool IsAuto()
            {
                return currentHeight < 0;
            }
        }

        public override bool IsOverlayLayoutSupported(Layout requested)
        {
            if (m_CurrentState == null)
                return requested == Layout.Panel;

            return requested == m_CurrentState.GetSupportedLayout();
        }

        public DynamicPanelOverlayContainer()
        {
            m_OverlayActions = new OverlayActions();
            m_OverlayActions.stateChanged += OnOverlayStateChangeRequested;
            Add(m_OverlayActions);

            m_ScrollView = new ScrollView();
            m_ScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_ScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_ScrollView.mode = ScrollViewMode.Vertical;
            Add(m_ScrollView);

            m_Section = CreateSection<MetaData>();
            m_Section.overlayInserted += OnOverlayInserted;
            m_Section.overlayRemoved += OnOverlayRemoved;
            m_ScrollView.Add(m_Section);

            // Create width dragger
            m_WidthDragger = new DraggerElement(DragDirection.Horizontal);
            m_WidthDragger.AddToClassList(k_WidthDraggerClassName);
            m_WidthDragger.style.position = Position.Absolute;
            m_WidthDragger.translationBegun += OnWidthTranslationBegun;
            m_WidthDragger.translated += OnWidthTranslated;
            Add(m_WidthDragger);

            UpdateWidth();

            AddToClassList(k_ClassName);
            alignRight = false;

            k_PanelState.owner = this;
            k_ToolbarState.owner = this;
            k_MinimizedState.owner = this;

            m_ToolbarDropzone = new OverlayContainerInsertDropZone(this, OverlayContainerSection.BeforeSpacer, OverlayContainerDropZone.Placement.End); // don't add to hierarchy by default

            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
            m_ScrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);

            SwitchState(State.Panel);

            UpdateFirstLastElementStyle();
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            canvas.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnParentGeometryChanged);
        }

        void DetachFromPanel(DetachFromPanelEvent evt)
        {
            canvas.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnParentGeometryChanged);
        }

        public enum State
        {
            Panel,
            Toolbar,
            Minimized
        }

        abstract class StateImplementation
        {
            public abstract Layout GetSupportedLayout();
            public DynamicPanelOverlayContainer owner { get; set; }
            public abstract void Start();
            public abstract void Stop();
        }

        sealed class PanelState : StateImplementation
        {
            const string k_StateUssName = k_ClassName + "--panel-mode";

            public override void Start()
            {
                owner.AddToClassList(k_StateUssName);
                owner.SetChunksEnabled(true);
            }

            public override void Stop()
            {
                owner.RemoveFromClassList(k_StateUssName);
            }

            public override Layout GetSupportedLayout()
            {
                return Layout.Panel;
            }
        }

        sealed class ToolbarState : StateImplementation
        {
            const string k_StateUssName = k_ClassName + "--toolbar-mode";

            public override void Start()
            {
                owner.AddToClassList(k_StateUssName);
                owner.AddToClassList(k_AdditionalClassNameToolbar);
                owner.SetChunksEnabled(false);
                owner.m_Section.hierarchy.Add(owner.m_ToolbarDropzone);
            }

            public override void Stop()
            {
                owner.RemoveFromClassList(k_AdditionalClassNameToolbar);
                owner.RemoveFromClassList(k_StateUssName);
                owner.SetChunksEnabled(true);
                owner.m_ToolbarDropzone.RemoveFromHierarchy();
            }

            public override Layout GetSupportedLayout()
            {
                return Layout.VerticalToolbar;
            }
        }

        sealed class MinimizedState : StateImplementation
        {
            const string k_StateUssName = k_ClassName + "--minimized-mode";

            public override void Start()
            {
                owner.AddToClassList(k_StateUssName);
                owner.SetChunksEnabled(false);
            }

            public override void Stop()
            {
                owner.RemoveFromClassList(k_StateUssName);
                owner.SetChunksEnabled(true);
            }

            public override Layout GetSupportedLayout()
            {
                return Layout.Panel;
            }
        }

        public State GetCurrentState()
        {
            if (m_CurrentState is PanelState) return State.Panel;
            if (m_CurrentState is ToolbarState) return State.Toolbar;
            if (m_CurrentState is MinimizedState) return State.Minimized;
            return (State)(-1);
        }

        public void SwitchState(State state)
        {
            Layout previousRequestedLayout = 0;
            if (m_CurrentState != null)
            {
                previousRequestedLayout = m_CurrentState.GetSupportedLayout();
                m_CurrentState.Stop();
            }

            switch (state)
            {
                case State.Panel:
                    m_CurrentState = k_PanelState;
                    m_OverlayActions.state = OverlayActions.State.Default;
                    break;
                case State.Toolbar:
                    m_CurrentState = k_ToolbarState;
                    m_OverlayActions.state = OverlayActions.State.Toolbar;
                    break;
                case State.Minimized:
                    m_CurrentState = k_MinimizedState;
                    m_OverlayActions.state = OverlayActions.State.Minimized;
                    break;
                default: m_CurrentState = null; break;
            }

            if (m_CurrentState != null)
            {
                if (previousRequestedLayout != m_CurrentState.GetSupportedLayout())
                {
                    // Force rebuild visible overlay to the new layout requested
                    for (int i = 0; i < m_Section.overlayCount; ++i)
                    {
                        var overlay = m_Section.GetOverlay(i);
                        if (overlay.displayed)
                            overlay.RebuildContent();
                    }
                }

                m_CurrentState.Start();
            }
        }

        sealed class ChunkData : IDisposable
        {
            public Overlay overlay { get; private set; }

            public DraggerElement dragger { get; private set; }

            public bool draggerEnabled
            {
                get => dragger.style.display.value == DisplayStyle.Flex;
                set => dragger.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public event Action<Overlay> sizeChangeBegun;
            public event Action<(Overlay overlay, float total, float delta)> sizeChanged;

            public ChunkData(OverlayCanvas.DynamicPanelBehavior behavior)
            {
                dragger = new DraggerElement(DragDirection.Vertical);
                dragger.translated += OnDraggerTranslated;
                dragger.translationBegun += OnDraggerTranslationBegun;
            }

            public void Dispose()
            {
                dragger.translationBegun -= OnDraggerTranslationBegun;
                dragger.translated -= OnDraggerTranslated;
                Set(null);
            }

            void OnDraggerTranslationBegun()
            {
                sizeChangeBegun?.Invoke(overlay);
            }

            void OnDraggerTranslated((Vector2 total, Vector2 delta) args)
            {
                sizeChanged?.Invoke((overlay, args.total.y, args.delta.y));
            }

            public bool Set(Overlay overlay)
            {
                if (this.overlay == overlay)
                    return false;

                // Unassign
                if (this.overlay != null)
                    ResetSize();

                // Assign
                this.overlay = overlay;

                dragger.RemoveFromHierarchy();
                if (this.overlay != null)
                {
                    // Add after overlay
                    var parent = overlay.rootVisualElement.parent;
                    var index = parent.IndexOf(overlay.rootVisualElement);
                    parent.Insert(index + 1, dragger);
                }

                return true;
            }

            public void ResetSize()
            {
                overlay.rootVisualElement.style.height = StyleKeyword.Null;
                overlay.rootVisualElement.style.flexGrow = StyleKeyword.Null;
                overlay.rootVisualElement.style.flexShrink = StyleKeyword.Null;
                overlay.rootVisualElement.style.flexBasis = StyleKeyword.Null;
            }

            public void SetSize(float size)
            {
                if (overlay != null)
                {
                    overlay.rootVisualElement.style.height = size;
                }
            }

            public void SetExpanding(bool expanding)
            {
                if (overlay != null)
                {
                    overlay.rootVisualElement.style.flexGrow = expanding ? 1 : 0;
                }
            }
        }

        void EnsureEnoughChunks()
        {
            var count = m_Section.GetVisibleCount();

            if (m_Chunks.Count == count)
                return;

            // Create chunks
            if (m_Chunks.Count < count)
            {
                while (m_Chunks.Count < count)
                {
                    var chunk = new ChunkData(canvas.dynamicPanelBehavior);
                    chunk.draggerEnabled = m_ChunksEnabled;
                    chunk.sizeChangeBegun += HeightDraggerTranslationStart;
                    chunk.sizeChanged += HeightDraggerTranslationUpdate;
                    m_Chunks.Add(chunk);
                }
            }

            // Delete chunks
            else
            {
                for (int i = m_Chunks.Count - 1; m_Chunks.Count > count; ++i)
                {
                    var chunk = m_Chunks[i];
                    chunk.sizeChangeBegun -= HeightDraggerTranslationStart;
                    chunk.sizeChanged -= HeightDraggerTranslationUpdate;
                    chunk.Dispose();
                    m_Chunks.RemoveAt(i);
                }
            }

            PopulateChunks();
            UpdateDraggers();
        }

        void PopulateChunks()
        {
            int chunkIndex = 0;
            bool changed = false;
            for (int i = 0; i < m_Section.overlayCount; ++i)
            {
                var overlay = m_Section.GetOverlay(i);
                var data = m_Section.GetData(i);

                if (overlay.displayed)
                {
                    changed |= m_Chunks[chunkIndex].Set(overlay);
                    ++chunkIndex;
                }
            }

            if (changed)
                UpdateChunkHeights();
        }

        float m_OriginalOverlayHeight;
        void HeightDraggerTranslationStart(Overlay overlay)
        {
            m_OriginalOverlayHeight = m_Section.GetData(overlay).currentHeight;
        }

        void HeightDraggerTranslationUpdate((Overlay overlay, float total, float delta) args)
        {
            var total = m_OriginalOverlayHeight + args.total;
            if (TryGetChunk(args.overlay, out int chunkIndex))
                RequestChunkHeightChange(chunkIndex, args.delta);
        }

        void RequestChunkHeightChange(int chunkIndex, float delta)
        {
            var index = m_Section.GetOverlayIndex(m_Chunks[chunkIndex].overlay);
            if (index >= 0)
            {
                var data = m_Section.GetData(index);

                if (chunkIndex + 1 >= m_Chunks.Count)
                    return;

                ChunkData expandingChunk;

                var remaining = Mathf.Abs(delta);

                if (delta < 0)
                {
                    delta = -delta; // make the delta positive
                    expandingChunk = m_Chunks[chunkIndex + 1];

                    for (int i = chunkIndex; i >= 0 && remaining > 0; --i)
                        ReduceChunkHeight(m_Chunks[i], true, ref remaining);
                }
                else
                {
                    expandingChunk = m_Chunks[chunkIndex];

                    for (int i = chunkIndex + 1; i < m_Chunks.Count && remaining > 0; ++i)
                        ReduceChunkHeight(m_Chunks[i], true, ref remaining);
                }

                var expandingIndex = m_Section.GetOverlayIndex(expandingChunk.overlay);
                var expandingData = m_Section.GetData(expandingIndex);

                expandingData.currentHeight += delta - remaining;

                expandingChunk.SetSize(expandingData.currentHeight);
                m_Section.SetData(expandingIndex, expandingData);
            }
        }

        void ReduceChunkHeight(ChunkData chunk, bool useLayoutValues, ref float delta)
        {
            var idx = m_Section.GetOverlayIndex(chunk.overlay);
            var targetData = m_Section.GetData(idx);
            var actualHeight = useLayoutValues ? chunk.overlay.rootVisualElement.rect.height : targetData.currentHeight; 
            var newHeight = Mathf.Max(actualHeight - delta, minChunkHeight);

            delta -= actualHeight - newHeight;
            targetData.currentHeight = Mathf.Min(newHeight, targetData.currentHeight); // Take into account potential flex grow
            m_Section.SetData(idx, targetData);

            chunk.SetSize(targetData.currentHeight);
        }

        void UpdateExpandingChunk()
        {
            if (m_Chunks.Count == 0)
                return;

            for (int i = 0; i < m_Chunks.Count; ++i)
            {
                var chunk = m_Chunks[i];
                chunk.SetExpanding(i == m_Chunks.Count - 1); 
            }
        }

        void SetChunksEnabled(bool enabled)
        {
            if (m_ChunksEnabled == enabled)
                return;

            m_ChunksEnabled = enabled;

            // Ensure the container has the correct height before we recalculate overlays
            if (m_ChunksEnabled)
                m_DelayHeightUpdateToGeometryChange = true;

            UpdateDraggers();
            UpdateChunkHeights();
            UpdateWidth();
            UpdateWidthDraggerStyling(m_Section.HasVisibleOverlays());
        }

        void UpdateDraggers()
        {
            for (int i = 0; i < m_Chunks.Count; ++i)
                m_Chunks[i].draggerEnabled = ShouldEnableDragger(i);
        }

        bool ShouldEnableDragger(int chunkIndex)
        {
            return m_ChunksEnabled && chunkIndex < m_Chunks.Count - 1;
        }

        public void SetPreferedHeight(Overlay overlay, float height)
        {
            if (TryGetChunk(overlay, out var index))
            {
                RequestChunkHeightChange(index, height - m_Section.GetData(overlay).currentHeight);
            }
        }

        public float GetPreferedHeight(Overlay overlay)
        {
            if (!overlay.displayed)
                return 0;

            return m_Section.GetData(overlay).currentHeight;
        }

        void UpdateChunkHeights()
        {
            if (m_Chunks.Count == 0)
                return;

            if (m_DelayHeightUpdateToGeometryChange)
                return;

            var containerHeight = m_ScrollView.layout.height;
            // We skip recalculation when the container isn't layout isn't calculated or we're in a hidden state
            if (float.IsNaN(containerHeight) || Mathf.Approximately(containerHeight, 0))
                return;

            if (!m_ChunksEnabled)
            {
                for (int i = 0; i < m_Chunks.Count; ++i)
                    m_Chunks[i].ResetSize();

                return;
            }

            float requestedHeight = 0;

            // Find requested height
            for (int i = m_Chunks.Count - 1; i >= 0; --i) 
            {
                requestedHeight += Mathf.Max(m_Section.GetData(m_Chunks[i].overlay).currentHeight, minChunkHeight);
            }

            UpdateExpandingChunk();

            // Requested sizes are smaller than the container height: Set all height to current
            if (requestedHeight <= containerHeight)
            {
                // Apply prefered height to all
                foreach (var chunk in m_Chunks)
                {
                    var index = m_Section.GetOverlayIndex(chunk.overlay);
                    if (index < 0)
                        continue;

                    var data = m_Section.GetData(index);
                    data.currentHeight = Mathf.Max(minChunkHeight, data.currentHeight); // Ensure is at least minimum
                    m_Section.SetData(index, data);
                    chunk.SetSize(data.currentHeight);
                }
            }

            // Requested sizes are higher than the container height: Reduce chunk height from bottom up
            else
            {
                var overflow = requestedHeight - containerHeight;
                for (int i = m_Chunks.Count - 1; i >= 0; --i)
                    ReduceChunkHeight(m_Chunks[i], false, ref overflow);
            }
        }

        bool TryGetChunk(Overlay overlay, out int index)
        {
            for (int i = 0; i < m_Chunks.Count; ++i)
            {
                if (m_Chunks[i].overlay == overlay)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        void OnOverlayStateChangeRequested(OverlayActions.State requested)
        {
            var state = State.Panel;
            switch (requested)
            {
                case OverlayActions.State.Default: state = State.Panel; break;
                case OverlayActions.State.Toolbar: state = State.Toolbar; break;
                case OverlayActions.State.Minimized: state = State.Minimized; break;
            }

            SwitchState(state);
        }

        void OnParentGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateChunkHeights();
            SetWidth(m_Width); //Ensure we're still within max percent width
        }

        void OnScrollViewGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_DelayHeightUpdateToGeometryChange)
            {
                m_DelayHeightUpdateToGeometryChange = false;
                UpdateChunkHeights();
            }
        }

        void OnOverlayInserted(Overlay overlay, int insertedOverlayIndex, DockingHint hint)
        {
            var currentHeight = overlay.rootVisualElement.layout.height;

            m_Section.SetData(overlay, float.IsNaN(currentHeight) ? MetaData.@default : new MetaData(currentHeight));

            Action<bool> handler = (displayed) => OnDisplayedChanged(overlay, displayed);
            overlay.displayedChanged += handler;
            m_OverlayToDisplayCallback.Add(overlay, handler);

            if (overlay.displayed)
            {
                if (overlay.collapsed)
                    overlay.collapsed = false;

                EnsureEnoughChunks();
            }

            // First chunk
            if (m_Chunks.Count == 1)
            {
                m_Width = minWidth;
                UpdateFirstLastElementStyle();
            }

            var overlayWidth = overlay.rootVisualElement.layout.width;

            if (m_Width < overlayWidth)
                SetWidth(overlayWidth);
            else
                UpdateWidth();
        }

        void OnOverlayRemoved(Overlay overlay, int removedOverlayIndex)
        {
            if (m_OverlayToDisplayCallback.TryGetValue(overlay, out var handler))
            {
                overlay.displayedChanged -= handler;
                m_OverlayToDisplayCallback.Remove(overlay);
            }

            if (overlay.displayed && TryGetChunk(overlay, out var index))
            {
                m_Chunks[index].Dispose();
                m_Chunks.RemoveAt(index);
                UpdateChunkHeights();

                // Last Chunk
                if (m_Chunks.Count == 0)
                    UpdateFirstLastElementStyle();
            }

            overlay.rootVisualElement.RemoveFromClassList(k_FirstElementClassName);
            overlay.rootVisualElement.RemoveFromClassList(k_LastElementClassName);
            overlay.layout = Layout.Panel;

            UpdateWidth();
        }

        void OnDisplayedChanged(Overlay overlay, bool displayed)
        {
            var overlayAlreadyInSection = m_Section.ContainsOverlay(overlay);
            if (!(overlayAlreadyInSection && displayed))
            {
                EnsureEnoughChunks();

                if (m_Chunks.Count == 0)
                    SetOverlayWidth(overlay);
                else
                    UpdateWidth();

                UpdateFirstLastElementStyle();
            }
            else  
            {   
                // Remove/Insert is required to avoid undertermined clip mode assert failure in UITK render data
                var prevIndex = m_Section.contentContainer.IndexOf(overlay.rootVisualElement);
                overlay.rootVisualElement.RemoveFromHierarchy();
                m_Section.contentContainer.Insert(prevIndex, overlay.rootVisualElement);
                
                var currentHeight = overlay.rootVisualElement.layout.height;
                EnsureEnoughChunks();
                
                if (m_Chunks.Count == 1)
                    UpdateFirstLastElementStyle();
                
                var overlayWidth = overlay.rootVisualElement.layout.width;
                if (m_Width < overlayWidth)
                    SetWidth(overlayWidth);
                else
                    UpdateWidth();
            }

            UpdateExpandingChunk();
        }

        float m_OriginalWidth;

        void OnWidthTranslationBegun()
        {
            m_OriginalWidth = m_Width;
        }

        void OnWidthTranslated((Vector2 total, Vector2 delta) args)
        {
            var total = alignRight ? -args.total.x : args.total.x;
            SetWidth(m_OriginalWidth + total);
        }

        public void SetWidth(float pixel)
        {
            var maxWidth = parent.rect.width * maxPercentWidth / 100;
            m_Width = Mathf.Clamp(pixel, minWidth, maxWidth);
            UpdateWidth();
        }

        void SetOverlayWidth(Overlay overlay)
        {
            SetWidth(overlay.rootVisualElement.layout.width);
        }

        void UpdateWidth()
        {
            style.width = m_Section.GetFirstVisible() != null && m_ChunksEnabled ? new Length(m_Width, LengthUnit.Pixel) : StyleKeyword.Null;
        }

        void UpdateFirstLastElementStyle()
        {
            var first = m_Section.GetFirstVisible();
            var last = m_Section.GetLastVisible();
            for (int i = 0; i < m_Section.overlayCount; ++i)
            {
                var overlay = m_Section.GetOverlay(i);
                overlay.rootVisualElement.EnableInClassList(k_FirstElementClassName, overlay == first);
                overlay.rootVisualElement.EnableInClassList(k_LastElementClassName, overlay == last);
            }

            bool hasElements = first != null;
            EnableInClassList(k_NoVisibleElementElementClassName, !hasElements);
            UpdateWidthDraggerStyling(hasElements);
        }

        void UpdateWidthDraggerStyling(bool hasElements)
        {
            m_WidthDragger.handle.pickingMode = hasElements && m_ChunksEnabled ? PickingMode.Position : PickingMode.Ignore;
        }

        public void UpdateStyling()
        {
            m_OverlayActions.pickingMode = PickingMode.Position;
            UpdateBorderStyle();
        }

        void UpdateBorderStyle()
        {
            if (canvas.dynamicPanelBehavior == OverlayCanvas.DynamicPanelBehavior.None)
                return;

            var isRight = ClassListContains(k_ClassNameRight);
            var index = parent.IndexOf(this);
            var toolbarContainerIndex = isRight ? index + 1 : index - 1;

            if (toolbarContainerIndex >= parent.childCount || toolbarContainerIndex < 0)
                return;

            var ve = parent.ElementAt(toolbarContainerIndex);
            if (ve is ToolbarOverlayContainer toolbarContainer)
                UpdateBorderStyle(toolbarContainer);
        }

        public void UpdateBorderStyle(ToolbarOverlayContainer toolbarContainer)
        {
            var toolbarContainerBeforeSpacer = toolbarContainer.GetContainerSection(OverlayContainerSection.BeforeSpacer);
            var toolbarContainerAfterSpacer = toolbarContainer.GetContainerSection(OverlayContainerSection.AfterSpacer);
            var toolbarContainerHasVisibleOverlays = toolbarContainerBeforeSpacer.HasVisibleOverlays() || toolbarContainerAfterSpacer.HasVisibleOverlays();

            var dynamicPanelContainerHasVisibleOverlays = m_Section.HasVisibleOverlays();

            var shouldEnableClassList = toolbarContainerHasVisibleOverlays && dynamicPanelContainerHasVisibleOverlays;
            EnableInClassList(k_BorderClassName, shouldEnableClassList);
        }

        internal override IEnumerable<OverlayDropZoneBase> GetDropZones()
        {
            if (GetCurrentState() == State.Toolbar)
            {
                yield return m_ToolbarDropzone;
            }
        }

        internal ContainerSaveData GetSaveData()
        {
            List<OverlaySaveData> overlayData = new List<OverlaySaveData>();

            // We only save the meta data of the currently visible elements
            foreach (var chunk in m_Chunks)
            {
                overlayData.Add(new OverlaySaveData
                {
                    overlayId = chunk.overlay.id,
                    metaData = m_Section.GetData(chunk.overlay),
                });
            }

            return new ContainerSaveData
            {
                state = GetCurrentState(),
                overlayData = overlayData
            };
        }

        internal void ApplySaveData(ContainerSaveData save)
        {
            if (save.overlayData != null)
            {
                foreach (var data in save.overlayData)
                {
                    for (int i = 0; i < m_Section.overlayCount; ++i)
                    {
                        if (m_Section.GetOverlay(i).id == data.overlayId)
                        {
                            m_Section.SetData(i, data.metaData);
                            continue;
                        }
                    }
                }
            }

            SwitchState(save.state);
            UpdateChunkHeights();
        }
    }
}
