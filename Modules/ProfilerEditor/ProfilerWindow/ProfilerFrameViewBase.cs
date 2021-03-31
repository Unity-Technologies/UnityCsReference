// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.MPE;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class ProfilerFrameDataViewBase
    {
        internal static GUIContent showFullDetailsForCallStacksContent => BaseStyles.showFullDetailsForCallStacks;

        protected static class BaseStyles
        {
            public static readonly GUIContent noData = EditorGUIUtility.TrTextContent("No frame data available. Select a frame from the charts above to see its details here.");
            public static GUIContent disabledSearchText = EditorGUIUtility.TrTextContent("Showing search results are disabled while recording with deep profiling.\nStop recording to view search results.");
            public static GUIContent cpuGPUTime = EditorGUIUtility.TrTextContent("CPU:{0}ms   GPU:{1}ms");

            public static readonly GUIStyle header = "OL title";
            public static readonly GUIStyle label = "OL label";
            public static readonly GUIStyle toolbar = EditorStyles.toolbar;
            public static readonly GUIStyle selectionExtraInfoArea = EditorStyles.helpBox;
            public static readonly GUIContent warningTriangle = EditorGUIUtility.IconContent("console.infoicon.inactive.sml");
            public static readonly GUIStyle tooltip = new GUIStyle("AnimationEventTooltip");
            public static readonly GUIStyle tooltipText = new GUIStyle("AnimationEventTooltip");
            public static readonly GUIStyle tooltipArrow = "AnimationEventTooltipArrow";
            public static readonly GUIStyle tooltipButton = EditorStyles.miniButton;
            public static readonly GUIStyle tooltipDropdown = new GUIStyle("MiniPopup");
            public static readonly int tooltipButtonAreaControlId = "ProfilerTimelineTooltipButton".GetHashCode();
            public static readonly int timelineTimeAreaControlId = "ProfilerTimelineTimeArea".GetHashCode();

            public static readonly GUIContent tooltipCopyTooltip = EditorGUIUtility.TrTextContent("Copy", "Copy to Clipboard");

            public static readonly GUIContent showDetailsDropdownContent = EditorGUIUtility.TrTextContent("Show");
            public static readonly GUIContent showFullDetailsForCallStacks = EditorGUIUtility.TrTextContent("Full details for Call Stacks");
            public static readonly GUIContent showSelectedSampleStacks = EditorGUIUtility.TrTextContent("Selected Sample Stack ...");
            public static readonly GUIStyle viewTypeToolbarDropDown = new GUIStyle(EditorStyles.toolbarDropDownLeft);
            public static readonly GUIStyle threadSelectionToolbarDropDown = new GUIStyle(EditorStyles.toolbarDropDown);
            public static readonly GUIStyle detailedViewTypeToolbarDropDown = new GUIStyle(EditorStyles.toolbarDropDown);
            public static readonly GUIContent updateLive = EditorGUIUtility.TrTextContent("Live", "Display the current or selected frame while recording Playmode or Editor. This increases the overhead in the EditorLoop when the Profiler Window is repainted.");
            public static readonly GUIContent liveUpdateMessage = EditorGUIUtility.TrTextContent("Displaying of frame data disabled while recording Playmode or Editor. To see the data, pause recording, or toggle \"Live\" display mode on. " +
                "\n \"Live\" display mode increases the overhead in the EditorLoop when the Profiler Window is repainted.");

            public static readonly string selectionExtraInfoHierarhcyView = L10n.Tr("Selection Info: ");
            public static readonly string proxySampleMessage = L10n.Tr("Sample \"{0}\" {1} {2} deeper not found in this frame within the selected Sample Stack.");
            public static readonly string proxySampleMessageScopeSingular = L10n.Tr("scope");
            public static readonly string proxySampleMessageScopePlural = L10n.Tr("scopes");
            public static readonly string proxySampleMessageTooltip = L10n.Tr("Selected Sample Stack: {0}");
            public static readonly string proxySampleMessagePart2TimelineView = L10n.Tr("\nClosest match:\n");

            public static readonly string callstackText = LocalizationDatabase.GetLocalizedString("Call Stack:");

            // 6 seems like a good default value for margins used in quite some places. Do note though, that this is little more than a semi-randomly chosen magic number.
            public const int magicMarginValue = 6;
            const float k_DetailedViewTypeToolbarDropDownWidth = 150f;

            public static readonly Rect tooltipArrowRect = new Rect(-32, 0, 64, 6);

            static BaseStyles()
            {
                viewTypeToolbarDropDown.fixedWidth = Chart.kSideWidth;
                viewTypeToolbarDropDown.stretchWidth = false;

                detailedViewTypeToolbarDropDown.fixedWidth = k_DetailedViewTypeToolbarDropDownWidth;
                tooltip.contentOffset = new Vector2(0, 0);
                tooltip.overflow = new RectOffset(0, 0, 0, 0);
                tooltipText = new GUIStyle(tooltip);
                tooltipText.onNormal.background = null;
                tooltipDropdown.margin.right += magicMarginValue;
            }
        }

        protected const string k_ShowFullDetailsForCallStacksPrefKey = "Profiler.ShowFullDetailsForCallStacks";

        internal static bool showFullDetailsForCallStacks
        {
            get
            {
                return EditorPrefs.GetBool(k_ShowFullDetailsForCallStacksPrefKey, false);
            }
            set
            {
                if (value != showFullDetailsForCallStacks)
                {
                    EditorPrefs.SetBool(k_ShowFullDetailsForCallStacksPrefKey, value);
                    callStackNeedsRegeneration = true;
                }
            }
        }

        [NonSerialized]
        internal static bool callStackNeedsRegeneration = false;

        [NonSerialized]
        public string dataAvailabilityMessage = null;

        static readonly GUIContent[] kCPUProfilerViewTypeNames = new GUIContent[]
        {
            EditorGUIUtility.TrTextContent("Timeline"),
            EditorGUIUtility.TrTextContent("Hierarchy"),
            EditorGUIUtility.TrTextContent("Raw Hierarchy")
        };

        static GUIContent GetCPUProfilerViewTypeName(ProfilerViewType viewType)
        {
            switch (viewType)
            {
                case ProfilerViewType.Hierarchy:
                    return kCPUProfilerViewTypeNames[1];
                case ProfilerViewType.Timeline:
                    return kCPUProfilerViewTypeNames[0];
                case ProfilerViewType.RawHierarchy:
                    return kCPUProfilerViewTypeNames[2];
                default:
                    throw new NotImplementedException($"Lookup Not Implemented for {viewType}");
            }
        }

        static readonly int[] kCPUProfilerViewTypes = new int[]
        {
            (int)ProfilerViewType.Timeline,
            (int)ProfilerViewType.Hierarchy,
            (int)ProfilerViewType.RawHierarchy
        };

        static readonly GUIContent[] kGPUProfilerViewTypeNames = new GUIContent[]
        {
            EditorGUIUtility.TrTextContent("Hierarchy"),
            EditorGUIUtility.TrTextContent("Raw Hierarchy")
        };
        static readonly int[] kGPUProfilerViewTypes = new int[]
        {
            (int)ProfilerViewType.Hierarchy,
            (int)ProfilerViewType.RawHierarchy
        };

        public bool gpuView { get; private set; }

        protected IProfilerWindowController m_ProfilerWindow;

        public CPUOrGPUProfilerModule cpuModule { get; private set; }

        public delegate void ViewTypeChangedCallback(ProfilerViewType viewType);
        public event ViewTypeChangedCallback viewTypeChanged = delegate {};

        public virtual void OnEnable(CPUOrGPUProfilerModule cpuOrGpuModule, IProfilerWindowController profilerWindow, bool isGpuView)
        {
            m_ProfilerWindow = profilerWindow;
            cpuModule = cpuOrGpuModule;
            gpuView = isGpuView;
        }

        public virtual void OnDisable()
        {
        }

        protected void DrawViewTypePopup(ProfilerViewType viewType)
        {
            ProfilerViewType newViewType;
            if (!gpuView)
            {
                newViewType = (ProfilerViewType)EditorGUILayout.IntPopup((int)viewType, kCPUProfilerViewTypeNames, kCPUProfilerViewTypes, BaseStyles.viewTypeToolbarDropDown, GUILayout.Width(BaseStyles.viewTypeToolbarDropDown.fixedWidth));
            }
            else
            {
                if (viewType == ProfilerViewType.Timeline)
                    viewType = ProfilerViewType.Hierarchy;
                newViewType = (ProfilerViewType)EditorGUILayout.IntPopup((int)viewType, kGPUProfilerViewTypeNames, kGPUProfilerViewTypes, BaseStyles.viewTypeToolbarDropDown, GUILayout.Width(BaseStyles.viewTypeToolbarDropDown.fixedWidth));
            }

            if (newViewType != viewType)
            {
                viewTypeChanged(newViewType);
                GUIUtility.ExitGUI();
            }
        }

        protected void DrawLiveUpdateToggle(ref bool updateViewLive)
        {
            using (new EditorGUI.DisabledScope(ProcessService.level != ProcessLevel.Main))
            {
                // This button is only needed in the Master Process
                updateViewLive = GUILayout.Toggle(updateViewLive, BaseStyles.updateLive, EditorStyles.toolbarButton);
            }
        }

        protected void DrawCPUGPUTime(float cpuTimeMs, float gpuTimeMs)
        {
            var cpuTime = cpuTimeMs > 0 ? UnityString.Format("{0:N2}", cpuTimeMs) : "--";
            var gpuTime = gpuTimeMs > 0 ? UnityString.Format("{0:N2}", gpuTimeMs) : "--";
            GUILayout.Label(UnityString.Format(BaseStyles.cpuGPUTime.text, cpuTime, gpuTime), EditorStyles.toolbarLabel);
        }

        protected void ShowLargeTooltip(Vector2 pos, Rect fullRect, GUIContent content, ReadOnlyCollection<string> selectedSampleStack,
            float lineHeight, int frameIndex, int threadIndex, bool hasCallstack, ref float downwardsZoomableAreaSpaceNeeded,
            int selectedSampleIndexIfProxySelection = -1)
        {
            // Arrow of tooltip
            var arrowRect = BaseStyles.tooltipArrowRect;
            arrowRect.position += pos;

            var style = BaseStyles.tooltip;
            var size = style.CalcSize(content);
            var copyButtonSize = BaseStyles.tooltipButton.CalcSize(BaseStyles.tooltipCopyTooltip);
            var sampleStackButtonSize = new Vector2();
            if (selectedSampleStack != null && selectedSampleStack.Count > 0)
            {
                sampleStackButtonSize = BaseStyles.tooltipButton.CalcSize(BaseStyles.showDetailsDropdownContent);
                sampleStackButtonSize.x += BaseStyles.magicMarginValue;
            }

            const int k_ButtonBottomMargin = BaseStyles.magicMarginValue;
            var requiredButtonHeight = Mathf.Max(copyButtonSize.y, sampleStackButtonSize.y) + k_ButtonBottomMargin;
            // report how big the zoomable area needs to be to be able to see the entire contents if the view is big enough
            downwardsZoomableAreaSpaceNeeded = arrowRect.height + size.y + requiredButtonHeight;
            size.y += requiredButtonHeight;

            size.x = Mathf.Max(size.x, copyButtonSize.x + sampleStackButtonSize.x + k_ButtonBottomMargin * 3);

            var heightAvailableDownwards = Mathf.Max(fullRect.yMax - arrowRect.yMax, 0);
            var heightAvailableUpwards = Mathf.Max(pos.y - lineHeight - arrowRect.height - fullRect.y, 0);

            float usedHeight = 0;
            float rectY = 0;
            // Flip tooltip if too close to bottom and there's more space above it
            var flipped = (arrowRect.yMax + arrowRect.height + size.y > fullRect.yMax) && heightAvailableUpwards > heightAvailableDownwards;
            if (flipped)
            {
                arrowRect.y -= (lineHeight + 2 * arrowRect.height);
                usedHeight = Mathf.Min(heightAvailableUpwards, size.y);
                rectY = arrowRect.y + arrowRect.height - usedHeight;
            }
            else
            {
                usedHeight = Mathf.Min(heightAvailableDownwards, size.y);
                rectY = arrowRect.yMax;
            }

            // The tooltip needs to have enough space (on line of text + buttons) to allow for us to but buttons in there without glitches
            var showButtons = usedHeight >= (requiredButtonHeight + style.CalcSize(BaseStyles.tooltipCopyTooltip).y);
            if (!showButtons)
            {
                size.y -= requiredButtonHeight;
            }

            // Label box
            var rect = new Rect(pos.x + BaseStyles.tooltipArrowRect.x, rectY, size.x, usedHeight);

            // Ensure it doesn't go too far right
            if (rect.xMax > fullRect.xMax)
                rect.x = Mathf.Max(fullRect.x, fullRect.xMax - rect.width);
            if (rect.xMax > fullRect.xMax)
                rect.xMax = fullRect.xMax;
            if (arrowRect.xMax > fullRect.xMax + 20)
                arrowRect.x = fullRect.xMax - arrowRect.width + 20;

            // Adjust left to we can always see giant (STL) names.
            if (rect.xMin < fullRect.xMin)
                rect.x = fullRect.xMin;
            if (arrowRect.xMin < fullRect.xMin - 20)
                arrowRect.x = fullRect.xMin - 20;

            // Draw small arrow
            GUI.BeginClip(arrowRect);
            var oldMatrix = GUI.matrix;
            if (flipped)
                GUIUtility.ScaleAroundPivot(new Vector2(1.0f, -1.0f), new Vector2(arrowRect.width * 0.5f, arrowRect.height));
            GUI.Label(new Rect(0, 0, arrowRect.width, arrowRect.height), GUIContent.none, BaseStyles.tooltipArrow);
            GUI.matrix = oldMatrix;
            GUI.EndClip();

            var copyButtonRect = new Rect(rect.x + k_ButtonBottomMargin, rect.yMax - copyButtonSize.y - k_ButtonBottomMargin, copyButtonSize.x, copyButtonSize.y);

            var selectableLabelTextRect = rect;
            if (showButtons)
                selectableLabelTextRect.yMax = copyButtonRect.y - k_ButtonBottomMargin;
            var buttonArea = rect;
            if (showButtons)
                buttonArea.yMin = copyButtonRect.y;

            // Draw tooltip
            if (Event.current.type == EventType.Repaint)
            {
                style.Draw(rect, false, false, false, false);
            }
            GUI.BeginClip(selectableLabelTextRect);
            selectableLabelTextRect.position = Vector2.zero;
            //var controlId = GUIUtility.GetControlID(BaseStyles.tooltipButtonAreaControlId, FocusType.Passive, buttonArea);
            EditorGUI.SelectableLabel(selectableLabelTextRect, content.text, style);
            GUI.EndClip();

            if (showButtons)
            {
                // overwrite the mouse cursor for the buttons on potential splitters underneath the buttons
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUIUtility.AddCursorRect(buttonArea, MouseCursor.Arrow);
                }
                // and steal the hot control if needed.
                var controlId = GUIUtility.GetControlID(BaseStyles.tooltipButtonAreaControlId, FocusType.Passive, buttonArea);
                var eventType = Event.current.GetTypeForControl(controlId);
                if (eventType == EventType.MouseDown && buttonArea.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                }
                else if (GUIUtility.hotControl == controlId && eventType == EventType.MouseUp)
                {
                    GUIUtility.hotControl = 0;
                }
                // copyButton
                if (GUI.Button(copyButtonRect, BaseStyles.tooltipCopyTooltip, BaseStyles.tooltipButton))
                {
                    Clipboard.stringValue = content.text;
                }
                if (selectedSampleStack != null && selectedSampleStack.Count > 0)
                {
                    var sampleStackRect = new Rect(copyButtonRect.xMax + k_ButtonBottomMargin, copyButtonRect.y, sampleStackButtonSize.x, sampleStackButtonSize.y);
                    if (EditorGUI.DropdownButton(sampleStackRect, BaseStyles.showDetailsDropdownContent, FocusType.Passive, BaseStyles.tooltipDropdown))
                    {
                        var menu = new GenericMenu();
                        // The tooltip is already pointing to the what's currently selected, switching the view will Apply that selection in the other view
                        menu.AddItem(GetCPUProfilerViewTypeName(ProfilerViewType.Hierarchy), false, () => { viewTypeChanged(ProfilerViewType.Hierarchy); });
                        menu.AddItem(GetCPUProfilerViewTypeName(ProfilerViewType.RawHierarchy), false, () => { viewTypeChanged(ProfilerViewType.RawHierarchy); });
                        menu.AddSeparator("");
                        if (hasCallstack)
                            menu.AddItem(BaseStyles.showFullDetailsForCallStacks, showFullDetailsForCallStacks, () => showFullDetailsForCallStacks = !showFullDetailsForCallStacks);
                        else
                            menu.AddDisabledItem(BaseStyles.showFullDetailsForCallStacks, showFullDetailsForCallStacks);

                        menu.AddSeparator("");

                        var rectWindowPosition = EditorGUIUtility.GUIToScreenRect(sampleStackRect).position;

                        // Show Sample Selection :
                        // admittedly, it'd be nice to only generate the text if sample selection option was chosen...
                        // however, that would need to happen in an OnGui call and not within the callback of the generic menu,
                        // to be able to calculate the needed window size and avoid glitches on first displaying it.
                        // at least the user already clicked on the dropdown for this...
                        string selectedSampleStackText = null;
                        var sampleStackSb = new System.Text.StringBuilder();
                        for (int i = selectedSampleStack.Count - 1; i >= 0; i--)
                        {
                            sampleStackSb.AppendLine(selectedSampleStack[i]);
                        }
                        selectedSampleStackText = sampleStackSb.ToString();
                        string actualSampleStackText = null;
                        if (selectedSampleIndexIfProxySelection >= 0)
                        {
                            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                            {
                                if (frameData.valid)
                                {
                                    sampleStackSb.Clear();
                                    string sampleName = null;
                                    var markerIdPath = new List<int>(selectedSampleStack != null ? selectedSampleStack.Count : 10);
                                    if (ProfilerTimelineGUI.GetItemMarkerIdPath(frameData, cpuModule, selectedSampleIndexIfProxySelection, ref sampleName, ref markerIdPath) >= 0)
                                    {
                                        for (int i = markerIdPath.Count - 1; i >= 0; i--)
                                        {
                                            sampleStackSb.AppendLine(frameData.GetMarkerName(markerIdPath[i]));
                                        }
                                        actualSampleStackText = sampleStackSb.ToString();
                                    }
                                }
                            }
                        }
                        var selectionSampleStackContent = selectedSampleStack != null ? new GUIContent(selectedSampleStackText) : null;
                        var actualSampleStackContent = actualSampleStackText != null ? new GUIContent(actualSampleStackText) : null;
                        var sampleStackWindowSize = SelectedSampleStackWindow.CalculateSize(selectionSampleStackContent, actualSampleStackContent);
                        menu.AddItem(BaseStyles.showSelectedSampleStacks, false, () =>
                        {
                            SelectedSampleStackWindow.ShowSampleStackWindow(rectWindowPosition, sampleStackWindowSize, selectionSampleStackContent, actualSampleStackContent);
                        });
                        menu.DropDown(sampleStackRect);
                    }
                }
            }
        }

        internal void CompileCallStack(System.Text.StringBuilder sb, List<ulong> m_CachedCallstack, FrameDataView frameDataView)
        {
            sb.Append(BaseStyles.callstackText);
            sb.Append('\n');
            var fullCallStack = showFullDetailsForCallStacks;
            foreach (var addr in m_CachedCallstack)
            {
                var methodInfo = frameDataView.ResolveMethodInfo(addr);
                if (string.IsNullOrEmpty(methodInfo.methodName))
                {
                    if (fullCallStack)
                        sb.AppendFormat("0x{0:X}\n", addr);
                }
                else if (string.IsNullOrEmpty(methodInfo.sourceFileName))
                {
                    if (fullCallStack)
                        sb.AppendFormat("0x{0:X}\t\t{1}\n", addr, methodInfo.methodName);
                    else
                        sb.AppendFormat("{0}\n", methodInfo.methodName);
                }
                else
                {
                    var normalizedPath = methodInfo.sourceFileName.Replace('\\', '/');
                    if (methodInfo.sourceFileLine == 0)
                    {
                        if (fullCallStack)
                            sb.AppendFormat("0x{0:X}\t\t{1}\t<a href=\"{2}\" line=\"1\">{2}</a>\n", addr, methodInfo.methodName, normalizedPath);
                        else
                            sb.AppendFormat("{0}\t<a href=\"{1}\" line=\"1\">{1}</a>\n", methodInfo.methodName, normalizedPath);
                    }
                    else
                    {
                        if (fullCallStack)
                            sb.AppendFormat("0x{0:X}\t\t{1}\t<a href=\"{2}\" line=\"{3}\">{2}:{3}</a>\n", addr, methodInfo.methodName, normalizedPath, methodInfo.sourceFileLine);
                        else
                            sb.AppendFormat("{0}\t<a href=\"{1}\" line=\"{2}\">{1}:{2}</a>\n", methodInfo.methodName, normalizedPath, methodInfo.sourceFileLine);
                    }
                }
            }
        }

        internal class SelectedSampleStackWindow : EditorWindow
        {
            public static class Content
            {
                public static readonly GUIContent title = EditorGUIUtility.TrTextContent("Sample Stack");
                public static readonly GUIContent selectedSampleStack = EditorGUIUtility.TrTextContent("Selected Sample Stack");
                public static readonly GUIContent actualSampleStack = EditorGUIUtility.TrTextContent("Actual Sample Stack");
            }

            [NonSerialized]
            static GCHandle m_PinnedInstance;

            [SerializeField]
            GUIContent m_SelectedSampleStackText = null;
            [SerializeField]
            GUIContent m_ActualSampleStackText = null;

            static readonly Vector2 k_DefaultInitalSize = new Vector2(100, 200);

            [NonSerialized]
            bool initialized = false;

            public static Vector2 CalculateSize(GUIContent selectedSampleStackText, GUIContent actualSampleStackText)
            {
                var copyButtonSize = GUI.skin.button.CalcSize(BaseStyles.tooltipCopyTooltip);
                if (copyButtonSize.x < 2)
                    return Vector2.zero; // Likely in a bad UI state, abort.

                var neededSelectedSampleStackSize = CalcSampleStackSize(Content.selectedSampleStack, selectedSampleStackText, copyButtonSize);
                var neededActualSampleStackSize = CalcSampleStackSize(Content.actualSampleStack, actualSampleStackText, copyButtonSize);

                var neededSize = new Vector2(neededSelectedSampleStackSize.x + neededActualSampleStackSize.x, Mathf.Max(neededSelectedSampleStackSize.y, neededActualSampleStackSize.y));

                neededSize.y += copyButtonSize.y + BaseStyles.magicMarginValue;

                // keep at least a minimum size according to the initial size
                neededSize.x = Mathf.Max(neededSize.x, k_DefaultInitalSize.x);
                return neededSize;
            }

            // use SelectedSampleStackWindow.CalculateSize from within OnGui to get precalculatedSize and avoid glitches on the first frame
            public static void ShowSampleStackWindow(Vector2 position, Vector2 precalculatedSize, GUIContent selectedSampleStackText, GUIContent actualSampleStackText)
            {
                var window = ShowSampleStackWindowInternal(position, precalculatedSize, selectedSampleStackText, actualSampleStackText);
                window.titleContent = Content.title;
                window.initialized = precalculatedSize != Vector2.zero;
            }

            static SelectedSampleStackWindow ShowSampleStackWindowInternal(Vector2 position, Vector2 size,  GUIContent selectedSampleStackText, GUIContent actualSampleStackText)
            {
                if (m_PinnedInstance.IsAllocated && m_PinnedInstance.Target != null)
                {
                    var target = m_PinnedInstance.Target as SelectedSampleStackWindow;
                    target.Close();
                    DestroyImmediate(target);
                }
                // can't calculate the size in here if the call does not come from an OnGui context, so going with a sane default and resizing on initialization
                var rect = new Rect(position, size);
                var window = GetWindowWithRect<SelectedSampleStackWindow>(rect, true);
                window.m_SelectedSampleStackText = selectedSampleStackText;
                window.m_ActualSampleStackText = actualSampleStackText;
                window.m_Parent.window.m_DontSaveToLayout = true;
                return window;
            }

            void OnEnable()
            {
                m_PinnedInstance = GCHandle.Alloc(this);
            }

            void OnDisable()
            {
                m_PinnedInstance.Free();
            }

            void OnGUI()
            {
                if (m_SelectedSampleStackText == null && m_ActualSampleStackText == null)
                    Close();
                if (!initialized)
                {
                    // Ugly fallback in case the size was not pre calculated... this will cause a one frame glitch
                    var neededSize = CalculateSize(m_SelectedSampleStackText, m_ActualSampleStackText);
                    if (neededSize != Vector2.zero)
                    {
                        var rect = new Rect(position.position, neededSize);
                        position = rect;
                        minSize = rect.size;
                        maxSize = rect.size;
                        initialized = true;
                        Repaint();
                    }
                }

                GUILayout.BeginHorizontal();
                ShowSampleStack(Content.selectedSampleStack, m_SelectedSampleStackText);
                ShowSampleStack(Content.actualSampleStack, m_ActualSampleStackText);
                GUILayout.EndHorizontal();
            }

            static Vector2 CalcSampleStackSize(GUIContent label, GUIContent sampleStack, Vector2 copyButtonSize)
            {
                if (sampleStack != null)
                {
                    var neededSelectedSampleStackSize = GUI.skin.textArea.CalcSize(sampleStack);
                    var labelSize = GUI.skin.label.CalcSize(Content.selectedSampleStack);
                    neededSelectedSampleStackSize.y += copyButtonSize.y + BaseStyles.magicMarginValue;
                    neededSelectedSampleStackSize.x = Mathf.Max(neededSelectedSampleStackSize.x + BaseStyles.magicMarginValue, labelSize.x + BaseStyles.magicMarginValue);
                    neededSelectedSampleStackSize.x = Mathf.Max(neededSelectedSampleStackSize.x + BaseStyles.magicMarginValue, copyButtonSize.x + BaseStyles.magicMarginValue);
                    return neededSelectedSampleStackSize;
                }
                return Vector2.zero;
            }

            void ShowSampleStack(GUIContent label, GUIContent sampleStack)
            {
                if (sampleStack != null)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label(label);
                    GUILayout.TextArea(sampleStack.text, GUILayout.ExpandHeight(true));
                    if (GUILayout.Button(BaseStyles.tooltipCopyTooltip))
                    {
                        Clipboard.stringValue = sampleStack.text;
                    }
                    GUILayout.EndVertical();
                }
            }
        }

        public virtual void Clear()
        {
        }
    }
}
