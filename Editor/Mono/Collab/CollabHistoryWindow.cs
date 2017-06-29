// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// dependencies

using UnityEngine;
using UnityEditor.Collaboration;
using UnityEditor.Web;
using UnityEditor.Connect;

// namespacing
namespace UnityEditor
{
    internal class CollabHistoryWindow : WebViewEditorWindowTabs, IHasCustomMenu
    {
        private const string kServiceName = "Collab History";


        protected CollabHistoryWindow() : base()
        {
            minSize = new Vector2(275, 50);
        }

        [MenuItem("Window/Collab History", false, 2011)]
        public static CollabHistoryWindow ShowHistoryWindow()
        {
            //Create a new window if it do not exist
            return GetWindow<CollabHistoryWindow>(kServiceName, typeof(UnityEditor.InspectorWindow)) as CollabHistoryWindow;
        }

        [MenuItem("Window/Collab History", true)]
        public static bool ValidateShowHistoryWindow()
        {
            return Collab.instance.IsCollabEnabledForCurrentProject();
        }

        // Receives HTML title
        public void OnReceiveTitle(string title)
        {
            titleContent.text = title;
        }

        public new void OnInitScripting()
        {
            base.OnInitScripting();
        }

        public override void OnEnable()
        {
            Collab.instance.StateChanged += this.OnCollabStateChanged;

            initialOpenUrl = "file:///" + EditorApplication.userJavascriptPackagesPath + "unityeditor-collab-history/dist/index.html";
            base.OnEnable();
        }

        public new void OnDestroy()
        {
            Collab.instance.StateChanged -= this.OnCollabStateChanged;
            base.OnDestroy();
        }

        public void OnCollabStateChanged(CollabInfo info)
        {
            if (!Collab.instance.IsCollabEnabledForCurrentProject())
            {
                CollabHistoryWindow.CloseHistoryWindows();
            }
        }

        public new void ToggleMaximize()
        {
            base.ToggleMaximize();
        }

        private static void CloseHistoryWindows()
        {
            var wins = Resources.FindObjectsOfTypeAll(typeof(CollabHistoryWindow)) as CollabHistoryWindow[];
            if (wins != null)
            {
                foreach (CollabHistoryWindow w in wins)
                    w.Close();
            }
        }
    }
}
