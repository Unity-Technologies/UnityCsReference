// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using DataModeSupportHandler = UnityEditor.DeclareDataModeSupportAttribute.DataModeSupportHandler;

namespace UnityEditor
{
    /// <summary>
    /// Options for the different modes of an <see cref="EditorWindow"/>.
    /// </summary>
    //
    // Dev note:
    //
    // There should be no reason to ever add to this enum. It exists only to express the duality between author-time
    // and runtime data. If you feel like you need to add to this, please reconsider: it will likely break the
    // `Authoring VS Runtime` feature for DOTS.
    //
    // Thank you for your consideration and have a great day!
    //
    // Sincerely,
    // The #dots-editor team
    //
    [Serializable]
    public enum DataMode // Values must be kept in sync with `DataMode.h`
    {
        /// <summary>
        /// Represents a situation or context in which the usage of <see cref="DataMode"/> is not applicable.
        /// </summary>
        /// <remarks> <para>
        /// This mode disables the DataMode switch in the docking area.
        /// </para> </remarks>
        Disabled = 0,
        /// <summary>
        /// Uses this mode where only authoring data is available.
        /// </summary>
        /// <remarks> <para>
        /// In this mode, only authoring data is available. When exiting Play mode, Unity retains authoring data.
        /// </para> </remarks>
        Authoring = 1,
        /// <summary>
        /// Uses this mode where a mix of authoring and runtime data is available.
        /// </summary>
        /// <remarks> <para>
        /// In this mode, a mixture of authoring and runtime data is available.
        /// When exiting Play mode, Unity loses runtime data. However, it retains any authoring data.
        /// </para> </remarks>
        Mixed = 2,
        /// <summary>
        /// Uses this mode where only runtime data is available.
        /// </summary>
        /// <remarks> <para>
        /// In this mode, only runtime data is available. When exiting Play mode, Unity loses runtime data.
        /// </para> </remarks>
        Runtime = 3
    }

    /// <summary>
    /// Container for the different parameters of the <see cref="IDataModeController.dataModeChanged"/> event.
    /// </summary>
    /// <param name="nextDataMode"> DataMode to which the <see cref="EditorWindow"/> should change.</param>
    /// <param name="changedThroughUI"> Whether the change was initiated by the DataMode switcher UI
    /// at the top-right of the Editor window.</param>
    public readonly struct DataModeChangeEventArgs
    {
        public readonly DataMode nextDataMode;
        public readonly bool changedThroughUI;

        public DataModeChangeEventArgs(DataMode nextDataMode, bool changedThroughUI)
        {
            this.nextDataMode = nextDataMode;
            this.changedThroughUI = changedThroughUI;
        }
    }

    /// <summary>
    /// Interface with which any <see cref="EditorWindow"/> can interact with <see cref="DataMode"/> functionalities.
    /// To obtain an instance, use <see cref="EditorWindow.dataModeController"/>>
    /// </summary>
    /// <remarks> <para>
    /// This interface displays a switch in the docking area when the window is visible and has
    /// more than one supported DataModes.
    /// </para> </remarks>
    public interface IDataModeController
    {
        /// <summary>
        /// Returns the <see cref="DataMode"/> currently active for the <see cref="EditorWindow"/> that
        /// owns this instance of IDataModeController.
        /// </summary>
        DataMode dataMode { get; }

        /// <summary>
        /// Event for subscribing to <see cref="DataMode"/> changes.
        /// </summary>
        /// <remarks> <para>
        /// This method accepts <see cref="DataModeChangeEventArgs"/>>.
        /// For example, you can register to this method to update the contents of the window for the given data mode.
        /// </para> </remarks>
        event Action<DataModeChangeEventArgs> dataModeChanged;

