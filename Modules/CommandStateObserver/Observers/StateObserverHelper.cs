// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace Unity.CSO
{
    static partial class StateObserverHelper
    {
        /// <summary>
        /// The currently active observer.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static IStateObserver CurrentObserver { get; set; }
    }
}
