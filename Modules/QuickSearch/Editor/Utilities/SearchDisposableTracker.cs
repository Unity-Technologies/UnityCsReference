// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;

namespace UnityEditor.Search
{
    sealed class SearchDisposableTracker : IDisposable
    {
        bool m_ReportFinalizer;
        bool m_ReportDoubleDispose;

        StackTrace m_CreationStackTrace;
        StackTrace m_DisposedStackTrace;

        public StackTrace CreationStackTrace => m_CreationStackTrace;
        public StackTrace DisposedStackTrace => m_DisposedStackTrace;

        public bool Disposed { get; private set; }

        public SearchDisposableTracker(bool reportFinalizer, bool reportDoubleDispose)
        {
            m_ReportFinalizer = reportFinalizer;
            m_ReportDoubleDispose = reportDoubleDispose;

            m_CreationStackTrace = new StackTrace(1, true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (Disposed && m_ReportDoubleDispose)
            {
                UnityEngine.Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, $"Double Dispose for object created at:\n{m_CreationStackTrace}\nand disposed at:\n{m_DisposedStackTrace}");
            }

            if (!disposing && m_ReportFinalizer)
            {
                UnityEngine.Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, $"Finalizer called for object created at:\n{m_CreationStackTrace}");
            }

            if (!Disposed && disposing)
            {
                m_DisposedStackTrace = new StackTrace(1, true);
            }

            Disposed = true;
        }
    }
}
