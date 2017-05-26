// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Connect;
using UnityEditor.Web;
using UnityEngine;
using System.Diagnostics;
using System.IO;

internal class TroubleshooterWindow : WebViewEditorWindow, IHasCustomMenu
{
    private WebView m_WebView;

    protected TroubleshooterWindow() : base()
    {
        m_InitialOpenURL = "https://bugservices.unity3d.com/troubleshooter/";
    }

    public new void OnInitScripting()
    {
        base.OnInitScripting();
    }

    internal override WebView webView
    {
        get { return m_WebView; }
        set { m_WebView = value; }
    }

    [MenuItem("Help/Troubleshoot Issue...")]
    public static void RunTroubleshooter()
    {
        TroubleshooterWindow window = EditorWindow.GetWindowWithRect<TroubleshooterWindow>(new Rect(100, 100, 990, 680), true, "Troubleshooter") as TroubleshooterWindow;
        if (window != null)
        {
            window.ShowUtility();
        }
    }
}
