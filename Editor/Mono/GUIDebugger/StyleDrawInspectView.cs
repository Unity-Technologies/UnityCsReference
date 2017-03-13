// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    //The only purpose of this class is to enable us to view the used GUIStyle
    //using Serialized Properties.
    class GUIStyleHolder : ScriptableObject
    {
        public GUIStyle inspectedStyle;
    }

    class StyleDrawInspectView : BaseInspectView
    {
        class GUIInstruction
        {
            public Rect         rect;
            public GUIStyle     usedGUIStyle = GUIStyle.none;
            public GUIContent   usedGUIContent = GUIContent.none;
            public StackFrame[] stackframes;

            public void Reset()
            {
                rect = new Rect();
                usedGUIStyle = GUIStyle.none;
                usedGUIContent = GUIContent.none;
            }
        }

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

        [NonSerialized] private GUIInstruction m_Instruction;
        [NonSerialized] private CachedInstructionInfo m_CachedinstructionInfo;
        private Vector2 m_StacktraceScrollPos =  new Vector2();


        public StyleDrawInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
        }

        public override void UpdateInstructions()
        {
        }

        protected override int GetInstructionCount()
        {
            return GUIViewDebuggerHelper.GetInstructionCount();
        }

        protected override void DoDrawInstruction(ListViewElement el, int id)
        {
            string listDisplayName = GetInstructionListName(el.row);
            GUIContent tempContent = GUIContent.Temp(listDisplayName);

            GUIViewDebuggerWindow.s_Styles.listItemBackground.Draw(el.position, false, false, m_ListViewState.row == el.row, false);

            GUIViewDebuggerWindow.s_Styles.listItem.Draw(el.position, tempContent, id, m_ListViewState.row == el.row);
        }

        internal override void OnDoubleClickInstruction(int index)
        {
            ShowInstructionInExternalEditor(GUIViewDebuggerHelper.GetManagedStackTrace(index));
        }

        private int GetInterestingFrameIndex(StackFrame[] stacktrace)
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

        internal override void DoDrawSelectedInstructionDetails(int index)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                DrawInspectedRect(m_Instruction.rect);
            }

            DrawInspectedStyle();

            using (new EditorGUI.DisabledScope(true))
            {
                DrawInspectedGUIContent();
            }
        }

        protected override bool HasSelectedinstruction()
        {
            return m_Instruction != null;
        }

        private void DrawInspectedGUIContent()
        {
            GUILayout.Label(GUIContent.Temp("GUIContent"));
            EditorGUI.indentLevel++;
            EditorGUILayout.TextField(m_Instruction.usedGUIContent.text);
            EditorGUILayout.ObjectField(m_Instruction.usedGUIContent.image, typeof(Texture2D), false);
            EditorGUI.indentLevel--;
        }

        private void DrawInspectedStyle()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_CachedinstructionInfo.styleSerializedProperty, GUIContent.Temp("Style"), true);
            if (EditorGUI.EndChangeCheck())
            {
                m_CachedinstructionInfo.styleContainerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                m_GuiViewDebuggerWindow.m_Inspected.Repaint();
            }
        }

        protected override void DrawInspectedStacktrace()
        {
            m_StacktraceScrollPos = EditorGUILayout.BeginScrollView(m_StacktraceScrollPos, GUIViewDebuggerWindow.s_Styles.stacktraceBackground, GUILayout.ExpandHeight(false));
            DrawStackFrameList(m_Instruction.stackframes);
            EditorGUILayout.EndScrollView();
        }

        public void GetSelectedStyleProperty(out SerializedObject serializedObject, out SerializedProperty styleProperty)
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
            serializedObject = new SerializedObject(m_CachedinstructionInfo.styleContainer);
            styleProperty = serializedObject.FindProperty("inspectedStyle");
        }

        internal override void OnSelectedInstructionChanged(int index)
        {
            m_ListViewState.row = index;

            if (m_ListViewState.row >= 0)
            {
                if (m_Instruction == null)
                    m_Instruction = new GUIInstruction();

                if (m_CachedinstructionInfo == null)
                    m_CachedinstructionInfo = new CachedInstructionInfo();


                //TODO: instead of calling multiple functions, do this properly!
                m_Instruction.rect = GUIViewDebuggerHelper.GetRectFromInstruction(m_ListViewState.row);
                m_Instruction.usedGUIStyle = GUIViewDebuggerHelper.GetStyleFromInstruction(m_ListViewState.row);
                m_Instruction.usedGUIContent = GUIViewDebuggerHelper.GetContentFromInstruction(m_ListViewState.row);
                m_Instruction.stackframes = GUIViewDebuggerHelper.GetManagedStackTrace(m_ListViewState.row);

                //updated Cached data related to the Selected Instruction
                m_CachedinstructionInfo.styleContainer.inspectedStyle = m_Instruction.usedGUIStyle;
                m_CachedinstructionInfo.styleContainerSerializedObject = null;
                m_CachedinstructionInfo.styleSerializedProperty = null;
                GetSelectedStyleProperty(out m_CachedinstructionInfo.styleContainerSerializedObject, out m_CachedinstructionInfo.styleSerializedProperty);

                //Hightlight the item
                m_GuiViewDebuggerWindow.HighlightInstruction(m_GuiViewDebuggerWindow.m_Inspected, m_Instruction.rect, m_Instruction.usedGUIStyle);
            }
            else
            {
                m_Instruction = null;
                m_CachedinstructionInfo = null;

                if (m_GuiViewDebuggerWindow.InstructionOverlayWindow != null)
                    m_GuiViewDebuggerWindow.InstructionOverlayWindow.Close();
            }
        }

        private void ShowInstructionInExternalEditor(StackFrame[] frames)
        {
            int frameIndex = GetInterestingFrameIndex(frames);
            StackFrame frame = frames[frameIndex];

            InternalEditorUtility.OpenFileAtLineExternal(frame.sourceFile, (int)frame.lineNumber);
        }

        internal override string GetInstructionListName(int index)
        {
            //This means we will resolve the stack trace for all instructions.
            //TODO: make sure only visible items do this. Also, cache so we don't have to do everyframe.
            StackFrame[] stacktrace = GUIViewDebuggerHelper.GetManagedStackTrace(index);
            var methodName = GetInstructionListName(stacktrace);


            //TODO: use the signature we get from the managed stack
            return string.Format("{0}. {1}", index, methodName);
        }

        protected string GetInstructionListName(StackFrame[] stacktrace)
        {
            int frameIndex = GetInterestingFrameIndex(stacktrace);

            if (frameIndex > 0)
                --frameIndex;

            StackFrame interestingFrame = stacktrace[frameIndex];
            string methodName = interestingFrame.methodName;
            return methodName;
        }

        public override void Unselect()
        {
            base.Unselect();
            m_Instruction = null;
        }

        public override void ShowOverlay()
        {
            if (m_Instruction != null)
                m_GuiViewDebuggerWindow.HighlightInstruction(m_GuiViewDebuggerWindow.m_Inspected, m_Instruction.rect, m_Instruction.usedGUIStyle);
        }
    }
}
