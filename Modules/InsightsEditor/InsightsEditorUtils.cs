// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityEditor.InsightsEditor;

[VisibleToOtherModules]
internal class InsightsEditorUtils
{
	public const string k_ProjectSettingsInfoBoxNodeName = "insights-project-settings-info";
	
    public const string k_InsightsStyleSheetPath = "StyleSheets/ServicesWindow/InsightsSettings.uss";

    public const string k_UssClass_LinkCursor = "link-cursor";

    public static event Action<bool> OnEngineDiagnosticsEnabledChanged;

    public static void NotifyEngineDiagnosticsSettingsChanged(bool enabled)
    {
        OnEngineDiagnosticsEnabledChanged?.Invoke(enabled);
    }

    public static void OnLabelLinkPointerOver(PointerOverLinkTagEvent evt) =>
        ((VisualElement)evt.target).AddToClassList(k_UssClass_LinkCursor);


    public static void OnLabelLinkPointerOut(PointerOutLinkTagEvent evt) =>
        ((VisualElement)evt.target).RemoveFromClassList(k_UssClass_LinkCursor);

    public static void RegisterLinkTagEventCallbacks(VisualElement node,
        EventCallback<PointerDownLinkTagEvent> onLinkClicked,
        EventCallback<PointerOverLinkTagEvent> onLabelLinkPointerOver,
        EventCallback<PointerOutLinkTagEvent> onLabelLinkPointerOut)
    {
        node.RegisterCallback(onLinkClicked);
        node.RegisterCallback(onLabelLinkPointerOver);
        node.RegisterCallback(onLabelLinkPointerOut);
    }
}
