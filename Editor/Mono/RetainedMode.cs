// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.Debugger;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Scripting;

namespace UnityEditor
{
    class RetainedMode : AssetPostprocessor
    {
        private const string k_UielementsUxmllivereloadPrefsKey = "UIElements_UXMLLiveReload";

        internal static bool UxmlLiveReloadIsEnabled
        {
            get { return EditorPrefs.GetBool(k_UielementsUxmllivereloadPrefsKey, false); }
            set { EditorPrefs.SetBool(k_UielementsUxmllivereloadPrefsKey, value); }
        }

        static bool s_FontInitialized = false;
        static RetainedMode()
        {
            UIElementsUtility.s_BeginContainerCallback = OnBeginContainer;
            UIElementsUtility.s_EndContainerCallback = OnEndContainer;
        }

        static void OnBeginContainer(IMGUIContainer c)
        {
            if (!s_FontInitialized)
            {
                s_FontInitialized = true;
                LocalizedEditorFontManager.LocalizeEditorFonts();
            }
            HandleUtility.BeginHandles();
        }

        static void OnEndContainer(IMGUIContainer c)
        {
            HandleUtility.EndHandles();
        }

        [RequiredByNativeCode]
        static void UpdateSchedulers()
        {
            DataWatchService.sharedInstance.PollNativeData();

            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;

                // Game panels' scheduler are ticked by the engine
                if (panel.contextType != ContextType.Editor)
                    continue;

                // Dispatch all timer update messages to each scheduled item
                panel.timerEventScheduler.UpdateScheduledEvents();
                panel.UpdateBindings();
            }
        }

        [RequiredByNativeCode]
        static void RequestRepaintForPanels()
        {
            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;

                // Game panels' scheduler are ticked by the engine
                if (panel.contextType != ContextType.Editor)
                    continue;

                // Dispatch might have triggered a repaint request.
                if (panel.isDirty)
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
                    if (!anyUssImported)
                    {
                        anyUssImported = true;
                        FlagStyleSheetChange();
                    }
                }
                else if (assetPath.EndsWith("uxml"))
                {
                    if (!anyUxmlImported)
                    {
                        anyUxmlImported = true;
                        UXMLImporterImpl.logger.FinishImport();

                        // the inline stylesheet cache might get out of date.
                        // Usually called by the USS importer, which might not get called here
                        StyleSheetCache.ClearCaches();

                        if (UxmlLiveReloadIsEnabled && Unsupported.IsDeveloperMode())
                        {
                            // Delay the view reloading so we do not try to reload the view that
                            // is currently active in the current callstack (i.e. ProjectView).
                            EditorApplication.update += OneShotUxmlLiveReload;
                        }
                    }
                }

                // no need to continue, as we found both extensions we were looking for
                if (anyUxmlImported && anyUssImported)
                    break;
            }
        }

        private static void OneShotUxmlLiveReload()
        {
            try
            {
                var it = UIElementsUtility.GetPanelsIterator();
                while (it.MoveNext())
                {
                    var view = it.Current.Value.ownerObject as HostView;
                    if (view != null && view.actualView != null && !(view.actualView is UIElementsDebugger))
                    {
                        view.Reload(view.actualView);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // Make sure to unregister ourself to prevent any infinit updates.
            EditorApplication.update -= OneShotUxmlLiveReload;
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

                panel.DirtyStyleSheets();

                var guiView = panel.ownerObject as GUIView;
                if (guiView != null)
                    guiView.Repaint();
            }
        }
    }
}
