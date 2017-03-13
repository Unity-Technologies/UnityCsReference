// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEditor.Build
{
    [RequiredByNativeCode]
    public class BuildFailedException : Exception
    {
        public BuildFailedException(string message) :
            base(message)
        {
        }

        public BuildFailedException(Exception innerException) :
            base(null, innerException)
        {
        }

        [RequiredByNativeCode]
        private Exception BuildFailedException_GetInnerException()
        {
            return InnerException;
        }
    }
}
