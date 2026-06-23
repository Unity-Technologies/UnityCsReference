// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.Editor;
using Unity.Profiling.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditorInternal.Profiling
{
    internal class ScreenshotDetailsViewController : ViewController
    {
        const string k_UxmlResourceName = "Profiler/Screenshots/ScreenshotDetailsView.uxml";

        readonly ScreenshotIndexCatalogue m_Catalogue;
        Image m_ScreenshotImage;
        VisualElement m_EmptyState;
        Texture2D m_CurrentTexture;
        // EmissionFrame of the screenshot whose pixels live in m_CurrentTexture. Lets us skip the
        // Texture2D re-allocation when consecutive LoadScreenshot calls resolve to the same source
        // image — the common case during recording, where the selected frame advances every editor
        // frame but the visible screenshot only changes every N frames.
        int m_CurrentTextureEmissionFrame = -1;

        // Raised whenever the per-frame info text changes. The module view controller pipes this into the details view toolbar.
        public event Action<string> InfoTextChanged;

        public ScreenshotDetailsViewController(ScreenshotIndexCatalogue catalogue)
        {
            // Catalogue is the source of truth for LogicalFrame → EmissionFrame resolution. Required
            // because LoadScreenshot is called with the user's selected LogicalFrame, but per-frame
            // metadata (texture info + pixel data) lives at EmissionFrame.
            m_Catalogue = catalogue;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidOperationException($"Failed to load UXML for {nameof(ScreenshotDetailsViewController)}");

            m_ScreenshotImage = view.Q<Image>("screenshot-details__image");
            m_EmptyState = view.Q("screenshot-details__empty-state");
            var emptyStateTitle = view.Q<Label>("screenshot-details__empty-state-title");
            var emptyStateBody = view.Q<Label>("screenshot-details__empty-state-body");

            if (m_ScreenshotImage == null || m_EmptyState == null || emptyStateTitle == null || emptyStateBody == null)
                throw new InvalidOperationException($"Failed to find required elements in UXML for {nameof(ScreenshotDetailsViewController)}");

            m_ScreenshotImage.scaleMode = ScaleMode.ScaleToFit;

            emptyStateTitle.text = L10n.Tr("No screenshots data available");

            var asyncReadbackLink = $"<a href=\"https://docs.unity3d.com/{Help.GetShortReleaseVersion()}/Documentation/ScriptReference/Rendering.AsyncGPUReadback.html\">AsyncGPUReadback</a>";
            emptyStateBody.enableRichText = true;
            emptyStateBody.text = string.Format(
                L10n.Tr("Screenshots are captured automatically while profiling a Player or Play Mode, as long as the platform supports {0}.\n\n" +
                        "The rate of screenshot capture can be modified under Preferences > Analysis > Profiler, or disabled by setting a value below 1."),
                asyncReadbackLink);

            return view;
        }

        // Toggles between the screenshot image and a centred "no screenshots" message. Driven by the
        // module view controller from the catalogue's total screenshot count — distinct from the
        // per-frame "no screenshot at this frame" case, which keeps the image slot and only updates
        // the toolbar text.
        public void ShowEmptyState(bool show)
        {
            if (m_EmptyState == null || m_ScreenshotImage == null)
                return;
            m_EmptyState.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_ScreenshotImage.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void SetInfoText(string text)
        {
            InfoTextChanged?.Invoke(text);
        }

        public void LoadScreenshot(int logicalFrame, int firstAvailableFrame)
        {
            try
            {
                // Resolve the source frame from the catalogue first — cheap (binary search, no
                // pixel-data read). If it points at the same EmissionFrame we already have on
                // screen, skip the expensive extract step and only refresh the label text.
                if (!TryResolveSourceFrame(logicalFrame, firstAvailableFrame, out var sourceEmissionFrame, out var sourceLogicalFrame))
                {
                    ClearCurrentTexture();
                    SetInfoText($"Frame {logicalFrame + 1} - No screenshot available");
                    return;
                }

                if (m_CurrentTexture != null && sourceEmissionFrame == m_CurrentTextureEmissionFrame)
                {
                    UpdateInfoText(logicalFrame, sourceLogicalFrame, m_CurrentTexture);
                    return;
                }

                var extracted = ExtractScreenshotFromEmissionFrame(sourceEmissionFrame);
                if (!extracted.Found)
                {
                    ClearCurrentTexture();
                    SetInfoText($"Frame {logicalFrame + 1} - No screenshot available");
                    return;
                }

                // Swap first, dispose second — anything else flashes blank mid-frame.
                var previousTexture = m_CurrentTexture;
                m_CurrentTexture = extracted.Texture;
                m_CurrentTextureEmissionFrame = sourceEmissionFrame;
                m_ScreenshotImage.image = extracted.Texture;
                if (previousTexture != null)
                    UnityEngine.Object.DestroyImmediate(previousTexture);

                UpdateInfoText(logicalFrame, sourceLogicalFrame, extracted.Texture);
            }
            catch (Exception ex)
            {
                // LoadScreenshot is called from selection event handlers — log rather than
                // let exceptions escape and bubble through the event dispatcher.
                Debug.LogException(ex);
            }
        }

        void ClearCurrentTexture()
        {
            m_ScreenshotImage.image = null;
            if (m_CurrentTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(m_CurrentTexture);
                m_CurrentTexture = null;
            }
            m_CurrentTextureEmissionFrame = -1;
        }

        void UpdateInfoText(int requestedLogicalFrame, int sourceLogicalFrame, Texture2D texture)
        {
            var requestedSourceDiff = requestedLogicalFrame - sourceLogicalFrame;
            SetInfoText(sourceLogicalFrame == requestedLogicalFrame
                ? $"Frame {requestedLogicalFrame + 1} - {texture.width}x{texture.height}"
                : $"Frame {requestedLogicalFrame + 1} - Showing screenshot from frame {sourceLogicalFrame + 1} " +
                 (requestedSourceDiff == 1 ? "(1 frame ago)" : $"({requestedSourceDiff} frames ago)"));
        }

        struct ScreenshotResult
        {
            public Texture2D Texture;
            public bool Found;
        }

        // Returns the EmissionFrame to extract pixel data from, plus the LogicalFrame that source
        // depicts (used for the "Showing from frame N (M frames ago)" label). Does no pixel-data
        // reads — caller uses the EmissionFrame to decide whether to reuse a cached texture.
        bool TryResolveSourceFrame(int logicalFrame, int firstAvailableFrame, out int sourceEmissionFrame, out int sourceLogicalFrame)
        {
            sourceEmissionFrame = -1;
            sourceLogicalFrame = -1;

            if (m_Catalogue == null)
                return false;

            // Direct LogicalFrame hit: the catalogue tells us the EmissionFrame to read metadata from.
            if (m_Catalogue.TryGetEmissionFrame(logicalFrame, out int directEmissionFrame))
            {
                sourceEmissionFrame = directEmissionFrame;
                sourceLogicalFrame = logicalFrame;
                return true;
            }

            // Fall back to the most recent prior screenshot in LogicalFrame space so the panel
            // keeps showing something rather than blanking out for every non-capture frame.
            // Catalogue-driven (binary search), so this works for legacy captures that lack
            // kFramesSinceLastScreenshot metadata on the requested frame.
            if (!m_Catalogue.TryGetNearestPriorLogicalFrame(logicalFrame, out ScreenshotFrame nearest))
                return false;

            // Reject a match whose depicted frame has fallen below the available window.
            if (nearest.LogicalFrame < firstAvailableFrame)
                return false;

            sourceEmissionFrame = nearest.EmissionFrame;
            sourceLogicalFrame = nearest.LogicalFrame;
            return true;
        }

        static ScreenshotResult ExtractScreenshotFromEmissionFrame(int emissionFrame)
        {
            using (var frameData = ProfilerDriver.GetRawFrameDataView(emissionFrame, 0))
            {
                if (!ScreenshotIndexCatalogue.TryGetScreenshotTextureInfo(frameData, out var width, out var height, out var format))
                    return default;

                var data = frameData.GetFrameMetaData<byte>(
                    ProfilerDriver.profilerInternalSessionMetaDataGuid,
                    (int)ProfilingSessionMetaDataEntry.ScreenshotRawTextureData);
                if (data.Length == 0)
                    return default;

                // Upload directly from the NativeArray slice (no managed copy).
                var texture = ScreenshotIndexCatalogue.CreateScreenshotTexture(data, width, height, format);
                return new ScreenshotResult { Texture = texture, Found = true };
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_CurrentTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_CurrentTexture);
                    m_CurrentTexture = null;
                }
                m_CurrentTextureEmissionFrame = -1;
            }

            base.Dispose(disposing);
        }
    }
}
