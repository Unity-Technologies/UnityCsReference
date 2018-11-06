// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.Experimental.UIElements
{
    // Update phases, the order of the enum define the updater order
    internal enum VisualTreeUpdatePhase
    {
        PersistentData,
        Bindings,
        Styles,
        Layout,
        TransformClip,
        Repaint,
        Count
    }

    internal sealed class VisualTreeUpdater : IDisposable
    {
        class UpdaterArray
        {
            private IVisualTreeUpdater[] m_VisualTreeUpdaters;

            public UpdaterArray()
            {
                m_VisualTreeUpdaters = new IVisualTreeUpdater[(int)VisualTreeUpdatePhase.Count];
            }

            public IVisualTreeUpdater this[VisualTreeUpdatePhase phase]
            {
                set { m_VisualTreeUpdaters[(int)phase] = value; }
                get { return m_VisualTreeUpdaters[(int)phase]; }
            }

            public IVisualTreeUpdater this[int index]
            {
                set { m_VisualTreeUpdaters[index] = value; }
                get { return m_VisualTreeUpdaters[index]; }
            }
        }

        private BaseVisualElementPanel m_Panel;
        private UpdaterArray m_UpdaterArray;

        public VisualTreeUpdater(BaseVisualElementPanel panel)
        {
            m_Panel = panel;
            m_UpdaterArray = new UpdaterArray();

            SetDefaultUpdaters();
        }

        public void Dispose()
        {
            for (int i = 0; i < (int)VisualTreeUpdatePhase.Count; i++)
            {
                var updater = m_UpdaterArray[i];
                updater.Dispose();
            }
        }

        public void UpdateVisualTree()
        {
            for (int i = 0; i < (int)VisualTreeUpdatePhase.Count; i++)
            {
                var updater = m_UpdaterArray[i];
                Profiler.BeginSample(updater.description);
                updater.Update();
                Profiler.EndSample();
            }
        }

        public void UpdateVisualTreePhase(VisualTreeUpdatePhase phase)
        {
            var updater = m_UpdaterArray[phase];

            Profiler.BeginSample(updater.description);
            updater.Update();
            Profiler.EndSample();
        }

        public void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            for (int i = 0; i < (int)VisualTreeUpdatePhase.Count; i++)
            {
                var updater = m_UpdaterArray[i];
                updater.OnVersionChanged(ve, versionChangeType);
            }
        }

        public void DirtyStyleSheets()
        {
            var styleUpdater = m_UpdaterArray[VisualTreeUpdatePhase.Styles] as VisualTreeStyleUpdater;
            styleUpdater.DirtyStyleSheets();
        }

        public void SetUpdater(IVisualTreeUpdater updater, VisualTreeUpdatePhase phase)
        {
            updater.panel = m_Panel;
            m_UpdaterArray[phase] = updater;
        }

        public void SetUpdater<T>(VisualTreeUpdatePhase phase) where T : IVisualTreeUpdater, new()
        {
            var updater = new T() {panel = m_Panel};
            m_UpdaterArray[phase] = updater;
        }

        public IVisualTreeUpdater GetUpdater(VisualTreeUpdatePhase phase)
        {
            return m_UpdaterArray[phase];
        }

        private void SetDefaultUpdaters()
        {
            SetUpdater<VisualTreePersistentDataUpdater>(VisualTreeUpdatePhase.PersistentData);
            SetUpdater<VisualTreeBindingsUpdater>(VisualTreeUpdatePhase.Bindings);
            SetUpdater<VisualTreeStyleUpdater>(VisualTreeUpdatePhase.Styles);
            SetUpdater<VisualTreeLayoutUpdater>(VisualTreeUpdatePhase.Layout);
            SetUpdater<VisualTreeTransformClipUpdater>(VisualTreeUpdatePhase.TransformClip);
            SetUpdater<VisualTreeRepaintUpdater>(VisualTreeUpdatePhase.Repaint);
        }
    }

    internal interface IVisualTreeUpdater : IDisposable
    {
        BaseVisualElementPanel panel { get; set; }

        string description { get; }

        void Update();
        void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType);
    }

    internal abstract class BaseVisualTreeUpdater : IVisualTreeUpdater
    {
        public BaseVisualElementPanel panel { get; set; }
        public VisualElement visualTree { get { return panel.visualTree; } }

        public abstract string description { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {}

        public abstract void Update();
        public abstract void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType);
    }
}
