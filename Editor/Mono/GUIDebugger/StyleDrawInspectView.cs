// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    //The only purpose of this class is to enable us to view the used GUIStyle
    //using Serialized Properties.
    class GUIStyleHolder : ScriptableObject
    {
        public GUIStyle inspectedStyle;

        protected GUIStyleHolder() {}
    }

    class StyleDrawInspectView : BaseInspectView
    {
        [Serializable]
        class CachedInstructionInfo
        {
            public SerializedObject        styleContainerSerializedObject = null;
            public SerializedProperty      styleSerializedProperty = null;
            public readonly GUIStyleHolder styleContainer;

            public CachedInstructionInfo()
            {
                styleContainer = ScriptableObject.CreateInstance<GUIStyleHolder>();
            }
        }

        Vector2 m_StacktraceScrollPos =  new Vector2();

        [NonSerialized] List<IMGUIDrawInstruction> m_Instructions = new List<IMGUIDrawInstruction>();
        [NonSerialized] IMGUIDrawInstruction m_Instruction;
        [NonSerialized] CachedInstructionInfo m_CachedInstructionInfo;

        public StyleDrawInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
        }

        public override void UpdateInstructions()
        {
            m_Instructions.Clear();
            GUIViewDebuggerHelper.GetDrawInstructions(m_Instructions);
        }

        public override void ClearRowSelection()
        {
            base.ClearRowSelection();
            m_CachedInstructionInfo = null;
        }

        public override void ShowOverlay()
        {
            if (m_CachedInstructionInfo != null)
                debuggerWindow.HighlightInstruction(debuggerWindow.inspected, m_Instruction.rect, m_Instruction.usedGUIStyle);
        }

        protected override int GetInstructionCount()
        {
            return m_Instructions.Count;
        }

        protected override void DoDrawInstruction(ListViewElement el, int id)
        {
            string listDisplayName = GetInstructionListName(el.row);
            GUIContent tempContent = GUIContent.Temp(listDisplayName);

            GUIViewDebuggerWindow.Styles.listItemBackground.Draw(el.position, false, false, listViewState.row == el.row, false);

            GUIViewDebuggerWindow.Styles.listItem.Draw(el.position, tempContent, id, listViewState.row == el.row);
        }

        protected override void DrawInspectedStacktrace()
        {
            m_StacktraceScrollPos = EditorGUILayout.BeginScrollView(m_StacktraceScrollPos, GUIViewDebuggerWindow.Styles.stacktraceBackground, GUILayout.ExpandHeight(false));
            DrawStackFrameList(m_Instruction.stackframes);
            EditorGUILayout.EndScrollView();
        }

        protected override bool isInstructionSelected { get { return m_CachedInstructionInfo != null; } }

        internal override void DoDrawSelectedInstructionDetails(int selectedInstructionIndex)
        {
            using (new EditorGUI.DisabledScope(true))
                DrawInspectedRect(m_Instruction.rect);

            DoSelectableInstructionDataField("VisibleRect", m_Instruction.visibleRect.ToString());
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CachedInstructionInfo.styleSerializedProperty, GUIContent.Temp("Style"), true);
            if (EditorGUI.EndChangeCheck())
            {
                m_CachedInstructionInfo.styleContainerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                debuggerWindow.inspected.Repaint();
            }

            GUILayout.Label(GUIContent.Temp("GUIContent"));
            using (new EditorGUI.IndentLevelScope())
            {
                DoSelectableInstructionDataField("Text", m_Instruction.usedGUIContent.text);
                DoSelectableInstructionDataField("Tooltip", m_Instruction.usedGUIContent.tooltip);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Icon", m_Instruction.usedGUIContent.image, typeof(Texture2D), false);
            }
        }

        internal override string GetInstructionListName(int index)
        {
            //This means we will resolve the stack trace for all instructions.
            //TODO: make sure only visible items do this. Also, cache so we don't have to do everyframe.
            var methodName = GetInstructionListName(m_Instructions[index].stackframes);

            //TODO: use the signature we get from the managed stack
            return string.Format("{0}. {1}", index, methodName);
        }

        string GetInstructionListName(StackFrame[] stacktrace)
        {
            int frameIndex = GetInterestingFrameIndex(stacktrace);

            if (frameIndex > 0)
                --frameIndex;

            StackFrame interestingFrame = stacktrace[frameIndex];
            return interestingFrame.methodName;
        }

        internal override void OnDoubleClickInstruction(int index)
        {
            ShowInstructionInExternalEditor(m_Instructions[index].stackframes);
        }

        internal override void OnSelectedInstructionChanged(int index)
        {
            listViewState.row = index;

            if (listViewState.row >= 0)
            {
                if (m_CachedInstructionInfo == null)
                {
                    m_CachedInstructionInfo = new CachedInstructionInfo();
                }

                m_Instruction = m_Instructions[listViewState.row];

                //updated Cached data related to the Selected Instruction
                m_CachedInstructionInfo.styleContainer.inspectedStyle = m_Instruction.usedGUIStyle;
                m_CachedInstructionInfo.styleContainerSerializedObject = null;
                m_CachedInstructionInfo.styleSerializedProperty = null;
                GetSelectedStyleProperty(out m_CachedInstructionInfo.styleContainerSerializedObject, out m_CachedInstructionInfo.styleSerializedProperty);

                //Hightlight the item
                debuggerWindow.HighlightInstruction(debuggerWindow.inspected, m_Instruction.rect, m_Instruction.usedGUIStyle);
            }
            else
            {
                m_CachedInstructionInfo = null;

                debuggerWindow.ClearInstructionHighlighter();
            }
        }

        int GetInterestingFrameIndex(StackFrame[] stacktrace)
        {
            //We try to find the first frame that belongs to the user project.
            //If there is no frame inside the user project, we will return the first frame outside any class starting with:
            //UnityEngine.GUI or UnityEditor.EditorGUI, this should include:
            // - UnityEngine.GUIStyle
            // - UnityEngine.GUILayout
            // - UnityEngine.GUI
            // - UnityEditor.EditorGUI
            // - UnityEditor.EditorGUILayout
            string currentProjectPath = Application.dataPath;

            int index = -1;

            for (int i = 0; i < stacktrace.Length; ++i)
            {
                StackFrame sf = stacktrace[i];
                if (string.IsNullOrEmpty(sf.sourceFile))
                    continue;
                if (sf.signature.StartsWith("UnityEngine.GUI"))
                    continue;
                if (sf.signature.StartsWith("UnityEditor.EditorGUI"))
                    continue;

                if (index == -1)
                    index = i;

                if (sf.sourceFile.StartsWith(currentProjectPath))
                    return i;
            }

            if (index != -1)
                return index;

            return stacktrace.Length - 1;
        }

        void GetSelectedStyleProperty(out SerializedObject serializedObject, out SerializedProperty styleProperty)
        {
            //GUISkin[] guiskins = FindObjectsOfType<GUISkin>();
            GUISkin guiskin = null;
            //foreach (GUISkin gs in guiskins)
            var gs = GUISkin.current;
            {
                GUIStyle style = gs.FindStyle(m_Instruction.usedGUIStyle.name);
                if (style != null && style == m_Instruction.usedGUIStyle)
                {
                    guiskin = gs;
                    //break;
                }
            }
            styleProperty = null;

            if (guiskin != null)
            {
                serializedObject = new SerializedObject(guiskin);
                SerializedProperty property = serializedObject.GetIterator();
                bool expanded = true;
                while (property.NextVisible(expanded))
                {
                    if (property.type == "GUIStyle")
                    {
                        expanded = false;
                        SerializedProperty spt = property.FindPropertyRelative("m_Name");
                        if (spt.stringValue == m_Instruction.usedGUIStyle.name)
                        {
                            styleProperty = property;
                            return;
                        }
                    }
                    else
                    {
                        expanded = true;
                    }

                    //EditorGUILayout.PropertyField(property, true);
                }
                Debug.Log(string.Format("Showing editable Style from GUISkin: {0}, IsPersistant: {1}", guiskin.name, EditorUtility.IsPersistent(guiskin)));
            }

            //Could not find the styles in the GUISkin.
            serializedObject = new SerializedObject(m_CachedInstructionInfo.styleContainer);
            styleProperty = serializedObject.FindProperty("inspectedStyle");
        }

        void ShowInstructionInExternalEditor(StackFrame[] frames)
        {
            int frameIndex = GetInterestingFrameIndex(frames);
            StackFrame frame = frames[frameIndex];

            InternalEditorUtility.OpenFileAtLineExternal(frame.sourceFile, (int)frame.lineNumber);
        }
    }
}
