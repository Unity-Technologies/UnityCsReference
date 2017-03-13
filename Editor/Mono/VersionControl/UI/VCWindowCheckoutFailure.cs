// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal.VersionControl;

namespace UnityEditor.VersionControl
{
    // Window allowing you to review files that could not be checked out.
    internal class WindowCheckoutFailure : EditorWindow
    {
        private AssetList assetList = new AssetList();
        private ListControl checkoutSuccessList = new ListControl();
        private ListControl checkoutFailureList = new ListControl();

        public void OnEnable()
        {
            position = new Rect(100, 100, 700, 230);
            minSize = new Vector2(700, 230);

            checkoutSuccessList.ReadOnly = true;
            checkoutFailureList.ReadOnly = true;
        }

        public static void OpenIfCheckoutFailed(AssetList assets)
        {
            Object[] windows = Resources.FindObjectsOfTypeAll(typeof(WindowCheckoutFailure));
            WindowCheckoutFailure window = (windows.Length > 0 ? windows[0] as WindowCheckoutFailure : null);

            bool alreadyOpen = (window != null);
            bool shouldOpen = alreadyOpen;

            if (!shouldOpen)
            {
                foreach (var asset in assets)
                {
                    if (!asset.IsState(Asset.States.CheckedOutLocal))
                    {
                        shouldOpen = true;
                        break;
                    }
                }
            }

            if (shouldOpen)
                GetWindow().DoOpen(assets, alreadyOpen);
        }

        private static WindowCheckoutFailure GetWindow()
        {
            return EditorWindow.GetWindow<WindowCheckoutFailure>(true, "Version Control Check Out Failed");
        }

        private void DoOpen(AssetList assets, bool alreadyOpen)
        {
            if (alreadyOpen)
            {
                foreach (var asset in assets)
                {
                    bool found = false;
                    int count = assetList.Count;

                    for (int i = 0; i < count; i++)
                    {
                        if (assetList[i].path == asset.path)
                        {
                            found = true;
                            assetList[i] = asset;
                            break;
                        }
                    }

                    if (!found)
                        assetList.Add(asset);
                }
            }
            else
                assetList.AddRange(assets);

            RefreshList();
        }

        private void RefreshList()
        {
            checkoutSuccessList.Clear();
            checkoutFailureList.Clear();

            foreach (var asset in assetList)
            {
                if (asset.IsState(Asset.States.CheckedOutLocal))
                    checkoutSuccessList.Add(null, asset.prettyPath, asset);
                else
                    checkoutFailureList.Add(null, asset.prettyPath, asset);
            }

            checkoutSuccessList.Refresh();
            checkoutFailureList.Refresh();

            Repaint();
        }

        public void OnGUI()
        {
            float h = (position.height - 122) / 2;

            // TODO: Show the reason: Who has the exclusive lock?
            GUILayout.Label("Some files could not be checked out:", EditorStyles.boldLabel);

            Rect r1 = new Rect(6, 40, position.width - 12, h);
            GUILayout.BeginArea(r1);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
            checkoutFailureList.OnGUI(new Rect(r1.x + 2, r1.y + 2, r1.width - 4, r1.height - 4), true);

            GUILayout.Space(20 + h);
            GUILayout.Label("The following files were successfully checked out:", EditorStyles.boldLabel);

            Rect r2 = new Rect(6, 40 + h + 40, position.width - 12, h);
            GUILayout.BeginArea(r2);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
            checkoutSuccessList.OnGUI(new Rect(r2.x + 2, r2.y + 2, r2.width - 4, r2.height - 4), true);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            EditorUserSettings.showFailedCheckout = !GUILayout.Toggle(!EditorUserSettings.showFailedCheckout, "Don't show this window again.");

            GUILayout.FlexibleSpace();

            bool enabled = GUI.enabled;
            GUI.enabled = checkoutFailureList.Size > 0;

            if (GUILayout.Button("Retry Check Out"))
                Provider.Checkout(assetList, CheckoutMode.Exact);

            GUI.enabled = checkoutSuccessList.Size > 0;

            if (GUILayout.Button("Revert Unchanged"))
            {
                Provider.Revert(assetList, RevertMode.Unchanged).SetCompletionAction(CompletionAction.UpdatePendingWindow);
                Provider.Status(assetList);
                Close();
            }

            GUI.enabled = enabled;

            if (GUILayout.Button("OK"))
                Close();

            GUILayout.EndHorizontal();
            GUILayout.Space(12);
        }
    }
}
