// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Unity.Localization.Editor;

static class ResourceTableCollectionCreateMenu
{
    [MenuItem("Assets/Create/Localization/Resource Table Collection", priority = 199)]
    static void CreateResourceTableCollection()
    {
        if (!LocalizationTableAuthoring.CanCreateCollection(out var reason))
        {
            if (EditorUtility.DisplayDialog(L10n.Tr("Create resource table collection", null), reason,
                L10n.Tr("Open Localization settings", null), L10n.Tr("Cancel", null)))
                SettingsService.OpenProjectSettings("Project/Localization");
            return;
        }

        // A collection is a folder of assets, so the placeholder the user renames is a folder.
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None,
            ScriptableObject.CreateInstance<DoCreateResourceTableCollection>(), "New Table Collection",
            EditorGUIUtility.IconContent(EditorResources.folderIconName).image as Texture2D, null);
    }
}

class DoCreateResourceTableCollection : AssetCreationEndAction
{
    public override void Action(EntityId entityId, string pathName, string resourceFile)
    {
        var parent = Path.GetDirectoryName(pathName)?.Replace('\\', '/');
        var collection = LocalizationTableAuthoring.CreateCollection(parent, Path.GetFileName(pathName));
        if (collection != null)
            ProjectWindowUtil.ShowCreatedAsset(collection);
    }
}
