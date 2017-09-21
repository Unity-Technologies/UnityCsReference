// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Scripting;

namespace UnityEditor
{
    class RetainedMode : AssetPostprocessor
    {
        static HashSet<Object> s_TmpDirtySet = new HashSet<Object>();

        static RetainedMode()
        {
            UIElementsUtility.s_BeginContainerCallback = OnBeginContainer;
            UIElementsUtility.s_EndContainerCallback = OnEndContainer;
        }

        static void OnBeginContainer(IMGUIContainer c)
        {
            HandleUtility.BeginHandles();
        }

        static void OnEndContainer(IMGUIContainer c)
        {
            HandleUtility.EndHandles();
        }

        [RequiredByNativeCode]
        static void UpdateSchedulers()
        {
            Debug.Assert(s_TmpDirtySet.Count == 0);
            try
            {
                UpdateSchedulersInternal(s_TmpDirtySet);
            }
            finally
            {
                s_TmpDirtySet.Clear();
            }
        }

        static void UpdateSchedulersInternal(HashSet<Object> tmpDirtySet)
        {
            DataWatchService.sharedInstance.PollNativeData();

            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;

                // Game panels' scheduler are ticked by the engine
                if (panel.contextType != ContextType.Editor)
                    continue;

                var timerEventScheduler = panel.scheduler;

                // Dispatch all timer update messages to each scheduled item
                panel.timerEventScheduler.UpdateScheduledEvents();

                // Dispatch might have triggered a repaint request.
                if (panel.visualTree.IsDirty(ChangeType.Repaint))
                {
                    var guiView = panel.ownerObject as GUIView;
                    if (guiView != null)
                        guiView.Repaint();
                }
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool anyUxmlImported = false;
            bool anyUssImported = false;
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.EndsWith("uss"))
                {
                    anyUssImported = true;
                    FlagStyleSheetChange();
                }
                else if (assetPath.EndsWith("uxml"))
                {
                    anyUxmlImported = true;
                    UIElementsViewImporter.logger.FinishImport();

                    // the inline stylesheet cache might get out of date.
                    // Usually called by the USS importer, which might not get called here
                    StyleSheetCache.ClearCaches();
                }

                // no need to continue, as we found both extensions we were looking for
                if (anyUxmlImported && anyUssImported)
                    break;
            }
        }

        public static void FlagStyleSheetChange()
        {
            // clear caches that depend on loaded style sheets
            StyleSheetCache.ClearCaches();

            // for now we don't bother tracking which panel depends on which style sheet
            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;

                // In-game doesn't support styling
                if (panel.contextType != ContextType.Editor)
                    continue;

                panel.styleContext.DirtyStyleSheets();
                panel.visualTree.Dirty(ChangeType.Styles); // dirty all styles

                var guiView = panel.ownerObject as GUIView;
                if (guiView != null)
                    guiView.Repaint();
            }
        }

    }
}
