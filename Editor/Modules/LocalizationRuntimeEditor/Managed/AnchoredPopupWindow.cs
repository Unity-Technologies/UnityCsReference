// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

/// <summary>A dropdown window that closes with the view it was opened from, since it is not part of that view's hierarchy.</summary>
abstract class AnchoredPopupWindow : EditorWindow
{
    Action m_Closed;

    /// <summary>Shows this window below <paramref name="activatorWorldBound"/>, closing it again when <paramref name="view"/> leaves its panel.</summary>
    protected void ShowUnder(Rect activatorWorldBound, VisualElement view, Vector2 size)
    {
        void CloseWithView(DetachFromPanelEvent _) => Close();

        view.RegisterCallback<DetachFromPanelEvent>(CloseWithView);
        // A reload drops the callbacks the dropdown was built with, so it cannot usefully survive one.
        AssemblyReloadEvents.beforeAssemblyReload += Close;
        m_Closed = () =>
        {
            view.UnregisterCallback<DetachFromPanelEvent>(CloseWithView);
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
        };
        ShowAsDropDown(GUIUtility.GUIToScreenRect(activatorWorldBound), size);
    }

    protected virtual void OnDisable()
    {
        m_Closed?.Invoke();
        m_Closed = null;
    }
}
