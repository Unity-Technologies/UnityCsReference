// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System.Collections.Generic;

namespace UnityEditor.PackageManager
{
    partial class NativeClient
    {
        public enum StatusCode : uint
        {
            InQueue = 0,
            InProgress = 1,
            Done = 2,
            Error = 3,
            NotFound = 4
        }

        public static Dictionary<string, OutdatedPackage> GetOutdatedOperationData(long operationId)
        {
            string[] names;
            OutdatedPackage[] outdated = GetOutdatedOperationData(operationId, out names);

            var res = new Dictionary<string, OutdatedPackage>(names.Length);
            for (int i = 0; i < names.Length; ++i)
            {
                res[names[i]] = outdated[i];
            }
            return res;
        }
    }
}

