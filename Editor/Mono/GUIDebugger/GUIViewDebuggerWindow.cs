// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.UIElements.Debugger;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Scripting;

namespace UnityEditor
{
    class GUIViewDebuggerWindow : EditorWindow
    {
        enum InstructionType
        {
            Draw,
            Clip,
            Layout,
            NamedControl,
            Property,
            Unified,
        }

        internal static class Styles
        {
            public static readonly string defaultWindowPopupText = "<Please Select>";

            public static readonly GUIContent inspectedWindowLabel = EditorGUIUtility.TrTextContent("Inspected View: ");
            public static readonly GUIContent pickStyleLabel = EditorGUIUtility.TrTextContent("Pick Style");
            public static readonly GUIContent pickingStyleLabel = EditorGUIUtility.TrTextContent("Picking   ");

            public static readonly GUIStyle listItem = "PR Label";
            public static readonly GUIStyle listItemBackground = "CN EntryBackOdd";
            public static readonly GUIStyle listBackgroundStyle = "CN Box";
            public static readonly GUIStyle boxStyle = "CN Box";
            public static readonly GUIStyle messageStyle = "CN Message";
            public static readonly GUIStyle stackframeStyle = "CN StacktraceStyle";
            public static readonly GUIStyle stacktraceBackground = "CN StacktraceBackground";
            public static readonly GUIStyle centeredText = "CN CenteredText";

            public static readonly Color contentHighlighterColor = new Color(0.62f, 0.77f, 0.90f, 0.5f);
            public static readonly Color paddingHighlighterColor = new Color(0.76f, 0.87f, 0.71f, 0.5f);
        }

        static GUIViewDebuggerWindow s_ActiveInspector;

        static EditorWindow GetEditorWindow(GUIView view)
        {
            var hostView = view as HostView;
            if (hostView != null)
                return hostView.actualView;

            return null;
        }

        static string GetViewName(GUIView view)
        {
            var editorWindow = GetEditorWindow(view);
            if (editorWindow != null)
                return editorWindow.titleContent.text;

            return view.GetType().Name;
        }

        public GUIView inspected
        {
            get
            {
                if (m_Inspected != null || m_InspectedEditorWindow == null)
                    return m_Inspected;
                // continue inspecting the same window if its dock area is destroyed by e.g., docking or undocking it
                return inspected = m_InspectedEditorWindow.m_Parent;
            }
            private set
            {
                if (m_Inspected != value)
                {
                    ClearInstructionHighlighter();

                    m_Inspected = value;
                    if (m_Inspected != null)
                    {
                        m_InspectedEditorWindow = (m_Inspected is HostView) ? ((HostView)m_Inspected).actualView : null;
                        if (!m_StylePicker.IsPicking)
                            GUIViewDebuggerHelper.DebugWindow(m_Inspected);
                        m_Inspected.Repaint();
                    }
                    else
                    {
                        m_InspectedEditorWindow = null;
                        GUIViewDebuggerHelper.StopDebugging();
                    }
                    if (instructionModeView != null)
                        instructionModeView.ClearRowSelection();

                    OnInspectedViewChanged();
                }
            }
        }
        [SerializeField]
        GUIView m_Inspected;
        EditorWindow m_InspectedEditorWindow;
        ElementHighlighter m_Highlighter;
        StylePicker m_StylePicker;

        public IBaseInspectView instructionModeView { get { return m_InstructionModeView; } }
        IBaseInspectView m_InstructionModeView;

        protected GUIViewDebuggerWindow()
        {
            m_InstructionModeView = new StyleDrawInspectView(this);
            m_Highlighter = new ElementHighlighter();
            m_StylePicker = new StylePicker(this, m_Highlighter);
            m_StylePicker.CanInspectView = CanInspectView;
        }

        public void ClearInstructionHighlighter()
        {
            if (!m_StylePicker.IsPicking)
                m_Highlighter.ClearElement();
        }

        public void HighlightInstruction(GUIView view, Rect instructionRect, GUIStyle style)
        {
            if (!m_ShowHighlighter)
                return;

            ClearInstructionHighlighter();

            m_Highlighter.HighlightElement(view.visualTree, instructionRect, style);
        }

        InstructionType instructionType
        {
            get { return m_InstructionType; }
            set
            {
                if (m_InstructionType != value || m_InstructionModeView == null)
                {
                    m_InstructionType = value;
                    switch (m_InstructionType)
                    {
                        case InstructionType.Clip:
                            m_InstructionModeView = new GUIClipInspectView(this);
                            break;
                        case InstructionType.Draw:
                            m_InstructionModeView = new StyleDrawInspectView(this);
                            break;
                        case InstructionType.Layout:
                            m_InstructionModeView = new GUILayoutInspectView(this);
                            break;
                        case InstructionType.NamedControl:
                            m_InstructionModeView = new GUINamedControlInspectView(this);
                            break;
                        case InstructionType.Property:
                            m_InstructionModeView = new GUIPropertyInspectView(this);
                            break;
                        case InstructionType.Unified:
                            m_InstructionModeView = new UnifiedInspectView(this);
                            break;
                    }
                    m_InstructionModeView.UpdateInstructions();
                }
            }
        }
        [SerializeField]
        InstructionType m_InstructionType = InstructionType.Draw;

