// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.CSO
{
    static class StateObserverHelper
    {
        /// <summary>
        /// The currently active observer.
        /// </summary>
        public static IStateObserver CurrentObserver { get; set; }
    }
}
