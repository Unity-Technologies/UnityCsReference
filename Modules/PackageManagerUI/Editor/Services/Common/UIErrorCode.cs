// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal enum UIErrorCode
    {
        // we must keep this Core Error Codes section in sync with Core team's ErrorCode.cs
        //  in order for error code casting to work - these values must also precede any new
        //  error codes we add specifically for PackageManagerUI
        #region Original Core Error Codes
        UpmError_Unknown = ErrorCode.Unknown,
        UpmError_NotFound = ErrorCode.NotFound,
        UpmError_Forbidden = ErrorCode.Forbidden,
        UpmError_InvalidParameter = ErrorCode.InvalidParameter,
        UpmError_Conflict = ErrorCode.Conflict,
        #endregion

        // These are additional UPM errors that we need but are not returned
        // by the UPM server. These values might override the original code.
        UpmError_ServerNotRunning = 100,
        UpmError_InvalidSignature,
        UpmError_UnsignedUnityPackage,
        UpmError_NotSignedIn,
        UpmError_NotAcquired,

        AssetStoreAuthorizationError = 500,
        AssetStoreClientError,
        AssetStoreRestApiError,
        AssetStoreOperationError,
        AssetStorePackageError,
    }
}
