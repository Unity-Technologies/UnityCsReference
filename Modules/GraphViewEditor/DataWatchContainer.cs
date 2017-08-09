// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal abstract class DataWatchContainer : VisualContainer
    {
        IDataWatchHandle[] handles;

        public bool forceNotififcationOnAdd { get; set; }

        protected DataWatchContainer()
        {
            // trigger data source reset when entering leaving panel
            onEnter += AddWatch;
            onLeave += RemoveWatch;
        }

        // called when Serialized object has changed
        // only works while widget is in a panel
        public virtual void OnDataChanged()
        {}

        protected abstract UnityEngine.Object[] toWatch { get; }

        protected void AddWatch()
        {
            var toWatch = this.toWatch;
            handles = new IDataWatchHandle[toWatch.Length];
            for (int i = 0; i < toWatch.Length; ++i)
            {
                if (panel != null && toWatch[i] != null)
                {
                    handles[i] = panel.dataWatch.AddWatch(this, toWatch[i], OnDataChanged);
                    if (forceNotififcationOnAdd)
                        OnDataChanged();
                }
            }
        }

        protected void RemoveWatch()
        {
            if (handles != null)
            {
                foreach (var handle in handles)
                    if (handle != null)
                        handle.Dispose();
                handles = null;
            }
        }
    }
}
