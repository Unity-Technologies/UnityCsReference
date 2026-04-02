// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Time;
using Unity.Timeline.Foundation.Commands.ViewData;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    partial class SequenceView : SequenceTreeView
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new SequenceView();
        }

        const string k_Name = "sequenceView";
        const string k_Class = "sequenceView";
        static readonly StylesheetResource k_Stylesheet = Internals.UIResources.StylesheetFactory.Get<SequenceView>();

        readonly SequenceElementBuilder m_Builder = new();

        public MoveManipulation moveManipulation { get; }
        public TrimManipulation trimManipulation { get; }
        protected IManipulationContextProvider contextProvider => m_ManipulationContextProvider;

        ManipulationContextProvider m_ManipulationContextProvider;
        SelectionManipulator m_SelectionManipulator;
        MoveManipulator m_MoveManipulator;
        EdgeManipulator m_EdgeManipulator;

        public SequenceView()
        {
            name = k_Name;
            this.AddToTimelineClassList(k_Class);
            k_Stylesheet.ApplyTo(this);

            CreateTrackHeaderElement = m_Builder.BuildTrackHeaderElement;
            CreateTrackElement = m_Builder.BuildTrackElement;
            CreateItemElement = m_Builder.BuildItemElement;

            TimeChanged += OnTimeChanged;
            m_ManipulationContextProvider = new ManipulationContextProvider(this);

            trimManipulation = new TrimManipulation();
            this.AddManipulator(m_EdgeManipulator = new EdgeManipulator(m_ManipulationContextProvider, Canvas));
            m_EdgeManipulator.AddEdgeManipulation(trimManipulation);

            moveManipulation = new MoveManipulation();
            this.AddManipulator(m_MoveManipulator = new MoveManipulator(m_ManipulationContextProvider, Canvas));
            m_MoveManipulator.SetMoveManipulation(moveManipulation);

            ContentsViewport.AddManipulator(m_SelectionManipulator = new SelectionManipulator(Canvas));

            VerticalScroller.RegisterCallback<ChangeEvent<float>>(OnVerticalScroll);
            RegisterCallback<DisplayRangeChangeEvent>(OnDisplayRangeChanged);
        }

        protected override void BindViewModel(ISequenceViewModel vm)
        {
            base.BindViewModel(vm);

            m_Builder.viewModel = ViewModel;

            if (ViewModel != null)
            {
                SetSequence(ViewModel.sequenceData.sequence);

                m_ManipulationContextProvider.viewModel = ViewModel;
                m_SelectionManipulator.viewModel = ViewModel;
            }
            else
            {
                SetSequence(null);
            }
        }

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            ViewModel.ListenTo<ViewData>(ViewDataChanged);
            ViewModel.ListenTo<SequenceData>(SequenceDataChanged);
            ViewModel.ListenTo<SelectionData>(SelectionDataChanged);
            ViewModel.ListenTo<TimeData>(TimeDataChanged);
        }

        protected override void UnregisterListeners()
        {
            base.UnregisterListeners();
            ViewModel.Detach<ViewData>(ViewDataChanged);
            ViewModel.Detach<SequenceData>(SequenceDataChanged);
            ViewModel.Detach<SelectionData>(SelectionDataChanged);
            ViewModel.Detach<TimeData>(TimeDataChanged);
        }

        void SequenceDataChanged(SequenceData sequenceData)
        {
            ApplySequenceDiff(sequenceData.lastDiff);
        }

        void SelectionDataChanged(SelectionData selectionData)
        {
            ApplySelection(selectionData);
        }

        void ViewDataChanged(ViewData viewData)
        {
            SetDisplayRange(viewData.displayRange);
            VerticalScroller.value = viewData.verticalScrollOffset;
            ControlsWidth = viewData.headerWidth;
        }

        void TimeDataChanged(TimeData timeData)
        {
            SetDisplayTransform(timeData.localToDisplayTimeTransform);
            SetTime(timeData.DisplayToLocal(timeData.displayTime));
        }

        void OnTimeChanged(DiscreteTime time)
        {
            ViewModel.Dispatch(new SetLocalTime(time));
        }

        void OnDisplayRangeChanged(DisplayRangeChangeEvent evt)
        {
            ViewModel.Dispatch(new ChangeDisplayRange(evt.newValue));
        }

        void OnVerticalScroll(ChangeEvent<float> evt)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (ViewModel.viewData.verticalScrollOffset != evt.newValue)
                ViewModel.Dispatch(new ChangeVerticalScrollOffset(evt.newValue));
        }

        public void SetManipulationHandler(IManipulationHandler handler)
        {
            m_ManipulationContextProvider.manipulationHandler = handler;
        }

        protected void AddEdgeManipulation(IEdgeManipulation manipulation)
        {
            m_EdgeManipulator.AddEdgeManipulation(manipulation);
        }

        public void SetManipulationEdgeSnap(bool edgeSnap)
        {
            m_MoveManipulator.edgeSnap = edgeSnap;
            m_EdgeManipulator.edgeSnap = edgeSnap;
        }

        public void SetManipulatorEnabled(bool enabled)
        {
            moveManipulation.enabled = enabled;
            m_EdgeManipulator.enabled = enabled;
        }

        public void ShowManipulationDebugOverlay(bool state)
        {
            m_MoveManipulator.showEdgeSnapDebug = state;
        }

        public void RegisterTrackBuilder<TData>(Func<TrackElementContext, TrackHeaderElement> builder)
            where TData : ITrackMetadata
        {
            m_Builder.trackHeaderBuilder.Register<TData>(builder);
        }

        public void RegisterDefaultTrackBuilder(Func<TrackElementContext, TrackHeaderElement> builder)
        {
            m_Builder.trackHeaderBuilder.RegisterDefault(builder);
        }

        public void RegisterTrackBuilder<TData>(Func<TrackElementContext, TrackElement> builder)
            where TData : ITrackMetadata
        {
            m_Builder.trackContentsBuilder.Register<TData>(builder);
        }

        public void RegisterItemBuilder<TData>(Func<ItemElementContext, ItemElement> builder)
            where TData : IItemContent
        {
            m_Builder.itemBuilder.Register<TData>(builder);
        }

        public void RegisterDefaultItemBuilder(Func<ItemElementContext, ItemElement> builder)
        {
            m_Builder.itemBuilder.RegisterDefault(builder);
        }
    }
}
