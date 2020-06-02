// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements.Debugger;
using UnityEditor.StyleSheets;
using UnityEditor.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Scripting;

using UXMLImporterImpl = UnityEditor.UIElements.UXMLImporterImpl;


namespace UnityEditor
{
    [InitializeOnLoad]
    class RetainedMode : AssetPostprocessor
    {
        private const string k_UielementsUxmllivereloadPrefsKey = "UIElements_UXMLLiveReload";

        internal static bool UxmlLiveReloadIsEnabled
        {
            get { return EditorPrefs.GetBool(k_UielementsUxmllivereloadPrefsKey, false); }
            set { EditorPrefs.SetBool(k_UielementsUxmllivereloadPrefsKey, value); }
        }

        static RetainedMode()
        {
            UIElementsUtility.s_BeginContainerCallback = OnBeginContainer;
            UIElementsUtility.s_EndContainerCallback = OnEndContainer;

            Panel.loadResourceFunc = StyleSheetResourceUtil.LoadResource;
            StylePropertyReader.getCursorIdFunc = UIElementsEditorUtility.GetCursorId;
            Panel.TimeSinceStartup = () => (long)(EditorApplication.timeSinceStartup * 1000.0f);
        }

        static void OnBeginContainer(IMGUIContainer c)
        {
            LocalizedEditorFontManager.LocalizeEditorFonts();
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

            UIEventRegistration.UpdateSchedulers();
        }

        [RequiredByNativeCode]
        static void RequestRepaintForPanels()
        {
            UIEventRegistration.RequestRepaintForPanels((obj) =>
            {
                var guiView = obj as GUIView;
                if (guiView != null)
                    guiView.Repaint();
            });
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
                        UnityEngine.UIElements.StyleSheets.StyleSheetCache.ClearCaches();
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
            UnityEngine.UIElements.StyleSheets.StyleSheetCache.ClearCaches();

            // for now we don't bother tracking which panel depends on which style sheet
            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;

                panel.DirtyStyleSheets();

                var guiView = panel.ownerObject as GUIView;
                if (guiView != null)
                    guiView.Repaint();
            }
        }
    }
}
