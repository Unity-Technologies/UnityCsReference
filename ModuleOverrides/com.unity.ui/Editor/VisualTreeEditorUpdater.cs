// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class VisualTreeEditorUpdater : IVisualTreeEditorUpdater
    {
        class UpdaterArray
        {
            private IVisualTreeUpdater[] m_VisualTreeUpdaters;

            public UpdaterArray()
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

        private BaseVisualElementPanel m_Panel;
        private UpdaterArray m_UpdaterArray;

        public VisualTreeEditorUpdater(BaseVisualElementPanel panel)
        {
            m_Panel = panel;
            m_UpdaterArray = new UpdaterArray();

            SetDefaultUpdaters();
        }

        public void Dispose()
        {
            for (int i = 0; i < (int)VisualTreeEditorUpdatePhase.Count; i++)
            {
                var updater = m_UpdaterArray[i];
                updater.Dispose();
            }
        }

        public IVisualTreeUpdater GetUpdater(VisualTreeEditorUpdatePhase phase)
        {
            return m_UpdaterArray[phase];
        }

        public void SetUpdater(IVisualTreeUpdater updater, VisualTreeEditorUpdatePhase phase)
        {
            m_UpdaterArray[phase]?.Dispose();
            updater.panel = m_Panel;
            m_UpdaterArray[phase] = updater;
        }

        public void SetUpdater<T>(VisualTreeEditorUpdatePhase phase) where T : IVisualTreeUpdater, new()
        {
            m_UpdaterArray[phase]?.Dispose();
            var updater = new T() {panel = m_Panel};
            m_UpdaterArray[phase] = updater;
        }

        public void Update()
        {
            for (int i = 0; i < (int)VisualTreeEditorUpdatePhase.Count; i++)
            {
                var updater = m_UpdaterArray[i];
                using (updater.profilerMarker.Auto())
                {
                    updater.Update();
                }
            }
        }

        public void UpdateVisualTreePhase(VisualTreeEditorUpdatePhase phase)
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
                var updater = m_UpdaterArray[i];
                using (updater.profilerMarker.Auto())
                {
                    updater.OnVersionChanged(ve, versionChangeType);
                }
            }
        }

        private void SetDefaultUpdaters()
        {
            SetUpdater<VisualTreeAssetChangeTrackerUpdater>(VisualTreeEditorUpdatePhase.AssetChange);
        }
    }
}
