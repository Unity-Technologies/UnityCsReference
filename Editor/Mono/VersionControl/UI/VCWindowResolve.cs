// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal.VersionControl;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VersionControl
{
    // Window allowing you to review files who will be resolved.  As this may be throwing changes away this
    // window gives you the opportunty to see what will change and if required the user can apply or cancel.
    internal class WindowResolve : EditorWindow
    {
        ListControl resolveList = new ListControl();
        AssetList assetList = new AssetList();
        bool cancelled = false;

        // This is required to preserve and be able to restore resolve list after assembly reload that
        // happens after resolving script conflicts
        List<string> assetPaths = new List<string>();

        public void OnEnable()
        {
            DoOpen(assetList);
            position = new Rect(100, 100, 650, 330);
            minSize = new Vector2(650, 330);
        }

        public void OnDisable()
        {
            if (!cancelled)
                WindowPending.UpdateAllWindows();
        }

        // Resolve all files within a change list
        static public void Open(ChangeSet change)
        {
            Task task = Provider.ChangeSetStatus(change);
            task.Wait();
            WindowResolve win = GetWindow();
            win.DoOpen(task.assetList, true);
        }

        // Resolve a list of files
        static public void Open(AssetList assets)
        {
            Task task = Provider.Status(assets);
            task.Wait();
            WindowResolve win = GetWindow();
            win.DoOpen(task.assetList, true);
        }

        static private WindowResolve GetWindow()
        {
            return EditorWindow.GetWindow<WindowResolve>(true, "Version Control Resolve");
        }

        void DoOpen(AssetList resolve, bool selectAll = false)
        {
            if (assetList.Count == 0 && assetPaths.Count != 0)
            {
                assetPaths.ForEach(assetPath => assetList.Add(Provider.CacheStatus(assetPath)));
            }

            bool includeFolders = true;
            assetList = resolve.Filter(includeFolders, Asset.States.Conflicted);
            assetPaths = assetList.Select(asset => asset.path).ToList();
            RefreshList(selectAll);
        }

        void RefreshList(bool selectAll)
        {
            resolveList.Clear();

            assetList.NaturalSort();

            bool first = true;
            foreach (Asset it in assetList)
            {
                ListItem newItem = resolveList.Add(null, it.prettyPath, it);
                if (selectAll)
                {
                    if (first)
                    {
                        resolveList.SelectedSet(newItem);
                        first = false;
                    }
                    else
                    {
                        resolveList.SelectedAdd(newItem);
                    }
                }
            }

            // Show a dummy entry if there is nothing to do
            if (assetList.Count == 0)
            {
                ChangeSet change = new ChangeSet("no files to resolve");
                ListItem item = resolveList.Add(null, change.description, change);
                item.Dummy = true;
            }

            resolveList.Refresh();
            Repaint();
        }

        void SimpleMerge(ResolveMethod resolveMethod)
        {
            AssetList selAssets = resolveList.SelectedAssets;
            Provider.Resolve(selAssets, resolveMethod).Wait();

            DoOpen(assetList);

            if (assetList.Count == 0)
                Close();
        }

        void OnGUI()
        {
            cancelled = false;
            GUILayout.Label("Conflicting files to resolve", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // I would use GUIUtility.GetLastRect() here after the box but that seems to have wierd side effects.
            Rect r1 = new Rect(6, 40, position.width - 12, position.height - 112);  // 82
            GUILayout.BeginArea(r1);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.EndArea();

            bool repaint = resolveList.OnGUI(new Rect(r1.x + 2, r1.y + 2, r1.width - 4, r1.height - 4), true);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUI.enabled = assetList.Count > 0;

            GUILayout.Label("Resolve selection by:");

            using (new EditorGUI.DisabledScope(resolveList.SelectedAssets.Count == 0))
            {
                bool methodWasSelected = false;
                ResolveMethod resolveMethod = ResolveMethod.UseMine;

                if (GUILayout.Button("using local version"))
                {
                    methodWasSelected = true;
                    resolveMethod = ResolveMethod.UseMine;
                }

                if (GUILayout.Button("using incoming version"))
                {
                    methodWasSelected = true;
                    resolveMethod = ResolveMethod.UseTheirs;
                }

                if (GUILayout.Button("merging"))
                {
                    methodWasSelected = true;
                    resolveMethod = ResolveMethod.UseMerged;
                }

                //                        We don't want to prompt the user about saving the current scene when using the local or incoming versions
                if (methodWasSelected && ((resolveMethod != ResolveMethod.UseMerged) || WindowChange.PromptUserToSaveDirtyScenes(assetList)))
                {
                    if (resolveMethod == ResolveMethod.UseMerged)
                    {
                        Task t = Provider.Merge(resolveList.SelectedAssets);
                        t.Wait();

                        if (t.success)
                        {
                            t = Provider.Resolve(t.assetList, ResolveMethod.UseMerged);
                            t.Wait();
                            if (t.success)
                            {
                                // Check that there are not more conflicts for the specified
                                // asset. This is possible in e.g. perforce where you handle
                                // one version conflict at a time.
                                t = Provider.Status(assetList);
                                t.Wait();

                                DoOpen(t.assetList);

                                if (t.success && assetList.Count == 0)
                                    Close();

                                // The view will be updated with the new conflicts
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error resolving", "Error during resolve of files. Inspect log for details", "Close");
                                AssetDatabase.Refresh();
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error merging", "Error during merge of files. Inspect log for details", "Close");
                            AssetDatabase.Refresh();
                        }
                    }
                    else
                    {
                        SimpleMerge(resolveMethod);
                    }
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = true;

            if (GUILayout.Button("Cancel"))
            {
                cancelled = true;
                Close();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12);
            if (repaint)
                Repaint();
        }
    }
}
