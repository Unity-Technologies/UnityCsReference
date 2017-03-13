// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor
{
    class GUIViewDebuggerWindow : EditorWindow
    {
        internal class Styles
        {
            public readonly GUIStyle listItem = new GUIStyle("PR Label");
            public readonly GUIStyle listItemBackground = new GUIStyle("CN EntryBackOdd");
            public readonly GUIStyle listBackgroundStyle = new GUIStyle("CN Box");
            public readonly GUIStyle boxStyle = new GUIStyle("CN Box");
            public readonly GUIStyle stackframeStyle = new GUIStyle(EditorStyles.label);
            public readonly GUIStyle stacktraceBackground = new GUIStyle("CN Box");
            public readonly GUIStyle centeredText = new GUIStyle("PR Label");


            public Styles()
            {
                stackframeStyle.margin = new RectOffset(0, 0, 0, 0);
                stackframeStyle.padding = new RectOffset(0, 0, 0, 0);
                stacktraceBackground.padding = new RectOffset(5, 5, 5, 5);

                centeredText.alignment = TextAnchor.MiddleCenter;
                centeredText.stretchHeight = true;
                centeredText.stretchWidth = true;
            }
        }

        enum InstructionType
        {
            Draw,
            Clip,
            Layout,
            Unified,
        }


        public GUIView m_Inspected;
        InstructionType m_InstructionType = InstructionType.Draw;

        bool m_ShowOverlay = true;


        [NonSerialized]
        int m_LastSelectedRow;


        [NonSerialized]
        private bool m_QueuedPointInspection = false;

        [NonSerialized]
        Vector2 m_PointToInspect;

        public static Styles s_Styles;

        //TODO: figure out proper minimum values and make sure the window also has compatible minimum size
        readonly SplitterState m_InstructionListDetailSplitter = new SplitterState(new float[] { 30, 70 }, new int[] { 32, 32 }, null);


        InstructionOverlayWindow m_InstructionOverlayWindow;

        static GUIViewDebuggerWindow s_ActiveInspector;
        private IBaseInspectView m_InstructionModeView;

        public GUIViewDebuggerWindow()
        {
            m_InstructionModeView = new StyleDrawInspectView(this);
        }

        public IBaseInspectView instructionModeView
        {
            get { return m_InstructionModeView; }
        }

        public InstructionOverlayWindow InstructionOverlayWindow
        {
            set { m_InstructionOverlayWindow = value; }
            get { return m_InstructionOverlayWindow; }
        }

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

        static void InspectPoint(Vector2 point)
        {
            Debug.Log("Inspecting " + point);
            s_ActiveInspector.InspectPointAt(point);
        }

        void OnEnable()
        {
            titleContent =  new GUIContent("GUI Inspector");
        }

        void OnGUI()
        {
            InitializeStylesIfNeeded();
            DoToolbar();
            ShowDrawInstructions();
        }

        void InitializeStylesIfNeeded()
        {
            if (s_Styles == null)
                s_Styles = new GUIViewDebuggerWindow.Styles();
        }

        static void OnInspectedViewChanged()
        {
            if (s_ActiveInspector == null)
                return;
            s_ActiveInspector.RefreshData();
            s_ActiveInspector.Repaint();
        }

        void DoToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            DoWindowPopup();
            DoInspectTypePopup();
            DoInstructionOverlayToggle();

            GUILayout.EndHorizontal();
        }

        private void DoWindowPopup()
        {
            string selectedName = "<Please Select>";
            if (m_Inspected != null)
                selectedName = GetViewName(m_Inspected);

            GUILayout.Label("Inspected Window: ", GUILayout.ExpandWidth(false));

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

                    string viewName = options.Count + ". " +  GetViewName(view);
                    GUIContent content = new GUIContent(viewName);
                    options.Add(content);
                    selectableViews.Add(view);

                    if (view == m_Inspected)
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
            {
                m_InstructionType = newInstructionType;
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
                    case InstructionType.Unified:
                        m_InstructionModeView = new UnifiedInspectView(this);
                        break;
                }
                m_InstructionModeView.UpdateInstructions();
            }
        }

        private void DoInstructionOverlayToggle()
        {
            EditorGUI.BeginChangeCheck();
            m_ShowOverlay = GUILayout.Toggle(m_ShowOverlay, GUIContent.Temp("Show overlay"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                OnShowOverlayChanged();
            }
        }

        private void OnShowOverlayChanged()
        {
            if (m_ShowOverlay == false)
            {
                if (m_InstructionOverlayWindow != null)
                {
                    m_InstructionOverlayWindow.Close();
                }
            }
            else
            {
                if (m_Inspected != null)
                {
                    instructionModeView.ShowOverlay();
                }
            }
        }

        private bool CanInspectView(GUIView view)
        {
            if (view == null)
                return false;

            EditorWindow editorWindow = GetEditorWindow(view);
            if (editorWindow == null)
                return true;

            if (editorWindow == this || editorWindow == m_InstructionOverlayWindow)
                return false;

            return true;
        }

        void OnWindowSelected(object userdata, string[] options, int selected)
        {
            GUIView newInspected;
            selected--;
            if (selected >= 0)
            {
                List<GUIView> views = (List<GUIView>)userdata;
                newInspected = views[selected];
            }
            else
                newInspected = null;

            if (m_Inspected != newInspected)
            {
                if (m_InstructionOverlayWindow != null)
                    m_InstructionOverlayWindow.Close();

                m_Inspected = newInspected;
                if (m_Inspected != null)
                {
                    GUIViewDebuggerHelper.DebugWindow(m_Inspected);
                    m_Inspected.Repaint();
                }
                else
                {
                    GUIViewDebuggerHelper.StopDebugging();
                }
                instructionModeView.Unselect();
            }


            Repaint();
        }

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

        private void RefreshData()
        {
            instructionModeView.UpdateInstructions();
        }

        void ShowDrawInstructions()
        {
            if (m_Inspected == null)
                return;

            if (m_QueuedPointInspection)
            {
                instructionModeView.Unselect();
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

        public void HighlightInstruction(GUIView view, Rect instructionRect, GUIStyle style)
        {
            /*if (m_ListViewState.row < 0)
                return;
                */
            if (!m_ShowOverlay)
                return;

            if (m_InstructionOverlayWindow == null)
            {
                m_InstructionOverlayWindow = CreateInstance<InstructionOverlayWindow>();
            }


            m_InstructionOverlayWindow.Show(view, instructionRect, style);
            Focus();
        }

        void InspectPointAt(Vector2 point)
        {
            m_PointToInspect = point;
            m_QueuedPointInspection = true;
            m_Inspected.Repaint();
            Repaint();
        }

        int FindInstructionUnderPoint(Vector2 point)
        {
            int instructionCount = GUIViewDebuggerHelper.GetInstructionCount();
            for (int i = 0; i < instructionCount; ++i)
            {
                Rect rect = GUIViewDebuggerHelper.GetRectFromInstruction(i);
                if (rect.Contains(point))
                {
                    return i;
                }
            }
            return -1;
        }

        void OnDisable()
        {
            GUIViewDebuggerHelper.StopDebugging();
            if (m_InstructionOverlayWindow != null)
                m_InstructionOverlayWindow.Close();
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
}
