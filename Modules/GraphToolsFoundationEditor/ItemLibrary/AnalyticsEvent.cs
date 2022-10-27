// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Data used for analytics purposed when an even occurs with the <see cref="ItemLibraryLibrary_Internal"/>.
    /// </summary>
    /// TODO: VladN made analytics internal until
    struct ItemLibraryAnalyticsEvent_Internal
    {
        /// <summary>
        /// Type of event logged.
        /// </summary>
        public ItemLibraryAnalyticsEventKind_Internal Kind;

        /// <summary>
        /// The current query used to search.
        /// </summary>
        public string CurrentSearchFieldText;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryAnalyticsEvent_Internal"/> class.
        /// </summary>
        /// <param name="eventKind">The type of event logged.</param>
        /// <param name="currentSearchFieldText">The current query used to search.</param>
        public ItemLibraryAnalyticsEvent_Internal(ItemLibraryAnalyticsEventKind_Internal eventKind, string currentSearchFieldText)
        {
            Kind = eventKind;
            CurrentSearchFieldText = currentSearchFieldText;
        }
    }

    /// <summary>
    /// Types of events handled.
    /// </summary>
    enum ItemLibraryAnalyticsEventKind_Internal
    {
        /// <summary>
        /// The user hasn't chosen an item yet
        /// </summary>
        Pending,

        /// <summary>
        /// The user has picked an item.
        /// </summary>
        Picked,

        /// <summary>
        /// The user has cancelled his search.
        /// </summary>
        Cancelled
    }
}
