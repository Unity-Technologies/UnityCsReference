// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.Center.Common.Analytics
{
    /// <summary>
    /// The type of interaction that the user has with a button in the getting started tab.
    /// </summary>
    [Obsolete("Use the new analytics events for multiplayer center analytics")]
    internal enum InteractionDataType
    {
        /// <summary>
        /// For a button that does something in the editor, e.g. a button that opens a window or imports a sample.
        /// </summary>
        CallToAction = 0,

        /// <summary>
        /// For a button that opens a URL in the browser (e.g. a documentation link).
        /// </summary>
        Link = 1,
    }

    /// <summary>
    /// For the object that provides the analytics functionality to send interaction events on some Onboarding section
    /// in the getting started tab.
    /// </summary>
    [Obsolete("Use the new analytics events for multiplayer center analytics")]
    internal interface IOnboardingSectionAnalyticsProvider
    {
        /// <summary>
        /// Send event for a button interaction in the getting started tab.
        /// </summary>
        /// <param name="type"> Whether it is a call to action or a link</param>
        /// <param name="displayName"> The name of the button in the UI</param>
        void SendInteractionEvent(InteractionDataType type, string displayName);
    }
}