        [SerializeField]
        bool m_ShowHighlighter = true;

        [SerializeField]
        private bool m_InspectOptimizedGUIBlocks = false;

        //TODO: figure out proper minimum values and make sure the window also has compatible minimum size
        readonly SplitterState m_InstructionListDetailSplitter = new SplitterState(new float[] { 30, 70 }, new int[] { 32, 32 }, null);

        //Internal Tool for now. Uncomment it to enable it.
        [MenuItem("Window/Analysis/IMGUI Debugger", false, 102, true)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            if (s_ActiveInspector == null)
            {
                GUIViewDebuggerWindow window = (GUIViewDebuggerWindow)GetWindow(typeof(GUIViewDebuggerWindow));
                s_ActiveInspector = window;
            }
            s_ActiveInspector.Show();
        }

        void OnEnable()
        {
            titleContent =  EditorGUIUtility.TrTextContent("IMGUI Debugger");
            GUIViewDebuggerHelper.onViewInstructionsChanged += OnInspectedViewChanged;
            GUIViewDebuggerHelper.onDebuggingViewchanged += OnDebuggedViewChanged;
            GUIView serializedInspected = m_Inspected;
            inspected = null;
            inspected = serializedInspected;
            m_InstructionModeView = null;
            instructionType = m_InstructionType;
        }

        void OnDestroy()
        {
            m_StylePicker.StopExploreStyle();
            m_StylePicker = null;
        }

        void OnDisable()
        {
            GUIViewDebuggerHelper.onViewInstructionsChanged -= OnInspectedViewChanged;
            GUIViewDebuggerHelper.onDebuggingViewchanged -= OnDebuggedViewChanged;
            inspected = null;
            m_StylePicker.StopExploreStyle();
        }

        void OnBecameVisible()
        {
            OnShowOverlayChanged();
        }

        void OnBecameInvisible()
        {
            ClearInstructionHighlighter();
        }

        void OnGUI()
        {
            HandleStylePicking();
            DoToolbar();
            ShowDrawInstructions();
        }

        private bool m_FlushingOptimizedGUIBlock;

        void OnDebuggedViewChanged(GUIView view, bool isDebugged)
        {
            if (!m_StylePicker.IsPicking && isDebugged && inspected != view)
            {
                inspected = view;
            }
        }

        void OnInspectedViewChanged()
        {
            if (m_InspectOptimizedGUIBlocks && !m_FlushingOptimizedGUIBlock)
            {
                var inspector = m_InspectedEditorWindow as InspectorWindow;
                if (inspector != null && inspector.tracker != null)
                {
                    m_FlushingOptimizedGUIBlock = true;
                    foreach (var editor in inspector.tracker.activeEditors)
                        editor.isInspectorDirty = true;
                    inspector.Repaint();
                }
            }
            m_FlushingOptimizedGUIBlock = false;
            RefreshData();
            Repaint();
        }

        void HandleStylePicking()
        {
            if (m_StylePicker != null && m_StylePicker.IsPicking)
            {
                m_StylePicker.OnGUI();
                if (Event.current.type == EventType.Ignore || Event.current.type == EventType.MouseUp)
                {
                    m_StylePicker.StopExploreStyle();
                }

                if (m_StylePicker.ExploredView != null && inspected != m_StylePicker.ExploredView)
                {
                    inspected = m_StylePicker.ExploredView;
                }
            }
        }

        void DoToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            DoWindowPopup();
            DoInspectTypePopup();
            DoInstructionOverlayToggle();
            DoOptimizedGUIBlockToggle();
            DoStylePicker();

            GUILayout.EndHorizontal();
        }

        bool CanInspectView(GUIView view)
        {
            if (view == null)
                return false;

            EditorWindow editorWindow = GetEditorWindow(view);
            if (editorWindow == null)
                return true;

            if (editorWindow == this)
                return false;

            return true;
        }

