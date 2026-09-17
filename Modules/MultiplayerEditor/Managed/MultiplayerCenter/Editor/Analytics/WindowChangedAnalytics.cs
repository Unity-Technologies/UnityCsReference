// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.Center.Editor.Analytics
{
    /// <summary>
    /// Describes the navigation transition that triggered a <see cref="WindowChangedEvent"/>.
    /// </summary>
    internal enum WindowTransition : ushort
    {
        /// <summary>The Multiplayer Center window was opened.</summary>
        Opened = 0,

        /// <summary>The Multiplayer Center window was closed.</summary>
        Closed = 1
    }

    /// <summary>
    /// Analytics data sent when the Multiplayer Center window is opened or closed.
    /// </summary>
    [Serializable]
    internal struct WindowChangedData : IAnalytic.IData
    {
        /// <summary>
        /// The type of window transition that occurred
        /// </summary>
        public string windowTransition;

        public WindowChangedData(WindowTransition transition)
        {
            windowTransition = transition.ToString("G");
        }
    }

    /// <summary>
    /// Event fired when the Multiplayer Center window is opened or closed.
    /// </summary>
    [AnalyticInfo(eventName: MultiplayerCenterAnalyticsConstants.k_WindowChanged, vendorKey: MultiplayerCenterAnalyticsConstants.k_VendorKey)]
    internal class WindowChangedEvent : AnalyticsEvent<WindowChangedEvent, WindowChangedData>
    {
    }
}
