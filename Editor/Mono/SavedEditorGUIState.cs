// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: IMGUIFramework not yet converted
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    // Managed counterpart of SavedGUIState for the per-pass EditorGUI state that
    // EditorGUIUtility.ResetGUIState wipes, so that an OnGUI pass dispatched while another
    // pass is running does not corrupt the state of the pass it interrupts (UUM-148902).
    internal struct SavedEditorGUIState
    {
        internal PropertyGUIData[] propertyStack;
        internal bool[] enabledStack;
        internal bool[] changedStack;
        internal (bool insideList, int depth)[] isInsideListStack;
        internal bool isInsideList;
        internal bool showMixedValue;
        internal int foldoutHeaderGroupActive;
        internal int indentLevel;
        internal object materialPropertyStack;
        internal PropertyDrawer[] drawerStack;
        internal PropertyHandlerCache propertyHandlerCache;
        internal float labelWidth;
        internal float fieldWidth;
        internal float overriddenViewWidth;
        internal bool boldDefaultFont;
        internal float[] contextWidthStack;
        internal bool hierarchyMode;
        internal bool wideMode;
        internal EditorGUIUtility.ComparisonViewMode comparisonViewMode;
        internal float leftMarginCoord;
        private bool captured;

        internal static SavedEditorGUIState Capture()
        {
            var state = new SavedEditorGUIState();
            EditorGUI.CaptureReentrantState(ref state);
            EditorGUIUtility.CaptureReentrantState(ref state);
            state.materialPropertyStack = MaterialProperty.CaptureReentrantStack();
            state.drawerStack = ScriptAttributeUtility.CaptureReentrantDrawerStack();
            state.captured = true;

            // The interrupting pass starts from the same clean slate ResetGUIState provides.
            EditorGUIUtility.ResetPerPassState();
            return state;
        }

        internal void Apply()
        {
            if (!captured)
                return;

            EditorGUI.RestoreReentrantState(in this);
            EditorGUIUtility.RestoreReentrantState(in this);
            MaterialProperty.RestoreReentrantStack(materialPropertyStack);
            ScriptAttributeUtility.RestoreReentrantDrawerStack(drawerStack);
        }

        internal static void RestoreStack<T>(Stack<T> stack, T[] saved)
        {
            stack.Clear();
            for (int i = saved.Length - 1; i >= 0; i--)
                stack.Push(saved[i]);
        }
    }

    public sealed partial class EditorGUI
    {
        internal static void CaptureReentrantState(ref SavedEditorGUIState state)
        {
            state.propertyStack = s_PropertyStack.ToArray();
            state.enabledStack = s_EnabledStack.ToArray();
            state.changedStack = s_ChangedStack.ToArray();
            state.isInsideListStack = s_IsInsideListStack.ToArray();
            state.isInsideList = GUI.isInsideList;
            state.showMixedValue = showMixedValue;
            state.foldoutHeaderGroupActive = s_FoldoutHeaderGroupActive;
            state.indentLevel = ms_IndentLevel;

            // Not part of ResetPerPassState: EndProperty in the interrupting pass would leave the
            // suspended multi-object property scope reading stale mixed-value state.
            showMixedValue = false;
        }

        internal static void RestoreReentrantState(in SavedEditorGUIState state)
        {
            SavedEditorGUIState.RestoreStack(s_PropertyStack, state.propertyStack);
            SavedEditorGUIState.RestoreStack(s_EnabledStack, state.enabledStack);
            SavedEditorGUIState.RestoreStack(s_ChangedStack, state.changedStack);
            SavedEditorGUIState.RestoreStack(s_IsInsideListStack, state.isInsideListStack);
            GUI.isInsideList = state.isInsideList;
            showMixedValue = state.showMixedValue;
            s_FoldoutHeaderGroupActive = state.foldoutHeaderGroupActive;
            ms_IndentLevel = state.indentLevel;
        }
    }

    public sealed partial class EditorGUIUtility
    {
        internal static void CaptureReentrantState(ref SavedEditorGUIState state)
        {
            state.labelWidth = s_LabelWidth;
            state.fieldWidth = s_FieldWidth;
            state.overriddenViewWidth = s_OverriddenViewWidth;
            state.boldDefaultFont = GetBoldDefaultFont();
            state.contextWidthStack = s_ContextWidthStack.ToArray();
            state.hierarchyMode = hierarchyMode;
            state.wideMode = wideMode;
            state.comparisonViewMode = comparisonViewMode;
            state.leftMarginCoord = leftMarginCoord;
            state.propertyHandlerCache = ScriptAttributeUtility.CaptureReentrantHandlerCache();

            // ResetPerPassState only pops one lock; the interrupting pass must not inherit the rest.
            s_ContextWidthStack.Clear();
        }

        internal static void RestoreReentrantState(in SavedEditorGUIState state)
        {
            s_LabelWidth = state.labelWidth;
            s_FieldWidth = state.fieldWidth;
            s_OverriddenViewWidth = state.overriddenViewWidth;
            SetBoldDefaultFont(state.boldDefaultFont);
            SavedEditorGUIState.RestoreStack(s_ContextWidthStack, state.contextWidthStack);
            hierarchyMode = state.hierarchyMode;
            wideMode = state.wideMode;
            comparisonViewMode = state.comparisonViewMode;
            leftMarginCoord = state.leftMarginCoord;
            ScriptAttributeUtility.RestoreReentrantHandlerCache(state.propertyHandlerCache);
        }
    }

    public sealed partial class MaterialProperty
    {
        internal static object CaptureReentrantStack()
        {
            return s_PropertyStack.ToArray();
        }

        internal static void RestoreReentrantStack(object saved)
        {
            s_PropertyStack.Clear();
            s_PropertyStack.AddRange((PropertyData[])saved);
        }
    }

    internal partial class ScriptAttributeUtility
    {
        internal static PropertyDrawer[] CaptureReentrantDrawerStack()
        {
            return s_DrawerStack.ToArray();
        }

        internal static void RestoreReentrantDrawerStack(PropertyDrawer[] saved)
        {
            SavedEditorGUIState.RestoreStack(s_DrawerStack, saved);
        }

        internal static PropertyHandlerCache CaptureReentrantHandlerCache()
        {
            return s_CurrentCache;
        }

        internal static void RestoreReentrantHandlerCache(PropertyHandlerCache cache)
        {
            s_CurrentCache = cache;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
