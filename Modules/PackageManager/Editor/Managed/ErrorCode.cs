// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEditor.PackageManager
{
    public enum ErrorCode : uint
    {
        Unknown = 0,
        NotFound = 1,
        Forbidden = 2,
        InvalidParameter = 3,
        Conflict = 4,
        // NOTE: Error code success from the C++ API is not defined here
        // since we never create errors for successful requests
    }
}

