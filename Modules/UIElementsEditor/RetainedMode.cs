// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Bindings;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [InitializeOnLoad]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class RetainedMode
    {
        static RetainedMode()
        {
            UIElementsIMGUIUtility.s_BeginContainerCallback = OnBeginContainer;
            UIElementsIMGUIUtility.s_EndContainerCallback = OnEndContainer;

            Panel.initEditorUpdaterFunc = EditorPanel.InitEditorUpdater;
            Panel.loadResourceFunc = StyleSheetResourceUtil.LoadResource;
            StylePropertyReader.getCursorIdFunc = UIElementsEditorUtility.GetCursorId;
            BindingExtensions.bindingImpl = new DefaultSerializedObjectBindingImplementation();
            EditorWindowBackendManager.defaultWindowBackend = (model) => model is IEditorWindowModel ? new DefaultEditorWindowBackend() :  new DefaultWindowBackend();
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
            UIElementsUtility.UpdateSchedulers();
        }

        [RequiredByNativeCode]
        static void RequestRepaintForPanels()
        {
            UIElementsUtility.RequestRepaintForPanels((obj) =>
            {
                var guiView = obj as GUIView;
                if (guiView != null)
                    guiView.Repaint();
            });
        }

        internal class RetainedModeAssetPostprocessor : AssetPostprocessor
        {
            private const string k_UxmlExtension = ".uxml";
            private const string k_UssExtension = ".uss";
            private const string k_UssExtensionGenerated = ".uss.asset"; // for editor_resources project

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                // Early exit: no imported or deleted assets.
                var uxmlImportedAssets = new HashSet<string>(importedAssets.Where(x => MatchesFileExtension(x, k_UxmlExtension)));
                var uxmlDeletedAssets = new HashSet<string>(deletedAssets.Where(x => MatchesFileExtension(x, k_UxmlExtension)));
                var ussImportedAssets = new HashSet<string>(importedAssets.Where(x => MatchesFileExtension(x, k_UssExtension) || MatchesFileExtension(x, k_UssExtensionGenerated)));
                var ussDeletedAssets = new HashSet<string>(deletedAssets.Where(x => MatchesFileExtension(x, k_UssExtension)));

                if (uxmlImportedAssets.Count == 0 && uxmlDeletedAssets.Count == 0 &&
                    ussImportedAssets.Count == 0 && ussDeletedAssets.Count == 0)
                {
                    return;
                }

                HashSet<VisualTreeAsset> uxmlModifiedAssets = null;
                if (uxmlImportedAssets.Count > 0)
                {
                    uxmlModifiedAssets = new HashSet<VisualTreeAsset>();
                    foreach (var assetPath in uxmlImportedAssets)
                    {
                        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
                        if (asset != null) // Shouldn't be!
                        {
                            uxmlModifiedAssets.Add(asset);
                        }
                    }
                }

                HashSet<StyleSheet> ussModifiedAssets = null;

                var iterator = UIElementsUtility.GetPanelsIterator();
                while (iterator.MoveNext())
                {
                    var panel = iterator.Current.Value;
                    panel.liveReloadSystem.OnVisualTreeAssetsImported(uxmlModifiedAssets, uxmlDeletedAssets);

                    // ussModifiedAssets is null but we don't care for those, only deleted ones (that we'll stop tracking).
                    panel.liveReloadSystem.OnStyleSheetAssetsImported(ussModifiedAssets, ussDeletedAssets);
                }

                if (ussImportedAssets.Count > 0 || ussDeletedAssets.Count > 0)
                {
                    foreach(var styleSheetPath in ussImportedAssets)
                        UIElementsUtility.MarkStyleSheetAsChanged(styleSheetPath);

                    foreach(var styleSheetPath in ussDeletedAssets)
                        UIElementsUtility.MarkStyleSheetAsChanged(styleSheetPath);
                }
            }

            private static bool MatchesFileExtension(string assetPath, string fileExtension)
            {
                return assetPath.EndsWithIgnoreCaseFast(fileExtension);
            }
        }
    }
}
