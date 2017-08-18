// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

            public static readonly GUIContent inspectedWindowLabel = new GUIContent("Inspected View: ");

            public static readonly GUIStyle listItem = new GUIStyle("PR Label");
            public static readonly GUIStyle listItemBackground = new GUIStyle("CN EntryBackOdd");
            public static readonly GUIStyle listBackgroundStyle = new GUIStyle("CN Box");
            public static readonly GUIStyle boxStyle = new GUIStyle("CN Box");
            public static readonly GUIStyle stackframeStyle = new GUIStyle(EditorStyles.label);
            public static readonly GUIStyle stacktraceBackground = new GUIStyle("CN Box");
            public static readonly GUIStyle centeredText = new GUIStyle("PR Label");

            public static readonly Color contentHighlighterColor = new Color(0.62f, 0.77f, 0.90f, 0.5f);
            public static readonly Color paddingHighlighterColor = new Color(0.76f, 0.87f, 0.71f, 0.5f);

            static Styles()
            {
                stackframeStyle.margin = new RectOffset(0, 0, 0, 0);
                stackframeStyle.padding = new RectOffset(0, 0, 0, 0);
                stacktraceBackground.padding = new RectOffset(5, 5, 5, 5);

                centeredText.alignment = TextAnchor.MiddleCenter;
                centeredText.stretchHeight = true;
                centeredText.stretchWidth = true;
            }
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
                        GUIViewDebuggerHelper.DebugWindow(m_Inspected);
                        m_Inspected.Repaint();
                    }
                    else
                    {
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

        public IBaseInspectView instructionModeView { get { return m_InstructionModeView; } }
        IBaseInspectView m_InstructionModeView;

        protected GUIViewDebuggerWindow()
        {
            m_InstructionModeView = new StyleDrawInspectView(this);
        }

        public void ClearInstructionHighlighter()
        {
            if (m_PaddingHighlighter != null && m_PaddingHighlighter.shadow.parent != null)
            {
                var parent = m_PaddingHighlighter.shadow.parent;

                m_PaddingHighlighter.RemoveFromHierarchy();
                m_ContentHighlighter.RemoveFromHierarchy();

                parent.Dirty(ChangeType.Repaint);
            }
        }

        public void HighlightInstruction(GUIView view, Rect instructionRect, GUIStyle style)
        {
            if (!m_ShowHighlighter)
                return;

            ClearInstructionHighlighter();

            if (m_PaddingHighlighter == null)
            {
                m_PaddingHighlighter = new VisualElement();
                m_PaddingHighlighter.style.backgroundColor = Styles.paddingHighlighterColor;
                m_ContentHighlighter = new VisualElement();
                m_ContentHighlighter.style.backgroundColor = Styles.contentHighlighterColor;
            }
            m_PaddingHighlighter.layout = instructionRect;
            view.visualTree.Add(m_PaddingHighlighter);
            if (style != null)
                instructionRect = style.padding.Remove(instructionRect);
            m_ContentHighlighter.layout = instructionRect;
            view.visualTree.Add(m_ContentHighlighter);
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

        VisualElement m_ContentHighlighter;
        VisualElement m_PaddingHighlighter;
        bool m_ShowHighlighter = true;

        [NonSerialized]
        bool m_QueuedPointInspection = false;

        [NonSerialized]
        Vector2 m_PointToInspect;

        //TODO: figure out proper minimum values and make sure the window also has compatible minimum size
        readonly SplitterState m_InstructionListDetailSplitter = new SplitterState(new float[] { 30, 70 }, new int[] { 32, 32 }, null);

        //Internal Tool for now. Uncomment it to enable it.
        //[MenuItem("Window/GUIView Inspector")]
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
            titleContent =  new GUIContent("GUI Inspector");
            GUIViewDebuggerHelper.onViewInstructionsChanged += OnInspectedViewChanged;
            GUIView serializedInspected = m_Inspected;
            inspected = null;
            inspected = serializedInspected;
            m_InstructionModeView = null;
            instructionType = m_InstructionType;
        }

        void OnDisable()
        {
            GUIViewDebuggerHelper.onViewInstructionsChanged -= OnInspectedViewChanged;
            GUIViewDebuggerHelper.StopDebugging();
            ClearInstructionHighlighter();
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
            DoToolbar();
            ShowDrawInstructions();
        }

        void OnInspectedViewChanged()
        {
            RefreshData();
            Repaint();
        }

        void DoToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            DoWindowPopup();
            DoInspectTypePopup();
            DoInstructionOverlayToggle();

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

                options.Add(new GUIContent("None"));

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
            m_ShowHighlighter = GUILayout.Toggle(m_ShowHighlighter, GUIContent.Temp("Show overlay"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                OnShowOverlayChanged();
            }
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

            if (m_QueuedPointInspection)
            {
                instructionModeView.ClearRowSelection();
                instructionModeView.SelectRow(FindInstructionUnderPoint(m_PointToInspect));
                m_QueuedPointInspection = false;
            }

            SplitterGUILayout.BeginHorizontalSplit(m_InstructionListDetailSplitter);

            instructionModeView.DrawInstructionList();

            EditorGUILayout.BeginVertical();
            {
                instructionModeView.DrawSelectedInstructionDetails();
            }
            EditorGUILayout.EndVertical();

            SplitterGUILayout.EndHorizontalSplit();
        }

        void InspectPointAt(Vector2 point)
        {
            m_PointToInspect = point;
            m_QueuedPointInspection = true;
            inspected.Repaint();
            Repaint();
        }

        int FindInstructionUnderPoint(Vector2 point)
        {
            List<IMGUIDrawInstruction> drawInstructions = new List<IMGUIDrawInstruction>();
            GUIViewDebuggerHelper.GetDrawInstructions(drawInstructions);
            for (int i = 0; i < drawInstructions.Count; ++i)
            {
                Rect rect = drawInstructions[i].rect;
                if (rect.Contains(point))
                {
                    return i;
                }
            }
            return -1;
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

    internal partial class GUIViewDebuggerHelper
    {
        internal static Action onViewInstructionsChanged;

        [RequiredByNativeCode]
        private static void CallOnViewInstructionsChanged()
        {
            if (onViewInstructionsChanged != null)
                onViewInstructionsChanged();
        }
    }
}
