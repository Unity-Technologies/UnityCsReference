using System;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    // Editor update phases, the order of the enum define the updater order
    internal enum VisualTreeEditorUpdatePhase
    {
        VisualTreeAssetChanged,
        Count
    }

    // Update phases, the order of the enum define the updater order
    internal enum VisualTreeUpdatePhase
    {
        ViewData,
        Bindings,
        Animation,
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


        class EditorUpdaterArray
        {
            private IVisualTreeUpdater[] m_VisualTreeUpdaters;

            public EditorUpdaterArray()
            {
                m_VisualTreeUpdaters = new IVisualTreeUpdater[(int)VisualTreeEditorUpdatePhase.Count];
            }

            public IVisualTreeUpdater this[VisualTreeEditorUpdatePhase phase]
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

        private EditorUpdaterArray m_EditorUpdaterArray;

        public VisualTreeUpdater(BaseVisualElementPanel panel)
        {
            m_Panel = panel;
            m_UpdaterArray = new UpdaterArray();
            m_EditorUpdaterArray = new EditorUpdaterArray();

            SetDefaultUpdaters();
        }

        public void Dispose()
        {
            for (int i = 0; i < (int)VisualTreeEditorUpdatePhase.Count; i++)
            {
                var updater = m_EditorUpdaterArray[i];
                updater.Dispose();
            }

            for (int i = 0; i < (int)VisualTreeUpdatePhase.Count; i++)
            {
                var updater = m_UpdaterArray[i];
                updater.Dispose();
            }
        }

        public void UpdateVisualTree()
        {
            for (int i = 0; i < (int)VisualTreeEditorUpdatePhase.Count; i++)
            {
                var updater = m_EditorUpdaterArray[i];

                using (updater.profilerMarker.Auto())
                {
                    updater.Update();
                }
            }

            for (int i = 0; i < (int)VisualTreeUpdatePhase.Count; i++)
            {
                var updater = m_UpdaterArray[i];

                using (updater.profilerMarker.Auto())
                {
                    updater.Update();
                }
            }
        }

        public void UpdateEditorVisualTreePhase(VisualTreeEditorUpdatePhase phase)
        {
            var updater = m_EditorUpdaterArray[phase];

            using (updater.profilerMarker.Auto())
            {
                updater.Update();
            }
        }


        public void UpdateVisualTreePhase(VisualTreeUpdatePhase phase)
        {
            var updater = m_UpdaterArray[phase];

            using (updater.profilerMarker.Auto())
            {
                updater.Update();
            }
        }

        public void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            for (int i = 0; i < (int)VisualTreeEditorUpdatePhase.Count; i++)
            {
                var updater = m_EditorUpdaterArray[i];
                updater.OnVersionChanged(ve, versionChangeType);
            }

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
            m_UpdaterArray[phase]?.Dispose();
            updater.panel = m_Panel;
            m_UpdaterArray[phase] = updater;
        }

        public void SetUpdater<T>(VisualTreeUpdatePhase phase) where T : IVisualTreeUpdater, new()
        {
            m_UpdaterArray[phase]?.Dispose();
            var updater = new T() {panel = m_Panel};
            m_UpdaterArray[phase] = updater;
        }

        public IVisualTreeUpdater GetUpdater(VisualTreeUpdatePhase phase)
        {
            return m_UpdaterArray[phase];
        }

        private void SetEditorUpdater<T>(VisualTreeEditorUpdatePhase phase) where T : IVisualTreeUpdater, new()
        {
            m_EditorUpdaterArray[phase]?.Dispose();
            var updater = new T() {panel = m_Panel};
            m_EditorUpdaterArray[phase] = updater;
        }

        public IVisualTreeUpdater GetEditorUpdater(VisualTreeEditorUpdatePhase phase)
        {
            return m_EditorUpdaterArray[phase];
        }


        private void SetDefaultUpdaters()
        {
            SetEditorUpdater<VisualTreeAssetChangeTrackerUpdater>(VisualTreeEditorUpdatePhase.VisualTreeAssetChanged);
            SetUpdater<VisualTreeViewDataUpdater>(VisualTreeUpdatePhase.ViewData);
            SetUpdater<VisualTreeBindingsUpdater>(VisualTreeUpdatePhase.Bindings);
            SetUpdater<VisualElementAnimationSystem>(VisualTreeUpdatePhase.Animation);
            SetUpdater<VisualTreeStyleUpdater>(VisualTreeUpdatePhase.Styles);
            SetUpdater<UIRLayoutUpdater>(VisualTreeUpdatePhase.Layout);
            SetUpdater<VisualTreeTransformClipUpdater>(VisualTreeUpdatePhase.TransformClip);
            SetUpdater<UIRRepaintUpdater>(VisualTreeUpdatePhase.Repaint);
        }
    }

    internal interface IVisualTreeUpdater : IDisposable
    {
        BaseVisualElementPanel panel { get; set; }

        ProfilerMarker profilerMarker { get; }

        void Update();
        void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType);
    }

    internal abstract class BaseVisualTreeUpdater : IVisualTreeUpdater
    {
        public event Action<BaseVisualElementPanel> panelChanged;

        private BaseVisualElementPanel m_Panel;
        public BaseVisualElementPanel panel
        {
            get { return m_Panel; }
            set
            {
                m_Panel = value;
                if (panelChanged != null) panelChanged(value);
            }
        }

        public VisualElement visualTree { get { return panel.visualTree; } }

        public abstract ProfilerMarker profilerMarker { get; }

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
