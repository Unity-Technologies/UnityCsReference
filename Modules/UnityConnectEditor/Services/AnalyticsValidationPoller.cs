// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    internal class AnalyticsValidationPoller
    {
        const double k_TickIntervalSeconds = 15.0;
        readonly TickTimerHelper m_Ticker = new TickTimerHelper(k_TickIntervalSeconds);

        //Auth signature for project, needed to access and validate data
        string m_ProjectAuthSignature;
        UnityWebRequest m_DataValidationRequest;

        private bool m_IsStarted;
        private string m_RawValidationData;

        Action<string> m_NotifyOnDataUpdate;

        public string projectAuthSignature
        {
            get { return m_ProjectAuthSignature; }
        }

        public void Setup(string authSignature, Action<string> updateNotification)
        {
            m_ProjectAuthSignature = authSignature;
            m_NotifyOnDataUpdate = updateNotification;
        }

        public void Shutdown()
        {
            Stop();
            m_ProjectAuthSignature = null;
            m_RawValidationData = null;
            m_NotifyOnDataUpdate = null;

            if (m_DataValidationRequest != null)
            {
                m_DataValidationRequest.Abort();
                m_DataValidationRequest.Dispose();
                m_DataValidationRequest = null;
            }
        }

        public bool IsReady()
        {
            return m_ProjectAuthSignature != null;
        }

        public void Start()
        {
            if (!m_IsStarted)
            {
                EditorApplication.update += Update;
                m_Ticker.Reset(); // ensures immediate tick on begin
                m_IsStarted = true;
            }
        }

        public void Stop()
        {
            if (m_IsStarted)
            {
                EditorApplication.update -= Update;
                m_IsStarted = false;
            }
        }

        private void Update()
        {
            if (m_Ticker.DoTick())
            {
                Poll();
            }
        }

        private bool HasDataChanged(string newRawData)
        {
            return m_RawValidationData != newRawData;
        }

        //Can be called independently without Start/Stopping, however will not adjust tick interval (it's private)
        public void Poll()
        {
            if (IsReady() && m_DataValidationRequest == null)
            {
                AnalyticsService.instance.RequestValidationData(OnPolled, m_ProjectAuthSignature, out m_DataValidationRequest);
            }
        }

        private void OnPolled(AsyncOperation op)
        {
            if (op.isDone)
            {
                if (m_DataValidationRequest != null)
                {
                    if ((m_DataValidationRequest.result != UnityWebRequest.Result.ProtocolError) && (m_DataValidationRequest.result != UnityWebRequest.Result.ConnectionError))
                    {
                        string newRawData = m_DataValidationRequest.downloadHandler.text;
                        if (HasDataChanged(newRawData))
                        {
                            m_RawValidationData = newRawData;
                            if (m_NotifyOnDataUpdate != null)
                            {
                                m_NotifyOnDataUpdate(m_RawValidationData);
                            }
                        }
                    }

                    m_DataValidationRequest.Dispose();
                    m_DataValidationRequest = null;
                }
            }
        }
    }
}
