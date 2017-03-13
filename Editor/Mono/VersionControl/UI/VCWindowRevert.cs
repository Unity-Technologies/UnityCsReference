// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditorInternal.VersionControl;

namespace UnityEditor.VersionControl
{
    // Window allowing you to review files who will be reverted.  As this may be throwing changes away this
    // window gives you the opportunty to see what will change and if required the user can apply or cancel.
    internal class WindowRevert : EditorWindow
    {
        ListControl revertList = new ListControl();
        AssetList assetList = new AssetList();

        public void OnEnable()
        {
            position = new Rect(100, 100, 700, 230);
            minSize = new Vector2(700, 230);

            revertList.ReadOnly = true;
        }

        // Revert all files within a change list
        static public void Open(ChangeSet change)
        {
            Task task = Provider.ChangeSetStatus(change);
            task.Wait();

            GetWindow().DoOpen(task.assetList);
        }

        // Revert a list of files
        static public void Open(AssetList assets)
        {
            Task task = Provider.Status(assets);
            task.Wait();

            const bool includeFolders = true;
            AssetList revert = task.assetList.Filter(includeFolders,
                    Asset.States.CheckedOutLocal,
                    Asset.States.DeletedLocal,
                    Asset.States.AddedLocal,
                    //Asset.States.Branch,
                    //Asset.States.Integrate,
                    Asset.States.Missing);

            GetWindow().DoOpen(revert);
        }

        static private WindowRevert GetWindow()
        {
            return EditorWindow.GetWindow<WindowRevert>(true, "Version Control Revert");
        }

        void DoOpen(AssetList revert)
        {
            assetList = revert;
            RefreshList();
        }

        void RefreshList()
        {
            revertList.Clear();

            foreach (Asset it in assetList)
                revertList.Add(null, it.prettyPath, it);

            // Show a dummy entry if there is nothing to do
            if (assetList.Count == 0)
            {
                ChangeSet change = new ChangeSet("no files to revert");
                ListItem item = revertList.Add(null, change.description, change);
                item.Dummy = true;
            }

            revertList.Refresh();
            Repaint();
        }

        void OnGUI()
        {
            GUILayout.Label("Revert Files", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // I would use GUIUtility.GetLastRect() here after the box but that seems to have wierd side effects.
            Rect r1 = new Rect(6, 40, position.width - 12, position.height - 82);
            GUILayout.BeginArea(r1);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
            revertList.OnGUI(new Rect(r1.x + 2, r1.y + 2, r1.width - 4, r1.height - 4), true);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel"))
                Close();

            if (assetList.Count > 0 && GUILayout.Button("Revert"))
            {
                string openScenes = "";
                foreach (Asset a in assetList)
                {
                    Scene openScene = SceneManager.GetSceneByPath(a.path);
                    if ((openScene.IsValid()) && (openScene.isLoaded))
                    {
                        openScenes += openScene.path + "\n";
                    }
                }

                if (openScenes.Length > 0)
                {
                    bool continueRevert = EditorUtility.DisplayDialog("Revert open scene(s)?",
                            "You are about to revert your currently open scene(s):\n\n" +
                            openScenes + "\nContinuing will remove all unsaved changes.",
                            "Continue", "Cancel");
                    if (!continueRevert)
                    {
                        Close();
                        return;
                    }
                }

                Provider.Revert(assetList, RevertMode.Normal).Wait();
                WindowPending.UpdateAllWindows();
                AssetDatabase.Refresh();
                Close();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12);
        }
    }
}
