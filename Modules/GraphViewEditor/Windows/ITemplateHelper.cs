// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEditor.Search;
using UnityEditor.PackageManager.UI;

namespace UnityEditor.Experimental.GraphView
{
    /// <summary>
    /// As a GraphView tool developer, you must implement this interface to use the GraphViewTemplateWindow.
    /// Then, you can instantiate your implementation and provide it to either GraphViewTemplateWindow.ShowCreateFromTemplate or GraphViewTemplateWindow.ShowInsertTemplate
    /// </summary>
    internal interface ITemplateHelper
    {
        /// <summary>
        /// This key is used to serialize data by tool
        /// </summary>
        string toolKey { get; }
        /// <summary>
        /// Name of the package where the tool lives, it's only used to find if any package sample is available
        /// </summary>
        string packageInfoName { get; }
        /// <summary>
        /// Name of the package sample if any. It is used easily install this sample with a button in the GraphViewTemplateWindow.
        /// It's especially relevant if the sample contains templates expected to be shown in the template window.
        /// </summary>
        string learningSampleName { get; }
        /// <summary>
        /// Documentation url which is used in the details panel. For the moment it's a single url whatever the selected template is.
        /// </summary>
        string templateWindowDocUrl { get; }
        /// <summary>
        /// Path the built-in templates.
        /// This is used to automatically identify built-in templates and group them in a category.
        /// </summary>
        string builtInTemplatePath { get; }
        /// <summary>
        /// Name of the built-in templates category.
        /// </summary>
        string builtInCategory { get; }
        /// <summary>
        /// Type of asset to search for.
        /// </summary>
        Type assetType { get; }
        /// <summary>
        /// Title of the template window when used in the `creation` mode
        /// </summary>
        string createNewAssetTitle { get; }
        /// <summary>
        /// Title of the template window when used in the `insert` mode
        /// </summary>
        string insertTemplateTitle { get; }
        /// <summary>
        /// Asset guid that represents an empty Template, leave it null if not used
        /// </summary>
        string emptyTemplateGuid { get; }
        /// <summary>
        /// Path to the default icon for templates which don't have an icon specified
        /// </summary>
        string customTemplateIcon { get; }
        /// <summary>
        /// If true, a banner will be shown to the user when the package indexing is disabled.
        /// This banner will help the user to quickly enable package indexing.
        /// </summary>
        bool showPackageIndexingBanner { get; set; }

        /// <summary>
        /// This interface is expected to simply wrap the SaveFileDialog.
        /// It is abstracted in this interface to easily mock it for automatic tests.
        /// </summary>
        GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper { get; set; }

        void RaiseImportSampleDependencies(PackageManager.PackageInfo packageInfo, Sample sample);

        /// <summary>
        /// This method is called each time a template is used.
        /// The primary goal is to track built-in templates usage in the analytics.
        /// </summary>
        /// <param name="usedTemplate">Name of the template used</param>
        void RaiseTemplateUsed(GraphViewTemplateDescriptor usedTemplate);

        /// <summary>
        /// Fetch the template description from the path of an asset.
        /// </summary>
        /// <param name="assetPath">Path to the asset</param>
        /// <param name="graphViewTemplate">Template description</param>
        /// <returns>True if the asset is a template and contains template information, false otherwise</returns>
        bool TryGetTemplate(string assetPath, out GraphViewTemplateDescriptor graphViewTemplate);

        /// <summary>
        /// Define template information for an asset.
        /// </summary>
        /// <param name="assetPath">Path to the asset to update (or set) the template information</param>
        /// <param name="graphViewTemplate">Template information to set</param>
        /// <returns>True if the template information have been properly set, false otherwise</returns>
        bool TrySetTemplate(string assetPath, GraphViewTemplateDescriptor graphViewTemplate);

        /// <summary>
        /// Allow the tool implementation to provide a custom set of search propositions.
        /// </summary>
        /// <returns>A collection of search propositions to be made available in the search bar</returns>
        SearchProposition[] GetSearchPropositions();

        /// <summary>
        /// Allow the tool implementation to provide a custom set of sorters
        /// </summary>
        /// <returns>A collection of sorters to be made available in the sort drop down</returns>
        ITemplateSorter[] GetTemplateSorter();
    }
}
