// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.CSO;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    internal interface IReadOnlyData { }

    internal interface IComponent<TData> : IStateComponent where TData : struct, IReadOnlyData
    {
        TData readonlyData { get; }
        bool IsDirty();
        void Update();
    }

    /// <summary>
    /// <![CDATA[To implement a Component, use Component<TData> instead]]>
    /// </summary>
    /// Note: this class should probably be internal somehow...
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal abstract class Component : IStateComponent, IDisposable
    {
        public uint CurrentVersion { get; private set; } = 1;
        uint LastUpdateVersion { get; set; } = 0;

        protected abstract void UpdateData();
        protected abstract void CheckForExternalChanges();
        public virtual void Dispose() { }

        public void Update()
        {
            UpdateData();
            LastUpdateVersion = CurrentVersion;
        }

        public void MarkAsDirty()
        {
            // unchecked: wrap around on overflow without exception
            unchecked
            {
                CurrentVersion++;
            }
        }

        public bool HasChanges()
        {
            if (IsDirty())
                return true;

            CheckForExternalChanges();
            return IsDirty();
        }

        public bool IsDirty()
        {
            return LastUpdateVersion < CurrentVersion;
        }

        public void PurgeOldChangesets(uint untilVersion) { }

        public UpdateType GetUpdateType(StateComponentVersion observerVersion)
        {
            return UpdateType.Complete;
        }

        //Explicit empty implementation, since we don't use it
        void IStateComponent.OnAddedToState(IState state) { }

        //Explicit empty implementation, since we don't use it
        void IStateComponent.OnRemovedFromState(IState state) { }

        public UpdateScope UpdateScope()
        {
            return new UpdateScope(this);
        }
    }

    /// <summary>
    /// Use this class to implement a component.
    /// A component communicates with the model to generate read-only data used by the UI.
    /// </summary>
    /// <typeparam name="TData">A read-only object, ideally a `readonly struct`.</typeparam>
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal abstract class Component<TData> : Component, IComponent<TData>
        where TData : struct, IReadOnlyData
    {
        public TData readonlyData { get; private set; }

        /// <summary>
        /// Use this method to generate a new snapshot of the component. This will only be called if the component is dirty.
        /// </summary>
        protected abstract TData GenerateReadOnlyData();

        /// <summary>
        /// Use this method to check if the data was changed from outside of the window.
        /// Warning: this method will be called on each Update of the editor, so try to limit the amount of work.
        /// </summary>
        protected override void CheckForExternalChanges() { }

        public Type GetDataType()
        {
            return typeof(TData);
        }

        protected sealed override void UpdateData()
        {
            readonlyData = GenerateReadOnlyData();
        }
    }
}
