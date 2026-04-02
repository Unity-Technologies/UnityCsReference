// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    /// <summary>
    /// Base ViewModel for sequences
    /// Inheritors must provide components for SequenceData, ViewData, PlayerData and SelectionData
    /// </summary>
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal abstract class SequenceViewModel : ViewModelBase, ISequenceViewModel, IDisposable
    {
        public SequenceData sequenceData => GetData<SequenceData>();
        public ViewData viewData => GetData<ViewData>();
        public PlayerData playerData => GetData<PlayerData>();
        public SelectionData selectionData => GetData<SelectionData>();
        public TimeData timeData => GetData<TimeData>();

        bool m_Initialized;

        /// <summary>
        /// Registers components, reducers and listeners
        /// </summary>
        public void Initialize()
        {
            if (m_Initialized)
                return;

            RegisterComponents();
            RegisterReducers();
            PostRegister();
            m_Initialized = true;
        }

        public override void Update()
        {
            if (!m_Initialized)
                throw new InvalidOperationException("You must call Initialize() on the ViewModel before using it");
            base.Update();
        }

        /// <summary>
        /// Register your components in this method.
        /// For your SequenceViewModel-inherited class to be valid, it must at minimum register
        /// components that provide the following types:
        /// * SequenceData
        /// * ViewData
        /// * PlayerData
        /// * SelectionData
        /// * TimeData
        /// </summary>
        protected abstract void RegisterComponents();

        /// <summary>
        /// Register your reducers in this method.
        /// Called after RegisterComponents
        /// </summary>
        protected abstract void RegisterReducers();

        /// <summary>
        /// Handle any post Register logic in this method.
        /// Called after RegisterComponents and RegisterReducers
        /// </summary>
        protected abstract void PostRegister();
    }
}
