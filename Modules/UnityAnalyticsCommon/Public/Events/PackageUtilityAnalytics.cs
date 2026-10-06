// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;


namespace UnityEditor.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    class AssetImportStatusAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public AssetImportStatusAnalytic() : base("assetImportStatus", 1, UnityEngine.Analytics.SendEventOptions.kAppendBuildTarget) { }

        [RequiredByNativeCode]
        public static AssetImportStatusAnalytic CreateAssetImportStatusAnalytic() { return new AssetImportStatusAnalytic(); }

        public string package_name;
        public int package_items_count;
        public int package_import_status;
        public string error_message;
        public int project_assets_count;
        public int unselected_assets_count;
        public int selected_new_assets_count;
        public int selected_changed_assets_count;
        public int unchanged_assets_count;
        public string[] selected_asset_extensions;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    class AssetImportAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public AssetImportAnalytic() : base("assetImport", 1) { }

        [RequiredByNativeCode]
        public static AssetImportAnalytic CreateAssetImportAnalytic() { return new AssetImportAnalytic(); }

        public string package_name;
        public int package_import_choice;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    class AssetExportAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public AssetExportAnalytic() : base("assetExport", 2) { }

        [RequiredByNativeCode]
        public static AssetExportAnalytic CreateAssetExportAnalytic() { return new AssetExportAnalytic(); }

        public string package_name;
        public string export_error_message;
        public int items_count;
        public string[] asset_extensions;
        public bool include_upm_dependencies;
        public int source;
        public bool signing_succeeded;
        public string owner_org_id;
        public bool has_bypass_entitlement;
        public bool user_signed_in;
        public string signature_error_message;
    }
}
