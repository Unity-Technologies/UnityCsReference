// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    /// <summary>
    /// Base class for VisualElements that bind to an ISequenceViewModel
    /// Inherit from SequenceElement if you only need to interact with ISequenceViewModel,
    /// not the concrete ViewModel type.
    /// </summary>
    abstract class SequenceElement : SequenceElement<ISequenceViewModel> { }

    /// <summary>
    /// <![CDATA[
    /// Base generic class for VisualElements that bind to an ISequenceViewModel
    /// Inherit from SequenceElement<TViewModel> if you need to directly interact
    /// with a concrete ISequenceViewModel-derived class.
    /// ]]>
    /// </summary>
    abstract class SequenceElement<TViewModel> : VisualElement where TViewModel : class, ISequenceViewModel
    {
        /// <summary>
        /// Initializes and binds the SequenceElement to the ViewModel.
        /// </summary>
        /// <param name="viewModel">The ViewModel to bind to.</param>
        public void Initialize(TViewModel viewModel)
        {
            BindViewModel(viewModel);
        }

        /// <summary>
        /// Binds the SequenceElement to the provided ViewModel.
        /// </summary>
        /// <remarks>
        /// Bind may call <see cref="RegisterListeners"/> or <see cref="UnregisterListeners"/> on derived classes to manage listeners on the provided ViewModel.
        /// Override this method to implement the initialization of your SequenceElement-derived instance with the ViewModel.
        /// </remarks>
        /// <param name="viewModel">The ViewModel to bind to.</param>
        /// <exception cref="ArgumentException">The provided ViewModel is null.</exception>
        protected virtual void BindViewModel(TViewModel viewModel)
        {
            bool vmChanged = ViewModel != viewModel;
            if (vmChanged)
                UnregisterListenersIfPossible();

            ViewModel = viewModel;

            if (viewModel == null)
                throw new ArgumentException("SequenceElement cannot be initialized with a null ViewModel ", nameof(viewModel));

            if (vmChanged)
                RegisterListenersIfPossible();
        }

        /// <summary>
        /// The ViewModel bound to the SequenceElement.
        /// </summary>
        protected TViewModel ViewModel { get; private set; }

        /// <summary>
        /// Default action implementation for events.
        /// Registers Listeners on AttachToPanel Events, Unregisters Listeners on DetachFromPanelEvent.
        /// </summary>
        /// <remarks>This method is not expected to be called manually. It is normally invoked by the UI system.</remarks>
        /// <seealso cref="RegisterListeners"/>
        /// <seealso cref="UnregisterListeners"/>
        /// <param name="evt">The event received.</param>
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            switch (evt)
            {
                case AttachToPanelEvent:
                    m_Attached = true;
                    RegisterListenersIfPossible();
                    break;
                case DetachFromPanelEvent:
                    UnregisterListenersIfPossible();
                    m_Attached = false;
                    break;
            }
        }

        /// <summary>
        /// The listener to bind to SequenceData in <see cref="RegisterListeners"/>.
        /// When this property is overridden, the provided listener will be automatically registered during the Bind operation.
        /// </summary>
        protected virtual Action<SequenceData> SequenceListener => null;

        /// <summary>
        /// The listener to bind to PlayerData in <see cref="RegisterListeners"/>.
        /// When this property is overridden, the provided listener will be automatically registered during the Bind operation.
        /// </summary>
        protected virtual Action<PlayerData> PlayerListener => null;

        /// <summary>
        /// The listener to bind to ViewData in <see cref="RegisterListeners"/>.
        /// When this property is overridden, the provided listener will be automatically registered during the Bind operation.
        /// </summary>
        protected virtual Action<ViewData> ViewListener => null;

        /// <summary>
        /// The listener to bind to ViewData in <see cref="RegisterListeners"/>.
        /// When this property is overridden, the provided listener will be automatically registered during the Bind operation.
        /// </summary>
        protected virtual Action<SelectionData> SelectionListener => null;

        /// <summary>
        /// Registers listeners to the ViewModel bound to the SequenceElement.
        /// Automatically registers the listeners provided by <see cref="SequenceListener"/>, <see cref="PlayerListener"/>,
        /// <see cref="ViewListener"/> and <see cref="SelectionListener"/>.
        /// </summary>
        /// <remarks>
        /// Override this to register additional listeners.
        /// Overrides are expected to call the base class implementation.
        /// </remarks>
        protected virtual void RegisterListeners()
        {
            if (!m_HasRegisteredListeners && ViewModel is { } viewModel)
            {
                if (SequenceListener is { } sequenceAction)
                    viewModel.ListenTo(sequenceAction);
                if (PlayerListener is { } playerAction)
                    viewModel.ListenTo(playerAction);
                if (ViewListener is { } viewAction)
                    viewModel.ListenTo(viewAction);
                if (SelectionListener is { } selectionAction)
                    viewModel.ListenTo(selectionAction);

                m_HasRegisteredListeners = true;
            }
        }

        /// <summary>
        /// Releases listeners from the ViewModel bound to the SequenceElement.
        /// Automatically releases the listeners provided by <see cref="SequenceListener"/>, <see cref="PlayerListener"/>,
        /// <see cref="ViewListener"/> and <see cref="SelectionListener"/>.
        /// </summary>
        /// <remarks>
        /// Override this to release additional listeners.
        /// Overrides are expected to call the base class implementation.
        /// </remarks>
        protected virtual void UnregisterListeners()
        {
            if (m_HasRegisteredListeners && ViewModel is { } viewModel)
            {
                if (SequenceListener is { } sequenceAction)
                    viewModel.Detach(sequenceAction);
                if (PlayerListener is { } playerAction)
                    viewModel.Detach(playerAction);
                if (ViewListener is { } viewAction)
                    viewModel.Detach(viewAction);
                if (SelectionListener is { } selectionAction)
                    viewModel.Detach(selectionAction);

                m_HasRegisteredListeners = false;
            }
        }

        void RegisterListenersIfPossible()
        {
            if (ViewModel != null && m_Attached)
                RegisterListeners();
        }

        void UnregisterListenersIfPossible()
        {
            if (ViewModel != null && m_Attached)
                UnregisterListeners();
        }

        bool m_HasRegisteredListeners;
        bool m_Attached;
    }
}
