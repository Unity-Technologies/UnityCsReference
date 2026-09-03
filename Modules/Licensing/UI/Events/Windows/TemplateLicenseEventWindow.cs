// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: License not yet converted
using UnityEditor.Licensing.UI.Data.Events.Base;
using UnityEditor.Licensing.UI.Helper;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Licensing.UI.Events.Windows;

abstract class TemplateLicenseEventWindow : EditorWindow
{
    #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
    protected TemplateLicenseEventWindow() { }
    #pragma warning restore UAL0015

    [NoAutoStaticsCleanup] // injected dependency; set during window init
    protected static INativeApiWrapper s_NativeApiWrapper;
    [NoAutoStaticsCleanup] // injected dependency; set during window init
    protected static ILicenseLogger s_LicenseLogger;

    [NoAutoStaticsCleanup] // injected dependency; set during window init
    protected static Notification s_Notification;

    [NoAutoStaticsCleanup] // injected dependency; set during window init
    protected static TemplateLicenseEventWindowContents s_Root;

    // UX designer fixes the width to preference
    const float k_ContentWidth = 334f;

    protected static void ShowWindow<T>(string txtWindowTitle, bool enableWindowTrapping = false) where T : EditorWindow
    {
        // trap user until user clicks on a button and not 'x' in the title bar
        // trapping is only required if there is no more ui entitlement
        while (Render<T>(txtWindowTitle) && !s_NativeApiWrapper.HasUiEntitlement())
        {
            if (!enableWindowTrapping)
            {
                break;
            }
        }
    }

    static bool Render<T>(string txtWindowTitle) where T : EditorWindow
    {
        var window = CreateInstance<T>();

        window.titleContent = new GUIContent(txtWindowTitle);

        window.ShowModal();

        // returns true: if and only if user has clicked on 'x' icon in titlebar
        return s_Root.UserClosedModalFromTitleBar;
    }

    public void OnEnable()
    {
        rootVisualElement.RegisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
    }

    void GeometryChangedCallback(GeometryChangedEvent evt)
    {
        // set the size of the window according to the content description and elements
        var mainContainer = rootVisualElement.Q<VisualElement>("MainContainer");

        if (mainContainer == null)
        {
            return;
        }

        minSize = new Vector2(
            k_ContentWidth,
            mainContainer.resolvedStyle.height + 1 // for some reason this 1 pixel fixes the resizing glitch
        );
        rootVisualElement.schedule.Execute(() => { maxSize = minSize; });
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
