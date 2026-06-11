// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace UnityEditor
{
    // Export package option. Multiple options can be combined together using the | operator.
    [Flags]
    public enum ExportPackageOptions
    {
        // Default mode. Will not include dependencies or subdirectories nor include Library assets unless specifically included in the asset list.
        Default = 0,

        // The export operation will be run asynchronously and reveal the exported package file in a file browser window after the export is finished.
        Interactive = 1,

        // Will recurse through any subdirectories listed and include all assets inside them.
        Recurse = 2,

        // In addition to the assets paths listed, all dependent assets will be included as well.
        IncludeDependencies = 4,

        // The exported package will include all library assets, ie. the project settings located in the Library folder of the project.
        IncludeLibraryAssets = 8
    }
}