        /// <summary>
        /// Updates the list of <see cref="DataMode"/>s that the <see cref="EditorWindow"/> supports,
        /// and sets the preferred DataMode to be used when the DataMode switcher UI is set to Automatic.
        /// </summary>
        /// <remarks> <para>
        /// That list of the DataModes the Editor window supports varies based on a number of factors,
        /// so it should only contain the DataModes available to the current context.
        /// For example, a window might support the <see cref="DataMode.Authoring"/> and <see cref="DataMode.Runtime"/>
        /// modes when in Edit mode, and the <see cref="DataMode.Mixed"/> and <see cref="DataMode.Runtime"/> modes when
        /// in Play mode. A common pattern for that case is to store two lists internally and use
        /// <see cref="EditorApplication.isPlaying"/> to select which one to return.
        /// <param name="supportedDataMode"> A list of the supported DataModes. </param>
        /// <param name="preferredDataMode">
        /// Preferred DataMode to use given the current context when the DataMode switcher UI is set to Automatic.
        /// </param>
        /// </para> </remarks>
        void UpdateSupportedDataModes(IList<DataMode> supportedDataMode, DataMode preferredDataMode);

        /// <summary>
        /// Requests a <see cref="DataMode"/> change for the <see cref="EditorWindow"/>.
        /// </summary>
        /// <remarks> <para>
        /// If the DataMode switcher UI is currently set to Automatic, the Editor window also
        /// changes to that preferred DataMode.
        /// <seealso cref="IDataModeController.UpdateSupportedDataModes"/>>
        /// </para> </remarks>
        /// <param name="newDataMode">
        /// The DataMode to which the Editor window should change.
        /// </param>
        /// <returns>
        /// Whether the Editor window has accepted the requested DataMode change.
        /// </returns>>
        bool TryChangeDataMode(DataMode newDataMode);
    }

    // DataModeController handles DataMode related actions internally.
    // Each Editor window has a DataModeController instance.
    [Serializable]
    internal sealed class DataModeController : IDataModeController
    {
        static readonly DataMode[] k_DefaultModes = Array.Empty<DataMode>();

        public event Action<DataModeChangeEventArgs> dataModeChanged;

        [SerializeField] DataMode m_DataMode = DataMode.Disabled;
        public DataMode dataMode
        {
            get => m_DataMode;
            private set => m_DataMode = value;
        }

        [SerializeField] DataMode m_PreferredDataMode = DataMode.Disabled;
        public DataMode preferredDataMode
        {
            get => m_PreferredDataMode;
            private set => m_PreferredDataMode = value;
        }

        [SerializeField] DataMode[] m_SupportedDataModes = k_DefaultModes;
        public IList<DataMode> supportedDataModes
        {
            get => m_SupportedDataModes;
            private set => m_SupportedDataModes = value.ToArray();
        }

        [SerializeField] internal bool isAutomatic = true;

        readonly List<DataMode> m_DataModeSanitizationCache = new List<DataMode>(3); // Number of modes, minus `Disabled`

        public void UpdateSupportedDataModes(IList<DataMode> supported, DataMode preferred)
        {
            SanitizeSupportedDataModesList(supported.ToList(), m_DataModeSanitizationCache);

            supportedDataModes = m_DataModeSanitizationCache.Count != 0 ? m_DataModeSanitizationCache : k_DefaultModes;

            preferredDataMode = supportedDataModes.Count switch
            {
                0 => DataMode.Disabled,
                1 => supportedDataModes[0],
                _ => supportedDataModes.Contains(preferred) ? preferred : supportedDataModes[0]
            };

            if (!isAutomatic || dataMode == preferredDataMode)
                return;

            // Recover if automatic
            dataMode = preferredDataMode;
            dataModeChanged?.Invoke(new DataModeChangeEventArgs(dataMode, false));
        }

        static void SanitizeSupportedDataModesList(IReadOnlyList<DataMode> originalList, List<DataMode> sanitizedList)
        {
            sanitizedList.Clear();

            foreach (var mode in originalList)
            {
                if (mode == DataMode.Disabled)
                    continue; // Never list `DataMode.Disabled`

                if (sanitizedList.Contains(mode))
                    continue; // Prevent duplicate entries

                sanitizedList.Add(mode);
            }

            // Ensure we are displaying the data modes in a predefined order, regardless of
            // the order in which the user defined their list.
            sanitizedList.Sort();
        }

