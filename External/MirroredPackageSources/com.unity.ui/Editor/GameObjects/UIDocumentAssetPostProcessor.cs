using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal class UIDocumentAssetPostProcessor : AssetPostprocessor
    {
        private const string k_UxmlExtension = ".uxml";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var uxmlImportedAssets = new HashSet<string>(importedAssets.Where(x => MatchesFileExtension(x, k_UxmlExtension)));
            var uxmlDeletedAssets =  new HashSet<string>(deletedAssets.Where(x => MatchesFileExtension(x, k_UxmlExtension)));

            // Early exit: no imported or deleted assets.
            if (uxmlImportedAssets.Count == 0 && uxmlDeletedAssets.Count == 0)
            {
                return;
            }

            var uiDocuments = Object.FindObjectsOfType<UIDocument>();

            // Early exit: no UIDocument to live reload.
            if (uiDocuments == null || uiDocuments.Length == 0)
            {
                return;
            }

            foreach (var uiDocument in uiDocuments)
            {
                if (uiDocument.visualTreeAsset != null)
                {
                    string uxmlPath = AssetDatabase.GetAssetPath(uiDocument.visualTreeAsset);
                    if (!uxmlImportedAssets.Contains(uxmlPath))
                    {
                        // Check templates used inside the UXML for changes as well.
                        bool shouldRecreateUI = false;
                        foreach (var template in uiDocument.visualTreeAsset.templateDependencies)
                        {
                            // Check for possible deleted template references
                            if (template != null)
                            {
                                uxmlPath = AssetDatabase.GetAssetPath(template);

                                if (uxmlImportedAssets.Contains(uxmlPath))
                                {
                                    shouldRecreateUI = true;
                                    break;
                                }
                            }
                            else if (uxmlDeletedAssets.Count > 0 && !ReferenceEquals(template, null))
                            {
                                // A referenced template was deleted; there'll be an error on the console but
                                // we also want to go ahead and update the UI to let the user know it's gone.
                                shouldRecreateUI = true;
                                break;
                            }
                        }

                        if (!shouldRecreateUI)
                        {
                            continue;
                        }
                    }

                    if (uiDocument.rootVisualElement != null)
                    {
                        uiDocument.HandleLiveReload();
                    }
                }
                else if (uxmlDeletedAssets.Count > 0 &&
                         !ReferenceEquals(uiDocument.visualTreeAsset, null) &&
                         uiDocument.rootVisualElement != null)
                {
                    // We can assume the uxml reference has been deleted in this case.
                    uiDocument.HandleLiveReload();
                }
            }
        }

        private static bool MatchesFileExtension(string assetPath, string fileExtension)
        {
            return assetPath.EndsWithIgnoreCaseFast(fileExtension);
        }
    }
}
