// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager
{
    internal static class NativeStatusCodeExtensions
    {
        public static StatusCode ConvertToManaged(this NativeStatusCode status)
        {
            switch (status)
            {
                case NativeStatusCode.InProgress:
                case NativeStatusCode.InQueue:
                    return StatusCode.InProgress;
                case NativeStatusCode.Error:
                case NativeStatusCode.NotFound:
                case NativeStatusCode.Cancelled:
                    return StatusCode.Failure;
                case NativeStatusCode.Done:
                    return StatusCode.Success;
            }

            throw new NotSupportedException(string.Format("Unknown native status code {0}", status));
        }

        public static bool IsCompleted(this NativeStatusCode status)
        {
            return (ConvertToManaged(status) != StatusCode.InProgress);
        }
    }
}
