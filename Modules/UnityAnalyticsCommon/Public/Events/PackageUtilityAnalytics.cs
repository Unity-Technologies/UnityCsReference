// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
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

        [UsedByNativeCode]
        public static AssetImportStatusAnalytic CreateAssetImportStatusAnalytic() { return new AssetImportStatusAnalytic(); }

        public string package_name;
        public int package_items_count;
        public int package_import_status;
        public string error_message;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    class AssetImportAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public AssetImportAnalytic() : base("assetImport", 1) { }

        [UsedByNativeCode]
        public static AssetImportAnalytic CreateAssetImportAnalytic() { return new AssetImportAnalytic(); }

        public string package_name;
        public int package_import_choice;
    }
}
