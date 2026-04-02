// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.Serialization;
using UnityEngine.Scripting;



namespace UnityEngine
{
    public static class StackTraceUtility
    {
        [System.Security.SecuritySafeCritical] // System.Diagnostics.StackTrace cannot be accessed from transparent code (PSM, 2.12)
        [RequiredByNativeCode]
        static unsafe public string ExtractStackTrace()
        {
            int bufMax = 16384;
            byte* buf = stackalloc byte[bufMax];

            int quickSize = Debug.ExtractStackTraceNoAlloc(buf, bufMax, Unity.Scripting.StackTrace.BasePath);
            if (quickSize > 0)
            {
                return new string((sbyte*)buf, 0, quickSize, Encoding.UTF8);
            }

            var trace = new StackTrace(1, true);
            string traceString = Unity.Scripting.StackTrace.Format(trace);
            return traceString;
        }

        static public string ExtractStringFromException(System.Object exception)
        {
            Unity.Scripting.StackTrace.GetMessageAndStackTrace(exception as Exception, out var message, out var stackTrace);
            return message + "\n" + stackTrace;
        }

        // As this class is part of UnityEngine, it might not be available during CoreCLR code loading operations.
        // On CoreCLR use the StackTraceInterop directly as it is part of Scripting Core, which doesn't get unloaded.

        [RequiredByNativeCode]
        static void SetProjectFolder(string folder)
            => Unity.Scripting.StackTrace.BasePath = folder;

        [RequiredByNativeCode]
        static void ExtractStringFromExceptionInternal(System.Object exceptiono, out string message, out string stackTrace)
            => Unity.Scripting.StackTrace.GetMessageAndStackTrace(exceptiono as Exception, out message, out stackTrace);
    }

    [Serializable]
    [RequiredByNativeCode]
    public class UnityException : SystemException
    {
        const int Result = unchecked ((int)0x80004003);
#pragma warning disable 169
        string unityStackTrace;

        // Constructors
        public UnityException()
            : base("A Unity Runtime error occurred!")
        {
            HResult = Result;
        }

        public UnityException(string message)
            : base(message)
        {
            HResult = Result;
        }

        public UnityException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = Result;
        }

        protected UnityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }

    [Serializable]
    public class MissingComponentException : SystemException
    {
        const int Result = unchecked ((int)0x80004003);
#pragma warning disable 169
        string unityStackTrace;

        // Constructors
        public MissingComponentException()
            : base("A Unity Runtime error occurred!")
        {
            HResult = Result;
        }

        public MissingComponentException(string message)
            : base(message)
        {
            HResult = Result;
        }

        public MissingComponentException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = Result;
        }

        protected MissingComponentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }


    [Serializable]
    public class UnassignedReferenceException : SystemException
    {
        const int Result = unchecked ((int)0x80004003);
#pragma warning disable 169
        string unityStackTrace;

        // Constructors
        public UnassignedReferenceException()
            : base("A Unity Runtime error occurred!")
        {
            HResult = Result;
        }

        public UnassignedReferenceException(string message)
            : base(message)
        {
            HResult = Result;
        }

        public UnassignedReferenceException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = Result;
        }

        protected UnassignedReferenceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }


    [Serializable]
    public class MissingReferenceException : SystemException
    {
        const int Result = unchecked ((int)0x80004003);
#pragma warning disable 169
        string unityStackTrace;

        // Constructors
        public MissingReferenceException()
            : base("A Unity Runtime error occurred!")
        {
            HResult = Result;
        }

        public MissingReferenceException(string message)
            : base(message)
        {
            HResult = Result;
        }

        public MissingReferenceException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = Result;
        }

        protected MissingReferenceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }
}
