// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal interface IBaseInspectView
    {
        void UpdateInstructions();
        void DrawInstructionList();
        void DrawSelectedInstructionDetails();
        void ShowOverlay();
        void SelectRow(int index);
        void ClearRowSelection();
    }

    internal abstract class BaseInspectView : IBaseInspectView
    {
        protected static class Styles
        {
            public static readonly GUIContent instructionsLabel = new GUIContent("Instructions");
            public static readonly GUIContent emptyViewLabel = new GUIContent("Select an Instruction on the left to see details");

            public static readonly GUIStyle centeredLabel = new GUIStyle("PR Label");

            static Styles()
            {
                centeredLabel.alignment = TextAnchor.MiddleCenter;
                centeredLabel.padding.right = 0;
                centeredLabel.padding.left = 0;
            }
        }

        protected ListViewState listViewState { get { return m_ListViewState; } }
        [NonSerialized]
        readonly ListViewState m_ListViewState = new ListViewState();

        protected GUIViewDebuggerWindow debuggerWindow { get { return m_DebuggerWindow; } }
        GUIViewDebuggerWindow m_DebuggerWindow;

        Vector2 m_InstructionDetailsScrollPos = new Vector2();

        readonly SplitterState m_InstructionDetailStacktraceSplitter = new SplitterState(new float[] { 80, 20 }, new int[] { 100, 100 }, null);

        public BaseInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow)
        {
            m_DebuggerWindow = guiViewDebuggerWindow;
        }

        public abstract void UpdateInstructions();

        public virtual void DrawInstructionList()
        {
            Event evt = Event.current;
            m_ListViewState.totalRows = GetInstructionCount();

            EditorGUILayout.BeginVertical(GUIViewDebuggerWindow.Styles.listBackgroundStyle);
            GUILayout.Label(Styles.instructionsLabel);

            int id = GUIUtility.GetControlID(FocusType.Keyboard);
            foreach (var element in ListViewGUI.ListView(m_ListViewState, GUIViewDebuggerWindow.Styles.listBackgroundStyle))
            {
                ListViewElement listViewElement = (ListViewElement)element;
                if (evt.type == EventType.MouseDown && evt.button == 0 && listViewElement.position.Contains(evt.mousePosition))
                {
                    if (evt.clickCount == 2)
                    {
                        OnDoubleClickInstruction(listViewElement.row);
                    }
                }
                // Paint list view element
                if (evt.type == EventType.Repaint && listViewElement.row < GetInstructionCount())
                {
                    DoDrawInstruction(listViewElement, id);
                }
            }
            EditorGUILayout.EndVertical();
        }

        public virtual void DrawSelectedInstructionDetails()
        {
            if (m_ListViewState.selectionChanged)
                OnSelectedInstructionChanged(m_ListViewState.row);
            else if (m_ListViewState.row >= GetInstructionCount())
                OnSelectedInstructionChanged(-1);

            if (!isInstructionSelected)
            {
                DoDrawNothingSelected();
                return;
            }

            SplitterGUILayout.BeginVerticalSplit(m_InstructionDetailStacktraceSplitter);

            m_InstructionDetailsScrollPos = EditorGUILayout.BeginScrollView(m_InstructionDetailsScrollPos, GUIViewDebuggerWindow.Styles.boxStyle);

            DoDrawSelectedInstructionDetails(m_ListViewState.row);
            EditorGUILayout.EndScrollView();

            DrawInspectedStacktrace();
            SplitterGUILayout.EndVerticalSplit();
        }

        public abstract void ShowOverlay();

        public virtual void SelectRow(int index)
        {
            m_ListViewState.row = index;
            m_ListViewState.selectionChanged = true;
        }

        public virtual void ClearRowSelection()
        {
            m_ListViewState.row = -1;
            m_ListViewState.selectionChanged = true;
        }

        protected abstract int GetInstructionCount();

        protected abstract void DoDrawInstruction(ListViewElement el, int controlId);

        protected abstract void DrawInspectedStacktrace();

        protected virtual bool isInstructionSelected { get { return m_ListViewState.row >= 0 && m_ListViewState.row < GetInstructionCount(); } }

        protected void DrawStackFrameList(StackFrame[] stackframes)
        {
            if (stackframes != null)
            {
                foreach (var stackframe in stackframes)
                {
                    if (string.IsNullOrEmpty(stackframe.sourceFile))
                        continue;

                    GUILayout.Label(string.Format("{0} [{1}:{2}]", stackframe.signature, stackframe.sourceFile, stackframe.lineNumber), GUIViewDebuggerWindow.Styles.stackframeStyle);
                    //GUILayout.Label(string.Format("{0} - {1}", stackframe.methodName, stackframe.moduleName), stackframeStyle);
                }
            }
        }

        protected void DrawInspectedRect(Rect instructionRect)
        {
            var totalRect = GUILayoutUtility.GetRect(0, 100);

            var reserveTopFieldHeight = Mathf.CeilToInt(EditorGUI.kSingleLineHeight * 2 + EditorGUI.kControlVerticalSpacing);
            var reserveBottomFieldHeight = Mathf.CeilToInt(EditorGUI.kSingleLineHeight);
            var reserveFieldWidth = 100;
            var fieldsArea = new RectOffset(50, reserveFieldWidth, reserveTopFieldHeight, reserveBottomFieldHeight);
            var visualRect = fieldsArea.Remove(totalRect);

            float aspectRatio = instructionRect.width / instructionRect.height;
            var aspectedRect = new Rect();
            var dummy = new Rect();

            GUI.CalculateScaledTextureRects(visualRect, ScaleMode.ScaleToFit, aspectRatio, ref aspectedRect, ref dummy);
            visualRect = aspectedRect;
            visualRect.width = Mathf.Max(80, visualRect.width);
            visualRect.height = Mathf.Max(EditorGUI.kSingleLineHeight + 10, visualRect.height);

            var startPointFieldRect = new Rect();
            startPointFieldRect.height = EditorGUI.kSingleLineHeight;
            startPointFieldRect.width = fieldsArea.left * 2;
            startPointFieldRect.y = visualRect.y - fieldsArea.top;
            startPointFieldRect.x = visualRect.x - startPointFieldRect.width / 2f;

            var endPointFieldRect = new Rect
            {
                height = EditorGUI.kSingleLineHeight,
                width = fieldsArea.right * 2,
                y = visualRect.yMax
            };
            endPointFieldRect.x = visualRect.xMax - endPointFieldRect.width / 2f;

            var widthMarkersArea = new Rect
            {
                x = visualRect.x,
                y = startPointFieldRect.yMax + EditorGUI.kControlVerticalSpacing,
                width = visualRect.width,
                height = EditorGUI.kSingleLineHeight
            };

            var widthFieldRect = widthMarkersArea;
            widthFieldRect.width = widthMarkersArea.width / 3;
            widthFieldRect.x = widthMarkersArea.x + (widthMarkersArea.width - widthFieldRect.width) / 2f;

            var heightMarkerArea = visualRect;
            heightMarkerArea.x = visualRect.xMax;
            heightMarkerArea.width = EditorGUI.kSingleLineHeight;

            var heightFieldRect = heightMarkerArea;
            heightFieldRect.height = EditorGUI.kSingleLineHeight;
            heightFieldRect.width = fieldsArea.right;
            heightFieldRect.y = heightFieldRect.y + (heightMarkerArea.height - heightFieldRect.height) / 2f;

            //Draw TopLeft point
            GUI.Label(startPointFieldRect, string.Format("({0},{1})", instructionRect.x, instructionRect.y), Styles.centeredLabel);

            Handles.color = new Color(1, 1, 1, 0.5f);
            //Draw Width markers and value
            var startP = new Vector3(widthMarkersArea.x, widthFieldRect.y);
            var endP = new Vector3(widthMarkersArea.x, widthFieldRect.yMax);
            Handles.DrawLine(startP, endP);

            startP.x = endP.x = widthMarkersArea.xMax;
            Handles.DrawLine(startP, endP);

            startP.x = widthMarkersArea.x;
            startP.y = endP.y = Mathf.Lerp(startP.y, endP.y, .5f);
            endP.x = widthFieldRect.x;
            Handles.DrawLine(startP, endP);

            startP.x = widthFieldRect.xMax;
            endP.x = widthMarkersArea.xMax;
            Handles.DrawLine(startP, endP);

            GUI.Label(widthFieldRect, instructionRect.width.ToString(), Styles.centeredLabel);

            //Draw Height markers and value
            startP = new Vector3(heightMarkerArea.x, heightMarkerArea.y);
            endP = new Vector3(heightMarkerArea.xMax, heightMarkerArea.y);
            Handles.DrawLine(startP, endP);

            startP.y = endP.y = heightMarkerArea.yMax;
            Handles.DrawLine(startP, endP);

            startP.x = endP.x = Mathf.Lerp(startP.x, endP.x, .5f);
            startP.y = heightMarkerArea.y;
            endP.y = heightFieldRect.y;
            Handles.DrawLine(startP, endP);

            startP.y = heightFieldRect.yMax;
            endP.y = heightMarkerArea.yMax;
            Handles.DrawLine(startP, endP);

            GUI.Label(heightFieldRect, instructionRect.height.ToString());

            GUI.Label(endPointFieldRect, string.Format("({0},{1})", instructionRect.xMax, instructionRect.yMax), Styles.centeredLabel);

            //Draws the rect
            GUI.Box(visualRect, GUIContent.none);
        }

        protected void DoSelectableInstructionDataField(string label, string instructionData)
        {
            var rect = EditorGUILayout.GetControlRect(true);
            EditorGUI.LabelField(rect, label);
            rect.xMin += EditorGUIUtility.labelWidth;
            EditorGUI.SelectableLabel(rect, instructionData);
        }

        internal abstract void DoDrawSelectedInstructionDetails(int selectedInstructionIndex);

        internal abstract string GetInstructionListName(int index);

        internal abstract void OnDoubleClickInstruction(int index);

        internal abstract void OnSelectedInstructionChanged(int newSelectionIndex);

        void DoDrawNothingSelected()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Styles.emptyViewLabel, GUIViewDebuggerWindow.Styles.centeredText);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
    }
}
