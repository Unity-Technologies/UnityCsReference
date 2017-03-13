// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal partial class LicenseManagementWindow : EditorWindow
    {
        static private int width = 600;
        static private int height = 350;
        static private int left = 0;
        static private int top = 0;
        static private int offsetX = 50;
        static private int offsetY = 25;
        static private int buttonWidth = 140;

        static private Rect windowArea;
        static private Rect rectArea = new Rect(offsetX, offsetY, width - offsetX * 2, height - offsetY * 2);
        static LicenseManagementWindow win = null;

        private static LicenseManagementWindow Window
        {
            get
            {
                if (win == null)
                    win = EditorWindow.GetWindowWithRect<LicenseManagementWindow>(windowArea, true, "License Management");

                return win;
            }
        }

        static void ShowWindow()
        {
            Resolution res = Screen.currentResolution;
            left = (res.width - width) / 2;
            top = (res.height - height) / 2;
            windowArea = new Rect(left, top, width, height);
            win = Window;
            win.position = windowArea;
            win.Show();
        }

        void OnGUI()
        {
            GUILayout.BeginArea(rectArea);
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Check for updates", GUILayout.ExpandHeight(true), GUILayout.Width(buttonWidth)))
                CheckForUpdates();
            GUI.skin.label.wordWrap = true;
            GUILayout.Label("Checks for updates to the currently installed license. If you have purchased "
                + "addons you can install them by pressing this button (Internet access required)");
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Activate new license", GUILayout.ExpandHeight(true), GUILayout.Width(buttonWidth)))
            {
                ActivateNewLicense();
                Window.Close();
            }
            GUILayout.Label("Activate Unity with a different serial number, switch to a free serial or start "
                + "a trial period if you are eligible for it (Internet access required).");
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Return license", GUILayout.ExpandHeight(true), GUILayout.Width(buttonWidth)))
                ReturnLicense();
            GUILayout.Label("Return this license and free an activation for the serial it is using. You can then"
                + " reuse the activation on another machine (Internet access required).");
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Manual activation", GUILayout.ExpandHeight(true), GUILayout.Width(buttonWidth)))
                ManualActivation();
            GUILayout.Label("Start the manual activation process, you can save this machines license activation "
                + "request file or deploy a license file you have already activated manually.");
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unity FAQ", GUILayout.ExpandHeight(true), GUILayout.Width(buttonWidth)))
                Application.OpenURL("http://unity3d.com/unity/faq");
            GUILayout.Label("Open the Unity FAQ web page, where you can find information about Unity's license system.");
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }
    }
}
