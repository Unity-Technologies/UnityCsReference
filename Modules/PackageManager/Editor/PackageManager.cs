// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Net;
using System.Net.Sockets;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    public enum StatusCode : uint
    {
        InQueue = 0,
        InProgress = 1,
        Done = 2,
        Error = 3,
        NotFound = 4
    }

    public enum ErrorCode : uint
    {
        Success = 0,
        NotFound = 1,
        Forbidden = 2,
        InvalidParameter = 3,
        Unknown = 4,
    }

    public enum OriginType : uint
    {
        Registry = 0,
        Url = 1,
        Path = 2,
        Unknown = 3
    }

    public enum RelationType : uint
    {
        ReadOnly = 0,
        Excluded = 1,
        Internalized = 2,
        Unknown = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class OperationStatus
    {
        private StatusCode m_Status;
        private string m_Id;
        private string m_Type;
        private UpmPackageInfo[] m_PackageList;
        private float m_Progress;

        private OperationStatus() {}

        public string id { get { return m_Id;  } }
        public StatusCode status { get { return m_Status;  } }
        public string type { get { return m_Type;  } }
        public UpmPackageInfo[] packageList { get { return m_PackageList; } }
        public float progress { get { return m_Progress; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class Error
    {
        private ErrorCode m_ErrorCode;
        private string m_Message;

        private Error() {}

        public ErrorCode errorCode { get { return m_ErrorCode;  } }
        public string message { get { return m_Message;  } }
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class UpmPackageInfo
    {
        private string m_PackageId;
        private string m_Tag;
        private string m_Version;
        private OriginType m_OriginType;
        private string m_OriginLocation;
        private RelationType m_RelationType;
        private string m_ResolvedPath;
        private string m_Name;
        private string m_DisplayName;
        private string m_Category;
        private string m_Description;

        private UpmPackageInfo() {}

        public UpmPackageInfo(
            string packageId,
            string displayName = "",
            string category = "",
            string description = "")
        {
            // Set the default values
            m_OriginType = OriginType.Unknown;
            m_RelationType = RelationType.Unknown;
            m_Tag = string.Empty;
            m_OriginLocation = "not implemented";
            m_PackageId = packageId;
            m_DisplayName = displayName;
            m_Category = category;
            m_Description = description;

            // Populate name and version
            var nameAndVersion = packageId.Split('@');
            m_Name = nameAndVersion[0];
            m_Version = nameAndVersion[1];
        }

        public string packageId { get { return m_PackageId;  } }
        public string tag { get { return m_Tag;  } }
        public string version { get { return m_Version;  } }
        public OriginType originType { get { return m_OriginType;  } }
        public string originLocation { get { return m_OriginLocation;  } }
        public RelationType relationType { get { return m_RelationType;  } }
        public string resolvedPath { get { return m_ResolvedPath;  } }
        public string name { get { return m_Name;  } }
        public string displayName { get { return m_DisplayName;  } }
        public string category { get { return m_Category;  } }
        public string description { get { return m_Description;  } }
    }

    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class OutdatedPackage
    {
        private UpmPackageInfo m_Current;
        private UpmPackageInfo m_Latest;

        private OutdatedPackage() {}

        public UpmPackageInfo current { get { return m_Current;  } }
        public UpmPackageInfo latest { get { return m_Latest;  } }
    }

    internal class Menu
    {
        [MenuItem("internal:Project/Packages/Reset to Editor defaults")]
        public static void ResetProjectPackagesToEditorDefaults()
        {
            long operationId = 0;
            var statusCode = Client.ResetToEditorDefaults(out operationId);
            if (statusCode == StatusCode.Error)
            {
                var error = Client.GetOperationError(operationId);
                Debug.LogError("Reset to Editor defaults failed, reason: " + error.message);
            }
        }
    }
}

