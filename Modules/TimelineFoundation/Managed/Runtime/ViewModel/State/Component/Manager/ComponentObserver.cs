// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.CSO;

namespace Unity.Timeline.Foundation.ViewModel.Internals
{
    interface IComponentUpdater
    {
        IStateComponent Component { get; }
        Type DataType { get; }

        void Process();
        void Notify(ViewModelBase vm);
    }

    sealed class ComponentUpdater<TData> : IComponentUpdater where TData : struct, IReadOnlyData
    {
        public event Action<TData> OnComponentChanged;
        public IStateComponent Component => m_Component;
        public Type DataType => typeof(TData);

        readonly IComponent<TData> m_Component;
        bool m_WasUpdated;

        public ComponentUpdater(IComponent<TData> component)
        {
            m_Component = component;
        }

        public void Process()
        {
            if (m_Component.IsDirty())
            {
                m_Component.Update();
                m_WasUpdated = true;
            }
        }

        public void Notify(ViewModelBase vm)
        {
            if (m_WasUpdated)
            {
                vm.OnComponentChanged_Internal(m_Component);
                OnComponentChanged?.Invoke(m_Component.readonlyData);
                m_WasUpdated = false;
            }
        }
    }
}