        public bool ShouldDrawDataModesSwitch()
        {
            return dataMode != DataMode.Disabled
                   // We don't want to show DataMode switch if there are not
                   // at least 2 modes supported at the current moment.
                   && supportedDataModes.Count > 1;
        }

        public bool TryChangeDataMode(DataMode newDataMode)
        {
            // Only change if currently in automatic mode
            if (!isAutomatic || dataMode == newDataMode || !supportedDataModes.Contains(newDataMode))
                return false;

            dataMode = newDataMode;
            dataModeChanged?.Invoke(new DataModeChangeEventArgs(newDataMode, false));
            return true;
        }

        // Invoked when user interacts with the DataMode dropdown menu, for internal use only.
        internal void SwitchToAutomatic()
        {
            if (isAutomatic)
                return;

            isAutomatic = true;

            if (dataMode == preferredDataMode)
                return;

            // If the DataMode is not supported in current context, we fall back to default one.
            dataMode = preferredDataMode;
            dataModeChanged?.Invoke(new DataModeChangeEventArgs(dataMode, true));
        }

        // Invoked when user interacts with the DataMode dropdown men, for internal use only.
        internal void SwitchToStickyDataMode(DataMode stickyDataMode)
        {
            isAutomatic = false;

            if (dataMode == stickyDataMode)
                return;

            dataMode = supportedDataModes.Contains(stickyDataMode)
                ? stickyDataMode
                : preferredDataMode;

            dataModeChanged?.Invoke(new DataModeChangeEventArgs(dataMode, true));
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("IDataModeHandler has been deprecated, please use EditorWindow.dataModeController instead.", false)]
    public interface IDataModeHandler
    {
        DataMode dataMode { get; }
        IReadOnlyList<DataMode> supportedDataModes { get; }
        bool IsDataModeSupported(DataMode mode);
        void SwitchToNextDataMode();
        void SwitchToDataMode(DataMode mode);
        void SwitchToDefaultDataMode();
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("IDataModeHandlerAndDispatcher has been deprecated, please use EditorWindow.dataModeController instead.", false)]
    public interface IDataModeHandlerAndDispatcher : IDataModeHandler
    {
        event Action<DataMode> dataModeChanged;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    class DeclareDataModeSupportAttribute : Attribute
    {
        public delegate void DataModeSupportHandler(UnityObject activeSelection, UnityObject activeContext, HashSet<DataMode> supportedModes);

        [RequiredSignature]
        static void signature(UnityObject activeSelection, UnityObject activeContext, HashSet<DataMode> supportedModes)
        {
        }
    }

    static class DataModeSupportUtils
    {
        static readonly List<DataModeSupportHandler> k_Handlers = new(4);

        static DataModeSupportUtils()
        {
            Rebuild();
        }

        public static void GetDataModeSupport(UnityObject activeSelection, UnityObject activeContext, HashSet<DataMode> supportedDataModes)
        {
            foreach (var handler in k_Handlers)
            {
                handler(activeSelection, activeContext, supportedDataModes);
            }
        }

        static void Rebuild()
        {
            k_Handlers.Clear();
            var candidates = TypeCache.GetMethodsWithAttribute<DeclareDataModeSupportAttribute>();
            var attributeType = typeof(DeclareDataModeSupportAttribute);
            foreach (var candidate in candidates)
            {
                if (!AttributeHelper.MethodMatchesAnyRequiredSignatureOfAttribute(candidate, attributeType))
                    continue;

                if (Delegate.CreateDelegate(typeof(DataModeSupportHandler), candidate) is DataModeSupportHandler handler)
                    k_Handlers.Add(handler);
            }
        }
    }
}
