// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;


namespace UnityEditor.AssetPackage
{
    public struct ExportPackageParameters
    {
        public string[] AssetPathNames { get; set; }
        public string FileName { get; set; }
        public string OwnerOrgId { get; set; }
        public ExportPackageOptions Flags { get; set; }

        public ExportPackageParameters(string[] assetPathNames, string fileName, string ownerOrgId = "", ExportPackageOptions flags = ExportPackageOptions.Default)
        {
            AssetPathNames = assetPathNames;
            FileName = fileName;
            OwnerOrgId = ownerOrgId;
            Flags = flags;
        }

        public ExportPackageParameters(string assetPathName, string fileName, string ownerOrgId = "", ExportPackageOptions flags = ExportPackageOptions.Default)
        {
            AssetPathNames = new[] { assetPathName };
            FileName = fileName;
            OwnerOrgId = ownerOrgId;
            Flags = flags;
        }
    }

}
