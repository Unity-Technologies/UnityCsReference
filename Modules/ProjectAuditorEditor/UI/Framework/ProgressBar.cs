// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class ProgressBar : IProgress
    {
        int m_Current;
        int m_Total;

        string m_Description;
        string m_Title;

        bool m_IsCancelled = false;

        public void Advance(string description = "")
        {
            if (m_IsCancelled)
                return;

            if (!string.IsNullOrEmpty(description))
                m_Description = description;
            m_Current++;
            var currentFrame = Mathf.Clamp(0, m_Current, m_Total);
            var progress = m_Total > 0 ? (float)currentFrame / m_Total : 0f;

            m_IsCancelled = EditorUtility.DisplayCancelableProgressBar(m_Title, description, progress);
            if (m_IsCancelled)
                EditorUtility.DisplayProgressBar("Cancelling...", "Please wait while the operation is cancelled", 0f);
        }

        public void Start(string title, string description, int total)
        {
            if (m_IsCancelled)
                return;

            m_Current = 0;
            m_Total = total;

            m_Title = title;
            m_Description = description;

            m_IsCancelled = EditorUtility.DisplayCancelableProgressBar(m_Title, m_Description, m_Current);
            if (m_IsCancelled)
                EditorUtility.DisplayProgressBar("Canceling...", "Please wait while the operation is cancelled", 0f);
        }

        public void Clear()
        {
            EditorUtility.ClearProgressBar();
        }

        public bool IsCancelled => m_IsCancelled;
    }
}
