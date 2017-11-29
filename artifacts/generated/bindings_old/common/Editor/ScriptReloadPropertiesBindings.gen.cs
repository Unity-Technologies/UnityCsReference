// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{


[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct MonoReloadableIntPtr
{
    internal IntPtr  m_IntPtr;
}

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct MonoReloadableIntPtrClear
{
    internal IntPtr  m_IntPtr;
}

internal sealed partial class ScriptReloadProperties : ScriptableObject
{
    
            public bool EditorGUI_IsActuallEditing;
            public int EditorGUI_TextEditor_cursorIndex;
            public int EditorGUI_TextEditor_selectIndex;
            public int EditorGUI_TextEditor_controlID;
            public bool EditorGUI_TextEditor_hasHorizontalCursorPos;
            public Vector2 EditorGUI_TextEditor_scrollOffset;
            public bool EditorGUI_TextEditor_hasFocus;
            public Vector2 EditorGUI_TextEditor_graphicalCursorPos;
            public string EditorGUI_TextEditor_content;
            public string EditorGUI_Current_Editing_String;
    
            public int EditorGUI_DelayedTextEditor_cursorIndex;
            public int EditorGUI_DelayedTextEditor_selectIndex;
            public int EditorGUI_DelayedTextEditor_controlID;
            public bool EditorGUI_DelayedTextEditor_hasHorizontalCursorPos;
            public Vector2 EditorGUI_DelayedTextEditor_scrollOffset;
            public bool EditorGUI_DelayedTextEditor_hasFocus;
            public Vector2 EditorGUI_DelayedTextEditor_graphicalCursorPos;
            public string EditorGUI_DelayedTextEditor_content;
            public string EditorGUI_DelayedControlThatHadFocusValue;
    
    static ScriptReloadProperties Store()
        {
            ScriptReloadProperties obj = ScriptableObject.CreateInstance<ScriptReloadProperties>();
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.ManagedStore();
            return obj;
        }
    
    static void Load(ScriptReloadProperties properties)
        {
            properties.ManagedLoad();
        }
    
    private void ManagedStore()
        {
            EditorGUI_IsActuallEditing = EditorGUI.RecycledTextEditor.s_ActuallyEditing;
            EditorGUI_TextEditor_cursorIndex = EditorGUI.s_RecycledEditor.cursorIndex;
            EditorGUI_TextEditor_selectIndex = EditorGUI.s_RecycledEditor.selectIndex;
            EditorGUI_TextEditor_controlID = EditorGUI.s_RecycledEditor.controlID;
            EditorGUI_TextEditor_hasHorizontalCursorPos = EditorGUI.s_RecycledEditor.hasHorizontalCursorPos;
            EditorGUI_TextEditor_scrollOffset = EditorGUI.s_RecycledEditor.scrollOffset;
            EditorGUI_TextEditor_hasFocus = EditorGUI.s_RecycledEditor.m_HasFocus;
            EditorGUI_TextEditor_graphicalCursorPos = EditorGUI.s_RecycledEditor.graphicalCursorPos;
            EditorGUI_TextEditor_content = EditorGUI.s_RecycledEditor.text;
            EditorGUI_Current_Editing_String = EditorGUI.s_RecycledCurrentEditingString;

            EditorGUI_DelayedTextEditor_cursorIndex = EditorGUI.s_DelayedTextEditor.cursorIndex;
            EditorGUI_DelayedTextEditor_selectIndex = EditorGUI.s_DelayedTextEditor.selectIndex;
            EditorGUI_DelayedTextEditor_controlID = EditorGUI.s_DelayedTextEditor.controlID;
            EditorGUI_DelayedTextEditor_hasHorizontalCursorPos = EditorGUI.s_DelayedTextEditor.hasHorizontalCursorPos;
            EditorGUI_DelayedTextEditor_scrollOffset = EditorGUI.s_DelayedTextEditor.scrollOffset;
            EditorGUI_DelayedTextEditor_hasFocus = EditorGUI.s_DelayedTextEditor.m_HasFocus;
            EditorGUI_DelayedTextEditor_graphicalCursorPos = EditorGUI.s_DelayedTextEditor.graphicalCursorPos;
            EditorGUI_DelayedTextEditor_content = EditorGUI.s_DelayedTextEditor.text;
            EditorGUI_DelayedControlThatHadFocusValue = EditorGUI.s_DelayedTextEditor.controlThatHadFocusValue;
        }
    
    private void ManagedLoad()
        {
            EditorGUI.s_RecycledEditor.text = EditorGUI_TextEditor_content;
            EditorGUI.s_RecycledCurrentEditingString = EditorGUI_Current_Editing_String;
            EditorGUI.RecycledTextEditor.s_ActuallyEditing = EditorGUI_IsActuallEditing;
            EditorGUI.s_RecycledEditor.cursorIndex = EditorGUI_TextEditor_cursorIndex;
            EditorGUI.s_RecycledEditor.selectIndex = EditorGUI_TextEditor_selectIndex;
            EditorGUI.s_RecycledEditor.controlID = EditorGUI_TextEditor_controlID;
            EditorGUI.s_RecycledEditor.hasHorizontalCursorPos = EditorGUI_TextEditor_hasHorizontalCursorPos;
            EditorGUI.s_RecycledEditor.scrollOffset = EditorGUI_TextEditor_scrollOffset;
            EditorGUI.s_RecycledEditor.m_HasFocus = EditorGUI_TextEditor_hasFocus;
            EditorGUI.s_RecycledEditor.graphicalCursorPos = EditorGUI_TextEditor_graphicalCursorPos;


            EditorGUI.s_DelayedTextEditor.text = EditorGUI_DelayedTextEditor_content;
            EditorGUI.s_DelayedTextEditor.cursorIndex = EditorGUI_DelayedTextEditor_cursorIndex;
            EditorGUI.s_DelayedTextEditor.selectIndex = EditorGUI_DelayedTextEditor_selectIndex;
            EditorGUI.s_DelayedTextEditor.controlID = EditorGUI_DelayedTextEditor_controlID;
            EditorGUI.s_DelayedTextEditor.hasHorizontalCursorPos = EditorGUI_DelayedTextEditor_hasHorizontalCursorPos;
            EditorGUI.s_DelayedTextEditor.scrollOffset = EditorGUI_DelayedTextEditor_scrollOffset;
            EditorGUI.s_DelayedTextEditor.m_HasFocus = EditorGUI_DelayedTextEditor_hasFocus;
            EditorGUI.s_DelayedTextEditor.graphicalCursorPos = EditorGUI_DelayedTextEditor_graphicalCursorPos;
            EditorGUI.s_DelayedTextEditor.controlThatHadFocusValue = EditorGUI_DelayedControlThatHadFocusValue;
        }
    
    
}

}
