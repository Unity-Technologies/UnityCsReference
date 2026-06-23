// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling.Editor;
using Unity.Profiling.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditorInternal.Profiling
{
    // Scrollable thumbnail strip at the top of the Screenshots module details panel.
    // Shows every screenshot in the capture, with linear spacing and click-to-select behaviour.
    // The chart-area timeline strip is rendered separately by ScreenshotsChartTimelineViewController.
    // Both consume the same ScreenshotIndexCatalogue for the authoritative list of screenshot frames.
    internal class ScreenshotsTimelineViewController : ViewController
    {
        const string k_UxmlResourceName = "Profiler/Screenshots/ScreenshotsTimelineView.uxml";
        const string k_UssClass_Thumbnail = "screenshots-timeline__thumbnail";
        const string k_UssClass_ThumbnailDetailed = "screenshots-timeline__thumbnail--detailed";
        const string k_UssClass_ThumbnailImage = "screenshots-timeline__thumbnail-image";
        const string k_UssClass_ThumbnailLabel = "screenshots-timeline__thumbnail-label";
        const string k_UssClass_ThumbnailSelected = "screenshots-timeline__thumbnail--selected";

        const int k_ThumbnailWidth = 128;
        const int k_ThumbnailHeight = 72;
        const int k_Spacing = 4;

        // Switching modules tears this controller down and recreates it on return. The centred frame
        // index lives on the per-window ScreenshotsProfilerModule (see LastCentredScreenshotFrameIndex)
        // so it survives the round-trip without bleeding across multiple open ProfilerWindow instances.
        // Tracking by frame number (not pixel offset) means new screenshots recorded or appended while
        // away don't shift the saved position — we still centre on the same content.

        readonly ProfilerWindow m_ProfilerWindow;
        readonly ScreenshotsProfilerModule m_Module;

        ScreenshotIndexCatalogue m_Catalogue;
        ScrollView m_ScrollView;
        VisualElement m_TimelineContainer;
        Label m_RecordingPlaceholder;

        List<ScreenshotFrameInfo> m_AllScreenshots = new List<ScreenshotFrameInfo>();
        List<ScreenshotFrameInfo> m_VisibleThumbnails = new List<ScreenshotFrameInfo>();
        int m_SelectedFrameIndex = -1;
        bool m_SuppressAutoScroll = false; // Suppress auto-scroll when user clicks thumbnail (they're already looking at it)
        CancellationTokenSource m_LoadCancellation;
        // True between LoadAllVisibleThumbnailsAsync's entry and its terminal paths (success +
        // cancel + error). Lets UpdateSelection avoid restarting an in-flight load on every
        // frame during recording — the eager-fill phase will pick up the newly selected thumb.
        bool m_LoadInFlight;
        bool m_IsDisposed = false;
        int m_PendingRestoreFrameIndex = -1;
        ThumbnailAtlasPool m_AtlasPool;
        EventCallback<GeometryChangedEvent> m_DeferredScrollCallback;
        VisualElement m_DeferredScrollTarget;

        public event Action<int> FrameSelected;
        public event Action<int> ScreenshotCountChanged;

        public int ScreenshotCount => m_AllScreenshots.Count;

        class ScreenshotFrameInfo
        {
            // EmissionFrame is what ProfilerDriver.GetRawFrameDataView / TryReadScreenshotDimensions /
            // TryScaleScreenshot need (metadata + pixel data live there). LogicalFrame is what the
            // user sees on screen: it's the frame the screenshot DEPICTS, and the value used for
            // labels, click events, and selection sync with the chart-area cursor.
            public int LogicalFrame;
            public int EmissionFrame;
            // The atlas this thumb lives in, plus its bottom-left-origin UV rect within it.
            // Once assigned, both stay valid for the controller's lifetime — the atlas pool
            // never evicts slots, so scroll-back is just re-presenting an already resident region.
            public Texture2D AtlasTexture;
            public Rect AtlasUv;
            public VisualElement TimelineElement;
            public ActivityIndicatorOverlay ActivityOverlay;
            public float ActualWidth = k_ThumbnailWidth;
            public float ActualHeight = k_ThumbnailHeight;
            // True once we've read the source texture dimensions from the screenshot's metadata
            // (cheap, no pixel decode) and scaled them. Stops layout from shifting later when the
            // full texture finishes loading.
            public bool DimensionsLoaded;
        }

        public ScreenshotsTimelineViewController(ProfilerWindow profilerWindow, ScreenshotsProfilerModule module)
        {
            m_ProfilerWindow = profilerWindow;
            m_Module = module;
            m_PendingRestoreFrameIndex = m_Module?.LastCentredScreenshotFrameIndex ?? -1;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidOperationException($"Failed to load UXML for {nameof(ScreenshotsTimelineViewController)}");

            m_ScrollView = view.Q<ScrollView>("screenshots-timeline");
            m_TimelineContainer = view.Q("screenshots-timeline__container");
            m_RecordingPlaceholder = view.Q<Label>("screenshots-timeline__recording-placeholder");

            if (m_ScrollView == null || m_TimelineContainer == null || m_RecordingPlaceholder == null)
                throw new InvalidOperationException($"Failed to find required elements in UXML for {nameof(ScreenshotsTimelineViewController)}");

            m_ScrollView.contentContainer.pickingMode = PickingMode.Position;
            m_TimelineContainer.pickingMode = PickingMode.Position;
            m_ScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_AtlasPool = new ThumbnailAtlasPool();

            m_ScrollView.horizontalScroller.valueChanged += OnScrollValueChanged;

            m_Catalogue = m_ProfilerWindow?.GetScreenshotIndexCatalogue();
            if (m_Catalogue != null)
            {
                m_Catalogue.Changed += OnCatalogueChanged;
                m_Catalogue.BurstStarted += OnBurstStarted;
                m_Catalogue.BurstEnded += OnBurstEnded;
            }

            ReloadTimeline();
            // Handles the case where the user opens the screenshots module while a recording is
            // already in progress — without this, the scroll view would be shown briefly until the
            // next Changed event swapped to the placeholder.
            ApplyRecordingPlaceholderVisibility();
            TryRestoreScrollToSavedFrame();
        }

        void TryRestoreScrollToSavedFrame()
        {
            if (m_PendingRestoreFrameIndex < 0)
                return;

            var targetFrame = m_PendingRestoreFrameIndex;
            m_PendingRestoreFrameIndex = -1;

            var frameInfo = FindClosestVisibleThumbnail(targetFrame);
            if (frameInfo?.TimelineElement != null)
                ScrollToCenterDeferred(frameInfo.TimelineElement);
        }

        int CaptureCentredFrameIndex()
        {
            if (m_ScrollView == null || m_VisibleThumbnails.Count == 0)
                return -1;

            var viewportWidth = m_ScrollView.contentViewport.resolvedStyle.width;
            if (float.IsNaN(viewportWidth) || viewportWidth <= 0)
                return -1;

            var centreX = m_ScrollView.scrollOffset.x + viewportWidth / 2f;

            ScreenshotFrameInfo closest = null;
            var closestDistance = float.MaxValue;
            foreach (var frameInfo in m_VisibleThumbnails)
            {
                if (frameInfo.TimelineElement == null)
                    continue;
                var left = frameInfo.TimelineElement.style.left.value.value;
                var width = frameInfo.ActualWidth;
                var centre = left + width / 2f;
                var distance = Mathf.Abs(centre - centreX);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = frameInfo;
                }
            }

            return closest?.LogicalFrame ?? -1;
        }

        void OnCatalogueChanged()
        {
            if (m_IsDisposed)
                return;
            // Skip the merge + dimension prefetch + thumbnail re-decode while recording is
            // actively bursting — the strip is hidden behind the recording placeholder and the
            // chart timeline's append path already keeps the user informed of new frames.
            // OnBurstEnded reconciles once recording quiets.
            if (m_Catalogue != null && m_Catalogue.IsRecordingBurst)
                return;
            ReloadTimeline();
        }

        void OnBurstStarted()
        {
            if (m_IsDisposed)
                return;
            ApplyRecordingPlaceholderVisibility();
        }

        void OnBurstEnded()
        {
            if (m_IsDisposed)
                return;
            ApplyRecordingPlaceholderVisibility();
            ReloadTimeline();
        }

        void ApplyRecordingPlaceholderVisibility()
        {
            var recording = m_Catalogue != null && m_Catalogue.IsRecordingBurst;
            m_ScrollView.style.display = recording ? DisplayStyle.None : DisplayStyle.Flex;
            m_RecordingPlaceholder.style.display = recording ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ReloadTimeline()
        {
            // Merge the catalogue's authoritative frame list into m_AllScreenshots, preserving any
            // existing entries (and their atlas slots) keyed by EmissionFrame. Without preservation,
            // every catalogue Changed event would tear down loaded thumbnails and trigger a re-decode.
            var existingByEmissionFrame = new Dictionary<int, ScreenshotFrameInfo>(m_AllScreenshots.Count);
            foreach (var info in m_AllScreenshots)
                existingByEmissionFrame[info.EmissionFrame] = info;

            var preservedCount = 0;
            var catalogueFrames = m_Catalogue?.Frames;
            var newList = new List<ScreenshotFrameInfo>(catalogueFrames?.Count ?? 0);
            if (catalogueFrames != null)
            {
                foreach (var frame in catalogueFrames)
                {
                    if (existingByEmissionFrame.TryGetValue(frame.EmissionFrame, out var existing))
                    {
                        // Catalogue may have refreshed LogicalFrame for this emission (e.g. cache-load
                        // identity fallback → real offset once metadata is re-read). Keep the cached
                        // atlas slot but refresh the index so the label stays in sync.
                        existing.LogicalFrame = frame.LogicalFrame;
                        newList.Add(existing);
                        preservedCount++;
                    }
                    else
                    {
                        newList.Add(new ScreenshotFrameInfo
                        {
                            LogicalFrame = frame.LogicalFrame,
                            EmissionFrame = frame.EmissionFrame,
                        });
                    }
                }
            }

            // If nothing survived (data cleared, profile reloaded, capture replaced), the atlas pool
            // is full of orphan slots. Drop and recreate so per-cycle memory stays bounded.
            var wholesaleReplace = preservedCount == 0 && m_AllScreenshots.Count > 0;
            if (wholesaleReplace)
            {
                m_AtlasPool?.Dispose();
                m_AtlasPool = new ThumbnailAtlasPool();
            }

            m_AllScreenshots = newList;

            RecreateLoadCancellation();

            ScreenshotCountChanged?.Invoke(m_AllScreenshots.Count);

            RebuildTimelineUI();
            LoadAllVisibleThumbnailsAsync();
        }

        void RebuildTimelineUI()
        {
            m_TimelineContainer.Clear();
            m_VisibleThumbnails.Clear();

            if (m_AllScreenshots.Count == 0)
                return;

            // Read just the dimension metadata for any newly arrived screenshots so element widths
            // are correct on first layout — otherwise textures finish loading asynchronously and
            // every element shifts, which throws off scroll-position restoration and looks jittery.
            PrefetchScreenshotDimensions();

            // Hide screenshots from frames that are no longer shown in the Profiler window — both those
            // evicted from memory, and those still resident but trimmed by the frame-count setting.
            var firstSelectableFrame = ScreenshotIndexCatalogue.FirstDisplayedFrameIndex();
            if (firstSelectableFrame < 0)
                firstSelectableFrame = 0;
            foreach (var frameInfo in m_AllScreenshots)
            {
                if (frameInfo.LogicalFrame >= firstSelectableFrame)
                    m_VisibleThumbnails.Add(frameInfo);
            }

            if (m_VisibleThumbnails.Count == 0)
                return;

            float totalWidth = k_Spacing;
            foreach (var frameInfo in m_VisibleThumbnails)
                totalWidth += frameInfo.ActualWidth + k_Spacing;

            m_TimelineContainer.style.left = 0;
            m_TimelineContainer.style.width = totalWidth;
            m_TimelineContainer.style.height = k_ThumbnailHeight + 20;

            float xPos = k_Spacing;
            foreach (var frameInfo in m_VisibleThumbnails)
            {
                CreateAndAddThumbnailElement(frameInfo, xPos);
                xPos += frameInfo.ActualWidth + k_Spacing;
            }
        }

        void PrefetchScreenshotDimensions()
        {
            foreach (var frameInfo in m_AllScreenshots)
            {
                if (frameInfo.DimensionsLoaded)
                    continue;
                // Dimensions live in per-frame metadata at EmissionFrame.
                if (TryReadScaledDimensions(frameInfo.EmissionFrame, out int width, out int height))
                {
                    frameInfo.ActualWidth = width;
                    frameInfo.ActualHeight = height;
                    frameInfo.DimensionsLoaded = true;
                }
            }
        }

        static bool TryReadScaledDimensions(int frameIndex, out int width, out int height)
        {
            width = k_ThumbnailWidth;
            height = k_ThumbnailHeight;
            if (!ScreenshotIndexCatalogue.TryReadScreenshotDimensions(frameIndex, out int srcWidth, out int srcHeight, out _))
                return false;
            ComputeScaledDimensions(srcWidth, srcHeight, k_ThumbnailWidth, k_ThumbnailHeight, out width, out height);
            return true;
        }

        static void ComputeScaledDimensions(int srcWidth, int srcHeight, int maxWidth, int maxHeight, out int width, out int height)
        {
            if (srcWidth <= 0 || srcHeight <= 0)
            {
                width = maxWidth;
                height = maxHeight;
                return;
            }
            var sourceAspect = (float)srcWidth / srcHeight;
            var targetAspect = (float)maxWidth / maxHeight;
            if (sourceAspect > targetAspect)
            {
                width = maxWidth;
                height = Mathf.RoundToInt(maxWidth / sourceAspect);
            }
            else
            {
                height = maxHeight;
                width = Mathf.RoundToInt(maxHeight * sourceAspect);
            }
        }

        void OnScrollValueChanged(float newValue)
        {
            // User scrolled — re-prioritise loading so newly visible thumbnails come first.
            RecreateLoadCancellation();
            LoadAllVisibleThumbnailsAsync();
        }

        void RecreateLoadCancellation()
        {
            ScreenshotIndexCatalogue.ReplaceCts(ref m_LoadCancellation, recreate: true);
        }

        VisualElement CreateThumbnailElement(ScreenshotFrameInfo frameInfo)
        {
            var container = new VisualElement();
            container.AddToClassList(k_UssClass_Thumbnail);
            container.AddToClassList(k_UssClass_ThumbnailDetailed);
            container.style.position = Position.Relative; // ActivityIndicatorOverlay positions itself absolutely.

            var image = new Image();
            image.name = "thumbnail-image";
            image.AddToClassList(k_UssClass_ThumbnailImage);
            image.pickingMode = PickingMode.Ignore;
            container.Add(image);

            var activityOverlay = new ActivityIndicatorOverlay();
            container.Add(activityOverlay);
            frameInfo.ActivityOverlay = activityOverlay;

            // Label/click both in LogicalFrame space — what the user sees on the chart, where the
            // chart-strip thumbnail is anchored, and what selectedFrameIndex represents for the
            // rest of the screenshots module.
            var label = new Label($"{frameInfo.LogicalFrame + 1}");
            label.AddToClassList(k_UssClass_ThumbnailLabel);
            label.pickingMode = PickingMode.Ignore;
            container.Add(label);

            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                m_SuppressAutoScroll = true;
                FrameSelected?.Invoke(frameInfo.LogicalFrame);
            });

            return container;
        }

        void CreateAndAddThumbnailElement(ScreenshotFrameInfo frameInfo, float xPos)
        {
            var element = CreateThumbnailElement(frameInfo);
            // ActualWidth/Height are populated by PrefetchScreenshotDimensions before we get here;
            // they fall back to the k_Thumbnail* defaults if metadata wasn't readable.
            element.style.width = frameInfo.ActualWidth;
            element.style.height = frameInfo.ActualHeight + 14; // extra height for the label
            element.style.position = Position.Absolute;
            element.style.left = xPos;
            element.style.marginRight = 4;

            m_TimelineContainer.Add(element);
            frameInfo.TimelineElement = element;

            if (frameInfo.AtlasTexture == null)
                return;

            // Already-resident atlas slot — present it immediately and hide the loading overlay.
            AssignThumbnailToElement(frameInfo);
            frameInfo.ActivityOverlay?.Hide();
        }

        async void LoadAllVisibleThumbnailsAsync()
        {
            // Capture the CTS this invocation owns so the finally below only clears m_LoadInFlight when
            // no newer load has superseded it (see the ownership check there).
            var cts = m_LoadCancellation;
            var cancellationToken = cts.Token;
            m_LoadInFlight = true;

            try
            {
                // Debounce the activity-indicator overlays. If decoding finishes (or the user re-scrolls
                // and cancels) within 100 ms we never show the overlays, so fast loads don't flash one in.
                await Task.Delay(100, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                var prioritizedThumbnails = GetPrioritizedThumbnailsForLoading();

                // Reset every still-unloaded overlay before re-showing only for thumbs we're about to
                // load. Without this, a prior cancelled load can leave its overlay visible indefinitely
                // (the show fired but the load itself was cancelled before the finally-block could hide it).
                foreach (var frameInfo in m_VisibleThumbnails)
                {
                    if (frameInfo.AtlasTexture == null)
                        frameInfo.ActivityOverlay?.Hide();
                }
                // GetPrioritizedThumbnailsForLoading filters AtlasTexture != null, so every
                // entry here genuinely needs loading.
                foreach (var frameInfo in prioritizedThumbnails)
                    frameInfo.ActivityOverlay?.Show();

                // Foreground: prioritise the near-viewport thumbs so the user sees results first.
                await LoadThumbnailsInParallel(prioritizedThumbnails, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                UpdateThumbnailPositions();

                // Eager fill: silently load every other unloaded thumb so subsequent scrolls land on
                // already-resident textures. The user's next scroll cancels via the shared token, and
                // we re-enter from the top with a freshly prioritised list.
                var eagerFillThumbnails = new List<ScreenshotFrameInfo>(m_VisibleThumbnails.Count);
                foreach (var frameInfo in m_VisibleThumbnails)
                {
                    if (frameInfo.AtlasTexture == null)
                        eagerFillThumbnails.Add(frameInfo);
                }
                await LoadThumbnailsInParallel(eagerFillThumbnails, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                UpdateThumbnailPositions();
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                // ObjectDisposedException can land here when a cancellation token's source is
                // disposed (Dispose / RecreateLoadCancellation) while a Task.Run registration is
                // in-flight — it's a cancellation signal, not a bug.
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                // Only clear if no newer load has superseded this one — otherwise a cancelled older
                // invocation would clear the flag out from under the active load, letting the per-frame
                // UpdateSelection during recording restart (and cancel) loads in a tight loop.
                if (ReferenceEquals(m_LoadCancellation, cts))
                    m_LoadInFlight = false;
            }
        }

        async Task LoadThumbnailsInParallel(List<ScreenshotFrameInfo> frames, CancellationToken cancellationToken)
        {
            if (frames.Count == 0)
                return;

            // Leave one core for the main thread (UI + atlas upload). Running ProcessorCount-1 chunks in
            // parallel keeps every BG core busy while the main thread drains finished chunks in
            // submission order — predictable left-to-right fill rather than completion order.
            var chunkSize = Math.Max(2, Environment.ProcessorCount - 1);

            for (var i = 0; i < frames.Count; i += chunkSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var end = Math.Min(i + chunkSize, frames.Count);
                var pending = new List<(ScreenshotFrameInfo Frame, Task<ScaledThumb> Task)>(end - i);
                for (var j = i; j < end; j++)
                {
                    var frame = frames[j];
                    if (frame.AtlasTexture != null)
                        continue;
                    // Capture EmissionFrame for the closure — pixel data lives there, not at LogicalFrame.
                    var emissionFrame = frame.EmissionFrame;
                    pending.Add((frame, Task.Run(() => ScaleThumbnailOnBackground(emissionFrame), cancellationToken)));
                }

                foreach (var item in pending)
                {
                    ScaledThumb scaled;
                    try
                    {
                        scaled = await item.Task;
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        // Without this catch the exception escapes the foreach and skips the
                        // ActivityOverlay.Hide() for every remaining item, leaving stuck spinners
                        // until the next selection change cancels and re-runs the load.
                        Debug.LogException(ex);
                        item.Frame.ActivityOverlay?.Hide();
                        continue;
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    try
                    {
                        if (scaled.Bytes == null)
                            continue;

                        var slot = m_AtlasPool.AllocateSlot();
                        m_AtlasPool.Upload(slot, scaled.Bytes, scaled.Width, scaled.Height);

                        item.Frame.AtlasTexture = slot.Atlas;
                        item.Frame.AtlasUv = ThumbnailAtlasPool.ComputeUv(slot, scaled.Width, scaled.Height);
                        item.Frame.ActualWidth = scaled.Width;
                        item.Frame.ActualHeight = scaled.Height;
                        AssignThumbnailToElement(item.Frame);
                    }
                    finally
                    {
                        item.Frame.ActivityOverlay?.Hide();
                    }
                }
            }
        }

        static ScaledThumb ScaleThumbnailOnBackground(int emissionFrame)
        {
            if (!ScreenshotIndexCatalogue.TryScaleScreenshot(emissionFrame, k_ThumbnailWidth, k_ThumbnailHeight,
                out byte[] bytes, out int actualWidth, out int actualHeight))
                return default;

            return new ScaledThumb { Bytes = bytes, Width = actualWidth, Height = actualHeight };
        }

        List<ScreenshotFrameInfo> GetPrioritizedThumbnailsForLoading()
        {
            var scrollOffset = m_ScrollView.scrollOffset.x;
            var viewportWidth = m_ScrollView.contentViewport.resolvedStyle.width;
            var viewportLeft = scrollOffset;
            var viewportRight = scrollOffset + viewportWidth;

            var loadThumbnails = new List<ScreenshotFrameInfo>();

            // Half-viewport preload window. Thumbs farther out are deliberately not loaded —
            // they'd just sit idle past the residency window and get evicted next pass anyway.
            var padding = viewportWidth * 0.5f;

            foreach (var frameInfo in m_VisibleThumbnails)
            {
                if (frameInfo.TimelineElement == null)
                    continue;

                var thumbnailLeft = frameInfo.TimelineElement.style.left.value.value;
                var thumbnailWidth = frameInfo.ActualWidth;
                var thumbnailRight = thumbnailLeft + thumbnailWidth;

                var isInLoadWindow = thumbnailRight >= (viewportLeft - padding) && thumbnailLeft <= (viewportRight + padding);

                // Skip already-loaded thumbnails so LoadThumbnailsInParallel's chunks are packed
                // with real work — without this filter, a half-loaded load window produces sparse
                // chunks where most slots are no-ops, serialising what should run in parallel.
                if (isInLoadWindow && frameInfo.AtlasTexture == null)
                    loadThumbnails.Add(frameInfo);
            }

            // Always include the selected frame even if it isn't currently in the load window —
            // the user expects clicking a far-off thumbnail to load it. Skip if already loaded
            // for the same reason as the load-window filter above. m_SelectedFrameIndex is in
            // LogicalFrame space (matches what's labelled on the thumbs and what FrameSelected emits).
            if (m_SelectedFrameIndex < 0)
                return loadThumbnails;

            var selectedFrame = m_VisibleThumbnails.Find(f => f.LogicalFrame == m_SelectedFrameIndex);
            if (selectedFrame != null && selectedFrame.AtlasTexture == null && !loadThumbnails.Contains(selectedFrame))
                loadThumbnails.Insert(0, selectedFrame);

            return loadThumbnails;
        }

        struct ScaledThumb
        {
            public byte[] Bytes;
            public int Width;
            public int Height;
        }

        void AssignThumbnailToElement(ScreenshotFrameInfo frameInfo)
        {
            var container = frameInfo.TimelineElement;
            var image = container?.Q<Image>("thumbnail-image");
            if (image == null)
                return;

            image.image = frameInfo.AtlasTexture;
            image.uv = frameInfo.AtlasUv;

            container.style.paddingLeft = 0;
            container.style.paddingRight = 0;
            container.style.paddingTop = 0;
            container.style.paddingBottom = 0;

            container.style.width = frameInfo.ActualWidth;
            container.style.height = frameInfo.ActualHeight + 14;
            container.style.marginRight = 4;
        }

        public void UpdateSelection(int logicalFrame)
        {
            var shouldScroll = !m_SuppressAutoScroll;
            m_SuppressAutoScroll = false;

            var frameInfo = FindClosestVisibleThumbnail(logicalFrame);

            if (m_SelectedFrameIndex >= 0)
            {
                var oldFrameInfo = m_VisibleThumbnails.Find(f => f.LogicalFrame == m_SelectedFrameIndex);
                if (oldFrameInfo?.TimelineElement != null)
                    oldFrameInfo.TimelineElement.RemoveFromClassList(k_UssClass_ThumbnailSelected);
            }

            m_SelectedFrameIndex = frameInfo?.LogicalFrame ?? -1;

            if (frameInfo?.TimelineElement == null)
                return;

            frameInfo.TimelineElement.AddToClassList(k_UssClass_ThumbnailSelected);

            if (shouldScroll)
                ScrollToCenterDeferred(frameInfo.TimelineElement);

            if (frameInfo.AtlasTexture == null && !m_LoadInFlight)
            {
                // No restart if a load is already running: its eager-fill phase will reach this
                // thumbnail without us cancelling everything that's currently mid-flight. Without
                // this guard, selecting a new frame every editor tick (the recording path) cancels
                // and re-cancels loads in a tight loop and nothing ever finishes.
                RecreateLoadCancellation();
                LoadAllVisibleThumbnailsAsync();
            }
        }

        void ScrollToCenter(VisualElement element)
        {
            if (element == null || m_ScrollView == null)
                return;

            // Read element and container widths from inline style (both are set inline in
            // CreateAndAddThumbnailElement / RebuildTimelineUI / UpdateThumbnailPositions).
            // resolvedStyle.width returns NaN for elements whose layout pass hasn't fired yet —
            // a real risk here because ScrollToCenterDeferred fires as soon as the viewport has
            // any width, which can predate the layout pass for newly added thumbnail elements.
            // NaN would then propagate through to scrollOffset and stick the scroll position.
            var elementLeft = element.style.left.value.value;
            var elementWidth = element.style.width.value.value;
            var elementCenter = elementLeft + elementWidth / 2f;

            var viewportWidth = m_ScrollView.contentViewport.resolvedStyle.width;
            var targetScrollOffset = elementCenter - (viewportWidth / 2f);

            var containerWidth = m_TimelineContainer.style.width.value.value;
            var maxScroll = Mathf.Max(0, containerWidth - viewportWidth);
            targetScrollOffset = Mathf.Clamp(targetScrollOffset, 0, maxScroll);

            m_ScrollView.scrollOffset = new UnityEngine.Vector2(targetScrollOffset, 0);
        }

        // ScrollToCenter reads resolvedStyle.width on the element and viewport — both are 0 before
        // UIElements has resolved layout. When called during module init (UpdateSelection from
        // ScreenshotsModuleViewController.CreateView, or the saved-frame restore in ViewLoaded),
        // wait for the first GeometryChangedEvent that produces a real viewport width, then scroll.
        // The callback is stored in a field so Dispose can unregister it if it never fires —
        // an anonymous lambda would leak this controller (via captured `this`) until the scroll view
        // itself is collected.
        void ScrollToCenterDeferred(VisualElement element)
        {
            if (element == null || m_ScrollView == null)
                return;

            // Supersede any prior in-flight deferred scroll, even when we can scroll immediately:
            // otherwise a pending deferred callback from an earlier selection fires later and
            // overwrites this scroll position with a stale target.
            UnregisterDeferredScrollCallback();

            if (m_ScrollView.contentViewport.resolvedStyle.width > 0f)
            {
                ScrollToCenter(element);
                return;
            }

            m_DeferredScrollTarget = element;
            m_DeferredScrollCallback = OnDeferredScrollGeometry;
            m_ScrollView.contentViewport.RegisterCallback(m_DeferredScrollCallback);
        }

        void OnDeferredScrollGeometry(GeometryChangedEvent _)
        {
            // resolvedStyle.width returns NaN during early layout passes, and NaN <= 0f evaluates
            // to false — without an explicit IsNaN check we'd fall through to ScrollToCenter,
            // propagate NaN into m_ScrollView.scrollOffset, and permanently break the ScrollView.
            // Matches the guard pattern in CaptureCentredFrameIndex.
            if (m_ScrollView == null)
                return;
            var viewportWidth = m_ScrollView.contentViewport.resolvedStyle.width;
            if (float.IsNaN(viewportWidth) || viewportWidth <= 0f)
                return;
            var target = m_DeferredScrollTarget;
            UnregisterDeferredScrollCallback();
            if (m_IsDisposed || target == null)
                return;
            ScrollToCenter(target);
        }

        void UnregisterDeferredScrollCallback()
        {
            if (m_DeferredScrollCallback != null && m_ScrollView?.contentViewport != null)
                m_ScrollView.contentViewport.UnregisterCallback(m_DeferredScrollCallback);
            m_DeferredScrollCallback = null;
            m_DeferredScrollTarget = null;
        }

        ScreenshotFrameInfo FindClosestVisibleThumbnail(int targetLogicalFrame)
        {
            if (m_VisibleThumbnails.Count == 0)
                return null;

            ScreenshotFrameInfo closest = m_VisibleThumbnails[0];
            var minDistance = Mathf.Abs(closest.LogicalFrame - targetLogicalFrame);

            foreach (var thumbnail in m_VisibleThumbnails)
            {
                var distance = Mathf.Abs(thumbnail.LogicalFrame - targetLogicalFrame);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = thumbnail;
                }
            }

            return closest;
        }

        void UpdateThumbnailPositions()
        {
            m_TimelineContainer.style.left = 0;

            float spacing = k_Spacing;
            var totalWidth = spacing;
            foreach (var frameInfo in m_VisibleThumbnails)
            {
                if (frameInfo.TimelineElement != null && frameInfo.TimelineElement.style.display != DisplayStyle.None)
                    totalWidth += frameInfo.ActualWidth + spacing;
            }

            m_TimelineContainer.style.width = totalWidth;

            var xPos = spacing;
            foreach (var frameInfo in m_VisibleThumbnails)
            {
                if (frameInfo.TimelineElement != null && frameInfo.TimelineElement.style.display != DisplayStyle.None)
                {
                    frameInfo.TimelineElement.style.left = xPos;
                    xPos += frameInfo.ActualWidth + spacing;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Capture before m_IsDisposed flips and before lists are torn down — element
                // positions are still valid here. Stored on the per-window module so it survives
                // controller recreation on next module activation.
                var centred = CaptureCentredFrameIndex();
                if (centred >= 0 && m_Module != null)
                    m_Module.LastCentredScreenshotFrameIndex = centred;

                m_IsDisposed = true;

                if (m_Catalogue != null)
                {
                    m_Catalogue.Changed -= OnCatalogueChanged;
                    m_Catalogue.BurstStarted -= OnBurstStarted;
                    m_Catalogue.BurstEnded -= OnBurstEnded;
                    m_Catalogue = null;
                }

                ScreenshotIndexCatalogue.ReplaceCts(ref m_LoadCancellation, recreate: false);

                if (m_ScrollView?.horizontalScroller != null)
                    m_ScrollView.horizontalScroller.valueChanged -= OnScrollValueChanged;

                UnregisterDeferredScrollCallback();

                m_AtlasPool?.Dispose();
                m_AtlasPool = null;

                m_AllScreenshots.Clear();
                m_VisibleThumbnails.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
