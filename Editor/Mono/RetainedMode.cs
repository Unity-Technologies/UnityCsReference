// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Scripting;

namespace UnityEditor
{
    class RetainedMode : AssetPostprocessor
    {
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

                DataWatchService service = panel.dataWatch as DataWatchService;
                Debug.Assert(service != null, "Expect Editor panel to have a DataWatchService object");
                service.ProcessNotificationQueue();

                // Dispatch might have triggered a repaint request.
                if (panel.visualTree.IsDirty(ChangeType.Repaint))
                {
                    var guiView = EditorUtility.InstanceIDToObject(panel.instanceID) as GUIView;
                    if (guiView != null)
                        guiView.Repaint();
                }
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.EndsWith("uss"))
                {
                    FlagStyleSheetChange();
                    break;
                }
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

                var guiView = EditorUtility.InstanceIDToObject(panel.instanceID) as GUIView;
                if (guiView != null)
                    guiView.Repaint();
            }
        }

    }
}