        void DoWindowPopup()
        {
            string selectedName = inspected == null ? Styles.defaultWindowPopupText : GetViewName(inspected);

            GUILayout.Label(Styles.inspectedWindowLabel, GUILayout.ExpandWidth(false));

            Rect popupPosition = GUILayoutUtility.GetRect(GUIContent.Temp(selectedName), EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(true));
            if (GUI.Button(popupPosition, GUIContent.Temp(selectedName), EditorStyles.toolbarDropDown))
            {
                List<GUIView> views = new List<GUIView>();
                GUIViewDebuggerHelper.GetViews(views);

                List<GUIContent> options = new List<GUIContent>(views.Count + 1);

                options.Add(EditorGUIUtility.TrTextContent("None"));

                int selectedIndex = 0;
                List<GUIView> selectableViews = new List<GUIView>(views.Count + 1);
                for (int i = 0; i < views.Count; ++i)
                {
                    GUIView view = views[i];

                    //We can't inspect ourselves, otherwise we get infinite recursion.
                    //Also avoid the InstructionOverlay
                    if (!CanInspectView(view))
                        continue;

                    GUIContent label = new GUIContent(string.Format("{0}. {1}", options.Count,  GetViewName(view)));
                    options.Add(label);
                    selectableViews.Add(view);

                    if (view == inspected)
                        selectedIndex = selectableViews.Count;
                }
                //TODO: convert this to a Unity Window style popup. This way we could highlight the window on hover ;)
                EditorUtility.DisplayCustomMenu(popupPosition, options.ToArray(), selectedIndex, OnWindowSelected, selectableViews);
            }
        }

        void DoInspectTypePopup()
        {
            EditorGUI.BeginChangeCheck();
            var newInstructionType = (InstructionType)EditorGUILayout.EnumPopup(m_InstructionType, EditorStyles.toolbarDropDown);
            if (EditorGUI.EndChangeCheck())
                instructionType = newInstructionType;
        }

        void DoInstructionOverlayToggle()
        {
            EditorGUI.BeginChangeCheck();
            m_ShowHighlighter = GUILayout.Toggle(m_ShowHighlighter, GUIContent.Temp("Show Overlay"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                OnShowOverlayChanged();
            }
        }

        void DoOptimizedGUIBlockToggle()
        {
            EditorGUI.BeginChangeCheck();
            m_InspectOptimizedGUIBlocks = GUILayout.Toggle(m_InspectOptimizedGUIBlocks, GUIContent.Temp("Force Inspect Optimized GUI Blocks"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
                OnInspectedViewChanged();
        }

        void DoStylePicker()
        {
            var pickerRect = GUILayoutUtility.GetRect(Styles.pickStyleLabel, EditorStyles.toolbarButton);
            if (!m_StylePicker.IsPicking && Event.current.isMouse && Event.current.type == EventType.MouseDown && pickerRect.Contains(Event.current.mousePosition))
            {
                m_StylePicker.StartExploreStyle();
            }
            GUI.Toggle(pickerRect, m_StylePicker.IsPicking, m_StylePicker.IsPicking ? Styles.pickingStyleLabel : Styles.pickStyleLabel, EditorStyles.toolbarButton);
        }

        void OnShowOverlayChanged()
        {
            if (m_ShowHighlighter == false)
            {
                ClearInstructionHighlighter();
            }
            else
            {
                if (inspected != null)
                {
                    instructionModeView.ShowOverlay();
                }
            }
        }

        void OnWindowSelected(object userdata, string[] options, int selected)
        {
            selected--;
            inspected = selected < 0 ? null : ((List<GUIView>)userdata)[selected];
        }

        void RefreshData()
        {
            instructionModeView.UpdateInstructions();
        }

        void ShowDrawInstructions()
        {
            if (inspected == null)
            {
                ClearInstructionHighlighter();
                return;
            }

            SplitterGUILayout.BeginHorizontalSplit(m_InstructionListDetailSplitter);

            instructionModeView.DrawInstructionList();

            EditorGUILayout.BeginVertical();
            {
                if (m_StylePicker.IsPicking &&
                    m_StylePicker.ExploredDrawInstructionIndex != -1 &&
                    m_InstructionType == InstructionType.Draw)
                {
                    m_InstructionModeView.ClearRowSelection();
                    m_InstructionModeView.SelectRow(m_StylePicker.ExploredDrawInstructionIndex);
                }
                instructionModeView.DrawSelectedInstructionDetails(position.width - m_InstructionListDetailSplitter.realSizes[0]);
            }
            EditorGUILayout.EndVertical();

            SplitterGUILayout.EndHorizontalSplit();
        }
    }

    //This needs to match ManagedStackFrameToMono in native
    [StructLayout(LayoutKind.Sequential)]
    struct StackFrame
    {
        public uint   lineNumber;
        public string sourceFile;
        public string methodName;
        public string signature;
        public string moduleName; //TODO: we only get "Mono JIT Code" or "wrapper something", can we get the actual assembly name?
    }

    internal static partial class GUIViewDebuggerHelper
    {
        internal static event Action onViewInstructionsChanged = null;
        internal static event Action<GUIView, bool> onDebuggingViewchanged = null;

        [RequiredByNativeCode]
        private static void CallOnViewInstructionsChanged()
        {
            onViewInstructionsChanged?.Invoke();
        }

        [RequiredByNativeCode]
        private static void CallDebuggingViewChanged(GUIView view, bool isBeingDebugged)
        {
            onDebuggingViewchanged?.Invoke(view, isBeingDebugged);
        }
    }
}
