// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityObject = UnityEngine.Object;
using DataModeSupportHandler = UnityEditor.DeclareDataModeSupportAttribute.DataModeSupportHandler;

namespace UnityEditor
{
    /// <summary>
    /// Options for the different modes of an <see cref="EditorWindow"/> that implements
    /// <see cref="IDataModeHandler"/> or <see cref="IDataModeHandlerAndDispatcher"/>.
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
    public enum DataMode // Values must be kept in sync with `DataMode.h`
    {
        /// <summary>
        /// Represents a situation or context in which the usage of data modes is not applicable.
        /// </summary>
        /// <remarks> <para>
        /// This mode informs the docking area that the data modes switch should now be displayed.
        /// </para> </remarks>
        Disabled = 0,
        /// <summary>
        /// Uses a mode where only authoring data is available.
        /// </summary>
        /// <remarks> <para>
        /// In this mode, only authoring data is available. When exiting Play mode, Unity retains authoring data.
        /// </para> </remarks>
        Authoring = 1,
        /// <summary>
        /// Uses a mode where a mix of authoring and runtime data is available.
        /// </summary>
        /// <remarks> <para>
        /// In this mode, a mixture of authoring and runtime data is available. **Important:** When exiting Play mode,
        /// Unity loses runtime data. However, it retains any authoring data.
        /// </para> </remarks>
        Mixed = 2,
        /// <summary>
        /// Uses a mode where only runtime data is available.
        /// </summary>
        /// <remarks> <para>
        /// In this mode, only runtime data is available. **Important:** When exiting Play mode, Unity loses runtime
        /// data.
        /// </para> </remarks>
        Runtime = 3
    }

    /// <summary>
    /// Implement this interface to allow an <see cref="EditorWindow"/> to handle <see cref="DataMode"/> changes.
    /// </summary>
    /// <remarks> <para>
    /// This interface displays a switch in the docking area when the window is visible and lists the supported modes in
    /// the contextual menu for that window. Use this interface if your window only needs to react to direct user
    /// interactions with the data mode switch or the contextual menu. If your window needs to change its state based on
    /// other factors, like entering or exiting play mode, you should implement
    /// <see cref="IDataModeHandlerAndDispatcher"/> instead.
    /// </para> </remarks>
    public interface IDataModeHandler
    {
        /// <summary>
        /// Returns the <see cref="DataMode"/> currently active for the implementor <see cref="EditorWindow"/>.
        /// </summary>
        /// <remarks> <para>
        /// Unity does not serialize or store this value. It is the window's responsibility to do so.
        /// </para> </remarks>
        DataMode dataMode { get; }

        /// <summary>
        /// <para>
        /// A list of the <see cref="DataMode"/>s the <see cref="EditorWindow"/> supports.
        /// </para>
        /// <para>
        /// That list of the <see cref="DataMode"/>s the <see cref="EditorWindow"/> supports varies based
        /// on a number of factors, so it should only contain the modes available to the current context.
        /// For example, a window might support the <see cref="DataMode.Authoring"/> and <see cref="DataMode.Runtime"/>
        /// modes when in Edit mode, and the <see cref="DataMode.Mixed"/> and <see cref="DataMode.Runtime"/> modes when
        /// in Play mode. A common pattern for that case is to store two lists internally and use
        /// <see cref="EditorApplication.isPlaying"/> to select which one to return.
        /// </para>
        /// </summary>
        IReadOnlyList<DataMode> supportedDataModes { get; }

        /// <summary>
        /// Unity calls this method automatically before any call to <see cref="SwitchToDataMode"/> is made. If the
        /// method returns <code>false</code>, <see cref="SwitchToDefaultDataMode"/> is called instead.
        /// </summary>
        /// <param name="mode">
        /// The <see cref="DataMode"/> for which support is being tested.
        /// </param>
        /// <returns>
        /// Whether the <see cref="EditorWindow"/> currently supports the specified <see cref="DataMode"/>.
        /// </returns>
        bool IsDataModeSupported(DataMode mode);

        /// <summary>
        /// Unity calls this method automatically when a user clicks the <see cref="DataMode"/> switch in the docking
        /// area tied to the implementing <see cref="EditorWindow"/>.
        /// </summary>
        /// <remarks> <para>
        /// This method informs the window to change its <see cref="dataMode"/> to whatever mode should come after
        /// the current. In most cases, a window only supports two data modes at a time, but it is possible to support
        /// all three. Also, a window that supports all three modes might want the switch to only toggle between two
        /// specific modes and rely on the contextual menu to change to the third mode.
        /// </para> </remarks>
        void SwitchToNextDataMode();

        /// <summary>
        /// Unity calls this method automatically whenever the Editor wants an <see cref="EditorWindow"/> to be in a
        /// specific <see cref="DataMode"/>.
        /// </summary>
        /// <remarks> <para>
        /// By convention, Unity always calls <see cref="IsDataModeSupported"/> before calling this method. If the
        /// data mode is not supported, <see cref="SwitchToDefaultDataMode"/> is called instead.
        /// </para> </remarks>
        /// <param name="mode">
        /// The explicit data mode to which the Editor window should change.
        /// </param>
        void SwitchToDataMode(DataMode mode);

        /// <summary>
        /// Unity calls this method automatically whenever going to a requested <see cref="DataMode"/> is impossible
        /// because of the result of <see cref="IsDataModeSupported"/>.
        /// </summary>
        /// <remarks> <para>
        /// This method is a fallback to make sure the <see cref="EditorWindow"/> is always in a valid state.
        /// </para> </remarks>
        /// <seealso cref="SwitchToDataMode"/>
        void SwitchToDefaultDataMode();
    }

    /// <summary>
    /// Implement this interface to allow an <see cref="EditorWindow"/> to handle <see cref="DataMode"/> changes and
    /// alter its <see cref="IDataModeHandler.dataMode"/> internally.
    /// </summary>
    /// <remarks> <para>
    /// This interface displays a switch in the docking area when the window is visible and lists the supported modes in
    /// the contextual menu for that window. Use this interface if your window needs to control its mode internally
    /// based on factors other than the user directly interacting with the data mode switch or the contextual menu, for
    /// example, entering or exiting Play mode. If your window does not need to control its own mode, use
    /// <see cref="IDataModeHandler"/> instead.
    /// </para> </remarks>
    public interface IDataModeHandlerAndDispatcher : IDataModeHandler
    {
        /// <summary>
        /// <para>
        /// Calls the methods in its invocation list when the <see cref="DataMode"/> changes due to an external factor
        /// and passes the new data mode as an argument.
        /// </para>
        /// <para>
        /// An external factor refers to any action which results in a data mode change that Unity did not initiate
        /// directly through calling either <see cref="IDataModeHandler.SwitchToNextDataMode"/>,
        /// <see cref="IDataModeHandler.SwitchToDefaultDataMode"/>, or <see cref="IDataModeHandler.SwitchToDataMode"/>.
        /// </para>
        /// <para>
        /// For example, when entering or exiting Play mode, some windows might want to force a data mode switch.
        /// </para>
        /// </summary>
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
