// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public interface IUIElementDataWatchRequest : IDisposable {}

    public interface IUIElementDataWatch
    {
        IUIElementDataWatchRequest RegisterWatch(Object toWatch, Action<Object> watchNotification);
        void UnregisterWatch(IUIElementDataWatchRequest requested);
    }

    public partial class VisualElement : IUIElementDataWatch
    {
        private class DataWatchRequest : IUIElementDataWatchRequest, IVisualElementPanelActivatable
        {
            public Action<Object> notification { get; set; }
            public Object watchedObject { get; set; }
            public IDataWatchHandle requestedHandle { get; set; }

            private VisualElementPanelActivator m_Activator;
            public VisualElement element { get; set; }
            public DataWatchRequest(VisualElement handler)
            {
                element = handler;
                m_Activator = new VisualElementPanelActivator(this);
            }

            public void Start()
            {
                m_Activator.SetActive(true);
            }

            public void Stop()
            {
                m_Activator.SetActive(false);
            }

            public bool CanBeActivated()
            {
                return element != null && element.elementPanel != null && element.elementPanel.dataWatch != null;
            }

            public void OnPanelActivate()
            {
                if (requestedHandle == null)
                {
                    requestedHandle = element.elementPanel.dataWatch.AddWatch(watchedObject, notification);
                }
            }

            public void OnPanelDeactivate()
            {
                if (requestedHandle != null)
                {
                    element.elementPanel.dataWatch.RemoveWatch(requestedHandle);
                    requestedHandle = null;
                }
            }

            public void Dispose()
            {
                Stop();
            }
        }

        // this allows us to not expose all datawatch accessors directly on VisualElement class
        public IUIElementDataWatch dataWatch
        {
            get { return this; }
        }

        IUIElementDataWatchRequest IUIElementDataWatch.RegisterWatch(Object toWatch, Action<Object> watchNotification)
        {
            var datawatchRequest = new DataWatchRequest(this)
            {
                notification = watchNotification,
                watchedObject = toWatch
            };

            datawatchRequest.Start();
            return datawatchRequest;
        }

        void IUIElementDataWatch.UnregisterWatch(IUIElementDataWatchRequest requested)
        {
            DataWatchRequest r = requested as DataWatchRequest;
            if (r != null)
            {
                r.Stop();
            }
        }
    }
}
