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
        // We can set the BuildFailedException to be silent if we know we have already printed an
        // error message for the failure. That is the case when the BuildFailedException originates
        // from the BeeBuildPostprocessor. That way we can avoid redundant error messages about builds
        // failing.
        private bool m_Silent;

        internal BuildFailedException(string message, bool silent = false) :
            base(message)
        {
            m_Silent = silent;
        }

        public BuildFailedException(string message) :
            base(message)
        {
        }

        public BuildFailedException(Exception innerException) :
            base(null, innerException)
        {
        }

        [RequiredByNativeCode]
        private bool IsSilent()
        {
            return m_Silent;
        }

        [RequiredByNativeCode]
        private Exception BuildFailedException_GetInnerException()
        {
            return InnerException;
        }
    }
}
