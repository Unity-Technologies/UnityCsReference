// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal enum UIErrorCode
    {
        // we must keep this Core Error Codes section in sync with Core team's ErrorCode.cs
        //  in order for error code casting to work - these values must also precede any new
        //  error codes we add specifically for PackageManagerUI
        #region Core Error Codes
        Unknown,
        NotFound,
        Forbidden,
        InvalidParameter,
        Conflict,
        #endregion

        AssetStoreAuthorizationError,
        AssetStoreClientError,
        AssetStoreRestApiError,
        AssetStoreOperationError,
        AssetStorePackageError,
        UpmError,
        NetworkError
    }
}
