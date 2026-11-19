// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Timeline.Foundation.ViewModel
{
    class WindowViewModel : IDisposable
    {
        public event Action CurrentViewModelChanged;

        public ISequenceViewModel currentViewModel => current;
        public SequenceViewModel current { get; private set; }

        public virtual void Update()
        {
            current?.Update();
        }

        public virtual void Dispose()
        {
            current?.Dispose();
            current = null;
        }

        public void SetCurrent(SequenceViewModel viewModel)
        {
            current?.DetachAll();
            current?.Dispose();

            current = viewModel;
            current?.Initialize();
            CurrentViewModelChanged?.Invoke();
        }
    }
}
