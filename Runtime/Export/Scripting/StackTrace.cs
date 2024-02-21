// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections;
using UnityEngine.Scripting;



namespace UnityEngine
{
    public static class StackTraceUtility
    {
        static string projectFolder = "";

        [RequiredByNativeCode]
        static internal void SetProjectFolder(string folder)
        {
            projectFolder = folder;

            if (!string.IsNullOrEmpty(projectFolder))
                projectFolder = projectFolder.Replace("\\", "/");
        }

        [System.Security.SecuritySafeCritical] // System.Diagnostics.StackTrace cannot be accessed from transparent code (PSM, 2.12)
        [RequiredByNativeCode]
        static unsafe public string ExtractStackTrace()
        {
            int bufMax = 16384;
            byte* buf = stackalloc byte[bufMax];

            int quickSize = Debug.ExtractStackTraceNoAlloc(buf, bufMax, projectFolder);
            if (quickSize > 0)
            {
                return new string((sbyte*)buf, 0, quickSize, Encoding.UTF8);
            }

            StackTrace trace = new StackTrace(1, true);
            string traceString = ExtractFormattedStackTrace(trace);
            return traceString;
        }

        static public string ExtractStringFromException(System.Object exception)
        {
            string message, stackTrace;
            ExtractStringFromExceptionInternal(exception, out message, out stackTrace);
            return message + "\n" + stackTrace;
        }

        [RequiredByNativeCode]
        [System.Security.SecuritySafeCritical] // System.Diagnostics.StackTrace cannot be accessed from transparent code (PSM, 2.12)
        static internal void ExtractStringFromExceptionInternal(System.Object exceptiono, out string message, out string stackTrace)
        {
            if (exceptiono == null) throw new ArgumentException("ExtractStringFromExceptionInternal called with null exception");
            var exception = exceptiono as System.Exception;
            if (exception == null) throw new ArgumentException("ExtractStringFromExceptionInternal called with an exception that was not of type System.Exception");

            // StackTrace might not be available
            StringBuilder sb = new StringBuilder(exception.StackTrace == null ? 512 : exception.StackTrace.Length * 2);
            message = "";
            string traceString = "";
            while (exception != null)
            {
                if (traceString.Length == 0)
                    traceString = exception.StackTrace;
                else
                    traceString = exception.StackTrace + "\n" + traceString;

                string thisMessage = exception.GetType().Name;
                string exceptionMessage = "";
                if (exception.Message != null) exceptionMessage = exception.Message;
                if (exceptionMessage.Trim().Length != 0)
                {
                    thisMessage += ": ";
                    thisMessage += exceptionMessage;
                }
                message = thisMessage;
                if (exception.InnerException != null)
                {
                    traceString = "Rethrow as " + thisMessage + "\n" + traceString;
                }
                exception = exception.InnerException;
            }

            sb.Append(traceString + "\n");

            StackTrace trace = new StackTrace(1, true);
            sb.Append(ExtractFormattedStackTrace(trace));

            stackTrace = sb.ToString();
        }

        // NB if you change this formatting/code there is a separate Mono quick path in MonoManager.cpp that must be updated as well.
        [System.Security.SecuritySafeCritical] // System.Diagnostics.StackTrace cannot be accessed from transparent code (PSM, 2.12)
        static internal string ExtractFormattedStackTrace(StackTrace stackTrace)
        {
            StringBuilder sb = new StringBuilder(255);
            int iIndex;

            // need to skip over "n" frames which represent the
            // System.Diagnostics package frames
            for (iIndex = 0; iIndex < stackTrace.FrameCount; iIndex++)
            {
                StackFrame frame = stackTrace.GetFrame(iIndex);

                MethodBase mb = frame.GetMethod();
                if (mb == null)
                    continue;

                Type classType = mb.DeclaringType;
                if (classType == null)
                    continue;

                // Add namespace.classname:MethodName
                String ns = classType.Namespace;
                if (!string.IsNullOrEmpty(ns))
                {
                    sb.Append(ns);
                    sb.Append(".");
                }

                sb.Append(classType.Name);
                sb.Append(":");
                sb.Append(mb.Name);
                sb.Append("(");

                // Add parameters
                int j = 0;
                ParameterInfo[] pi = mb.GetParameters();
                bool fFirstParam = true;
                while (j < pi.Length)
                {
                    if (fFirstParam == false)
                        sb.Append(", ");
                    else
                        fFirstParam = false;

                    sb.Append(pi[j].ParameterType.Name);
                    j++;
                }
                sb.Append(")");

                // Add path name and line number - unless it is a Debug.Log call, then we are only interested
                // in the calling frame.
                string path = frame.GetFileName();
                if (path != null)
                {
                    bool shouldStripLineNumbers =
                        (classType.Name == "Debug" && classType.Namespace == "UnityEngine") ||
                        (classType.Name == "Logger" && classType.Namespace == "UnityEngine") ||
                        (classType.Name == "DebugLogHandler" && classType.Namespace == "UnityEngine") ||
                        (classType.Name == "Assert" && classType.Namespace == "UnityEngine.Assertions") ||
                        (mb.Name == "print" && classType.Name == "MonoBehaviour" && classType.Namespace == "UnityEngine")
                    ;

                    if (!shouldStripLineNumbers)
                    {
                        sb.Append(" (at ");

                        if (!string.IsNullOrEmpty(projectFolder))
                        {
                            if (path.Replace("\\", "/").StartsWith(projectFolder))
                            {
                                path = path.Substring(projectFolder.Length, path.Length - projectFolder.Length);
                            }
                        }

                        sb.Append(path);
                        sb.Append(":");
                        sb.Append(frame.GetFileLineNumber().ToString());
                        sb.Append(")");
                    }
                }

                sb.Append("\n");
            }

            return sb.ToString();
        }
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
